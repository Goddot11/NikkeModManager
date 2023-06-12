using NikkeModManagerCore;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NikkeModManagerTests.Tests {
    internal class Defines {
        protected readonly string ModDirectory = Path.Join("mods");
        protected readonly string CacheDirectory = Path.Join("cache");
        protected readonly string GameDirectory = Path.Join("game_data");

        protected readonly string SourceModDirectory = Path.Join("test_data", "shared_data", "mods");
        protected readonly string SourceGameDirectory = Path.Join("test_data", "shared_data", "game_data");

        protected readonly string DirectoryModName = "DirectoryMod";
        protected readonly string ZipModName = "ZipMod.zip";

        [SetUp]
        public void GeneralSetup() {

            NikkeConfig.LoadConfig(new NikkeConfig.ConfigData() {
                ModDirectory = ModDirectory,
                CacheDirectory = CacheDirectory,
                GameDirectory = GameDirectory,

                UseMultiThreading = true,
                LoadGameData = true,
            });

            if (File.Exists(NikkeDataHelper.NAME_DATA_FILE)) File.Delete(NikkeDataHelper.NAME_DATA_FILE);
            NikkeDataHelper.Initialize();

            if (File.Exists(NikkeDataService.STATE_FILE)) File.Delete(NikkeDataService.STATE_FILE);

            ResetDirectory(ModDirectory);
            ResetDirectory(CacheDirectory);
            ResetDirectory(GameDirectory);

            CopyFilesRecursively(SourceModDirectory, ModDirectory);
            CopyFilesRecursively(SourceGameDirectory, GameDirectory);
        }

        protected static void ResetDirectory(string path) {
            if (Directory.Exists(path)) Directory.Delete(path, true);
            Directory.CreateDirectory(path);
        }

        protected static void CopyFilesRecursively(string sourcePath, string targetPath) {
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories)) {
                Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
            }
            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories)) {
                File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
            }
        }
    }
}
