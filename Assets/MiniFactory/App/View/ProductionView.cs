using MiniFactory.Core.Game;
using MiniFactory.Core.Model;
using UnityEngine;
using UnityEngine.UI;

namespace MiniFactory.App.View
{
    /// <summary>Суммарная производительность фабрики.</summary>
    public sealed class ProductionView : MonoBehaviour
    {
        [SerializeField] private Text label;

        private GameController _game;

        public void Initialize(GameController game)
        {
            _game = game;
            _game.Model.OnMachineChanged += OnMachineChanged;
            _game.Model.OnBoostChanged += OnBoostChanged;
            Refresh();
        }

        private void OnMachineChanged(MachineState _) => Refresh();

        // буст стартовал/истёк — «×N» должно обновиться сразу, а не после следующей покупки
        private void OnBoostChanged(long _) => Refresh();

        private void Refresh()
        {
            if (label == null || _game == null) return;
            var active = _game.IsBoostActive;
            var boost = active ? $" ×{_game.Config.BoostMultiplier:0.#}" : string.Empty;
            label.text = $"+{BalanceView.FormatMoney(_game.TotalProduction)}/сек{boost}";
            label.color = active ? new Color(1f, 0.85f, 0.35f) : new Color(0.85f, 1f, 0.85f);
        }

        private void OnDestroy()
        {
            if (_game == null) return;
            _game.Model.OnMachineChanged -= OnMachineChanged;
            _game.Model.OnBoostChanged -= OnBoostChanged;
        }
    }
}
