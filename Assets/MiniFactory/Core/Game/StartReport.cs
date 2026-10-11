namespace MiniFactory.Core.Game
{
    /// <summary>
    /// Итог стартовой инициализации — то, что показывает титульный экран
    /// и offline-диалог после «Продолжить».
    /// </summary>
    public readonly struct StartReport
    {
        /// <summary>true — была загружена существующая сохранённая игра.</summary>
        public readonly bool LoadedExistingSave;

        /// <summary>Доход, начисленный за время отсутствия.</summary>
        public readonly double OfflineIncome;

        /// <summary>Сколько секунд оффлайна применено (после ограничителя).</summary>
        public readonly long OfflineElapsedSeconds;

        public StartReport(bool loadedExistingSave, double offlineIncome, long offlineElapsedSeconds)
        {
            LoadedExistingSave = loadedExistingSave;
            OfflineIncome = offlineIncome;
            OfflineElapsedSeconds = offlineElapsedSeconds;
        }

        /// <summary>Нужно ли показывать offline-диалог после «Продолжить».</summary>
        public bool ShowOfflineDialog => OfflineIncome > 0;
    }
}
