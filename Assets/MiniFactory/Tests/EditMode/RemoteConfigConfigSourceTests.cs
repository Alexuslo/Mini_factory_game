using System.Collections.Generic;
using MiniFactory.Core.Config;
using MiniFactory.Core.Contracts;
using MiniFactory.Core.Services;
using NUnit.Framework;

namespace MiniFactory.Tests.EditMode
{
    [TestFixture]
    public sealed class RemoteConfigConfigSourceTests
    {
        private sealed class StubRemote : IRemoteConfigService
        {
            private readonly Dictionary<string, string> _values;
            public bool IsReady { get; set; } = true;
            public event System.Action Fetched { add { } remove { } }

            public StubRemote(Dictionary<string, string> values) => _values = values;

            public void Fetch() { }

            public bool TryGetValue(string key, out string value)
            {
                if (IsReady && _values.TryGetValue(key, out var v))
                {
                    value = v;
                    return true;
                }
                value = null;
                return false;
            }
        }

        private sealed class StubLocal : IGameConfigSource
        {
            public GameConfig GetConfig() => new GameConfig
            {
                Machines = new[] { new MachineConfig { Id = "press", UnlockCost = 0 } },
                BoostEnabled = true,
                BoostDurationSeconds = 300,
                BoostMultiplier = 3f,
                MaxOfflineSeconds = 7200
            };
        }

        [Test]
        public void NoRemote_ReturnsLocalValues()
        {
            var source = new RemoteConfigConfigSource(new StubLocal(), null);
            var c = source.GetConfig();
            Assert.AreEqual(300, c.BoostDurationSeconds);
            Assert.AreEqual(3f, c.BoostMultiplier);
            Assert.IsTrue(c.BoostEnabled);
        }

        [Test]
        public void RemoteOverrides_ApplyToConfig()
        {
            var remote = new StubRemote(new Dictionary<string, string>
            {
                [RemoteConfigConfigSource.KeyBoostDuration] = "600",
                [RemoteConfigConfigSource.KeyBoostMultiplier] = "5",
                [RemoteConfigConfigSource.KeyBoostEnabled] = "false",
                [RemoteConfigConfigSource.KeyMaxOffline] = "3600"
            });
            var source = new RemoteConfigConfigSource(new StubLocal(), remote);
            var c = source.GetConfig();
            Assert.AreEqual(600, c.BoostDurationSeconds);
            Assert.AreEqual(5f, c.BoostMultiplier);
            Assert.IsFalse(c.BoostEnabled);
            Assert.AreEqual(3600, c.MaxOfflineSeconds);
        }

        [Test]
        public void RemotePartialOverrides_OnlyChangedKeys()
        {
            var remote = new StubRemote(new Dictionary<string, string>
            {
                [RemoteConfigConfigSource.KeyBoostMultiplier] = "2.5"
            });
            var source = new RemoteConfigConfigSource(new StubLocal(), remote);
            var c = source.GetConfig();
            Assert.AreEqual(2.5f, c.BoostMultiplier);
            Assert.AreEqual(300, c.BoostDurationSeconds); // локальный дефолт
            Assert.IsTrue(c.BoostEnabled); // локальный дефолт
        }

        [Test]
        public void RemoteMultiplier_ParsesDotDecimal_UnderInvariantCulture()
        {
            var prev = System.Threading.Thread.CurrentThread.CurrentCulture;
            try
            {
                System.Threading.Thread.CurrentThread.CurrentCulture =
                    new System.Globalization.CultureInfo("ru-RU");

                var remote = new StubRemote(new Dictionary<string, string>
                {
                    [RemoteConfigConfigSource.KeyBoostMultiplier] = "2.5"
                });
                var source = new RemoteConfigConfigSource(new StubLocal(), remote);
                Assert.AreEqual(2.5f, source.GetConfig().BoostMultiplier);
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = prev;
            }
        }
    }
}
