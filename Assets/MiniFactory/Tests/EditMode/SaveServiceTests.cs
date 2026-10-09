using System.IO;
using MiniFactory.App.Save;
using MiniFactory.Core.Save;
using NUnit.Framework;

namespace MiniFactory.Tests.EditMode
{
    /// <summary>Сохранение: round-trip, отсутствие и повреждение файла (обработка ошибок).</summary>
    public class SaveServiceTests
    {
        private string _path;
        private JsonSaveService _save;

        [SetUp]
        public void SetUp()
        {
            _path = Path.Combine(Path.GetTempPath(),
                "minifactory_test_" + Path.GetRandomFileName() + ".json");
            _save = new JsonSaveService(_path);
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_path)) File.Delete(_path);
        }

        [Test]
        public void SaveAndLoad_RoundTrip_PreservesState()
        {
            var progress = new PlayerProgress
            {
                balance = 1234.5,
                lastActiveUnix = 1_700_000_000,
                boostEndUnix = 1_700_000_300,
                tutorialStep = 2,
                machines = new[]
                {
                    new MachineSave { id = "m1", level = 5, unlocked = true },
                    new MachineSave { id = "m2", level = 0, unlocked = false }
                }
            };

            _save.Save(progress);
            var loaded = _save.Load();

            Assert.IsNotNull(loaded);
            Assert.AreEqual(1234.5, loaded.balance, 0.0001);
            Assert.AreEqual(1_700_000_000, loaded.lastActiveUnix);
            Assert.AreEqual(1_700_000_300, loaded.boostEndUnix);
            Assert.AreEqual(2, loaded.tutorialStep);
            Assert.AreEqual(2, loaded.machines.Length);
            Assert.AreEqual("m2", loaded.machines[1].id);
            Assert.IsFalse(loaded.machines[1].unlocked);
        }

        [Test]
        public void Load_NoFile_ReturnsNull()
        {
            Assert.IsFalse(_save.HasProgress());
            Assert.IsNull(_save.Load());
        }

        [Test]
        public void Load_CorruptedFile_ReturnsNullInsteadOfCrash()
        {
            File.WriteAllText(_path, "{ this is not valid json !!!");

            Assert.IsNull(_save.Load());
        }

        [Test]
        public void Delete_RemovesProgress()
        {
            _save.Save(new PlayerProgress { balance = 10 });
            Assert.IsTrue(_save.HasProgress());

            _save.Delete();

            Assert.IsFalse(_save.HasProgress());
        }
    }
}
