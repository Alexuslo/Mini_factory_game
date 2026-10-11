using System;
using MiniFactory.Core.Config;
using UnityEngine;

namespace MiniFactory.App.Config
{
    /// <summary>
    /// Авторский ассет конфигурации (ТЗ п.3): правится в инспекторе Unity.
    /// Игра ассет не видит — только данные через IGameConfigSource.
    /// </summary>
    [CreateAssetMenu(menuName = "Mini Factory/Game Config", fileName = "GameConfig")]
    public sealed class GameConfigAsset : ScriptableObject
    {
        /// <summary>Всплывает при правке в инспекторе (для live-применения UI в Play).</summary>
        public event Action UiChanged;

        private void OnValidate()
        {
            UiChanged?.Invoke();
        }

        [Serializable]
        public sealed class MachineEntry
        {
            public string id;
            public string displayName;

            [Tooltip("0 — машина доступна сразу (первая машина по ТЗ)")]
            public long unlockCost;

            [Tooltip("Базовый доход в секунду на первом уровне")]
            public double baseProduction;

            public double upgradeCostBase = 10;
            public float upgradeCostGrowth = 1.15f;
            public float productionGrowth = 1.1f;
        }

        [Serializable]
        public sealed class ProductEntry
        {
            public string id;
            public string displayName;

            [Tooltip("Награда монетами; 0 — смотри boostSeconds")]
            public long coinsReward = 1000;

            [Tooltip("Если > 0 — покупка активирует Boost на N секунд вместо монет")]
            public int boostSeconds;
        }

        [Serializable]
        public sealed class TutorialStepEntry
        {
            [Tooltip("UpgradeAny / UnlockAny / UpgradeMachine / UnlockMachine")]
            public string trigger = "UpgradeAny";

            [Tooltip("Для *Machine-триггеров; пусто — любая машина")]
            public string machineId = "";

            [Tooltip("Куда указывает стрелка: first / firstLocked / id машины")]
            public string targetMachine = "firstLocked";
        }

        [Serializable]
        public sealed class UiEntry
        {
            [Header("Camera")]
            [Tooltip("Фон камеры (пустой цех)")]
            public Color cameraBackground = new Color(0.04f, 0.05f, 0.07f);

            [Header("TopBar")]
            [Tooltip("Высота верхней панели, px")]
            public int topBarHeight = 240;

            [Tooltip("Отступ верхнего края панели от верха экрана, px")]
            public int topBarOffsetTop = 0;

            public Color topBarColor = new Color(0.12f, 0.14f, 0.18f);

            [Tooltip("Отступ строки баланса от верха панели, px")]
            public int balanceTextTop = 46;

            [Tooltip("Отступ строки дохода от верха панели, px")]
            public int productionTextTop = 152;
            public Color productionTextColor = new Color(0.75f, 0.8f, 0.85f);
            public int fontBalance = 76;
            public int fontProduction = 44;
            public int fontBoost = 34;

            [Header("Boost button")]
            public Color boostColor = new Color(0.85f, 0.55f, 0.15f);

            [Tooltip("Смещение центра кнопки от правого края панели, px")]
            public int boostOffsetRight = 130;

            [Tooltip("Смещение центра кнопки от верха панели, px")]
            public int boostOffsetTop = 160;
            public int boostWidth = 220;
            public int boostHeight = 76;

            [Header("Card")]
            [Tooltip("Высота карточки машины, px (canvas 1080x1920)")]
            public int cardHeight = 200;

            [Tooltip("Вертикальный отступ между карточками, px")]
            public int cardSpacing = 24;

            public int fontCardName = 42;
            public int fontCardInfo = 32;
            public int fontCardCost = 30;
            public int fontCardButton = 38;
            public int cardButtonWidth = 210;
            public int cardButtonHeight = 100;

            [Tooltip("Отступ кнопки действия от правого края карточки, px")]
            public int cardButtonMargin = 16;
            public Color cardBackgroundColor = new Color(0.30f, 0.30f, 0.32f);
            public Color cardLockedColor = new Color(0.35f, 0.35f, 0.35f);
            public Color cardUnlockedColor = new Color(0.20f, 0.45f, 0.30f);
            public Color cardButtonColor = new Color(0.95f, 0.75f, 0.2f);
            public Color cardInfoColor = new Color(0.8f, 0.85f, 0.9f);
            public Color cardCostColor = new Color(1f, 0.85f, 0.4f);

            [Header("List")]
            [Tooltip("Отступ списка карточек сверху, px (при смене topBarHeight подстройте)")]
            public int listTopOffset = 280;

            [Tooltip("Боковой отступ списка карточек, px (слева и справа)")]
            public int listSideMargin = 20;

            [Header("Store")]
            [Tooltip("Высота панели магазина (и её кнопок), px")]
            public int storePanelHeight = 130;

            [Tooltip("Отступ панели магазина от краёв экрана, px")]
            public int storeMargin = 20;

            [Tooltip("Зазор между кнопками магазина, px")]
            public int storeSpacing = 16;
            public int fontStoreButton = 34;
            public Color storeButtonColor = new Color(0.2f, 0.45f, 0.32f);

            [Header("Title")]
            [Tooltip("Шрифт названия игры на титульном экране")]
            public int fontTitle = 96;

            [Tooltip("Шрифт кнопок и текста панелей титульника")]
            public int fontTitlePanel = 40;

            public int titleButtonWidth = 640;
            public int titleButtonHeight = 120;

            [Tooltip("Зазор между кнопками, px")]
            public int titleButtonSpacing = 40;

            [Tooltip("AnchorY блоков титульника (0 снизу, 1 сверху)")]
            public float titleTextAnchorY = 0.65f;
            public float titleMenuButtonAnchorY = 0.42f;
            public float confirmTextAnchorY = 0.58f;
            public float confirmButtonAnchorY = 0.40f;
            public float offlineTextAnchorY = 0.55f;
            public float offlineButtonAnchorY = 0.38f;

            [Tooltip("Габариты текстовых блоков, px")]
            public int titleTextWidth = 1000;
            public int titleTextHeight = 200;
            public int titlePanelTextWidth = 900;
            public int confirmTextHeight = 160;
            public int offlineTextHeight = 220;

            [Tooltip("Альфа затемнения фона титульника")]
            public float titleBackgroundAlpha = 0.85f;

            [Tooltip("Затемнение панели подтверждения/offline-диалога")]
            public Color titlePanelColor = new Color(0.04f, 0.05f, 0.07f, 0.97f);

            public Color titleButtonColor = new Color(0.2f, 0.45f, 0.32f);
            public Color titleConfirmColor = new Color(0.65f, 0.25f, 0.2f);

            [Header("Tutorial")]
            [Tooltip("Шрифт-стрелка «▼» туториала, px")]
            public int tutorialArrowSize = 72;

            [Tooltip("Смещение центра стрелки от верха кнопки, px (стрелка висит над кнопкой)")]
            public int tutorialArrowOffsetY = 90;

            public Color tutorialArrowColor = new Color(1f, 0.85f, 0.2f);
        }

        [Header("Machines")]
        public MachineEntry[] machines;

        [Header("IAP")]
        public ProductEntry[] products;

        [Header("Boost")]
        public bool boostEnabled = true;
        public int boostDurationSeconds = 300;
        public float boostMultiplier = 3f;

        [Header("Offline")]
        [Tooltip("Максимальное время offline production, сек")]
        public int maxOfflineSeconds = 7200;

        [Header("Tutorial")]
        [Tooltip("Id туториала — по нему композер выдаёт TutorialDirector потребителю")]
        public string tutorialId = "main";

        [Tooltip("Шаги мини-туториала; пусто — выключен. Порядок = порядок продвижения.")]
        public TutorialStepEntry[] tutorialSteps = new TutorialStepEntry[0];

        [Header("UI")]
        [Tooltip("Шрифты и размеры интерфейса (canvas 1080x1920). Применяются при Build Scene")]
        public UiEntry ui = new UiEntry();

        [Header("Experiment (A/B)")]
        [Tooltip("Множитель базового дохода варианта B; null — без изменений (вариант A = контроль)")]
        public float? experimentProductionMultiplier;

        [Tooltip("Множитель стоимости апгрейда варианта B; null — без изменений")]
        public float? experimentUpgradeCostMultiplier;

        [Tooltip("Множитель стоимости открытия варианта B; null — без изменений")]
        public float? experimentUnlockCostMultiplier;

        /// <summary>Оверрайды для варианта B; null-значения означают «без изменений».</summary>
        public Core.Config.ExperimentConfig ToExperimentConfig() => new Core.Config.ExperimentConfig
        {
            ProductionMultiplier = experimentProductionMultiplier,
            UpgradeCostMultiplier = experimentUpgradeCostMultiplier,
            UnlockCostMultiplier = experimentUnlockCostMultiplier
        };

        public GameConfig ToGameConfig()
        {
            var result = new GameConfig
            {
                Machines = new MachineConfig[machines?.Length ?? 0],
                Products = new ProductConfig[products?.Length ?? 0],
                TutorialId = string.IsNullOrEmpty(tutorialId) ? "main" : tutorialId,
                Tutorial = new TutorialStep[tutorialSteps?.Length ?? 0],
                BoostEnabled = boostEnabled,
                BoostDurationSeconds = boostDurationSeconds,
                BoostMultiplier = boostMultiplier,
                MaxOfflineSeconds = maxOfflineSeconds
            };

            for (var i = 0; i < result.Machines.Length; i++)
            {
                var m = machines[i];
                result.Machines[i] = new MachineConfig
                {
                    Id = m.id,
                    DisplayName = m.displayName,
                    UnlockCost = m.unlockCost,
                    BaseProduction = m.baseProduction,
                    UpgradeCostBase = m.upgradeCostBase,
                    UpgradeCostGrowth = m.upgradeCostGrowth,
                    ProductionGrowth = m.productionGrowth
                };
            }

            for (var i = 0; i < result.Products.Length; i++)
            {
                var p = products[i];
                result.Products[i] = new ProductConfig
                {
                    Id = p.id,
                    DisplayName = p.displayName,
                    CoinsReward = p.coinsReward,
                    BoostSeconds = p.boostSeconds
                };
            }

            for (var i = 0; i < result.Tutorial.Length; i++)
            {
                var t = tutorialSteps[i];
                result.Tutorial[i] = new TutorialStep
                {
                    Trigger = ParseTrigger(t.trigger),
                    MachineId = t.machineId,
                    TargetMachine = t.targetMachine
                };
            }

            return result;
        }

        private static TutorialTrigger ParseTrigger(string value)
        {
            return Enum.TryParse<TutorialTrigger>(value, true, out var parsed)
                ? parsed
                : TutorialTrigger.UpgradeAny;
        }
    }
}
