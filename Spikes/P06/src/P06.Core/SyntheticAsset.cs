using System.Security.Cryptography;

namespace CRNL.HiBoP.Spikes.P06;

public static class SyntheticAsset
{
    public const long DefaultLength = 100L * 1024 * 1024;
    public const int DefaultChunkLength = 1024 * 1024;

    public static void Fill(Span<byte> destination, long offset)
    {
        for (var index = 0; index < destination.Length; index++)
        {
            destination[index] = unchecked((byte)(((offset + index) * 31) + 17));
        }
    }

    public static string ComputeSha256Hex(long length)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[DefaultChunkLength];
        for (long offset = 0; offset < length; offset += buffer.Length)
        {
            var count = (int)Math.Min(buffer.Length, length - offset);
            Fill(buffer.AsSpan(0, count), offset);
            hash.AppendData(buffer, 0, count);
        }

        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }
}
