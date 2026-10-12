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
        public void AssignInitial_FirstTime_AssignsAndSaves()
        {
            var p = new PlayerProgress();
            var v = ExperimentSelector.AssignInitial(p, new Random(42));
            Assert.IsTrue(v == "A" || v == "B");
            Assert.AreEqual(v, p.experimentVariant);
        }

        [Test]
        public void ResolveExperiment_B_ReturnsOverrides()
        {
            var exp = new ExperimentConfig { ProductionMultiplier = 2f };
            var result = ExperimentSelector.ResolveExperiment("B", exp);
            Assert.IsNotNull(result);
            Assert.AreEqual(2f, result.ProductionMultiplier);
        }
    }
}
