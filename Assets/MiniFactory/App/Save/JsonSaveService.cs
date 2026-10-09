using System;
using System.IO;
using MiniFactory.Core.Contracts;
using MiniFactory.Core.Save;
using UnityEngine;

namespace MiniFactory.App.Save
{
    /// <summary>
    /// Сохранение: JSON в файл (атомарная запись через временный файл).
    /// Повреждённое сохранение не роняет игру — Load вернёт null.
    /// </summary>
    public sealed class JsonSaveService : ISaveService
    {
        private readonly string _path;

        public JsonSaveService(string path)
        {
            _path = path ?? throw new ArgumentNullException(nameof(path));
        }

        public static string DefaultPath =>
            Path.Combine(Application.persistentDataPath, "progress.json");

        public bool HasProgress() => File.Exists(_path);

        public PlayerProgress Load()
        {
            if (!File.Exists(_path)) return null;
            try
            {
                var json = File.ReadAllText(_path);
                var progress = JsonUtility.FromJson<PlayerProgress>(json);
                if (progress == null || progress.machines == null) return null;
                return progress;
            }
            catch (Exception)
            {
                // битый файл → читаем как отсутствие сохранения
                return null;
            }
        }

        public void Save(PlayerProgress progress)
        {
            if (progress == null) return;
            var directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            var tmp = _path + ".tmp";
            File.WriteAllText(tmp, JsonUtility.ToJson(progress, true));
            if (File.Exists(_path)) File.Delete(_path);
            File.Move(tmp, _path);
        }

        public void Delete()
        {
            if (File.Exists(_path)) File.Delete(_path);
        }
    }
}
