using MiniFactory.App.Config;
using MiniFactory.Core.Game;
using MiniFactory.Core.Model;
using UnityEngine;
using UnityEngine.UI;

namespace MiniFactory.App.View
{
    /// <summary>
    /// Мини-туториал: анимированная стрелка над кнопкой действия нужной машины.
    /// Потребитель: зашил свой tutorialId, композер выдал TutorialDirector по этому Id.
    /// Цель берёт у TutorialDirector (data-driven шаги); следует за кнопкой (список скроллится).
    /// Стрелка видна, только когда кнопка доступна (хватает монет).
    /// </summary>
    public sealed class TutorialOverlay : MonoBehaviour
    {
        private const float BobAmplitude = 14f;
        private const float BobSpeed = 5f;

        [Tooltip("Id туториала — по нему композер выдаёт TutorialDirector")]
        [SerializeField] private string tutorialId = "main";

        [SerializeField] private MachineCardSpawner cardSpawner;
        [SerializeField] private RectTransform arrow;
        [SerializeField] private Text arrowLabel;

        /// <summary>Id туториала, который хочет этот потребитель.</summary>
        public string TutorialId => tutorialId;

        private GameController _game;
        private TutorialDirector _tutorial;
        private RectTransform _target;
        private Button _targetButton;
        private Vector2 _offset;
        private bool _listenersBound;

        public void Initialize(GameController game, TutorialDirector tutorial)
        {
            _game = game;
            _tutorial = tutorial;
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
            Resolve();
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

        private void OnStepChanged(int _) => Resolve();

        // машина могла открыться/улучшиться — цель или доступность кнопки изменились
        private void OnMachineChanged(MachineState _) => Resolve();

        // баланс изменился — кнопка могла стать доступной/недоступной
        private void OnBalanceChanged(double _) => UpdateVisibility();

        private void Resolve()
        {
            var machineId = _tutorial != null ? _tutorial.ResolveTargetMachineId() : null;
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
