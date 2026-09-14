using System;
using System.IO;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Agreement.JPake;
using Org.BouncyCastle.Math;

namespace HBP.Transfer.Transport
{
    /// <summary>J-PAKE NIST-3072/SHA-256, including mutual key confirmation.
    /// Proof-bound participant IDs bind both roles to the actual TLS certificate.
    /// No password, credential or application data is sent before confirmation.</summary>
    public static class PairingPake
    {
        public static async Task AuthenticateAsync(Stream stream, string code, byte[] certificateHash, bool server, CancellationToken stop)
        {
            if (code == null || code.Length != 6 || Array.Exists(code.ToCharArray(), c => c < '0' || c > '9'))
                throw new ArgumentException("Enter the six-digit code shown in the headset.");
            if (certificateHash == null || certificateHash.Length != 32) throw new ArgumentException("Missing TLS identity.");
            string binding = "HiBoP-JPAKE-v1:" + Convert.ToBase64String(certificateHash) + ":";
            string localId = binding + (server ? "quest:" : "desktop:") + Guid.NewGuid().ToString("N");
            string remotePrefix = binding + (server ? "desktop:" : "quest:");
            char[] password = code.ToCharArray();
            var participant = new JPakeParticipant(localId, password);
            Array.Clear(password, 0, password.Length);
            try
            {
                // Callers execute this on a worker: modular exponentiation must not block Unity frames.
                var first = participant.CreateRound1PayloadToSend();
                await WriteAsync(stream, writer =>
                {
                    writer.Write(first.ParticipantId);
                    WriteInteger(writer, first.Gx1);
                    WriteInteger(writer, first.Gx2);
                    foreach (var n in first.KnowledgeProofForX1) WriteInteger(writer, n);
                    foreach (var n in first.KnowledgeProofForX2) WriteInteger(writer, n);
                }, stop).ConfigureAwait(false);
                using (var reader = await ReadAsync(stream, stop).ConfigureAwait(false))
                {
                    string id = ReadId(reader);
                    if (!id.StartsWith(remotePrefix, StringComparison.Ordinal) || id.Length != remotePrefix.Length + 32 || !Guid.TryParseExact(id.Substring(remotePrefix.Length), "N", out _))
                        throw new AuthenticationException("Pairing channel identity mismatch.");
                    var remote = new JPakeRound1Payload(id, ReadInteger(reader), ReadInteger(reader), new[] { ReadInteger(reader), ReadInteger(reader) }, new[] { ReadInteger(reader), ReadInteger(reader) });
                    End(reader);
                    participant.ValidateRound1PayloadReceived(remote);
                }

                var second = participant.CreateRound2PayloadToSend();
                await WriteAsync(stream, writer =>
                {
                    writer.Write(second.ParticipantId);
                    WriteInteger(writer, second.A);
                    foreach (var n in second.KnowledgeProofForX2s) WriteInteger(writer, n);
                }, stop).ConfigureAwait(false);
                using (var reader = await ReadAsync(stream, stop).ConfigureAwait(false))
                {
                    var remote = new JPakeRound2Payload(ReadId(reader), ReadInteger(reader), new[] { ReadInteger(reader), ReadInteger(reader) });
                    End(reader);
                    participant.ValidateRound2PayloadReceived(remote);
                }

                var key = participant.CalculateKeyingMaterial();
                var third = participant.CreateRound3PayloadToSend(key);
                await WriteAsync(stream, writer =>
                {
                    writer.Write(third.ParticipantId);
                    WriteInteger(writer, third.MacTag);
                }, stop).ConfigureAwait(false);
                using (var reader = await ReadAsync(stream, stop).ConfigureAwait(false))
                {
                    var remote = new JPakeRound3Payload(ReadId(reader), ReadInteger(reader));
                    End(reader);
                    participant.ValidateRound3PayloadReceived(remote, key);
                }
            }
            catch (CryptoException exception)
            {
                throw new AuthenticationException("Incorrect or expired pairing code.", exception);
            }
        }

        private static string ReadId(BinaryReader reader)
        {
            // IDs in v1 fit in a single-byte BinaryWriter string length. Reject
            // multi-byte/oversized lengths before allocating or decoding text.
            int length = reader.ReadByte();
            if (length < 1 || length > 127) throw new InvalidDataException("Invalid pairing participant identity.");
            byte[] bytes = reader.ReadBytes(length);
            if (bytes.Length != length) throw new EndOfStreamException();
            return new UTF8Encoding(false, true).GetString(bytes);
        }

        private static void End(BinaryReader reader)
        {
            if (reader.BaseStream.Position != reader.BaseStream.Length) throw new InvalidDataException("Trailing pairing data.");
        }

        private static void WriteInteger(BinaryWriter writer, BigInteger value)
        {
            byte[] bytes = value.ToByteArray(); // Signed encoding also preserves BouncyCastle's round-3 MAC.
            writer.Write(bytes.Length);
            writer.Write(bytes);
        }

        private static BigInteger ReadInteger(BinaryReader reader)
        {
            int length = reader.ReadInt32();
            if (length < 1 || length > 385) throw new InvalidDataException("Invalid pairing integer.");
            byte[] value = reader.ReadBytes(length);
            if (value.Length != length) throw new EndOfStreamException();
            return new BigInteger(value);
        }

        private static async Task WriteAsync(Stream stream, Action<BinaryWriter> encode, CancellationToken stop)
        {
            using var buffer = new MemoryStream();
            using (var writer = new BinaryWriter(buffer, Encoding.UTF8, true)) encode(writer);
            byte[] payload = buffer.ToArray();
            byte[] size = BitConverter.GetBytes(payload.Length);
            await stream.WriteAsync(size, 0, size.Length, stop).ConfigureAwait(false);
            await stream.WriteAsync(payload, 0, payload.Length, stop).ConfigureAwait(false);
        }

        private static async Task<BinaryReader> ReadAsync(Stream stream, CancellationToken stop)
        {
            var header = new byte[4];
            await PinnedTlsTransfer.ReadExactAsync(stream, header, 0, 4, stop).ConfigureAwait(false);
            int size = BitConverter.ToInt32(header, 0);
            if (size < 1 || size > 4096) throw new InvalidDataException("Invalid pairing frame.");
            var payload = new byte[size];
            await PinnedTlsTransfer.ReadExactAsync(stream, payload, 0, size, stop).ConfigureAwait(false);
            return new BinaryReader(new MemoryStream(payload, false), Encoding.UTF8);
        }
    }
}
