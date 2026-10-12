using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MiniFactory.App.Config
{
    /// <summary>
    /// Локальный fallback Remote Config: читает JSON из StreamingAssets/remote_config.json.
    /// Работает без Firebase — для разработки, тестов и ревью.
    /// В проде заменяется на FirebaseRemoteConfigService.
    /// </summary>
    public sealed class LocalRemoteConfigService : Core.Contracts.IRemoteConfigService
    {
        private readonly Dictionary<string, string> _values = new Dictionary<string, string>();

        public bool IsReady { get; private set; }
        public event Action Fetched;

        public void Fetch()
        {
            try
            {
                var path = Path.Combine(Application.streamingAssetsPath, "remote_config.json");
                if (!File.Exists(path))
                {
                    Debug.Log("[RemoteConfig] local: remote_config.json not found, using defaults");
                    IsReady = true;
                    Fetched?.Invoke();
                    return;
                }

                var json = File.ReadAllText(path);
                var parsed = JsonUtility.FromJson<RemoteConfigJson>(json);
                _values.Clear();
                if (parsed?.values != null)
                    foreach (var kv in parsed.values)
                        _values[kv.key] = kv.value;

                Debug.Log($"[RemoteConfig] local: loaded {_values.Count} overrides");
                IsReady = true;
                Fetched?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[RemoteConfig] local fetch failed: {e.Message}");
                IsReady = true; // fallback — не блокируем игру
                Fetched?.Invoke();
            }
        }

        public bool TryGetValue(string key, out string value)
        {
            if (IsReady && _values.TryGetValue(key, out var v))
            {
                value = v;
                return true;
            }
            value = null;
            return false;
        }

        [Serializable]
        private sealed class RemoteConfigJson
        {
            public RemoteConfigEntry[] values;
        }

        [Serializable]
        private sealed class RemoteConfigEntry
        {
            public string key;
            public string value;
        }
    }
}
