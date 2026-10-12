using System.Collections.Generic;
using MiniFactory.Core.Config;
using MiniFactory.Core.Model;
using MiniFactory.Core.Services;
using NUnit.Framework;

namespace MiniFactory.Tests.EditMode
{
    [TestFixture]
    public sealed class EconomyServiceTests
    {
        private static GameConfig MakeConfig() => new GameConfig
        {
            Machines = new[]
            {
                new MachineConfig
                {
                    Id = "m1", UnlockCost = 0, BaseProduction = 10,
                    UpgradeCostBase = 100, UpgradeCostGrowth = 1.15f, ProductionGrowth = 1.1f
                }
            }
        };

        [Test]
        public void GetUpgradeCost_FormulaBaseGrowthLevel()
        {
            var economy = new EconomyService(MakeConfig());
            var machine = MakeConfig().Machines[0];

            // base * growth^level
            Assert.AreEqual(100, economy.GetUpgradeCost(machine, 0), 0.001);
            Assert.AreEqual(115, economy.GetUpgradeCost(machine, 1), 0.001);
            Assert.AreEqual(132.25, economy.GetUpgradeCost(machine, 2), 0.01);
        }

        [Test]
        public void GetProduction_ExponentialGrowth()
        {
            var economy = new EconomyService(MakeConfig());
            var machine = MakeConfig().Machines[0];

            // base * level * growth^(level-1)
            Assert.AreEqual(10, economy.GetProduction(machine, 1), 0.001);
            Assert.AreEqual(22, economy.GetProduction(machine, 2), 0.001); // 10 * 2 * 1.1
            Assert.AreEqual(0, economy.GetProduction(machine, 0), 0.001);
        }

        [Test]
        public void GetTotalProduction_SumsUnlockedMachines()
        {
            var config = MakeConfig();
            var economy = new EconomyService(config);
            var model = new FactoryModel(config);

            model.SetLevel("m1", 2); // m1 уже открыта (unlockCost=0)
            var total = economy.GetTotalProduction(model.Machines);

            // m1: 10 × 2 × 1.1 = 22
            Assert.AreEqual(22, total, 0.001);
        }
    }
}
