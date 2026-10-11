using System;
using MiniFactory.App.Config;
using MiniFactory.App.View;
using UnityEngine;
using UnityEngine.UI;

namespace MiniFactory.App
{
    /// <summary>
    /// Применяет UI-секцию GameConfigAsset к сцене на рантайме:
    /// правка в инспекторе видна сразу в Play без Build Scene.
    /// Ветка событий модели не трогает — только RectTransform/Image/Text.
    /// </summary>
    public sealed class UiConfigApplier : MonoBehaviour
    {
        [SerializeField] private GameConfigAsset configAsset;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private RectTransform topBar;
        [SerializeField] private Text balanceText;
        [SerializeField] private Text productionText;
        [SerializeField] private BoostView boostView;
        [SerializeField] private ScrollRect machineScroll;
        [SerializeField] private RectTransform storePanel;
        [SerializeField] private MachineCardSpawner cardSpawner;
        [SerializeField] private TitleScreenView titleScreen;
        [SerializeField] private TutorialOverlay tutorialOverlay;
        [SerializeField] private PurchaseView[] purchaseViews;

        private void Awake()
        {
            Apply();
#if UNITY_EDITOR
            // в Play режиме правка ассета в инспекторе применяется на лету
            if (configAsset != null) configAsset.UiChanged += Apply;
#endif
        }

        private void OnDestroy()
        {
#if UNITY_EDITOR
            if (configAsset != null) configAsset.UiChanged -= Apply;
#endif
        }

        public void Apply()
        {
            var ui = configAsset != null && configAsset.ui != null
                ? configAsset.ui
                : new GameConfigAsset.UiEntry();

            if (targetCamera != null)
                targetCamera.backgroundColor = ui.cameraBackground;

            if (topBar != null)
            {
                topBar.sizeDelta = new Vector2(topBar.sizeDelta.x, ui.topBarHeight);
                var topBarRt = topBar;
                var pos = topBarRt.anchoredPosition;
                topBarRt.anchoredPosition = new Vector2(pos.x, -ui.topBarOffsetTop);
                var barImage = topBar.GetComponent<Image>();
                if (barImage != null) barImage.color = ui.topBarColor;
            }

            if (balanceText != null)
            {
                balanceText.fontSize = ui.fontBalance;
                var rt = balanceText.rectTransform;
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -ui.balanceTextTop);
            }

            if (productionText != null)
            {
                productionText.fontSize = ui.fontProduction;
                productionText.color = ui.productionTextColor;
                var rt = productionText.rectTransform;
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -ui.productionTextTop);
            }

            if (boostView != null) boostView.ApplyUi(ui);

            if (machineScroll != null)
            {
                var rt = machineScroll.GetComponent<RectTransform>();
                rt.offsetMin = new Vector2(ui.listSideMargin, ui.storeMargin + ui.storePanelHeight + 20);
                rt.offsetMax = new Vector2(-ui.listSideMargin, -ui.listTopOffset);
            }

            if (storePanel != null)
            {
                storePanel.anchoredPosition = new Vector2(ui.storeMargin, ui.storeMargin);
                storePanel.sizeDelta = new Vector2(-2 * ui.storeMargin, ui.storePanelHeight);
                var storeLayout = storePanel.GetComponent<HorizontalLayoutGroup>();
                if (storeLayout != null) storeLayout.spacing = ui.storeSpacing;
            }

            if (cardSpawner != null) cardSpawner.ApplyUi(ui);

            if (purchaseViews != null)
                foreach (var view in purchaseViews)
                    if (view != null) view.ApplyUi(ui);

            if (titleScreen != null) titleScreen.ApplyUi(ui);
            if (tutorialOverlay != null) tutorialOverlay.ApplyUi(ui);
        }
    }
}
