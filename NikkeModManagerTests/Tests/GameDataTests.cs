using NikkeModManagerCore;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NikkeModManagerTests.Tests {
    internal class GameDataTests : Defines {

        protected readonly string LocalCacheDirectory = Path.Join("game_data_tests", "cache");
        protected string LocalModDirectory = Path.Join("game_data_tests", "mods");

        protected readonly string LocalExistingCacheDirectory = Path.Join("test_data", "game_data_tests_existing", "cache");
        protected string LocalExistingModDirectory = Path.Join("test_data", "game_data_tests_existing", "mods");

        protected readonly string LocalOutdatedCacheDirectory = Path.Join("test_data", "game_data_tests_outdated", "cache");
        protected string LocalOutdatedModDirectory = Path.Join("test_data", "game_data_tests_outdated", "mods");

        [SetUp]
        public void Setup() {
            ResetDirectory(LocalModDirectory);
            ResetDirectory(LocalCacheDirectory);
        }

        void ValidateBundleFiles(NikkeMod mod) {
            List<NikkeBundle> bundles = mod.Bundles;

            foreach (NikkeBundle bundle in bundles) {
                string cachedFilename = NikkeDataHelper.GetFilename(bundle.FileIdentifier);
                Assert.AreEqual(cachedFilename, bundle.FileName);
                Assert.True(File.Exists(Path.Join(GameDirectory, cachedFilename)));
            }
        }

        [Test]
        public void TestDefaultModCreation() {
            Assert.AreEqual(0, Directory.GetFiles(LocalModDirectory, "*", SearchOption.AllDirectories).Length);
            Assert.AreEqual(0, Directory.GetFiles(LocalCacheDirectory, "*", SearchOption.AllDirectories).Length);

            ModCollector collector = new ModCollector();
            List<NikkeMod> mods = collector.CollectMods(LocalCacheDirectory, LocalModDirectory, GameDirectory);
            Assert.AreEqual(1, mods.Count);
            List<NikkeBundle> bundles = mods.First().Bundles;
            Assert.AreEqual(6, bundles.Count);

            ValidateBundleFiles(mods.First());

            Assert.AreEqual(6, Directory.GetFiles(LocalModDirectory, "*", SearchOption.AllDirectories).Length);
            Assert.AreEqual(6, Directory.GetFiles(LocalCacheDirectory, "*.cache", SearchOption.AllDirectories).Length);
        }

        [Test]
        public void TestDefaultModInvalidMapping() {
            Dictionary<string, string> fileMappings = new Dictionary<string, string>() {
                {"c432_0_aim", "asdasd"},
                {"c191_1_aim", "asdasd"},
                {"c432_0_cover", "asdasd"},
                {"c432_0_idle", "asdasd"},
                {"c191_0_cover", "asdasd"},
                {"c191_0_idle", "asdasd"},
            };

            if (File.Exists(NikkeDataHelper.NAME_DATA_FILE)) File.Delete(NikkeDataHelper.NAME_DATA_FILE);
            fileMappings.ToList().ForEach(q => NikkeDataHelper.SetFilename(q.Key, q.Value));

            CopyFilesRecursively(LocalExistingModDirectory, LocalModDirectory);
            CopyFilesRecursively(LocalExistingCacheDirectory, LocalCacheDirectory);

            ModCollector collector = new ModCollector();
            List<NikkeMod> mods = collector.CollectMods(LocalCacheDirectory, LocalModDirectory, GameDirectory);
            Assert.AreEqual(1, mods.Count);
            List<NikkeBundle> bundles = mods.First().Bundles;
            Assert.AreEqual(6, bundles.Count);

            ValidateBundleFiles(mods.First());
        }


        [Test]
        public void TestDefaultModOutdatedMapping() {
            Dictionary<string, string> fileMappings = new Dictionary<string, string>() {
                {"c432_0_aim", "00a0cea013ca9b06c4757ac267546788"},
                {"c191_1_aim", "9068ce377b1f6b5eb9da6ca642bac2b2"},
                {"c432_0_cover", "0ae94c6ed1d4f45b16d3ee8c02c5b449"},
                {"c432_0_idle", "0ad65d4b80b090ac85b3cbee5fc0e28c"},
                {"c191_0_cover", "0a3d7c87b374f9187589d8a8003c2b8c"},
                {"c191_0_idle", "0a41eacccfa3691ea55a1c9e51db5b3d"},
            };

            if (File.Exists(NikkeDataHelper.NAME_DATA_FILE)) File.Delete(NikkeDataHelper.NAME_DATA_FILE);
            fileMappings.ToList().ForEach(q => NikkeDataHelper.SetFilename(q.Key, q.Value));

            CopyFilesRecursively(LocalOutdatedModDirectory, LocalModDirectory);
            CopyFilesRecursively(LocalOutdatedCacheDirectory, LocalCacheDirectory);

            ModCollector collector = new ModCollector();
            List<NikkeMod> mods = collector.CollectMods(LocalCacheDirectory, LocalModDirectory, GameDirectory);
            Assert.AreEqual(1, mods.Count);
            List<NikkeBundle> bundles = mods.First().Bundles;
            Assert.AreEqual(6, bundles.Count);

            ValidateBundleFiles(mods.First());
        }
    }
}
