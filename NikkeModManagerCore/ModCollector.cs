using FMOD;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NikkeModManagerCore.Exceptions;

namespace NikkeModManagerCore {
    public class ModCollector {

        public static string DefaultGameMod { get; } = "Default";

        public List<NikkeMod> CollectMods(string cacheDir, string modDir, string gameDir) {
            Stopwatch sw = Stopwatch.StartNew();
            Logger.WriteLine($"Searching for mods in {modDir}");
            if (!Directory.Exists(cacheDir)) {
                Directory.CreateDirectory(cacheDir);
                Logger.WriteLine($"Creating cache directory at {cacheDir}");
            } else {
                Logger.WriteLine($"Loading from cache directory at {Path.Join(Directory.GetCurrentDirectory(), cacheDir)}");
            }

            string defaultModDirectory = Path.Join(modDir, DefaultGameMod);
            if (!Directory.Exists(defaultModDirectory)) {
                Logger.WriteLine($"Creating Default mod from game data");
                BuildDefaultMod(cacheDir, defaultModDirectory, gameDir);
            }

            List<NikkeMod> mods = new List<NikkeMod>();
            if (!Directory.Exists(modDir)) Directory.CreateDirectory(modDir);
            foreach (string path in Directory.GetFiles(modDir, "*.zip")) {
                string absolutePath = Path.Combine(Directory.GetCurrentDirectory(), path);
                mods.Add(new NikkeZipMod(absolutePath));
            }
            foreach (string path in Directory.GetDirectories(modDir)) {
                string absolutePath = Path.Combine(Directory.GetCurrentDirectory(), path);
                mods.Add(new NikkeDirectoryMod(absolutePath));
            }

            mods.ForEach(mod => mod.Load(cacheDir));
            Logger.WriteLine($"Found {mods.SelectMany(q => q.Bundles).Count()} bundles in {mods.Count} mods in {sw.ElapsedMilliseconds}ms");

            // Check that the default game mod has valid mappings to existing game files
            NikkeMod defaultMod = mods.FirstOrDefault(mod => mod.Name == DefaultGameMod);
            if (ValidateDefaultMod(defaultMod, gameDir)) {
                Logger.WriteLine("Successfully Validated all file mappings");
            } else {
                Logger.WriteLine("Default mod has invalid or missing mappings, rebuilding");
                mods.Remove(defaultMod);
                BuildDefaultMod(cacheDir, defaultModDirectory, gameDir);
                NikkeMod mod = new NikkeDirectoryMod(defaultModDirectory);
                mod.Load(cacheDir);
                mods.Add(mod);
            }

            sw.Restart();
            Logger.WriteLine("Exporting bundle cache files");
            mods.ForEach(mod => mod.ExportCache(cacheDir));
            Logger.WriteLine($"Exported cache files in {sw.ElapsedMilliseconds}ms");
            return mods;
        }

        private bool ValidateDefaultMod(NikkeMod defaultMod, string gameDir) {
            Logger.WriteLine("Validating Default mod");
            bool valid = true;

            if (defaultMod.Bundles.Count == 0) {
                valid = false;
                Logger.WriteLine("Default mod contains no bundles");
            }

            foreach (NikkeBundle bundle in defaultMod.Bundles) {
                string cachedFilename = NikkeDataHelper.GetFilename(bundle.FileIdentifier);
                if (cachedFilename == "") {
                    valid = false;
                    Logger.WriteLine($"No cached filename for {bundle.FileIdentifier} - {bundle.FileName}");
                }
                if (cachedFilename != bundle.FileName) {
                    valid = false;
                    Logger.WriteLine($"Found File mismatch for {bundle.FileIdentifier} between {cachedFilename} in cache and {bundle.FileName} in mod");
                }
                if(!File.Exists(Path.Join(gameDir, cachedFilename))) {
                    valid = false;
                    Logger.WriteLine($"Could not find filename for {bundle.FileIdentifier} - {cachedFilename} in \"{gameDir}\"");
                }
            }

            return valid;
        }

        private void BuildDefaultMod(string cacheDir, string defaultModDirectory, string gameDir) {
            NikkeMod mod = new NikkeDirectoryMod(gameDir);
            mod.Load(cacheDir);

            if (mod.Bundles.Count == 0) {
                throw new GameDataNotFoundException("Game Data Directory contains no asset bundles.");
            }

            if (Directory.Exists(defaultModDirectory)) Directory.Delete(defaultModDirectory, true);
            Directory.CreateDirectory(defaultModDirectory);

            foreach (NikkeBundle bundle in mod.Bundles) {
                bundle.ExportEncrypted(defaultModDirectory);
                NikkeDataHelper.SetFilename(bundle.FileIdentifier, bundle.FileName);
            }

            NikkeDataHelper.SaveData();
            Logger.WriteLine("Default mod built");
        }
    }
}
