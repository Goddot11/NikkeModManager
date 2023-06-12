using System.IO;
using NikkeModManagerCore;
using NUnit.Framework;

namespace NikkeModManagerTests.Tests;

internal class NikkeBundleTests : Defines {

    [SetUp]
    public void Setup() {

    }

    [Test]
    public void TestBundleLoad() {
        string filename = "3168a2ef1fdda40ce79f004916cc5b17";
        string modPath = Path.Join(ModDirectory, DirectoryModName, filename);
        using FileStream stream = File.OpenRead(modPath);
        NikkeBundle bundle = new NikkeBundle(modPath, stream, CacheDirectory);


        Assert.AreEqual(filename, bundle.FileName);
        Assert.False(bundle.LoadedFromCache);
        Assert.AreEqual("c202", bundle.CharacterId);
        Assert.AreEqual(0, bundle.SkinKey);
        Assert.AreEqual("idle", bundle.Pose);

        Assert.NotZero(bundle.AtlasFile.Length);
        Assert.NotZero(bundle.AtlasTexture.Length);
        Assert.NotZero(bundle.SkeletonFile.Length);
    }

    [Test]
    public void TestBundleCache() {
        string filename = "3168a2ef1fdda40ce79f004916cc5b17";
        string modPath = Path.Join(ModDirectory, DirectoryModName, filename);
        string cachePath = Path.Join(CacheDirectory, filename);

        NikkeBundle bundle;
        using (FileStream file = File.OpenRead(modPath)) {
            bundle = new NikkeBundle(filename, file, CacheDirectory);
        }

        bundle.ExportToCache(CacheDirectory);

        using (FileStream file = File.OpenRead(modPath)) {
            bundle = new NikkeBundle(filename, file, CacheDirectory);
        }

        Assert.AreEqual(filename, bundle.FileName);
        Assert.True(bundle.LoadedFromCache);
        Assert.AreEqual("c202", bundle.CharacterId);
        Assert.AreEqual(0, bundle.SkinKey);
        Assert.AreEqual("idle", bundle.Pose);

        Assert.NotZero(bundle.AtlasFile.Length);
        Assert.NotZero(bundle.AtlasTexture.Length);
        Assert.NotZero(bundle.SkeletonFile.Length);
    }
}