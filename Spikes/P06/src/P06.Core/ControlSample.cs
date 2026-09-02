using MemoryPack;
using MessagePack;

namespace CRNL.HiBoP.Spikes.P06;

public sealed record ControlSample(
    uint Kind,
    ulong MessageId,
    ulong CorrelationId,
    uint Sequence,
    long TimestampTicks,
    string Payload,
    int[] Values)
{
    public const int MaximumPayloadCharacters = 4096;
    public const int MaximumValues = 1024;

    public void Validate()
    {
        if (Payload is null)
        {
            throw new ArgumentNullException(nameof(Payload));
        }

        if (Values is null)
        {
            throw new ArgumentNullException(nameof(Values));
        }

        if (Kind == 0)
        {
            throw new InvalidDataException("Control kind 0 is reserved for Unknown.");
        }

        if (Payload.Length > MaximumPayloadCharacters)
        {
            throw new InvalidDataException("Control payload exceeds the character limit.");
        }

        if (Values.Length > MaximumValues)
        {
            throw new InvalidDataException("Control values exceed the collection limit.");
        }
    }
}

[MessagePackObject]
public sealed class MessagePackControlSample
{
    [Key(0)]
    public uint Kind { get; set; }

    [Key(1)]
    public ulong MessageId { get; set; }

    [Key(2)]
    public ulong CorrelationId { get; set; }

    [Key(3)]
    public uint Sequence { get; set; }

    [Key(4)]
    public long TimestampTicks { get; set; }

    [Key(5)]
    public string Payload { get; set; } = string.Empty;

    [Key(6)]
    public int[] Values { get; set; } = Array.Empty<int>();
}

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial class MemoryPackControlSample
{
    [MemoryPackOrder(0)]
    public uint Kind { get; set; }

    [MemoryPackOrder(1)]
    public ulong MessageId { get; set; }

    [MemoryPackOrder(2)]
    public ulong CorrelationId { get; set; }

    [MemoryPackOrder(3)]
    public uint Sequence { get; set; }

    [MemoryPackOrder(4)]
    public long TimestampTicks { get; set; }

    [MemoryPackOrder(5)]
    public string Payload { get; set; } = string.Empty;

    [MemoryPackOrder(6)]
    public int[] Values { get; set; } = Array.Empty<int>();
}
