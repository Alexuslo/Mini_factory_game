using System.Collections.Generic;
using System.Text;
using MiniFactory.Core.Contracts;
using UnityEngine;

namespace MiniFactory.App.Analytics
{
    /// <summary>
    /// Обязательный провайдер аналитики (ТЗ п.5): печатает события в консоль.
    /// Реальные SDK подключаются отдельными провайдерами тем же интерфейсом.
    /// </summary>
    public sealed class ConsoleAnalyticsProvider : IAnalyticsProvider
    {
        public void Log(string eventName, IReadOnlyDictionary<string, object> parameters)
        {
            var sb = new StringBuilder(64);
            sb.Append("[Analytics] ").Append(eventName);
            if (parameters != null && parameters.Count > 0)
            {
                sb.Append(" {");
                var first = true;
                foreach (var pair in parameters)
                {
                    if (!first) sb.Append(", ");
                    first = false;
                    sb.Append(pair.Key).Append('=').Append(pair.Value);
                }
                sb.Append('}');
            }
            Debug.Log(sb.ToString());
        }
    }
}
