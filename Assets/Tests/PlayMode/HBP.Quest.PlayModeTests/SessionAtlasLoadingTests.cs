using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Core.Object3D;
using HBP.Core.Tools;
using NUnit.Framework;

namespace HBP.Tests.Quest
{
    public sealed class SessionAtlasLoadingTests
    {
        [Test]
        public async Task InstalledLocalizerDoubleLoadSharesNativeWorkAndCancelledConsumerDoesNotUnloadIt()
        {
            const string id = "localizer:AUDI";
            string folder = Path.Combine(Path.GetTempPath(), "hibop-localizer-test-" + Guid.NewGuid().ToString("N"));
            var dataPath = typeof(ApplicationState).GetProperty(nameof(ApplicationState.DataPath));
            string previousPath = ApplicationState.DataPath;
            var previousLocalizers = Object3DManager.Localizers;
            int loads = 0;

            void Loading(AtlasLoadResult result)
            {
                if (result.Id == id && result.State == AtlasLoadState.Loading) loads++;
            }

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();
            try
            {
                string directory = Path.Combine(folder, "Atlases", "Localizers", "AUDI", "test");
                Directory.CreateDirectory(directory);
                WriteVolume(Path.Combine(directory, "bloc.nii"));
                dataPath.SetValue(null, folder);
                Object3DManager.Localizers = new LocalizersObjects();
                AtlasResources.Changed += Loading;
                var first = AtlasResources.LoadAsync(id, timeout.Token).AsTask();
                var second = AtlasResources.LoadAsync(id, timeout.Token).AsTask();
                var abandoned = AtlasResources.LoadAsync(id, cancelled.Token).AsTask();
                Assert.Throws<InvalidOperationException>(() => ResourceRetention.ReserveRelease(id));
                Assert.That((await first).State, Is.EqualTo(AtlasLoadState.Loaded));
                Assert.That((await second).State, Is.EqualTo(AtlasLoadState.Loaded));
                Assert.That((await abandoned).State, Is.EqualTo(AtlasLoadState.Cancelled));
                Assert.That(loads, Is.EqualTo(1));
                Assert.That(Object3DManager.Localizers.Protocols.Count, Is.EqualTo(1));
                Assert.That(AtlasResources.IsLoaded(id), Is.True);
                var native = Object3DManager.Localizers.Protocols[0];
                string volume = Path.Combine(directory, "bloc.nii");
                using (var changed = new FileStream(volume, FileMode.Open, FileAccess.Write))
                {
                    changed.Position = 352;
                    changed.WriteByte(99);
                }
                Assert.That((await AtlasResources.LoadAsync(id, timeout.Token)).State, Is.EqualTo(AtlasLoadState.Failed));
                Assert.That((await AtlasResources.LoadAsync(id, timeout.Token)).State, Is.EqualTo(AtlasLoadState.Failed), "A failed verification must not relabel the old native atlas on retry.");
                Assert.That(AtlasResources.Status(id).Fingerprint, Is.Null);
                Assert.That(Object3DManager.Localizers.Protocols[0], Is.SameAs(native));
                using (ResourceRetention.Retain(id)) Assert.Throws<InvalidOperationException>(() => AtlasResources.Unload(id));
                Assert.That(AtlasResources.Unload(id).State, Is.EqualTo(AtlasLoadState.Unloaded));
                Assert.That(AtlasResources.IsLoaded(id), Is.False);
                Assert.That((await AtlasResources.LoadAsync(id, cancelled.Token)).State, Is.EqualTo(AtlasLoadState.Cancelled));
                Assert.That(AtlasResources.IsLoaded(id), Is.False);
                Assert.That((await AtlasResources.LoadAsync("localizer:VISU", timeout.Token)).State, Is.EqualTo(AtlasLoadState.Failed));
            }
            finally
            {
                AtlasResources.Changed -= Loading;
                Object3DManager.Localizers.Clean();
                Object3DManager.Localizers = previousLocalizers;
                dataPath.SetValue(null, previousPath);
                if (Directory.Exists(folder)) Directory.Delete(folder, true);
            }
        }

        private static void WriteVolume(string path)
        {
            using var stream = new FileStream(path, FileMode.CreateNew);
            using var writer = new BinaryWriter(stream);
            stream.SetLength(376);
            writer.Write(348);
            stream.Position = 40;
            writer.Write((short)3);
            writer.Write((short)2);
            writer.Write((short)3);
            writer.Write((short)4);
            stream.Position = 70;
            writer.Write((short)2);
            writer.Write((short)8);
            stream.Position = 80;
            writer.Write(1f);
            writer.Write(2f);
            writer.Write(3f);
            stream.Position = 108;
            writer.Write(352f);
            stream.Position = 344;
            writer.Write(new byte[] { (byte)'n', (byte)'+', (byte)'1', 0 });
            stream.Position = 352;
            writer.Write(Enumerable.Range(0, 24).Select(i => (byte)i).ToArray());
        }
    }
}
