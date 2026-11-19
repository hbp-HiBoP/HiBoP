using System;
using System.Collections.Generic;
using System.IO;
using HBP.Core.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HBP.Transfer.Scene
{
    internal sealed class SceneGlobalReferences
    {
        internal const string FileName = "globals.refs.json";
        private readonly PairingContext context;
        private readonly Dictionary<string, int> indices = new(StringComparer.Ordinal);
        private readonly List<(string key, string hash, BaseData value)> entries = new();
        internal int Count => entries.Count;
        internal string ContextId { get; }

        internal SceneGlobalReferences(PairingContext context)
        {
            this.context = context;
            ContextId = context.Id;
        }

        internal int Add(string key, string hash)
        {
            if (indices.TryGetValue(key, out int index))
            {
                if (entries[index].hash != hash) throw new InvalidDataException("Global definition changed after capture.");
                return index;
            }

            if (entries.Count >= 100000) throw new InvalidDataException("Too many global references.");
            BaseData value = context.ResolveReference(key, hash, typeof(BaseData));
            indices.Add(key, entries.Count);
            entries.Add((key, hash, value));
            return entries.Count - 1;
        }

        internal BaseData Resolve(long index, Type type)
        {
            if (index < 0 || index >= entries.Count || !type.IsInstanceOfType(entries[(int)index].value)) throw new InvalidDataException("Invalid global reference index or type.");
            return entries[(int)index].value;
        }

        internal byte[] Encode()
        {
            var references = new JArray();
            foreach (var item in entries) references.Add(new JArray(item.key, item.hash));
            return System.Text.Encoding.UTF8.GetBytes(new JObject { ["version"] = 1, ["context"] = ContextId, ["references"] = references }.ToString(Formatting.None));
        }

        internal static SceneGlobalReferences Read(string path, PairingContext context)
        {
            using var reader = new JsonTextReader(File.OpenText(path)) { MaxDepth = 4 };
            var data = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
            if (data.Count != 3 || data["version"]?.Type != JTokenType.Integer || (long)data["version"] != 1 || (string)data["context"] != context.Id || data["references"] is not JArray references || references.Count > 100000 || reader.Read())
                throw new InvalidDataException("Invalid scene global reference table.");
            var result = new SceneGlobalReferences(context);
            foreach (var item in references)
            {
                if (item is not JArray pair || pair.Count != 2 || pair[0].Type != JTokenType.String || pair[1].Type != JTokenType.String || result.indices.ContainsKey((string)pair[0])) throw new InvalidDataException("Invalid or duplicate global reference.");
                result.Add((string)pair[0], (string)pair[1]);
            }

            return result;
        }
    }
}
