using System.Security.Cryptography;
using System.Text.Json;
using CRNL.HiBoP.Spikes.P06;

if (args.Length != 1)
{
    throw new ArgumentException("Expected the fixture output path as the only argument.");
}

var sample = new ControlSample(
    1,
    0x0102030405060708,
    0x1112131415161718,
    42,
    638923456789012345,
    new string('x', 256),
    Enumerable.Range(-16, 33).ToArray());
var vectors = ControlCodecs.All.Select(
    codec =>
    {
        var payload = codec.Encode(sample);
        var frame = WireFrame.Encode(codec.Id, 1, 0, sample.MessageId, payload);
        return new
        {
            codec = codec.Id.ToString(),
            codecName = codec.Name,
            payloadBytes = payload.Length,
            frameBase64 = Convert.ToBase64String(frame),
            frameSha256 = Convert.ToHexStringLower(SHA256.HashData(frame)),
        };
    }).ToArray();
var fixture = new
{
    schemaVersion = 1,
    headerVersion = WireFrame.HeaderVersion,
    sample = new
    {
        sample.Kind,
        sample.MessageId,
        sample.CorrelationId,
        sample.Sequence,
        sample.TimestampTicks,
        sample.Payload,
        sample.Values,
    },
    vectors,
};
var output = Path.GetFullPath(args[0]);
Directory.CreateDirectory(Path.GetDirectoryName(output)!);
await File.WriteAllTextAsync(
    output,
    JsonSerializer.Serialize(fixture, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true }));
Console.WriteLine(output);
