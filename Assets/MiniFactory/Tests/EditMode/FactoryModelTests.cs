using System.Collections.Generic;
using MiniFactory.Core.Config;
using MiniFactory.Core.Contracts;
using MiniFactory.Core.Model;
using MiniFactory.Core.Save;
using NUnit.Framework;

namespace MiniFactory.Tests.EditMode
{
    [TestFixture]
    public sealed class FactoryModelTests
    {
        private static GameConfig MakeConfig() => new GameConfig
        {
            Machines = new[]
            {
                new MachineConfig { Id = "m1", UnlockCost = 0, BaseProduction = 10 },
                new MachineConfig { Id = "m2", UnlockCost = 50, BaseProduction = 50 }
            }
        };

        [Test]
        public void Constructor_UnlockCostZero_UnlockedImmediately()
        {
            var model = new FactoryModel(MakeConfig());
            Assert.IsTrue(model.GetMachine("m1").IsUnlocked);
            Assert.AreEqual(1, model.GetMachine("m1").Level);
            Assert.IsFalse(model.GetMachine("m2").IsUnlocked);
        }

        [Test]
        public void TrySpend_NegativeAmount_ReturnsFalse()
        {
            var model = new FactoryModel(MakeConfig());
            model.AddBalance(100);

            Assert.IsFalse(model.TrySpend(-1));
            Assert.AreEqual(100, model.Balance);
        }

        [Test]
        public void Restore_OrphanedMachineId_Skipped()
        {
            var model = new FactoryModel(MakeConfig());
            model.Restore(new PlayerProgress
            {
                balance = 500,
                machines = new[]
                {
                    new MachineSave { id = "m1", level = 3, unlocked = true },
                    new MachineSave { id = "ghost", level = 99, unlocked = true }
                }
            });

            Assert.AreEqual(3, model.GetMachine("m1").Level);
            Assert.AreEqual(0, model.GetMachine("m2").Level); // не тронута
        }

        [Test]
        public void Reset_RestoresInitialState()
        {
            var model = new FactoryModel(MakeConfig());
            model.AddBalance(5000);
            model.UnlockMachine("m2");
            model.SetLevel("m1", 5);

            model.Reset();

            Assert.AreEqual(0, model.Balance);
            Assert.IsTrue(model.GetMachine("m1").IsUnlocked);
            Assert.AreEqual(1, model.GetMachine("m1").Level);
            Assert.IsFalse(model.GetMachine("m2").IsUnlocked);
        }
    }
}
