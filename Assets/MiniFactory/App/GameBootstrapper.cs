using MiniFactory.App.Analytics;
using MiniFactory.App.Config;
using MiniFactory.App.Gameplay;
using MiniFactory.App.Save;
using MiniFactory.App.Timing;
using MiniFactory.Core.Game;
using MiniFactory.Core.Model;
using MiniFactory.Core.Services;
using UnityEngine;

namespace MiniFactory.App
{
    /// <summary>
    /// Composition root («композитор»): единственное место,
    /// где создаются все сервисы и раздаются зависимости через конструкторы.
    /// Ни одного синглтона, ни одного DI-фреймворка.
    /// </summary>
    public sealed class GameBootstrapper : MonoBehaviour
    {
        [SerializeField] private GameConfigAsset configAsset;
        [SerializeField] private GameLoop gameLoop;
        [SerializeField] private View.BalanceView balanceView;
        [SerializeField] private View.ProductionView productionView;
        [SerializeField] private View.BoostView boostView;
        [SerializeField] private View.MachineGridRenderer machineGrid;

        private GameController _controller;

        public GameController Controller => _controller;

        private void Awake()
        {
            // --- слой 1: инфраструктура ---
            var clock = new SystemClock();
            var config = new LocalConfigSource(configAsset).GetConfig();
            var save = new JsonSaveService(JsonSaveService.DefaultPath);
            var analytics = new AnalyticsService(new ConsoleAnalyticsProvider());

            // --- слой 2: логика ---
            var model = new FactoryModel(config);
            var economy = new EconomyService(config);

            // --- контроллер: собирает всё вместе ---
            _controller = new GameController(config, model, economy, save, analytics, clock);

            // --- View/луп получают зависимости явным вызовом ---
            if (gameLoop != null) gameLoop.Initialize(_controller);
        }

        private void Start()
        {
            _controller.Initialize();

            // каждая View подписывается на события модели и шлёт команды контроллеру
            if (balanceView != null) balanceView.Initialize(_controller.Model);
            if (productionView != null) productionView.Initialize(_controller);
            if (boostView != null) boostView.Initialize(_controller);
            if (machineGrid != null) machineGrid.Initialize(_controller);
        }
    }
}
