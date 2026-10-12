using MiniFactory.Core.Config;
using MiniFactory.Core.Contracts;
using NUnit.Framework;

namespace MiniFactory.Tests.EditMode
{
    /// <summary>A/B-овой: ExperimentConfigSource применяет оверрайды поверх базы.</summary>
    public class ExperimentConfigSourceTests
    {
        private sealed class StubSource : IGameConfigSource
        {
            private readonly GameConfig _config;
            public StubSource(GameConfig config) => _config = config;
            public GameConfig GetConfig() => _config;
        }

        private static GameConfig MakeConfig() => new GameConfig
        {
            Machines = new[]
            {
                new MachineConfig { Id = "m1", UnlockCost = 0, BaseProduction = 10, UpgradeCostBase = 100 },
                new MachineConfig { Id = "m2", UnlockCost = 50, BaseProduction = 50, UpgradeCostBase = 500 }
            },
            BoostEnabled = true,
            BoostDurationSeconds = 300,
            BoostMultiplier = 3f,
            MaxOfflineSeconds = 7200,
            TutorialId = "main",
            Tutorial = new TutorialStep[0]
        };

        [Test]
        public void ProductionMultiplier_AppliesToAllMachines()
        {
            var source = new ExperimentConfigSource(
                new StubSource(MakeConfig()),
                new ExperimentConfig { ProductionMultiplier = 2f });

            var result = source.GetConfig();
            Assert.AreEqual(20, result.Machines[0].BaseProduction, 0.001);
            Assert.AreEqual(100, result.Machines[1].BaseProduction, 0.001);
        }

        [Test]
        public void BaseConfig_NotMutated()
        {
            var baseConfig = MakeConfig();
            var source = new ExperimentConfigSource(
                new StubSource(baseConfig),
                new ExperimentConfig { ProductionMultiplier = 3f });

            source.GetConfig();

            Assert.AreEqual(10, baseConfig.Machines[0].BaseProduction, 0.001);
        }

        [Test]
        public void NonMachineFields_CopiedThrough()
        {
            var source = new ExperimentConfigSource(
                new StubSource(MakeConfig()),
                new ExperimentConfig { ProductionMultiplier = 1.5f });

            var result = source.GetConfig();
            Assert.AreEqual("main", result.TutorialId);
            Assert.AreEqual(3f, result.BoostMultiplier, 0.001);
        }
    }
}
