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
    /// в наше событие PurchaseOutcome. Fake Store работает и в редакторе,
    /// и на устройстве (ТЗ: «для проверки достаточно Fake Store»).
    /// Если IAP недоступен — mock-режим с toast.
    /// </summary>
    public sealed class UnityIapService : IGamePurchaseService, IDetailedStoreListener
    {
        private IStoreController _controller;
        private bool _mockMode;

        public bool IsAvailable => _mockMode || _controller != null;

        public event Action<PurchaseOutcome> OnPurchaseCompleted;
        /// <summary>Всплывающая надпись UI (mock-режим и т.п.).</summary>
        public event Action<string> OnToast;

        public void Initialize(IEnumerable<string> consumableProductIds)
        {
#if UNITY_EDITOR
            // Fake Store — editor-only (ТЗ: «для проверки достаточно Fake Store»)
            try
            {
                var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
                foreach (var id in consumableProductIds)
                    builder.AddProduct(id, ProductType.Consumable);

                UnityPurchasing.Initialize(this, builder);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[IAP] Initialize exception: {e.Message}");
                EnableMock();
            }
#else
            // На устройстве Fake Store недоступен — mock без Google Play
            EnableMock();
#endif
        }

        public void Buy(string productId)
        {
            if (_mockMode)
            {
                Fire(true, productId, null);
                OnToast?.Invoke("MOCK-покупка (IAP недоступен)");
                return;
            }

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
            _mockMode = false;
            Debug.Log("[IAP] Initialized (Fake Store)");
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            Debug.LogWarning($"[IAP] Init failed: {error}");
            EnableMock();
        }

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            Debug.LogWarning($"[IAP] Init failed: {error} ({message})");
            EnableMock();
        }

        private void EnableMock()
        {
            _mockMode = true;
            OnToast?.Invoke("IAP недоступен — mock-режим");
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
