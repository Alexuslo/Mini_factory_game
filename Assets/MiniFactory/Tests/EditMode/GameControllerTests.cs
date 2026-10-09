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
    /// offline-доход при старте, Boost. Все зависимости подменены фейками —
    /// ни сцены, ни реального времени, ни файлов.
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
            public PlayerProgress Load() => Progress;
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
            public (string, IReadOnlyDictionary<string, object>) First(string name)
                => Events.First(e => e.name == name);
        }

        private sealed class FakePurchases : IGamePurchaseService
        {
            public bool IsAvailable { get; set; } = true;
            public readonly List<string> Bought = new List<string>();

            public event Action<PurchaseOutcome> OnPurchaseCompleted;

            public void Initialize(IEnumerable<string> consumableProductIds) { }

            public void Buy(string productId) => Bought.Add(productId);

            public void Succeed(string productId)
                => OnPurchaseCompleted?.Invoke(new PurchaseOutcome(true, productId, null));

            public void Fail(string productId, string error)
                => OnPurchaseCompleted?.Invoke(new PurchaseOutcome(false, productId, error));
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
                        // первая — unlockCost 0 → доступна сразу (ТЗ п.1)
                        new MachineConfig { Id = "m1", UnlockCost = 0, BaseProduction = 10, UpgradeCostBase = 100 },
                        new MachineConfig { Id = "m2", UnlockCost = 50, BaseProduction = 50, UpgradeCostBase = 500 },
                        new MachineConfig { Id = "m3", UnlockCost = 500, BaseProduction = 200, UpgradeCostBase = 2000 }
                    },
                    Products = new[]
                    {
                        new ProductConfig { Id = "coins_pack_small", CoinsReward = 1000 },
                        new ProductConfig { Id = "boost_pack", CoinsReward = 0, BoostSeconds = 600 }
                    }
                };
                Model = new FactoryModel(Config);
                Economy = new EconomyService(Config);
                Game = new GameController(Config, Model, Economy, Save, Analytics, Clock, Purchases);
            }
        }

        [Test]
        public void FirstMachine_AvailableImmediately_NoSave()
        {
            var h = new Harness();

            Assert.IsTrue(Model_GetMachine(h, "m1").IsUnlocked);
            Assert.AreEqual(1, Model_GetMachine(h, "m1").Level);
            Assert.IsFalse(Model_GetMachine(h, "m2").IsUnlocked);
        }

        [Test]
        public void Unlock_EnoughBalance_SpendsAndLogs()
        {
            var h = new Harness();
            h.Model.AddBalance(100);

            var ok = h.Game.UnlockMachine("m2");

            Assert.IsTrue(ok);
            Assert.IsTrue(Model_GetMachine(h, "m2").IsUnlocked);
            Assert.AreEqual(50, h.Model.Balance, 0.0001);
            Assert.AreEqual(1, h.Analytics.Count(AnalyticsEvents.MachineUnlocked));
            Assert.IsTrue(h.Save.HasProgress());
        }

        [Test]
        public void Unlock_NotEnoughBalance_FailsAndKeepsState()
        {
            var h = new Harness();
            h.Model.AddBalance(10);

            var ok = h.Game.UnlockMachine("m2");

            Assert.IsFalse(ok);
            Assert.IsFalse(Model_GetMachine(h, "m2").IsUnlocked);
            Assert.AreEqual(10, h.Model.Balance, 0.0001);
            Assert.AreEqual(0, h.Analytics.Count(AnalyticsEvents.MachineUnlocked));
        }

        [Test]
        public void Upgrade_EnoughBalance_IncreasesLevelAndCost()
        {
            var h = new Harness();
            h.Model.AddBalance(1000);

            var ok = h.Game.UpgradeMachine("m1");

            Assert.IsTrue(ok);
            Assert.AreEqual(2, Model_GetMachine(h, "m1").Level);
            // cost = 100 * 1.15^1 = 115 (машина уже на 1 уровне → степень 1)
            Assert.AreEqual(1000 - 115, h.Model.Balance, 0.0001);
            Assert.AreEqual(1, h.Analytics.Count(AnalyticsEvents.MachineUpgraded));
        }

        [Test]
        public void Initialize_WithOldSave_AppliesOfflineIncome()
        {
            var h = new Harness(nowUnix: 1_000_600);
            // сохранение сделано 600 сек назад, m1 открыта (10/сек) → +6000
            h.Save.Progress = new PlayerProgress
            {
                balance = 0,
                lastActiveUnix = 1_000_000,
                boostEndUnix = 0,
                machines = new[]
                {
                    new MachineSave { id = "m1", level = 1, unlocked = true },
                    new MachineSave { id = "m2", level = 0, unlocked = false },
                    new MachineSave { id = "m3", level = 0, unlocked = false }
                }
            };

            h.Game.Initialize();

            Assert.AreEqual(6000, h.Model.Balance, 0.0001);
            Assert.AreEqual(1, h.Analytics.Count(AnalyticsEvents.OfflineIncomeApplied));
            Assert.AreEqual(1, h.Analytics.Count(AnalyticsEvents.GameStarted));
        }

        [Test]
        public void Initialize_NoSave_LogsGameStartedWithFreshState()
        {
            var h = new Harness();

            h.Game.Initialize();

            Assert.AreEqual(1, h.Analytics.Count(AnalyticsEvents.GameStarted));
            Assert.AreEqual(0, h.Model.Balance, 0.0001);
            Assert.IsTrue(h.Save.HasProgress());
        }

        [Test]
        public void StartBoost_UsesConfigDurationAndLogs()
        {
            var h = new Harness();
            h.Game.StartBoost();

            Assert.AreEqual(h.Config.BoostDurationSeconds, h.Model.BoostEndUnix - h.Clock.NowUnix);
            Assert.AreEqual(1, h.Analytics.Count(AnalyticsEvents.BoostStarted));
        }

        [Test]
        public void Tick_WithActiveBoost_MultipliesProduction()
        {
            var h = new Harness();
            h.Game.StartBoost();

            h.Game.Tick(10); // 10 сек × 10/сек × 3 = 300

            Assert.AreEqual(300, h.Model.Balance, 0.0001);
        }

        [Test]
        public void Tick_BoostExpires_LogsBoostFinishedAndClearsBoost()
        {
            var h = new Harness();
            h.Game.StartBoost();

            h.Clock.NowUnix += h.Config.BoostDurationSeconds + 1; // буст истёк
            h.Game.Tick(1);

            Assert.AreEqual(1, h.Analytics.Count(AnalyticsEvents.BoostFinished));
            Assert.IsFalse(h.Model.IsBoostActive(h.Clock.NowUnix));
            Assert.AreEqual(0, h.Model.BoostEndUnix); // окно сброшено, не залипло
        }

        [Test]
        public void StartBoost_WhenBoostDisabledInConfig_FailsWithoutEvent()
        {
            var h = new Harness();
            h.Config.BoostEnabled = false;

            var ok = h.Game.StartBoost();

            Assert.IsFalse(ok);
            Assert.AreEqual(0, h.Analytics.Count(AnalyticsEvents.BoostStarted));
            Assert.IsFalse(h.Model.IsBoostActive(h.Clock.NowUnix));
        }

        [Test]
        public void StartBoost_WhileActive_ResetsWindowFromNow()
        {
            var h = new Harness();
            h.Game.StartBoost();

            h.Clock.NowUnix += 100; // прошло 100 из 300 сек
            h.Game.StartBoost();    // повторный старт — окно заново от текущего момента

            Assert.AreEqual(h.Config.BoostDurationSeconds, h.Model.BoostEndUnix - h.Clock.NowUnix);
        }

        [Test]
        public void Purchase_CoinsPack_Success_AddsRewardAndLogs()
        {
            var h = new Harness();

            h.Game.BuyProduct("coins_pack_small");
            h.Purchases.Succeed("coins_pack_small");

            Assert.AreEqual(1000, h.Model.Balance, 0.0001);
            Assert.AreEqual(1, h.Analytics.Count(AnalyticsEvents.PurchaseSucceeded));
            Assert.IsTrue(h.Save.HasProgress()); // награда сразу в сохранении
        }

        [Test]
        public void Purchase_BoostPack_Success_ActivatesBoostForProductDuration()
        {
            var h = new Harness();

            h.Game.BuyProduct("boost_pack");
            h.Purchases.Succeed("boost_pack");

            Assert.IsTrue(h.Model.IsBoostActive(h.Clock.NowUnix));
            Assert.AreEqual(600, h.Model.BoostEndUnix - h.Clock.NowUnix);
            Assert.AreEqual(1, h.Analytics.Count(AnalyticsEvents.BoostStarted));
            Assert.AreEqual(1, h.Analytics.Count(AnalyticsEvents.PurchaseSucceeded));
            Assert.AreEqual(0, h.Model.Balance, 0.0001); // буст-пак не даёт монет
        }

        [Test]
        public void Purchase_Failed_LogsFailedEvent_NoReward()
        {
            var h = new Harness();

            h.Game.BuyProduct("coins_pack_small");
            h.Purchases.Fail("coins_pack_small", "user_cancelled");

            Assert.AreEqual(0, h.Model.Balance, 0.0001);
            Assert.AreEqual(0, h.Analytics.Count(AnalyticsEvents.PurchaseSucceeded));
            Assert.AreEqual(1, h.Analytics.Count(AnalyticsEvents.PurchaseFailed));
        }

        [Test]
        public void BuyProduct_WhenIapUnavailable_LogsFailedAndDoesNotCrash()
        {
            var h = new Harness();
            h.Purchases.IsAvailable = false;

            h.Game.BuyProduct("coins_pack_small");

            Assert.IsEmpty(h.Purchases.Bought); // в магазин не пошли
            Assert.AreEqual(1, h.Analytics.Count(AnalyticsEvents.PurchaseFailed));
            var (_, pars) = h.Analytics.First(AnalyticsEvents.PurchaseFailed);
            Assert.AreEqual("iap_unavailable", pars["error"]);
        }

        private static MachineState Model_GetMachine(Harness h, string id) => h.Model.GetMachine(id);
    }
}
