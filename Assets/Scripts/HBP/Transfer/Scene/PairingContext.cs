using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using HBP.Core.Data;
using HBP.Core.Preferences;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HBP.Transfer.Scene
{
    public sealed class GlobalDataPayload
    {
        public const int FormatVersion = 2;
        public int Version = FormatVersion;
        public string Id = Guid.NewGuid().ToString("N");
        public UserPreferences Preferences;
        public TagCollection Tags;
        public List<Protocol> Protocols;
        public AliasCollection Aliases;
        public JToken FilterPresets;
        public int Grid;
        public Core.Enums.VolumeInterpolation Interpolation;
    }

    /// <summary>The pairing snapshot. Its resource archive must outlive every scene using it.</summary>
    public sealed class PairingContext
    {
        public GlobalDataPayload Data { get; }
        public FilterConditionsPresetCollection FilterPresets { get; private set; }
        public string Id => Data.Id;
        private readonly Dictionary<string, BaseData> objects = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string> hashes = new(StringComparer.Ordinal);

        public PairingContext(GlobalDataPayload data)
        {
            if (data == null || data.Version != GlobalDataPayload.FormatVersion || string.IsNullOrEmpty(data.Id) || data.Id.Length > 128 || data.Preferences?.General == null || data.Preferences.Data?.EEG == null || data.Preferences.Data.Anatomic == null || data.Preferences.Data.Atlases == null || data.Preferences.Data.Protocol == null || data.Preferences.Visualization?._3D == null || data.Tags == null || data.Protocols == null || data.Aliases == null || data.Grid < 2 || data.Grid > 1024 || !Enum.IsDefined(typeof(Core.Enums.VolumeInterpolation), data.Interpolation))
                throw new InvalidDataException("Incomplete or unsupported pairing data.");
            Data = data;
            foreach (var tag in data.Tags.AllTags) Register(tag);
            foreach (var protocol in data.Protocols)
            {
                if (protocol == null) throw new InvalidDataException("Missing global protocol.");
                foreach (var item in protocol.GetAllIdentifiable()) Register(item);
            }
        }

        private void Register(BaseData value)
        {
            if (value == null || string.IsNullOrEmpty(value.ID)) throw new InvalidDataException("Missing global definition identity.");
            string key = Key(value);
            if (objects.ContainsKey(key)) throw new InvalidDataException("Ambiguous global definition identity: " + key);
            objects.Add(key, value);
            hashes.Add(key, Fingerprint(value));
        }

        internal static bool IsGlobal(Type type) => typeof(BaseTag).IsAssignableFrom(type) || type == typeof(Protocol) || type == typeof(Bloc) || type == typeof(SubBloc) || type == typeof(Event) || typeof(Icon).IsAssignableFrom(type) || typeof(Treatment).IsAssignableFrom(type);

        private static string Key(BaseData value) => value.GetType().FullName + ":" + value.ID;

        private static string Fingerprint(BaseData value)
        {
            var serializer = PreparedDataJson.Create(null);
            serializer.PreserveReferencesHandling = PreserveReferencesHandling.None;
            JToken Sort(JToken token) => token is JObject obj ? new JObject(obj.Properties().OrderBy(p => p.Name, StringComparer.Ordinal).Select(p => new JProperty(p.Name, Sort(p.Value)))) : token is JArray array ? new JArray(array.Select(Sort)) : token.DeepClone();
            string json = Sort(JToken.FromObject(value, serializer)).ToString(Formatting.None);
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(json))).Replace("-", "").ToLowerInvariant();
        }

        public void CaptureFilterPresets(FilterConditionsPresetCollection presets, SceneArchive archive)
        {
            Data.FilterPresets = JToken.FromObject(presets, PreparedDataJson.Create(archive, this));
        }

        public void RestoreFilterPresets(SceneArchive archive)
        {
            if (Data.FilterPresets == null) throw new InvalidDataException("Missing global filter presets.");
            FilterPresets = Data.FilterPresets.ToObject<FilterConditionsPresetCollection>(PreparedDataJson.Create(archive, this));
            if (FilterPresets == null) throw new InvalidDataException("Invalid global filter presets.");
        }

        internal BaseData ResolveReference(string key, string hash, Type type)
        {
            if (key == null || !objects.TryGetValue(key, out var value) || !type.IsInstanceOfType(value) || hash != hashes[key])
                throw new InvalidDataException("Unknown or incompatible global definition. Pair again before sending this visualization.");
            return value;
        }

        internal JsonConverter ReferenceConverter(SceneGlobalReferences references = null) => new GlobalReferenceConverter(this, references);

        private sealed class GlobalReferenceConverter : JsonConverter
        {
            private readonly PairingContext context;

            private readonly SceneGlobalReferences references;

            // This converter belongs to one serializer/capture, never to the paired session.
            // BaseData equality is ID-based; separate objects sharing an ID must each be checked.
            private readonly HashSet<BaseData> validated = new(ReferenceComparer.Instance);

            public GlobalReferenceConverter(PairingContext context, SceneGlobalReferences references)
            {
                this.context = context;
                this.references = references;
            }

            public override bool CanConvert(Type type) => IsGlobal(type);

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                if (value == null)
                {
                    writer.WriteNull();
                    return;
                }

                string key = Key((BaseData)value);
                if (!context.hashes.TryGetValue(key, out string hash) || (!validated.Contains((BaseData)value) && hash != MeasuredFingerprint((BaseData)value)))
                    throw new InvalidOperationException("A protocol or tag definition is absent or changed since pairing. Pair again before sending this visualization: " + key);
                validated.Add((BaseData)value);
                if (references != null)
                {
                    writer.WriteValue(references.Add(key, hash));
                    return;
                }

                writer.WriteStartObject();
                writer.WritePropertyName("global");
                writer.WriteValue(key);
                writer.WritePropertyName("hash");
                writer.WriteValue(hash);
                writer.WriteEndObject();
            }

            private string MeasuredFingerprint(BaseData value)
            {
                return Fingerprint(value);
            }

            public override object ReadJson(JsonReader reader, Type type, object existing, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Null)
                    return null;
                if (references != null)
                {
                    if (reader.TokenType != JsonToken.Integer || reader.Value is not long index)
                        throw new InvalidDataException("Expected a global reference index.");
                    return references.Resolve(index, type);
                }

                var reference = JObject.Load(reader);
                string key = (string)reference["global"];
                if (reference.Count != 2 || key == null || !context.objects.TryGetValue(key, out var value) || !type.IsInstanceOfType(value) || (string)reference["hash"] != context.hashes[key])
                    throw new InvalidDataException("Unknown or incompatible global definition. Pair again before sending this visualization.");
                return value;
            }
        }

        private sealed class ReferenceComparer : IEqualityComparer<BaseData>
        {
            public static readonly ReferenceComparer Instance = new();
            public bool Equals(BaseData x, BaseData y) => ReferenceEquals(x, y);
            public int GetHashCode(BaseData value) => RuntimeHelpers.GetHashCode(value);
        }
    }
}
