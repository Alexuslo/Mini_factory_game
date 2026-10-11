using MiniFactory.App;
using MiniFactory.App.Config;
using MiniFactory.App.View;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace MiniFactory.Tests.EditMode
{
    /// <summary>
    /// Runtime UI: правка GameConfigAsset.UiEntry должна применяться
    /// к объектам сцены без Build Scene.
    /// </summary>
    public class UiConfigApplierTests
    {
        private GameObject _root;
        private GameConfigAsset _asset;
        private UiConfigApplier _applier;
        private Camera _camera;
        private RectTransform _topBar;
        private Image _topBarImage;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("TestRoot");

            _asset = ScriptableObject.CreateInstance<GameConfigAsset>();
            _asset.ui = new GameConfigAsset.UiEntry
            {
                cameraBackground = new Color(0.2f, 0.3f, 0.4f),
                topBarHeight = 300,
                topBarColor = new Color(0.9f, 0.1f, 0.1f),
                balanceTextTop = 60,
                fontBalance = 80,
                productionTextTop = 160,
                productionTextColor = new Color(0.5f, 0.5f, 0.5f),
                fontProduction = 50,
                listSideMargin = 30,
                listTopOffset = 320,
                storeMargin = 25,
                storePanelHeight = 150,
                storeSpacing = 20,
                storeButtonColor = new Color(0.1f, 0.2f, 0.3f),
                fontStoreButton = 36
            };

            var camGo = new GameObject("Cam", typeof(Camera));
            _camera = camGo.GetComponent<Camera>();
            camGo.transform.SetParent(_root.transform);

            var topBarGo = new GameObject("TopBar", typeof(RectTransform), typeof(Image));
            _topBar = topBarGo.GetComponent<RectTransform>();
            _topBarImage = topBarGo.GetComponent<Image>();
            _topBar.sizeDelta = new Vector2(0, 240);
            topBarGo.transform.SetParent(_root.transform);

            _applier = _root.AddComponent<UiConfigApplier>();

            var so = new UnityEditor.SerializedObject(_applier);
            so.FindProperty("configAsset").objectReferenceValue = _asset;
            so.FindProperty("targetCamera").objectReferenceValue = _camera;
            so.FindProperty("topBar").objectReferenceValue = _topBar;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_asset);
        }

        [Test]
        public void Apply_SetsCameraBackgroundFromConfig()
        {
            _applier.Apply();

            Assert.AreEqual(new Color(0.2f, 0.3f, 0.4f), _camera.backgroundColor);
        }

        [Test]
        public void Apply_SetsTopBarHeightAndColor()
        {
            _applier.Apply();

            Assert.AreEqual(300, _topBar.sizeDelta.y);
            Assert.AreEqual(new Color(0.9f, 0.1f, 0.1f), _topBarImage.color);
        }

        [Test]
        public void Apply_ChangedInInspector_AppliesOnNextApply()
        {
            _asset.ui.topBarHeight = 400;
            _asset.ui.cameraBackground = Color.black;

            _applier.Apply();

            Assert.AreEqual(400, _topBar.sizeDelta.y);
            Assert.AreEqual(Color.black, _camera.backgroundColor);
        }

        [Test]
        public void Apply_SetsBalanceTextPositionAndFont()
        {
            var textGo = new GameObject("Balance", typeof(RectTransform), typeof(Text));
            var text = textGo.GetComponent<Text>();
            var rect = text.rectTransform;
            rect.anchoredPosition = new Vector2(30, -46);
            textGo.transform.SetParent(_root.transform);
            var so = new UnityEditor.SerializedObject(_applier);
            so.FindProperty("balanceText").objectReferenceValue = text;
            so.ApplyModifiedPropertiesWithoutUndo();

            _applier.Apply();

            Assert.AreEqual(-60, rect.anchoredPosition.y, 0.01f);
            Assert.AreEqual(80, text.fontSize);
        }

        [Test]
        public void Apply_SetsScrollOffsetsFromListAndStore()
        {
            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect));
            var scrollRect = scrollGo.GetComponent<RectTransform>();
            scrollGo.transform.SetParent(_root.transform);
            var so = new UnityEditor.SerializedObject(_applier);
            so.FindProperty("machineScroll").objectReferenceValue = scrollGo.GetComponent<ScrollRect>();
            so.ApplyModifiedPropertiesWithoutUndo();

            _applier.Apply();

            // offsetMin.y = storeMargin + storePanelHeight + 20 = 25 + 150 + 20
            Assert.AreEqual(195, scrollRect.offsetMin.y, 0.01f);
            Assert.AreEqual(30, scrollRect.offsetMin.x, 0.01f);
            Assert.AreEqual(-30, scrollRect.offsetMax.x, 0.01f);
            Assert.AreEqual(-320, scrollRect.offsetMax.y, 0.01f);
        }

        [Test]
        public void Apply_SetsStorePanelGeometryAndButtonStyle()
        {
            var storeGo = new GameObject("Store", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            var storeRect = storeGo.GetComponent<RectTransform>();
            var storeLayout = storeGo.GetComponent<HorizontalLayoutGroup>();
            storeGo.transform.SetParent(_root.transform);

            var buttonGo = new GameObject("Buy", typeof(RectTransform), typeof(Image), typeof(Button));
            var buttonImage = buttonGo.GetComponent<Image>();
            buttonGo.transform.SetParent(storeGo.transform);
            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(buttonGo.transform);
            var purchaseView = buttonGo.AddComponent<PurchaseView>();
            var soButton = new UnityEditor.SerializedObject(purchaseView);
            soButton.FindProperty("button").objectReferenceValue = buttonGo.GetComponent<Button>();
            soButton.FindProperty("label").objectReferenceValue = labelGo.GetComponent<Text>();
            soButton.ApplyModifiedPropertiesWithoutUndo();

            var so = new UnityEditor.SerializedObject(_applier);
            so.FindProperty("storePanel").objectReferenceValue = storeRect;
            var views = so.FindProperty("purchaseViews");
            views.arraySize = 1;
            views.GetArrayElementAtIndex(0).objectReferenceValue = purchaseView;
            so.ApplyModifiedPropertiesWithoutUndo();

            _applier.Apply();

            Assert.AreEqual(new Vector2(25, 25), storeRect.anchoredPosition);
            Assert.AreEqual(150, storeRect.sizeDelta.y);
            Assert.AreEqual(20, storeLayout.spacing);
            Assert.AreEqual(new Color(0.1f, 0.2f, 0.3f), buttonImage.color);
            Assert.AreEqual(36, buttonGo.GetComponentInChildren<Text>(true).fontSize);
        }

        [Test]
        public void Apply_CardSpawner_SetsSpacingAndCardHeight()
        {
            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
            var layout = contentGo.GetComponent<VerticalLayoutGroup>();
            contentGo.transform.SetParent(_root.transform);

            var cardGo = new GameObject("Card", typeof(RectTransform), typeof(LayoutElement), typeof(Image));
            var element = cardGo.GetComponent<LayoutElement>();
            element.preferredHeight = 200;
            cardGo.transform.SetParent(contentGo.transform);

            var so = new UnityEditor.SerializedObject(_applier);
            so.FindProperty("cardSpawner").objectReferenceValue = contentGo.AddComponent<MachineCardSpawner>();
            so.ApplyModifiedPropertiesWithoutUndo();

            _asset.ui.cardSpacing = 48;
            _asset.ui.cardHeight = 260;

            _applier.Apply();

            Assert.AreEqual(48, layout.spacing);
            Assert.AreEqual(260, element.preferredHeight);
        }
    }
}
