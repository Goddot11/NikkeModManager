using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NikkeModManagerCore;
using NUnit.Framework;

namespace NikkeModManagerTests.Tests;

internal class DataServiceTests : Defines {

    [SetUp]
    public void Setup() {

    }

    [Test]
    public void TestServiceCreation() {
        bool callbackInvoked = false;

        NikkeDataService service = new NikkeDataService();
        service.DataUpdated += () => callbackInvoked = true;
        service.LoadData().Wait();

        Assert.IsTrue(callbackInvoked);
        Assert.AreEqual(3, service.GetMods().Count);
        Assert.AreEqual(15, service.GetBundles().Count);
        Assert.AreEqual(2, service.GetNikkesIds().Count);
    }

    [Test]
    public void TestServicePatching() {
        string filename = "3168a2ef1fdda40ce79f004916cc5b17";
        string filePath = Path.Join(GameDirectory, filename);

        NikkeDataService service = new NikkeDataService();
        service.LoadData().Wait();

        NikkeBundle bundle = service.GetBundles().First(bundle => bundle.FileName == filename);
        string targetFilename = NikkeDataHelper.GetFilename(bundle.FileIdentifier);

        List<string> deletedFiles = new List<string>();
        List<string> createdFiles = new List<string>();

        FileSystemWatcher watcher = new FileSystemWatcher(GameDirectory, "*");
        watcher.NotifyFilter = NotifyFilters.Attributes
                               | NotifyFilters.CreationTime
                               | NotifyFilters.DirectoryName
                               | NotifyFilters.FileName
                               | NotifyFilters.LastAccess
                               | NotifyFilters.LastWrite
                               | NotifyFilters.Security
                               | NotifyFilters.Size;

        watcher.EnableRaisingEvents = true;
        watcher.IncludeSubdirectories = true;

        watcher.Deleted += (a, b) => deletedFiles.Add(b.Name);
        watcher.Created += (a, b) => createdFiles.Add(b.Name);

        service.EnableBundle(bundle);
        service.PatchGame();

        Assert.AreEqual(1, deletedFiles.Count);
        Assert.Contains(targetFilename, deletedFiles);
        Assert.AreEqual(1, createdFiles.Count);
        Assert.Contains(targetFilename, createdFiles);

        Assert.True(File.Exists(NikkeDataService.STATE_FILE));
        string[] stateData = File.ReadAllLines(NikkeDataService.STATE_FILE);
        Assert.True(stateData.Any(q => q.Contains(service.GetBundleUniqueIdentifier(bundle))));
    }

    [Test]
    public void TestServicePatchingFailOnMissingFile() {
        string filename = "3168a2ef1fdda40ce79f004916cc5b17";

        NikkeDataService service = new NikkeDataService();
        service.LoadData().Wait();

        int patchedCount = -1;
        service.PatchComplete += bundles => patchedCount = bundles.Count;

        ResetDirectory(GameDirectory);

        NikkeBundle bundle = service.GetBundles().First(bundle => bundle.FileName == filename);
        service.EnableBundle(bundle);
        service.PatchGame();
        Assert.AreEqual(0, patchedCount);
    }

    [Test]
    public void TestDeleteDefaultMod() {
        string filename = "3168a2ef1fdda40ce79f004916cc5b17";

        NikkeDataService service = new NikkeDataService();
        service.LoadData().Wait();

        var mods = service.GetMods().First(bundle => bundle.Name == ModCollector.DefaultGameMod);
        service.DeleteDefaultGameMod();
        Assert.False(Directory.Exists(mods.ModPath));
    }


    [Test]
    public void TestDeleteNikkeFiles() {
        string filename = "3168a2ef1fdda40ce79f004916cc5b17";

        List<string> existingFiles = Directory.GetFiles(GameDirectory).ToList();
        List<string> newFiles = new List<string>() { "test1", "test2" };

        foreach (string file in newFiles) {
            File.WriteAllText(file, "asdasd");
        }

        NikkeDataService service = new NikkeDataService();
        service.LoadData().Wait();

        var mods = service.GetMods().First(bundle => bundle.Name == ModCollector.DefaultGameMod);
        service.DeleteGameNikkeBundles();
        Assert.True(Directory.Exists(GameDirectory));
        foreach (string file in existingFiles) {
            Assert.False(File.Exists(file));
        }
        foreach (string file in newFiles) {
            Assert.True(File.Exists(file));
        }
    }
}