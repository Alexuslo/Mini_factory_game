#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using MiniFactory.Core.Game;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MiniFactory.App.DebugTools
{
    /// <summary>
    /// Debug-панель (только редактор / development build): откроется по 5 тапам
    /// на невидимую зону в правом верхнем углу. Даёт QA/ревьюеру быстрые действия.
    /// UI создаётся в рантайме — сцена не пересобирается.
    /// </summary>
    public sealed class DebugPanelView : MonoBehaviour, IPointerClickHandler
    {
        private const int TapCountToOpen = 5;
        private const float TapWindowSeconds = 1.5f;
        private const float HotZoneSize = 120f;

        private GameController _game;
        private RectTransform _panel;
        private int _taps;
        private float _lastTapTime;

        /// <summary>
        /// Создаёт невидимую hot-zone в правом верхнем углу canvas и вешает на неё панель.
        /// </summary>
        public static DebugPanelView Attach(GameController game)
        {
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return null;

            var hotZone = new GameObject("DebugHotZone", typeof(RectTransform), typeof(Image));
            hotZone.transform.SetParent(canvas.transform, false);
            var rt = (RectTransform)hotZone.transform;
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(HotZoneSize, HotZoneSize);

            var img = hotZone.GetComponent<Image>();
            img.color = new Color(0, 0, 0, 0.01f); // почти прозрачная, но ловит клики

            var view = hotZone.AddComponent<DebugPanelView>();
            view.Initialize(game);
            return view;
        }

        private void Initialize(GameController game)
        {
            _game = game;
            BuildPanel();
            SetPanelVisible(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            var now = Time.unscaledTime;
            if (now - _lastTapTime > TapWindowSeconds) _taps = 0;
            _lastTapTime = now;
            _taps++;
            if (_taps >= TapCountToOpen)
            {
                _taps = 0;
                if (_panel != null) SetPanelVisible(!_panel.gameObject.activeSelf);
            }
        }

        private void SetPanelVisible(bool visible)
        {
            if (_panel != null) _panel.gameObject.SetActive(visible);
        }

        private void BuildPanel()
        {
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;

            var panelGo = new GameObject("DebugPanel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(canvas.transform, false);
            _panel = (RectTransform)panelGo.transform;
            _panel.anchorMin = new Vector2(0.5f, 0.5f);
            _panel.anchorMax = new Vector2(0.5f, 0.5f);
            _panel.pivot = new Vector2(0.5f, 0.5f);
            _panel.sizeDelta = new Vector2(960, 1280);
            panelGo.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.95f);

            var layout = panelGo.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(48, 48, 48, 48);
            layout.spacing = 24;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            AddLabel(_panel, "DEBUG", 80, TextAnchor.MiddleCenter);
            AddButton(_panel, "+1000 монет", () => _game.Model.AddBalance(1000));
            AddButton(_panel, "+10K монет", () => _game.Model.AddBalance(10_000));
            AddButton(_panel, "Открыть первую закрытую", UnlockFirstLocked);
            AddButton(_panel, "+1 уровень первой", UpgradeFirst);
            AddButton(_panel, "Сбросить туториал", () => _game.Model.SetTutorialStep(0));
            AddButton(_panel, "Стереть сейв", () => _game.RestartProgress());
            AddButton(_panel, "Закрыть", () => SetPanelVisible(false));
        }

        private void UnlockFirstLocked()
        {
            foreach (var m in _game.Model.Machines)
                if (!m.IsUnlocked)
                {
                    _game.Model.AddBalance(999_999);
                    _game.UnlockMachine(m.Id);
                    return;
                }
        }

        private void UpgradeFirst()
        {
            foreach (var m in _game.Model.Machines)
                if (m.IsUnlocked)
                {
                    _game.Model.AddBalance(999_999);
                    _game.UpgradeMachine(m.Id);
                    return;
                }
        }

        private static void AddLabel(Transform parent, string text, int size, TextAnchor anchor)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<LayoutElement>().preferredHeight = size + 16;
            var t = go.GetComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = size;
            t.fontStyle = FontStyle.Bold;
            t.alignment = anchor;
            t.color = Color.white;
            t.text = text;
        }

        private static void AddButton(Transform parent, string label, Action onClick)
        {
            var go = new GameObject("Btn_" + label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<LayoutElement>().preferredHeight = 144;
            go.GetComponent<Image>().color = new Color(0.2f, 0.35f, 0.5f);
            go.GetComponent<Button>().onClick.AddListener(() => onClick?.Invoke());

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var rt = (RectTransform)textGo.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var t = textGo.GetComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 64;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.text = label;
        }
    }
}
#endif
