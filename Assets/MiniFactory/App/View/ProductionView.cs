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
            Refresh();
        }

        private void OnMachineChanged(MachineState _) => Refresh();

        private void Refresh()
        {
            if (label == null || _game == null) return;
            var boost = _game.IsBoostActive ? $" ×{_game.Config.BoostMultiplier:0.#}" : string.Empty;
            label.text = $"+{BalanceView.FormatMoney(_game.TotalProduction)}/сек{boost}";
        }

        private void OnDestroy()
        {
            if (_game != null) _game.Model.OnMachineChanged -= OnMachineChanged;
        }
    }
}
