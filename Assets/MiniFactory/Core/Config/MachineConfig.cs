using System;

namespace MiniFactory.Core.Config
{
    /// <summary>
    /// Статические параметры производственной машины (конфиг, не состояние).
    /// Состояние (уровень, открыт/закрыт) живёт в Model.
    /// </summary>
    [Serializable]
    public sealed class MachineConfig
    {
        /// <summary>Стабильный идентификатор — ключ состояния в сохранении.</summary>
        public string Id;

        public string DisplayName;

        /// <summary>Стоимость открытия. 0 — машина доступна сразу (ТЗ п.1).</summary>
        public long UnlockCost;

        /// <summary>Базовый доход в секунду на первом уровне.</summary>
        public double BaseProduction;

        /// <summary>База стоимости улучшения.</summary>
        public double UpgradeCostBase;

        /// <summary>Множитель роста стоимости за уровень, например 1.15.</summary>
        public float UpgradeCostGrowth = 1.15f;

        /// <summary>Множитель роста дохода за уровень, например 1.1.</summary>
        public float ProductionGrowth = 1.1f;
    }
}
