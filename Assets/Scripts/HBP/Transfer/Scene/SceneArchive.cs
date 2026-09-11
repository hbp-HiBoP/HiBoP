using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using HBP.Core.Tools;
using Newtonsoft.Json;
using UnityEngine;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace HBP.Transfer.Scene
{
    /// <summary>Content-addressed files and numeric buffers; the wire never contains native handles.</summary>
    public sealed class SceneArchive : IDisposable
    {
        public const long MaximumExpandedBytes = 4L * 1024 * 1024 * 1024;
        public const int MaximumMetadataBytes = 128 * 1024 * 1024;
        private readonly string directory;
        private long numericBytesRead;
        public string DirectoryPath => directory;
        public PairingContext Globals { get; set; }

        public SceneArchive(string directory, bool reading = false, PairingContext globals = null)
        {
            this.directory = Path.GetFullPath(directory);
            Globals = globals;

            Directory.CreateDirectory(directory);
        }

        public string AddFile(string path, string expectedHash, string companionHash = null)
        {
            if (string.IsNullOrEmpty(expectedHash)) throw new InvalidOperationException($"Missing loaded resource provenance: {path}");
            string extension = path.EndsWith(".nii.gz", StringComparison.OrdinalIgnoreCase) ? ".nii.gz" : Path.GetExtension(path).ToLowerInvariant();
            string name = expectedHash.Replace("-", "").ToLowerInvariant() + extension;
            string target = Resolve(name);
            if (!File.Exists(target))
            {
                File.Copy(path, target);
                if (StandardData.HashFile(target) != name.Substring(0, 64)) throw new IOException("A source resource changed since it was loaded: " + path);
            }

            if (StandardData.CompanionFile(path) is string companion)
            {
                if (companionHash == null) throw new InvalidOperationException("Missing NIfTI companion provenance.");
                string otherName = companionHash.Replace("-", "").ToLowerInvariant() + Path.GetExtension(companion).ToLowerInvariant();
                string otherPath = Resolve(otherName);
                if (!File.Exists(otherPath)) File.Copy(companion, otherPath);
                if (StandardData.HashFile(otherPath) != otherName.Substring(0, 64)) throw new IOException("NIfTI companion changed since loading.");
                // The pair, not either member alone, identifies a native image. Equal voxel
                // bytes can have different headers (and equal headers different voxel bytes).
                return AddBuffer(writer => writer.Write(Encoding.UTF8.GetBytes(name + "\n" + otherName)), ".pair");
            }

            return name;
        }

        public string AddBuffer(Action<BinaryWriter> write, string extension = ".bin")
        {
            string temporary = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".tmp");
            using (var writer = new BinaryWriter(File.Create(temporary))) write(writer);
            string name = StandardData.HashFile(temporary) + extension;
            string target = Resolve(name);
            if (File.Exists(target)) File.Delete(temporary);
            else File.Move(temporary, target);
            return name;
        }

        /// <summary>Illustrations are optional; unavailable source files must not block their definitions.</summary>
        public string AddIllustration(string path)
        {
            if (!File.Exists(path)) return "";
            string extension = Path.GetExtension(path).ToLowerInvariant();
            if (extension.Length < 4 || extension.Length > 16 || extension.Substring(1).Any(c => !char.IsLetter(c) && c != '.')) return "";
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

                            if (count == 0) break;
                            output.Write(buffer, 0, count);
                        }
                    }

                    string name = StandardData.HashFile(temporary) + extension;
                    string target = Resolve(name);
                    if (!File.Exists(target)) File.Move(temporary, target);
                    return name;
                }
                finally
                {
                    if (File.Exists(temporary)) File.Delete(temporary);
                }
        }

        public string Resolve(string name)
        {
            if (name == null || name.Length < 68 || name.Length > 80 || name.Take(64).Any(c => !"0123456789abcdef".Contains(c)) || name[64] != '.' || name.Substring(65).Any(c => !char.IsLetter(c) && c != '.')) throw new InvalidDataException("Invalid content resource identity.");
            return StandardData.Resolve(directory, name);
        }

        private JsonSerializer Serializer(bool globalData = false)
        {
            var serializer = PreparedDataJson.Create(this, globalData ? null : Globals);
            serializer.Converters.Add(new NumericBufferConverter(this));
            return serializer;
        }

        public void Write(ScenePayload payload, string output)
        {
            if (Globals == null || payload.GlobalContextId != Globals.Id) throw new InvalidOperationException("Pair with Quest before capturing a visualization.");
            WritePackage(payload, output, "visualization.json", false);
        }

        public void WriteGlobalData(GlobalDataPayload payload, string output) => WritePackage(payload, output, "globals.json", true);

        private void WritePackage(object payload, string output, string metadataName, bool globalData)
        {
            string metadata = Path.Combine(directory, metadataName);
            using (var writer = new JsonTextWriter(new StreamWriter(metadata, false, new UTF8Encoding(false)))) Serializer(globalData).Serialize(writer, payload);
            if (new FileInfo(metadata).Length > MaximumMetadataBytes) throw new InvalidDataException("Visualization metadata exceeds the transfer budget.");
            if (Directory.EnumerateFiles(directory).Sum(path => new FileInfo(path).Length) > MaximumExpandedBytes) throw new InvalidDataException("Visualization exceeds the expanded content budget.");
            using var zip = ZipFile.Open(output, ZipArchiveMode.Create);
            foreach (string path in Directory.EnumerateFiles(directory).OrderBy(p => p, StringComparer.Ordinal)) zip.CreateEntryFromFile(path, Path.GetFileName(path), CompressionLevel.Fastest);
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
            if (Globals == null) throw new InvalidOperationException("Global pairing data is required before receiving a visualization.");
            Extract(input, "visualization.json", token);
            using var reader = new JsonTextReader(File.OpenText(Path.Combine(directory, "visualization.json"))) { MaxDepth = 128 };
            var payload = Serializer().Deserialize<ScenePayload>(reader);
            if (payload?.GlobalContextId != Globals.Id) throw new InvalidDataException("This visualization belongs to a different pairing. Send a new snapshot.");
            SceneValidation.Validate(payload, this);
            return payload;
        }

        public GlobalDataPayload ReadGlobalData(string input, System.Threading.CancellationToken token = default)
        {
            Extract(input, "globals.json", token);
            return LoadGlobalData();
        }

        internal GlobalDataPayload LoadGlobalData()
        {
            using var reader = new JsonTextReader(File.OpenText(Path.Combine(directory, "globals.json"))) { MaxDepth = 128 };
            return Serializer(true).Deserialize<GlobalDataPayload>(reader);
        }

        private void Extract(string input, string metadataName, System.Threading.CancellationToken token)
        {
            using (var zip = ZipFile.OpenRead(input))
            {
                long total = 0;
                var names = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
                foreach (var entry in zip.Entries)
                {
                    token.ThrowIfCancellationRequested();
                    if (names.Count >= 100000) throw new InvalidDataException("Too many content resources.");
                    if (!names.Add(entry.FullName)) throw new InvalidDataException("Duplicate archive entry.");
                    if (entry.FullName != metadataName) Resolve(entry.FullName);
                    else if (entry.Length > MaximumMetadataBytes) throw new InvalidDataException("Visualization metadata exceeds the transfer budget.");
                    total = checked(total + entry.Length);
                    if (total > MaximumExpandedBytes || entry.Length < 0) throw new InvalidDataException("Visualization exceeds the expanded content budget.");
                }

                if (!names.Contains(metadataName)) throw new InvalidDataException("Missing visualization metadata.");
                foreach (var entry in zip.Entries)
                {
                    string path = StandardData.Resolve(directory, entry.FullName);
                    using (var source = entry.Open())
                    using (var target = File.Create(path))
                    {
                        var buffer = new byte[65536];
                        long remaining = entry.Length;
                        while (remaining > 0)
                        {
                            token.ThrowIfCancellationRequested();
                            int read = source.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining));
                            if (read == 0) throw new InvalidDataException("Truncated content resource.");
                            target.Write(buffer, 0, read);
                            remaining -= read;
                        }

                        if (source.ReadByte() != -1) throw new InvalidDataException("Expanded resource size mismatch.");
                    }

                    if (entry.FullName != metadataName && StandardData.HashFile(path) != entry.FullName.Substring(0, 64)) throw new InvalidDataException("Resource checksum mismatch.");
                }
            }
        }

        public string AddSurface(HBP.Core.DLL.Surface surface)
        {
            var mesh = new Mesh();
            try
            {
                int[] mask = surface.VisibilityMask;
                using var complete = (HBP.Core.DLL.Surface)surface.Clone();
                complete.UpdateVisibilityMask(Enumerable.Repeat(1, complete.NumberOfTriangles).ToArray()).Dispose();
                complete.UpdateMeshFromDLL(mesh);
                Vector3[] vertices = mesh.vertices, normals = mesh.normals;
                Color[] colors = mesh.colors;
                Vector2[] uv = mesh.uv;
                int[] triangles = mesh.triangles;
                return AddBuffer(writer =>
                {
                    writer.Write(vertices.Length);
                    writer.Write(triangles.Length);
                    foreach (var v in vertices)
                    {
                        writer.Write(v.x);
                        writer.Write(v.y);
                        writer.Write(v.z);
                    }

                    foreach (int index in triangles) writer.Write(index);
                    writer.Write(normals.Length);
                    foreach (var n in normals)
                    {
                        writer.Write(n.x);
                        writer.Write(n.y);
                        writer.Write(n.z);
                    }

                    writer.Write(uv.Length);
                    foreach (var u in uv)
                    {
                        writer.Write(u.x);
                        writer.Write(u.y);
                    }

                    writer.Write(colors.Length);
                    foreach (var c in colors)
                    {
                        writer.Write(c.r);
                        writer.Write(c.g);
                        writer.Write(c.b);
                        writer.Write(c.a);
                    }

                    writer.Write(surface.IsMarsAtlasLoaded);
                    writer.Write(mask.Length);
                    foreach (int visible in mask) writer.Write(visible);
                });
            }
            finally
            {
                UnityEngine.Object.Destroy(mesh);
            }
        }

        public HBP.Core.DLL.Surface ReadSurface(string name)
        {
            using var reader = new BinaryReader(File.OpenRead(Resolve(name)));
            int vertexCount = ReadCount(reader, 12), triangleCount = ReadCount(reader, 4);
            if (vertexCount == 0 || triangleCount % 3 != 0) throw new InvalidDataException("Invalid surface dimensions.");
            var vertices = new Vector3[vertexCount];
            for (int i = 0; i < vertexCount; i++) vertices[i] = new Vector3(Finite(reader), Finite(reader), Finite(reader));
            var triangles = new int[triangleCount];
            for (int i = 0; i < triangleCount; i++)
            {
                triangles[i] = reader.ReadInt32();
                if (triangles[i] < 0 || triangles[i] >= vertexCount) throw new InvalidDataException("Triangle index outside surface.");
            }

            int count = ReadCount(reader, 12);
            if (count != 0 && count != vertexCount) throw new InvalidDataException("Invalid normals.");
            var normals = new Vector3[count];
            for (int i = 0; i < count; i++) normals[i] = new Vector3(Finite(reader), Finite(reader), Finite(reader));
            count = ReadCount(reader, 8);
            if (count != 0 && count != vertexCount) throw new InvalidDataException("Invalid UVs.");
            var uv = new Vector2[count];
            for (int i = 0; i < count; i++) uv[i] = new Vector2(Finite(reader), Finite(reader));
            count = ReadCount(reader, 16);
            if (count != 0 && count != vertexCount) throw new InvalidDataException("Invalid colors.");
            var colors = new Color[count];
            for (int i = 0; i < count; i++) colors[i] = new Color(Finite(reader), Finite(reader), Finite(reader), Finite(reader));
            bool atlasAvailable = reader.ReadBoolean();
            count = ReadCount(reader, 4);
            if (count != triangleCount / 3) throw new InvalidDataException("Invalid surface visibility mask.");
            var mask = new int[count];
            for (int i = 0; i < count; i++)
            {
                mask[i] = reader.ReadInt32();
                if (mask[i] != 0 && mask[i] != 1) throw new InvalidDataException("Invalid surface visibility value.");
            }

            if (reader.BaseStream.Position != reader.BaseStream.Length) throw new InvalidDataException("Trailing surface data.");
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

        private static float Finite(BinaryReader reader)
        {
            float value = reader.ReadSingle();
            if (float.IsNaN(value) || float.IsInfinity(value)) throw new InvalidDataException("Nonfinite geometry.");
            return value;
        }

        public void Dispose()
        {
            // This directory is a uniquely created archive workspace, never a source project directory.
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
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

                writer.WriteValue(archive.AddBuffer(binary =>
                {
                    if (value is float[] floats)
                        foreach (float number in floats)
                            binary.Write(number);
                    else
                        foreach (int number in (int[])value)
                            binary.Write(number);
                }));
            }

            public override object ReadJson(JsonReader reader, Type type, object existing, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Null) return null;
                using var binary = new BinaryReader(File.OpenRead(archive.Resolve((string)reader.Value)));
                if (binary.BaseStream.Length % 4 != 0 || binary.BaseStream.Length > int.MaxValue) throw new InvalidDataException("Invalid numeric buffer.");
                archive.numericBytesRead = checked(archive.numericBytesRead + binary.BaseStream.Length);
                if (archive.numericBytesRead > MaximumExpandedBytes) throw new InvalidDataException("Decoded numeric data exceeds the memory budget.");
                int length = checked((int)(binary.BaseStream.Length / 4));
                if (type == typeof(float[]))
                {
                    var data = new float[length];
                    for (int i = 0; i < length; i++) data[i] = binary.ReadSingle();
                    return data;
                }
                else
                {
                    var data = new int[length];
                    for (int i = 0; i < length; i++) data[i] = binary.ReadInt32();
                    return data;
                }
            }
        }
    }
}
