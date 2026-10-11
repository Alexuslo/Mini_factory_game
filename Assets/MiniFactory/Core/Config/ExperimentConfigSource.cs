using System;
using MiniFactory.Core.Contracts;

namespace MiniFactory.Core.Config
{
    /// <summary>
    /// Декоратор над IGameConfigSource: применяет оверрайды эксперимента (A/B)
    /// к базовому конфигу. Базовый источник не изменяется — возврат к контролю A
    /// = убрать оверрайды. Композер решает, оборачивать ли базовый источник.
    /// </summary>
    public sealed class ExperimentConfigSource : IGameConfigSource
    {
        private readonly IGameConfigSource _base;
        private readonly ExperimentConfig _experiment;

        public ExperimentConfigSource(IGameConfigSource baseSource, ExperimentConfig experiment)
        {
            _base = baseSource ?? throw new ArgumentNullException(nameof(baseSource));
            _experiment = experiment; // null — прозрачный прокси
        }

        public GameConfig GetConfig()
        {
            var config = _base.GetConfig();
            if (_experiment == null) return config;
            return ApplyOverrides(config, _experiment);
        }

        private static GameConfig ApplyOverrides(GameConfig config, ExperimentConfig exp)
        {
            var needsMachines = exp.ProductionMultiplier.HasValue
                || exp.UpgradeCostMultiplier.HasValue
                || exp.UnlockCostMultiplier.HasValue;
            if (!needsMachines) return config;

            var machines = config.Machines ?? Array.Empty<MachineConfig>();
            var result = new MachineConfig[machines.Length];
            for (var i = 0; i < machines.Length; i++)
            {
                var m = machines[i];
                result[i] = new MachineConfig
                {
                    Id = m.Id,
                    DisplayName = m.DisplayName,
                    UnlockCost = exp.UnlockCostMultiplier.HasValue
                        ? (long)(m.UnlockCost * exp.UnlockCostMultiplier.Value)
                        : m.UnlockCost,
                    BaseProduction = exp.ProductionMultiplier.HasValue
                        ? m.BaseProduction * exp.ProductionMultiplier.Value
                        : m.BaseProduction,
                    UpgradeCostBase = exp.UpgradeCostMultiplier.HasValue
                        ? m.UpgradeCostBase * exp.UpgradeCostMultiplier.Value
                        : m.UpgradeCostBase,
                    UpgradeCostGrowth = m.UpgradeCostGrowth,
                    ProductionGrowth = m.ProductionGrowth
                };
            }

            return new GameConfig
            {
                Machines = result,
                Products = config.Products,
                BoostEnabled = config.BoostEnabled,
                BoostDurationSeconds = config.BoostDurationSeconds,
                BoostMultiplier = config.BoostMultiplier,
                MaxOfflineSeconds = config.MaxOfflineSeconds,
                TutorialId = config.TutorialId,
                Tutorial = config.Tutorial
            };
        }
    }
}
