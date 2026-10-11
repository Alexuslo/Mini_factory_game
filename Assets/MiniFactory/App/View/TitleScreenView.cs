using System;
using MiniFactory.App.Config;
using MiniFactory.Core.Game;
using UnityEngine;
using UnityEngine.UI;

namespace MiniFactory.App.View
{
    /// <summary>
    /// Титульный экран (ТЗ п.10): «Продолжить» / «Начать сначала»,
    /// подтверждение сброса и offline-диалог после входа в игру.
    /// Управляет своим жизненным циклом сам; когда игрок вошёл — зовёт onEntered.
    /// </summary>
    public sealed class TitleScreenView : MonoBehaviour
    {
        [SerializeField] private GameObject titlePanel;
        [SerializeField] private GameObject confirmPanel;
        [SerializeField] private GameObject offlinePanel;
        [SerializeField] private Text titleText;
        [SerializeField] private Text confirmText;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button confirmYesButton;
        [SerializeField] private Button confirmNoButton;
        [SerializeField] private Button offlineOkButton;
        [SerializeField] private Text offlineText;

        private GameController _game;
        private Action _onEntered;
        private bool _startedFresh;

        /// <summary>Runtime-применение UI-конфига (без пересборки сцены).</summary>
        public void ApplyUi(GameConfigAsset.UiEntry ui)
        {
            var bg = GetComponent<Image>();
            if (bg != null)
            {
                var c = bg.color;
                bg.color = new Color(c.r, c.g, c.b, ui.titleBackgroundAlpha);
            }

            var confirmBg = confirmPanel != null ? confirmPanel.GetComponent<Image>() : null;
            if (confirmBg != null) confirmBg.color = ui.titlePanelColor;
            var offlineBg = offlinePanel != null ? offlinePanel.GetComponent<Image>() : null;
            if (offlineBg != null) offlineBg.color = ui.titlePanelColor;

            if (titleText != null)
            {
                titleText.fontSize = ui.fontTitle;
                SetCenter(titleText.rectTransform, ui.titleTextAnchorY, ui.titleTextWidth, ui.titleTextHeight);
            }
            if (confirmText != null)
            {
                confirmText.fontSize = ui.fontTitlePanel;
                SetCenter(confirmText.rectTransform, ui.confirmTextAnchorY, ui.titlePanelTextWidth, ui.confirmTextHeight);
            }
            if (offlineText != null)
            {
                offlineText.fontSize = ui.fontTitlePanel;
                SetCenter(offlineText.rectTransform, ui.offlineTextAnchorY, ui.titlePanelTextWidth, ui.offlineTextHeight);
            }

            ApplyButton(continueButton, ui, ui.titleButtonColor, ui.titleMenuButtonAnchorY, 0);
            ApplyButton(newGameButton, ui, ui.titleButtonColor, ui.titleMenuButtonAnchorY, 1);
            ApplyButton(confirmYesButton, ui, ui.titleConfirmColor, ui.confirmButtonAnchorY, 0);
            ApplyButton(confirmNoButton, ui, ui.titleButtonColor, ui.confirmButtonAnchorY, 1);
            ApplyButton(offlineOkButton, ui, ui.titleButtonColor, ui.offlineButtonAnchorY, 0);
        }

        private static void SetCenter(RectTransform rt, float anchorY, int width, int height)
        {
            rt.anchorMin = new Vector2(0.5f, anchorY);
            rt.anchorMax = new Vector2(0.5f, anchorY);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(width, height);
        }

        private static void ApplyButton(Button button, GameConfigAsset.UiEntry ui, Color color, float anchorY, int row)
        {
            if (button == null) return;
            var img = button.GetComponent<Image>();
            if (img != null) img.color = color;
            var rt = button.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, anchorY);
            rt.anchorMax = new Vector2(0.5f, anchorY);
            rt.anchoredPosition = new Vector2(0, row == 0 ? 0 : -(ui.titleButtonHeight + ui.titleButtonSpacing));
            rt.sizeDelta = new Vector2(ui.titleButtonWidth, ui.titleButtonHeight);
            var label = button.GetComponentInChildren<Text>();
            if (label != null) label.fontSize = ui.fontTitlePanel;
        }

        /// <summary>
        /// Показать экран. onEntered вызывается ровно один раз,
        /// когда игрок вошёл в игру (Продолжить или подтверждённый сброс).
        /// </summary>
        public void Show(GameController game, Action onEntered)
        {
            _game = game;
            _onEntered = onEntered;
            _startedFresh = false;

            Wire(continueButton, OnContinueClicked);
            Wire(newGameButton, OnNewGameClicked);
            Wire(confirmYesButton, OnConfirmYes);
            Wire(confirmNoButton, OnConfirmNo);
            Wire(offlineOkButton, OnOfflineOk);

            if (continueButton != null) continueButton.gameObject.SetActive(game.HasSave);
            if (titlePanel != null) titlePanel.SetActive(true);
            if (confirmPanel != null) confirmPanel.SetActive(false);
            if (offlinePanel != null) offlinePanel.SetActive(false);
            gameObject.SetActive(true);
        }

        private void OnContinueClicked() => EnterGame();

        private void OnNewGameClicked()
        {
            // подтверждение нужно только если есть что терять
            if (_game.HasSave)
            {
                if (titlePanel != null) titlePanel.SetActive(false);
                if (confirmPanel != null) confirmPanel.SetActive(true);
            }
            else
            {
                EnterGame();
            }
        }

        private void OnConfirmYes()
        {
            if (confirmPanel != null) confirmPanel.SetActive(false);
            _game.RestartProgress();
            _startedFresh = true;
            EnterGame();
        }

        private void OnConfirmNo()
        {
            if (confirmPanel != null) confirmPanel.SetActive(false);
            if (titlePanel != null) titlePanel.SetActive(true);
        }

        private void EnterGame()
        {
            var report = _game.Initialize();
            // offline-диалог только при «Продолжить» со старым сейвом
            if (!_startedFresh && report.ShowOfflineDialog)
            {
                if (offlineText != null)
                    offlineText.text =
                        $"Пока вас не было\n+{BalanceView.FormatMoney(report.OfflineIncome)} монет за {FormatDuration(report.OfflineElapsedSeconds)}";
                if (titlePanel != null) titlePanel.SetActive(false);
                if (offlinePanel != null) offlinePanel.SetActive(true);
                return;
            }
            Finish();
        }

        private void OnOfflineOk() => Finish();

        private void Finish()
        {
            if (offlinePanel != null) offlinePanel.SetActive(false);
            gameObject.SetActive(false);
            var callback = _onEntered;
            _onEntered = null;
            callback?.Invoke();
        }

        private static void Wire(Button button, UnityEngine.Events.UnityAction handler)
        {
            if (button == null) return;
            button.onClick.RemoveListener(handler);
            button.onClick.AddListener(handler);
        }

        private static string FormatDuration(long seconds)
        {
            if (seconds >= 3600) return $"{seconds / 3600} ч {(seconds % 3600) / 60} мин";
            if (seconds >= 60) return $"{seconds / 60} мин";
            return $"{seconds} сек";
        }
    }
}
