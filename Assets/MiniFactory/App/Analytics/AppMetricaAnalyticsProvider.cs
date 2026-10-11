// AppMetrica Analytics Provider (доп. задание).
// Активируется когда AppMetrica SDK установлен (см. README: Yandex AppMetrica → .unitypackage).
// До установки SDK этот файл компилируется в пустоту — проект собирается без AppMetrica.
#if MINIFACTORY_APPMETRICA
using System.Collections.Generic;
using MiniFactory.Core.Contracts;
using UnityEngine;
using Yandex.Appmetrica;

namespace MiniFactory.App.Analytics
{
    /// <summary>
    /// AppMetrica Analytics: шлёт события через Yandex AppMetrica SDK.
    /// Core не знает про AppMetrica — только про IAnalyticsProvider (Core.Contracts).
    /// </summary>
    public sealed class AppMetricaAnalyticsProvider : IAnalyticsProvider
    {
        private readonly string _apiKey;

        public AppMetricaAnalyticsProvider(string apiKey)
        {
            _apiKey = apiKey;
            AppMetrica.Activate(_apiKey);
            AppMetrica.ResumeSession();
            Debug.Log("[Analytics] AppMetrica activated");
        }

        public void Log(string eventName, IReadOnlyDictionary<string, object> parameters)
        {
            var dict = new Dictionary<string, string>();
            if (parameters != null)
                foreach (var pair in parameters)
                    dict[pair.Key] = pair.Value?.ToString() ?? "";

            AppMetrica.ReportEvent(eventName, dict);
            Debug.Log($"[Analytics] AppMetrica: {eventName}");
        }
    }
}
#endif
