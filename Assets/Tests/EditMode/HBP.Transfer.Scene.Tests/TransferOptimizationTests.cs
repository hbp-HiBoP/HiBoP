using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using HBP.Core.Tools;
using HBP.Transfer.Scene;
using HBP.Transfer.Transport;
using NUnit.Framework;

namespace HBP.Tests.Transfer
{
    public sealed class TransferOptimizationTests
    {
        private string root;

        [Test]
        public async Task ConcurrentGiftiGeometryAndLabelsRetainTheirData()
        {
            string fixtures = Path.GetFullPath("Assets/Tests/Fixtures/Native");
            string atlasRoot = Path.GetFullPath("Assets/Data/Atlases/MarsAtlas");
            using var atlas = new HBP.Core.DLL.MarsAtlas();
            Assert.That(atlas.Load(Path.Combine(atlasRoot, "mars_atlas_index.csv"), Path.Combine(atlasRoot, "brodmann_areas.txt"), Path.Combine(atlasRoot, "colin27_MNI_MarsAtlas.nii")), Is.True);
            string geometry = Path.Combine(fixtures, "Meshes/single_surface.gii");
            string labels = Path.Combine(root, "labels.gii");
            File.WriteAllText(labels, "<?xml version=\"1.0\" encoding=\"UTF-8\"?><GIFTI Version=\"1.0\" NumberOfDataArrays=\"1\"><MetaData /><LabelTable /><DataArray Intent=\"NIFTI_INTENT_NONE\" DataType=\"NIFTI_TYPE_INT32\" ArrayIndexingOrder=\"RowMajorOrder\" Dimensionality=\"1\" Encoding=\"ASCII\" Endian=\"LittleEndian\" ExternalFileName=\"\" ExternalFileOffset=\"0\" Dim0=\"4\"><MetaData /><Data>1 2 1 2</Data></DataArray></GIFTI>");
            using var expected = new HBP.Core.DLL.Surface();
            Assert.That(expected.LoadGIIFile(geometry), Is.True);
            Assert.That(expected.SearchMarsParcelFileAndUpdateColors(atlas, labels), Is.True);
            var reference = expected.CopyTransferBuffers();
            await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() =>
            {
                for (int iteration = 0; iteration < 8; iteration++)
                {
                    using var actual = new HBP.Core.DLL.Surface();
                    Assert.That(actual.LoadGIIFile(geometry), Is.True);
                    Assert.That(actual.SearchMarsParcelFileAndUpdateColors(atlas, labels), Is.True);
                    var buffers = actual.CopyTransferBuffers();
                    Assert.That(buffers.vertices, Is.EqualTo(reference.vertices));
                    Assert.That(buffers.triangles, Is.EqualTo(reference.triangles));
                    Assert.That(buffers.colors, Is.EqualTo(reference.colors));
                }
            })));
        }

        [SetUp]
        public void SetUp()
        {
            root = Path.Combine(Path.GetTempPath(), "hibop-lot1-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void OnlyFullyVerifiedArchiveCanIssueLeaseAndLeaseRetainsFiles(bool corrupt)
        {
            string source = Path.Combine(root, "source.nii");
            File.WriteAllBytes(source, new byte[] { 1, 2, 3 });
            string hash = StandardData.HashFile(source), name = hash + ".nii";
            string zipPath = Path.Combine(root, "archive.zip");
            using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                using (var writer = new StreamWriter(zip.CreateEntry("globals.json").Open())) writer.Write("{}");
                using (var writer = zip.CreateEntry(name).Open()) writer.Write(new byte[] { 1, 2, (byte)(corrupt ? 4 : 3) }, 0, 3);
            }

            using var archive = new SceneArchive(Path.Combine(root, "received"));
            if (corrupt)
            {
                Assert.Throws<InvalidDataException>(() => archive.ReadGlobalData(zipPath));
                Assert.Throws<InvalidOperationException>(() => archive.AcquireNativeResource(name));
                return;
            }

            archive.ReadGlobalData(zipPath);
            Assert.Throws<InvalidOperationException>(() => archive.AddBuffer(writer => writer.Write(42)));
            var lease = archive.AcquireNativeResource(name);
            try
            {
                Assert.That(lease.Hash, Is.EqualTo(hash));
                Assert.Throws<IOException>(() => File.WriteAllBytes(lease.Path, new byte[] { 9 }));
                archive.Dispose();
                Assert.That(File.Exists(lease.Path), Is.True);
                Assert.Throws<ObjectDisposedException>(() => archive.AcquireNativeResource(name));
            }
            finally
            {
                lease.Dispose();
            }

            Assert.That(Directory.Exists(archive.DirectoryPath), Is.False);
        }

        [Test]
        public void MutableDesktopReferenceIsRehashedEvenWithSameLengthAndTimestamp()
        {
            var property = typeof(ApplicationState).GetProperty("DataPath");
            string original = ApplicationState.DataPath;
            string path = Path.Combine(root, "reference.nii");
            File.WriteAllBytes(path, new byte[] { 1, 2, 3 });
            string hash = StandardData.HashFile(path);
            DateTime stamp = File.GetLastWriteTimeUtc(path);
            try
            {
                property.SetValue(null, root);
                StandardData.ValidateExpectedFile("reference.nii", hash);
                File.WriteAllBytes(path, new byte[] { 4, 5, 6 });
                File.SetLastWriteTimeUtc(path, stamp);
                Assert.Throws<InvalidDataException>(() => StandardData.ValidateExpectedFile("reference.nii", hash));
            }
            finally
            {
                property.SetValue(null, original);
            }
        }

        [Test]
        public void WarmReferenceCacheStillRejectsIncompatibleExpectedHash()
        {
            var flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic;
            var hashField = typeof(StandardData).GetField("s_InstalledHashes", flags);
            var rootField = typeof(StandardData).GetField("s_InstalledRoot", flags);
            object previousHashes = hashField.GetValue(null), previousRoot = rootField.GetValue(null);
            var property = typeof(ApplicationState).GetProperty("DataPath");
            string original = ApplicationState.DataPath;
            string path = Path.Combine(root, "reference.nii");
            File.WriteAllBytes(path, new byte[] { 1, 2, 3 });
            string hash = StandardData.HashFile(path);
            using var guard = new VerifiedResourceScope(() => { });
            guard.Register(path, hash);
            guard.Seal();
            try
            {
                property.SetValue(null, root);
                hashField.SetValue(null, new System.Collections.Generic.Dictionary<string, string> { ["reference.nii"] = hash });
                rootField.SetValue(null, Path.GetFullPath(root));
                StandardData.ValidateExpectedFile("reference.nii", hash);
                Assert.Throws<InvalidDataException>(() => StandardData.ValidateExpectedFile("reference.nii", new string('0', 64)));
            }
            finally
            {
                property.SetValue(null, original);
                hashField.SetValue(null, previousHashes);
                rootField.SetValue(null, previousRoot);
            }
        }

        [Test]
        [Timeout(30000)]
        public async Task PreparedDeliveryResendsSameWireAndCanceledWaitDoesNotReleaseActiveSource()
        {
            string path = Path.Combine(root, "scene.zip");
            byte[] data = Enumerable.Range(0, PinnedTlsTransfer.ChunkBytes + 5).Select(i => (byte)i).ToArray();
            File.WriteAllBytes(path, data);
            using var sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(data);
            using var delivery = new SceneDelivery(path, "test", "session", "Small");
            Assert.Throws<IOException>(() => File.WriteAllBytes(path, new byte[] { 0 }));
            using var first = new ReceiptStream(hash, true);
            Task<DeliveryReceipt> send = delivery.SendAsync(first, CancellationToken.None);
            await first.ReceiptRequested.Task;
            using var canceled = new CancellationTokenSource();
            using var second = new ReceiptStream(hash, false);
            Task<DeliveryReceipt> waiting = delivery.SendAsync(second, canceled.Token);
            canceled.Cancel();
            Exception failure = null;
            try
            {
                await waiting;
            }
            catch (Exception e)
            {
                failure = e;
            }

            Assert.That(failure, Is.InstanceOf<OperationCanceledException>());
            first.AllowReceipt.TrySetResult(true);
            Assert.That((await send).Status, Is.EqualTo(DeliveryStatus.Published));
            await delivery.SendAsync(second, CancellationToken.None);
            Assert.That(second.ToArray(), Is.EqualTo(first.ToArray()));
            using var third = new ReceiptStream(hash, true);
            Task<DeliveryReceipt> final = delivery.SendAsync(third, CancellationToken.None);
            await third.ReceiptRequested.Task;
            delivery.Dispose();
            Assert.That(File.Exists(path), Is.True);
            third.AllowReceipt.TrySetResult(true);
            await final;
            Assert.That(File.Exists(path), Is.False);
        }

        private sealed class ReceiptStream : MemoryStream
        {
            private readonly byte[] receipt;
            public readonly TaskCompletionSource<bool> ReceiptRequested = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public readonly TaskCompletionSource<bool> AllowReceipt = new(TaskCreationOptions.RunContinuationsAsynchronously);

            public ReceiptStream(byte[] hash, bool wait)
            {
                receipt = new byte[33];
                receipt[0] = (byte)DeliveryStatus.Published;
                Buffer.BlockCopy(hash, 0, receipt, 1, 32);
                if (!wait) AllowReceipt.SetResult(true);
            }

            public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token)
            {
                ReceiptRequested.TrySetResult(true);
                await AllowReceipt.Task;
                token.ThrowIfCancellationRequested();
                Buffer.BlockCopy(receipt, 0, buffer, offset, receipt.Length);
                return receipt.Length;
            }
        }
    }
}
