using System.Buffers.Binary;
using System.Security.Cryptography;

namespace CRNL.HiBoP.Spikes.P06;

public readonly record struct DecodedWireFrame(
    ControlCodecId Codec,
    byte MessageType,
    uint Flags,
    ulong MessageId,
    ReadOnlyMemory<byte> Payload);

public static class WireFrame
{
    public const int HeaderLength = 32;
    public const ushort HeaderVersion = 1;
    public const uint Magic = 0x58504248; // HBPX as little-endian bytes.
    public const int MaximumFrameLength = HeaderLength + ControlCodecs.MaximumEncodedBytes;

    public static byte[] Encode(
        ControlCodecId codec,
        byte messageType,
        uint flags,
        ulong messageId,
        ReadOnlySpan<byte> payload)
    {
        ControlCodecs.ValidateEncodedLength(payload.Length);
        var frame = new byte[HeaderLength + payload.Length];
        var header = frame.AsSpan(0, HeaderLength);
        BinaryPrimitives.WriteUInt32LittleEndian(header, Magic);
        BinaryPrimitives.WriteUInt16LittleEndian(header[4..], HeaderVersion);
        header[6] = (byte)codec;
        header[7] = messageType;
        BinaryPrimitives.WriteUInt32LittleEndian(header[8..], flags);
        BinaryPrimitives.WriteUInt32LittleEndian(header[12..], (uint)payload.Length);
        BinaryPrimitives.WriteUInt64LittleEndian(header[16..], messageId);
        BinaryPrimitives.WriteUInt64LittleEndian(header[24..], ComputeChecksum(payload));
        payload.CopyTo(frame.AsSpan(HeaderLength));
        return frame;
    }

    public static DecodedWireFrame Decode(ReadOnlyMemory<byte> frame)
    {
        if (frame.Length is < HeaderLength or > MaximumFrameLength)
        {
            throw new InvalidDataException("Wire frame length is outside the negotiated limit.");
        }

        var span = frame.Span;
        if (BinaryPrimitives.ReadUInt32LittleEndian(span) != Magic)
        {
            throw new InvalidDataException("Wire frame magic is invalid.");
        }

        if (BinaryPrimitives.ReadUInt16LittleEndian(span[4..]) != HeaderVersion)
        {
            throw new InvalidDataException("Wire frame header version is unsupported.");
        }

        var codec = (ControlCodecId)span[6];
        _ = ControlCodecs.Get(codec);
        var payloadLength = BinaryPrimitives.ReadUInt32LittleEndian(span[12..]);
        if (payloadLength != frame.Length - HeaderLength)
        {
            throw new InvalidDataException("Wire frame payload length does not match the received bytes.");
        }

        var payload = frame[HeaderLength..];
        var expectedChecksum = BinaryPrimitives.ReadUInt64LittleEndian(span[24..]);
        var actualChecksum = ComputeChecksum(payload.Span);
        if (expectedChecksum != actualChecksum)
        {
            throw new InvalidDataException("Wire frame checksum is invalid.");
        }

        return new DecodedWireFrame(
            codec,
            span[7],
            BinaryPrimitives.ReadUInt32LittleEndian(span[8..]),
            BinaryPrimitives.ReadUInt64LittleEndian(span[16..]),
            payload);
    }

    private static ulong ComputeChecksum(ReadOnlySpan<byte> payload)
    {
        using var algorithm = SHA256.Create();
        var hash = algorithm.ComputeHash(payload.ToArray());
        return BinaryPrimitives.ReadUInt64LittleEndian(hash);
    }
}
