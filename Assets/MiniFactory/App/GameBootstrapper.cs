using System.Collections.Generic;
using MiniFactory.App.Analytics;
using MiniFactory.App.Config;
using MiniFactory.App.Gameplay;
using MiniFactory.App.Iap;
using MiniFactory.App.Save;
using MiniFactory.App.Timing;
using MiniFactory.App.View;
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
        [SerializeField] private BalanceView balanceView;
        [SerializeField] private ProductionView productionView;
        [SerializeField] private BoostView boostView;
        [SerializeField] private MachineCardSpawner cardSpawner;
        [SerializeField] private PurchaseView[] purchaseViews;
        [SerializeField] private TitleScreenView titleScreen;
        [SerializeField] private TutorialOverlay tutorialOverlay;

        private GameController _controller;
        private UnityIapService _purchases;
        private string _experimentVariant = "A";

        /// <summary>Id туториала → директор; композер выдаёт по зашитому у потребителя Id.</summary>
        private readonly Dictionary<string, TutorialDirector> _tutorials
            = new Dictionary<string, TutorialDirector>();

        public GameController Controller => _controller;

        private void Awake()
        {
            // --- слой 1: инфраструктура ---
            var clock = new SystemClock();
            var localSource = new LocalConfigSource(configAsset);
            var save = new JsonSaveService(JsonSaveService.DefaultPath);

            // A/B: вариант из сейва (при первом запуске — случайно, запоминается).
            // A = контроль (базовый конфиг), B = эксперимент (оверрайды из GameConfigAsset).
            var existingProgress = save.HasProgress() ? save.Load() : null;
            var variant = Core.Config.ExperimentSelector.AssignInitial(existingProgress
                ?? new Core.Save.PlayerProgress());
            _experimentVariant = variant;
            var experiment = Core.Config.ExperimentSelector.ResolveExperiment(
                variant, configAsset.ToExperimentConfig());

            // ExperimentConfigSource: decorator поверх локального; A → прозрачный прокси
            var baseSource = new Core.Config.ExperimentConfigSource(localSource, experiment);

            // Remote Config (доп. задание): decorator поверх experiment.
            // Firebase — за #if MINIFACTORY_FIREBASE; fallback — локальный JSON.
            Core.Contracts.IRemoteConfigService remoteConfig;
#if MINIFACTORY_FIREBASE
            try
            {
                remoteConfig = new FirebaseRemoteConfigService();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Bootstrapper] Firebase init failed, falling back to local: {e.Message}");
                remoteConfig = new LocalRemoteConfigService();
            }
#else
            remoteConfig = new LocalRemoteConfigService();
#endif
            var remoteSource = new Core.Services.RemoteConfigConfigSource(baseSource, remoteConfig);
            var config = remoteSource.GetConfig();
            // Firebase fetch асинхронный — на первом запуске конфиг строится до завершения.
            // Когда fetch завершится, перечитываем и обновляем поля на том же экземпляре,
            // который держит контроллер (GameConfig — mutable fields).
            remoteConfig.Fetched += () =>
            {
                var updated = remoteSource.GetConfig();
                config.BoostEnabled = updated.BoostEnabled;
                config.BoostDurationSeconds = updated.BoostDurationSeconds;
                config.BoostMultiplier = updated.BoostMultiplier;
                config.MaxOfflineSeconds = updated.MaxOfflineSeconds;
            };
            remoteConfig.Fetch();

            // сохраняем назначенный вариант (при первом запуске)
            if (existingProgress == null)
                save.Save(new Core.Save.PlayerProgress { experimentVariant = variant });
            // analytics: Console всегда; AppMetrica — когда SDK установлен (доп. задание)
            var analyticsProviders = new List<Core.Contracts.IAnalyticsProvider>
            {
                new ConsoleAnalyticsProvider()
            };
#if MINIFACTORY_APPMETRICA
            // API key хранится в AppMetricaSettings.asset (не в коде)
            var appmetricaKey = Resources.Load<AppMetricaSettings>("AppMetricaSettings");
            if (appmetricaKey != null && !string.IsNullOrEmpty(appmetricaKey.ApiKey))
                analyticsProviders.Add(new AppMetricaAnalyticsProvider(appmetricaKey.ApiKey));
#endif
            var analytics = new AnalyticsService(analyticsProviders.ToArray());
            _purchases = new UnityIapService();

            // --- слой 2: логика ---
            var model = new FactoryModel(config);
            var economy = new EconomyService(config);

            // --- контроллер: собирает всё вместе ---
            _controller = new GameController(config, model, economy, save, analytics, clock, _purchases);

            // --- туториал: data-driven шаги, регистрация по Id в словаре композера ---
            var tutorial = new TutorialDirector(model, config);
            tutorial.Bind(_controller);
            _tutorials[tutorial.Id] = tutorial;

            // --- View/луп получают зависимости явным вызовом ---
            if (gameLoop != null) gameLoop.Initialize(_controller);
        }

        /// <summary>Композер выдаёт TutorialDirector по Id, зашитому у потребителя.</summary>
        public TutorialDirector GetTutorial(string tutorialId)
        {
            return !string.IsNullOrEmpty(tutorialId) && _tutorials.TryGetValue(tutorialId, out var t)
                ? t
                : null;
        }

        private void Start()
        {
            // IAP: продукты из конфига, инициализация асинхронна (Fake Store в редакторе)
            var productIds = new List<string>();
            if (_controller.Config.Products != null)
                foreach (var p in _controller.Config.Products) productIds.Add(p.Id);
            _purchases.Initialize(productIds);

            // титульный экран (ТЗ п.10): показываем до входа в игру,
            // автосейвы при этом выключены (см. GameController.SaveProgress).
            // Initialize() зовёт сам титульник (ему нужен StartReport для offline-диалога).
            if (titleScreen != null)
            {
                if (gameLoop != null) gameLoop.Paused = true;
                titleScreen.Show(_controller, OnGameEntered);
            }
            else
            {
                _controller.Initialize();
                OnGameEntered();
            }
        }

        private void OnGameEntered()
        {

            // каждая View подписывается на события модели и шлёт команды контроллеру
            if (balanceView != null) balanceView.Initialize(_controller.Model);
            if (productionView != null) productionView.Initialize(_controller);
            if (boostView != null) boostView.Initialize(_controller);
            if (cardSpawner != null) cardSpawner.Initialize(_controller);
            if (tutorialOverlay != null)
            {
                // потребитель зашил свой Id — композер выдаёт нужного директора
                var tutorial = GetTutorial(tutorialOverlay.TutorialId);
                if (tutorial != null) tutorialOverlay.Initialize(_controller, tutorial);
            }

            // магазин: каждая кнопка получает свой продукт из конфига (порядок совпадает)
            var products = _controller.Config.Products;
            if (purchaseViews != null && products != null)
                for (var i = 0; i < purchaseViews.Length && i < products.Length; i++)
                    if (purchaseViews[i] != null)
                        purchaseViews[i].Initialize(_controller, products[i].Id);

            if (gameLoop != null) gameLoop.Paused = false;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // debug-панель: 5 тапов в правом верхнем углу (только редактор / dev build)
            MiniFactory.App.DebugTools.DebugPanelView.Attach(_controller, _experimentVariant);
#endif
        }
    }
}
