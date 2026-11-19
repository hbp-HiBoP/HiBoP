using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HBP.Core.Data;
using HBP.Core.Data.Processed;
using HBP.Core.Tools;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine.Events;
using IEEGData = HBP.Core.Data.Processed.IEEGData;
using CCEPData = HBP.Core.Data.Processed.CCEPData;
using StaticData = HBP.Core.Data.Processed.StaticData;

namespace HBP.Transfer.Scene
{
    /// <summary>A transfer-only contract: project JSON settings and on-disk schemas are untouched.</summary>
    internal sealed class PreparedDataJson : SerializationAliasContractResolver, ISerializationBinder
    {
        private readonly SceneArchive archive;
        private JsonConverter[] operationConverters;
        private Action columnRestored;
        internal bool InertPreferences { get; set; }

        private PreparedDataJson(SceneArchive archive)
        {
            this.archive = archive;
        }

        private static readonly HashSet<Type> SignalTypes = new()
        {
            typeof(IEEGData), typeof(CCEPData), typeof(StaticData), typeof(Timeline), typeof(SubTimeline),
            typeof(BlocChannelData), typeof(ChannelTrial), typeof(ChannelSubTrial), typeof(BlocChannelStatistics),
            typeof(ChannelTrialStat), typeof(ChannelSubTrialStat), typeof(BlocEventsStatistics), typeof(SubBlocEventsStatistics),
            typeof(EventInformation), typeof(EventInformation.EventOccurence), typeof(EventStatistics), typeof(Frequency)
        };

        public static JsonSerializer Create(SceneArchive archive, PairingContext globals = null, bool compactSceneReferences = false, bool inertPreferences = false, JsonConverter numericConverter = null, Action columnRestored = null)
        {
            var resolver = new PreparedDataJson(archive) { InertPreferences = inertPreferences, columnRestored = columnRestored };
            var converters = new List<JsonConverter>();
            if (globals != null) converters.Add(globals.ReferenceConverter(compactSceneReferences ? archive.GlobalReferences : null));
            converters.Add(new ObjectKeyDictionaryConverter());
            if (numericConverter != null) converters.Add(numericConverter);
            resolver.operationConverters = converters.ToArray();
            var serializer = JsonSerializer.Create(new JsonSerializerSettings
            {
                ContractResolver = resolver, SerializationBinder = resolver, TypeNameHandling = TypeNameHandling.Auto,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects, ObjectCreationHandling = ObjectCreationHandling.Replace,
                MaxDepth = 128
            });
            return serializer;
        }

        protected override JsonContract CreateContract(Type type)
        {
            var contract = base.CreateContract(type);
            // Resolve matching once per type and operation. Converter state still validates
            // every real instance; no mutable model or validation survives the capture.
            if (contract.Converter == null)
                contract.Converter = operationConverters.FirstOrDefault(converter => converter.CanConvert(type));
            if (columnRestored != null && typeof(Column).IsAssignableFrom(type))
                contract.OnDeserializedCallbacks.Add((_, _) => columnRestored());
            return contract;
        }

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization serialization)
        {
            if (SignalTypes.Contains(type))
            {
                // Serialize stored signal metadata, not calculated accessors or Unity callbacks.
                var fields = new List<JsonProperty>();
                for (Type current = type; current != null && current != typeof(object); current = current.BaseType)
                    foreach (var field in current.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    {
                        if (field.Name == "m_PinnedDataInfos" || typeof(UnityEventBase).IsAssignableFrom(field.FieldType) || typeof(Delegate).IsAssignableFrom(field.FieldType) || field.FieldType == typeof(IconicScenario)) continue;
                        var property = base.CreateProperty(field, MemberSerialization.Fields);
                        property.Readable = property.Writable = true;
                        fields.Add(property);
                    }

                return fields;
            }

            var properties = base.CreateProperties(type, serialization).ToList();
            // Expand references which project files normally resolve through a loaded project/database.
            foreach (string name in new[] { "Patients", "Dataset", "Bloc", "Protocol", "Tag", "IllustrationPath", "ImagePath" })
            {
                var member = name == "Tag" && typeof(BaseTagValue).IsAssignableFrom(type) ? typeof(BaseTagValue).GetProperty(name) : type.GetProperty(name);
                if (member == null || !member.CanWrite) continue;
                properties.RemoveAll(p => p.PropertyName == name || p.UnderlyingName == name);
                var property = base.CreateProperty(member, MemberSerialization.OptOut);
                property.Ignored = false;
                properties.Add(property);
            }

            if (type == typeof(IEEGColumn) || type == typeof(CCEPColumn) || type == typeof(StaticColumn))
            {
                properties.RemoveAll(p => p.UnderlyingName == "Data");
                var property = base.CreateProperty(type.GetProperty("Data"), MemberSerialization.OptOut);
                property.Ignored = false;
                properties.Add(property);
            }

            // Source anatomy is captured from the prepared native resources, and datasets carry
            // metadata only. No source project path or unrelated dataset enters the transfer.
            if (type == typeof(Patient)) properties.RemoveAll(p => p.PropertyName == "Meshes" || p.PropertyName == "MRIs");
            if (type == typeof(Dataset)) properties.RemoveAll(p => p.PropertyName == "Data");
            // Definition identity is independent of optional illustration files and local paths.
            // A paired session keeps its captured images even if Desktop files change later.
            if (archive == null) properties.RemoveAll(p => p.PropertyName == "IllustrationPath" || p.PropertyName == "ImagePath");
            foreach (var property in properties)
                if (property.PropertyName == "IllustrationPath" || property.PropertyName == "ImagePath")
                    property.Converter = new IllustrationConverter(archive);
            return properties;
        }

        protected override JsonObjectContract CreateObjectContract(Type type)
        {
            var contract = base.CreateObjectContract(type);
            if (InertPreferences && type.Namespace == typeof(HBP.Core.Preferences.UserPreferences).Namespace)
            {
                contract.OverrideCreator = null;
                contract.CreatorParameters.Clear();
                contract.DefaultCreator = () => FormatterServices.GetUninitializedObject(type);
                contract.OnDeserializingCallbacks.Clear();
                contract.OnDeserializedCallbacks.Clear();
            }

            // Project callbacks rewrite identifier/path fields and Visualization clears Patients.
            // The prepared graph already contains resolved references and must not use that path.
            if (typeof(BaseData).IsAssignableFrom(type)) contract.OnSerializingCallbacks.Clear();
            if (type == typeof(Visualization)) contract.OnDeserializedCallbacks.Clear();
            if (SignalTypes.Contains(type))
            {
                contract.OverrideCreator = null;
                contract.CreatorParameters.Clear();
                contract.DefaultCreator = () => type.GetConstructor(Type.EmptyTypes) != null ? Activator.CreateInstance(type) : FormatterServices.GetUninitializedObject(type);
                contract.DefaultCreatorNonPublic = false;
                if (type == typeof(Timeline))
                    contract.OnDeserializedCallbacks.Add((value, context) =>
                    {
                        var timeline = (Timeline)value;
                        timeline.OnUpdateCurrentIndex = new UnityEvent();
                        timeline.OnStopTimelinePlay = new UnityEvent();
                    });
            }

            return contract;
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization serialization)
        {
            var property = base.CreateProperty(member, serialization);
            if (InertPreferences && member.DeclaringType?.Namespace == typeof(HBP.Core.Preferences.UserPreferences).Namespace && member is PropertyInfo info)
            {
                // Store preference values without invoking setters which alter DLL/global state.
                string fieldName = "<" + member.Name + ">k__BackingField";
                var field = member.DeclaringType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic) ?? member.DeclaringType.GetField("m_" + member.Name, BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null) property.ValueProvider = new ReflectionValueProvider(field);
                else if (member.Name is "Averaging" or "Normalization" or "PositionAveraging" or "SiteNameCorrection")
                    property.ValueProvider = new InertPreferenceSetter(property.ValueProvider);
            }

            return property;
        }

        private sealed class InertPreferenceSetter : IValueProvider
        {
            private readonly IValueProvider original;
            internal InertPreferenceSetter(IValueProvider original) => this.original = original;
            public object GetValue(object target) => original.GetValue(target);

            public void SetValue(object target, object value)
            {
            }
        }

        public Type BindToType(string assemblyName, string typeName)
        {
            Type signal = SignalTypes.FirstOrDefault(t => t.FullName == typeName);
            return signal ?? SerializationTypeRegistry.Resolve(assemblyName, typeName);
        }

        public void BindToName(Type type, out string assemblyName, out string typeName)
        {
            if (SignalTypes.Contains(type))
            {
                assemblyName = null;
                typeName = type.FullName;
            }
            else SerializationTypeRegistry.GetSerializedName(type, out assemblyName, out typeName);
        }

        private sealed class IllustrationConverter : JsonConverter
        {
            private readonly SceneArchive archive;

            public IllustrationConverter(SceneArchive archive)
            {
                this.archive = archive;
            }

            public override bool CanConvert(Type type) => type == typeof(string);

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                string path = value as string;
                writer.WriteValue(string.IsNullOrEmpty(path) ? "" : archive.AddIllustration(path));
            }

            public override object ReadJson(JsonReader reader, Type type, object existing, JsonSerializer serializer)
            {
                string name = reader.Value as string;
                return string.IsNullOrEmpty(name) ? "" : archive.Resolve(name);
            }
        }

        /// <summary>Object keys remain canonical protocol objects through JSON reference identities.</summary>
        private sealed class ObjectKeyDictionaryConverter : JsonConverter
        {
            public override bool CanConvert(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>) && typeof(BaseData).IsAssignableFrom(type.GetGenericArguments()[0]);

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                writer.WriteStartArray();
                foreach (DictionaryEntry entry in (IDictionary)value)
                {
                    writer.WriteStartArray();
                    serializer.Serialize(writer, entry.Key);
                    serializer.Serialize(writer, entry.Value);
                    writer.WriteEndArray();
                }

                writer.WriteEndArray();
            }

            public override object ReadJson(JsonReader reader, Type type, object existing, JsonSerializer serializer)
            {
                if (reader.TokenType != JsonToken.StartArray) throw new JsonSerializationException("Expected protocol-key entries.");
                var result = (IDictionary)Activator.CreateInstance(type);
                Type[] arguments = type.GetGenericArguments();
                while (reader.Read() && reader.TokenType != JsonToken.EndArray)
                {
                    if (reader.TokenType != JsonToken.StartArray || !reader.Read()) throw new JsonSerializationException("Invalid protocol entry.");
                    object key = serializer.Deserialize(reader, arguments[0]);
                    reader.Read();
                    object value = serializer.Deserialize(reader, arguments[1]);
                    if (!reader.Read() || reader.TokenType != JsonToken.EndArray || key == null || result.Contains(key)) throw new JsonSerializationException("Invalid or repeated protocol key.");
                    result.Add(key, value);
                }

                return result;
            }
        }
    }
}
