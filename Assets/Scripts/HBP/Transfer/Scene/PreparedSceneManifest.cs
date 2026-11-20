using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using HBP.Transfer.Transport;
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

            internal MeshEntry(JToken token)
            {
                Name = RequiredText(token, "Name");
                PatientId = (string)token["PatientId"];
                SourceMRI = (string)token["SourceMRI"];
                Standard = (string)token["Standard"];
                Type = RequiredInt(token, "Type");
                DescriptorHash = Hash(token);
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

            internal FunctionalEntry(JToken token)
            {
                Name = RequiredText(token, "Name");
                PatientId = (string)token["PatientId"];
                File = (string)token["File"];
                Mask = (string)token["Mask"];
                DescriptorHash = Hash(token);
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
        public IReadOnlyList<MeshEntry> Meshes { get; }
        public IReadOnlyList<VolumeEntry> MRIs { get; }
        public IReadOnlyList<ColumnEntry> Columns { get; }

        internal PreparedSceneManifest(JToken metadata, string visualizationId, string timingScope)
        {
            TransferId = RequiredText(metadata, "TransferId");
            SessionId = RequiredText(metadata, "SessionId");
            GlobalContextId = RequiredText(metadata, "GlobalContextId");
            if (string.IsNullOrEmpty(visualizationId)) throw new InvalidDataException("Missing delivered visualization identity.");
            VisualizationId = visualizationId;
            using var timing = PairingTiming.Measure("scene.manifest-resource-descriptors", timingScope);
            if (metadata["StandardFiles"] == null) throw new InvalidDataException("Missing standard resource roster.");
            Meshes = Array.AsReadOnly(RequiredArray(metadata, "Meshes").Select(item => new MeshEntry(item)).ToArray());
            MRIs = Array.AsReadOnly(RequiredArray(metadata, "MRIs").Select(item => new VolumeEntry(item)).ToArray());
            Columns = Array.AsReadOnly(RequiredArray(metadata, "Columns").Select(item => new ColumnEntry(item)).ToArray());
        }

        internal static PreparedSceneManifest FromMetadata(JToken metadata, string timingScope = null)
        {
            if (metadata is not JObject) throw new InvalidDataException("Invalid delivered scene metadata.");
            JToken visualization = metadata["Visualization"] ?? throw new InvalidDataException("Missing delivered visualization.");
            return new PreparedSceneManifest(metadata, RequiredText(visualization, "ID"), timingScope);
        }

        internal static PreparedSceneManifest FromArchiveFile(string path, string timingScope = null)
        {
            using var zip = ZipFile.OpenRead(path);
            var entry = zip.GetEntry("visualization.json") ?? throw new InvalidDataException("Missing delivered scene metadata.");
            if (entry.Length > SceneArchive.MaximumMetadataBytes) throw new InvalidDataException("Scene metadata exceeds its budget.");
            using var text = new StreamReader(entry.Open());
            return Read(text, CancellationToken.None, timingScope);
        }

        // Check the original JSON and retain only the resource roster. The visualization's
        // full token tree is never held alongside the restored scientific object graph.
        internal static PreparedSceneManifest Read(TextReader text, CancellationToken token, string timingScope = null)
        {
            using var reader = new ManifestReader(text, token);
            if (!ReadContent(reader) || reader.TokenType != JsonToken.StartObject)
                throw new InvalidDataException("Invalid delivered scene metadata.");

            var fields = new JObject();
            string visualizationId = null;
            while (ReadContent(reader) && reader.TokenType != JsonToken.EndObject)
            {
                if (reader.TokenType != JsonToken.PropertyName)
                    throw new InvalidDataException("Invalid scene property.");
                string property = (string)reader.Value;
                if (!ReadContent(reader)) throw new InvalidDataException("Truncated scene metadata.");

                switch (property)
                {
                    case "Visualization":
                        visualizationId = ReadVisualizationId(reader);
                        break;
                    case "TransferId":
                    case "SessionId":
                    case "GlobalContextId":
                    case "StandardFiles":
                    case "Meshes":
                    case "MRIs":
                    case "Columns":
                        fields.Add(property, JToken.ReadFrom(reader));
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            if (reader.TokenType != JsonToken.EndObject || reader.Read())
                throw new InvalidDataException("Truncated or trailing scene metadata.");
            return new PreparedSceneManifest(fields, visualizationId, timingScope);
        }

        private static bool ReadContent(JsonReader reader)
        {
            while (reader.Read())
                if (reader.TokenType != JsonToken.Comment)
                    return true;
            return false;
        }

        private static string ReadVisualizationId(JsonReader reader)
        {
            if (reader.TokenType != JsonToken.StartObject) throw new InvalidDataException("Invalid delivered visualization.");
            string id = null;
            int depth = reader.Depth;
            while (ReadContent(reader))
            {
                if (reader.Depth == depth && reader.TokenType == JsonToken.EndObject) return id;
                if (reader.Depth != depth + 1 || reader.TokenType != JsonToken.PropertyName || (string)reader.Value != "ID") continue;
                if (!ReadContent(reader) || reader.TokenType != JsonToken.String)
                    throw new InvalidDataException("Invalid delivered visualization identity.");
                id = (string)reader.Value;
            }

            throw new InvalidDataException("Truncated delivered visualization.");
        }

        internal sealed class ManifestReader : JsonTextReader
        {
            private readonly List<HashSet<string>> properties = new();
            private readonly CancellationToken token;
            private int objectCount, reads;

            internal ManifestReader(TextReader reader, CancellationToken token) : base(reader)
            {
                this.token = token;
                CloseInput = false;
                MaxDepth = 128;
            }

            public override bool Read()
            {
                if ((reads++ & 1023) == 0) token.ThrowIfCancellationRequested();
                if (!base.Read()) return false;
                switch (TokenType)
                {
                    case JsonToken.StartObject:
                        if (objectCount == properties.Count) properties.Add(new HashSet<string>(StringComparer.Ordinal));
                        objectCount++;
                        break;
                    case JsonToken.EndObject:
                        properties[--objectCount].Clear();
                        break;
                    case JsonToken.PropertyName:
                        if (!properties[objectCount - 1].Add((string)Value))
                            throw new InvalidDataException("Duplicate scene property: " + Value);
                        break;
                }

                return true;
            }
        }

        // JsonReader's typed reads use this Read path as well. Keep the token type
        // originally delivered for resource hashes and retain only the small roster.
        internal sealed class SinglePassReader : JsonReader, IJsonLineInfo
        {
            private readonly ManifestReader input;
            private readonly JObject fields = new();
            private string field, visualizationId;
            private JTokenWriter value;
            private bool inVisualization, readVisualizationId;

            internal SinglePassReader(TextReader text, CancellationToken token)
            {
                input = new ManifestReader(text, token);
                MaxDepth = input.MaxDepth;
            }

            public override bool Read()
            {
                if (!input.Read()) return false;
                JsonToken token = input.TokenType;
                int depth = input.Depth;
                object item = input.Value;
                SetToken(token, item);
                if (token == JsonToken.Comment) return true;

                if (value != null)
                {
                    value.WriteToken(token, item);
                    if (depth == 1)
                    {
                        fields.Add(field, value.Token);
                        value.Close();
                        value = null;
                        field = null;
                    }
                }
                else if (inVisualization)
                {
                    if (readVisualizationId)
                    {
                        if (token != JsonToken.String) throw new InvalidDataException("Invalid delivered visualization identity.");
                        visualizationId = (string)item;
                        readVisualizationId = false;
                    }
                    else if (depth == 2 && token == JsonToken.PropertyName && (string)item == "ID")
                        readVisualizationId = true;
                    else if (depth == 1 && token == JsonToken.EndObject)
                        inVisualization = false;
                }
                else if (field != null)
                {
                    if (field == "Visualization")
                    {
                        if (token != JsonToken.StartObject) throw new InvalidDataException("Invalid delivered visualization.");
                        inVisualization = true;
                        field = null;
                    }
                    else if (token is JsonToken.StartObject or JsonToken.StartArray)
                    {
                        value = new JTokenWriter();
                        value.WriteToken(token, item);
                    }
                    else
                    {
                        fields.Add(field, new JValue(item));
                        field = null;
                    }
                }
                else if (depth == 1 && token == JsonToken.PropertyName)
                {
                    string name = (string)item;
                    if (name is "Visualization" or "TransferId" or "SessionId" or "GlobalContextId" or "StandardFiles" or "Meshes" or "MRIs" or "Columns")
                        field = name;
                }

                return true;
            }

            internal PreparedSceneManifest Finish(string timingScope)
            {
                if (TokenType != JsonToken.EndObject || Depth != 0 || Read())
                    throw new InvalidDataException("Truncated or trailing scene metadata.");
                return new PreparedSceneManifest(fields, visualizationId, timingScope);
            }

            public int LineNumber => input.LineNumber;
            public int LinePosition => input.LinePosition;
            public bool HasLineInfo() => input.HasLineInfo();

            public override void Close()
            {
                input.Close();
                value?.Close();
                base.Close();
            }
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
            using SHA256 sha = HBP.Core.Tools.Sha256.Create();
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
