using MiniFactory.Core.Game;
using UnityEngine;
using UnityEngine.UI;

namespace MiniFactory.App.View
{
    /// <summary>
    /// Кнопка покупки в магазине. Если IAP недоступен — кнопка
    /// неактивна, а попытка покупки превращается в событие purchase_failed.
    /// </summary>
    public sealed class PurchaseView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Text label;

        private GameController _game;
        private string _productId;

        public void Initialize(GameController game, string productId)
        {
            _game = game;
            _productId = productId;

            if (button != null)
            {
                button.onClick.RemoveListener(OnClick);
                button.onClick.AddListener(OnClick);
            }

            Refresh();
        }

        private void OnClick() => _game?.BuyProduct(_productId);

        private void Refresh()
        {
            if (button != null) button.interactable = _game != null && _game.PurchasesAvailable;

            if (label == null || _game == null) return;
            var product = _game.Config.GetProduct(_productId);
            if (product == null)
            {
                label.text = "Магазин";
            }
            else if (product.BoostSeconds > 0)
            {
                label.text = $"{product.DisplayName}\n+{product.BoostSeconds} сек буста";
            }
            else
            {
                label.text = $"{product.DisplayName}\n+{BalanceView.FormatMoney(product.CoinsReward)}";
            }
        }

        private void Update()
        {
            // IAP может инициализироваться асинхронно — следим за доступностью
            if (button != null && _game != null)
                button.interactable = _game.PurchasesAvailable;
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(OnClick);
        }
    }
}
