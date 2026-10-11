using System;

namespace MiniFactory.Core.Contracts
{
    /// <summary>Итог попытки покупки — в терминах игры, а не SDK.</summary>
    public readonly struct PurchaseOutcome
    {
        public readonly bool Success;
        public readonly string ProductId;

        /// <summary>Причина неудачи (для аналитики); null при успехе.</summary>
        public readonly string Error;

        public PurchaseOutcome(bool success, string productId, string error)
        {
            Success = success;
            ProductId = productId;
            Error = error;
        }
    }

    /// <summary>
    /// Магазин в терминах игры (ТЗ п.4): gameplay не знает про Unity IAP,
    /// реализация адаптирует SDK снаружи.
    /// </summary>
    public interface IGamePurchaseService
    {
        /// <summary>false — IAP недоступен или не инициализировался.</summary>
        bool IsAvailable { get; }

        void Initialize(System.Collections.Generic.IEnumerable<string> consumableProductIds);

        void Buy(string productId);

        event Action<PurchaseOutcome> OnPurchaseCompleted;
    }
}
