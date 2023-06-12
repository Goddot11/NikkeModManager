using FMOD;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NikkeModManagerCore {
    public class ModCollector {

        public static string DefaultGameMod { get; } = "Default";

        public List<NikkeMod> CollectMods(string cacheDir, string modDir, string gameDir) {
            Stopwatch sw = Stopwatch.StartNew();
            Console.WriteLine($"Searching for mods in {modDir}");
            if (!Directory.Exists(cacheDir)) {
                Directory.CreateDirectory(cacheDir);
                Console.WriteLine($"Creating cache directory at {cacheDir}");
            } else {
                Console.WriteLine($"Loading from cache directory at {Path.Join(Directory.GetCurrentDirectory(), cacheDir)}");
            }

            string defaultModDirectory = Path.Join(modDir, DefaultGameMod);
            if (!Directory.Exists(defaultModDirectory)) {
                Console.WriteLine($"Creating Default mod from game data");
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
            Console.WriteLine($"Found {mods.SelectMany(q => q.Bundles).Count()} bundles in {mods.Count} mods in {sw.ElapsedMilliseconds}ms");

            // Check that the default game mod has valid mappings to existing game files
            NikkeMod defaultMod = mods.FirstOrDefault(mod => mod.Name == DefaultGameMod);
            if (ValidateDefaultMod(defaultMod, gameDir)) {
                Console.WriteLine("Successfully Validated all file mappings");
            } else {
                Console.WriteLine("Default mod has invalid or missing mappings, rebuilding");
                mods.Remove(defaultMod);
                BuildDefaultMod(cacheDir, defaultModDirectory, gameDir);
                NikkeMod mod = new NikkeDirectoryMod(defaultModDirectory);
                mod.Load(cacheDir);
                mods.Add(mod);
            }

            sw.Restart();
            Console.WriteLine("Exporting bundle cache files");
            mods.ForEach(mod => mod.ExportCache(cacheDir));
            Console.WriteLine($"Exported cache files in {sw.ElapsedMilliseconds}ms");
            return mods;
        }

        private bool ValidateDefaultMod(NikkeMod defaultMod, string gameDir) {
            Console.WriteLine("Validating Default mod");
            bool valid = true;

            if (defaultMod.Bundles.Count == 0) {
                valid = false;
                Console.WriteLine("Default mod contains no bundles");
            }

            foreach (NikkeBundle bundle in defaultMod.Bundles) {
                string cachedFilename = NikkeDataHelper.GetFilename(bundle.FileIdentifier);
                if (cachedFilename == "") {
                    valid = false;
                    Console.WriteLine($"No cached filename for {bundle.FileIdentifier} - {bundle.FileName}");
                }
                if (cachedFilename != bundle.FileName) {
                    valid = false;
                    Console.WriteLine($"Found File mismatch for {bundle.FileIdentifier} between {cachedFilename} in cache and {bundle.FileName} in mod");
                }
                if(!File.Exists(Path.Join(gameDir, cachedFilename))) {
                    valid = false;
                    Console.WriteLine($"Could not find filename for {bundle.FileIdentifier} - {cachedFilename} in \"{gameDir}\"");
                }
            }

            return valid;
        }

        private void BuildDefaultMod(string cacheDir, string defaultModDirectory, string gameDir) {
            NikkeMod mod = new NikkeDirectoryMod(gameDir);
            mod.Load(cacheDir);

            if (Directory.Exists(defaultModDirectory)) Directory.Delete(defaultModDirectory, true);
            Directory.CreateDirectory(defaultModDirectory);

            foreach (NikkeBundle bundle in mod.Bundles) {
                bundle.ExportEncrypted(defaultModDirectory);
                NikkeDataHelper.SetFilename(bundle.FileIdentifier, bundle.FileName);
            }

            NikkeDataHelper.SaveData();
            Console.WriteLine("Default mod built");
        }
    }
}
