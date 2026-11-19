using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HBP.Sync;

namespace HBP.Transfer.Transport
{
    /// <summary>Authenticated routing preamble; the existing HBT2/HBS2/HBR3 protocols are unchanged.</summary>
    public static class ReplicaRoute
    {
        public const string Magic = "HBM2";

        public static async Task WriteAsync(Stream stream, SessionId session, SceneId scene, IncarnationId incarnation, CancellationToken token)
        {
            var bytes = new byte[52];
            Encoding.ASCII.GetBytes(Magic).CopyTo(bytes, 0);
            session.Value.ToByteArray().CopyTo(bytes, 4);
            scene.Value.ToByteArray().CopyTo(bytes, 20);
            incarnation.Value.ToByteArray().CopyTo(bytes, 36);
            await stream.WriteAsync(bytes, 0, bytes.Length, token).ConfigureAwait(false);
        }

        public static async Task<(SessionId Session, SceneId Scene, IncarnationId Incarnation)> ReadBodyAsync(Stream stream, CancellationToken token)
        {
            var bytes = new byte[48];
            await PinnedTlsTransfer.ReadExactAsync(stream, bytes, 0, bytes.Length, token).ConfigureAwait(false);

            Guid Read(int offset)
            {
                var id = new byte[16];
                Buffer.BlockCopy(bytes, offset, id, 0, 16);
                return new Guid(id);
            }

            return (new SessionId(Read(0)), new SceneId(Read(16)), new IncarnationId(Read(32)));
        }
    }
}
