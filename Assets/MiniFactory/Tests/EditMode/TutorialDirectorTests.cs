using MiniFactory.Core.Config;
using MiniFactory.Core.Game;
using MiniFactory.Core.Model;
using NUnit.Framework;

namespace MiniFactory.Tests.EditMode
{
    /// <summary>Data-driven туториальный: фильтр по машине, порядок шагов.</summary>
    public class TutorialDirectorTests
    {
        private static GameConfig MakeConfig(TutorialStep[] steps) => new GameConfig
        {
            Machines = new[]
            {
                new MachineConfig { Id = "m1", UnlockCost = 0, BaseProduction = 1, UpgradeCostBase = 10 },
                new MachineConfig { Id = "m2", UnlockCost = 50, BaseProduction = 5, UpgradeCostBase = 50 },
                new MachineConfig { Id = "m3", UnlockCost = 500, BaseProduction = 25, UpgradeCostBase = 200 }
            },
            Tutorial = steps
        };

        [Test]
        public void SpecificUpgradeTrigger_AdvancesOnlyOnThatMachine()
        {
            var config = MakeConfig(new[]
            {
                new TutorialStep { Trigger = TutorialTrigger.UpgradeMachine, MachineId = "m1", TargetMachine = "first" }
            });
            var model = new FactoryModel(config);
            var director = new TutorialDirector(model, config);

            director.NotifyUpgraded("m2"); // чужая — не двигаем
            Assert.AreEqual(0, model.TutorialStep);

            director.NotifyUpgraded("m1"); // целевая — двигаем
            Assert.AreEqual(1, model.TutorialStep);
        }

        [Test]
        public void MultiStep_FollowsConfigOrder()
        {
            var config = MakeConfig(new[]
            {
                new TutorialStep { Trigger = TutorialTrigger.UpgradeAny, TargetMachine = "first" },
                new TutorialStep { Trigger = TutorialTrigger.UnlockAny, TargetMachine = "firstLocked" },
                new TutorialStep { Trigger = TutorialTrigger.UpgradeMachine, MachineId = "m3", TargetMachine = "m3" }
            });
            var model = new FactoryModel(config);
            var director = new TutorialDirector(model, config);

            director.NotifyUpgraded("m1");
            Assert.AreEqual(1, model.TutorialStep);

            director.NotifyUnlocked("m2");
            Assert.AreEqual(2, model.TutorialStep);

            director.NotifyUpgraded("m3");
            Assert.AreEqual(3, model.TutorialStep);
            Assert.IsNull(director.CurrentStep);
        }
    }
}
