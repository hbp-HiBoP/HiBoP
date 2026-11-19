using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace HBP.Transfer.Scene
{
    public sealed class SceneSessionEntry
    {
        public string VisualizationId;
        public string TransferId;
        public bool Orphan;
    }

    public sealed class SceneSessionClose
    {
        public string VisualizationId;
        public string TransferId;
        public ulong FinalSequence;
        public ulong BulkThrough;
    }

    public static class SceneSessionState
    {
        public const int MaximumScenes = 64;
        public static byte[] Encode<T>(T value) => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value));

        public static T Decode<T>(byte[] bytes)
        {
            if (bytes == null || bytes.Length > 65536) throw new InvalidDataException("Scene inventory exceeds its budget.");
            return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(bytes), new JsonSerializerSettings { MaxDepth = 8, TypeNameHandling = TypeNameHandling.None }) ?? throw new InvalidDataException("Missing scene session data.");
        }
    }
}
