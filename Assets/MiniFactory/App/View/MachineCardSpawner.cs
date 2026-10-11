using MiniFactory.Core.Game;
using UnityEngine;

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

        public void Initialize(GameController game)
        {
            _game = game;

            foreach (var machine in game.Config.Machines)
            {
                if (cardPrefab == null) break;
                var card = Instantiate(cardPrefab, transform);
                card.name = $"Card_{machine.Id}";
                card.Initialize(game, machine.Id);
            }
        }
    }
}
