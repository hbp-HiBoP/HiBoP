using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using HBP.Core.Data;
using HBP.Transfer.Transport;
using Newtonsoft.Json;

namespace HBP.Transfer.Scene
{
    // Keep the enclosing model in the shared JSON graph. Only the large per-channel
    // dictionaries use this format, so timelines, callbacks and global objects retain their contracts.
    internal sealed class PackedIEEGData : JsonConverter
    {
        internal enum Section : byte
        {
            Trials = 1,
            Statistics = 2,
            Values = 3
        }

        private const int Magic = 0x31534948; // HIS1, little endian
        private const int ChunkBytes = 1024 * 1024;
        private const int MaximumStringBytes = 65536;
        private static readonly Encoding Text = new UTF8Encoding(false, true);
        private readonly SceneArchive archive;
        private readonly PairingContext.GlobalReferenceConverter globals;
        private readonly Section section;

        internal PackedIEEGData(SceneArchive archive, PairingContext.GlobalReferenceConverter globals, Section section)
        {
            this.archive = archive;
            this.globals = globals;
            this.section = section;
        }

        public override bool CanConvert(Type type) =>
            type == (section switch
            {
                Section.Trials => typeof(Dictionary<string, BlocChannelData>),
                Section.Statistics => typeof(Dictionary<string, BlocChannelStatistics>),
                _ => typeof(Dictionary<string, float[]>)
            });

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            using var timing = PairingTiming.Measure("source.pack-ieeg-binary", archive.TimingScope);
            writer.WriteStartArray();
            using var chunks = new ChunkWriter(bytes =>
            {
                archive.CaptureCancellation.ThrowIfCancellationRequested();
                if (writer is DetachedSceneMetadata detached) detached.WriteBuffer(bytes);
                else writer.WriteValue(archive.RegisterCapturedNumericBuffer(bytes));
            });
            using (var binary = new BinaryWriter(chunks, Text, true))
            {
                binary.Write(Magic);
                binary.Write((byte)section);
                switch (section)
                {
                    case Section.Trials:
                        WriteTrials(binary, chunks, (Dictionary<string, BlocChannelData>)value);
                        break;
                    case Section.Statistics:
                        WriteStatistics(binary, (Dictionary<string, BlocChannelStatistics>)value);
                        break;
                    default:
                        var channels = (Dictionary<string, float[]>)value;
                        binary.Write(channels.Count);
                        foreach (var channel in channels)
                        {
                            WriteText(binary, channel.Key);
                            WriteSamples(binary, channel.Value);
                        }

                        break;
                }
            }

            chunks.Flush();
            writer.WriteEndArray();
        }

        private void WriteTrials(BinaryWriter writer, ChunkWriter chunks, Dictionary<string, BlocChannelData> channels)
        {
            writer.Write(channels.Count);
            var events = new Dictionary<EventInformation, int>();
            string previousRecording = null;
            foreach (var channel in channels)
            {
                archive.CaptureCancellation.ThrowIfCancellationRequested();
                if (archive.SignalPatientIds != null)
                {
                    // Channel IDs begin with the owning patient ID. Match the longest
                    // known prefix, since a patient ID itself may contain underscores.
                    string recording = null;
                    foreach (string patient in archive.SignalPatientIds)
                        if (channel.Key.Length > patient.Length && channel.Key[patient.Length] == '_' && channel.Key.StartsWith(patient, StringComparison.Ordinal))
                        {
                            recording = patient;
                            break;
                        }

                    if (recording != previousRecording) chunks.Flush();
                    previousRecording = recording;
                }

                WriteText(writer, channel.Key);
                writer.Write(channel.Value != null);
                if (channel.Value == null) continue;
                var trials = channel.Value.Trials;
                writer.Write(trials?.Length ?? -1);
                if (trials == null) continue;
                foreach (var trial in trials)
                {
                    writer.Write(trial != null);
                    if (trial == null) continue;
                    writer.Write(trial.IsValid);
                    var subTrials = trial.ChannelSubTrialBySubBloc;
                    writer.Write(subTrials?.Count ?? -1);
                    if (subTrials == null) continue;
                    foreach (var subTrial in subTrials)
                    {
                        writer.Write(globals.CaptureIndex(subTrial.Key));
                        writer.Write(subTrial.Value.Found);
                        WriteText(writer, subTrial.Value.Unit);
                        WriteSamples(writer, subTrial.Value.Values);
                        var occurrences = subTrial.Value.InformationsByEvent;
                        writer.Write(occurrences?.Count ?? -1);
                        if (occurrences == null) continue;
                        foreach (var occurrence in occurrences)
                        {
                            writer.Write(globals.CaptureIndex(occurrence.Key));
                            WriteEvent(writer, events, occurrence.Value);
                        }
                    }
                }
            }
        }

        private static void WriteEvent(BinaryWriter writer, Dictionary<EventInformation, int> events, EventInformation value)
        {
            if (value == null)
            {
                writer.Write(-1);
                return;
            }

            if (events.TryGetValue(value, out int index))
            {
                writer.Write(index);
                return;
            }

            writer.Write(events.Count);
            events.Add(value, events.Count);
            writer.Write(value.Occurences?.Length ?? -1);
            if (value.Occurences == null) return;
            foreach (var item in value.Occurences)
            {
                writer.Write(item.Code);
                writer.Write(item.Index);
                writer.Write(item.IndexFromStart);
                writer.Write(item.IndexFromMainEvent);
                writer.Write(item.Time);
                writer.Write(item.TimeFromStart);
                writer.Write(item.TimeFromMainEvent);
            }
        }

        private void WriteStatistics(BinaryWriter writer, Dictionary<string, BlocChannelStatistics> channels)
        {
            writer.Write(channels.Count);
            foreach (var channel in channels)
            {
                archive.CaptureCancellation.ThrowIfCancellationRequested();
                WriteText(writer, channel.Key);
                writer.Write(channel.Value != null);
                if (channel.Value == null) continue;
                var trial = channel.Value.Trial;
                writer.Write(trial.TotalNumberOfTrials);
                writer.Write(trial.NumberOfValidTrials);
                var subTrials = trial.ChannelSubTrialBySubBloc;
                writer.Write(subTrials?.Count ?? -1);
                if (subTrials == null) continue;
                foreach (var item in subTrials)
                {
                    writer.Write(globals.CaptureIndex(item.Key));
                    writer.Write(item.Value.TotalNumberOfSubTrials);
                    writer.Write(item.Value.NumberOfFoundSubTrials);
                    WriteSamples(writer, item.Value.Values);
                    WriteSamples(writer, item.Value.SEM);
                }
            }
        }

        public override object ReadJson(JsonReader reader, Type type, object existing, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            if (reader.TokenType != JsonToken.StartArray) throw new InvalidDataException("Expected iEEG section references.");
            var indices = new List<int>();
            long length = 0;
            while (reader.Read() && reader.TokenType != JsonToken.EndArray)
            {
                archive.ReadCancellation.ThrowIfCancellationRequested();
                if (reader.TokenType != JsonToken.Integer || reader.Value is not long index || index < 0 || index >= BufferPack.MaximumCount || indices.Count >= BufferPack.MaximumCount)
                    throw new InvalidDataException("Invalid iEEG section reference.");
                long bytes = archive.SignalChunkLength((int)index);
                if (bytes < 1 || bytes > ChunkBytes || bytes > SceneArchive.MaximumExpandedBytes - length)
                    throw new InvalidDataException("Invalid iEEG chunk size.");
                indices.Add((int)index);
                length += bytes;
            }

            if (reader.TokenType != JsonToken.EndArray || length < 9) throw new InvalidDataException("Truncated iEEG section.");
            archive.AccountDecodedNumericBytes(length);
            using var timing = PairingTiming.Measure("scene.decode-ieeg-binary", archive.TimingScope);
            using var input = new ChunkReader(archive, indices, length);
            using var binary = new BinaryReader(input, Text);
            if (binary.ReadInt32() != Magic || binary.ReadByte() != (byte)section) throw new InvalidDataException("Invalid iEEG section format.");
            object value = section switch
            {
                Section.Trials => ReadTrials(binary),
                Section.Statistics => ReadStatistics(binary),
                _ => ReadChannels(binary, () => ReadSamples(binary))
            };
            if (input.Position != length) throw new InvalidDataException("Trailing bytes in iEEG section.");
            return value;
        }

        private Dictionary<string, BlocChannelData> ReadTrials(BinaryReader reader)
        {
            var events = new List<EventInformation>();
            return ReadChannels(reader, () =>
            {
                if (!ReadFlag(reader)) return null;
                int count = ReadCount(reader, 1, true);
                if (count < 0) return new BlocChannelData(null);
                var trials = new ChannelTrial[count];
                for (int i = 0; i < count; i++)
                {
                    archive.ReadCancellation.ThrowIfCancellationRequested();
                    if (!ReadFlag(reader)) continue;
                    bool valid = ReadFlag(reader);
                    int subCount = ReadCount(reader, 17, true);
                    Dictionary<SubBloc, ChannelSubTrial> subTrials = subCount < 0 ? null : new(subCount);
                    for (int j = 0; j < subCount; j++)
                    {
                        var key = ReadGlobal<SubBloc>(reader);
                        bool found = ReadFlag(reader);
                        string unit = ReadText(reader);
                        float[] samples = ReadSamples(reader);
                        int eventCount = ReadCount(reader, 8, true);
                        Dictionary<Event, EventInformation> occurrences = eventCount < 0 ? null : new(eventCount);
                        for (int k = 0; k < eventCount; k++)
                            AddUnique(occurrences, ReadGlobal<Event>(reader), ReadEvent(reader, events));
                        AddUnique(subTrials, key, new ChannelSubTrial(samples, unit, found, occurrences));
                    }

                    trials[i] = new ChannelTrial(subTrials, valid);
                }

                return new BlocChannelData(trials);
            });
        }

        private static EventInformation ReadEvent(BinaryReader reader, List<EventInformation> events)
        {
            int index = reader.ReadInt32();
            if (index == -1) return null;
            if (index < 0 || index > events.Count) throw new InvalidDataException("Invalid iEEG event reference.");
            if (index < events.Count) return events[index];
            int count = ReadCount(reader, 28, true);
            var items = count < 0 ? null : new EventInformation.EventOccurence[count];
            for (int i = 0; i < count; i++)
                items[i] = new EventInformation.EventOccurence(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            var value = new EventInformation(items);
            events.Add(value);
            return value;
        }

        private Dictionary<string, BlocChannelStatistics> ReadStatistics(BinaryReader reader) =>
            ReadChannels(reader, () =>
            {
                if (!ReadFlag(reader)) return null;
                int total = reader.ReadInt32(), valid = reader.ReadInt32();
                int count = ReadCount(reader, 20, true);
                Dictionary<SubBloc, ChannelSubTrialStat> statistics = count < 0 ? null : new(count);
                for (int i = 0; i < count; i++)
                {
                    var key = ReadGlobal<SubBloc>(reader);
                    var value = new ChannelSubTrialStat
                    {
                        TotalNumberOfSubTrials = reader.ReadInt32(),
                        NumberOfFoundSubTrials = reader.ReadInt32(),
                        Values = ReadSamples(reader),
                        SEM = ReadSamples(reader)
                    };
                    AddUnique(statistics, key, value);
                }

                return new BlocChannelStatistics(new ChannelTrialStat(statistics, total, valid));
            });

        private Dictionary<string, T> ReadChannels<T>(BinaryReader reader, Func<T> read)
        {
            int count = ReadCount(reader, 5);
            var channels = new Dictionary<string, T>(Math.Min(count, 4096), StringComparer.Ordinal);
            for (int i = 0; i < count; i++)
            {
                archive.ReadCancellation.ThrowIfCancellationRequested();
                string key = ReadText(reader) ?? throw new InvalidDataException("Missing iEEG channel key.");
                AddUnique(channels, key, read());
            }

            return channels;
        }

        private T ReadGlobal<T>(BinaryReader reader) where T : BaseData => (T)archive.GlobalReferences.Resolve(reader.ReadInt32(), typeof(T));

        private static void AddUnique<TKey, TValue>(Dictionary<TKey, TValue> values, TKey key, TValue value)
        {
            if (!values.TryAdd(key, value)) throw new InvalidDataException("Duplicate key in iEEG section.");
        }

        private static bool ReadFlag(BinaryReader reader)
        {
            byte value = reader.ReadByte();
            if (value > 1) throw new InvalidDataException("Invalid iEEG flag.");
            return value == 1;
        }

        private static int ReadCount(BinaryReader reader, int stride, bool nullable = false)
        {
            int count = reader.ReadInt32();
            if (nullable && count == -1) return count;
            if (count < 0 || (long)count * stride > reader.BaseStream.Length - reader.BaseStream.Position)
                throw new InvalidDataException("Invalid iEEG section dimensions.");
            return count;
        }

        private static void WriteText(BinaryWriter writer, string value)
        {
            if (value == null)
            {
                writer.Write(-1);
                return;
            }

            int count = Text.GetByteCount(value);
            if (count > MaximumStringBytes) throw new InvalidDataException("iEEG text exceeds its budget.");
            writer.Write(count);
            if (count != 0) writer.Write(Text.GetBytes(value));
        }

        private static string ReadText(BinaryReader reader)
        {
            int count = ReadCount(reader, 1, true);
            if (count == -1) return null;
            if (count > MaximumStringBytes) throw new InvalidDataException("iEEG text exceeds its budget.");
            byte[] bytes = reader.ReadBytes(count);
            if (bytes.Length != count) throw new EndOfStreamException();
            return Text.GetString(bytes);
        }

        private static void WriteSamples(BinaryWriter writer, float[] values)
        {
            writer.Write(values?.Length ?? -1);
            if (values == null) return;
            // BinaryWriter.BaseStream flushes the chunk writer on access.
            if (BitConverter.IsLittleEndian) writer.Write(MemoryMarshal.AsBytes(values.AsSpan()));
            else
                foreach (float value in values)
                    writer.Write(value);
        }

        private float[] ReadSamples(BinaryReader reader)
        {
            int count = ReadCount(reader, sizeof(float), true);
            return count < 0 ? null : archive.ReadComponents<float>(reader, count);
        }

        private abstract class SectionStream : Stream
        {
            public override bool CanSeek => false;
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
        }

        private sealed class ChunkWriter : SectionStream
        {
            private readonly Action<byte[]> emit;
            private byte[] buffer = new byte[ChunkBytes];
            private int used;

            internal ChunkWriter(Action<byte[]> emit) => this.emit = emit;
            public override bool CanRead => false;
            public override bool CanWrite => true;
            public override long Length => throw new NotSupportedException();

            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override int Read(byte[] bytes, int offset, int count) => throw new NotSupportedException();
            public override void Write(byte[] bytes, int offset, int count) => Write(bytes.AsSpan(offset, count));

            public override void Write(ReadOnlySpan<byte> bytes)
            {
                while (!bytes.IsEmpty)
                {
                    int count = Math.Min(bytes.Length, buffer.Length - used);
                    bytes.Slice(0, count).CopyTo(buffer.AsSpan(used));
                    used += count;
                    if (used == buffer.Length)
                    {
                        emit(buffer);
                        buffer = new byte[ChunkBytes];
                        used = 0;
                    }

                    bytes = bytes.Slice(count);
                }
            }

            public override void Flush()
            {
                if (used == 0) return;
                emit(buffer.AsSpan(0, used).ToArray());
                used = 0;
            }
        }

        private sealed class ChunkReader : SectionStream
        {
            private readonly SceneArchive archive;
            private readonly IReadOnlyList<int> indices;
            private readonly byte[] buffer = new byte[ChunkBytes];
            private int available, offset, next;
            private long position;

            internal ChunkReader(SceneArchive archive, IReadOnlyList<int> indices, long length)
            {
                this.archive = archive;
                this.indices = indices;
                Length = length;
            }

            public override bool CanRead => true;
            public override bool CanWrite => false;
            public override long Length { get; }

            public override long Position
            {
                get => position;
                set => throw new NotSupportedException();
            }

            public override void Flush()
            {
            }

            public override void Write(byte[] bytes, int offset, int count) => throw new NotSupportedException();
            public override int Read(byte[] bytes, int offset, int count) => Read(bytes.AsSpan(offset, count));

            public override int Read(Span<byte> bytes)
            {
                if (bytes.IsEmpty) return 0;
                if (offset == available)
                {
                    if (next == indices.Count) return 0;
                    archive.ReadCancellation.ThrowIfCancellationRequested();
                    using var chunk = archive.OpenSignalChunk(indices[next++]);
                    available = checked((int)chunk.Length);
                    offset = 0;
                    int filled = 0;
                    while (filled < available)
                    {
                        int read = chunk.Read(buffer, filled, available - filled);
                        if (read == 0) throw new EndOfStreamException("Truncated iEEG chunk.");
                        filled += read;
                    }
                }

                int count = Math.Min(bytes.Length, available - offset);
                buffer.AsSpan(offset, count).CopyTo(bytes);
                offset += count;
                position += count;
                return count;
            }
        }
    }
}
