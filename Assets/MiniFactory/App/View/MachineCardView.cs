using MiniFactory.App.Config;
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

        /// <summary>Rect кнопки действия — на неё указывает стрелка туториала.</summary>
        public RectTransform ActionButtonRect =>
            actionButton != null ? actionButton.GetComponent<RectTransform>() : null;

        /// <summary>Кнопка действия — её interactable определяет, пора ли показывать стрелку.</summary>
        public Button ActionButton => actionButton;

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

        /// <summary>Runtime-применение UI-конфига (без пересборки сцены).</summary>
        public void ApplyUi(GameConfigAsset.UiEntry ui)
        {
            lockedColor = ui.cardLockedColor;
            unlockedColor = ui.cardUnlockedColor;
            if (nameText != null) nameText.fontSize = ui.fontCardName;
            if (infoText != null)
            {
                infoText.fontSize = ui.fontCardInfo;
                infoText.color = ui.cardInfoColor;
            }
            if (costText != null)
            {
                costText.fontSize = ui.fontCardCost;
                costText.color = ui.cardCostColor;
            }
            if (buttonLabel != null) buttonLabel.fontSize = ui.fontCardButton;
            if (actionButton != null)
            {
                var img = actionButton.GetComponent<Image>();
                if (img != null) img.color = ui.cardButtonColor;
                var rt = actionButton.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(-ui.cardButtonMargin, rt.anchoredPosition.y);
            }
            if (_game != null) Refresh(); // цвета locked/unlocked уже новые
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
