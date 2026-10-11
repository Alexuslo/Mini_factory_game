using System;

namespace MiniFactory.Core.Config
{
    /// <summary>
    /// Оверрайды эксперимента (A/B): применяются поверх базового GameConfig
    /// в ExperimentConfigSource, не трогают сам базовый конфиг.
    /// </summary>
    [Serializable]
    public sealed class ExperimentConfig
    {
        /// <summary>Множитель базового дохода всех машин; null — без изменений.</summary>
        public float? ProductionMultiplier;

        /// <summary>Множитель стоимости апгрейда всех машин; null — без изменений.</summary>
        public float? UpgradeCostMultiplier;

        /// <summary>Множитель стоимости открытия всех машин; null — без изменений.</summary>
        public float? UnlockCostMultiplier;
    }
}
