using System.Collections.Generic;

namespace MiniFactory.Core.Contracts
{
    /// <summary>
    /// Асинхронный источник удалённых значений конфигурации (ТЗ: доп. задание — Firebase Remote Config).
    /// Core не знает про Firebase — только про этот контракт.
    /// </summary>
    public interface IRemoteConfigService
    {
        /// <summary>Запускает fetch; результат доступен после OnFetched / TryGetValue.</summary>
        void Fetch();

        /// <summary>В true после успешного fetch (или если remote выключен).</summary>
        bool IsReady { get; }

        /// <summary>Удалённое значение по ключу; false — ключа нет или fetch не завершён.</summary>
        bool TryGetValue(string key, out string value);

        /// <summary>Всплывает после каждого успешного fetch.</summary>
        event System.Action Fetched;
    }
}
