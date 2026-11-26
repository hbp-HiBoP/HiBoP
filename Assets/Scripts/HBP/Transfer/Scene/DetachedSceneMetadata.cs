using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using HBP.Transfer.Transport;
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
        // The wire reference uses 16 bits each for the offset and length in 32-bit samples.
        private const int GroupBytes = (ushort.MaxValue + 1) * sizeof(int);
        internal const int MaximumGroupedBufferBytes = 64 * 1024;
        private readonly List<(JValue token, byte[] bytes, bool numericArray)> numeric = new();
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
            if (count > SceneArchive.MaximumExpandedBytes - bytes)
                throw new InvalidDataException("Snapshot exceeds its numeric byte budget.");
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

        internal void WriteBuffer(byte[] owned, bool numericArray = false)
        {
            CheckNumericBudget(owned.LongLength);
            bytes = checked(bytes + owned.LongLength);
            WriteValue(0);
            numeric.Add(((JValue)CurrentToken, owned, numericArray));
        }

        private void EncodeNumericBuffers(SceneArchive archive)
        {
            bool group = archive.SupportsNumericSlices && archive.GroupNumericBuffers;
            var pending = group ? new List<(JValue token, int offset, int length)>() : null;
            byte[] block = group && numeric.Count > 0 ? new byte[GroupBytes] : null;
            int used = 0;
            long grouped = 0;

            void Flush()
            {
                if (used == 0) return;
                int index = archive.RegisterCapturedNumericBuffer(block.AsSpan(0, used).ToArray());
                foreach (var entry in pending)
                    entry.token.Value = ~(((long)index << 32) | ((long)entry.offset << 16) | (long)entry.length);
                pending.Clear();
                used = 0;
            }

            for (int i = 0; i < numeric.Count; i++)
            {
                archive.CaptureCancellation.ThrowIfCancellationRequested();
                var item = numeric[i];
                if (group && item.numericArray && item.bytes.Length is > 0 and <= MaximumGroupedBufferBytes)
                {
                    if (item.bytes.Length > GroupBytes - used) Flush();
                    item.bytes.AsSpan().CopyTo(block.AsSpan(used));
                    pending.Add((item.token, used / sizeof(int), item.bytes.Length / sizeof(int)));
                    used += item.bytes.Length;
                    grouped++;
                }
                else item.token.Value = archive.RegisterCapturedNumericBuffer(item.bytes);

                numeric[i] = default; // Release each owned source as soon as it is packed.
            }

            Flush();
            PairingTiming.Count("source.grouped-numeric-buffer-count", grouped, archive.TimingScope);
        }

        internal byte[] Encode(SceneArchive archive)
        {
            if (encoded)
                throw new InvalidOperationException("A detached snapshot can only be encoded once.");
            encoded = true;
            // This list counts references. The pack enforces its entry limit after
            // merging identical buffers and assigning indices.
            using (PairingTiming.Measure("source.pack-numeric-buffers", archive.TimingScope))
                EncodeNumericBuffers(archive);
            numeric.Clear();
            using (PairingTiming.Measure("source.pack-surface-buffers", archive.TimingScope))
                archive.ResolveCapturedSurfaces(Token);
            using var memory = new MemoryStream();
            using (PairingTiming.Measure("source.write-metadata-json", archive.TimingScope))
            using (var writer = new JsonTextWriter(new StreamWriter(memory, new UTF8Encoding(false), 8192, true)))
                Token.WriteTo(writer);
            if (memory.Length > SceneArchive.MaximumMetadataBytes)
                throw new InvalidDataException("Visualization metadata exceeds the transfer budget.");
            return memory.ToArray();
        }
    }
}
