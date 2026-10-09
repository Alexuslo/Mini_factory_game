namespace MiniFactory.Core.Model
{
    /// <summary>Изменяемое состояние одной машины (статика — в MachineConfig).</summary>
    public sealed class MachineState
    {
        public string Id;
        public bool IsUnlocked;

        /// <summary>Уровень; 0 — машина закрыта.</summary>
        public int Level;
    }
}
