using System;

namespace MiniFactory.Core.Config
{
    /// <summary>Продукт магазина: идентификатор в сторе + награда в валюте.</summary>
    [Serializable]
    public class ProductConfig
    {
        /// <summary>ID продукта, например coins_pack_small (ТЗ п.4).</summary>
        public string Id;

        public string DisplayName;

        /// <summary>Сколько игровой валюты начисляется за покупку.</summary>
        public long CoinsReward = 1000;

        /// <summary>Если &gt; 0 — покупка вместо монет активирует Boost на N секунд.</summary>
        public int BoostSeconds;
    }
}
