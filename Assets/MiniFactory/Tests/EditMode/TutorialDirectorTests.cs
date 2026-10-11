using MiniFactory.Core.Config;
using MiniFactory.Core.Game;
using MiniFactory.Core.Model;
using NUnit.Framework;

namespace MiniFactory.Tests.EditMode
{
    /// <summary>
    /// Тесты data-driven туториала: триггеры, резолв цели, пустой конфиг.
    /// </summary>
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
        public void EmptySteps_TutorialDisabled()
        {
            var config = MakeConfig(new TutorialStep[0]);
            var model = new FactoryModel(config);
            var director = new TutorialDirector(model, config);

            Assert.IsNull(director.CurrentStep);
            Assert.IsNull(director.ResolveTargetMachineId());

            model.UnlockMachine("m2");
            Assert.AreEqual(0, model.TutorialStep);
        }

        [Test]
        public void SpecificUpgradeTrigger_AdvancesOnlyOnThatMachine()
        {
            var config = MakeConfig(new[]
            {
                new TutorialStep { Trigger = TutorialTrigger.UpgradeMachine, MachineId = "m1", TargetMachine = "first" }
            });
            var model = new FactoryModel(config);
            var director = new TutorialDirector(model, config);

            director.NotifyUpgraded("m2"); // чужая машина — не двигаем
            Assert.AreEqual(0, model.TutorialStep);

            director.NotifyUpgraded("m1"); // целевая — двигаем
            Assert.AreEqual(1, model.TutorialStep);
        }

        [Test]
        public void SpecificUnlockTrigger_MachineIdEmpty_MatchesAny()
        {
            var config = MakeConfig(new[]
            {
                new TutorialStep { Trigger = TutorialTrigger.UnlockMachine, MachineId = "", TargetMachine = "firstLocked" }
            });
            var model = new FactoryModel(config);
            var director = new TutorialDirector(model, config);

            director.NotifyUnlocked("m2");
            Assert.AreEqual(1, model.TutorialStep);
        }

        [Test]
        public void ResolveTarget_First()
        {
            var config = MakeConfig(new[]
            {
                new TutorialStep { Trigger = TutorialTrigger.UpgradeAny, TargetMachine = "first" }
            });
            var model = new FactoryModel(config);
            var director = new TutorialDirector(model, config);

            Assert.AreEqual("m1", director.ResolveTargetMachineId());
        }

        [Test]
        public void ResolveTarget_FirstLocked()
        {
            var config = MakeConfig(new[]
            {
                new TutorialStep { Trigger = TutorialTrigger.UnlockAny, TargetMachine = "firstLocked" }
            });
            var model = new FactoryModel(config);
            var director = new TutorialDirector(model, config);

            Assert.AreEqual("m2", director.ResolveTargetMachineId());

            model.UnlockMachine("m2");
            Assert.AreEqual("m3", director.ResolveTargetMachineId());

            model.UnlockMachine("m3");
            Assert.IsNull(director.ResolveTargetMachineId());
        }

        [Test]
        public void ResolveTarget_SpecificId()
        {
            var config = MakeConfig(new[]
            {
                new TutorialStep { Trigger = TutorialTrigger.UpgradeAny, TargetMachine = "m3" }
            });
            var model = new FactoryModel(config);
            var director = new TutorialDirector(model, config);

            Assert.AreEqual("m3", director.ResolveTargetMachineId());
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

            director.NotifyUpgraded("m1"); // шаг 0 → 1
            Assert.AreEqual(1, model.TutorialStep);
            Assert.AreEqual("m2", director.ResolveTargetMachineId());

            director.NotifyUnlocked("m2"); // шаг 1 → 2
            Assert.AreEqual(2, model.TutorialStep);
            Assert.AreEqual("m3", director.ResolveTargetMachineId());

            director.NotifyUpgraded("m1"); // не m3 — шаг остаётся
            Assert.AreEqual(2, model.TutorialStep);

            director.NotifyUpgraded("m3"); // шаг 2 → завершено
            Assert.AreEqual(3, model.TutorialStep);
            Assert.IsNull(director.CurrentStep);
        }
    }
}
