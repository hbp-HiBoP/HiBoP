using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using HBP.Core.Tools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace HBP.Transfer.Scene
{
    /// <summary>Content-addressed files and numeric buffers; the wire never contains native handles.</summary>
    public sealed partial class SceneArchive : IDisposable
    {
        public const long MaximumExpandedBytes = 4L * 1024 * 1024 * 1024;
        public const int MaximumMetadataBytes = 128 * 1024 * 1024;
        private readonly string directory;
        private readonly bool deferResourceWrites;
        private readonly CancellationToken cancellationToken;
        private readonly Dictionary<string, byte[]> capturedBuffers = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string> capturedFiles = new(StringComparer.Ordinal);
        private long capturedBufferBytes;
        private long numericBytesRead;
        private CancellationToken readToken;
        private bool readingStarted, disposed, verifiedContent;
        private ReceivedSurfaceCache surfaceCache;
        private long surfaceCacheGeneration;

        public ReceivedSurfaceCache SurfaceCache
        {
            get => surfaceCache;
            set
            {
                surfaceCache = value;
                surfaceCacheGeneration = value?.Generation ?? 0;
            }
        }

        private readonly VerifiedResourceScope verifiedResources;
        private BufferPack pack = new();
        private bool packedBuffers;
        internal bool DetachCapture { get; set; }

        private readonly List<SurfaceCapture> detachedSurfaces = new();
        internal int ContentVersion => packedBuffers ? ScenePayload.FormatVersion : ScenePayload.LegacyFormatVersion;
        internal SceneGlobalReferences GlobalReferences { get; private set; }

        private readonly Dictionary<string, string> nativePaths = new(StringComparer.Ordinal);
        public string DirectoryPath => directory;
        public string VerifiedContentHash { get; private set; }
        public PreparedSceneManifest PreparedManifest { get; private set; }
        public PairingContext Globals { get; set; }

        public SceneArchive(string directory, bool reading = false, PairingContext globals = null, bool deferResourceWrites = false, CancellationToken cancellationToken = default)
        {
            this.directory = Path.GetFullPath(directory);
            Globals = globals;
            this.deferResourceWrites = deferResourceWrites;
            this.cancellationToken = cancellationToken;
            Directory.CreateDirectory(directory);
            verifiedResources = new VerifiedResourceScope(() => pack.Retire(() =>
            {
                if (Directory.Exists(this.directory))
                    Directory.Delete(this.directory, true);
            }));
        }

        public string AddFile(string path, string expectedHash, string companionHash = null)
        {
            EnsureWritable();
            if (string.IsNullOrEmpty(expectedHash))
                throw new InvalidOperationException($"Missing loaded resource provenance: {path}");
            string extension = path.EndsWith(".nii.gz", StringComparison.OrdinalIgnoreCase) ? ".nii.gz" : Path.GetExtension(path).ToLowerInvariant();
            string name = expectedHash.Replace("-", "").ToLowerInvariant() + extension;
            AddResourceFile(path, name);
            if (StandardData.CompanionFile(path) is string companion)
            {
                if (companionHash == null)
                    throw new InvalidOperationException("Missing NIfTI companion provenance.");
                string otherName = companionHash.Replace("-", "").ToLowerInvariant() + Path.GetExtension(companion).ToLowerInvariant();
                AddResourceFile(companion, otherName);
                // The pair, not either member alone, identifies a native image. Equal voxel
                // bytes can have different headers (and equal headers different voxel bytes).
                return AddBuffer(writer => writer.Write(Encoding.UTF8.GetBytes(name + "\n" + otherName)), ".pair");
            }

            return name;
        }

        public string AddBuffer(Action<BinaryWriter> write, string extension = ".bin")
        {
            EnsureWritable();
            cancellationToken.ThrowIfCancellationRequested();
            using var memory = new MemoryStream();
            using (var writer = new BinaryWriter(memory, Encoding.UTF8, true))
                write(writer);
            return AddOwnedBuffer(memory.ToArray(), extension);
        }

        private string AddOwnedBuffer(byte[] bytes, string extension = ".bin")
        {
            EnsureWritable();
            cancellationToken.ThrowIfCancellationRequested();
            using var sha = HBP.Transfer.Codecs.TransferCodec.CreateHash();
            string name = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant() + extension;
            if (extension == ".bin" && !pack.Contains(name))
                pack.Add(name, bytes.LongLength);
            string target = Resolve(name);
            if (deferResourceWrites)
            {
                if (!capturedBuffers.ContainsKey(name))
                {
                    capturedBufferBytes = checked(capturedBufferBytes + bytes.LongLength);
                    if (capturedBufferBytes > MaximumExpandedBytes)
                        throw new InvalidDataException("Visualization exceeds the expanded content budget.");
                    capturedBuffers.Add(name, bytes);
                }
            }
            else if (!File.Exists(target))
                File.WriteAllBytes(target, bytes);

            return name;
        }

        private void AddResourceFile(string path, string name)
        {
            EnsureWritable();
            cancellationToken.ThrowIfCancellationRequested();
            string target = Resolve(name);
            if (deferResourceWrites)
            {
                capturedFiles.TryAdd(name, path);
                return;
            }

            if (!File.Exists(target))
                File.Copy(path, target);
            if (StandardData.HashFile(target) != name.Substring(0, 64))
                throw new IOException("A source resource changed since it was loaded: " + path);
        }

        /// <summary>Freeze the live managed graph and numeric arrays before leaving Unity's thread.</summary>
        public byte[] CaptureMetadata(ScenePayload payload)
        {
            EnsureWritable();
            if (payload.Version != ScenePayload.FormatVersion)
                throw new InvalidOperationException("Capture requires the current scene format.");
            packedBuffers = true;
            if (!deferResourceWrites)
                throw new InvalidOperationException("Deferred resource capture is required.");
            if (Globals == null || payload.GlobalContextId != Globals.Id)
                throw new InvalidOperationException("Pair with Quest before capturing a visualization.");
            PrepareGlobalReferences();
            cancellationToken.ThrowIfCancellationRequested();
            using var memory = new MemoryStream();
            using (var writer = new JsonTextWriter(new StreamWriter(memory, new UTF8Encoding(false), 8192, true)))
                Serializer().Serialize(writer, payload);
            if (memory.Length > MaximumMetadataBytes)
                throw new InvalidDataException("Visualization metadata exceeds the transfer budget.");
            return memory.ToArray();
        }

        internal DetachedSceneMetadata CaptureDetachedMetadata(ScenePayload payload)
        {
            EnsureWritable();
            if (!DetachCapture || !deferResourceWrites || payload.Version != ScenePayload.FormatVersion || Globals == null || payload.GlobalContextId != Globals.Id)
                throw new InvalidOperationException("Detached capture requires the current format and pairing.");
            packedBuffers = true;
            PrepareGlobalReferences();
            var writer = new DetachedSceneMetadata();
            Serializer().Serialize(writer, payload);
            return writer;
        }

        internal int RegisterCapturedNumericBuffer(byte[] bytes) => pack.IndexOf(AddOwnedBuffer(bytes));

        internal void ResolveCapturedSurfaces(JToken metadata)
        {
            string[] names = new string[detachedSurfaces.Count];
            for (int i = 0; i < names.Length; i++)
                names[i] = WriteSurface(detachedSurfaces[i]);
            foreach (var mesh in (JArray)metadata["Meshes"])
            foreach (string field in SurfaceFields)
            {
                string value = (string)mesh[field];
                if (value == null)
                    continue;
                if (pack.Contains(value))
                    continue;
                if (!value.StartsWith("snapshot-surface-", StringComparison.Ordinal) || !int.TryParse(value.Substring(17), out int index) || index < 0 || index >= names.Length)
                    throw new InvalidDataException("Invalid detached surface reference.");
                mesh[field] = names[index];
            }

            detachedSurfaces.Clear();
        }

        private static readonly string[] SurfaceFields =
        {
            "Both",
            "Left",
            "Right",
            "SimplifiedBoth",
            "SimplifiedLeft",
            "SimplifiedRight",
            "InflatedBoth",
            "InflatedLeft",
            "InflatedRight",
            "InflatedSimplifiedBoth",
            "InflatedSimplifiedLeft",
            "InflatedSimplifiedRight"
        };

        /// <summary>Encode only owned buffers and provenance-checked source files; no live Unity/model access.</summary>
        public void WriteCaptured(byte[] metadata, string output)
        {
            EnsureWritable();
            if (!deferResourceWrites)
                throw new InvalidOperationException("Deferred resource capture is required.");
            cancellationToken.ThrowIfCancellationRequested();
            WritePacked(metadata, output);
        }

        private void WritePacked(byte[] metadata, string output)
        {
            foreach (string path in Directory.EnumerateFiles(directory))
                if (Path.GetFileName(path) != "visualization.json" && !path.EndsWith(".bin", StringComparison.Ordinal))
                    capturedFiles.TryAdd(Path.GetFileName(path), path);
            byte[] references = GlobalReferences.Encode();
            long indexBytes = 12L + pack.Entries.Count * 48L;
            long metadataBytes = metadata.LongLength + references.LongLength + indexBytes;
            var extraBuffers = capturedBuffers.Where(item => !item.Key.EndsWith(".bin", StringComparison.Ordinal)).ToArray();
            var files = capturedFiles.Where(item => !capturedBuffers.ContainsKey(item.Key)).OrderBy(item => item.Key, StringComparer.Ordinal).ToArray();
            long expanded = checked(metadataBytes + pack.Length + extraBuffers.Sum(item => item.Value.LongLength) + files.Sum(item => new FileInfo(item.Value).Length));
            if (metadataBytes > MaximumMetadataBytes || expanded > MaximumExpandedBytes || pack.Entries.Count + files.Length + extraBuffers.Length + 4 > 100000)
                throw new InvalidDataException("Packed visualization exceeds its budget.");
            var zip = ZipFile.Open(output, ZipArchiveMode.Create);
            try
            {
                byte[] buffer = new byte[65536];
                using (var input = new MemoryStream(metadata, false))
                    WriteCapturedEntry(zip, "visualization.json", input, false, buffer);
                using (var input = new MemoryStream(references, false))
                    WriteCapturedEntry(zip, SceneGlobalReferences.FileName, input, false, buffer);
                using (var index = zip.CreateEntry(BufferPack.IndexName, CompressionLevel.Fastest).Open())
                    pack.WriteIndex(index);
                using (var target = zip.CreateEntry(BufferPack.DataName, CompressionLevel.Fastest).Open())
                {
                    int used = 0;
                    foreach (var entry in pack.Entries)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        using Stream input = capturedBuffers.TryGetValue(entry.Name, out byte[] bytes) ? new MemoryStream(bytes, false) : File.OpenRead(Resolve(entry.Name));
                        long remaining = entry.Length;
                        while (remaining > 0)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            int read = input.Read(buffer, used, (int)Math.Min(buffer.Length - used, remaining));
                            if (read == 0)
                                throw new InvalidDataException("Truncated captured buffer.");
                            used += read;
                            remaining -= read;
                            if (used == buffer.Length)
                            {
                                target.Write(buffer, 0, used);
                                used = 0;
                            }
                        }

                        if (input.ReadByte() != -1)
                            throw new InvalidDataException("Captured buffer size changed.");
                    }

                    if (used != 0)
                        target.Write(buffer, 0, used);
                }

                foreach (var item in extraBuffers)
                {
                    using var input = new MemoryStream(item.Value, false);
                    WriteCapturedEntry(zip, item.Key, input, false, buffer);
                }

                foreach (var item in files)
                {
                    using var input = File.OpenRead(item.Value);
                    WriteCapturedEntry(zip, item.Key, input, true, buffer);
                }
            }
            finally
            {
                zip.Dispose();
            }
        }

        private void WriteCapturedEntry(ZipArchive zip, string name, Stream source, bool verifyHash, byte[] buffer)
        {
            Stream target;
            target = zip.CreateEntry(name, CompressionLevel.Fastest).Open();
            try
            {
                using var sha = verifyHash ? HBP.Transfer.Codecs.TransferCodec.CreateHash() : null;
                long remaining = source.Length;
                while (remaining > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    int read;
                    read = source.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining));
                    if (read == 0)
                        throw new IOException("A source resource changed during capture: " + name);
                    if (sha != null)
                        sha.TransformBlock(buffer, 0, read, buffer, 0);
                    target.Write(buffer, 0, read);
                    remaining -= read;
                }

                if (source.ReadByte() != -1)
                    throw new IOException("A source resource changed during capture: " + name);
                if (sha != null)
                {
                    sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                    if (BitConverter.ToString(sha.Hash).Replace("-", "").ToLowerInvariant() != name.Substring(0, 64))
                        throw new IOException("A source resource changed since it was loaded: " + name);
                }
            }
            finally
            {
                target.Dispose();
            }
        }

        /// <summary>Illustrations are optional; unavailable source files must not block their definitions.</summary>
        public string AddIllustration(string path)
        {
            EnsureWritable();
            if (!File.Exists(path))
                return "";
            string extension = Path.GetExtension(path).ToLowerInvariant();
            if (extension.Length < 4 || extension.Length > 16 || extension.Substring(1).Any(c => !char.IsLetter(c) && c != '.'))
                return "";
            FileStream source;
            try
            {
                source = File.OpenRead(path);
            }
            catch (IOException)
            {
                return "";
            }
            catch (UnauthorizedAccessException)
            {
                return "";
            }

            string temporary = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".tmp");
            using (source)
                try
                {
                    using (var output = File.Create(temporary))
                    {
                        byte[] buffer = new byte[81920];
                        while (true)
                        {
                            int count;
                            try
                            {
                                count = source.Read(buffer, 0, buffer.Length);
                            }
                            catch (IOException)
                            {
                                return "";
                            }

                            if (count == 0)
                                break;
                            output.Write(buffer, 0, count);
                        }
                    }

                    string name = StandardData.HashFile(temporary) + extension;
                    string target = Resolve(name);
                    if (!File.Exists(target))
                        File.Move(temporary, target);
                    return name;
                }
                finally
                {
                    if (File.Exists(temporary))
                        File.Delete(temporary);
                }
        }

        public string Resolve(string name)
        {
            if (name == null || name.Length < 68 || name.Length > 80 || name.Take(64).Any(c => !"0123456789abcdef".Contains(c)) || name[64] != '.' || name.Substring(65).Any(c => !char.IsLetter(c) && c != '.')) throw new InvalidDataException("Invalid content resource identity.");
            return StandardData.Resolve(directory, name);
        }

        private void PrepareGlobalReferences()
        {
            // Keep indices append-only: previously captured metadata can still be written.
            GlobalReferences ??= new SceneGlobalReferences(Globals);
            if (GlobalReferences.ContextId != Globals.Id)
                throw new InvalidOperationException("Use a new archive after changing pairing.");
        }

        private sealed class SceneMetadataReader : JsonTextReader
        {
            private bool versionSeen;

            internal SceneMetadataReader(TextReader reader) : base(reader)
            {
                MaxDepth = 128;
            }

            public override bool Read()
            {
                bool read = base.Read();
                if (read && Depth == 1 && TokenType == JsonToken.PropertyName && (string)Value == "Version")
                {
                    if (versionSeen)
                        throw new InvalidDataException("Duplicate scene version.");
                    versionSeen = true;
                }

                return read;
            }
        }

        private JsonSerializer Serializer(bool globalData = false)
        {
            var serializer = PreparedDataJson.Create(this, globalData ? null : Globals, !globalData && packedBuffers);
            serializer.Converters.Add(new NumericBufferConverter(this));
            return serializer;
        }

        public void Write(ScenePayload payload, string output)
        {
            if (Globals == null || payload.GlobalContextId != Globals.Id)
                throw new InvalidOperationException("Pair with Quest before capturing a visualization.");
            packedBuffers = payload.Version != ScenePayload.LegacyFormatVersion;
            if (packedBuffers)
                PrepareGlobalReferences();
            if (!packedBuffers)
            {
                WritePackage(payload, output, "visualization.json", false);
                return;
            }

            EnsureWritable();
            using var metadata = new MemoryStream();
            using (var writer = new JsonTextWriter(new StreamWriter(metadata, new UTF8Encoding(false), 8192, true)))
                Serializer().Serialize(writer, payload);
            WritePacked(metadata.ToArray(), output);
        }

        public void WriteGlobalData(GlobalDataPayload payload, string output) => WritePackage(payload, output, "globals.json", true);

        private void WritePackage(object payload, string output, string metadataName, bool globalData)
        {
            EnsureWritable();
            string metadata = Path.Combine(directory, metadataName);
            using (var writer = new JsonTextWriter(new StreamWriter(metadata, false, new UTF8Encoding(false))))
                Serializer(globalData).Serialize(writer, payload);
            if (new FileInfo(metadata).Length > MaximumMetadataBytes)
                throw new InvalidDataException("Visualization metadata exceeds the transfer budget.");
            if (Directory.EnumerateFiles(directory).Sum(path => new FileInfo(path).Length) > MaximumExpandedBytes)
                throw new InvalidDataException("Visualization exceeds the expanded content budget.");
            using var zip = ZipFile.Open(output, ZipArchiveMode.Create);
            foreach (string path in Directory.EnumerateFiles(directory).OrderBy(p => p, StringComparer.Ordinal))
                zip.CreateEntryFromFile(path, Path.GetFileName(path), CompressionLevel.Fastest);
        }

        public string[] ReadNativePair(string name)
        {
            string path = Resolve(name);
            if (!name.EndsWith(".pair", StringComparison.Ordinal) || new FileInfo(path).Length > 256) throw new InvalidDataException("Invalid native image pair.");
            string[] members = File.ReadAllText(path, Encoding.UTF8).Split('\n');
            if (members.Length != 2) throw new InvalidDataException("Incomplete native image pair.");
            string extension = Path.GetExtension(members[0]);
            if ((extension != ".img" && extension != ".hdr") || Path.GetExtension(members[1]) != (extension == ".img" ? ".hdr" : ".img")) throw new InvalidDataException("Invalid native image pair extensions.");
            foreach (string member in members)
                if (!File.Exists(Resolve(member)))
                    throw new InvalidDataException("Missing native image pair member.");
            return members;
        }

        public string ResolveNativeFile(string name)
        {
            if (!name.EndsWith(".pair", StringComparison.Ordinal)) return Resolve(name);
            string[] members = ReadNativePair(name);
            string folder = Path.Combine(directory, "native", name.Substring(0, 64));
            Directory.CreateDirectory(folder);
            string target = Path.Combine(folder, "volume" + Path.GetExtension(members[0]));
            if (!File.Exists(target)) File.Copy(Resolve(members[0]), target);
            string otherTarget = StandardData.CompanionFile(target);
            if (!File.Exists(otherTarget)) File.Copy(Resolve(members[1]), otherTarget);
            return target;
        }

        public ScenePayload Read(string input, System.Threading.CancellationToken token = default)
        {
            if (Globals == null)
                throw new InvalidOperationException("Global pairing data is required before receiving a visualization.");
            using var source = new FileStream(input, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var hash = HBP.Transfer.Codecs.TransferCodec.CreateHash();
            string contentHash = BitConverter.ToString(hash.ComputeHash(source)).Replace("-", "").ToLowerInvariant();
            source.Position = 0;
            Extract(source, "visualization.json", token);
            ScenePayload payload = ReadPrepared();
            VerifiedContentHash = contentHash;
            return payload;
        }

        public ScenePayload ReadPrepared(string verifiedContentHash = null)
        {
            if (!verifiedContent || Globals == null)
                throw new InvalidOperationException("Verified paired content is required.");
            readToken.ThrowIfCancellationRequested();
            if (packedBuffers)
                GlobalReferences = SceneGlobalReferences.Read(Path.Combine(directory, SceneGlobalReferences.FileName), Globals);
            using var reader = new SceneMetadataReader(File.OpenText(Path.Combine(directory, "visualization.json")));
            ScenePayload payload;
            payload = Serializer().Deserialize<ScenePayload>(reader);
            if (reader.Read())
                throw new InvalidDataException("Trailing scene metadata.");
            if (payload?.GlobalContextId != Globals.Id)
                throw new InvalidDataException("This visualization belongs to a different pairing. Send a new snapshot.");
            SceneValidation.Validate(payload, this);
            PreparedManifest = PreparedSceneManifest.FromMetadataFile(Path.Combine(directory, "visualization.json"));
            if (verifiedContentHash != null)
            {
                if (verifiedContentHash.Length != 64 || verifiedContentHash.Any(character => character < '0' || character > '9' && character < 'a' || character > 'f'))
                    throw new InvalidDataException("Invalid verified delivery hash.");
                VerifiedContentHash = verifiedContentHash;
            }

            return payload;
        }

        public GlobalDataPayload ReadGlobalData(string input, System.Threading.CancellationToken token = default)
        {
            using var source = new FileStream(input, FileMode.Open, FileAccess.Read, FileShare.Read);
            Extract(source, "globals.json", token);
            return LoadGlobalData();
        }

        internal GlobalDataPayload LoadGlobalData()
        {
            using var reader = new JsonTextReader(File.OpenText(Path.Combine(directory, "globals.json"))) { MaxDepth = 128 };
            return Serializer(true).Deserialize<GlobalDataPayload>(reader);
        }

        private void Extract(Stream input, string metadataName, System.Threading.CancellationToken token)
        {
            EnsureWritable();
            if (Directory.EnumerateFileSystemEntries(directory).Any())
                throw new InvalidOperationException("Extraction requires an empty private workspace.");
            readingStarted = true;
            readToken = token;
            byte[] buffer = new byte[65536];
            using var sha = HBP.Transfer.Codecs.TransferCodec.CreateHash();
            using (var zip = new ZipArchive(input, ZipArchiveMode.Read, true))
            {
                long total = 0;
                var names = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
                foreach (var entry in zip.Entries)
                {
                    token.ThrowIfCancellationRequested();
                    if (names.Count >= 100000)
                        throw new InvalidDataException("Too many content resources.");
                    if (!names.Add(entry.FullName))
                        throw new InvalidDataException("Duplicate archive entry.");
                    bool special = entry.FullName == BufferPack.DataName || entry.FullName == BufferPack.IndexName || entry.FullName == SceneGlobalReferences.FileName;
                    if (entry.FullName != metadataName && !special)
                        Resolve(entry.FullName);
                    else if (entry.FullName != BufferPack.DataName && entry.Length > MaximumMetadataBytes)
                        throw new InvalidDataException("Visualization metadata exceeds the transfer budget.");
                    if (special && metadataName != "visualization.json")
                        throw new InvalidDataException("Scene resources are not valid pairing data.");
                    total = checked(total + entry.Length);
                    if (total > MaximumExpandedBytes || entry.Length < 0)
                        throw new InvalidDataException("Visualization exceeds the expanded content budget.");
                }

                if (!names.Contains(metadataName))
                    throw new InvalidDataException("Missing visualization metadata.");
                if (metadataName == "visualization.json")
                {
                    int version;
                    using (var metadata = zip.GetEntry(metadataName).Open())
                        version = ReadSceneVersion(metadata);
                    packedBuffers = version == ScenePayload.FormatVersion;
                    if (version != ScenePayload.FormatVersion && version != ScenePayload.LegacyFormatVersion)
                        throw new InvalidDataException("Unknown visualization format. Rebuild Desktop and Quest together.");
                    bool hasPack = names.Contains(BufferPack.DataName), hasIndex = names.Contains(BufferPack.IndexName), hasRefs = names.Contains(SceneGlobalReferences.FileName);
                    if (packedBuffers ? !hasPack || !hasIndex || !hasRefs || names.Any(name => name.EndsWith(".bin", StringComparison.Ordinal)) : hasPack || hasIndex || hasRefs)
                        throw new InvalidDataException("Missing or mixed scene container resources.");
                    if (packedBuffers)
                    {
                        var index = zip.GetEntry(BufferPack.IndexName);
                        if (zip.GetEntry(metadataName).Length + index.Length + zip.GetEntry(SceneGlobalReferences.FileName).Length > MaximumMetadataBytes)
                            throw new InvalidDataException("Scene metadata exceeds its combined budget.");
                        using (var stream = index.Open())
                            pack = BufferPack.ReadIndex(stream, index.Length, zip.GetEntry(BufferPack.DataName).Length, token);
                        if (pack.Entries.Count + names.Count > 100000)
                            throw new InvalidDataException("Too many logical resources.");
                    }
                }

                foreach (var entry in zip.Entries)
                {
                    string path = StandardData.Resolve(directory, entry.FullName);
                    bool isPack = entry.FullName == BufferPack.DataName;
                    bool verify = entry.FullName != metadataName && !isPack && entry.FullName != BufferPack.IndexName && entry.FullName != SceneGlobalReferences.FileName;
                    using var packVerifier = isPack ? new BufferPack.Verifier(pack) : null;
                    if (verify)
                        sha.Initialize();
                    using (var source = entry.Open())
                    using (var target = File.Create(path))
                    {
                        long remaining = entry.Length;
                        while (remaining > 0)
                        {
                            token.ThrowIfCancellationRequested();
                            int read;
                            read = source.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining));
                            if (read == 0)
                                throw new InvalidDataException("Truncated content resource.");
                            if (verify)
                                sha.TransformBlock(buffer, 0, read, null, 0);
                            if (packVerifier != null)
                                packVerifier.Append(buffer, read, token);
                            target.Write(buffer, 0, read);
                            remaining -= read;
                        }

                        if (source.ReadByte() != -1)
                            throw new InvalidDataException("Expanded resource size mismatch.");
                    }

                    packVerifier?.Complete();
                    if (verify)
                    {
                        sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                        string hash = BitConverter.ToString(sha.Hash).Replace("-", "").ToLowerInvariant();
                        if (hash != entry.FullName.Substring(0, 64))
                            throw new InvalidDataException("Resource checksum mismatch.");
                        if (!entry.FullName.EndsWith(".bin", StringComparison.Ordinal) && !entry.FullName.EndsWith(".pair", StringComparison.Ordinal))
                        {
                            verifiedResources.Register(path, hash);
                            nativePaths.Add(entry.FullName, path);
                        }
                    }
                }

                foreach (string name in names.Where(name => name.EndsWith(".pair", StringComparison.Ordinal)))
                {
                    token.ThrowIfCancellationRequested();
                    string[] members = ReadNativePair(name);
                    string path = ResolveNativeFile(name);
                    verifiedResources.Register(path, members[0].Substring(0, 64));
                    verifiedResources.Register(StandardData.CompanionFile(path), members[1].Substring(0, 64));
                    nativePaths.Add(name, path);
                }

                token.ThrowIfCancellationRequested();
                if (packedBuffers)
                {
                    pack.Attach(Path.Combine(directory, BufferPack.DataName));
                }

                verifiedResources.Seal();
                verifiedContent = true;
            }
        }

        private static int ReadSceneVersion(Stream stream)
        {
            using var reader = new JsonTextReader(new StreamReader(stream))
            {
                MaxDepth = 128
            };
            if (!reader.Read() || reader.TokenType != JsonToken.StartObject)
                throw new InvalidDataException("Invalid scene metadata.");
            while (reader.Read() && reader.TokenType != JsonToken.EndObject)
            {
                if (reader.TokenType != JsonToken.PropertyName)
                    throw new InvalidDataException("Invalid scene property.");
                string name = (string)reader.Value;
                if (!reader.Read())
                    throw new InvalidDataException("Truncated scene metadata.");
                if (name == "Version")
                {
                    if (reader.TokenType != JsonToken.Integer || reader.Value is not long version || version < 0 || version > int.MaxValue)
                        throw new InvalidDataException("Invalid scene version.");
                    return (int)version;
                }

                reader.Skip();
            }

            throw new InvalidDataException("Missing scene version.");
        }

        internal bool ContainsResource(string name) => name != null && (packedBuffers && pack.Contains(name) || File.Exists(Resolve(name)));

        internal Stream OpenBuffer(string name)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(SceneArchive));
            readToken.ThrowIfCancellationRequested();
            if (packedBuffers)
            {
                return pack.Open(name);
            }

            return File.OpenRead(Resolve(name));
        }

        internal VerifiedResourceScope.Lease AcquireNativeResource(string name)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(SceneArchive));
            if (!nativePaths.TryGetValue(name, out string path))
                throw new InvalidOperationException("Native resource has not been verified.");
            return verifiedResources.Acquire(path, StandardData.CompanionFile(path));
        }

        private void EnsureWritable()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(SceneArchive));
            if (readingStarted)
                throw new InvalidOperationException("Received archive contents are immutable.");
        }

        public string AddSurface(HBP.Core.DLL.Surface surface)
        {
            EnsureWritable();
            cancellationToken.ThrowIfCancellationRequested();
            var snapshot = new SurfaceCapture(surface);
            if (!DetachCapture)
                return WriteSurface(snapshot);
            if (detachedSurfaces.Count >= 100000)
                throw new InvalidDataException("Too many captured surfaces.");
            detachedSurfaces.Add(snapshot);
            return "snapshot-surface-" + (detachedSurfaces.Count - 1);
        }

        private string WriteSurface(SurfaceCapture snapshot)
        {
            return AddBuffer(writer => WriteSurfaceData(writer, snapshot));
        }

        private static void WriteGeometryData(BinaryWriter writer, SurfaceCapture snapshot)
        {
            var geometry = snapshot.Data;
            writer.Write(geometry.Vertices.Length);
            writer.Write(geometry.Triangles.Length);
            WriteComponents(writer, geometry.Vertices);
            WriteComponents(writer, geometry.Triangles);
            writer.Write(geometry.Normals.Length);
            WriteComponents(writer, geometry.Normals);
            writer.Write(geometry.UV.Length);
            WriteComponents(writer, geometry.UV);
            writer.Write(geometry.Colors.Length);
            WriteComponents(writer, geometry.Colors);
        }

        private static void WriteSurfaceData(BinaryWriter writer, SurfaceCapture snapshot)
        {
            WriteGeometryData(writer, snapshot);
            writer.Write(snapshot.Atlas);
            writer.Write(snapshot.Mask.Length);
            WriteComponents(writer, snapshot.Mask);
        }

        // Unity mesh structs contain packed 32-bit components, as used by the native mesh copier.
        private static void WriteComponents<T>(BinaryWriter writer, T[] values) where T : struct
        {
            var bytes = MemoryMarshal.AsBytes(values.AsSpan());
            if (BitConverter.IsLittleEndian) writer.Write(bytes);
            else
            {
                byte[] copy = bytes.ToArray();
                for (int i = 0; i < copy.Length; i += 4) Array.Reverse(copy, i, 4);
                writer.Write(copy);
            }
        }

        public HBP.Core.DLL.Surface ReadSurface(string name)
        {
            Stream input = OpenBuffer(name);
            using var reader = new BinaryReader(verifiedContent && SurfaceCache != null ? SurfaceCache.Open(name, input, readToken, surfaceCacheGeneration) : input);
            int vertexCount = ReadCount(reader, 12), triangleCount = ReadCount(reader, 4);
            if (vertexCount == 0 || triangleCount % 3 != 0)
                throw new InvalidDataException("Invalid surface dimensions.");
            var vertices = ReadComponents<Vector3>(reader, vertexCount, true);
            var triangles = ReadComponents<int>(reader, triangleCount);
            for (int i = 0; i < triangleCount; i++)
            {
                if (triangles[i] < 0 || triangles[i] >= vertexCount)
                    throw new InvalidDataException("Triangle index outside surface.");
            }

            int count = ReadCount(reader, 12);
            if (count != 0 && count != vertexCount)
                throw new InvalidDataException("Invalid normals.");
            var normals = ReadComponents<Vector3>(reader, count, true);
            count = ReadCount(reader, 8);
            if (count != 0 && count != vertexCount)
                throw new InvalidDataException("Invalid UVs.");
            var uv = ReadComponents<Vector2>(reader, count, true);
            count = ReadCount(reader, 16);
            if (count != 0 && count != vertexCount)
                throw new InvalidDataException("Invalid colors.");
            var colors = ReadComponents<Color>(reader, count, true);
            bool atlasAvailable = reader.ReadBoolean();
            count = ReadCount(reader, 4);
            if (count != triangleCount / 3)
                throw new InvalidDataException("Invalid surface visibility mask.");
            var mask = ReadComponents<int>(reader, count);
            for (int i = 0; i < count; i++)
            {
                if (mask[i] != 0 && mask[i] != 1)
                    throw new InvalidDataException("Invalid surface visibility value.");
            }

            if (reader.BaseStream.Position != reader.BaseStream.Length)
                throw new InvalidDataException("Trailing surface data.");
            var surface = new HBP.Core.DLL.Surface();
            try
            {
                surface.SetBuffers(vertices, triangles, normals.Length == 0 ? null : normals, uv.Length == 0 ? null : uv, colors.Length == 0 ? null : colors);
                surface.SetPreparedAtlasAvailability(atlasAvailable);
                surface.UpdateVisibilityMask(mask).Dispose();
                return surface;
            }
            catch
            {
                surface.Dispose();
                throw;
            }
        }

        private static int ReadCount(BinaryReader reader, int stride)
        {
            int count = reader.ReadInt32();
            if (count < 0 || (long)count * stride > reader.BaseStream.Length - reader.BaseStream.Position) throw new InvalidDataException("Invalid buffer dimensions.");
            return count;
        }

        private T[] ReadComponents<T>(BinaryReader reader, int count, bool finite = false) where T : struct
        {
            readToken.ThrowIfCancellationRequested();
            var values = new T[count];
            var bytes = MemoryMarshal.AsBytes(values.AsSpan());
            for (int offset = 0; offset < bytes.Length;)
            {
                readToken.ThrowIfCancellationRequested();
                int read = reader.BaseStream.Read(bytes.Slice(offset, Math.Min(65536, bytes.Length - offset)));
                if (read == 0)
                    throw new EndOfStreamException("Truncated numeric buffer.");
                offset += read;
            }

            if (!BitConverter.IsLittleEndian)
                for (int i = 0; i < bytes.Length; i += 4)
                {
                    byte a = bytes[i], b = bytes[i + 1];
                    bytes[i] = bytes[i + 3];
                    bytes[i + 1] = bytes[i + 2];
                    bytes[i + 2] = b;
                    bytes[i + 3] = a;
                }

            if (finite)
            {
                var components = MemoryMarshal.Cast<byte, float>(bytes);
                for (int i = 0; i < components.Length; i++)
                {
                    if ((i & 16383) == 0)
                        readToken.ThrowIfCancellationRequested();
                    if (float.IsNaN(components[i]) || float.IsInfinity(components[i]))
                        throw new InvalidDataException("Nonfinite geometry.");
                }
            }

            return values;
        }

        public void Dispose()
        {
            disposed = true;
            capturedBuffers.Clear();
            capturedFiles.Clear();
            detachedSurfaces.Clear();
            verifiedResources.Dispose();
        }

        private sealed class NumericBufferConverter : JsonConverter
        {
            private readonly SceneArchive archive;

            public NumericBufferConverter(SceneArchive archive)
            {
                this.archive = archive;
            }

            public override bool CanConvert(Type type) => type == typeof(float[]) || type == typeof(int[]);

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                if (value == null)
                {
                    writer.WriteNull();
                    return;
                }

                // Own the bytes before asynchronous encoding; a source scene may change or close.
                archive.cancellationToken.ThrowIfCancellationRequested();
                var values = (Array)value;
                byte[] bytes = new byte[checked(values.Length * 4)];
                Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
                if (!BitConverter.IsLittleEndian)
                    for (int i = 0; i < bytes.Length; i += 4)
                        Array.Reverse(bytes, i, 4);
                if (writer is DetachedSceneMetadata detached)
                {
                    detached.WriteBuffer(bytes);
                    return;
                }

                string name = archive.AddOwnedBuffer(bytes);
                if (archive.packedBuffers)
                    writer.WriteValue(archive.pack.IndexOf(name));
                else
                    writer.WriteValue(name);
            }

            public override object ReadJson(JsonReader reader, Type type, object existing, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Null)
                    return null;
                Stream file;
                archive.readToken.ThrowIfCancellationRequested();
                if (archive.packedBuffers)
                {
                    if (reader.TokenType != JsonToken.Integer || reader.Value is not long index || index < 0 || index >= archive.pack.Entries.Count)
                        throw new InvalidDataException("Invalid numeric pack index.");
                    file = archive.pack.Open((int)index);
                }
                else
                {
                    if (reader.TokenType != JsonToken.String)
                        throw new InvalidDataException("Expected a legacy numeric resource.");
                    file = File.OpenRead(archive.Resolve((string)reader.Value));
                }

                using var binary = new BinaryReader(file);
                if (binary.BaseStream.Length % 4 != 0 || binary.BaseStream.Length > int.MaxValue)
                    throw new InvalidDataException("Invalid numeric buffer.");
                archive.numericBytesRead = checked(archive.numericBytesRead + binary.BaseStream.Length);
                if (archive.numericBytesRead > MaximumExpandedBytes)
                    throw new InvalidDataException("Decoded numeric data exceeds the memory budget.");
                int length = checked((int)(binary.BaseStream.Length / 4));
                if (type == typeof(float[]))
                {
                    return archive.ReadComponents<float>(binary, length);
                }
                else
                {
                    return archive.ReadComponents<int>(binary, length);
                }
            }
        }
    }
}
