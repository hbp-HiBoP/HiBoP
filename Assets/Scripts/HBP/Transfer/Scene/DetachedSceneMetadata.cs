using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HBP.Transfer.Scene
{
    // Only JSON values and owned numeric bytes cross the worker boundary.
    // Reusing the existing serializer contracts preserves aliases, references and callbacks.
    internal sealed class DetachedSceneMetadata : JTokenWriter
    {
        private readonly List<(JValue token, byte[] bytes)> numeric = new();
        private bool encoded;
        private long bytes;

        internal void WriteBuffer(byte[] owned)
        {
            bytes = checked(bytes + owned.LongLength);
            if (bytes > SceneArchive.MaximumExpandedBytes || numeric.Count >= 100000)
                throw new InvalidDataException("Snapshot exceeds its buffer budget.");
            WriteValue(0);
            numeric.Add(((JValue)CurrentToken, owned));
        }

        internal byte[] Encode(SceneArchive archive)
        {
            if (encoded)
                throw new InvalidOperationException("A detached snapshot can only be encoded once.");
            encoded = true;
            foreach (var item in numeric)
                item.token.Value = archive.RegisterCapturedNumericBuffer(item.bytes);
            numeric.Clear();
            archive.ResolveCapturedSurfaces(Token);
            using var memory = new MemoryStream();
            using (var writer = new JsonTextWriter(new StreamWriter(memory, new UTF8Encoding(false), 8192, true)))
                Token.WriteTo(writer);
            if (memory.Length > SceneArchive.MaximumMetadataBytes)
                throw new InvalidDataException("Visualization metadata exceeds the transfer budget.");
            return memory.ToArray();
        }
    }
}
