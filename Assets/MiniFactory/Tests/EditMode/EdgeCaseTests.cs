using System.Collections.Generic;
using MiniFactory.Core.Config;
using MiniFactory.Core.Contracts;
using MiniFactory.Core.Game;
using MiniFactory.Core.Model;
using MiniFactory.Core.Save;
using MiniFactory.Core.Services;
using NUnit.Framework;

namespace MiniFactory.Tests.EditMode
{
    /// <summary>Edge-case: защита от эксплойтов, границы денег, offline-cap.</summary>
    public class EdgeCaseTests
    {
        private sealed class FakeClock : IClock
        {
            public long NowUnix { get; set; } = 1_000_000;
        }

        private sealed class FakeSave : ISaveService
        {
            public PlayerProgress Progress;
            public bool HasProgress() => Progress != null;
            public PlayerProgress Load() => Progress;
            public void Save(PlayerProgress progress) => Progress = progress;
            public void Delete() => Progress = null;
        }

        private sealed class FakeAnalytics : IAnalytics
        {
            public readonly List<(string name, IReadOnlyDictionary<string, object> pars)> Events
                = new List<(string, IReadOnlyDictionary<string, object>)>();
            public void Log(string eventName, IReadOnlyDictionary<string, object> parameters = null)
                => Events.Add((eventName, parameters));
            public int Count(string name) => Events.FindAll(e => e.name == name).Count;
        }

        private sealed class FakePurchases : IGamePurchaseService
        {
            public bool IsAvailable { get; set; } = true;
            public event System.Action<PurchaseOutcome> OnPurchaseCompleted { add { } remove { } }
            public void Initialize(IEnumerable<string> ids) { }
            public void Buy(string id) { }
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
                    Products = new ProductConfig[0],
                    Tutorial = new TutorialStep[0]
                };
                Model = new FactoryModel(Config);
                Economy = new EconomyService(Config);
                Game = new GameController(Config, Model, Economy, Save, Analytics, Clock, Purchases);
            }
        }

        [Test]
        public void Unlock_AlreadyUnlocked_Fails_NoSideEffects()
        {
            var h = new Harness();
            h.Model.AddBalance(100);

            var ok = h.Game.UnlockMachine("m1"); // уже открыта

            Assert.IsFalse(ok);
            Assert.AreEqual(100, h.Model.Balance, 0.0001);
        }

        [Test]
        public void Unlock_ExactBalance_Succeeds_BalanceZero()
        {
            var h = new Harness();
            h.Model.AddBalance(50); // ровно стоимость открытия m2

            var ok = h.Game.UnlockMachine("m2");

            Assert.IsTrue(ok);
            Assert.AreEqual(0, h.Model.Balance, 0.0001);
        }

        [Test]
        public void Initialize_OfflineLongerThanCap_CappedEndToEnd()
        {
            var h = new Harness(nowUnix: 1_000_000 + 86_400); // 1 день
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

            var report = h.Game.Initialize();

            Assert.AreEqual(7200, report.OfflineElapsedSeconds); // cap
            Assert.AreEqual(72_000, report.OfflineIncome, 0.0001);
        }

        [Test]
        public void Initialize_FutureLastActive_NoIncome_NoCrash()
        {
            var h = new Harness(nowUnix: 1_000_000);
            h.Save.Progress = new PlayerProgress
            {
                balance = 50,
                lastActiveUnix = 1_000_600, // в будущем
                machines = new[]
                {
                    new MachineSave { id = "m1", level = 1, unlocked = true }
                }
            };

            h.Game.Initialize();

            Assert.AreEqual(50, h.Model.Balance, 0.0001);
        }
    }
}
