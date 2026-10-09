using System;
using System.Collections.Generic;
using MiniFactory.Core.Contracts;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

namespace MiniFactory.App.Iap
{
    /// <summary>
    /// Адаптер Unity IAP v5 (ТЗ п.4): единственное место проекта,
    /// где используется API Unity Purchasing. Все исходы превращаются
    /// в наше событие PurchaseOutcome; в редакторе работает Fake Store.
    /// </summary>
    public sealed class UnityIapService : IGamePurchaseService, IDetailedStoreListener
    {
        private IStoreController _controller;

        public bool IsAvailable { get; private set; }

        public event Action<PurchaseOutcome> OnPurchaseCompleted;

        public void Initialize(IEnumerable<string> consumableProductIds)
        {
            try
            {
                var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
                foreach (var id in consumableProductIds)
                    builder.AddProduct(id, ProductType.Consumable);

                UnityPurchasing.Initialize(this, builder);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[IAP] Initialize failed: {e.Message}");
                IsAvailable = false;
            }
        }

        public void Buy(string productId)
        {
            if (_controller == null)
            {
                Fire(false, productId, "iap_not_initialized");
                return;
            }

            try
            {
                _controller.InitiatePurchase(productId);
            }
            catch (Exception e)
            {
                Fire(false, productId, e.Message);
            }
        }

        // ---------- IStoreListener ----------

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            _controller = controller;
            IsAvailable = true;
            Debug.Log("[IAP] Initialized");
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            IsAvailable = false;
            Debug.LogWarning($"[IAP] Init failed: {error}");
        }

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            IsAvailable = false;
            Debug.LogWarning($"[IAP] Init failed: {error} ({message})");
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            Fire(true, args.purchasedProduct.definition.id, null);
            return PurchaseProcessingResult.Complete;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
        {
            Fire(false, product.definition.id, reason.ToString());
        }

        // ---------- IDetailedStoreListener ----------

        public void OnPurchaseFailed(Product product, PurchaseFailureDescription description)
        {
            Fire(false, product.definition.id,
                 string.IsNullOrEmpty(description.message) ? description.reason.ToString() : description.message);
        }

        private void Fire(bool success, string productId, string error)
        {
            OnPurchaseCompleted?.Invoke(new PurchaseOutcome(success, productId, error));
        }
    }
}
