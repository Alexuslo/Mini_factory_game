namespace MiniFactory.Core.Contracts
{
    /// <summary>
    /// Источник текущего времени. Выделен за интерфейс, чтобы
    /// offline/boost-логика тестировалась фейковыми часами без реального сна.
    /// </summary>
    public interface IClock
    {
        /// <summary>Текущее время в секундах Unix (UTC).</summary>
        long NowUnix { get; }
    }
}
