using System;

namespace MiniFactory.Core.Save
{
    /// <summary>Сохранённое состояние одной машины (ключ — стабильный Id).</summary>
    [Serializable]
    public class MachineSave
    {
        public string id;
        public int level;
        public bool unlocked;
    }

    /// <summary>Корень сохранения игрока.</summary>
    [Serializable]
    public class PlayerProgress
    {
        public double balance;
        public MachineSave[] machines;

        /// <summary>Последняя активность — точка отсчёта offline-прогресса.</summary>
        public long lastActiveUnix;

        public long boostEndUnix;
        public long boostStartedUnix;
        public int tutorialStep;

        /// <summary>A/B вариант: "A" (контроль) или "B" (эксперимент). Пусто — A.</summary>
        public string experimentVariant;
    }
}
