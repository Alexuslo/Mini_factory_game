using System;

namespace MiniFactory.Core.Config
{
    /// <summary>Событие, продвигающее мини-туториал на следующий шаг.</summary>
    public enum TutorialTrigger
    {
        /// <summary>Любой апгрейд любой машины.</summary>
        UpgradeAny,

        /// <summary>Открытие любой машины.</summary>
        UnlockAny,

        /// <summary>Апгрейд конкретной машины (MachineId; пусто — любая).</summary>
        UpgradeMachine,

        /// <summary>Открытие конкретной машины (MachineId; пусто — любая).</summary>
        UnlockMachine
    }

    /// <summary>
    /// Один шаг мини-туториала: чем продвигается и куда указывает стрелка.
    /// Порядок шагов = порядок в GameConfig.Tutorial; завершение — когда шагов больше нет.
    /// </summary>
    [Serializable]
    public sealed class TutorialStep
    {
        public TutorialTrigger Trigger;

        /// <summary>Для UpgradeMachine/UnlockMachine; пусто — любая машина.</summary>
        public string MachineId = "";

        /// <summary>
        /// Куда указывает стрелка, пока шаг активен:
        /// "first" (первая), "firstLocked" (первая закрытая) или id машины.
        /// </summary>
        public string TargetMachine = "firstLocked";
    }
}
