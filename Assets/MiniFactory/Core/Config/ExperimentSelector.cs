using System;
using MiniFactory.Core.Save;

namespace MiniFactory.Core.Config
{
    /// <summary>
    /// Выбор A/B варианта эксперимента: при первом запуске — случайно,
    /// дальше запоминается в сейве. Вариант A = контроль (базовый конфиг),
    /// B = эксперимент (оверрайды из ExperimentConfig).
    /// </summary>
    public static class ExperimentSelector
    {
        public const string VariantA = "A";
        public const string VariantB = "B";

        /// <summary>Возвращает текущий вариант из сейва; пусто/невалидно — A.</summary>
        public static string GetVariant(PlayerProgress progress)
        {
            var v = progress?.experimentVariant;
            return v == VariantB ? VariantB : VariantA;
        }

        /// <summary>Определяет вариант при первом запуске и сохраняет в прогресс.</summary>
        public static string AssignInitial(PlayerProgress progress, Random random = null)
        {
            if (progress == null) throw new ArgumentNullException(nameof(progress));
            if (!string.IsNullOrEmpty(progress.experimentVariant))
                return progress.experimentVariant;

            var variant = (random ?? new Random()).Next(2) == 0 ? VariantA : VariantB;
            progress.experimentVariant = variant;
            return variant;
        }

        /// <summary>Принудительная смена варианта (debug-панель).</summary>
        public static void SetVariant(PlayerProgress progress, string variant)
        {
            if (progress == null) throw new ArgumentNullException(nameof(progress));
            progress.experimentVariant = variant == VariantB ? VariantB : VariantA;
        }

        /// <summary>Возвращает ExperimentConfig для варианта B; для A — null (контроль).</summary>
        public static ExperimentConfig ResolveExperiment(string variant, ExperimentConfig overrides)
        {
            return variant == VariantB ? overrides : null;
        }
    }
}
