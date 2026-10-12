using MiniFactory.Core.Config;
using MiniFactory.Core.Offline;
using NUnit.Framework;

namespace MiniFactory.Tests.EditMode
{
    /// <summary>Offline-прогресс (ТЗ п.2): базовый доход, лимит, Boost.</summary>
    public class OfflineCalculatorTests
    {
        private static GameConfig Config(
            int maxOfflineSeconds = 7200,
            float boostMultiplier = 3f,
            bool boostEnabled = true)
        {
            return new GameConfig
            {
                MaxOfflineSeconds = maxOfflineSeconds,
                BoostMultiplier = boostMultiplier,
                BoostEnabled = boostEnabled
            };
        }

        [Test]
        public void Calculate_SixtySeconds_AppliesBaseRate()
        {
            var result = OfflineCalculator.Calculate(60, 10, 100_000, 0, Config());
            Assert.AreEqual(600, result.Income, 0.0001);
            Assert.AreEqual(60, result.AppliedSeconds);
        }

        [Test]
        public void Calculate_OfflineLongerThanLimit_CapsToMaxDuration()
        {
            var result = OfflineCalculator.Calculate(86_400, 10, 200_000, 0, Config(7200));
            Assert.AreEqual(7200, result.AppliedSeconds);
            Assert.AreEqual(72_000, result.Income, 0.0001);
        }

        [Test]
        public void Calculate_ActiveBoost_AppliesMultiplierOnlyToBoostWindow()
        {
            var now = 100_000;
            var result = OfflineCalculator.Calculate(600, 10, now, now - 300, Config());
            Assert.AreEqual(300, result.BoostedSeconds);
            Assert.AreEqual(12_000, result.Income, 0.0001); // 300×10×3 + 300×10
        }
    }
}
