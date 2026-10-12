using MiniFactory.App;
using MiniFactory.App.Config;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace MiniFactory.Tests.EditMode
{
    /// <summary>Runtime UI: правка GameConfigAsset.UiEntry применяется к объектам сцены.</summary>
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
                topBarColor = new Color(0.9f, 0.1f, 0.1f)
            };

            var camGo = new GameObject("Cam", typeof(Camera));
            _camera = camGo.GetComponent<Camera>();
            camGo.transform.SetParent(_root.transform);

            var topBarGo = new GameObject("TopBar", typeof(RectTransform), typeof(Image));
            _topBar = topBarGo.GetComponent<RectTransform>();
            _topBarImage = topBarGo.GetComponent<Image>();
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
        public void Apply_SetsCameraBackgroundAndTopBar()
        {
            _applier.Apply();

            Assert.AreEqual(new Color(0.2f, 0.3f, 0.4f), _camera.backgroundColor);
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
    }
}
