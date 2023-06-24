using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NikkeModManagerCore.Exceptions;

namespace NikkeModManagerCore;

public abstract class NikkeMod {
    public string Name { get; }
    public string ModPath { get; }
    public List<NikkeBundle> Bundles { get => _bundles.ToList(); }
    public int FailedBundles{ get; protected set; }
    public ModManifest Manifest { get; protected set; } = new ModManifest();

    protected readonly Regex BundleRegex = new Regex("[0-9a-z]{32}(?:\\s|$)");
    public const string MANIFEST_FILE = "manifest.json";

    private List<NikkeBundle> _bundles { get; } = new List<NikkeBundle>();

    protected string GetCacheDir(string cachesDirectory) => Path.Join(cachesDirectory, Name);

    /// <summary>
    /// Call <see cref="Load(string)"/> to build the mods <see cref="NikkeBundle"/>s
    /// </summary>
    /// <param name="modPath">Path to the mod from the applications directory</param>
    protected NikkeMod(string modPath) {
        Name = Path.GetFileNameWithoutExtension(modPath);
        ModPath = modPath;
    }

    /// <summary>
    /// Reads and builds all <see cref="NikkeBundle"/>s. Will prefer .cache files in cachesDirectory over asset bundle files/>
    /// </summary>
    /// <param name="cachesDirectory">Path to the applications Cache Directory</param>
    public void Load(string cachesDirectory) {
        Dictionary<string, Stream> bundleStreams = LoadData(ModPath);
        List<Task> tasks = new List<Task>();
        foreach (var pair in bundleStreams) {
            Task task = LoadBundle(pair.Key, pair.Value, cachesDirectory);
            if (NikkeConfig.UseMultiThreading) task.Start();
            else task.RunSynchronously();
            tasks.Add(task);
        }
        Task.WaitAll(tasks.ToArray());
    }

    protected abstract Dictionary<string, Stream> LoadData(string modPath);

    private Task LoadBundle(string filePath, Stream stream, string cachesDirectory) {
        return new Task(() => {
            try {
                _bundles.Add(new NikkeBundle(filePath, stream, GetCacheDir(cachesDirectory)));
            } catch (NotSkinException ex) {
                Console.WriteLine($"Skipping {filePath} in {Name}: {ex.Message}");
            } catch (NotWindowsException ex) {
                Console.WriteLine($"Skipping {filePath} in {Name}: Not for windows, detected platform {ex.TargetPlatform}");
            } catch (Exception ex) {
                Console.WriteLine($"Failed to build bundle {filePath} in {Name}:\n{ex}");
                FailedBundles++;
            } finally {
                stream.Dispose();
            }
        });
    }

    protected void LoadManifest(Stream stream) {
        try {
            Manifest = JsonSerializer.Deserialize<ModManifest>(stream);
        } catch (Exception ex) {
            Console.WriteLine($"Unable to read mod manifest for {Name}:\n{ex}");
        }
    }

    /// <summary>
    /// Export a .cache file to the specified directory for each loaded <see cref="NikkeBundle"/>.
    /// </summary>
    /// <param name="cachesDirectory">Path to the applications Cache Directory</param>
    public void ExportCache(string cachesDirectory) {
        string cacheDirectory = GetCacheDir(cachesDirectory);
        if (!Directory.Exists(cacheDirectory)) Directory.CreateDirectory(cacheDirectory);
        foreach (NikkeBundle bundle in _bundles) {
            bundle.ExportToCache(cacheDirectory);
        }
    }

    /// <summary>
    /// Delete this mods cache directory
    /// </summary>
    /// <param name="cachesDirectory">Path to the applications Cache Directory</param>
    public void DeleteCache(string cachesDirectory) {
        if(Directory.Exists(GetCacheDir(cachesDirectory)))
            Directory.Delete(GetCacheDir(cachesDirectory), true);
    }

    /// <summary>
    /// Delete this mods cache directory, then the mods directory/file
    /// </summary>
    /// <param name="cachesDirectory">Path to the applications Cache Directory</param>
    public void DeleteMod(string cachesDirectory) {
        DeleteCache(cachesDirectory);
        if (File.Exists(ModPath)) {
            File.Delete(ModPath);
        } else {
            Directory.Delete(ModPath, true);
        }
    }
}

public class NikkeDirectoryMod : NikkeMod {
    public NikkeDirectoryMod(string modPath) : base(modPath) { }

    protected override Dictionary<string, Stream> LoadData(string modPath) {
        Console.WriteLine($"Loading Directory Mod {ModPath}");
        Dictionary<string, Stream> files = new Dictionary<string, Stream>();
        foreach (string filePath in Directory.GetFiles(modPath, "*", SearchOption.AllDirectories)) {
            string relativePath = Path.GetRelativePath(modPath, filePath);
            if (BundleRegex.IsMatch(Path.GetFileName(filePath))) {
                files[relativePath] = File.Open(filePath, FileMode.Open);
            }
            if (Path.GetFileName(filePath) == MANIFEST_FILE) {
                using FileStream stream = File.OpenRead(filePath);
                LoadManifest(stream);
            }
        }
        return files;
    }
}

public class NikkeZipMod : NikkeMod {
    public NikkeZipMod(string modPath) : base(modPath) { }

    protected override Dictionary<string, Stream> LoadData(string modPath) {
        Console.WriteLine($"Loading Zip Mod {ModPath}");
        Dictionary<string, Stream> files = new Dictionary<string, Stream>();
        using FileStream file = File.OpenRead(modPath);
        ZipArchive archive = new ZipArchive(file);
        foreach (ZipArchiveEntry entry in archive.Entries) {
            if (BundleRegex.IsMatch(entry.Name)) {
                using Stream zipStream = entry.Open();
                MemoryStream memoryStream = new MemoryStream();
                zipStream.CopyTo(memoryStream);
                memoryStream.Seek(0, SeekOrigin.Begin);
                files[entry.FullName] = memoryStream;
            }
            if (entry.Name == MANIFEST_FILE) {
                using Stream zipStream = entry.Open();
                MemoryStream memoryStream = new MemoryStream();
                zipStream.CopyTo(memoryStream);
                memoryStream.Seek(0, SeekOrigin.Begin);
                LoadManifest(memoryStream);
            }
        }

        return files;
    }
}