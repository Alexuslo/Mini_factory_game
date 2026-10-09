using MiniFactory.Core.Game;
using MiniFactory.Core.Model;
using UnityEngine;
using UnityEngine.UI;

namespace MiniFactory.App.View
{
    /// <summary>
    /// Карточка машины: состояние, уровень, цена и действие (открыть/улучшить).
    /// Вся логика — в контроллере; карточка только рисует и шлёт команды (MVC).
    /// </summary>
    public sealed class MachineCardView : MonoBehaviour
    {
        [SerializeField] private Text nameText;
        [SerializeField] private Text infoText;
        [SerializeField] private Text costText;
        [SerializeField] private Button actionButton;
        [SerializeField] private Text buttonLabel;
        [SerializeField] private Image background;
        [SerializeField] private Color lockedColor = new Color(0.35f, 0.35f, 0.35f);
        [SerializeField] private Color unlockedColor = new Color(0.20f, 0.45f, 0.30f);

        private GameController _game;
        private string _machineId;

        public void Initialize(GameController game, string machineId)
        {
            _game = game;
            _machineId = machineId;

            if (actionButton != null)
            {
                actionButton.onClick.RemoveListener(OnClick);
                actionButton.onClick.AddListener(OnClick);
            }

            _game.Model.OnMachineChanged += OnMachineChanged;
            _game.Model.OnBalanceChanged += OnBalanceChanged;
            Refresh();
        }

        private void OnMachineChanged(MachineState state)
        {
            if (state.Id == _machineId) Refresh();
        }

        private void OnBalanceChanged(double _) => Refresh();

        private void OnClick()
        {
            var state = _game.Model.GetMachine(_machineId);
            if (state == null) return;

            if (state.IsUnlocked) _game.UpgradeMachine(_machineId);
            else _game.UnlockMachine(_machineId);
        }

        private void Refresh()
        {
            var config = _game.Config.GetMachine(_machineId);
            var state = _game.Model.GetMachine(_machineId);
            if (config == null || state == null) return;

            if (nameText != null) nameText.text = config.DisplayName;

            double cost;
            if (state.IsUnlocked)
            {
                if (infoText != null) infoText.text = $"Уровень {state.Level}";
                cost = _game.Economy.GetUpgradeCost(config, state.Level);
                if (costText != null) costText.text = $"Улучшить: {BalanceView.FormatMoney(cost)}";
                if (buttonLabel != null) buttonLabel.text = "Улучшить";
                if (background != null) background.color = unlockedColor;
            }
            else
            {
                if (infoText != null) infoText.text = "Закрыта";
                cost = config.UnlockCost;
                if (costText != null) costText.text = $"Открыть: {BalanceView.FormatMoney(cost)}";
                if (buttonLabel != null) buttonLabel.text = "Открыть";
                if (background != null) background.color = lockedColor;
            }

            if (actionButton != null)
                actionButton.interactable = _game.Model.Balance >= cost;
        }

        private void OnDestroy()
        {
            if (_game == null) return;
            _game.Model.OnMachineChanged -= OnMachineChanged;
            _game.Model.OnBalanceChanged -= OnBalanceChanged;
            if (actionButton != null) actionButton.onClick.RemoveListener(OnClick);
        }
    }
}
