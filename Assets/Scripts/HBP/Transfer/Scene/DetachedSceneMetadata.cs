using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HBP.Transfer.Scene
{
    public sealed class ScenePreparationBudgetExceededException : InvalidOperationException
    {
        public ScenePreparationBudgetExceededException() : base("The ahead-of-send scene snapshot exceeds its memory budget.")
        {
        }
    }

    // Only JSON values and owned numeric bytes cross the worker boundary.
    // Reusing the existing serializer contracts preserves aliases, references and callbacks.
    internal sealed class DetachedSceneMetadata : JTokenWriter
    {
        private readonly List<(JValue token, byte[] bytes)> numeric = new();
        private bool encoded;
        private long bytes;
        private long metadataBytes;
        private readonly long maximumBytes;
        internal long EstimatedRetainedBytes => checked(bytes + metadataBytes);
        internal long OwnedNumericBytes => bytes;
        internal int NumericBufferCount => numeric.Count;

        internal DetachedSceneMetadata() : this(long.MaxValue, 0)
        {
        }

        internal DetachedSceneMetadata(long maximumBytes, long surfaceBytes)
        {
            this.maximumBytes = maximumBytes;
            Account(surfaceBytes);
        }

        // Conservative retained-size accounting: token/container/property overhead,
        // owned UTF-16 values, numeric copies and captured geometry. No live model cache.
        private void Account(long count)
        {
            metadataBytes = checked(metadataBytes + count);
            if (EstimatedRetainedBytes > maximumBytes) throw new ScenePreparationBudgetExceededException();
        }

        internal void CheckNumericBudget(long count)
        {
            if (count > maximumBytes - EstimatedRetainedBytes) throw new ScenePreparationBudgetExceededException();
        }

        public override void WriteStartObject()
        {
            Account(256);
            base.WriteStartObject();
        }

        public override void WriteStartArray()
        {
            Account(256);
            base.WriteStartArray();
        }

        public override void WritePropertyName(string value)
        {
            Account(256 + 2L * value.Length);
            base.WritePropertyName(value);
        }

        public override void WriteNull()
        {
            Account(128);
            base.WriteNull();
        }

        public override void WriteUndefined()
        {
            Account(128);
            base.WriteUndefined();
        }

        public override void WriteRaw(string value)
        {
            Account(128 + 2L * (value?.Length ?? 0));
            base.WriteRaw(value);
        }

        public override void WriteValue(byte value)
        {
            Account(128);
            base.WriteValue(value);
        }

        public override void WriteValue(sbyte value)
        {
            Account(128);
            base.WriteValue(value);
        }

        public override void WriteValue(short value)
        {
            Account(128);
            base.WriteValue(value);
        }

        public override void WriteValue(ushort value)
        {
            Account(128);
            base.WriteValue(value);
        }

        public override void WriteValue(char value)
        {
            Account(128);
            base.WriteValue(value);
        }

        public override void WriteValue(Uri value)
        {
            Account(128 + 2L * (value?.OriginalString.Length ?? 0));
            base.WriteValue(value);
        }

        public override void WriteValue(string value)
        {
            Account(128 + 2L * (value?.Length ?? 0));
            base.WriteValue(value);
        }

        public override void WriteValue(int value)
        {
            Account(128);
            base.WriteValue(value);
        }

        public override void WriteValue(long value)
        {
            Account(128);
            base.WriteValue(value);
        }

        public override void WriteValue(uint value)
        {
            Account(128);
            base.WriteValue(value);
        }

        public override void WriteValue(ulong value)
        {
            Account(128);
            base.WriteValue(value);
        }

        public override void WriteValue(float value)
        {
            Account(128);
            base.WriteValue(value);
        }

        public override void WriteValue(double value)
        {
            Account(128);
            base.WriteValue(value);
        }

        public override void WriteValue(decimal value)
        {
            Account(128);
            base.WriteValue(value);
        }

        public override void WriteValue(bool value)
        {
            Account(128);
            base.WriteValue(value);
        }

        public override void WriteValue(DateTime value)
        {
            Account(128);
            base.WriteValue(value);
        }

        public override void WriteValue(DateTimeOffset value)
        {
            Account(128);
            base.WriteValue(value);
        }

        public override void WriteValue(Guid value)
        {
            Account(128);
            base.WriteValue(value);
        }

        public override void WriteValue(TimeSpan value)
        {
            Account(128);
            base.WriteValue(value);
        }

        public override void WriteValue(byte[] value)
        {
            Account(128 + (value?.LongLength ?? 0));
            base.WriteValue(value == null ? null : (byte[])value.Clone());
        }

        internal void WriteBuffer(byte[] owned)
        {
            CheckNumericBudget(owned.LongLength);
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
