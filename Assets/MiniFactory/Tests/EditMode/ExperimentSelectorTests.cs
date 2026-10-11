using System;
using MiniFactory.Core.Config;
using MiniFactory.Core.Save;
using NUnit.Framework;

namespace MiniFactory.Tests.EditMode
{
    [TestFixture]
    public sealed class ExperimentSelectorTests
    {
        [Test]
        public void GetVariant_Empty_ReturnsA()
        {
            Assert.AreEqual(ExperimentSelector.VariantA, ExperimentSelector.GetVariant(null));
            Assert.AreEqual(ExperimentSelector.VariantA,
                ExperimentSelector.GetVariant(new PlayerProgress()));
        }

        [Test]
        public void GetVariant_B_ReturnsB()
        {
            var p = new PlayerProgress { experimentVariant = "B" };
            Assert.AreEqual(ExperimentSelector.VariantB, ExperimentSelector.GetVariant(p));
        }

        [Test]
        public void GetVariant_Garbage_ReturnsA()
        {
            var p = new PlayerProgress { experimentVariant = "X" };
            Assert.AreEqual(ExperimentSelector.VariantA, ExperimentSelector.GetVariant(p));
        }

        [Test]
        public void AssignInitial_FirstTime_AssignsAndSaves()
        {
            var p = new PlayerProgress();
            var v = ExperimentSelector.AssignInitial(p, new Random(42));
            Assert.IsTrue(v == "A" || v == "B");
            Assert.AreEqual(v, p.experimentVariant);
        }

        [Test]
        public void AssignInitial_AlreadyAssigned_KeepsExisting()
        {
            var p = new PlayerProgress { experimentVariant = "B" };
            var v = ExperimentSelector.AssignInitial(p, new Random(0));
            Assert.AreEqual("B", v);
        }

        [Test]
        public void SetVariant_Toggles()
        {
            var p = new PlayerProgress();
            ExperimentSelector.SetVariant(p, "B");
            Assert.AreEqual("B", p.experimentVariant);
            ExperimentSelector.SetVariant(p, "A");
            Assert.AreEqual("A", p.experimentVariant);
        }

        [Test]
        public void ResolveExperiment_A_ReturnsNull()
        {
            var exp = new ExperimentConfig { ProductionMultiplier = 2f };
            Assert.IsNull(ExperimentSelector.ResolveExperiment("A", exp));
        }

        [Test]
        public void ResolveExperiment_B_ReturnsOverrides()
        {
            var exp = new ExperimentConfig { ProductionMultiplier = 2f };
            var result = ExperimentSelector.ResolveExperiment("B", exp);
            Assert.IsNotNull(result);
            Assert.AreEqual(2f, result.ProductionMultiplier);
        }

        [Test]
        public void ResolveExperiment_BWithNullOverrides_ReturnsNull()
        {
            Assert.IsNull(ExperimentSelector.ResolveExperiment("B", null));
        }
    }
}
