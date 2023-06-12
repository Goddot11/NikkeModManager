using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AssetStudio;
using ProtoBuf;
using SixLabors.ImageSharp.Formats.Png;

namespace NikkeModManagerCore;

public class NikkeBundle {
    public string FileName { get; }
    public string RelativePath { get; }
    public Stream ContentsStream { get; }
    public bool LoadedFromCache { get; private set; } = false;
    public event Action OnLoadComplete = () => { };

    public string Name { get => NikkeDataHelper.GetName(CharacterId); }
    public string CharacterId { get; private set; } = "";
    public int SkinKey { get; private set; }
    public string Pose { get; private set; } = "";
    public List<string> Animations { get; } = new List<string>();
    public string DefaultAnimation { get => _defaultAnimations.ContainsKey(Pose) ? _defaultAnimations[Pose] : ""; }
    public string FileIdentifier { get => $"{CharacterId}_{SkinKey}_{Pose}"; }

    public Stream AtlasFile { get => new MemoryStream(_atlasData); }
    public Stream AtlasTexture { get => new MemoryStream(_textureData); }
    public Stream SkeletonFile { get => new MemoryStream(_skeletonData);}

    private byte[] _atlasData { get; set; }
    private byte[] _textureData { get; set; }
    private byte[] _skeletonData { get; set; }

    private readonly Dictionary<string, string> _defaultAnimations = new Dictionary<string, string>() {
        { "idle", "idle" },
        { "cover", "cover_idle" },
        { "aim", "aim_idle" },
    };

    public NikkeBundle(string relativePath, Stream contentsStream, string modCacheDirectory) {
        FileName = Path.GetFileName(relativePath);
        RelativePath = relativePath;
        ContentsStream = new MemoryStream();
        contentsStream.CopyTo(ContentsStream);
        ContentsStream.Seek(0, SeekOrigin.Begin);
        UnpackBundle(modCacheDirectory);
    }

    public string GetCacheFile(string modCacheDirectory) => Path.Join(modCacheDirectory, RelativePath + ".cache");

    public void NotifyLoaded(IEnumerable<string> animations) {
        Logger.WriteLine($"=== Successfully Loaded {FileName} - {CharacterId} ===");
        Animations.AddRange(animations);
    }

    private readonly Regex _atlasNameMatch = new Regex("^([0-9a-z]+)_(?:([a-z]+)_)?([0-9]+).atlas");

    void UnpackBundle(string modCacheDirectory) {
        Stopwatch stopwatch = Stopwatch.StartNew();
        string cachePath = GetCacheFile(modCacheDirectory);
        if (File.Exists(cachePath)) {
            using FileStream file = File.OpenRead(cachePath);
            BundleCache cache = Serializer.Deserialize<BundleCache>(file);
            cache.LoadBundle(this);
            LoadedFromCache = true;
            //Logger.WriteLine($"Loaded {CharacterId} {Pose} {SkinKey} from cache in {stopwatch.ElapsedMilliseconds}ms");
            return;
        }

        Stream decryptedContentsStream = NikkeTools.DecryptBundle(ContentsStream);
        ContentsStream.Seek(0, SeekOrigin.Begin);

        AssetsManager manager = new AssetsManager();
        manager.LoadStream(decryptedContentsStream);

        SerializedFile assetFile = manager.assetsFileList.FirstOrDefault();
        if (assetFile == null) 
            throw new Exception($"Unable to load bundle {FileName}: Failed to load asset file");
        if (assetFile.m_TargetPlatform != BuildTarget.StandaloneWindows64) throw new NotWindowsException(assetFile.m_TargetPlatform.ToString());
        foreach (AssetStudio.Object asset in assetFile.Objects.Where(q => q != null)) {
            MemoryStream stream = new MemoryStream();
            switch (asset) {
                case TextAsset mTextAsset:
                    if (mTextAsset.m_Name.Contains(".skel")) {
                        stream.Write(mTextAsset.m_Script);
                        _skeletonData = stream.ToArray();
                    } else if (mTextAsset.m_Name.Contains(".atlas")) {
                        Match match = _atlasNameMatch.Match(mTextAsset.m_Name);
                        if (!match.Success) throw new NotSkinException();
                        CharacterId = match.Groups[1].Value;
                        Pose = match.Groups[2].Success ? match.Groups[2].Value : "idle";
                        SkinKey = int.Parse(match.Groups[3].Value);
                        if (NikkeDataHelper.ShouldSkip(CharacterId)) throw new NotSkinException("Listed as non-nikke file");

                        stream.Write(mTextAsset.m_Script);
                        _atlasData = stream.ToArray();
                    }
                    break;
                case Texture2D mTexture2D:
                    var texture = mTexture2D.ConvertToImage(true);
                    texture.Save(stream, new PngEncoder());
                    _textureData = stream.ToArray();
                    break;
            }

            stream.Dispose();
        }

        if (_atlasData == null) throw new NotSkinException($"Unable to find atlas file");
        if (_textureData == null) throw new NotSkinException($"Unable to find atlas texture");
        if (_skeletonData == null) throw new NotSkinException($"Unable to find skeleton file");
        //Logger.WriteLine($"Unpacked {CharacterId} {Pose} {SkinKey} in {stopwatch.ElapsedMilliseconds}ms");
    }

    public void ExportToCache(Stream output) {
        Stopwatch stopwatch = Stopwatch.StartNew();
        BundleCache cache = new BundleCache(this);
        Serializer.Serialize(output, cache);
        //Logger.WriteLine($"Exported {CharacterId} {Pose} {SkinKey} to cache in {stopwatch.ElapsedMilliseconds}ms");
    }

    public void ExportToCache(string modCacheDirectory) {
        string cachePath = GetCacheFile(modCacheDirectory);
        string cacheDirectory = Path.GetDirectoryName(cachePath);
        if(!Directory.Exists(cacheDirectory)) Directory.CreateDirectory(cacheDirectory);
        if(File.Exists(cachePath)) File.Delete(cachePath);
        using FileStream file = File.OpenWrite(cachePath);
        ExportToCache(file);
    }

    public void ExportEncrypted(Stream exportStream) {
        ContentsStream.CopyTo(exportStream);
        ContentsStream.Seek(0, SeekOrigin.Begin);
    }
    public void ExportEncrypted(string directoryPath, bool overwrite=false) {
        string path = Path.Join(directoryPath, FileName);
        if (File.Exists(path)) {
            if (overwrite) File.Delete(path);
            else {
                Logger.WriteLine($"Cannot export bundle file {FileName} to \"{path}\": File already exists");
                return;
            }
        }
        using FileStream file = File.Open(path, FileMode.Create);
        Logger.WriteLine($"Successfully exported {path}");
        ExportEncrypted(file);
    }

    [ProtoContract]
    private class BundleCache {
        [ProtoMember(1)]
        string _characterId { get; set; }
        [ProtoMember(2)]
        string _pose { get; set; }
        [ProtoMember(3)]
        int _skinKey { get; set; }

        [ProtoMember(4)]
        byte[] _atlasData { get; set; }
        [ProtoMember(5)]
        byte[] _textureData { get; set; }
        [ProtoMember(6)]
        byte[] _skeletonData { get; set; }

        protected BundleCache(){}

        public BundleCache(NikkeBundle bundle) {
            _characterId = bundle.CharacterId;
            _pose = bundle.Pose;
            _skinKey = bundle.SkinKey;
            _atlasData = bundle._atlasData;
            _textureData = bundle._textureData;
            _skeletonData = bundle._skeletonData;
        }

        public void LoadBundle(NikkeBundle bundle) {
            bundle.CharacterId = _characterId;
            bundle.Pose = _pose;
            bundle.SkinKey = _skinKey;
            bundle._atlasData = _atlasData;
            bundle._textureData = _textureData;
            bundle._skeletonData = _skeletonData;
        }
    }
}

internal class NotSkinException : Exception {
    public NotSkinException() { }
    public NotSkinException(string message) : base(message) { }
}

internal class NotWindowsException : Exception {
    public string TargetPlatform;

    public NotWindowsException(string targetPlatform) : base(targetPlatform) {
        TargetPlatform = targetPlatform;
    }
}