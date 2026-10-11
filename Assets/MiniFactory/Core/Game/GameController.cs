using System;
using System.Collections.Generic;
using MiniFactory.Core.Config;
using MiniFactory.Core.Contracts;
using MiniFactory.Core.Model;
using MiniFactory.Core.Offline;
using MiniFactory.Core.Save;

namespace MiniFactory.Core.Game
{
    /// <summary>
    /// Оркестратор (MVC-контроллер): принимает действия игрока,
    /// двигает модель, вызывает сервисы, шлёт аналитику и сохранение.
    /// Зависимости приходят через конструктор — создаёт их composition root.
    /// </summary>
    public sealed class GameController
    {
        private readonly GameConfig _config;
        private readonly FactoryModel _model;
        private readonly Services.EconomyService _economy;
        private readonly ISaveService _save;
        private readonly IAnalytics _analytics;
        private readonly IClock _clock;
        private readonly IGamePurchaseService _purchases;
        private bool _initialized;

        public FactoryModel Model => _model;
        public GameConfig Config => _config;
        public Services.EconomyService Economy => _economy;

        /// <summary>Был ли прогресс на диске — для кнопки «Продолжить».</summary>
        public bool HasSave => _save.HasProgress();

        /// <summary>false — покупки недоступны (IAP не инициализировался).</summary>
        public bool PurchasesAvailable => _purchases != null && _purchases.IsAvailable;

        /// <summary>Суммарная производительность открытых машин (без Boost).</summary>
        public double TotalProduction => _economy.GetTotalProduction(_model.Machines);

        public bool IsBoostActive => _model.IsBoostActive(_clock.NowUnix);

        /// <summary>Остаток активного Boost в секундах; 0 — не активен.</summary>
        public long BoostRemainingSeconds
        {
            get
            {
                var remaining = _model.BoostEndUnix - _clock.NowUnix;
                return remaining > 0 ? remaining : 0;
            }
        }

        public GameController(
            GameConfig config,
            FactoryModel model,
            Services.EconomyService economy,
            ISaveService save,
            IAnalytics analytics,
            IClock clock,
            IGamePurchaseService purchases)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _economy = economy ?? throw new ArgumentNullException(nameof(economy));
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _purchases = purchases;

            if (_purchases != null)
                _purchases.OnPurchaseCompleted += HandlePurchaseCompleted;
        }

        /// <summary>Покупка продукта из магазина. Недоступность IAP — не краш, а событие.</summary>
        public void BuyProduct(string productId)
        {
            if (_purchases == null || !_purchases.IsAvailable)
            {
                _analytics.Log(AnalyticsEvents.PurchaseFailed, new Dictionary<string, object>
                {
                    ["product_id"] = productId,
                    ["error"] = "iap_unavailable"
                });
                return;
            }
            _purchases.Buy(productId);
        }

        private void HandlePurchaseCompleted(PurchaseOutcome outcome)
        {
            if (outcome.Success)
            {
                var product = _config.GetProduct(outcome.ProductId);
                var reward = product?.CoinsReward ?? 0;
                var boostSeconds = product?.BoostSeconds ?? 0;

                if (reward > 0) _model.AddBalance(reward);
                if (boostSeconds > 0) ApplyBoost(boostSeconds);
                SaveProgress();

                _analytics.Log(AnalyticsEvents.PurchaseSucceeded, new Dictionary<string, object>
                {
                    ["product_id"] = outcome.ProductId,
                    ["reward"] = reward,
                    ["boost_seconds"] = boostSeconds
                });
            }
            else
            {
                _analytics.Log(AnalyticsEvents.PurchaseFailed, new Dictionary<string, object>
                {
                    ["product_id"] = outcome.ProductId,
                    ["error"] = outcome.Error ?? "unknown"
                });
            }
        }

        /// <summary>
        /// Старт сессии: загрузка прогресса, применение offline-дохода.
        /// Возвращает отчёт — его показывает offline-диалог после «Продолжить».
        /// </summary>
        public StartReport Initialize()
        {
            _initialized = true; // с этого момента разрешены автосохранения

            var hasSave = _save.HasProgress();
            PlayerProgress progress = null;
            if (hasSave)
            {
                progress = _save.Load();
                if (progress != null) _model.Restore(progress);
            }

            double offlineIncome = 0;
            long offlineElapsed = 0;
            if (progress != null)
            {
                (offlineIncome, offlineElapsed) = ApplyOfflineIncome(progress);
            }
            else
            {
                SaveProgress(); // фиксируем стартовое состояние первой машиной
            }

            _analytics.Log(AnalyticsEvents.GameStarted, new Dictionary<string, object>
            {
                ["has_save"] = hasSave,
                ["balance"] = _model.Balance,
                ["unlocked_machines"] = CountUnlocked()
            });

            return new StartReport(progress != null, offlineIncome, offlineElapsed);
        }

        /// <summary>«Начать сначала» (титульный экран): стирает прогресс (ТЗ).</summary>
        public void RestartProgress()
        {
            _save.Delete();
            _analytics.Log(AnalyticsEvents.ProgressRestarted);
        }

        private (double income, long elapsed) ApplyOfflineIncome(PlayerProgress progress)
        {
            var now = _clock.NowUnix;
            var elapsed = now - progress.lastActiveUnix;
            if (elapsed <= 0) return (0, 0);

            var totalCps = _economy.GetTotalProduction(_model.Machines);
            var result = OfflineCalculator.Calculate(
                elapsed, totalCps, now, progress.boostEndUnix, _config);

            if (result.Income > 0)
            {
                _model.AddBalance(result.Income);
                SaveProgress();

                _analytics.Log(AnalyticsEvents.OfflineIncomeApplied, new Dictionary<string, object>
                {
                    ["elapsed_seconds"] = elapsed,
                    ["applied_seconds"] = result.AppliedSeconds,
                    ["boosted_seconds"] = result.BoostedSeconds,
                    ["income"] = result.Income
                });
                return (result.Income, result.AppliedSeconds);
            }

            return (0, 0);
        }

        /// <summary>Открыть машину за валюту (ТЗ п.1).</summary>
        public bool UnlockMachine(string id)
        {
            var config = _config.GetMachine(id);
            var state = _model.GetMachine(id);
            if (config == null || state == null || state.IsUnlocked) return false;

            if (!_model.TrySpend(config.UnlockCost)) return false;

            _model.UnlockMachine(id);
            SaveProgress();

            _analytics.Log(AnalyticsEvents.MachineUnlocked, new Dictionary<string, object>
            {
                ["machine_id"] = id,
                ["cost"] = config.UnlockCost
            });
            return true;
        }

        /// <summary>Улучшить открытую машину (ТЗ п.1).</summary>
        public bool UpgradeMachine(string id)
        {
            var config = _config.GetMachine(id);
            var state = _model.GetMachine(id);
            if (config == null || state == null || !state.IsUnlocked) return false;

            var cost = _economy.GetUpgradeCost(config, state.Level);
            if (!_model.TrySpend(cost)) return false;

            _model.SetLevel(id, state.Level + 1);
            SaveProgress();

            _analytics.Log(AnalyticsEvents.MachineUpgraded, new Dictionary<string, object>
            {
                ["machine_id"] = id,
                ["level"] = state.Level,
                ["cost"] = cost
            });
            return true;
        }

        /// <summary>Активировать Boost (ТЗ п.1-2), длительность и множитель — из конфига.</summary>
        public bool StartBoost()
        {
            if (!_config.BoostEnabled) return false;
            return ApplyBoost(_config.BoostDurationSeconds);
        }

        private bool ApplyBoost(int durationSeconds)
        {
            if (!_config.BoostEnabled || durationSeconds <= 0) return false;

            var now = _clock.NowUnix;
            var end = now + durationSeconds;
            _model.SetBoost(now, end);
            SaveProgress();

            _analytics.Log(AnalyticsEvents.BoostStarted, new Dictionary<string, object>
            {
                ["duration_seconds"] = durationSeconds,
                ["multiplier"] = _config.BoostMultiplier
            });
            return true;
        }

        /// <summary>Тик производства. Вызывается из App-слоя с реальным deltaTime.</summary>
        public void Tick(double deltaSeconds)
        {
            if (deltaSeconds <= 0) return;

            var now = _clock.NowUnix;
            var boostActive = _model.IsBoostActive(now);

            var cps = _economy.GetTotalProduction(_model.Machines);
            if (cps > 0)
            {
                var factor = boostActive ? _config.BoostMultiplier : 1;
                _model.AddBalance(cps * deltaSeconds * factor);
            }

            // Boost истёк в процессе игры
            if (!boostActive && _model.BoostStartedUnix > 0)
            {
                _analytics.Log(AnalyticsEvents.BoostFinished, new Dictionary<string, object>
                {
                    ["duration_seconds"] = _model.BoostEndUnix - _model.BoostStartedUnix
                });
                _model.SetBoost(0, 0);
                SaveProgress();
            }
        }

        /// <summary>Сохранение текущего состояния (вызывается при сворачивании и по действиям).</summary>
        public void SaveProgress()
        {
            // Пока идёт титульный экран, автосейвы выключены:
            // иначе сворачивание затёрло бы старый сейв свежим пустым состоянием
            if (!_initialized) return;

            var machines = new MachineSave[_model.Machines.Length];
            for (var i = 0; i < _model.Machines.Length; i++)
            {
                var m = _model.Machines[i];
                machines[i] = new MachineSave
                {
                    id = m.Id,
                    level = m.Level,
                    unlocked = m.IsUnlocked
                };
            }

            _save.Save(new PlayerProgress
            {
                balance = _model.Balance,
                machines = machines,
                lastActiveUnix = _clock.NowUnix,
                boostEndUnix = _model.BoostEndUnix,
                boostStartedUnix = _model.BoostStartedUnix,
                tutorialStep = _model.TutorialStep
            });
        }

        private int CountUnlocked()
        {
            var count = 0;
            foreach (var m in _model.Machines)
                if (m.IsUnlocked) count++;
            return count;
        }
    }
}
