using System.Collections.Generic;

namespace MiniFactory.Core.Contracts
{
    /// <summary>Один destination для аналитических событий (ТЗ п.5).</summary>
    public interface IAnalyticsProvider
    {
        void Log(string eventName, IReadOnlyDictionary<string, object> parameters);
    }

    /// <summary>
    /// Игровой код шлёт события только через этот интерфейс —
    /// конкретный SDK (или консоль) подключён не здесь.
    /// </summary>
    public interface IAnalytics
    {
        void Log(string eventName, IReadOnlyDictionary<string, object> parameters = null);
    }

    /// <summary>Имена событий (ТЗ п.5) — константы вместо строковых литералов в местах вызова.</summary>
    public static class AnalyticsEvents
    {
        public const string GameStarted = "game_started";
        public const string MachineUnlocked = "machine_unlocked";
        public const string MachineUpgraded = "machine_upgraded";
        public const string BoostStarted = "boost_started";
        public const string BoostFinished = "boost_finished";
        public const string OfflineIncomeApplied = "offline_income_applied";
        public const string PurchaseSucceeded = "purchase_succeeded";
        public const string PurchaseFailed = "purchase_failed";
        public const string ProgressRestarted = "progress_restarted";
    }
}
