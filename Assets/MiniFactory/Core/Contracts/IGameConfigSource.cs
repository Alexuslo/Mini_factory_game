using MiniFactory.Core.Config;

namespace MiniFactory.Core.Contracts
{
    /// <summary>
    /// Источник игровой конфигурации (ТЗ п.3): локальная реализация сейчас,
    /// позможно заменить/дополнить Remote Config или экспериментом (A/B)
    /// без изменения игровой логики.
    /// </summary>
    public interface IGameConfigSource
    {
        GameConfig GetConfig();
    }
}
