using MiniFactory.App.Config;
using MiniFactory.Core.Game;
using UnityEngine;
using UnityEngine.UI;

namespace MiniFactory.App.View
{
    /// <summary>
    /// Сетка карточек: создаёт их по количеству машин из конфига
    /// (состав и раскладка — данные, а не хардкод в сцене).
    /// </summary>
    public sealed class MachineCardSpawner : MonoBehaviour
    {
        [SerializeField] private MachineCardView cardPrefab;

        private GameController _game;
        private GameConfigAsset.UiEntry _ui;

        public void Initialize(GameController game)
        {
            _game = game;

            foreach (var machine in game.Config.Machines)
            {
                if (cardPrefab == null) break;
                var card = Instantiate(cardPrefab, transform);
                card.name = $"Card_{machine.Id}";
                card.Initialize(game, machine.Id);
                if (_ui != null) card.ApplyUi(_ui); // карточки появляются после Awake апplierа
            }
        }

        /// <summary>Карточка машины по id (null — ещё не создана).</summary>
        public MachineCardView GetCard(string machineId)
        {
            var t = transform.Find("Card_" + machineId);
            return t != null ? t.GetComponent<MachineCardView>() : null;
        }

        /// <summary>Runtime-применение UI-конфига к раскладке и уже созданным карточкам.</summary>
        public void ApplyUi(GameConfigAsset.UiEntry ui)
        {
            _ui = ui;
            var layout = GetComponent<VerticalLayoutGroup>();
            if (layout != null) layout.spacing = ui.cardSpacing;

            foreach (Transform child in transform)
            {
                var element = child.GetComponent<LayoutElement>();
                if (element != null)
                {
                    element.preferredHeight = ui.cardHeight;
                    element.minHeight = ui.cardHeight;
                }
                var card = child.GetComponent<MachineCardView>();
                if (card != null) card.ApplyUi(ui);
            }
        }
    }
}
