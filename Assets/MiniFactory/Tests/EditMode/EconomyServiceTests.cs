using MiniFactory.Core.Config;
using MiniFactory.Core.Model;
using MiniFactory.Core.Services;
using NUnit.Framework;

namespace MiniFactory.Tests.EditMode
{
    /// <summary>Тесты формул экономики (ТЗ п.1): цены улучшений и производительность.</summary>
    public class EconomyServiceTests
    {
        private static MachineConfig Machine(
            double baseProduction = 10,
            double upgradeCostBase = 100,
            float upgradeCostGrowth = 1.15f,
            float productionGrowth = 1.1f)
        {
            return new MachineConfig
            {
                Id = "m1",
                DisplayName = "Test",
                UnlockCost = 50,
                BaseProduction = baseProduction,
                UpgradeCostBase = upgradeCostBase,
                UpgradeCostGrowth = upgradeCostGrowth,
                ProductionGrowth = productionGrowth
            };
        }

        private static EconomyService Economy(params MachineConfig[] machines)
        {
            return new EconomyService(new GameConfig { Machines = machines });
        }

        [Test]
        public void UpgradeCost_GrowsExponentiallyWithLevel()
        {
            var economy = Economy(Machine());
            var config = Machine(upgradeCostBase: 100, upgradeCostGrowth: 1.15f);

            Assert.AreEqual(100, economy.GetUpgradeCost(config, 0), 0.0001);   // 100 * 1.15^0
            Assert.AreEqual(115, economy.GetUpgradeCost(config, 1), 0.0001);   // 100 * 1.15^1
            Assert.AreEqual(132.25, economy.GetUpgradeCost(config, 2), 0.0001); // 100 * 1.15^2
        }

        [Test]
        public void Production_AtFirstLevel_EqualsBaseProduction()
        {
            var economy = Economy(Machine(baseProduction: 10));
            var config = Machine(baseProduction: 10);

            Assert.AreEqual(10, economy.GetProduction(config, 1), 0.0001);
        }

        [Test]
        public void Production_ScalesWithLevel()
        {
            var economy = Economy(Machine(baseProduction: 10, productionGrowth: 1.1f));
            var config = Machine(baseProduction: 10, productionGrowth: 1.1f);

            // 10 * 3 * 1.1^2 = 36.3
            Assert.AreEqual(36.3, economy.GetProduction(config, 3), 0.0001);
        }

        [Test]
        public void TotalProduction_CountsOnlyUnlockedMachines()
        {
            var economy = Economy(
                Machine(baseProduction: 10),
                new MachineConfig { Id = "m2", UnlockCost = 50, BaseProduction = 50, UpgradeCostBase = 10 });

            var states = new[]
            {
                new MachineState { Id = "m1", IsUnlocked = true, Level = 1 },
                new MachineState { Id = "m2", IsUnlocked = false, Level = 0 }
            };

            Assert.AreEqual(10, economy.GetTotalProduction(states), 0.0001);
        }
    }
}
