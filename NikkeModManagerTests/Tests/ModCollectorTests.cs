using System.IO;
using System.Linq;
using NikkeModManagerCore;
using NUnit.Framework;

namespace NikkeModManagerTests.Tests;

internal class ModCollectorTests : Defines {

    [SetUp]
    public void Setup() {

    }

    [Test]
    public void TestModCollector() {
        ModCollector collector = new ModCollector();
        var mods = collector.CollectMods(CacheDirectory, ModDirectory, GameDirectory);

        string[] names = mods.Select(q => q.Name).ToArray();

        Assert.AreEqual(3, mods.Count);
        Assert.Contains(ModCollector.DefaultGameMod, names);
        Assert.Contains(Path.GetFileNameWithoutExtension(DirectoryModName), names);
        Assert.Contains(Path.GetFileNameWithoutExtension(ZipModName), names);
    }
}