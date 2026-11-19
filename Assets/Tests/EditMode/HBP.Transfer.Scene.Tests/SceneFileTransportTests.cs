using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using HBP.Transfer.Transport;
using NUnit.Framework;

namespace HBP.Tests.Transfer
{
    public sealed class SceneFileTransportTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public async Task FileChunksAreVerifiedBeforePublicationAndAcknowledgment(bool corrupt)
        {
            string file = Path.Combine(Path.GetTempPath(), "hibop-wire-" + Guid.NewGuid().ToString("N"));
            byte[] data = Enumerable.Range(0, PinnedTlsTransfer.ChunkBytes + 17).Select(i => (byte)i).ToArray();
            using var stream = new MemoryStream();
            using var sha = SHA256.Create();
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
            {
                writer.Write(new byte[] { (byte)'H', (byte)'B', (byte)'T', 3 });
                writer.Write((long)data.Length);
                writer.Write(sha.ComputeHash(data));
                int index = 0;
                for (int offset = 0; offset < data.Length; offset += PinnedTlsTransfer.ChunkBytes)
                {
                    int count = Math.Min(PinnedTlsTransfer.ChunkBytes, data.Length - offset);
                    writer.Write(index++);
                    writer.Write(count);
                    byte[] hash = sha.ComputeHash(data, offset, count);
                    if (corrupt) hash[0] ^= 1;
                    writer.Write(hash);
                    writer.Write(data, offset, count);
                }
            }

            long requestLength = stream.Length;
            stream.Position = 0;
            bool published = false;
            Exception failure = null;
            try
            {
                try
                {
                    await PinnedTlsTransfer.ReceiveFileAsync(stream, file, CancellationToken.None, (path, hash, stop) =>
                    {
                        Assert.That(File.ReadAllBytes(path), Is.EqualTo(data));
                        Assert.That(stream.Length, Is.EqualTo(requestLength), "Receipt must follow publication.");
                        published = true;
                        return Task.FromResult(DeliveryStatus.Published);
                    });
                }
                catch (Exception exception)
                {
                    failure = exception;
                }

                if (corrupt)
                {
                    Assert.That(failure, Is.TypeOf<InvalidDataException>());
                    Assert.That(published, Is.False);
                    Assert.That(stream.Length, Is.EqualTo(requestLength));
                }
                else
                {
                    Assert.That(failure, Is.Null);
                    Assert.That(published, Is.True);
                    Assert.That(stream.Length, Is.EqualTo(requestLength + 33));
                }
            }
            finally
            {
                if (File.Exists(file)) File.Delete(file);
            }
        }
    }
}
