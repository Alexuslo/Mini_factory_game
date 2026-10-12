// Firebase Remote Config адаптер.
// Активируется когда Firebase SDK установлен (см. README: Firebase Remote Config → .unitypackage).
// До установки Firebase этот файл компилируется в пустоту — проект собирается без Firebase.
#if MINIFACTORY_FIREBASE
using System;
using System.Threading.Tasks;
using Firebase.RemoteConfig;
using UnityEngine;

namespace MiniFactory.App.Config
{
    /// <summary>
    /// Firebase Remote Config: fetch с сервера, кэш, дефолты.
    /// Игра не знает про Firebase — только про IRemoteConfigService (Core.Contracts).
    /// </summary>
    public sealed class FirebaseRemoteConfigService : Core.Contracts.IRemoteConfigService
    {
        private readonly FirebaseRemoteConfig _rc;

        public bool IsReady { get; private set; }
        public event Action Fetched;

        public FirebaseRemoteConfigService()
        {
            // DefaultInstance в Unity SDK 13.x сам создаёт app из google-services.json
            _rc = FirebaseRemoteConfig.DefaultInstance;
            // дефолты: значения из локального конфига (fallback если fetch не удался)
            _rc.SetDefaultsAsync(CreateDefaults()).ContinueWith(_ =>
            {
                Debug.Log("[RemoteConfig] Firebase defaults set");
            });
        }

        public async void Fetch()
        {
            try
            {
                var fetchTask = _rc.FetchAsync(TimeSpan.FromSeconds(5));
                await fetchTask;
                await _rc.ActivateAsync();

                IsReady = true;
                Debug.Log("[RemoteConfig] Firebase fetch+activate OK");
                Fetched?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[RemoteConfig] Firebase fetch failed: {e.Message}");
                IsReady = true; // fallback — игра работает с локальными дефолтами
                Fetched?.Invoke();
            }
        }

        public bool TryGetValue(string key, out string value)
        {
            // GetValue возвращает активные значения: кэш с прошлого запуска или дефолты.
            // Не ждём fetch — на 2-м запуске кэш доступен сразу.
            var info = _rc.GetValue(key);
            if (info.Source == ValueSource.RemoteValue || info.Source == ValueSource.DefaultValue)
            {
                value = info.StringValue;
                return true;
            }

            value = null;
            return false;
        }

        private static System.Collections.Generic.IDictionary<string, object> CreateDefaults()
            => new System.Collections.Generic.Dictionary<string, object>
            {
                { "boost_enabled", "true" },
                { "boost_duration_seconds", "300" },
                { "boost_multiplier", "3" },
                { "max_offline_seconds", "7200" }
            };
    }
}
#endif
