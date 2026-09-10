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
    internal sealed class PreparedDataJson : DefaultContractResolver, ISerializationBinder
    {
        private readonly SceneArchive archive;

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

        public static JsonSerializer Create(SceneArchive archive, PairingContext globals = null)
        {
            var resolver = new PreparedDataJson(archive);
            var serializer = JsonSerializer.Create(new JsonSerializerSettings
            {
                ContractResolver = resolver, SerializationBinder = resolver, TypeNameHandling = TypeNameHandling.Auto,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects, ObjectCreationHandling = ObjectCreationHandling.Replace,
                MaxDepth = 128, Converters = { new ObjectKeyDictionaryConverter() }
            });
            if (globals != null) serializer.Converters.Insert(0, globals.ReferenceConverter());
            return serializer;
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
            foreach (var property in properties)
                if (property.PropertyName == "IllustrationPath" || property.PropertyName == "ImagePath")
                    property.Converter = new IllustrationConverter(archive);
            return properties;
        }

        protected override JsonObjectContract CreateObjectContract(Type type)
        {
            var contract = base.CreateObjectContract(type);
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
                writer.WriteValue(string.IsNullOrEmpty(path) ? "" : archive == null ? StandardData.HashFile(path) : archive.AddFile(path, StandardData.HashFile(path)));
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
