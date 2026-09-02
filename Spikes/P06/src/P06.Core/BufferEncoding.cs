using System.Buffers.Binary;
using K4os.Compression.LZ4;

namespace CRNL.HiBoP.Spikes.P06;

public static class BufferEncoding
{
    public static byte[] EncodeFloat32(ReadOnlySpan<float> values)
    {
        var bytes = new byte[checked(values.Length * sizeof(float))];
        for (var index = 0; index < values.Length; index++)
        {
            BinaryPrimitives.WriteInt32LittleEndian(
                bytes.AsSpan(index * sizeof(float), sizeof(float)),
                BitConverter.SingleToInt32Bits(values[index]));
        }

        return bytes;
    }

    public static float[] DecodeFloat32(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length % sizeof(float) != 0)
        {
            throw new InvalidDataException("A float32 buffer length must be divisible by four.");
        }

        var values = new float[bytes.Length / sizeof(float)];
        for (var index = 0; index < values.Length; index++)
        {
            values[index] = BitConverter.Int32BitsToSingle(
                BinaryPrimitives.ReadInt32LittleEndian(bytes.Slice(index * sizeof(float), sizeof(float))));
        }

        return values;
    }

    public static byte[] EncodeFloat16(ReadOnlySpan<float> values)
    {
        var bytes = new byte[checked(values.Length * sizeof(ushort))];
        for (var index = 0; index < values.Length; index++)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(
                bytes.AsSpan(index * sizeof(ushort), sizeof(ushort)),
                BitConverter.HalfToUInt16Bits((Half)values[index]));
        }

        return bytes;
    }

    public static float[] DecodeFloat16(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length % sizeof(ushort) != 0)
        {
            throw new InvalidDataException("A float16 buffer length must be divisible by two.");
        }

        var values = new float[bytes.Length / sizeof(ushort)];
        for (var index = 0; index < values.Length; index++)
        {
            values[index] = (float)BitConverter.UInt16BitsToHalf(
                BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(index * sizeof(ushort), sizeof(ushort))));
        }

        return values;
    }

    public static byte[] CompressLz4(ReadOnlySpan<byte> source)
    {
        var destination = new byte[LZ4Codec.MaximumOutputSize(source.Length)];
        var encodedLength = LZ4Codec.Encode(source, destination, LZ4Level.L00_FAST);
        if (encodedLength <= 0)
        {
            throw new InvalidDataException("LZ4 failed to encode the buffer.");
        }

        return destination.AsSpan(0, encodedLength).ToArray();
    }

    public static byte[] DecompressLz4(ReadOnlySpan<byte> source, int decodedLength)
    {
        if (decodedLength is < 0 or > 512 * 1024 * 1024)
        {
            throw new InvalidDataException("LZ4 decoded length is outside the spike limit.");
        }

        var destination = new byte[decodedLength];
        var actualLength = LZ4Codec.Decode(source, destination);
        if (actualLength != decodedLength)
        {
            throw new InvalidDataException("LZ4 decoded length does not match the descriptor.");
        }

        return destination;
    }
}
