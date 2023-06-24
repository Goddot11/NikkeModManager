using NikkeModManagerCore.Exceptions;
using SpirV;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace NikkeModManagerCore {
    public class NikkeDataService {
        public const string STATE_FILE = "_state";

        public event Action DataUpdated = () => Console.WriteLine("Data Loaded");
        public event Action<NikkeBundle, bool> BundleEnabled = (bundle, status) => Console.WriteLine($"{(status ? "Enabled":"Disabled")} {bundle.FileIdentifier}");
        public event Action<List<NikkeBundle>> PatchComplete = (lst) => Console.WriteLine($"Successfully patched {lst.Count} bundles");
        public event Action<string> Error = _ => { };

        public bool IsLoading { get; private set; }

        private List<NikkeBundle> _installedBundles { get; set; }
        private Dictionary<NikkeBundle, NikkeBundle> _swappedBundles { get; set; } = new Dictionary<NikkeBundle, NikkeBundle>();
        private List<NikkeBundle> _enabledBundles { get; set; }

        private List<NikkeMod> _mods { get; set; } = new List<NikkeMod>();
        private List<NikkeBundle> _bundles { get => _mods.SelectMany(q => q.Bundles).ToList(); }
        private Dictionary<string, List<NikkeBundle>> _nikkeBundle { get => _bundles.GroupBy(q => q.CharacterId).ToDictionary(q => q.Key, q => q.ToList()); }

        private NikkeMod _defaultMod { get => _mods.First(mod => mod.Name == ModCollector.DefaultGameMod); }

        public NikkeDataService() {
            NikkeConfig.LoadConfig("_config");
        }

        /// <summary>
        /// Start a task to load all <see cref="NikkeMod"/>s in the background. <see cref="DataUpdated"/> Will be called when the load is complete
        /// </summary>
        /// <returns></returns>
        public Task LoadData() {
            IsLoading = true;
            Task task = new Task(() => {
                try {
                    Load();
                } catch (GameDataNotFoundException ex) {
                    Console.WriteLine($"Unable to load mods:\n{ex}");
                    Error(ex.Message);

                    IsLoading = false;
                    DataUpdated();
                } catch (Exception ex) {
                    Console.WriteLine($"Unable to load mods:\n{ex}");
                    Error(ex.ToString());

                    IsLoading = false;
                    DataUpdated();
                }
            });
            task.Start();
            return task;
        }

        /// <summary>
        /// Reload all mod data
        /// </summary>
        public void ReloadAllData() => LoadData();

        protected void Load() {
            NikkeDataHelper.Initialize();

            _mods = new ModCollector()
                .CollectMods(
                    NikkeConfig.CacheDirectory,
                    NikkeConfig.ModDirectory,
                    NikkeConfig.GameDataDirectory);

            LoadInstalledData();

            IsLoading = false;
            DataUpdated();
        }

        public bool ValidateDataPath() => Directory.Exists(NikkeConfig.GameDataDirectory);

        /// <summary>
        /// </summary>
        /// <returns>All loaded <see cref="NikkeMod"/>s</returns>
        public List<NikkeMod> GetMods() => new List<NikkeMod>(_mods);
        /// <summary>
        /// </summary>
        /// <returns>All loaded <see cref="NikkeBundle"/>s</returns>
        public List<NikkeBundle> GetBundles() => new List<NikkeBundle>(_bundles);
        /// <summary>
        /// </summary>
        /// <param name="bundle"></param>
        /// <returns>The <see cref="NikkeMod"/> that owns the specified <see cref="NikkeBundle"/></returns>
        public NikkeMod GetBundleSource(NikkeBundle bundle) => _mods.First(mod => mod.Bundles.Contains(bundle));
        /// <summary>
        /// </summary>
        /// <returns>A list of all distinct <see cref="NikkeBundle.CharacterId"/>s in all loaded bundles</returns>
        public List<string> GetNikkesIds() => _bundles.Select(q => q.CharacterId).Distinct().ToList();
        /// <summary>
        /// </summary>
        /// <param name="bundle"></param>
        /// <returns>The unique identifier of the specified <see cref="NikkeBundle"/>, formatted as "{<see cref="NikkeMod.Name"/>}\<see cref="NikkeBundle.RelativePath"/>"</returns>
        public string GetBundleUniqueIdentifier(NikkeBundle bundle) => Path.Join(GetBundleSource(bundle).Name, bundle.RelativePath);

        /// <summary>
        /// </summary>
        /// <param name="id"></param>
        /// <returns>All nikke bundles that with a given <see cref="NikkeBundle.CharacterId"/></returns>
        public List<NikkeBundle> GetNikkeBundles(string id) => _nikkeBundle[id];
        /// <summary>
        /// </summary>
        /// <param name="id"></param>
        /// <returns>All <see cref="NikkeBundle.SkinKey"/>s for a given <see cref="NikkeBundle.CharacterId"/> </returns>
        public List<int> GetNikkeSkins(string id) => GetNikkeBundles(id).Select(q => q.SkinKey).Distinct().OrderBy(q => q).ToList();
        /// <summary>
        /// Will most likely be ["idle", "cover", "aim"]
        /// </summary>
        /// <param name="id"></param>
        /// <param name="skin"></param>
        /// <returns>All <see cref="NikkeBundle.Pose"/>s for a given <see cref="NikkeBundle.CharacterId"/> and <see cref="NikkeBundle.SkinKey"/></returns>
        public List<string> GetNikkeSkinPoses(string id, int skin) => GetNikkeBundles(id).Where(q => q.SkinKey == skin).Select(q => q.Pose).Distinct().OrderBy(q => q).ToList();
        /// <summary>
        /// </summary>
        /// <param name="id"></param>
        /// <param name="skin"></param>
        /// <param name="pose"></param>
        /// <returns>All <see cref="NikkeBundle"/>s with the specified set of <see cref="NikkeBundle.CharacterId"/>, <see cref="NikkeBundle.SkinKey"/>, and <see cref="NikkeBundle.Pose"/></returns>
        public List<NikkeBundle> GetNikkeSkinPoseBundles(string id, int skin, string pose) => GetNikkeBundles(id).Where(q => q.SkinKey == skin && q.Pose == pose).ToList();

        /// <summary>
        /// </summary>
        /// <param name="bundle"></param>
        /// <returns>Whether or not the specified <see cref="NikkeBundle"/> is marked to be installed</returns>
        public bool IsEnabled(NikkeBundle bundle) => _enabledBundles.Contains(bundle);
        /// <summary>
        /// </summary>
        /// <returns>A list of all <see cref="NikkeBundle"/> that are marked to be either installed or uninstalled</returns>
        public List<NikkeBundle> GetChangedBundles() => _enabledBundles.Where(b => !_installedBundles.Contains(b)).ToList();
        /// <summary>
        /// </summary>
        /// <returns>A list of all <see cref="NikkeBundle"/> that are marked to be installed</returns>
        public List<NikkeBundle> GetEnabledBundles() => _enabledBundles.ToList();
        /// <summary>
        /// Mark the specified bundle to be installed with the next patch. Clears all other bundles with the same <see cref="NikkeBundle.FileIdentifier"/>
        /// </summary>
        /// <param name="bundle"></param>
        public void EnableBundle(NikkeBundle bundle) {
            NikkeBundle swapped = _enabledBundles.FirstOrDefault(b => b.FileIdentifier == bundle.FileIdentifier);
            if (swapped == null) {
                Error($"Mod doesn't have a valid default value: {bundle.FileIdentifier} - {GetBundleUniqueIdentifier(bundle)}");
                return;
            }
            _enabledBundles.Remove(swapped);
            BundleEnabled(swapped, false);
            _swappedBundles[bundle] = swapped;
            _enabledBundles.Add(bundle);
            BundleEnabled(bundle, true);
        }

        /// <summary>
        /// Install all marked <see cref="NikkeBundle"/> into the game directory, overwriting existing files.
        /// If a file with the same name is not present in the game directory then either the filename map is out of date or the mod was created incorrectly.
        /// </summary>
        public void PatchGame() {
            if (!ValidateDataPath()) throw new GameDataNotFoundException($"Unable to find Nikke Game Data in `{NikkeConfig.GameDataDirectory}`");

            List<NikkeBundle> toInstall = GetChangedBundles();
            Console.WriteLine($"Patching game with {toInstall.Count} mods\n\t" + string.Join("\n\t", toInstall.Select(GetBundleUniqueIdentifier)));

            List<NikkeBundle> failed = new List<NikkeBundle>();
            foreach (NikkeBundle bundle in toInstall.ToList()) {
                string filename = NikkeDataHelper.GetFilename(bundle.FileIdentifier);
                string path = Path.Join(NikkeConfig.GameDataDirectory, filename);
                if (!File.Exists(path)) {
                    failed.Add(bundle);
                    toInstall.Remove(bundle);
                    _enabledBundles.Remove(bundle);
                    _enabledBundles.Add(_swappedBundles[bundle]);
                }
            }

            if (failed.Count > 0) {
                string message =$"The following mod files don't have corresponding game data files to overwrite. This means either they were made with an invalid character id/skin id/pose or the filename map is out of date.\n\t";
                message += string.Join("\n\t", failed.Select(q => $"{q.FileIdentifier} {GetBundleUniqueIdentifier(q)}"));
                Error(message);
            }

            foreach (NikkeBundle bundle in toInstall.ToList()) {
                string filename = NikkeDataHelper.GetFilename(bundle.FileIdentifier);
                string path = Path.Join(NikkeConfig.GameDataDirectory, filename);
                byte[] backupData = File.ReadAllBytes(path);
                try {
                    File.Delete(path);
                    using FileStream file = File.OpenWrite(path);
                    bundle.ExportEncrypted(file);
                } catch (Exception e) {
                    if(!File.Exists(path))
                        File.WriteAllBytes(path, backupData);
                    Error($"Unable to write mod file \"{bundle.FileName}\": {e}");
                    toInstall.Remove(bundle);
                    _enabledBundles.Remove(bundle);
                    _enabledBundles.Add(_swappedBundles[bundle]);
                }
            }

            _installedBundles = new List<NikkeBundle>(_enabledBundles);
            _swappedBundles = new Dictionary<NikkeBundle, NikkeBundle>();
            SaveInstalledData();
            PatchComplete(toInstall);
        }

        /// <summary>
        /// Delete the global cache directory
        /// </summary>
        public void DeleteAllCaches() {
            _mods.ForEach(mod => mod.DeleteCache(NikkeConfig.CacheDirectory));
        }

        /// <summary>
        /// Delete the Default game <see cref="NikkeMod"/>, which contains all base game data
        /// </summary>
        public void DeleteDefaultGameMod() {
            _defaultMod.DeleteMod(NikkeConfig.CacheDirectory);
        }

        /// <summary>
        /// Delete all asset bundle files in the game data directory that contain Nikke data. Used to make it easier to redownload game files.
        /// </summary>
        public void DeleteGameNikkeBundles() {
            if (_defaultMod == null) return;
            Console.WriteLine($"Deleting {_defaultMod.Bundles.Count} bundle files from {NikkeConfig.GameDataDirectory}");
            foreach (NikkeBundle bundle in _defaultMod.Bundles) {
                string path = Path.Join(NikkeConfig.GameDataDirectory, bundle.RelativePath);
                if (File.Exists(path)) {
                    File.Delete(path);
                    Console.WriteLine($"File deleted {bundle.RelativePath}");
                } else {
                    Console.WriteLine($"File not found in game directory {bundle.RelativePath}");
                }
            }

            _defaultMod.DeleteMod(NikkeConfig.CacheDirectory);
            _mods.Remove(_defaultMod);
            DataUpdated();
        }

        private void LoadInstalledData() {
            if (File.Exists(STATE_FILE)) {
                string[] data = File.ReadAllLines(STATE_FILE);
                _installedBundles = data.Select(id => _bundles.FirstOrDefault(q => GetBundleUniqueIdentifier(q) == id)).Where(q => q != null).ToList();
            } else {
                _installedBundles = _defaultMod.Bundles;
            }

            foreach (NikkeBundle bundle in _defaultMod.Bundles) {
                if (_installedBundles.All(q => q.FileIdentifier != bundle.FileIdentifier)) {
                    _installedBundles.Add(bundle);
                }
            }

            _enabledBundles = _installedBundles.ToList();
            _swappedBundles = new Dictionary<NikkeBundle, NikkeBundle>();
        }

        private void SaveInstalledData() {
            File.WriteAllLines(STATE_FILE, _installedBundles.Select(GetBundleUniqueIdentifier));
        }
    }
}
