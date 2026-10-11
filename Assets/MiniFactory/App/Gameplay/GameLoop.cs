using MiniFactory.Core.Game;
using UnityEngine;

namespace MiniFactory.App.Gameplay
{
    /// <summary>
    /// Мост между Unity-циклом и игровой логикой:
    /// тики производства и сохранение при сворачивании (mobile lifecycle, ТЗ п.2).
    /// </summary>
    public sealed class GameLoop : MonoBehaviour
    {
        private GameController _controller;

        /// <summary>true — тики остановлены (титульный экран до входа в игру).</summary>
        public bool Paused { get; set; }

        public void Initialize(GameController controller)
        {
            _controller = controller;
        }

        private void Update()
        {
            if (Paused) return;
            _controller?.Tick(Time.deltaTime);
        }

        // Android убивает процесс без OnApplicationQuit — сохраняем на pause/focus
        private void OnApplicationPause(bool paused)
        {
            if (paused) _controller?.SaveProgress();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) _controller?.SaveProgress();
        }

        private void OnApplicationQuit()
        {
            _controller?.SaveProgress();
        }
    }
}
