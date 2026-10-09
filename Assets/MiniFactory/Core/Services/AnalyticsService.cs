using System;
using System.Collections.Generic;
using MiniFactory.Core.Contracts;

namespace MiniFactory.Core.Services
{
    /// <summary>
    /// Шина аналитики: рассыляет событие всем подключённым провайдерам.
    /// Упавший провайдер не валит игру (обработка ошибок).
    /// </summary>
    public sealed class AnalyticsService : IAnalytics
    {
        private readonly List<IAnalyticsProvider> _providers;

        public AnalyticsService(params IAnalyticsProvider[] providers)
        {
            _providers = providers != null
                ? new List<IAnalyticsProvider>(providers)
                : new List<IAnalyticsProvider>();
        }

        public void Log(string eventName, IReadOnlyDictionary<string, object> parameters = null)
        {
            for (var i = 0; i < _providers.Count; i++)
            {
                try
                {
                    _providers[i].Log(eventName, parameters);
                }
                catch (Exception)
                {
                    // один неработающий провайдер не должен прерывать ни игру,
                    // ни доставку остальным провайдерам
                }
            }
        }
    }
}
