using NikkeModManagerCore;
using NUnit.Framework;
using System.IO;

namespace NikkeModManagerTests.Tests {
    internal class NikkeModTests : Defines {
        
        protected readonly string LocalCacheDirectory = Path.Join("ModBundleTests", "cache");

        [SetUp]
        public void Setup() {

        }

        [Test]
        public void TestDirectoryMod() {
            string modPath = Path.Join(ModDirectory, DirectoryModName);

            NikkeMod mod = new NikkeDirectoryMod(modPath);
            mod.Load(LocalCacheDirectory);

            Assert.AreEqual(6, mod.Bundles.Count);
            Assert.AreEqual(DirectoryModName, mod.Name);
            Assert.AreEqual(Path.Join(ModDirectory, DirectoryModName), mod.ModPath);
        }

        [Test]
        public void TestZipMod() {
            string modPath = Path.Join(ModDirectory, ZipModName);

            NikkeMod mod = new NikkeZipMod(modPath);
            mod.Load(LocalCacheDirectory);

            Assert.AreEqual(3, mod.Bundles.Count);
            Assert.AreEqual(Path.GetFileNameWithoutExtension(ZipModName), mod.Name);
            Assert.AreEqual(Path.Join(ModDirectory, ZipModName), mod.ModPath);
        }

        [Test]
        public void TestModManifest() {
            string modPath = Path.Join(ModDirectory, DirectoryModName);

            NikkeMod mod = new NikkeDirectoryMod(modPath);
            mod.Load(LocalCacheDirectory);

            ModManifest manifest = mod.Manifest;
            Assert.AreEqual("author", manifest.Author);
            Assert.AreEqual("link", manifest.Link);
            Assert.AreEqual("0.0.1", manifest.GameVersion);
            Assert.AreEqual("0.1", manifest.ModVersion);
            Assert.AreEqual(1, manifest.Data.Count);
            Assert.True(manifest.Data.ContainsKey("CustomField"));
            Assert.AreEqual("CustomValue", manifest.Data["CustomField"]);
        }
    }
}