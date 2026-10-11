using System;
using System.Collections.Generic;
using System.Linq;
using MiniFactory.Core.Config;
using MiniFactory.Core.Contracts;
using MiniFactory.Core.Game;
using MiniFactory.Core.Model;
using MiniFactory.Core.Offline;
using MiniFactory.Core.Save;
using MiniFactory.Core.Services;
using NUnit.Framework;

namespace MiniFactory.Tests.EditMode
{
    /// <summary>
    /// Дополнительные edge-case тесты: защита от эксплойтов, границы денег,
    /// интеграция контроллера с offline-калькулятором и восстановление буста.
    /// </summary>
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
            public int Count(string name) => Events.Count(e => e.name == name);
        }

        private sealed class FakePurchases : IGamePurchaseService
        {
            public bool IsAvailable { get; set; } = true;
            private event Action<PurchaseOutcome> _onPurchaseCompleted;
            public event Action<PurchaseOutcome> OnPurchaseCompleted
            {
                add => _onPurchaseCompleted += value;
                remove => _onPurchaseCompleted -= value;
            }
            public void Initialize(IEnumerable<string> ids) { }
            public void Buy(string id) { }
            public void Succeed(string id)
                => _onPurchaseCompleted?.Invoke(new PurchaseOutcome(true, id, null));
            public void Fail(string id, string error)
                => _onPurchaseCompleted?.Invoke(new PurchaseOutcome(false, id, error));
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

        // --- Защита от эксплойтов ---

        [Test]
        public void Unlock_AlreadyUnlocked_Fails_NoSideEffects()
        {
            var h = new Harness();
            h.Model.AddBalance(100);

            var ok = h.Game.UnlockMachine("m1"); // m1 уже открыта (unlockCost=0)

            Assert.IsFalse(ok);
            Assert.AreEqual(100, h.Model.Balance, 0.0001);
            Assert.AreEqual(0, h.Analytics.Count(AnalyticsEvents.MachineUnlocked));
        }

        [Test]
        public void Upgrade_LockedMachine_FailsCleanly()
        {
            var h = new Harness();
            h.Model.AddBalance(1000);

            var ok = h.Game.UpgradeMachine("m2"); // m2 закрыта

            Assert.IsFalse(ok);
            Assert.IsFalse(Model_GetMachine(h, "m2").IsUnlocked);
            Assert.AreEqual(1000, h.Model.Balance, 0.0001);
            Assert.AreEqual(0, h.Analytics.Count(AnalyticsEvents.MachineUpgraded));
        }

        [Test]
        public void Unlock_UnknownId_ReturnsFalse_NoCrash()
        {
            var h = new Harness();
            h.Model.AddBalance(100);

            Assert.IsFalse(h.Game.UnlockMachine("ghost"));
            Assert.IsFalse(h.Game.UpgradeMachine("ghost"));
        }

        // --- Границы денег ---

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
        public void Upgrade_OneCoinShort_FailsAtomically()
        {
            var h = new Harness();
            var cost = h.Economy.GetUpgradeCost(h.Config.GetMachine("m1"), 1); // 100 * 1.15 = 115
            h.Model.AddBalance(cost - 1); // на 1 монету меньше

            var ok = h.Game.UpgradeMachine("m1");

            Assert.IsFalse(ok);
            Assert.AreEqual(1, Model_GetMachine(h, "m1").Level);
            Assert.AreEqual(cost - 1, h.Model.Balance, 0.0001); // ничего не списано
        }

        // --- Tick с нулевым/отрицательным дельта ---

        [Test]
        public void Tick_ZeroOrNegativeDelta_DoesNothing()
        {
            var h = new Harness();
            h.Game.StartBoost();

            h.Game.Tick(0);
            h.Game.Tick(-5);

            Assert.AreEqual(0, h.Model.Balance, 0.0001);
            Assert.AreEqual(0, h.Analytics.Count(AnalyticsEvents.BoostFinished));
        }

        // --- Offline: интеграция контроллера ---

        [Test]
        public void Initialize_ZeroElapsed_NoOfflineIncome()
        {
            var h = new Harness(nowUnix: 1_000_000);
            h.Save.Progress = new PlayerProgress
            {
                balance = 100,
                lastActiveUnix = 1_000_000, // ровно сейчас
                boostEndUnix = 0,
                machines = new[]
                {
                    new MachineSave { id = "m1", level = 1, unlocked = true },
                    new MachineSave { id = "m2", level = 0, unlocked = false }
                }
            };

            var report = h.Game.Initialize();

            Assert.AreEqual(100, h.Model.Balance, 0.0001); // без начислений
            Assert.AreEqual(0, h.Analytics.Count(AnalyticsEvents.OfflineIncomeApplied));
            Assert.IsFalse(report.ShowOfflineDialog);
        }

        [Test]
        public void Initialize_FutureLastActive_NoIncome_NoCrash()
        {
            var h = new Harness(nowUnix: 1_000_000);
            h.Save.Progress = new PlayerProgress
            {
                balance = 50,
                lastActiveUnix = 1_000_600, // в будущем (часы сдвинулись)
                boostEndUnix = 0,
                machines = new[]
                {
                    new MachineSave { id = "m1", level = 1, unlocked = true },
                    new MachineSave { id = "m2", level = 0, unlocked = false }
                }
            };

            h.Game.Initialize();

            Assert.AreEqual(50, h.Model.Balance, 0.0001);
            Assert.AreEqual(0, h.Analytics.Count(AnalyticsEvents.OfflineIncomeApplied));
        }

        [Test]
        public void Initialize_OfflineLongerThanCap_CappedEndToEnd()
        {
            var h = new Harness(nowUnix: 1_000_000 + 86_400); // 1 день
            h.Save.Progress = new PlayerProgress
            {
                balance = 0,
                lastActiveUnix = 1_000_000,
                boostEndUnix = 0,
                machines = new[]
                {
                    new MachineSave { id = "m1", level = 1, unlocked = true },
                    new MachineSave { id = "m2", level = 0, unlocked = false }
                }
            };

            var report = h.Game.Initialize();

            Assert.AreEqual(7200, report.OfflineElapsedSeconds); // cap = MaxOfflineSeconds
            Assert.AreEqual(72_000, report.OfflineIncome, 0.0001); // 7200 × 10
        }

        [Test]
        public void Initialize_OfflineWithBoostAcrossWindow_AppliesMultiplier()
        {
            var h = new Harness(nowUnix: 1_000_600); // elapsed 600
            h.Save.Progress = new PlayerProgress
            {
                balance = 0,
                lastActiveUnix = 1_000_000,
                boostEndUnix = 1_000_300, // буст истёк 300 сек назад, но был активен первые 300 сек
                machines = new[]
                {
                    new MachineSave { id = "m1", level = 1, unlocked = true },
                    new MachineSave { id = "m2", level = 0, unlocked = false }
                }
            };

            var report = h.Game.Initialize();

            // 300 boosted × 10 × 3 + 300 normal × 10 = 9000 + 3000 = 12000
            Assert.AreEqual(12_000, report.OfflineIncome, 0.0001);
        }

        [Test]
        public void Initialize_CappedWindowWithBoost_BoostIntersectsCappedNotRaw()
        {
            // elapsed 86400, cap 7200 → window = [now-7200, now]
            // boostEnd = now - 3600 (внутри capped-окна, но не внутри raw-окна)
            var h = new Harness(nowUnix: 1_000_000 + 86_400);
            h.Save.Progress = new PlayerProgress
            {
                balance = 0,
                lastActiveUnix = 1_000_000,
                boostEndUnix = 1_000_000 + 86_400 - 3600, // 3600 сек назад от now
                machines = new[]
                {
                    new MachineSave { id = "m1", level = 1, unlocked = true },
                    new MachineSave { id = "m2", level = 0, unlocked = false }
                }
            };

            var report = h.Game.Initialize();

            // capped window = [now-7200, now], boostEnd = now-3600
            // boosted = min((now-3600) - (now-7200), 7200) = 3600
            // income = 3600 × 10 × 3 + 3600 × 10 = 108000 + 36000 = 144000
            Assert.AreEqual(7200, report.OfflineElapsedSeconds);
            Assert.AreEqual(144_000, report.OfflineIncome, 0.0001);
        }

        // --- Восстановление буста из сейва ---

        [Test]
        public void Initialize_RestoresUnexpiredBoost_NextTickMultiplies()
        {
            var h = new Harness(nowUnix: 1_000_100);
            h.Save.Progress = new PlayerProgress
            {
                balance = 0,
                lastActiveUnix = 1_000_000,
                boostEndUnix = 1_000_300, // ещё активен (осталось 200 сек)
                machines = new[]
                {
                    new MachineSave { id = "m1", level = 1, unlocked = true },
                    new MachineSave { id = "m2", level = 0, unlocked = false }
                }
            };

            h.Game.Initialize(); // offline income = 100 × 10 × 3 = 3000 (буст активен весь elapsed)

            Assert.IsTrue(h.Model.IsBoostActive(h.Clock.NowUnix));
            Assert.AreEqual(200, h.Game.BoostRemainingSeconds);

            var balanceBefore = h.Model.Balance;
            h.Game.Tick(1); // 1 × 10 × 3 = 30 (буст активен)
            Assert.AreEqual(balanceBefore + 30, h.Model.Balance, 0.0001);
        }

        [Test]
        public void Initialize_ExpiredBoostInSave_FirstTickLogsBoostFinishedOnce()
        {
            var h = new Harness(nowUnix: 1_000_600);
            h.Save.Progress = new PlayerProgress
            {
                balance = 0,
                lastActiveUnix = 1_000_000,
                boostStartedUnix = 1_000_000,
                boostEndUnix = 1_000_300, // истёк 300 сек назад
                machines = new[]
                {
                    new MachineSave { id = "m1", level = 1, unlocked = true },
                    new MachineSave { id = "m2", level = 0, unlocked = false }
                }
            };

            h.Game.Initialize();
            h.Game.Tick(1);

            Assert.AreEqual(1, h.Analytics.Count(AnalyticsEvents.BoostFinished));

            h.Game.Tick(1); // второй тик — не должен логировать повторно
            Assert.AreEqual(1, h.Analytics.Count(AnalyticsEvents.BoostFinished));
        }

        // --- Economy edge cases ---

        [Test]
        public void Economy_ProductionAtLevelZero_ReturnsZero()
        {
            var h = new Harness();
            var config = h.Config.GetMachine("m1");
            Assert.AreEqual(0, h.Economy.GetProduction(config, 0), 0.0001);
            Assert.AreEqual(0, h.Economy.GetProduction(config, -1), 0.0001);
        }

        [Test]
        public void Economy_TotalProduction_SumsUnlockedMachines()
        {
            var h = new Harness();
            h.Model.UnlockMachine("m1");
            h.Model.SetLevel("m1", 2);
            h.Model.UnlockMachine("m2");
            h.Model.SetLevel("m2", 1);

            var total = h.Economy.GetTotalProduction(h.Model.Machines);

            // m1: 10 × 2 × 1.1^1 = 22; m2: 50 × 1 × 1.1^0 = 50; m3 locked → 0
            Assert.AreEqual(72, total, 0.001);
        }

        // --- OfflineCalculator edges ---

        [Test]
        public void Offline_NegativeElapsed_ReturnsZero()
        {
            var config = new GameConfig { MaxOfflineSeconds = 7200, BoostMultiplier = 3f, BoostEnabled = true };
            var result = OfflineCalculator.Calculate(-10, 10, 1000, 0, config);
            Assert.AreEqual(0, result.Income);
            Assert.AreEqual(0, result.AppliedSeconds);
        }

        [Test]
        public void Offline_ZeroCps_ReturnsZero()
        {
            var config = new GameConfig { MaxOfflineSeconds = 7200 };
            var result = OfflineCalculator.Calculate(100, 0, 1000, 0, config);
            Assert.AreEqual(0, result.Income);
        }

        [Test]
        public void Offline_MaxOfflineZero_NoIncome()
        {
            var config = new GameConfig { MaxOfflineSeconds = 0, BoostMultiplier = 3f, BoostEnabled = true };
            var result = OfflineCalculator.Calculate(100, 10, 1000, 0, config);
            Assert.AreEqual(0, result.Income);
            Assert.AreEqual(0, result.AppliedSeconds);
        }

        // --- Save edge cases ---

        [Test]
        public void Save_Null_NoFileCreated()
        {
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "test_save_null.json");
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);

            var save = new MiniFactory.App.Save.JsonSaveService(path);
            save.Save(null);

            Assert.IsFalse(save.HasProgress());
        }

        [Test]
        public void Save_OverwriteTwice_LoadsSecondState()
        {
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "test_save_overwrite.json");
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);

            var save = new MiniFactory.App.Save.JsonSaveService(path);
            save.Save(new PlayerProgress { balance = 100, lastActiveUnix = 1, machines = new MachineSave[0] });
            save.Save(new PlayerProgress { balance = 999, lastActiveUnix = 2, machines = new MachineSave[0] });

            var loaded = save.Load();
            Assert.AreEqual(999, loaded.balance);
            Assert.IsFalse(System.IO.File.Exists(path + ".tmp")); // атомарная запись без мусора

            save.Delete();
        }

        private static MachineState Model_GetMachine(Harness h, string id) => h.Model.GetMachine(id);
    }
}
