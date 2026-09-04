using Google.Protobuf;
using MemoryPack;
using MessagePack;
using CRNL.HiBoP.Spikes.P06.Protobuf;

namespace CRNL.HiBoP.Spikes.P06;

public enum ControlCodecId : byte
{
    Protobuf = 1,
    MessagePack = 2,
    MemoryPack = 3,
}

public interface IControlCodec
{
    ControlCodecId Id { get; }

    string Name { get; }

    byte[] Encode(ControlSample sample);

    ControlSample Decode(ReadOnlyMemory<byte> payload);
}

public static class ControlCodecs
{
    public const int MaximumEncodedBytes = 64 * 1024;

    public static IReadOnlyList<IControlCodec> All { get; } =
        new IControlCodec[] { new ProtobufControlCodec(), new MessagePackControlCodec(), new MemoryPackControlCodec() };

    public static IControlCodec Get(ControlCodecId id) =>
        All.FirstOrDefault(codec => codec.Id == id)
        ?? throw new InvalidDataException($"Unsupported control codec: {(byte)id}.");

    internal static void ValidateEncodedLength(int length)
    {
        if (length is < 1 or > MaximumEncodedBytes)
        {
            throw new InvalidDataException($"Encoded control payload length {length} is outside 1..{MaximumEncodedBytes}.");
        }
    }

    internal static ControlSample ValidateDecoded(ControlSample sample)
    {
        sample.Validate();
        return sample;
    }
}

public sealed class ProtobufControlCodec : IControlCodec
{
    public ControlCodecId Id => ControlCodecId.Protobuf;

    public string Name => "Google.Protobuf 3.36.1";

    public byte[] Encode(ControlSample sample)
    {
        sample.Validate();
        var wire = new Protobuf.ControlSample
        {
            Kind = sample.Kind,
            MessageId = sample.MessageId,
            CorrelationId = sample.CorrelationId,
            Sequence = sample.Sequence,
            TimestampTicks = sample.TimestampTicks,
            Payload = sample.Payload,
        };
        wire.Values.Add(sample.Values);
        var bytes = wire.ToByteArray();
        ControlCodecs.ValidateEncodedLength(bytes.Length);
        return bytes;
    }

    public ControlSample Decode(ReadOnlyMemory<byte> payload)
    {
        ControlCodecs.ValidateEncodedLength(payload.Length);
        var wire = Protobuf.ControlSample.Parser.ParseFrom(payload.ToArray());
        return ControlCodecs.ValidateDecoded(
            new ControlSample(
                wire.Kind,
                wire.MessageId,
                wire.CorrelationId,
                wire.Sequence,
                wire.TimestampTicks,
                wire.Payload,
                wire.Values.ToArray()));
    }
}

public sealed class MessagePackControlCodec : IControlCodec
{
    private static readonly MessagePackSerializerOptions Options =
        MessagePackSerializerOptions.Standard.WithSecurity(MessagePackSecurity.UntrustedData);

    public ControlCodecId Id => ControlCodecId.MessagePack;

    public string Name => "MessagePack 3.1.8";

    public byte[] Encode(ControlSample sample)
    {
        sample.Validate();
        var bytes = MessagePackSerializer.Serialize(
            new MessagePackControlSample
            {
                Kind = sample.Kind,
                MessageId = sample.MessageId,
                CorrelationId = sample.CorrelationId,
                Sequence = sample.Sequence,
                TimestampTicks = sample.TimestampTicks,
                Payload = sample.Payload,
                Values = sample.Values,
            },
            Options);
        ControlCodecs.ValidateEncodedLength(bytes.Length);
        return bytes;
    }

    public ControlSample Decode(ReadOnlyMemory<byte> payload)
    {
        ControlCodecs.ValidateEncodedLength(payload.Length);
        var wire = MessagePackSerializer.Deserialize<MessagePackControlSample>(payload, Options);
        return ControlCodecs.ValidateDecoded(
            new ControlSample(
                wire.Kind,
                wire.MessageId,
                wire.CorrelationId,
                wire.Sequence,
                wire.TimestampTicks,
                wire.Payload,
                wire.Values));
    }
}

public sealed class MemoryPackControlCodec : IControlCodec
{
    public ControlCodecId Id => ControlCodecId.MemoryPack;

    public string Name => "MemoryPack 1.21.4";

    public byte[] Encode(ControlSample sample)
    {
        sample.Validate();
        var bytes = MemoryPackSerializer.Serialize(
            new MemoryPackControlSample
            {
                Kind = sample.Kind,
                MessageId = sample.MessageId,
                CorrelationId = sample.CorrelationId,
                Sequence = sample.Sequence,
                TimestampTicks = sample.TimestampTicks,
                Payload = sample.Payload,
                Values = sample.Values,
            });
        ControlCodecs.ValidateEncodedLength(bytes.Length);
        return bytes;
    }

    public ControlSample Decode(ReadOnlyMemory<byte> payload)
    {
        ControlCodecs.ValidateEncodedLength(payload.Length);
        var wire = MemoryPackSerializer.Deserialize<MemoryPackControlSample>(payload.Span)
            ?? throw new InvalidDataException("MemoryPack returned a null control payload.");
        return ControlCodecs.ValidateDecoded(
            new ControlSample(
                wire.Kind,
                wire.MessageId,
                wire.CorrelationId,
                wire.Sequence,
                wire.TimestampTicks,
                wire.Payload,
                wire.Values));
    }
}
