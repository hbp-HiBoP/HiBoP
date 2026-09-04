using CRNL.HiBoP.Spikes.P06;

namespace CRNL.HiBoP.Spikes.P06.Tests;

public sealed class BufferTests
{
    [Fact]
    public void Float32LittleEndian_RoundTripsExactly()
    {
        float[] values = [0f, -1.25f, float.Epsilon, float.PositiveInfinity, float.NaN];

        var bytes = BufferEncoding.EncodeFloat32(values);
        var decoded = BufferEncoding.DecodeFloat32(bytes);

        Assert.Equal(0x00, bytes[0]);
        Assert.Equal(0x00, bytes[1]);
        Assert.Equal(0x00, bytes[2]);
        Assert.Equal(0x00, bytes[3]);
        Assert.Equal(values.Select(BitConverter.SingleToInt32Bits), decoded.Select(BitConverter.SingleToInt32Bits));
    }

    [Fact]
    public void Float16_ReportsAQuantizedButFiniteRoundTrip()
    {
        float[] values = [-1.25f, -0.1f, 0f, 0.1f, 1.25f];

        var decoded = BufferEncoding.DecodeFloat16(BufferEncoding.EncodeFloat16(values));

        Assert.Equal(values.Length, decoded.Length);
        Assert.InRange(values.Zip(decoded).Max(pair => Math.Abs(pair.First - pair.Second)), 0f, 0.0001f);
    }

    [Fact]
    public void Lz4_RoundTripsFloat32BytesExactly()
    {
        var values = Enumerable.Range(0, 10000).Select(index => MathF.Sin(index * 0.001f)).ToArray();
        var source = BufferEncoding.EncodeFloat32(values);

        var decoded = BufferEncoding.DecompressLz4(BufferEncoding.CompressLz4(source), source.Length);

        Assert.Equal(source, decoded);
    }

    [Fact]
    public void Decoders_RejectMalformedLengths()
    {
        Assert.Throws<InvalidDataException>(() => BufferEncoding.DecodeFloat32([0, 1, 2]));
        Assert.Throws<InvalidDataException>(() => BufferEncoding.DecodeFloat16([0]));
        Assert.Throws<InvalidDataException>(() => BufferEncoding.DecompressLz4([0], 512 * 1024 * 1024 + 1));
    }

    [Fact]
    public void SyntheticAsset_RangesComposeToTheSameHash()
    {
        const int length = (2 * SyntheticAsset.DefaultChunkLength) + 17;
        var all = new byte[length];
        SyntheticAsset.Fill(all, 0);
        var split = new byte[length];
        SyntheticAsset.Fill(split.AsSpan(0, SyntheticAsset.DefaultChunkLength), 0);
        SyntheticAsset.Fill(split.AsSpan(SyntheticAsset.DefaultChunkLength), SyntheticAsset.DefaultChunkLength);

        Assert.Equal(all, split);
        Assert.Equal(Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(all)), SyntheticAsset.ComputeSha256Hex(length));
    }
}
