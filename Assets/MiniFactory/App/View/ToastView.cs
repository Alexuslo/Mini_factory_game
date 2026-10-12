using UnityEngine;
using UnityEngine.UI;

namespace MiniFactory.App.View
{
    /// <summary>Простая всплывающая надпись (toast) внизу экрана.</summary>
    public sealed class ToastView : MonoBehaviour
    {
        [SerializeField] private Text label;
        [SerializeField] private float duration = 2.5f;

        private float _timer;

        public void Show(string message)
        {
            if (label != null) label.text = message;
            gameObject.SetActive(true);
            _timer = duration;
        }

        private void Update()
        {
            if (_timer <= 0f) return;
            _timer -= Time.deltaTime;
            if (_timer <= 0f) gameObject.SetActive(false);
        }
    }
}
