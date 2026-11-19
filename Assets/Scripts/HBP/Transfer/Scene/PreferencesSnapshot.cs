using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using HBP.Core.Enums;
using HBP.Core.Preferences;
using HBP.Core.Tools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace HBP.Transfer.Scene
{
    [Flags]
    public enum PreferencesChangeEffect
    {
        None = 0,
        HotApply = 1,
        NextOpening = 2,
        SceneReload = 4
    }

    /// <summary>A validated detached JSON value. Reading it never invokes runtime preference setters.</summary>
    public sealed class PreferencesSnapshot
    {
        private readonly JObject m_Value;
        public NormalizationType Normalization { get; }

        private PreferencesSnapshot(JObject value)
        {
            m_Value = value;
            Normalization = (NormalizationType)(int)value["Data"]["EEG"]["Normalization"];
        }

        public static byte[] Capture(UserPreferences preferences)
        {
            return CaptureSerialization(preferences)();
        }

        public static Func<byte[]> CaptureSerialization(UserPreferences preferences)
        {
            // Materialize values while Unity owns the mutable preferences; encode the detached value on a worker.
            var value = JObject.FromObject(preferences, CreateSerializer(false));
            return () => Encoding.UTF8.GetBytes(value.ToString(Formatting.None));
        }

        public static PreferencesSnapshot Read(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0 || bytes.Length > Transport.SessionControlCodec.MaximumBodyBytes) throw new InvalidDataException("Preferences exceed their budget.");
            using var text = new StringReader(new UTF8Encoding(false, true).GetString(bytes));
            using var reader = new JsonTextReader(text) { MaxDepth = 64, DateParseHandling = DateParseHandling.None };
            var value = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
            if (reader.Read()) throw new InvalidDataException("Trailing preferences data.");
            var serializer = CreateSerializer(true);
            ValidateShape(value, typeof(UserPreferences), serializer.ContractResolver, 0);
            // Conversion checks numeric ranges, rectangular color arrays, and property types using inert setters.
            var candidate = value.ToObject<UserPreferences>(serializer);
            candidate.Data.TagImport.CreatePolicy();
            if ((int)value["Data"]["EEG"]["Normalization"] == (int)NormalizationType.Auto) throw new InvalidDataException("Auto is not a user normalization preference.");
            return new(value);
        }

        private static void ValidateShape(JToken value, Type type, IContractResolver resolver, int depth)
        {
            if (depth > 64 || value == null || value.Type == JTokenType.Null) throw new InvalidDataException("Incomplete preferences.");
            if (type.IsEnum)
            {
                if (value.Type != JTokenType.Integer || !Enum.IsDefined(type, Enum.ToObject(type, (int)value))) throw new InvalidDataException("Invalid preference enum: " + type.Name);
                return;
            }

            if (type == typeof(float) || type == typeof(double))
            {
                if (value.Type != JTokenType.Float && value.Type != JTokenType.Integer || !double.IsFinite((double)value)) throw new InvalidDataException("Non-finite preference value.");
            }

            if (resolver.ResolveContract(type) is JsonObjectContract contract)
            {
                if (value is not JObject obj) throw new InvalidDataException("Invalid preference object: " + type.Name);
                foreach (var property in contract.Properties)
                    if (!property.Ignored && property.Readable && property.Writable)
                        ValidateShape(obj[property.PropertyName], property.PropertyType, resolver, depth + 1);
                foreach (var property in obj.Properties())
                    if (contract.Properties.GetClosestMatchProperty(property.Name) == null || property.Name.StartsWith("$", StringComparison.Ordinal))
                        throw new InvalidDataException("Unknown preference member: " + property.Name);
            }
            else if (resolver.ResolveContract(type) is JsonArrayContract array)
            {
                if (value is not JArray values) throw new InvalidDataException("Invalid preference array.");
                foreach (var item in values)
                    if (type.IsArray && type.GetArrayRank() > 1 && item is JArray nested)
                        foreach (var element in nested)
                            ValidateShape(element, array.CollectionItemType, resolver, depth + 1);
                    else ValidateShape(item, array.CollectionItemType, resolver, depth + 1);
            }
            else if (type == typeof(string) && value.Type != JTokenType.String || type == typeof(bool) && value.Type != JTokenType.Boolean) throw new InvalidDataException("Invalid preference value: " + type.Name);
        }

        public PreferencesChangeEffect Effect(UserPreferences current)
        {
            var existing = JObject.Parse(Encoding.UTF8.GetString(Capture(current)));
            PreferencesChangeEffect effects = PreferencesChangeEffect.None;
            foreach (var value in m_Value.Descendants())
            {
                if (value is not JValue || JToken.DeepEquals(value, existing.SelectToken(value.Path))) continue;
                effects |= value.Path == "Data.EEG.Normalization" ? PreferencesChangeEffect.SceneReload : value.Path.StartsWith("Data.Atlases.", StringComparison.Ordinal) || value.Path.StartsWith("General.Project.", StringComparison.Ordinal) ? PreferencesChangeEffect.NextOpening : PreferencesChangeEffect.HotApply;
            }

            return effects;
        }

        public void Apply(UserPreferences target, bool hasSharedScene)
        {
            if (hasSharedScene && (Effect(target) & PreferencesChangeEffect.SceneReload) != 0) throw new InvalidOperationException("Close the shared visualizations before changing EEG normalization.");
            // All validation is complete before runtime setters run on the caller's Unity thread.
            var candidate = m_Value.ToObject<UserPreferences>(CreateSerializer(false));
            target.Copy(candidate);
            HBP.Core.Data.DataManager.ConfigureMemoryBudget(target.General.System.MemoryCacheLimit, UnityEngine.SystemInfo.systemMemorySize);
            target.NotifyChanged();
        }

        private static JsonSerializer CreateSerializer(bool inert) =>
            JsonSerializer.Create(new JsonSerializerSettings
            {
                ContractResolver = new SnapshotResolver(inert), TypeNameHandling = TypeNameHandling.None,
                MissingMemberHandling = MissingMemberHandling.Error, ObjectCreationHandling = ObjectCreationHandling.Replace,
                MetadataPropertyHandling = MetadataPropertyHandling.Ignore, MaxDepth = 64
            });

        private sealed class SnapshotResolver : SerializationAliasContractResolver
        {
            private readonly bool m_Inert;
            public SnapshotResolver(bool inert) => m_Inert = inert;

            protected override JsonObjectContract CreateObjectContract(Type type)
            {
                var contract = base.CreateObjectContract(type);
                if (type.Namespace == typeof(UserPreferences).Namespace)
                {
                    contract.OverrideCreator = null;
                    contract.CreatorParameters.Clear();
                    contract.DefaultCreator = () => FormatterServices.GetUninitializedObject(type);
                    contract.OnDeserializingCallbacks.Clear();
                    contract.OnDeserializedCallbacks.Clear();
                    contract.OnSerializingCallbacks.Clear();
                    contract.OnSerializedCallbacks.Clear();
                }

                return contract;
            }

            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization serialization)
            {
                var property = base.CreateProperty(member, serialization);
                bool runtimeSetter = member.DeclaringType == typeof(EEGPreferences) && (member.Name == nameof(EEGPreferences.Averaging) || member.Name == nameof(EEGPreferences.Normalization)) || member.DeclaringType == typeof(ProtocolPreferences) && member.Name == nameof(ProtocolPreferences.PositionAveraging) || member.DeclaringType == typeof(AnatomicPreferences) && member.Name == nameof(AnatomicPreferences.SiteNameCorrection);
                if (m_Inert && runtimeSetter) property.ValueProvider = new InertSetter(property.ValueProvider);
                return property;
            }
        }

        private sealed class InertSetter : IValueProvider
        {
            private readonly IValueProvider m_Original;
            public InertSetter(IValueProvider original) => m_Original = original;
            public object GetValue(object target) => m_Original.GetValue(target);

            public void SetValue(object target, object value)
            {
            }
        }
    }
}
