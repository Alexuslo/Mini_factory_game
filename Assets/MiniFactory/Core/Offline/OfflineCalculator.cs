using System;
using MiniFactory.Core.Config;

namespace MiniFactory.Core.Offline
{
    /// <summary>Результат расчёта offline-дохода.</summary>
    public readonly struct OfflineResult
    {
        /// <summary>Начисленная валюта.</summary>
        public readonly double Income;

        /// <summary>Сколько секунд реально учтено (после ограничения MaxOfflineSeconds).</summary>
        public readonly long AppliedSeconds;

        /// <summary>Из учтённых секунд — сколько пришлось на активный Boost.</summary>
        public readonly long BoostedSeconds;

        public OfflineResult(double income, long appliedSeconds, long boostedSeconds)
        {
            Income = income;
            AppliedSeconds = appliedSeconds;
            BoostedSeconds = boostedSeconds;
        }
    }

    /// <summary>
    /// Чистый расчёт offline-прогресса (ТЗ п.2):
    /// - производство идёт по фактически прошедшему времени;
    /// - время ограничено MaxOfflineSeconds;
    /// - период, пока Boost был активен, учитывается с множителем.
    /// Не зависит от UnityEngine — тестируется без сцены и без реального времени.
    /// </summary>
    public static class OfflineCalculator
    {
        /// <param name="elapsedSeconds">Секунды с момента последней активности.</param>
        /// <param name="totalCps">Суммарная производительность фабрики (без Boost), в секунду.</param>
        /// <param name="nowUnix">Текущее время (для границы окна Boost).</param>
        /// <param name="boostEndUnix">Момент окончания активного Boost; меньше окна — Boost уже истёк.</param>
        public static OfflineResult Calculate(
            long elapsedSeconds,
            double totalCps,
            long nowUnix,
            long boostEndUnix,
            GameConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (elapsedSeconds <= 0 || totalCps <= 0)
                return new OfflineResult(0, 0, 0);

            // 1. Ограничиваем прошедшее время потолком из конфига.
            var applied = Math.Min(elapsedSeconds, (long)config.MaxOfflineSeconds);
            var windowStart = nowUnix - applied;

            // 2. Окно Boost = пересечение [windowStart, now] с временем до boostEndUnix.
            long boosted = 0;
            if (config.BoostEnabled && boostEndUnix > windowStart)
                boosted = Math.Min(boostEndUnix - windowStart, applied);

            // 3. Доход: усиленный участок + обычный.
            var multiplier = config.BoostEnabled ? (double)config.BoostMultiplier : 1.0;
            var income = boosted * totalCps * multiplier
                       + (applied - boosted) * totalCps;

            return new OfflineResult(income, applied, boosted);
        }
    }
}
