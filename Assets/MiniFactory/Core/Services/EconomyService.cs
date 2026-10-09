using System;
using System.Collections.Generic;
using MiniFactory.Core.Config;
using MiniFactory.Core.Model;

namespace MiniFactory.Core.Services
{
    /// <summary>
    /// Формулы экономики (ТЗ п.1): стоимость улучшения, производительность.
    /// Чистая математика — состояние не хранит, от Unity не зависит.
    /// </summary>
    public sealed class EconomyService
    {
        private readonly GameConfig _config;

        public EconomyService(GameConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>Стоимость перехода с currentLevel на следующий: base * growth^level.</summary>
        public double GetUpgradeCost(MachineConfig machine, int currentLevel)
        {
            if (machine == null) throw new ArgumentNullException(nameof(machine));
            if (currentLevel < 0) currentLevel = 0;
            return machine.UpgradeCostBase * Math.Pow(machine.UpgradeCostGrowth, currentLevel);
        }

        /// <summary>Доход машины в секунду: base * level * growth^(level-1).</summary>
        public double GetProduction(MachineConfig machine, int level)
        {
            if (machine == null || level <= 0) return 0;
            return machine.BaseProduction * level * Math.Pow(machine.ProductionGrowth, level - 1);
        }

        /// <summary>Суммарная производительность открытых машин (без учёта Boost).</summary>
        public double GetTotalProduction(MachineState[] machines)
        {
            if (machines == null) return 0;
            double total = 0;
            for (var i = 0; i < machines.Length; i++)
            {
                var state = machines[i];
                if (state == null || !state.IsUnlocked) continue;
                total += GetProduction(_config.GetMachine(state.Id), state.Level);
            }
            return total;
        }
    }
}
