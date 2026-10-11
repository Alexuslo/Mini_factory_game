using MiniFactory.App.Config;
using MiniFactory.Core.Game;
using MiniFactory.Core.Model;
using UnityEngine;
using UnityEngine.UI;

namespace MiniFactory.App.View
{
    /// <summary>
    /// Мини-туториал: анимированная стрелка над кнопкой действия нужной машины.
    /// Сама находит цель по шагу модели и следует за кнопкой (список скроллится).
    /// Не блокирует игру; при шаге ≥ 2 скрывается навсегда.
    /// </summary>
    public sealed class TutorialOverlay : MonoBehaviour
    {
        private const float BobAmplitude = 14f;
        private const float BobSpeed = 5f;

        [SerializeField] private MachineCardSpawner cardSpawner;
        [SerializeField] private RectTransform arrow;
        [SerializeField] private Text arrowLabel;

        private GameController _game;
        private RectTransform _target;
        private Button _targetButton;
        private Vector2 _offset;
        private int _step;
        private bool _listenersBound;

        public void Initialize(GameController game)
        {
            _game = game;
            var model = game.Model;
            if (_listenersBound)
            {
                model.OnTutorialStepChanged -= OnStepChanged;
                model.OnMachineChanged -= OnMachineChanged;
                model.OnBalanceChanged -= OnBalanceChanged;
            }
            model.OnTutorialStepChanged += OnStepChanged;
            model.OnMachineChanged += OnMachineChanged;
            model.OnBalanceChanged += OnBalanceChanged;
            _listenersBound = true;
            Resolve(model.TutorialStep);
        }

        /// <summary>Runtime-применение UI-конфига (без пересборки сцены).</summary>
        public void ApplyUi(GameConfigAsset.UiEntry ui)
        {
            _offset = new Vector2(0, ui.tutorialArrowOffsetY);
            if (arrowLabel != null)
            {
                arrowLabel.fontSize = ui.tutorialArrowSize;
                arrowLabel.color = ui.tutorialArrowColor;
            }
        }

        private void OnStepChanged(int step) => Resolve(step);

        private void OnMachineChanged(MachineState _) => Resolve(_step);

        // баланс изменился — кнопка могла стать доступной/недоступной
        private void OnBalanceChanged(double _) => UpdateVisibility();

        private void Resolve(int step)
        {
            _step = step;
            if (step >= 2 || _game == null)
            {
                _target = null;
                _targetButton = null;
                if (arrow != null) arrow.gameObject.SetActive(false);
                return;
            }

            var machineId = step == 0
                ? _game.Config.Machines[0].Id
                : FindFirstLockedId();
            var card = FindCard(machineId);
            _target = card != null ? card.ActionButtonRect : null;
            _targetButton = card != null ? card.ActionButton : null;
            UpdateVisibility();
        }

        /// <summary>Стрелка видна, только когда целевую кнопку реально можно нажать.</summary>
        private void UpdateVisibility()
        {
            if (arrow == null) return;
            var show = _target != null && _targetButton != null && _targetButton.interactable;
            arrow.gameObject.SetActive(show);
        }

        private string FindFirstLockedId()
        {
            foreach (var m in _game.Model.Machines)
                if (!m.IsUnlocked) return m.Id;
            return null;
        }

        private MachineCardView FindCard(string machineId)
        {
            if (cardSpawner == null || string.IsNullOrEmpty(machineId)) return null;
            return cardSpawner.GetCard(machineId);
        }

        private void LateUpdate()
        {
            if (_target == null || arrow == null || !arrow.gameObject.activeSelf) return;

            var canvasRt = arrow.parent as RectTransform;
            if (canvasRt == null) return;

            var corners = new Vector3[4];
            _target.GetWorldCorners(corners);
            var topCenter = (corners[1] + corners[2]) * 0.5f;
            var screen = RectTransformUtility.WorldToScreenPoint(null, topCenter);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screen, null, out var local))
                return;

            var bob = Mathf.Sin(Time.unscaledTime * BobSpeed) * BobAmplitude;
            arrow.anchoredPosition = local + _offset + new Vector2(0, bob);
        }

        private void OnDestroy()
        {
            if (_game == null) return;
            var model = _game.Model;
            model.OnTutorialStepChanged -= OnStepChanged;
            model.OnMachineChanged -= OnMachineChanged;
            model.OnBalanceChanged -= OnBalanceChanged;
        }
    }
}
