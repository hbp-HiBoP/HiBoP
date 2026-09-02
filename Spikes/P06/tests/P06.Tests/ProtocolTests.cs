using System.Security.Cryptography;
using System.Text.Json;
using CRNL.HiBoP.Spikes.P06;

namespace CRNL.HiBoP.Spikes.P06.Tests;

public sealed class ProtocolTests
{
    public static IEnumerable<object[]> Codecs() =>
        ControlCodecs.All.Select(codec => new object[] { codec });

    [Theory]
    [MemberData(nameof(Codecs))]
    public void CodecRoundTrip_PreservesTheBoundedControlSample(IControlCodec codec)
    {
        var sample = CreateSample();

        var decoded = codec.Decode(codec.Encode(sample));

        Assert.Equal(sample with { Values = [] }, decoded with { Values = [] });
        Assert.Equal(sample.Values, decoded.Values);
    }

    [Theory]
    [MemberData(nameof(Codecs))]
    public void CodecDecode_RejectsPayloadAboveTheEnvelopeLimit(IControlCodec codec)
    {
        var oversized = new byte[ControlCodecs.MaximumEncodedBytes + 1];

        Assert.Throws<InvalidDataException>(() => codec.Decode(oversized));
    }

    [Fact]
    public void WireFrameRoundTrip_PreservesHeaderAndPayload()
    {
        var codec = ControlCodecs.Get(ControlCodecId.Protobuf);
        var payload = codec.Encode(CreateSample());
        var bytes = WireFrame.Encode(codec.Id, 7, 3, 42, payload);

        var decoded = WireFrame.Decode(bytes);

        Assert.Equal(codec.Id, decoded.Codec);
        Assert.Equal(7, decoded.MessageType);
        Assert.Equal(3u, decoded.Flags);
        Assert.Equal(42ul, decoded.MessageId);
        Assert.Equal(payload, decoded.Payload.ToArray());
    }

    [Fact]
    public void WireFrameDecode_RejectsCorruptionBeforeCodecAllocation()
    {
        var codec = ControlCodecs.Get(ControlCodecId.Protobuf);
        var bytes = WireFrame.Encode(codec.Id, 1, 0, 1, codec.Encode(CreateSample()));
        bytes[^1] ^= 0xff;

        Assert.Throws<InvalidDataException>(() => WireFrame.Decode(bytes));
    }

    [Fact]
    public void WireFrameDecode_RejectsDeclaredLengthMismatch()
    {
        var codec = ControlCodecs.Get(ControlCodecId.Protobuf);
        var bytes = WireFrame.Encode(codec.Id, 1, 0, 1, codec.Encode(CreateSample()));
        bytes[12]++;

        Assert.Throws<InvalidDataException>(() => WireFrame.Decode(bytes));
    }

    [Fact]
    public void CertificateSasAndPin_AreStableForTheSameCertificate()
    {
        using var certificate = CertificateIdentity.CreateEphemeral("127.0.0.1");
        var first = CertificateIdentity.ComputeSpkiPin(certificate);
        var second = CertificateIdentity.ComputeSpkiPin(certificate);

        Assert.True(CertificateIdentity.PinsMatch(first, second));
        Assert.Matches("^[0-9]{6}$", CertificateIdentity.ComputeShortAuthenticationString(first));
    }

    [Fact]
    public void PinComparison_RejectsChangedIdentity()
    {
        var first = RandomNumberGenerator.GetBytes(SHA256.HashSizeInBytes);
        var second = first.ToArray();
        second[^1] ^= 0xff;

        Assert.False(CertificateIdentity.PinsMatch(first, second));
    }

    [Fact]
    public void GoldenWireVectors_AreByteStableAndDecodable()
    {
        using var document = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "control-sample-v1.json")));
        var vectors = document.RootElement.GetProperty("vectors").EnumerateArray().ToArray();

        Assert.Equal(ControlCodecs.All.Count, vectors.Length);
        foreach (var vector in vectors)
        {
            var frame = Convert.FromBase64String(vector.GetProperty("frameBase64").GetString()!);
            var expectedHash = vector.GetProperty("frameSha256").GetString();
            Assert.Equal(expectedHash, Convert.ToHexStringLower(SHA256.HashData(frame)));
            var decodedFrame = WireFrame.Decode(frame);
            var sample = ControlCodecs.Get(decodedFrame.Codec).Decode(decodedFrame.Payload);
            Assert.Equal(0x0102030405060708ul, sample.MessageId);
        }
    }

    private static ControlSample CreateSample() =>
        new(1, 2, 3, 4, 5, "bounded", [-1, 0, 1]);
}
