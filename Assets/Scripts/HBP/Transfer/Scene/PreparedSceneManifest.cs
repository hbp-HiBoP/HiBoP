using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HBP.Transfer.Scene
{
    /// <summary>Immutable resource identities taken from the final delivered metadata.</summary>
    public sealed class PreparedSceneManifest
    {
        public sealed class MeshEntry
        {
            public string Name { get; }
            public string PatientId { get; }
            public string SourceMRI { get; }
            public string Standard { get; }
            public string DescriptorHash { get; }
            public int Type { get; }
            public string GeometryHash { get; }

            internal MeshEntry(JToken token)
            {
                Name = RequiredText(token, "Name");
                PatientId = (string)token["PatientId"];
                SourceMRI = (string)token["SourceMRI"];
                Standard = (string)token["Standard"];
                Type = RequiredInt(token, "Type");
                DescriptorHash = Hash(token);
                GeometryHash = (string)token["GeometryHash"];
            }
        }

        public sealed class VolumeEntry
        {
            public string Name { get; }
            public string PatientId { get; }
            public string File { get; }
            public string Standard { get; }
            public string DescriptorHash { get; }

            internal VolumeEntry(JToken token)
            {
                Name = RequiredText(token, "Name");
                PatientId = (string)token["PatientId"];
                File = (string)token["File"];
                Standard = (string)token["Standard"];
                DescriptorHash = Hash(token);
            }
        }

        public sealed class FunctionalEntry
        {
            public string Name { get; }
            public string PatientId { get; }
            public string File { get; }
            public string Mask { get; }
            public string DescriptorHash { get; }
            public string MegContentHash { get; }

            internal FunctionalEntry(JToken token)
            {
                Name = RequiredText(token, "Name");
                PatientId = (string)token["PatientId"];
                File = (string)token["File"];
                Mask = (string)token["Mask"];
                DescriptorHash = Hash(token);
                MegContentHash = (string)token["MegContentHash"];
            }

            public bool MatchesMegContent(IReadOnlyDictionary<string, float[]> values, IReadOnlyDictionary<string, string> units, float frequency)
            {
                return !string.IsNullOrEmpty(MegContentHash) && MegContentHash == SceneArchive.MegContentFingerprint(values, units, frequency);
            }
        }

        public sealed class ColumnEntry
        {
            public string Id { get; }
            public IReadOnlyList<FunctionalEntry> Functional { get; }

            internal ColumnEntry(JToken token)
            {
                Id = RequiredText(token, "Id");
                Functional = Array.AsReadOnly(RequiredArray(token, "Functional").Select(item => new FunctionalEntry(item)).ToArray());
            }
        }

        public string TransferId { get; }
        public string SessionId { get; }
        public string VisualizationId { get; }
        public string GlobalContextId { get; }
        public string VisualizationDescriptorHash { get; }
        public string StandardFilesDescriptorHash { get; }
        public IReadOnlyList<MeshEntry> Meshes { get; }
        public IReadOnlyList<VolumeEntry> MRIs { get; }
        public IReadOnlyList<ColumnEntry> Columns { get; }

        private PreparedSceneManifest(JToken metadata)
        {
            if (metadata is not JObject) throw new InvalidDataException("Invalid delivered scene metadata.");
            TransferId = RequiredText(metadata, "TransferId");
            SessionId = RequiredText(metadata, "SessionId");
            GlobalContextId = RequiredText(metadata, "GlobalContextId");
            JToken visualization = metadata["Visualization"] ?? throw new InvalidDataException("Missing delivered visualization.");
            VisualizationId = RequiredText(visualization, "ID");
            VisualizationDescriptorHash = Hash(visualization);
            StandardFilesDescriptorHash = Hash(metadata["StandardFiles"] ?? throw new InvalidDataException("Missing standard resource roster."));
            Meshes = Array.AsReadOnly(RequiredArray(metadata, "Meshes").Select(item => new MeshEntry(item)).ToArray());
            MRIs = Array.AsReadOnly(RequiredArray(metadata, "MRIs").Select(item => new VolumeEntry(item)).ToArray());
            Columns = Array.AsReadOnly(RequiredArray(metadata, "Columns").Select(item => new ColumnEntry(item)).ToArray());
        }

        internal static PreparedSceneManifest FromMetadata(JToken metadata) => new(metadata);

        internal static PreparedSceneManifest FromMetadataFile(string path)
        {
            using var reader = new JsonTextReader(File.OpenText(path)) { MaxDepth = 128 };
            return FromMetadata(JToken.ReadFrom(reader));
        }

        internal static PreparedSceneManifest FromArchiveFile(string path)
        {
            using var zip = ZipFile.OpenRead(path);
            var entry = zip.GetEntry("visualization.json") ?? throw new InvalidDataException("Missing delivered scene metadata.");
            if (entry.Length > SceneArchive.MaximumMetadataBytes) throw new InvalidDataException("Scene metadata exceeds its budget.");
            using var reader = new JsonTextReader(new StreamReader(entry.Open())) { MaxDepth = 128 };
            return FromMetadata(JToken.ReadFrom(reader));
        }

        private static JArray RequiredArray(JToken token, string name) => token[name] as JArray ?? throw new InvalidDataException("Missing delivered resource roster: " + name);

        private static string RequiredText(JToken token, string name)
        {
            string value = (string)token[name];
            return string.IsNullOrEmpty(value) ? throw new InvalidDataException("Missing delivered identity: " + name) : value;
        }

        private static int RequiredInt(JToken token, string name) => token[name]?.Type == JTokenType.Integer ? (int)token[name] : throw new InvalidDataException("Missing delivered resource type: " + name);

        private static string Hash(JToken token)
        {
            using SHA256 sha = SHA256.Create();
            using (var hashStream = new CryptoStream(Stream.Null, sha, CryptoStreamMode.Write))
            using (var textWriter = new StreamWriter(hashStream, new UTF8Encoding(false), 8192, true))
            using (var jsonWriter = new JsonTextWriter(textWriter) { CloseOutput = false })
            {
                token.WriteTo(jsonWriter);
                jsonWriter.Flush();
                textWriter.Flush();
                hashStream.FlushFinalBlock();
            }

            return BitConverter.ToString(sha.Hash).Replace("-", "").ToLowerInvariant();
        }
    }
}
