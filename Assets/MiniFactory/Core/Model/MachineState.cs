namespace MiniFactory.Core.Model
{
    /// <summary>
    /// Состояние одной машины (статика — в MachineConfig).
    /// Изменяется только через FactoryModel — поля закрыты от прямой записи.
    /// </summary>
    public sealed class MachineState
    {
        public string Id { get; }
        public bool IsUnlocked { get; private set; }

        /// <summary>Уровень; 0 — машина закрыта.</summary>
        public int Level { get; private set; }

        public MachineState(string id, bool unlocked, int level)
        {
            Id = id;
            IsUnlocked = unlocked;
            Level = level;
        }

        internal void SetUnlocked(bool unlocked) => IsUnlocked = unlocked;
        internal void SetLevel(int level) => Level = level;
    }
}
