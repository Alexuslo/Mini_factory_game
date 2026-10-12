using System;
using System.Globalization;
using MiniFactory.Core.Config;
using MiniFactory.Core.Contracts;

namespace MiniFactory.Core.Services
{
    /// <summary>
    /// Decorator поверх локального IGameConfigSource: применяет удалённые переопределения
    /// поверх локальных значений. Каждый ключ обрабатывается независимо —
    /// отсутствие одного не блокирует остальные. Если remote недоступен — локальный конфиг.
    /// </summary>
    public sealed class RemoteConfigConfigSource : IGameConfigSource
    {
        public const string KeyBoostEnabled = "boost_enabled";
        public const string KeyBoostDuration = "boost_duration_seconds";
        public const string KeyBoostMultiplier = "boost_multiplier";
        public const string KeyMaxOffline = "max_offline_seconds";

        private readonly IGameConfigSource _local;
        private readonly IRemoteConfigService _remote;

        public RemoteConfigConfigSource(IGameConfigSource local, IRemoteConfigService remote)
        {
            _local = local ?? throw new ArgumentNullException(nameof(local));
            _remote = remote;
        }

        public GameConfig GetConfig()
        {
            var config = _local.GetConfig();
            if (_remote == null) return config;

            if (_remote.TryGetValue(KeyBoostEnabled, out var enabled))
                config.BoostEnabled = ParseBool(enabled, config.BoostEnabled);

            if (_remote.TryGetValue(KeyBoostDuration, out var duration)
                && int.TryParse(duration, NumberStyles.Integer, CultureInfo.InvariantCulture, out var d) && d > 0)
                config.BoostDurationSeconds = d;

            if (_remote.TryGetValue(KeyBoostMultiplier, out var mult)
                && float.TryParse(mult, NumberStyles.Float, CultureInfo.InvariantCulture, out var m) && m > 0f)
                config.BoostMultiplier = m;

            if (_remote.TryGetValue(KeyMaxOffline, out var offline)
                && int.TryParse(offline, NumberStyles.Integer, CultureInfo.InvariantCulture, out var o) && o > 0)
                config.MaxOfflineSeconds = o;

            return config;
        }

        private static bool ParseBool(string value, bool fallback)
        {
            if (bool.TryParse(value, out var b)) return b;
            if (value == "1") return true;
            if (value == "0") return false;
            return fallback;
        }
    }
}
