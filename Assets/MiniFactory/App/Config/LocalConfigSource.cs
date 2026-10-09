using MiniFactory.Core.Config;
using MiniFactory.Core.Contracts;

namespace MiniFactory.App.Config
{
    /// <summary>
    /// Локальный источник конфигурации: ScriptableObject → immutable GameConfig.
    /// Единственное место, где gameplay-данные собираются из ассета.
    /// Замена на Remote Config / ExperimentConfigSource = другой класс, логика не меняется.
    /// </summary>
    public sealed class LocalConfigSource : IGameConfigSource
    {
        private readonly GameConfigAsset _asset;

        public LocalConfigSource(GameConfigAsset asset)
        {
            _asset = asset;
        }

        public GameConfig GetConfig() => _asset.ToGameConfig();
    }
}
