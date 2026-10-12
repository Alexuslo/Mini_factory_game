using System;
using System.Collections.Generic;
using MiniFactory.Core.Contracts;
using MiniFactory.Core.Services;
using NUnit.Framework;

namespace MiniFactory.Tests.EditMode
{
    [TestFixture]
    public sealed class AnalyticsServiceTests
    {
        private sealed class FakeProvider : IAnalyticsProvider
        {
            public readonly List<(string name, IReadOnlyDictionary<string, object> pars)> Received
                = new List<(string, IReadOnlyDictionary<string, object>)>();
            public Exception ThrowOn { get; set; }

            public void Log(string eventName, IReadOnlyDictionary<string, object> parameters)
            {
                if (ThrowOn != null) throw ThrowOn;
                Received.Add((eventName, parameters));
            }
        }

        [Test]
        public void Log_AllProvidersReceiveEvent()
        {
            var p1 = new FakeProvider();
            var p2 = new FakeProvider();
            var service = new AnalyticsService(p1, p2);

            service.Log("test_event");

            Assert.AreEqual(1, p1.Received.Count);
            Assert.AreEqual(1, p2.Received.Count);
        }

        [Test]
        public void Log_ThrowingProvider_DoesNotBlockOthers()
        {
            var bad = new FakeProvider { ThrowOn = new InvalidOperationException("boom") };
            var good = new FakeProvider();
            var service = new AnalyticsService(bad, good);

            service.Log("test_event");

            Assert.AreEqual(1, good.Received.Count);
        }
    }
}
