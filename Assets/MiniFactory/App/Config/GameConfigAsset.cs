using System;
using MiniFactory.Core.Config;
using UnityEngine;

namespace MiniFactory.App.Config
{
    /// <summary>
    /// Авторский ассет конфигурации (ТЗ п.3): правится в инспекторе Unity.
    /// Игра ассет не видит — только данные через IGameConfigSource.
    /// </summary>
    [CreateAssetMenu(menuName = "Mini Factory/Game Config", fileName = "GameConfig")]
    public sealed class GameConfigAsset : ScriptableObject
    {
        [Serializable]
        public sealed class MachineEntry
        {
            public string id;
            public string displayName;

            [Tooltip("0 — машина доступна сразу (первая машина по ТЗ)")]
            public long unlockCost;

            [Tooltip("Базовый доход в секунду на первом уровне")]
            public double baseProduction;

            public double upgradeCostBase = 10;
            public float upgradeCostGrowth = 1.15f;
            public float productionGrowth = 1.1f;
        }

        [Header("Machines")]
        public MachineEntry[] machines;

        [Header("Boost")]
        public bool boostEnabled = true;
        public int boostDurationSeconds = 300;
        public float boostMultiplier = 3f;

        [Header("Offline")]
        [Tooltip("Максимальное время offline production, сек")]
        public int maxOfflineSeconds = 7200;

        public GameConfig ToGameConfig()
        {
            var result = new GameConfig
            {
                Machines = new MachineConfig[machines?.Length ?? 0],
                BoostEnabled = boostEnabled,
                BoostDurationSeconds = boostDurationSeconds,
                BoostMultiplier = boostMultiplier,
                MaxOfflineSeconds = maxOfflineSeconds
            };

            for (var i = 0; i < result.Machines.Length; i++)
            {
                var m = machines[i];
                result.Machines[i] = new MachineConfig
                {
                    Id = m.id,
                    DisplayName = m.displayName,
                    UnlockCost = m.unlockCost,
                    BaseProduction = m.baseProduction,
                    UpgradeCostBase = m.upgradeCostBase,
                    UpgradeCostGrowth = m.upgradeCostGrowth,
                    ProductionGrowth = m.productionGrowth
                };
            }

            return result;
        }
    }
}
