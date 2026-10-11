using MiniFactory.Core.Config;
using MiniFactory.Core.Contracts;
using NUnit.Framework;

namespace MiniFactory.Tests.EditMode
{
    /// <summary>
    /// Тесты A/B-шова: ExperimentConfigSource применяет оверрайды поверх базы,
    /// не трогая базовый источник.
    /// </summary>
    public class ExperimentConfigSourceTests
    {
        private sealed class StubSource : IGameConfigSource
        {
            private readonly GameConfig _config;
            public int Calls;

            public StubSource(GameConfig config) => _config = config;

            public GameConfig GetConfig()
            {
                Calls++;
                return _config;
            }
        }

        private static GameConfig MakeConfig() => new GameConfig
        {
            Machines = new[]
            {
                new MachineConfig
                {
                    Id = "m1", UnlockCost = 0, BaseProduction = 10,
                    UpgradeCostBase = 100, UpgradeCostGrowth = 1.15f, ProductionGrowth = 1.1f
                },
                new MachineConfig
                {
                    Id = "m2", UnlockCost = 50, BaseProduction = 50,
                    UpgradeCostBase = 500, UpgradeCostGrowth = 1.15f, ProductionGrowth = 1.1f
                }
            },
            BoostEnabled = true,
            BoostDurationSeconds = 300,
            BoostMultiplier = 3f,
            MaxOfflineSeconds = 7200,
            TutorialId = "main",
            Tutorial = new[]
            {
                new TutorialStep { Trigger = TutorialTrigger.UpgradeAny, TargetMachine = "first" }
            }
        };

        [Test]
        public void NoOverrides_PassthroughBaseConfig()
        {
            var baseConfig = MakeConfig();
            var source = new ExperimentConfigSource(new StubSource(baseConfig), new ExperimentConfig());

            var result = source.GetConfig();

            Assert.AreSame(baseConfig, result);
        }

        [Test]
        public void ProductionMultiplier_AppliesToAllMachines()
        {
            var baseConfig = MakeConfig();
            var source = new ExperimentConfigSource(
                new StubSource(baseConfig),
                new ExperimentConfig { ProductionMultiplier = 2f });

            var result = source.GetConfig();

            Assert.AreEqual(20, result.Machines[0].BaseProduction, 0.001);
            Assert.AreEqual(100, result.Machines[1].BaseProduction, 0.001);
        }

        [Test]
        public void UpgradeCostMultiplier_Applies()
        {
            var source = new ExperimentConfigSource(
                new StubSource(MakeConfig()),
                new ExperimentConfig { UpgradeCostMultiplier = 0.5f });

            var result = source.GetConfig();

            Assert.AreEqual(50, result.Machines[0].UpgradeCostBase, 0.001);
            Assert.AreEqual(250, result.Machines[1].UpgradeCostBase, 0.001);
        }

        [Test]
        public void UnlockCostMultiplier_Applies()
        {
            var source = new ExperimentConfigSource(
                new StubSource(MakeConfig()),
                new ExperimentConfig { UnlockCostMultiplier = 2f });

            var result = source.GetConfig();

            Assert.AreEqual(0, result.Machines[0].UnlockCost); // 0 * 2 = 0
            Assert.AreEqual(100, result.Machines[1].UnlockCost);
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
            Assert.AreEqual(50, baseConfig.Machines[1].BaseProduction, 0.001);
        }

        [Test]
        public void NonMachineFields_CopiedThrough()
        {
            var source = new ExperimentConfigSource(
                new StubSource(MakeConfig()),
                new ExperimentConfig { ProductionMultiplier = 1.5f });

            var result = source.GetConfig();

            Assert.AreEqual("main", result.TutorialId);
            Assert.AreEqual(1, result.Tutorial.Length);
            Assert.AreEqual(3f, result.BoostMultiplier, 0.001);
            Assert.AreEqual(7200, result.MaxOfflineSeconds);
        }
    }
}
