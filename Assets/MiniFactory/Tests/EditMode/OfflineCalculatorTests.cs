using MiniFactory.Core.Config;
using MiniFactory.Core.Offline;
using NUnit.Framework;

namespace MiniFactory.Tests.EditMode
{
    /// <summary>
    /// Тесты offline-прогресса (ТЗ п.2): базовый доход, ограничение
    /// максимального времени и учёт активного Boost по реальному времени.
    /// </summary>
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
            // 60 секунд офлайна при 10/сек → 600
            var result = OfflineCalculator.Calculate(
                elapsedSeconds: 60,
                totalCps: 10,
                nowUnix: 100_000,
                boostEndUnix: 0,      // буст не активен
                config: Config());

            Assert.AreEqual(600, result.Income, 0.0001);
            Assert.AreEqual(60, result.AppliedSeconds);
            Assert.AreEqual(0, result.BoostedSeconds);
        }

        [Test]
        public void Calculate_OfflineLongerThanLimit_CapsToMaxDuration()
        {
            // сутки офлайна при лимите 2 часа (7200 сек) → считаем только 7200
            var result = OfflineCalculator.Calculate(
                elapsedSeconds: 86_400,
                totalCps: 10,
                nowUnix: 200_000,
                boostEndUnix: 0,
                config: Config(maxOfflineSeconds: 7200));

            Assert.AreEqual(7200, result.AppliedSeconds);
            Assert.AreEqual(72_000, result.Income, 0.0001);
        }

        [Test]
        public void Calculate_ActiveBoost_AppliesMultiplierOnlyToBoostWindow()
        {
            // Прошло 600 секунд, буст закончился через 300 секунд после выхода.
            // 300 сек × 10 × 3 + 300 сек × 10 = 9000 + 3000 = 12000
            var now = 100_000;
            var result = OfflineCalculator.Calculate(
                elapsedSeconds: 600,
                totalCps: 10,
                nowUnix: now,
                boostEndUnix: now - 600 + 300,
                config: Config());

            Assert.AreEqual(300, result.BoostedSeconds);
            Assert.AreEqual(600, result.AppliedSeconds);
            Assert.AreEqual(12_000, result.Income, 0.0001);
        }

        [Test]
        public void Calculate_BoostExpiredBeforeWindow_NoMultiplierApplied()
        {
            // Буст закончился до начала офлайн-окна → весь доход по базовому тарифу
            var now = 100_000;
            var result = OfflineCalculator.Calculate(
                elapsedSeconds: 600,
                totalCps: 10,
                nowUnix: now,
                boostEndUnix: now - 600 - 100,   // истёк за 100 сек до выхода
                config: Config());

            Assert.AreEqual(0, result.BoostedSeconds);
            Assert.AreEqual(6000, result.Income, 0.0001);
        }

        [Test]
        public void Calculate_BoostDisabledInConfig_IgnoresBoostWindow()
        {
            // Boost выключен конфигом (ТЗ п.3) → даже активное окно не учитывается
            var now = 100_000;
            var result = OfflineCalculator.Calculate(
                elapsedSeconds: 600,
                totalCps: 10,
                nowUnix: now,
                boostEndUnix: now + 1000,
                config: Config(boostEnabled: false));

            Assert.AreEqual(0, result.BoostedSeconds);
            Assert.AreEqual(6000, result.Income, 0.0001);
        }

        [Test]
        public void Calculate_ZeroElapsedTime_ReturnsZero()
        {
            var result = OfflineCalculator.Calculate(
                elapsedSeconds: 0,
                totalCps: 10,
                nowUnix: 100_000,
                boostEndUnix: 0,
                config: Config());

            Assert.AreEqual(0, result.Income, 0.0001);
            Assert.AreEqual(0, result.AppliedSeconds);
        }
    }
}
