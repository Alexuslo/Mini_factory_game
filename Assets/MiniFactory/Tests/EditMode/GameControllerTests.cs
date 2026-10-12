using System;
using System.Collections.Generic;
using System.Linq;
using MiniFactory.Core.Config;
using MiniFactory.Core.Contracts;
using MiniFactory.Core.Game;
using MiniFactory.Core.Model;
using MiniFactory.Core.Save;
using MiniFactory.Core.Services;
using NUnit.Framework;

namespace MiniFactory.Tests.EditMode
{
    /// <summary>
    /// Тесты игровых сценариев на контроллере: открытие/улучшение машин,
    /// offline-доход при старте, Boost, IAP, сохранение.
    /// </summary>
    public class GameControllerTests
    {
        private sealed class FakeClock : IClock
        {
            public long NowUnix { get; set; } = 1_000_000;
        }

        private sealed class FakeSave : ISaveService
        {
            public PlayerProgress Progress;
            public int SaveCalls;
            public bool HasProgress() => Progress != null;
            public PlayerProgress Load() => Progress?.machines != null ? Progress : null;
            public void Save(PlayerProgress progress) { Progress = progress; SaveCalls++; }
            public void Delete() { Progress = null; }
        }

        private sealed class FakeAnalytics : IAnalytics
        {
            public readonly List<(string name, IReadOnlyDictionary<string, object> pars)> Events
                = new List<(string, IReadOnlyDictionary<string, object>)>();

            public void Log(string eventName, IReadOnlyDictionary<string, object> parameters = null)
                => Events.Add((eventName, parameters));

            public int Count(string name) => Events.Count(e => e.name == name);
        }

        private sealed class FakePurchases : IGamePurchaseService
        {
            public bool IsAvailable { get; set; } = true;
            public readonly List<string> Bought = new List<string>();
            public event Action<PurchaseOutcome> OnPurchaseCompleted;
            public void Initialize(IEnumerable<string> ids) { }
            public void Buy(string id) => Bought.Add(id);
            public void Succeed(string id)
                => OnPurchaseCompleted?.Invoke(new PurchaseOutcome(true, id, null));
            public void Fail(string id, string error)
                => OnPurchaseCompleted?.Invoke(new PurchaseOutcome(false, id, error));
        }

        private sealed class Harness
        {
            public readonly FakeClock Clock = new FakeClock();
            public readonly FakeSave Save = new FakeSave();
            public readonly FakeAnalytics Analytics = new FakeAnalytics();
            public readonly FakePurchases Purchases = new FakePurchases();
            public readonly GameConfig Config;
            public readonly FactoryModel Model;
            public readonly EconomyService Economy;
            public readonly GameController Game;

            public Harness(long nowUnix = 1_000_000)
            {
                Clock.NowUnix = nowUnix;
                Config = new GameConfig
                {
                    MaxOfflineSeconds = 7200,
                    BoostDurationSeconds = 300,
                    BoostMultiplier = 3f,
                    BoostEnabled = true,
                    Machines = new[]
                    {
                        new MachineConfig { Id = "m1", UnlockCost = 0, BaseProduction = 10, UpgradeCostBase = 100 },
                        new MachineConfig { Id = "m2", UnlockCost = 50, BaseProduction = 50, UpgradeCostBase = 500 }
                    },
                    Products = new[]
                    {
                        new ProductConfig { Id = "coins_pack_small", CoinsReward = 1000 },
                        new ProductConfig { Id = "boost_pack", CoinsReward = 0, BoostSeconds = 600 }
                    },
                    Tutorial = new TutorialStep[0]
                };
                Model = new FactoryModel(Config);
                Economy = new EconomyService(Config);
                Game = new GameController(Config, Model, Economy, Save, Analytics, Clock, Purchases);
            }
        }

        [Test]
        public void FirstMachine_AvailableImmediately()
        {
            var h = new Harness();
            Assert.IsTrue(h.Model.GetMachine("m1").IsUnlocked);
            Assert.AreEqual(1, h.Model.GetMachine("m1").Level);
            Assert.IsFalse(h.Model.GetMachine("m2").IsUnlocked);
        }

        [Test]
        public void Unlock_EnoughBalance_SpendsAndLogs()
        {
            var h = new Harness();
            h.Game.Initialize();
            h.Model.AddBalance(100);

            var ok = h.Game.UnlockMachine("m2");

            Assert.IsTrue(ok);
            Assert.IsTrue(h.Model.GetMachine("m2").IsUnlocked);
            Assert.AreEqual(50, h.Model.Balance, 0.0001);
            Assert.AreEqual(1, h.Analytics.Count(AnalyticsEvents.MachineUnlocked));
        }

        [Test]
        public void Initialize_WithOldSave_AppliesOfflineIncome()
        {
            var h = new Harness(nowUnix: 1_000_600);
            h.Save.Progress = new PlayerProgress
            {
                balance = 0,
                lastActiveUnix = 1_000_000,
                machines = new[]
                {
                    new MachineSave { id = "m1", level = 1, unlocked = true },
                    new MachineSave { id = "m2", level = 0, unlocked = false }
                }
            };

            h.Game.Initialize();

            Assert.AreEqual(6000, h.Model.Balance, 0.0001); // 600 сек × 10/сек
            Assert.AreEqual(1, h.Analytics.Count(AnalyticsEvents.OfflineIncomeApplied));
        }

        [Test]
        public void Purchase_CoinsPack_Success_AddsReward()
        {
            var h = new Harness();
            h.Game.Initialize();

            h.Game.BuyProduct("coins_pack_small");
            h.Purchases.Succeed("coins_pack_small");

            Assert.AreEqual(1000, h.Model.Balance, 0.0001);
            Assert.AreEqual(1, h.Analytics.Count(AnalyticsEvents.PurchaseSucceeded));
        }

        [Test]
        public void ExperimentVariant_SurvivesSaveLoad()
        {
            var h = new Harness();
            h.Game.SetExperimentVariant("B");
            h.Game.Initialize();

            var h2 = new Harness();
            h2.Save.Progress = h.Save.Progress;
            h2.Game.Initialize();

            Assert.AreEqual("B", h2.Game.ExperimentVariant);
        }
    }
}
