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

        public FactoryModel Model => _model;
        public GameConfig Config => _config;
        public Services.EconomyService Economy => _economy;

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
            IClock clock)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _economy = economy ?? throw new ArgumentNullException(nameof(economy));
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <summary>Старт сессии: загрузка прогресса, применение offline-дохода.</summary>
        public void Initialize()
        {
            var hasSave = _save.HasProgress();
            PlayerProgress progress = null;
            if (hasSave)
            {
                progress = _save.Load();
                if (progress != null) _model.Restore(progress);
            }

            if (progress != null)
                ApplyOfflineIncome(progress);
            else
                SaveProgress(); // фиксируем стартовое состояние первой машиной

            _analytics.Log(AnalyticsEvents.GameStarted, new Dictionary<string, object>
            {
                ["has_save"] = hasSave,
                ["balance"] = _model.Balance,
                ["unlocked_machines"] = CountUnlocked()
            });
        }

        private void ApplyOfflineIncome(PlayerProgress progress)
        {
            var now = _clock.NowUnix;
            var elapsed = now - progress.lastActiveUnix;
            if (elapsed <= 0) return;

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
            }
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

            var now = _clock.NowUnix;
            var end = now + _config.BoostDurationSeconds;
            _model.SetBoost(now, end);
            SaveProgress();

            _analytics.Log(AnalyticsEvents.BoostStarted, new Dictionary<string, object>
            {
                ["duration_seconds"] = _config.BoostDurationSeconds,
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
