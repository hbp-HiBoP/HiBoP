using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HBP.Core.Tools;
using HBP.Transfer.Transport;

namespace HBP.Transfer.Scene
{
    public sealed class SceneDelivery : IDisposable
    {
        private readonly string file;
        private readonly object lifetime = new();
        private int sends;
        private bool disposed;
        public string TransferId { get; }
        public string SessionId { get; }
        public string Summary { get; }
        public string ContentHash { get; }
        public long EncodedBytes { get; }

        public SceneDelivery(string file, string transferId, string sessionId, string summary)
        {
            this.file = file;
            TransferId = transferId;
            SessionId = sessionId;
            Summary = summary;
            EncodedBytes = new FileInfo(file).Length;
            if (EncodedBytes > PinnedTlsTransfer.MaximumSceneFileBytes) throw new InvalidDataException("Visualization exceeds the transfer budget.");
            ContentHash = StandardData.HashFile(file);
        }

        public async Task<DeliveryReceipt> SendAsync(Stream stream, CancellationToken stop, Action<long> progress = null)
        {
            lock (lifetime)
            {
                if (disposed) throw new ObjectDisposedException(nameof(SceneDelivery));
                ++sends;
            }

            try
            {
                return await PinnedTlsTransfer.SendFileAsync(stream, file, stop, progress).ConfigureAwait(false);
            }
            finally
            {
                lock (lifetime)
                {
                    --sends;
                    if (disposed && sends == 0) DeleteFiles();
                }
            }
        }

        public void Dispose()
        {
            lock (lifetime)
            {
                if (disposed) return;
                disposed = true;
                if (sends == 0) DeleteFiles();
            }
        }

        private void DeleteFiles()
        {
            if (File.Exists(file)) File.Delete(file);
            string directory = Path.GetDirectoryName(file);
            if (Directory.Exists(directory) && Directory.GetFileSystemEntries(directory).Length == 0) Directory.Delete(directory);
        }
    }
}
