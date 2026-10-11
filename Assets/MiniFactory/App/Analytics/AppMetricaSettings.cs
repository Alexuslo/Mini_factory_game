using UnityEngine;

namespace MiniFactory.App.Analytics
{
    /// <summary>
    /// Хранит API key AppMetrica (не в коде — в ассете).
    /// Создаётся: Create Asset → Mini Factory → AppMetrica Settings.
    /// </summary>
    [CreateAssetMenu(menuName = "Mini Factory/AppMetrica Settings", fileName = "AppMetricaSettings")]
    public sealed class AppMetricaSettings : ScriptableObject
    {
        [Tooltip("API key из кабинета AppMetrica")]
        public string ApiKey = "";
    }
}
