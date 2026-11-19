using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using HBP.Core.Data;
using HBP.Transfer.Scene;
using HBP.Transfer.Transport;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace HBP.Tests.Transfer
{
    public sealed class BufferPackTests
    {
        private string root;

        [SetUp]
        public void SetUp()
        {
            root = Path.Combine(Path.GetTempPath(), "hibop-pack-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }

        private static string Identity(byte[] bytes)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant() + ".bin";
        }

        [Test]
        public void DensePackIndexHasItsOwnBoundedMetadataBudget()
        {
            const int packedIndex = 20 * 1024 * 1024;
            const int globalReferences = 1024 * 1024;
            int sceneMetadata = SceneArchive.MaximumMetadataBytes - globalReferences;
            Assert.DoesNotThrow(() => SceneArchive.ValidatePackedMetadataBudget(sceneMetadata, globalReferences, packedIndex));
            Assert.Throws<InvalidDataException>(() => SceneArchive.ValidatePackedMetadataBudget(sceneMetadata + 1, globalReferences, packedIndex));
            Assert.Throws<InvalidDataException>(() => SceneArchive.ValidatePackedMetadataBudget(sceneMetadata, globalReferences, BufferPack.MaximumIndexBytes + 1));
        }

        [Test]
        public void DetachedCaptureKeepsAllReferencesInADenseSceneWithRepeatedBuffers()
        {
            string samplesPath = Path.Combine(root, "source");
            using var archive = new SceneArchive(samplesPath);
            using var writer = new DetachedSceneMetadata();
            writer.WriteStartObject();
            writer.WritePropertyName("Meshes");
            writer.WriteStartArray();
            writer.WriteEndArray();
            writer.WritePropertyName("Samples");
            writer.WriteStartArray();
            byte[] first = { 1, 2, 3, 4 };
            byte[] second = { 5, 6, 7, 8 };
            const int count = 100_001;
            for (int index = 0; index < count; index++)
                writer.WriteBuffer(index % 2 == 0 ? first : second);
            writer.WriteEndArray();
            writer.WriteEndObject();

            var metadata = JObject.Parse(System.Text.Encoding.UTF8.GetString(writer.Encode(archive)));
            var references = (JArray)metadata["Samples"];
            Assert.That(references.Count, Is.EqualTo(count));
            Assert.That(references.Select((reference, index) => (int)reference == index % 2), Is.All.True);
            Assert.That(File.ReadAllBytes(Path.Combine(samplesPath, Identity(first))), Is.EqualTo(first));
            Assert.That(File.ReadAllBytes(Path.Combine(samplesPath, Identity(second))), Is.EqualTo(second));
            Assert.That(Directory.GetFiles(samplesPath), Has.Length.EqualTo(2));
        }

        [TestCase(false, true)]
        [TestCase(true, true)]
        [TestCase(false, false)]
        public async Task DenseSceneTransferRestoresEveryDistinctSignal(bool streamBlocks, bool detach)
        {
            using var source = new SceneArchive(Path.Combine(root, "source"), deferResourceWrites: true) { DetachCapture = detach };
            var payload = PreparedSceneArchiveTests.Fixture(source);
            var values = ((IEEGColumn)payload.Visualization.Columns[1]).Data.ProcessedValuesByChannel;
            for (int index = 0; index < 100_001; index++)
                values.Add("synthetic_" + index, new[] { (float)index, -index - .25f, index + .5f, index + .75f });

            byte[] metadata = detach ? source.CaptureDetachedMetadata(payload).Encode(source) : source.CaptureMetadata(payload);
            var manifest = PreparedSceneManifest.FromMetadata(JObject.Parse(System.Text.Encoding.UTF8.GetString(metadata)));
            using var target = new SceneArchive(Path.Combine(root, "target"), reading: true, globals: source.Globals);
            ScenePayload restored;
            if (streamBlocks)
            {
                using var transport = new MemoryStream();
                BlockContainer.Produce(source.CaptureBlockResources(metadata), packet => transport.Write(packet, 0, packet.Length), CancellationToken.None);
                transport.Position = 4; // The receiver dispatches after reading the block format magic.
                await BlockContainer.ReceiveAsync(transport, target, CancellationToken.None);
                restored = target.ReadPrepared();
            }
            else
            {
                string path = Path.Combine(root, "scene.zip");
                source.WriteCaptured(metadata, path);
                restored = target.Read(path);
            }

            var received = ((IEEGColumn)restored.Visualization.Columns[1]).Data.ProcessedValuesByChannel;
            Assert.That(target.PreparedManifest.VisualizationDescriptorHash, Is.EqualTo(manifest.VisualizationDescriptorHash));
            Assert.That(target.PreparedManifest.Meshes.Select(mesh => mesh.DescriptorHash), Is.EqualTo(manifest.Meshes.Select(mesh => mesh.DescriptorHash)));
            Assert.That(received.Count, Is.EqualTo(values.Count));
            foreach (var signal in values)
            {
                if (!received.TryGetValue(signal.Key, out var samples) || !signal.Value.SequenceEqual(samples))
                    Assert.Fail("Missing or changed signal: " + signal.Key);
            }
        }

        [Test]
        public void StreamedMetadataRejectsDuplicatePropertiesInsideTheVisualization()
        {
            using var reader = new StringReader("{\"Visualization\":{\"ID\":\"first\",\"Columns\":[{\"ID\":\"a\",\"ID\":\"b\"}]}}");
            var error = Assert.Throws<InvalidDataException>(() => PreparedSceneManifest.Read(reader, CancellationToken.None));
            Assert.That(error.Message, Is.EqualTo("Duplicate scene property: ID"));
        }

        [TestCase(3)]
        [TestCase(4)]
        public void LegacyAndPackedScenesKeepValuesCanonicalReferencesAndIndependentArrays(int version)
        {
            using var source = new SceneArchive(Path.Combine(root, "source"));
            var payload = PreparedSceneArchiveTests.Fixture(source);
            payload.Version = version;
            payload.Visualization.Configuration.ErasedTriangles = null;
            payload.Visualization.Configuration.ErasedSimplifiedTriangles = null;
            payload.Columns[5].Functional[0].Values["patient_A1"] = Array.Empty<float>();
            string path = Path.Combine(root, "scene.zip");
            source.Write(payload, path);
            using (var zip = ZipFile.OpenRead(path))
            {
                Assert.That(zip.GetEntry(BufferPack.DataName) != null, Is.EqualTo(version == 4));
                Assert.That(zip.Entries.Any(e => e.Name.EndsWith(".bin")), Is.EqualTo(version == 3));
            }

            using var target = new SceneArchive(Path.Combine(root, "target"), true, source.Globals);
            var restored = target.Read(path);
            var ieeg = (IEEGColumn)restored.Visualization.Columns[1];
            var ccep = (CCEPColumn)restored.Visualization.Columns[2];
            float[] values = ieeg.Data.ProcessedValuesByChannel["patient_A1"];
            float[] trial = ieeg.Data.DataByChannelID["patient_A1"].Trials[0].ChannelSubTrialBySubBloc.Values.Single().Values;
            Assert.That(values, Is.EqualTo(new[] { 1f, 2f, 3f, 4f }));
            Assert.That(trial, Is.EqualTo(values));
            Assert.That(trial, Is.Not.SameAs(values));
            values[0] = 999;
            Assert.That(trial[0], Is.EqualTo(1f));
            Assert.That(ieeg.Bloc, Is.SameAs(ccep.Bloc));
            Assert.That(ieeg.Bloc, Is.SameAs(source.Globals.Data.Protocols.Single().Blocs.Single()));
            Assert.That(restored.Visualization.Configuration.ErasedTriangles, Is.Null);
            Assert.That(restored.Columns[5].Functional[0].Values["patient_A1"], Is.Empty);
            if (version == 4) Assert.That(Directory.GetFiles(target.DirectoryPath).Length, Is.EqualTo(4));
        }

        [Test]
        public void LaterCaptureDoesNotChangeEarlierMetadataReferenceIndices()
        {
            using var source = new SceneArchive(Path.Combine(root, "source"), deferResourceWrites: true);
            var payload = PreparedSceneArchiveTests.Fixture(source);
            var patient = payload.Visualization.Patients[0];
            var first = patient.Tags[0].Tag;
            var second = new StringTag { Name = "second global" };
            var globals = source.Globals.Data;
            globals.Tags = new TagCollection(Array.Empty<BaseTag>(), new BaseTag[] { first, second }, Array.Empty<BaseTag>());
            source.Globals = new PairingContext(globals);
            byte[] firstMetadata = source.CaptureMetadata(payload);
            patient.Tags.Clear();
            patient.Tags.Add(new StringTagValue(second, "changed snapshot"));
            source.CaptureMetadata(payload);
            string path = Path.Combine(root, "scene.zip");
            source.WriteCaptured(firstMetadata, path);
            using var target = new SceneArchive(Path.Combine(root, "target"), true, source.Globals);
            var restored = target.Read(path);
            Assert.That(restored.Visualization.Patients[0].Tags.Single().Tag, Is.SameAs(first));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void HashVerificationCoversEmptyBuffersAndBoundaries(bool corrupt)
        {
            byte[][] resources = { Array.Empty<byte>(), new byte[] { 1, 2, 3 }, Enumerable.Range(0, 70001).Select(i => (byte)i).ToArray(), new byte[] { 8, 9 } };
            var pack = new BufferPack();
            foreach (var bytes in resources) pack.Add(Identity(bytes), bytes.Length);
            byte[] data = resources.SelectMany(v => v).ToArray();
            if (corrupt) data[3] ^= 1;
            using var verifier = new BufferPack.Verifier(pack);

            void Verify()
            {
                for (int offset = 0; offset < data.Length;)
                {
                    byte[] block = data.Skip(offset).Take(65536).ToArray();
                    verifier.Append(block, block.Length, CancellationToken.None);
                    offset += block.Length;
                }

                verifier.Complete();
            }

            if (corrupt) Assert.Throws<InvalidDataException>(Verify);
            else Verify();
        }

        [Test]
        public void ChangedPairingDefinitionsCannotReuseCapturedReferenceIndices()
        {
            using var source = new SceneArchive(Path.Combine(root, "source"), deferResourceWrites: true);
            var payload = PreparedSceneArchiveTests.Fixture(source);
            source.CaptureMetadata(payload);
            payload.Visualization.Patients[0].Tags[0].Tag.Name += " changed";
            source.Globals = new PairingContext(source.Globals.Data);
            Assert.Throws<InvalidDataException>(() => source.CaptureMetadata(payload));
        }

        [TestCase("count")]
        [TestCase("version")]
        [TestCase("negative")]
        [TestCase("overflow")]
        [TestCase("overlap")]
        [TestCase("gap")]
        [TestCase("duplicate")]
        [TestCase("trailing")]
        public void InvalidIndexIsRejectedBeforeOpeningPack(string mutation)
        {
            var pack = new BufferPack();
            pack.Add(Identity(new byte[] { 1, 2, 3 }), 3);
            pack.Add(Identity(new byte[] { 4, 5 }), 2);
            using var stream = new MemoryStream();
            pack.WriteIndex(stream);
            byte[] index = stream.ToArray();
            void Put(long value, int offset) => Buffer.BlockCopy(BitConverter.GetBytes(value), 0, index, offset, 8);
            if (mutation == "count") Buffer.BlockCopy(BitConverter.GetBytes(int.MaxValue), 0, index, 8, 4);
            if (mutation == "version") index[4] = 99;
            if (mutation == "negative") Put(-1, 52);
            if (mutation == "overflow") Put(long.MaxValue, 52);
            if (mutation == "overlap") Put(2, 92);
            if (mutation == "gap") Put(4, 92);
            if (mutation == "duplicate") Buffer.BlockCopy(index, 12, index, 60, 32);
            using var input = new MemoryStream(index);
            Assert.Throws<InvalidDataException>(() => BufferPack.ReadIndex(input, index.Length, mutation == "trailing" ? 6 : 5, CancellationToken.None));
        }

        [Test]
        public void ViewsAreBoundedAndKeepFileAliveAfterArchiveRetirement()
        {
            byte[] first = { 1, 2, 3 }, second = { 4, 5 };
            var pack = new BufferPack();
            pack.Add(Identity(first), first.Length);
            pack.Add(Identity(second), second.Length);
            string path = Path.Combine(root, "buffers.pack");
            File.WriteAllBytes(path, first.Concat(second).ToArray());
            pack.Attach(path);
            var a = pack.Open(0);
            var b = pack.Open(1);
            try
            {
                Assert.That(a.ReadByte(), Is.EqualTo(1));
                Assert.That(b.ReadByte(), Is.EqualTo(4));
                pack.Retire(() => File.Delete(path));
                Assert.Throws<ObjectDisposedException>(() => pack.Open(0));
                Assert.That(a.ReadByte(), Is.EqualTo(2));
                Assert.That(a.ReadByte(), Is.EqualTo(3));
                Assert.That(a.ReadByte(), Is.EqualTo(-1));
                Assert.Throws<IOException>(() => a.Seek(4, SeekOrigin.Begin));
                a.Dispose();
                Assert.That(File.Exists(path), Is.True);
                Assert.That(b.ReadByte(), Is.EqualTo(5));
                Assert.That(b.ReadByte(), Is.EqualTo(-1));
            }
            finally
            {
                a.Dispose();
                b.Dispose();
            }

            Assert.That(File.Exists(path), Is.False);
        }

        [TestCase("missing-pack")]
        [TestCase("missing-index")]
        [TestCase("missing-table")]
        [TestCase("missing-version")]
        [TestCase("duplicate-version")]
        [TestCase("hybrid")]
        [TestCase("wrong-context")]
        [TestCase("wrong-global-hash")]
        [TestCase("duplicate-global")]
        [TestCase("bad-global-index")]
        [TestCase("bad-numeric-index")]
        public void InvalidContainerAndReferencesFailBeforeRestoration(string mutation)
        {
            using var source = new SceneArchive(Path.Combine(root, "source"));
            var payload = PreparedSceneArchiveTests.Fixture(source);
            string path = Path.Combine(root, "scene.zip");
            source.Write(payload, path);
            using (var zip = ZipFile.Open(path, ZipArchiveMode.Update))
            {
                if (mutation == "missing-pack") zip.GetEntry(BufferPack.DataName).Delete();
                else if (mutation == "missing-index") zip.GetEntry(BufferPack.IndexName).Delete();
                else if (mutation == "missing-table") zip.GetEntry(SceneGlobalReferences.FileName).Delete();
                else
                {
                    bool table = mutation is "wrong-context" or "wrong-global-hash" or "duplicate-global";
                    string name = table ? SceneGlobalReferences.FileName : "visualization.json";
                    var entry = zip.GetEntry(name);
                    JObject data;
                    using (var reader = new StreamReader(entry.Open())) data = JObject.Parse(reader.ReadToEnd());
                    if (mutation == "missing-version") data.Remove("Version");
                    if (mutation == "hybrid") data["Version"] = 3;
                    if (mutation == "wrong-context") data["context"] = "other-pairing";
                    if (mutation == "wrong-global-hash") data["references"][0][1] = new string('0', 64);
                    if (mutation == "duplicate-global") ((JArray)data["references"]).Add(data["references"][0].DeepClone());
                    if (mutation == "bad-global-index") data.Descendants().OfType<JProperty>().First(p => p.Name == "Bloc").Value = -1;
                    if (mutation == "bad-numeric-index") data.Descendants().OfType<JProperty>().First(p => p.Name == "ErasedTriangles").Value = long.MaxValue;
                    string json = data.ToString(Formatting.None);
                    if (mutation == "duplicate-version") json = "{\"Version\":4,\"Version\":3," + json.Substring(1);
                    entry.Delete();
                    using var writer = new StreamWriter(zip.CreateEntry(name).Open());
                    writer.Write(json);
                }
            }

            using var target = new SceneArchive(Path.Combine(root, "target"), true, source.Globals);
            Assert.Throws<InvalidDataException>(() => target.Read(path));
        }

        [Test]
        public void CancellationRejectsIndexAndRangeVerification()
        {
            var pack = new BufferPack();
            pack.Add(Identity(new byte[] { 1 }), 1);
            using var index = new MemoryStream();
            pack.WriteIndex(index);
            index.Position = 0;
            var token = new CancellationToken(true);
            Assert.Throws<OperationCanceledException>(() => BufferPack.ReadIndex(index, index.Length, 1, token));
            using var verifier = new BufferPack.Verifier(pack);
            Assert.Throws<OperationCanceledException>(() => verifier.Append(new byte[] { 1 }, 1, token));
        }
    }
}
