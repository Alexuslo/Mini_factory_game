using MiniFactory.Core.Model;
using UnityEngine;
using UnityEngine.UI;

namespace MiniFactory.App.View
{
    /// <summary>Баланс игрока: подписка на событие модели (Model → View).</summary>
    public sealed class BalanceView : MonoBehaviour
    {
        [SerializeField] private Text label;

        /// <summary>Rect текста баланса — на него вешается 5-тап для debug-панели.</summary>
        public RectTransform LabelRect => label != null ? label.rectTransform : null;

        private FactoryModel _model;

        public void Initialize(FactoryModel model)
        {
            if (_model != null) _model.OnBalanceChanged -= Refresh;
            _model = model;
            _model.OnBalanceChanged += Refresh;
            Refresh(_model.Balance);
        }

        private void Refresh(double balance)
        {
            if (label != null) label.text = FormatMoney(balance);
        }

        public static string FormatMoney(double value)
        {
            if (value >= 1_000_000) return (value / 1_000_000).ToString("0.##") + "M";
            if (value >= 1_000) return (value / 1_000).ToString("0.##") + "K";
            return value.ToString("0");
        }

        private void OnDestroy()
        {
            if (_model != null) _model.OnBalanceChanged -= Refresh;
        }
    }
}
