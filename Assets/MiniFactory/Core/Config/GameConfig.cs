using System;

namespace MiniFactory.Core.Config
{
    /// <summary>
    /// Неизменяемая (immutable) игровая конфигурация — всё, что вне gameplay-кода.
    /// Игра получает её через IGameConfigSource; сам формат хранения (ScriptableObject,
    /// JSON, Remote Config) ей неизвестен.
    /// </summary>
    [Serializable]
    public sealed class GameConfig
    {
        /// <summary>Машины фабрики. Количество и состав определяются конфигом.</summary>
        public MachineConfig[] Machines;

        public bool BoostEnabled = true;

        /// <summary>Длительность Boost в секундах (ТЗ п.3).</summary>
        public int BoostDurationSeconds = 300;

        /// <summary>Множитель производства во время Boost (ТЗ п.3).</summary>
        public float BoostMultiplier = 3f;

        /// <summary>Максимальная продолжительность offline production в секундах (ТЗ п.3).</summary>
        public int MaxOfflineSeconds = 7200;

        public MachineConfig GetMachine(string id)
        {
            if (Machines == null) return null;
            for (var i = 0; i < Machines.Length; i++)
                if (Machines[i].Id == id) return Machines[i];
            return null;
        }
    }
}
