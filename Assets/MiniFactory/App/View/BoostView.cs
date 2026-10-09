using MiniFactory.Core.Game;
using UnityEngine;
using UnityEngine.UI;

namespace MiniFactory.App.View
{
    /// <summary>
    /// Кнопка Boost и отсчёт его окончания.
    /// Длительность и множитель — из конфига (ТЗ п.2).
    /// </summary>
    public sealed class BoostView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Text label;

        private static readonly Color ActiveColor = new Color(0.25f, 0.75f, 0.35f);

        private GameController _game;
        private Image _background;
        private Color _idleColor;

        public void Initialize(GameController game)
        {
            _game = game;
            _background = GetComponent<Image>();
            if (_background != null) _idleColor = _background.color;

            if (button != null)
            {
                button.onClick.RemoveListener(OnClick);
                button.onClick.AddListener(OnClick);
            }
            Refresh();
        }

        private void OnClick() => _game?.StartBoost();

        private void Update()
        {
            if (_game == null) return;

            if (!_game.Config.BoostEnabled)
            {
                SetLabel("Boost выключен");
                if (button != null) button.interactable = false;
                return;
            }

            var remaining = _game.BoostRemainingSeconds;
            if (remaining > 0)
            {
                SetLabel($"Boost ×{_game.Config.BoostMultiplier:0.#} {FormatTime(remaining)}");
                if (button != null) button.interactable = false;
                if (_background != null) _background.color = ActiveColor; // буст активен — подсветка
            }
            else
            {
                SetLabel($"Boost ×{_game.Config.BoostMultiplier:0.#}");
                if (button != null) button.interactable = true;
                if (_background != null) _background.color = _idleColor;
            }
        }

        private void Refresh()
        {
            if (_game == null) return;
            SetLabel($"Boost ×{_game.Config.BoostMultiplier:0.#}");
        }

        private void SetLabel(string text)
        {
            if (label != null) label.text = text;
        }

        private static string FormatTime(long seconds) =>
            $"{seconds / 60:00}:{seconds % 60:00}";

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(OnClick);
        }
    }
}
