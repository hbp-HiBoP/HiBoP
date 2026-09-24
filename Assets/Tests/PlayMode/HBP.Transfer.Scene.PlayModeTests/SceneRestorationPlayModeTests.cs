#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Enums;
using HBP.Core.Object3D;
using HBP.Core.Preferences;
using HBP.Core.Tools;
using HBP.Data.Module3D;
using HBP.Quest;
using HBP.Sync;
using HBP.Sync.Scene;
using HBP.Tests.PlayMode.Utilities;
using HBP.Transfer.Scene;
using HBP.Transfer.Transport;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using Event = HBP.Core.Data.Event;
using ProjectPreferences = HBP.Core.Data.ProjectPreferences;

namespace HBP.Tests.SceneTransfer
{
    public class SceneRestorationPlayModeTests
    {
        [Test]
        [Timeout(180000)]
        public Task S2_PreparedMeshManifestBindsAfterTwoSceneOpening() => VerifyPreparedMeshAndConfigurationAsync(false);

        [Test]
        [Timeout(180000)]
        public Task S2_LiveDesktopCaptureBindsDeliveredQuestScene() => VerifyPreparedMeshAndConfigurationAsync(true);

        [Test]
        [Timeout(180000)]
        public Task S3_ReplicaStreamAppliesDesktopStateWithoutReplacingQuestPresentation() => VerifyPreparedMeshAndConfigurationAsync(false, true);

        [Test, Explicit("Requires a paired Quest running HiBoP and USB forwarding on port 45871.")]
        [Timeout(240000)]
        public async Task S3_PhysicalQuestReceivesVisibleRevisions()
        {
            string root = Path.Combine(Path.GetTempPath(), "hibop-s3-device-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            using var settings = new PlayModePersistentDataScope(root);
            using var scope = new PlayModeSceneScope("S3PhysicalQuest");
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(4));
            CancellationToken token = timeout.Token;
            RestoredScene desktop = null;
            byte[] credential = null;
            try
            {
                await PrepareReferencesAsync();
                using var source = new SceneArchive(Path.Combine(root, "source"));
                ScenePayload fixture = CreateFixture(source);
                fixture.TransferId = Guid.NewGuid().ToString("N");
                fixture.SessionId = Guid.NewGuid().ToString("N");
                string fixtureFile = Path.Combine(root, "fixture.hbscene");
                source.Write(fixture, fixtureFile);
                var desktopArchive = new SceneArchive(Path.Combine(root, "desktop"), true, source.Globals);
                desktop = await SceneRestoration.PrepareAsync(desktopArchive.Read(fixtureFile), desktopArchive, AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/3D/Scenes/Scene 3D.prefab").GetComponent<Base3DScene>(), scope.Root.transform, token);

                await desktop.Scene.PrepareRenderingAsync(token);

                using var globalArchive = new SceneArchive(Path.Combine(root, "globals"), globals: source.Globals);
                source.Globals.CaptureFilterPresets(PersistentDataManager.FilterConditionsPresets, globalArchive);
                string globalsFile = Path.Combine(root, "globals.hbglobal");
                globalArchive.WriteGlobalData(source.Globals.Data, globalsFile);
                using var globalsDelivery = new SceneDelivery(globalsFile, source.Globals.Id, source.Globals.Id, "S3 device globals");
                using var delivery = await DesktopSceneCapture.CaptureDeliveryAsync(desktop.Scene, Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N"), 1, source.Globals, token);

                const string host = "127.0.0.1";
                QuestDevice device = await QuestPairing.DescribeAsync(host, token);
                string pairingPath = Path.Combine(Application.persistentDataPath, "QuestPairings", BitConverter.ToString(device.Pin).Replace("-", "") + ".pair");
                credential = PairingStorage.Read(pairingPath);
                Assert.That(credential, Has.Length.EqualTo(32), "The existing Quest pairing must be available to this Editor.");
                await QuestPairing.ResumeAsync(host, device.Pin, credential, source.Globals.Id, token, (stream, stop) => globalsDelivery.SendAsync(stream, stop));
                DeliveryReceipt receipt = await QuestPairing.SendAsync(host, device.Pin, credential, token, (stream, stop) => delivery.SendAsync(stream, stop));
                Assert.That(receipt.Status, Is.EqualTo(DeliveryStatus.Published));

                var adapter = new LiveGeometryStateAdapter(desktop.Scene, Guid.NewGuid(), PreparedSceneDeliveryBinding.FromSent(delivery, receipt));
                StateSnapshot initial = adapter.Capture(1);
                adapter.BindInitialState(initial);
                float firstClock = Time.realtimeSinceStartup;
                ((Column3DAnatomy)desktop.Scene.Columns[0]).AnatomyParameters.InfluenceDistance = 22.5f;
                await desktop.Scene.PrepareRenderingAsync(token);
                StateSnapshot next = adapter.Capture(2);
                float secondClock = Time.realtimeSinceStartup;
                await QuestPairing.OpenReplicaAsync(host, device.Pin, credential, token, async (stream, stop) =>
                {
                    var checkpoint = await ReplicaWire.ReadAsync(stream, stop);
                    Assert.That(checkpoint.Kind, Is.EqualTo(ReplicaFrameKind.Checkpoint));
                    Assert.That(ReplicaWire.ReadCheckpoint(checkpoint.Body).Revision, Is.Zero);
                    await SendDeviceReplicaStateAsync(stream, ReplicaFrameKind.Snapshot, SharedStateCodec.Encode(initial), 1, firstClock, stop);
                    await SendDeviceReplicaStateAsync(stream, ReplicaFrameKind.Delta, ReplicaDelta.Between(initial, next).Encode(), 2, secondClock, stop);
                });
                await QuestPairing.OpenReplicaAsync(host, device.Pin, credential, token, async (stream, stop) =>
                {
                    var checkpoint = await ReplicaWire.ReadAsync(stream, stop);
                    Assert.That(ReplicaWire.ReadCheckpoint(checkpoint.Body).Revision, Is.EqualTo(2));
                    Assert.That(ReplicaWire.ReadCheckpoint(checkpoint.Body).Hash, Is.EqualTo(ReplicaWire.Hash(next)));
                    await SendDeviceReplicaStateAsync(stream, ReplicaFrameKind.Snapshot, SharedStateCodec.Encode(next), 2, secondClock, stop);
                });
            }
            finally
            {
                if (credential != null) Array.Clear(credential, 0, credential.Length);
                await UniTask.SwitchToMainThread();
                if (desktop != null) await desktop.CloseAsync();
            }
        }

        private static async Task SendDeviceReplicaStateAsync(Stream stream, ReplicaFrameKind kind, byte[] state, ulong revision, float clock, CancellationToken token)
        {
            byte[] body = new byte[4 + state.Length];
            Buffer.BlockCopy(BitConverter.GetBytes(clock), 0, body, 0, 4);
            Buffer.BlockCopy(state, 0, body, 4, state.Length);
            await ReplicaWire.WriteAsync(stream, kind, body, token);
            foreach (ReplicaFrameKind expected in new[] { ReplicaFrameKind.Received, ReplicaFrameKind.Applied, ReplicaFrameKind.Visible })
            {
                var reply = await ReplicaWire.ReadAsync(stream, token);
                Assert.That(reply.Kind, Is.EqualTo(expected), reply.Kind == ReplicaFrameKind.Rejected ? System.Text.Encoding.UTF8.GetString(reply.Body) : "Quest acknowledgement");
                Assert.That(ReplicaWire.ReadRevision(reply.Body), Is.EqualTo(revision));
            }
        }

        private async Task VerifyPreparedMeshAndConfigurationAsync(bool captureLiveDelivery, bool exerciseReplica = false)
        {
            string root = Path.Combine(Path.GetTempPath(), "hibop-s2-mesh-binding-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            using var settings = new PlayModePersistentDataScope(root);
            using var scope = new PlayModeSceneScope("S2MeshBinding");
            var clock = System.Diagnostics.Stopwatch.StartNew();
            await PrepareReferencesAsync();
            if (!Object3DManager.MarsAtlas.Loaded) Object3DManager.MarsAtlas.Load();
            Debug.Log($"S2 mesh binding: references {clock.Elapsed.TotalSeconds:F1}s");
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            var token = timeout.Token;
            RestoredScene desktop = null;
            QuestAnatomySession replicaSession = null;
            LocalizerProtocol transferredLocalizer = null;
            var view = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/QuestAnatomy.prefab"), scope.Root.transform).GetComponent<QuestAnatomyView>();
            try
            {
                using var source = new SceneArchive(Path.Combine(root, "source"));
                ScenePayload payload = CreateFixture(source, marsTags: true);
                string file = Path.Combine(root, "fixture.hbscene");
                source.Write(payload, file);
                using var delivery = new SceneDelivery(file, payload.TransferId, payload.SessionId, "S2 mesh binding");
                Debug.Log($"S2 mesh binding: archive {clock.Elapsed.TotalSeconds:F1}s");
                var desktopArchive = new SceneArchive(Path.Combine(root, "desktop"), true, source.Globals);
                desktop = await SceneRestoration.PrepareAsync(desktopArchive.Read(file), desktopArchive, AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/3D/Scenes/Scene 3D.prefab").GetComponent<Base3DScene>(), scope.Root.transform, token);
                Debug.Log($"S2 mesh binding: Desktop scene {clock.Elapsed.TotalSeconds:F1}s");
                var original = Object3DManager.MNI.GreyMatter;
                var prepared = (LeftRightMesh3D)desktop.Scene.MeshManager.Meshes[0];
                AssertPreparedSurface(original.Both, prepared.Both, "both");
                AssertPreparedSurface(original.SimplifiedBoth, prepared.SimplifiedBoth, "simplified both");
                AssertPreparedSurface(original.Left, prepared.Left, "left");
                AssertPreparedSurface(original.Right, prepared.Right, "right");
                AssertPreparedSurface(original.SimplifiedLeft, prepared.SimplifiedLeft, "simplified left");
                AssertPreparedSurface(original.SimplifiedRight, prepared.SimplifiedRight, "simplified right");
                Assert.That(new SurfaceCapture(original.Both).Data.UV, Is.Empty, "The prepared MNI source has no projection UVs.");
                Assert.That(new SurfaceCapture(prepared.Both).Data.UV, Is.Not.Empty, "Scene initialization adds projection UVs without changing resource identity.");

                var questArchive = new SceneArchive(Path.Combine(root, "quest"), true, source.Globals);
                if (exerciseReplica)
                {
                    var receiverObject = new GameObject("S3 Replica Receiver");
                    replicaSession = receiverObject.AddComponent<QuestAnatomySession>();
                    typeof(QuestAnatomySession).GetField("view", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(replicaSession, view);
                    var publish = typeof(QuestAnatomySession).GetMethod("PublishAsync", BindingFlags.Instance | BindingFlags.NonPublic);
                    var publication = (Task<DeliveryStatus>)publish.Invoke(replicaSession, new object[] { questArchive.Read(file), questArchive, delivery.ContentHash, token });
                    Assert.That(await publication, Is.EqualTo(DeliveryStatus.Published));
                }
                else await view.ApplyAsync(questArchive.Read(file), questArchive, token);

                Debug.Log($"S2 mesh binding: Quest scene {clock.Elapsed.TotalSeconds:F1}s");

                byte[] digest = Enumerable.Range(0, 32).Select(index => Convert.ToByte(delivery.ContentHash.Substring(index * 2, 2), 16)).ToArray();
                var receipt = new HBP.Transfer.Transport.DeliveryReceipt(digest, HBP.Transfer.Transport.DeliveryStatus.Published);
                Guid epoch = Guid.NewGuid();
                LiveGeometryStateAdapter sender = new(desktop.Scene, epoch, PreparedSceneDeliveryBinding.FromSent(delivery, receipt));
                LiveGeometryStateAdapter receiver = new(view.Scene, epoch, PreparedSceneDeliveryBinding.FromPublished(receipt, view.PublishedScene));
                await desktop.Scene.PrepareRenderingAsync(token);
                await view.Scene.PrepareRenderingAsync(token);
                if (exerciseReplica)
                {
                    await ExerciseReplicaControlAsync(desktop.Scene, view, replicaSession, sender, token);
                    return;
                }

                receiver.BindInitialState(sender.Capture(0));

                desktop.Scene.AtlasManager.DisplayMarsAtlas = true;
                await desktop.Scene.PrepareRenderingAsync(token);
                receiver.Apply(sender.Capture(1));
                await view.Scene.PrepareRenderingAsync(token);
                int[] fullMask = desktop.Scene.MeshManager.BrainSurface.VisibilityMask;
                int[] simplifiedMask = desktop.Scene.MeshManager.SimplifiedMeshToUse.VisibilityMask;
                fullMask[1] = 0;
                simplifiedMask[1] = 0;
                desktop.Scene.TriangleEraser.CurrentMasks = new List<int[]> { fullMask, simplifiedMask };
                await desktop.Scene.PrepareRenderingAsync(token);
                receiver.Apply(sender.Capture(2));
                await view.Scene.PrepareRenderingAsync(token);
                Assert.That(view.Scene.Columns[0].BrainMesh.GetComponent<MeshFilter>().sharedMesh.colors, Is.EqualTo(desktop.Scene.Columns[0].BrainMesh.GetComponent<MeshFilter>().sharedMesh.colors), "D20 mask colors");
                desktop.Scene.SaveConfiguration();
                desktop.Scene.ResetConfiguration();
                await desktop.Scene.PrepareRenderingAsync(token);
                StateSnapshot reset = sender.Capture(3);
                receiver.Apply(reset);
                await view.Scene.PrepareRenderingAsync(token);
                Assert.That(SharedStateCodec.Encode(receiver.Capture(3)), Is.EqualTo(SharedStateCodec.Encode(reset)), "D33 reset state");
                Assert.That(view.Scene.Columns[0].BrainMesh.GetComponent<MeshFilter>().sharedMesh.colors, Is.EqualTo(desktop.Scene.Columns[0].BrainMesh.GetComponent<MeshFilter>().sharedMesh.colors), "D33 reset colors");

                desktop.Scene.LoadConfiguration();
                await desktop.Scene.PrepareRenderingAsync(token);
                StateSnapshot loaded = sender.Capture(4);
                receiver.Apply(loaded);
                await view.Scene.PrepareRenderingAsync(token);
                Assert.That(SharedStateCodec.Encode(receiver.Capture(4)), Is.EqualTo(SharedStateCodec.Encode(loaded)), "D33 load state");
                Assert.That(view.Scene.Columns[0].BrainMesh.GetComponent<MeshFilter>().sharedMesh.colors, Is.EqualTo(desktop.Scene.Columns[0].BrainMesh.GetComponent<MeshFilter>().sharedMesh.colors), "D33 load colors");

                if (captureLiveDelivery)
                {
                    string localizerRoot = Path.GetFullPath("Assets/Tests/Fixtures/Native/Localizers/protocol-alpha/signal-alpha");
                    transferredLocalizer = new LocalizerProtocol("protocol-alpha", localizerRoot, false);
                    var transferredData = new LocalizerData("signal-alpha", localizerRoot, false);
                    var transferredBloc = new LocalizerBloc("bloc-alpha", Path.Combine(localizerRoot, "bloc-alpha.nii"), Path.Combine(localizerRoot, "bloc-alpha_MASK.nii"));
                    transferredData.Blocs.Add(transferredBloc);
                    transferredLocalizer.Datas.Add(transferredData);
                    await transferredBloc.FMRI.LoadAsync();
                    string expectedLocalizerHash = transferredBloc.FMRI.SourceHash;
                    await UniTask.SwitchToMainThread();
                    Object3DManager.Localizers.Protocols.Add(transferredLocalizer);
                    using var liveDelivery = await DesktopSceneCapture.CaptureDeliveryAsync(desktop.Scene, "s2-live", "s2-local", 1, source.Globals, token);
                    string liveFile = (string)typeof(SceneDelivery).GetField("file", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(liveDelivery);
                    using (var package = ZipFile.OpenRead(liveFile))
                        Assert.That(package.Entries.Select(entry => entry.FullName), Does.Not.Contain(expectedLocalizerHash.Replace("-", "").ToLowerInvariant() + ".nii"), "Localizer bytes stay outside scene delivery");
                    await view.ClearAsync();
                    var liveQuestArchive = new SceneArchive(Path.Combine(root, "live-quest"), true, source.Globals);
                    ScenePayload livePayload = liveQuestArchive.Read(liveFile);
                    await view.ApplyAsync(livePayload, liveQuestArchive, token);
                    var installedBloc = Object3DManager.Localizers.Protocols.Single(protocol => protocol.Name == "protocol-alpha").Datas.Single(data => data.Name == "signal-alpha").Blocs.Single(bloc => bloc.Name == "bloc-alpha");
                    Assert.That(installedBloc.Loaded, Is.True, "Localizer remains available independently of scene delivery");
                    Assert.That(installedBloc.FMRI.SourceHash, Is.EqualTo(expectedLocalizerHash));
                    Assert.That(installedBloc.FMRI.MaskVolume, Is.Not.Null);
                    byte[] liveDigest = Enumerable.Range(0, 32).Select(index => Convert.ToByte(liveDelivery.ContentHash.Substring(index * 2, 2), 16)).ToArray();
                    var liveReceipt = new HBP.Transfer.Transport.DeliveryReceipt(liveDigest, HBP.Transfer.Transport.DeliveryStatus.Published);
                    Guid liveEpoch = Guid.NewGuid();
                    LiveGeometryStateAdapter liveSender = new(desktop.Scene, liveEpoch, PreparedSceneDeliveryBinding.FromSent(liveDelivery, liveReceipt));
                    LiveGeometryStateAdapter liveReceiver = new(view.Scene, liveEpoch, PreparedSceneDeliveryBinding.FromPublished(liveReceipt, view.PublishedScene));
                    await view.Scene.PrepareRenderingAsync(token);
                    StateSnapshot liveInitial = liveSender.Capture(0);
                    liveReceiver.BindInitialState(liveInitial);
                    Transform liveWrapper = view.Columns[0].transform;
                    liveWrapper.SetPositionAndRotation(new Vector3(2, 3, 4), Quaternion.Euler(5, 15, 25));
                    liveWrapper.localScale = Vector3.one * 1.5f;
                    liveReceiver.Apply(liveInitial);
                    await view.Scene.PrepareRenderingAsync(token);
                    StateSnapshot liveActual = liveReceiver.Capture(0);
                    var expectedFields = liveInitial.Fields;
                    var actualFields = liveActual.Fields;
                    Assert.That(actualFields.Keys, Is.EqualTo(expectedFields.Keys), "Live Desktop capture field topology");
                    foreach (var field in expectedFields)
                    {
                        byte[] actualValue = actualFields[field.Key];
                        if (actualValue.SequenceEqual(field.Value)) continue;
                        int firstDifference = Enumerable.Range(0, Math.Min(field.Value.Length, actualValue.Length)).Where(index => field.Value[index] != actualValue[index]).DefaultIfEmpty(-1).First();
                        Assert.Fail($"Live Desktop capture differs at {field.Key}: expected {field.Value.Length} bytes, actual {actualValue.Length} bytes, first difference {firstDifference}; expected {BitConverter.ToString(field.Value.Take(32).ToArray())}; actual {BitConverter.ToString(actualValue.Take(32).ToArray())}");
                    }

                    Assert.That(SharedStateCodec.Encode(liveActual), Is.EqualTo(SharedStateCodec.Encode(liveInitial)), "Live Desktop capture binds to its delivered Quest scene");
                    Assert.That(view.Scene.Columns[0].BrainMesh.GetComponent<MeshFilter>().sharedMesh.colors, Is.EqualTo(desktop.Scene.Columns[0].BrainMesh.GetComponent<MeshFilter>().sharedMesh.colors), "Live Desktop capture surface colors");
                    Assert.That(liveWrapper.position, Is.EqualTo(new Vector3(2, 3, 4)), "Live Desktop capture wrapper position");
                    Assert.That(Quaternion.Angle(liveWrapper.rotation, Quaternion.Euler(5, 15, 25)), Is.LessThan(0.001f), "Live Desktop capture wrapper rotation");
                    Assert.That(liveWrapper.localScale, Is.EqualTo(Vector3.one * 1.5f), "Live Desktop capture wrapper scale");

                    view.ToggleSurface();
                    using var refreshArchive = new SceneArchive(Path.Combine(root, "refresh-quest"), true, source.Globals);
                    await view.ApplyAsync(refreshArchive.Read(liveFile), refreshArchive, token);
                    Transform refreshedWrapper = view.Columns[0].transform;
                    Assert.That(refreshedWrapper, Is.Not.SameAs(liveWrapper), "Resource epoch publishes a new prepared scene");
                    Assert.That(refreshedWrapper.position, Is.EqualTo(new Vector3(2, 3, 4)), "Resource transition keeps Quest position");
                    Assert.That(Quaternion.Angle(refreshedWrapper.rotation, Quaternion.Euler(5, 15, 25)), Is.LessThan(0.001f), "Resource transition keeps Quest rotation");
                    Assert.That(refreshedWrapper.localScale, Is.EqualTo(Vector3.one * 1.5f), "Resource transition keeps Quest scale");
                    Assert.That(view.SurfaceHidden, Is.True, "Resource transition keeps local surface visibility");
                }

                Debug.Log($"S2 mesh binding: bound {clock.Elapsed.TotalSeconds:F1}s");
            }
            finally
            {
                await UniTask.SwitchToMainThread();
                if (transferredLocalizer != null)
                {
                    Object3DManager.Localizers.Protocols.Remove(transferredLocalizer);
                    transferredLocalizer.Clean();
                }

                Object3DManager.Localizers.Unload("protocol-alpha");
                await view.ClearAsync();
                if (desktop != null) await desktop.CloseAsync();
                if (replicaSession != null) Object.Destroy(replicaSession.gameObject);
                Object.Destroy(view.gameObject);
                await UniTask.NextFrame();
                if (!Directory.EnumerateFileSystemEntries(root).Any()) Directory.Delete(root);
            }
        }

        private static async Task ExerciseReplicaControlAsync(Base3DScene desktop, QuestAnatomyView view, QuestAnatomySession session, LiveGeometryStateAdapter sender, CancellationToken token)
        {
            Base3DScene questScene = view.Scene;
            Transform wrapper = view.Columns[0].transform;
            wrapper.SetPositionAndRotation(new Vector3(2, 3, 4), Quaternion.Euler(5, 15, 25));
            wrapper.localScale = Vector3.one * 1.5f;
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            using var client = new TcpClient();
            Task connect = client.ConnectAsync(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndpoint).Port);
            using var peer = await listener.AcceptTcpClientAsync();
            await connect;
            using var stop = CancellationTokenSource.CreateLinkedTokenSource(token);
            Task receiving = session.ReceiveReplicaAsync(peer.GetStream(), stop.Token);
            try
            {
                var emptyCheckpoint = await ReplicaWire.ReadAsync(client.GetStream(), token);
                Assert.That(emptyCheckpoint.Kind, Is.EqualTo(ReplicaFrameKind.Checkpoint));
                Assert.That(ReplicaWire.ReadCheckpoint(emptyCheckpoint.Body).Revision, Is.Zero);
                StateSnapshot initial = sender.Capture(1);
                await SendReplicaStateAsync(client.GetStream(), ReplicaFrameKind.Snapshot, SharedStateCodec.Encode(initial), 1, token);
                Assert.That(session.ReceivedRevision, Is.EqualTo(1));
                Assert.That(session.AppliedRevision, Is.EqualTo(1));
                Assert.That(session.VisibleRevision, Is.EqualTo(1));
                await view.Scene.PrepareRenderingAsync(token);
                var questAdapter = (LiveGeometryStateAdapter)typeof(QuestAnatomySession).GetField("replica", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
                Assert.That(SharedStateCodec.Encode(questAdapter.Capture(1)), Is.EqualTo(SharedStateCodec.Encode(initial)));

                ((Column3DAnatomy)desktop.Columns[0]).AnatomyParameters.InfluenceDistance = 22.5f;
                await desktop.PrepareRenderingAsync(token);
                StateSnapshot next = sender.Capture(2);
                await SendReplicaStateAsync(client.GetStream(), ReplicaFrameKind.Delta, ReplicaDelta.Between(initial, next).Encode(), 2, token);
                await view.Scene.PrepareRenderingAsync(token);
                Assert.That(view.Scene, Is.SameAs(questScene));
                Assert.That(((Column3DAnatomy)view.Scene.Columns[0]).AnatomyParameters.InfluenceDistance, Is.EqualTo(22.5f));
                Assert.That(SharedStateCodec.Encode(questAdapter.Capture(2)), Is.EqualTo(SharedStateCodec.Encode(next)));
                Assert.That(wrapper.position, Is.EqualTo(new Vector3(2, 3, 4)));
                Assert.That(Quaternion.Angle(wrapper.rotation, Quaternion.Euler(5, 15, 25)), Is.LessThan(0.001f));
                Assert.That(wrapper.localScale, Is.EqualTo(Vector3.one * 1.5f));
                Assert.That(session.VisibleRevision, Is.EqualTo(2));

                var cut = desktop.AddCutPlane();
                await desktop.PrepareRenderingAsync(token);
                StateSnapshot createdCuts = sender.CaptureCuts(next);
                StateSnapshot created = createdCuts.WithFields(createdCuts.Fields, 3);
                await SendReplicaStateAsync(client.GetStream(), ReplicaFrameKind.Delta, ReplicaDelta.Between(next, created).Encode(), 3, token);
                Assert.That(view.Scene.Cuts.Select(item => item.ID), Does.Contain(cut.ID));
                cut.Position = 0.72f;
                desktop.UpdateCutPlane(cut, true);
                await desktop.PrepareRenderingAsync(token);
                StateSnapshot movedCuts = sender.CaptureCuts(created);
                next = movedCuts.WithFields(movedCuts.Fields, 4);
                await SendReplicaStateAsync(client.GetStream(), ReplicaFrameKind.Delta, ReplicaDelta.Between(created, next).Encode(), 4, token);
                Assert.That(view.Scene.Cuts.Single(item => item.ID == cut.ID).Position, Is.EqualTo(0.72f));

                client.Close();
                peer.Close();
                try
                {
                    await receiving;
                }
                catch (Exception exception) when (exception is OperationCanceledException || exception is IOException || exception is ObjectDisposedException)
                {
                }

                using var retryClient = new TcpClient();
                Task retryConnect = retryClient.ConnectAsync(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndpoint).Port);
                using var retryPeer = await listener.AcceptTcpClientAsync();
                await retryConnect;
                Task retryReceiving = session.ReceiveReplicaAsync(retryPeer.GetStream(), stop.Token);
                try
                {
                    var checkpoint = await ReplicaWire.ReadAsync(retryClient.GetStream(), token);
                    Assert.That(checkpoint.Kind, Is.EqualTo(ReplicaFrameKind.Checkpoint));
                    Assert.That(ReplicaWire.ReadCheckpoint(checkpoint.Body).Revision, Is.EqualTo(4));
                    Assert.That(ReplicaWire.ReadCheckpoint(checkpoint.Body).Hash, Is.EqualTo(ReplicaWire.Hash(next)));
                    await SendReplicaStateAsync(retryClient.GetStream(), ReplicaFrameKind.Snapshot, SharedStateCodec.Encode(next), 4, token);
                    Assert.That(view.Scene, Is.SameAs(questScene));
                    Assert.That(wrapper.position, Is.EqualTo(new Vector3(2, 3, 4)));
                }
                finally
                {
                    retryClient.Close();
                    retryPeer.Close();
                    try
                    {
                        await retryReceiving;
                    }
                    catch (Exception exception) when (exception is OperationCanceledException || exception is IOException || exception is ObjectDisposedException)
                    {
                    }
                }
            }
            finally
            {
                stop.Cancel();
                peer.Close();
                listener.Stop();
                try
                {
                    await receiving;
                }
                catch (Exception exception) when (exception is OperationCanceledException || exception is IOException || exception is ObjectDisposedException)
                {
                }
            }
        }

        private static async Task SendReplicaStateAsync(Stream stream, ReplicaFrameKind kind, byte[] state, ulong revision, CancellationToken token)
        {
            byte[] body = new byte[4 + state.Length];
            Buffer.BlockCopy(BitConverter.GetBytes(Time.realtimeSinceStartup), 0, body, 0, 4);
            Buffer.BlockCopy(state, 0, body, 4, state.Length);
            await ReplicaWire.WriteAsync(stream, kind, body, token);
            foreach (ReplicaFrameKind expected in new[] { ReplicaFrameKind.Received, ReplicaFrameKind.Applied, ReplicaFrameKind.Visible })
            {
                var reply = await ReplicaWire.ReadAsync(stream, token);
                Assert.That(reply.Kind, Is.EqualTo(expected), reply.Kind == ReplicaFrameKind.Rejected ? System.Text.Encoding.UTF8.GetString(reply.Body) : "Replica ACK");
                Assert.That(ReplicaWire.ReadRevision(reply.Body), Is.EqualTo(revision));
            }
        }

        private static void AssertPreparedSurface(HBP.Core.DLL.Surface expected, HBP.Core.DLL.Surface actual, string variant)
        {
            var left = new SurfaceCapture(expected);
            var right = new SurfaceCapture(actual);
            Assert.That(right.Data.Vertices, Is.EqualTo(left.Data.Vertices), variant + " vertices");
            Assert.That(right.Data.Triangles, Is.EqualTo(left.Data.Triangles), variant + " triangles");
            Assert.That(right.Data.Normals, Is.EqualTo(left.Data.Normals), variant + " normals");
            Assert.That(right.Atlas, Is.EqualTo(left.Atlas), variant + " atlas capability");
        }

        [Test]
        [Timeout(900000)]
        public async Task S2_ReplaysCorrelationsAcrossDeliveredSixModalityScenesWithoutReplacingQuestPresentation()
        {
            string root = Path.Combine(Path.GetTempPath(), "hibop-s2-six-modalities-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            using var settings = new PlayModePersistentDataScope(root);
            using var scope = new PlayModeSceneScope("S2SixModalityReplay");
            PersistentDataManager.UserPreferences.Visualization._3D.AutomaticEEGUpdate = false;
            await PrepareReferencesAsync();
            if (!Object3DManager.MarsAtlas.Loaded) Object3DManager.MarsAtlas.Load();
            Assert.That(Object3DManager.MarsAtlas.Labels().Length, Is.GreaterThan(1));
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(8));
            var token = timeout.Token;
            RestoredScene desktop = null;
            LocalizerProtocol localizerProtocol = null;
            HBP.Core.Object3D.FMRI difumoVolume = null;
            DiFuMoInformation difumoInformation = null;
            IBCObjects previousIbc = Object3DManager.IBC;
            HBP.Core.Object3D.FMRI ibcVolume = null;
            var view = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/QuestAnatomy.prefab"), scope.Root.transform).GetComponent<QuestAnatomyView>();
            try
            {
                using var source = new SceneArchive(Path.Combine(root, "source"));
                ScenePayload payload = CreateFixture(source, marsTags: true);
                string file = Path.Combine(root, "fixture.hbscene");
                source.Write(payload, file);
                using var delivery = new SceneDelivery(file, payload.TransferId, payload.SessionId, "S2 six modalities");
                var desktopArchive = new SceneArchive(Path.Combine(root, "desktop"), true, source.Globals);
                desktop = await SceneRestoration.PrepareAsync(desktopArchive.Read(file), desktopArchive, AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/3D/Scenes/Scene 3D.prefab").GetComponent<Base3DScene>(), scope.Root.transform, token);
                var questArchive = new SceneArchive(Path.Combine(root, "quest"), true, source.Globals);
                await view.ApplyAsync(questArchive.Read(file), questArchive, token);
                Base3DScene desktopScene = desktop.Scene, questScene = view.Scene;
                await desktopScene.PrepareRenderingAsync(token);
                await questScene.PrepareRenderingAsync(token);
                string localizerRoot = Path.GetFullPath("Assets/Tests/Fixtures/Native/Localizers/protocol-alpha/signal-alpha");
                localizerProtocol = new LocalizerProtocol("protocol-alpha", localizerRoot, false);
                var localizerData = new LocalizerData("signal-alpha", localizerRoot, false);
                var localizerBloc = new LocalizerBloc("bloc-alpha", Path.Combine(localizerRoot, "bloc-alpha.nii"), Path.Combine(localizerRoot, "bloc-alpha_MASK.nii"));
                localizerData.Blocs.Add(localizerBloc);
                localizerProtocol.Datas.Add(localizerData);
                await localizerBloc.FMRI.LoadAsync();
                await UniTask.SwitchToMainThread();
                Object3DManager.Localizers.Protocols.Add(localizerProtocol);
                difumoVolume = new HBP.Core.Object3D.FMRI("s2-fixture", Path.GetFullPath("Assets/Tests/Fixtures/Native/Nifti/fmri_4d.nii.gz"), loadInBackground: false);
                difumoInformation = new DiFuMoInformation(Path.GetFullPath("Assets/Data/Atlases/DiFuMo/64/labels_64_dictionary.csv"));
                await difumoVolume.LoadAsync();
                await difumoInformation.LoadCompletion;
                await UniTask.SwitchToMainThread();
                Object3DManager.DiFuMo.FMRIs.Add("s2-fixture", difumoVolume);
                Object3DManager.DiFuMo.Information.Add("s2-fixture", difumoInformation);
                ibcVolume = new HBP.Core.Object3D.FMRI("s2-ibc", Path.GetFullPath("Assets/Tests/Fixtures/Native/Nifti/fmri_4d.nii.gz"), loadInBackground: false);
                var ibcInformation = new IBCInformation(Path.GetFullPath("Assets/Data/Atlases/IBC/map_labels.csv"));
                await ibcVolume.LoadAsync();
                await ibcInformation.LoadCompletion;
                await UniTask.SwitchToMainThread();
                var ibcFixture = new IBCObjects();
                typeof(IBCObjects).GetProperty(nameof(IBCObjects.FMRI)).SetValue(ibcFixture, ibcVolume);
                typeof(IBCObjects).GetProperty(nameof(IBCObjects.Information)).SetValue(ibcFixture, ibcInformation);
                Object3DManager.IBC = ibcFixture;
                Guid epoch = Guid.NewGuid();
                byte[] digest = Enumerable.Range(0, 32).Select(index => Convert.ToByte(delivery.ContentHash.Substring(index * 2, 2), 16)).ToArray();
                var receipt = new HBP.Transfer.Transport.DeliveryReceipt(digest, HBP.Transfer.Transport.DeliveryStatus.Published);
                byte[] wrongDigest = (byte[])digest.Clone();
                wrongDigest[0] ^= 1;
                var mismatchedReceipt = new HBP.Transfer.Transport.DeliveryReceipt(wrongDigest, HBP.Transfer.Transport.DeliveryStatus.Published);
                Assert.Throws<InvalidDataException>(() => PreparedSceneDeliveryBinding.FromSent(delivery, mismatchedReceipt));
                Assert.Throws<InvalidDataException>(() => PreparedSceneDeliveryBinding.FromPublished(mismatchedReceipt, view.PublishedScene));
                PreparedSceneDeliveryBinding sent = PreparedSceneDeliveryBinding.FromSent(delivery, receipt);
                PreparedSceneDeliveryBinding published = PreparedSceneDeliveryBinding.FromPublished(receipt, view.PublishedScene);
                string originalMeshName = desktop.Payload.Meshes[0].Name;
                desktop.Payload.Meshes[0].Name = "unpublished-mesh";
                try
                {
                    Assert.DoesNotThrow(() => new LiveGeometryStateAdapter(desktopScene, epoch, sent), "A mutable restored payload cannot change the delivery manifest.");
                }
                finally
                {
                    desktop.Payload.Meshes[0].Name = originalMeshName;
                }

                string originalLiveMeshName = desktopScene.MeshManager.Meshes[0].Name;
                desktopScene.MeshManager.Meshes[0].Name = "unpublished-mesh";
                try
                {
                    Assert.Throws<InvalidDataException>(() => new LiveGeometryStateAdapter(desktopScene, epoch, sent));
                }
                finally
                {
                    desktopScene.MeshManager.Meshes[0].Name = originalLiveMeshName;
                }

                Mesh3D originalMesh = desktopScene.MeshManager.Meshes[0];
                var changedSurface = (HBP.Core.DLL.Surface)originalMesh.SimplifiedBoth.Clone();
                var surfaceBuffer = new Mesh();
                try
                {
                    changedSurface.UpdateMeshFromDLL(surfaceBuffer);
                    Vector3[] changedVertices = surfaceBuffer.vertices;
                    changedVertices[0].x += 1f;
                    var normals = surfaceBuffer.normals;
                    var uv = surfaceBuffer.uv;
                    var colors = surfaceBuffer.colors;
                    changedSurface.SetBuffers(changedVertices, surfaceBuffer.triangles, normals.Length == 0 ? null : normals, uv.Length == 0 ? null : uv, colors.Length == 0 ? null : colors);
                    var hemispheres = (LeftRightMesh3D)originalMesh;
                    desktopScene.MeshManager.Meshes[0] = Mesh3D.FromPrepared(originalMesh.Name, originalMesh.Type, originalMesh.Both, changedSurface, hemispheres.Left, hemispheres.Right, hemispheres.SimplifiedLeft, hemispheres.SimplifiedRight, null, null, null, null, null, null);
                    Assert.Throws<InvalidDataException>(() => new LiveGeometryStateAdapter(desktopScene, epoch, sent), "Different mesh vertices under the same name must not bind.");
                }
                finally
                {
                    desktopScene.MeshManager.Meshes[0] = originalMesh;
                    changedSurface.Dispose();
                    Object.Destroy(surfaceBuffer);
                }

                var megItem = desktopScene.Columns.OfType<Column3DMEG>().Single().ColumnMEGData.Data.MEGItems[0];
                float originalMegValue = megItem.ValuesByChannel["A1"][0];
                megItem.ValuesByChannel["A1"][0] = originalMegValue + 1f;
                try
                {
                    Assert.Throws<InvalidDataException>(() => new LiveGeometryStateAdapter(desktopScene, epoch, sent), "A different MEG channel value under the same label must not bind.");
                }
                finally
                {
                    megItem.ValuesByChannel["A1"][0] = originalMegValue;
                }

                string originalFunctionalFile = desktop.Payload.Columns[4].Functional[0].File;
                desktop.Payload.Columns[4].Functional[0].File = new string('0', 64) + ".nii.gz";
                try
                {
                    Assert.DoesNotThrow(() => new LiveGeometryStateAdapter(desktopScene, epoch, sent), "A mutable restored payload cannot change a delivered fMRI descriptor.");
                }
                finally
                {
                    desktop.Payload.Columns[4].Functional[0].File = originalFunctionalFile;
                }

                LiveGeometryStateAdapter sender = new(desktopScene, epoch, sent);
                LiveGeometryStateAdapter receiver = new(questScene, epoch, published);
                StateSnapshot initial = sender.Capture(0);
                receiver.BindInitialState(initial);
                Assert.That(SharedStateCodec.Encode(receiver.Capture(0)), Is.EqualTo(SharedStateCodec.Encode(initial)));

                var wrapper = view.Columns[1].transform;
                wrapper.SetPositionAndRotation(new Vector3(1, 2, 3), Quaternion.Euler(10, 20, 30));
                wrapper.localScale = Vector3.one * 2;
                Column3DIEEG desktopIEEG = desktopScene.ColumnsIEEG.Single();
                Column3DIEEG questIEEG = questScene.ColumnsIEEG.Single();
                var first = desktopIEEG.Sites[0];
                var second = desktopIEEG.Sites[1];
                desktopScene.SelectSite(desktopIEEG, first);
                desktopScene.ImplantationManager.ComparingSites = true;
                desktopIEEG.CorrelationBySitePair[first] = new() { [second] = 0.25f };
                desktopIEEG.CorrelationMeanBySitePair[first] = new() { [second] = 0.75f };
                desktopScene.DisplayCorrelations = true;
                StateSnapshot correlated = sender.Capture(1);
                Assert.Throws<InvalidDataException>(() => receiver.Apply(correlated), "The result bytes must arrive before the dependent state.");
                string reference = System.Text.Encoding.UTF8.GetString(correlated.Fields[new StateKey(EntityKind.Scene, "", "", 9)]);
                receiver.PrepareCorrelationResource(reference, sender.ExportCorrelationResource(reference));
                receiver.Apply(correlated);
                Assert.That(SharedStateCodec.Encode(receiver.Capture(1)), Is.EqualTo(SharedStateCodec.Encode(correlated)));
                var questFirst = questIEEG.Sites.Single(site => site.Information.FullID == first.Information.FullID);
                var questSecond = questIEEG.Sites.Single(site => site.Information.FullID == second.Information.FullID);
                Assert.That(questIEEG.CorrelationBySitePair[questFirst][questSecond], Is.EqualTo(0.25f));
                Assert.That(questIEEG.CorrelationMeanBySitePair[questFirst][questSecond], Is.EqualTo(0.75f));
                Assert.That(view.Scene, Is.SameAs(questScene));
                Assert.That(wrapper.position, Is.EqualTo(new Vector3(1, 2, 3)));
                Assert.That(Quaternion.Angle(wrapper.rotation, Quaternion.Euler(10, 20, 30)), Is.LessThan(0.001f));
                Assert.That(wrapper.localScale, Is.EqualTo(Vector3.one * 2));

                desktopScene.ResetCorrelations();
                desktopScene.DisplayCorrelations = false;
                desktopScene.ImplantationManager.ComparingSites = false;
                StateSnapshot cleared = sender.Capture(2);
                receiver.Apply(cleared);
                Assert.That(questIEEG.CorrelationBySitePair, Is.Empty);
                Assert.That(questIEEG.CorrelationMeanBySitePair, Is.Empty);
                Assert.That(SharedStateCodec.Encode(receiver.Capture(2)), Is.EqualTo(SharedStateCodec.Encode(cleared)));

                ulong revision = 2;

                async Task Replay(string operation, Action edit)
                {
                    edit();
                    await desktopScene.PrepareRenderingAsync(token);
                    StateSnapshot expected = sender.Capture(++revision);
                    receiver.Apply(expected);
                    await questScene.PrepareRenderingAsync(token);
                    Assert.That(SharedStateCodec.Encode(receiver.Capture(revision)), Is.EqualTo(SharedStateCodec.Encode(expected)), operation);
                    for (int columnIndex = 0; columnIndex < desktopScene.Columns.Count; columnIndex++)
                    {
                        Column3D desktopColumn = desktopScene.Columns[columnIndex];
                        Column3D questColumn = questScene.Columns[columnIndex];
                        Assert.That(questColumn.SurfaceGenerator.ActivityUV, Is.EqualTo(desktopColumn.SurfaceGenerator.ActivityUV), operation + " activity UV " + columnIndex);
                        Assert.That(questColumn.SurfaceGenerator.AlphaUV, Is.EqualTo(desktopColumn.SurfaceGenerator.AlphaUV), operation + " alpha UV " + columnIndex);
                        Assert.That(questColumn.BrainMesh.GetComponent<MeshFilter>().sharedMesh.colors, Is.EqualTo(desktopColumn.BrainMesh.GetComponent<MeshFilter>().sharedMesh.colors), operation + " atlas colors " + columnIndex);
                        Assert.That(questColumn.Sites.Select(site => site.State.IsOutOfROI), Is.EqualTo(desktopColumn.Sites.Select(site => site.State.IsOutOfROI)), operation + " ROI site mask " + columnIndex);
                    }

                    Assert.That(view.Scene, Is.SameAs(questScene), operation + " Quest content");
                    Assert.That(wrapper.position, Is.EqualTo(new Vector3(1, 2, 3)), operation + " Quest wrapper position");
                    Assert.That(Quaternion.Angle(wrapper.rotation, Quaternion.Euler(10, 20, 30)), Is.LessThan(0.001f), operation + " Quest wrapper rotation");
                    Assert.That(wrapper.localScale, Is.EqualTo(Vector3.one * 2), operation + " Quest wrapper scale");
                }

                foreach (Column3D column in desktopScene.Columns)
                {
                    await Replay("D1 select " + column.ColumnData.ID, () => desktopScene.SelectColumn(column));
                    Assert.That(questScene.SelectedColumn.ColumnData.ID, Is.EqualTo(column.ColumnData.ID));
                }

                await Replay("D2 select site", () => desktopScene.SelectSite(desktopIEEG, first));
                Assert.That(questIEEG.SelectedSite.Information.FullID, Is.EqualTo(first.Information.FullID));
                await Replay("D5 strong cuts", () => desktopScene.StrongCuts = true);
                desktopScene.AutomaticCutAroundSelectedSite = true;
                await desktopScene.PrepareRenderingAsync(token);
                StateSnapshot automaticCuts = sender.Capture(++revision);
                receiver.Apply(automaticCuts);
                await questScene.PrepareRenderingAsync(token);
                Assert.That(questScene.Cuts.Select(cut => cut.ID), Is.EqualTo(desktopScene.Cuts.Select(cut => cut.ID)));
                var automaticCutFields = receiver.Capture(revision).Fields;
                Assert.That(automaticCutFields.Keys, Is.EqualTo(automaticCuts.Fields.Keys), "D6 automatic cut topology");
                string cutDetails = string.Join("; ", desktopScene.Cuts.Zip(questScene.Cuts, (left, right) => $"{left.Orientation}: Desktop flip={left.Flip} normal={left.Normal} position={left.Position}; Quest flip={right.Flip} normal={right.Normal} position={right.Position}"));
                foreach (var field in automaticCuts.Fields)
                    Assert.That(automaticCutFields[field.Key], Is.EqualTo(field.Value), "D6 " + field.Key + " " + cutDetails);
                desktopScene.AutomaticCutAroundSelectedSite = false;
                await desktopScene.PrepareRenderingAsync(token);
                await Replay("D6 automatic cuts disabled", () => { });
                await Replay("D2 clear site", () => desktopScene.SelectSite(desktopIEEG, null));
                Assert.That(questIEEG.SelectedSite, Is.Null);

                await Replay("D13 site display and gain", () =>
                {
                    desktopScene.HideBlacklistedSites = true;
                    desktopScene.ShowAllSites = true;
                    desktopScene.SiteGain = 1.25f;
                });
                Assert.That(questScene.SiteGain, Is.EqualTo(1.25f));

                await Replay("D19 scene appearance", () =>
                {
                    desktopScene.BrainColor = ColorType.Hot;
                    desktopScene.CutColor = ColorType.Winter;
                    desktopScene.Colormap = ColorType.Cool;
                    desktopScene.EdgeMode = true;
                    desktopScene.IsBrainTransparent = true;
                    desktopScene.BrainMaterials.SetAlpha(0.7f);
                });
                Assert.That(questScene.BrainMaterials.Alpha, Is.EqualTo(0.7f));
                Assert.That(questScene.IsBrainTransparent, Is.True);

                await Replay("D21 column activity opacity", () => desktopScene.Columns.OfType<Column3DStatic>().Single().ActivityAlpha = 0.55f);
                Assert.That(questScene.Columns.OfType<Column3DStatic>().Single().ActivityAlpha, Is.EqualTo(0.55f));

                await Replay("D22 anatomy influence", () => desktopScene.Columns.OfType<Column3DAnatomy>().Single().AnatomyParameters.InfluenceDistance = 23f);
                Assert.That(questScene.Columns.OfType<Column3DAnatomy>().Single().AnatomyParameters.InfluenceDistance, Is.EqualTo(23f));
                await Replay("D17 MRI contrast", () => desktopScene.MRIManager.SetCalValues(0.15f, 0.85f));
                Assert.That(questScene.MRIManager.MRICalMinFactor, Is.EqualTo(0.15f));
                Assert.That(questScene.MRIManager.MRICalMaxFactor, Is.EqualTo(0.85f));

                await Replay("D23 static source and span", () =>
                {
                    var column = desktopScene.Columns.OfType<Column3DStatic>().Single();
                    column.SelectedLabelIndex = 1;
                    column.StaticParameters.SetSpanValues(-2f, 0f, 2f);
                    column.StaticParameters.InfluenceDistance = 20f;
                });
                var questStatic = questScene.Columns.OfType<Column3DStatic>().Single();
                Assert.That(questStatic.SelectedLabelIndex, Is.EqualTo(1));
                Assert.That(questStatic.StaticParameters.SpanMin, Is.EqualTo(-2f));

                await Replay("D24 iEEG span and influence", () =>
                {
                    desktopIEEG.DynamicParameters.SetSpanValues(-3f, 0f, 3f);
                    desktopIEEG.DynamicParameters.InfluenceDistance = 18f;
                });
                Assert.That(questIEEG.DynamicParameters.SpanMax, Is.EqualTo(3f));
                Assert.That(questIEEG.DynamicParameters.InfluenceDistance, Is.EqualTo(18f));

                await Replay("D24 CCEP span and influence", () =>
                {
                    var column = desktopScene.Columns.OfType<Column3DCCEP>().Single();
                    column.DynamicParameters.SetSpanValues(-4f, 0f, 4f);
                    column.DynamicParameters.InfluenceDistance = 19f;
                });

                await Replay("D26 MEG source", () => desktopScene.Columns.OfType<Column3DMEG>().Single().SelectedMEGIndex = 1);
                Assert.That(questScene.Columns.OfType<Column3DMEG>().Single().SelectedMEGIndex, Is.EqualTo(1));

                await Replay("D27 iEEG seek and step", () =>
                {
                    desktopIEEG.NavigationTimeline.CurrentIndex = 2;
                    desktopIEEG.NavigationTimeline.Step = 2;
                    desktopIEEG.NavigationTimeline.IsLooping = true;
                });
                Assert.That(questIEEG.NavigationTimeline.CurrentIndex, Is.EqualTo(2));
                Assert.That(questIEEG.NavigationTimeline.Step, Is.EqualTo(2));

                await Replay("D28 Mars atlas display", () =>
                {
                    desktopScene.AtlasManager.DisplayMarsAtlas = true;
                    desktopScene.AtlasManager.AtlasAlpha = 0.4f;
                });
                await desktopScene.PrepareRenderingAsync(token);
                await questScene.PrepareRenderingAsync(token);
                Assert.That(questScene.Columns[0].BrainMesh.GetComponent<MeshFilter>().sharedMesh.colors, Is.EqualTo(desktopScene.Columns[0].BrainMesh.GetComponent<MeshFilter>().sharedMesh.colors));
                Assert.That(wrapper.localScale, Is.EqualTo(Vector3.one * 2));

                var unavailableMarsSource = sender.Capture(revision + 1).Fields;
                unavailableMarsSource[new StateKey(EntityKind.Column, "", "ccep", 16)] = StateValue.Int((int)Column3DCCEP.CCEPMode.MarsAtlas);
                unavailableMarsSource[new StateKey(EntityKind.Column, "", "ccep", 17)] = StateValue.Text("");
                unavailableMarsSource[new StateKey(EntityKind.Column, "", "ccep", 18)] = StateValue.Text(int.MaxValue.ToString());
                Assert.Throws<InvalidDataException>(() => receiver.Apply(sender.Capture(revision + 1).WithFields(unavailableMarsSource, revision + 1)));
                await Replay("D25 CCEP source reset", () => desktopScene.Columns.OfType<Column3DCCEP>().Single().SelectedSourceSite = null);
                var questCCEP = questScene.Columns.OfType<Column3DCCEP>().Single();
                Assert.That(questCCEP.SelectedSourceSite, Is.Null);
                await Replay("D25 CCEP source restore", () => desktopScene.Columns.OfType<Column3DCCEP>().Single().SelectedSourceSite = desktopScene.Columns.OfType<Column3DCCEP>().Single().Sources.Single());
                Assert.That(questCCEP.SelectedSourceSite.Information.FullID, Is.EqualTo("patient_A1"));
                int marsSourceLabel = Object3DManager.MarsAtlas.Labels()[0];
                await Replay("D25 CCEP Mars area source", () =>
                {
                    var column = desktopScene.Columns.OfType<Column3DCCEP>().Single();
                    column.Mode = Column3DCCEP.CCEPMode.MarsAtlas;
                    column.SelectedSourceMarsAtlasLabel = marsSourceLabel;
                });
                Assert.That(questCCEP.SelectedSourceMarsAtlasLabel, Is.EqualTo(marsSourceLabel));
                Assert.That(questCCEP.AreaMask, Is.EqualTo(desktopScene.Columns.OfType<Column3DCCEP>().Single().AreaMask));
                Assert.That(questCCEP.ActivityValues, Is.EqualTo(desktopScene.Columns.OfType<Column3DCCEP>().Single().ActivityValues));
                await Replay("D25 CCEP site source after Mars area", () =>
                {
                    var column = desktopScene.Columns.OfType<Column3DCCEP>().Single();
                    column.Mode = Column3DCCEP.CCEPMode.Site;
                    column.SelectedSourceSite = column.Sources.Single();
                });

                await Replay("D26 fMRI calibration", () =>
                {
                    var column = desktopScene.Columns.OfType<Column3DFMRI>().Single();
                    column.FMRIParameters.SetSpanValues(0.1f, 0.8f, 0.2f, 0.9f);
                    column.FMRIParameters.SetHideValues(true, false, true);
                });
                var questFMRI = questScene.Columns.OfType<Column3DFMRI>().Single();
                Assert.That(questFMRI.FMRIParameters.FMRINegativeCalMinFactor, Is.EqualTo(0.1f));
                Assert.That(questFMRI.FMRIParameters.HideLowerValues, Is.True);

                await Replay("D26 MEG calibration", () =>
                {
                    var column = desktopScene.Columns.OfType<Column3DMEG>().Single();
                    column.MEGParameters.SetSpanValues(0.1f, 0.7f, 0.2f, 0.9f);
                    column.MEGParameters.SetHideValues(false, true, false);
                });
                var questMEG = questScene.Columns.OfType<Column3DMEG>().Single();
                Assert.That(questMEG.MEGParameters.FMRINegativeCalMaxFactor, Is.EqualTo(0.7f));
                Assert.That(questMEG.MEGParameters.HideMiddleValues, Is.True);

                await Replay("D31 fMRI atlas calibration", () =>
                {
                    desktopScene.FMRIManager.FMRIAlpha = 0.6f;
                    desktopScene.FMRIManager.FMRINegativeCalMinFactor = 0.1f;
                    desktopScene.FMRIManager.FMRINegativeCalMaxFactor = 0.8f;
                    desktopScene.FMRIManager.FMRIPositiveCalMinFactor = 0.2f;
                    desktopScene.FMRIManager.FMRIPositiveCalMaxFactor = 0.9f;
                });
                Assert.That(questScene.FMRIManager.FMRIAlpha, Is.EqualTo(0.6f));

                await Replay("D10 site filtering", () => desktopIEEG.Sites[1].State.IsFiltered = false);
                Assert.That(questIEEG.Sites[1].State.IsFiltered, Is.False);
                await Replay("D11 site highlight and blacklist", () =>
                {
                    desktopIEEG.Sites[1].State.IsHighlighted = true;
                    desktopIEEG.Sites[1].State.IsBlackListed = true;
                });
                Assert.That(questIEEG.Sites[1].State.IsBlackListed, Is.True);
                await Replay("D12 site color and labels", () =>
                {
                    desktopIEEG.Sites[0].State.Color = Color.cyan;
                    desktopIEEG.Sites[0].State.Labels.Add("synchronized-label");
                });
                Assert.That(questIEEG.Sites[0].State.Labels, Does.Contain("synchronized-label"));
                await Replay("D34 bulk site attributes", () =>
                {
                    foreach (var site in desktopIEEG.Sites)
                    {
                        site.State.IsHighlighted = true;
                        site.State.Color = Color.magenta;
                        site.State.Labels.Add("bulk-label");
                    }
                });
                Assert.That(questIEEG.Sites.All(site => site.State.IsHighlighted && site.State.Color == Color.magenta && site.State.Labels.Contains("bulk-label")), Is.True);
                await Replay("D14 site position", () => desktopIEEG.Sites[0].transform.localPosition += new Vector3(1, 2, 3));
                Assert.That(questIEEG.Sites[0].transform.localPosition, Is.EqualTo(desktopIEEG.Sites[0].transform.localPosition));
                await Replay("D20 erase surface triangles", () =>
                {
                    int[] fullMask = desktopScene.MeshManager.BrainSurface.VisibilityMask;
                    int[] simplifiedMask = desktopScene.MeshManager.SimplifiedMeshToUse.VisibilityMask;
                    fullMask[1] = 0;
                    simplifiedMask[1] = 0;
                    desktopScene.TriangleEraser.CurrentMasks = new List<int[]> { fullMask, simplifiedMask };
                });
                Assert.That(questScene.MeshManager.BrainSurface.VisibilityMask, Is.EqualTo(desktopScene.MeshManager.BrainSurface.VisibilityMask));
                Assert.That(questScene.MeshManager.SimplifiedMeshToUse.VisibilityMask, Is.EqualTo(desktopScene.MeshManager.SimplifiedMeshToUse.VisibilityMask));

                ROI roi = null;
                await Replay("D7 create ROI", () => roi = desktopScene.ROIManager.AddROI("Synchronized ROI"));
                Assert.That(questScene.ROIManager.ROIs.Single().ID, Is.EqualTo(roi.ID));
                await Replay("D8 clear active ROI", () => desktopScene.ROIManager.SelectedROI = null);
                Assert.That(questScene.ROIManager.SelectedROI, Is.Null);
                await Replay("D8 select active ROI", () => desktopScene.ROIManager.SelectedROI = roi);
                Assert.That(questScene.ROIManager.SelectedROI.ID, Is.EqualTo(roi.ID));
                await Replay("D9 add ROI sphere", () => roi.AddSphere(Module3DMain.DEFAULT_MESHES_LAYER, "Sphere", first.Information.DefaultPosition, 2f));
                Assert.That(questIEEG.Sites[0].State.IsOutOfROI, Is.False);
                Assert.That(questIEEG.Sites[1].State.IsOutOfROI, Is.True);
                await Replay("D9 move and resize ROI sphere", () =>
                {
                    roi.MoveSelectedSphere(second.Information.DefaultPosition - first.Information.DefaultPosition);
                    roi.Spheres.Single().SetInfluenceRadius(2.5f);
                });
                Assert.That(questIEEG.Sites[0].State.IsOutOfROI, Is.True);
                Assert.That(questIEEG.Sites[1].State.IsOutOfROI, Is.False);
                Assert.That(questScene.ROIManager.ROIs.Single().Spheres.Single().InfluenceRadius, Is.EqualTo(2.5f));
                await Replay("D7 rename ROI", () => roi.Name = "Renamed synchronized ROI");
                Assert.That(questScene.ROIManager.ROIs.Single().Name, Is.EqualTo(roi.Name));
                await Replay("D7 delete ROI", () => desktopScene.ROIManager.RemoveROI(roi));
                Assert.That(questScene.ROIManager.ROIs, Is.Empty);

                await desktopScene.PrepareRenderingAsync(token);
                await questScene.PrepareRenderingAsync(token);
                Assert.That(questStatic.ActivityValues, Is.EqualTo(desktopScene.Columns.OfType<Column3DStatic>().Single().ActivityValues));
                Assert.That(questCCEP.ActivityValues, Is.EqualTo(desktopScene.Columns.OfType<Column3DCCEP>().Single().ActivityValues));
                Assert.That(questIEEG.ActivityValues, Is.EqualTo(desktopIEEG.ActivityValues));
                Assert.That(wrapper.localScale, Is.EqualTo(Vector3.one * 2));

                await Replay("D28 Mars atlas hidden for functional projection", () => desktopScene.AtlasManager.DisplayMarsAtlas = false);
                await Replay("D32 remove activity", () => desktopScene.SetProjectionEnabled(false));
                Assert.That(questScene.ProjectionEnabled, Is.False);
                await Replay("D32 compute activity", () => desktopScene.RequestActivityProjection());
                Assert.That(desktopScene.IsGeneratorUpToDate, Is.True);
                desktopIEEG.NavigationTimeline.IsLooping = true;
                StateSnapshot projection = sender.Capture(++revision);
                receiver.Apply(projection);
                await questScene.PrepareRenderingAsync(token);
                Assert.That(questScene.IsGeneratorUpToDate, Is.True);
                Assert.That(questIEEG.NavigationTimeline.IsLooping, Is.True, "Native recomputation must preserve the accepted timeline.");
                Assert.That(questIEEG.SurfaceGenerator.ActivityUV, Is.EqualTo(desktopIEEG.SurfaceGenerator.ActivityUV));
                Assert.That(SharedStateCodec.Encode(receiver.Capture(revision)), Is.EqualTo(SharedStateCodec.Encode(projection)));

                await Replay("D29 IBC contrast", () =>
                {
                    var fmri = desktopScene.FMRIManager;
                    fmri.SelectedIBCContrastID = 1;
                    fmri.DisplayIBCContrasts = true;
                });
                await desktopScene.PrepareRenderingAsync(token);
                await questScene.PrepareRenderingAsync(token);
                Assert.That(questScene.FMRIManager.CurrentFMRI, Is.SameAs(ibcVolume));
                Assert.That(questScene.FMRIManager.CurrentVolume, Is.SameAs(ibcVolume.Volumes[1]));
                Assert.That(questScene.Columns[0].BrainMesh.GetComponent<MeshFilter>().sharedMesh.colors, Is.EqualTo(desktopScene.Columns[0].BrainMesh.GetComponent<MeshFilter>().sharedMesh.colors));

                await Replay("D29 DiFuMo selection and area", () =>
                {
                    var fmri = desktopScene.FMRIManager;
                    fmri.DisplayIBCContrasts = false;
                    fmri.SelectedDiFuMoAtlas = "s2-fixture";
                    fmri.SelectedDiFuMoArea = 1;
                    fmri.DisplayDiFuMo = true;
                });
                await desktopScene.PrepareRenderingAsync(token);
                await questScene.PrepareRenderingAsync(token);
                Assert.That(questScene.FMRIManager.CurrentFMRI, Is.SameAs(difumoVolume));
                Assert.That(questScene.FMRIManager.CurrentVolume, Is.SameAs(difumoVolume.Volumes[1]));
                Assert.That(questScene.Columns[0].BrainMesh.GetComponent<MeshFilter>().sharedMesh.colors, Is.EqualTo(desktopScene.Columns[0].BrainMesh.GetComponent<MeshFilter>().sharedMesh.colors));

                await Replay("D30 localizer selection and thresholds", () =>
                {
                    var fmri = desktopScene.FMRIManager;
                    fmri.DisplayDiFuMo = false;
                    fmri.SelectedLocalizersProtocol = "protocol-alpha";
                    fmri.SelectedLocalizersData = "signal-alpha";
                    fmri.SelectedLocalizersBloc = "bloc-alpha";
                    fmri.SelectedLocalizersTimelineIndex = 0;
                    fmri.LocalizersMin = 0.1f;
                    fmri.LocalizersMiddle = 0.5f;
                    fmri.LocalizersMax = 0.9f;
                    fmri.DisplayLocalizers = true;
                });
                await desktopScene.PrepareRenderingAsync(token);
                await questScene.PrepareRenderingAsync(token);
                Assert.That(questScene.FMRIManager.CurrentFMRI, Is.SameAs(localizerBloc.FMRI));
                Assert.That(questScene.FMRIManager.CurrentLocalizersMask, Is.SameAs(localizerBloc.FMRI.MaskVolume));
                Assert.That(questScene.Columns[0].BrainMesh.GetComponent<MeshFilter>().sharedMesh.colors, Is.EqualTo(desktopScene.Columns[0].BrainMesh.GetComponent<MeshFilter>().sharedMesh.colors));
                Assert.That(wrapper.localScale, Is.EqualTo(Vector3.one * 2));

                desktopScene.SaveConfiguration();
                await Replay("D33 reset configuration", () => desktopScene.ResetConfiguration());
                await Replay("D33 load configuration", () => desktopScene.LoadConfiguration());
            }
            finally
            {
                await UniTask.SwitchToMainThread();
                await view.ClearAsync();
                if (desktop != null) await desktop.CloseAsync();
                if (localizerProtocol != null)
                {
                    Object3DManager.Localizers.Protocols.Remove(localizerProtocol);
                    localizerProtocol.Clean();
                }

                if (difumoVolume != null)
                {
                    Object3DManager.DiFuMo.FMRIs.Remove("s2-fixture");
                    Object3DManager.DiFuMo.Information.Remove("s2-fixture");
                    difumoVolume.Clean();
                }

                Object3DManager.IBC = previousIbc;
                ibcVolume?.Clean();

                Object.Destroy(view.gameObject);
                await UniTask.NextFrame();
                // SceneArchive retires its pack after the final reader closes.
                // Only remove the parent once its owned archive folders are gone.
                if (!Directory.EnumerateFileSystemEntries(root).Any()) Directory.Delete(root);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        [Timeout(900000)]
        public async Task SnapshotConfiguration_OpensWithCommonPreferencesAndCalibrations(bool automatic)
        {
            using var temp = new PlayModeTempDirectoryScope();
            using var settings = new PlayModePersistentDataScope(temp.Path);
            using var scope = new PlayModeSceneScope("SnapshotConfigurationParity");
            PersistentDataManager.UserPreferences.Visualization._3D.AutomaticEEGUpdate = automatic;
            var preferences = PersistentDataManager.UserPreferences;
            await PrepareReferencesAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(6));
            var token = timeout.Token;
            using var source = new SceneArchive(Path.Combine(temp.Path, "source"));
            var payload = CreateFixture(source);
            var ccepModel = (CCEPColumn)payload.Visualization.Columns[2];
            ccepModel.CCEPConfiguration.SpanMin = -11;
            ccepModel.CCEPConfiguration.Middle = 3;
            ccepModel.CCEPConfiguration.SpanMax = 17;
            ((AnatomicColumn)payload.Visualization.Columns[0]).AnatomicConfiguration.MaximumInfluence = 23;
            ((StaticColumn)payload.Visualization.Columns[3]).StaticConfiguration.SelectedResourceIndex = 1;
            ((MEGColumn)payload.Visualization.Columns[5]).MEGConfiguration.SelectedResourceIndex = 1;
            payload.Visualization.Configuration.AtlasConfiguration = new AtlasConfiguration { MarsAtlas = true, AtlasAlpha = .37f, IBCIndex = 4, NegativeMin = .1f, NegativeMax = .8f, PositiveMin = .2f, PositiveMax = .9f, FMRIAlpha = .6f, LocalizerMin = 70, LocalizerMiddle = 90, LocalizerMax = 130 };
            payload.Visualization.Configuration.RegionsOfInterest.Add(new RegionOfInterest("Scientific ROI", new List<Core.Data.Sphere> { new Core.Data.Sphere(Vector3.zero, 12f) }));
            string file = Path.Combine(temp.Path, "snapshot.hbscene");
            source.Write(payload, file);
            RestoredScene desktop = null, quest = null;
            try
            {
                var desktopArchive = new SceneArchive(Path.Combine(temp.Path, "desktop"), true, source.Globals);
                desktop = await SceneRestoration.PrepareAsync(desktopArchive.Read(file), desktopArchive, AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/3D/Scenes/Scene 3D.prefab").GetComponent<Base3DScene>(), scope.Root.transform, token);
                var questArchive = new SceneArchive(Path.Combine(temp.Path, "quest"), true, source.Globals);
                quest = await SceneRestoration.PrepareAsync(questArchive.Read(file), questArchive, AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/3D/Scenes/Scene 3D Content.prefab").GetComponent<Base3DScene>(), scope.Root.transform, token);
                foreach (var restored in new[] { desktop, quest })
                {
                    var scene = restored.Scene;
                    Assert.That(PersistentDataManager.UserPreferences, Is.SameAs(preferences));
                    Assert.That(scene.IsGeneratorUpToDate, Is.EqualTo(automatic));
                    var ccep = (Column3DCCEP)scene.Columns[2];
                    Assert.That(ccep.SelectedSourceSite.Information.FullID, Is.EqualTo("patient_A1"));
                    Assert.That(ccep.DynamicParameters.SpanMin, Is.EqualTo(-11));
                    Assert.That(ccep.DynamicParameters.Middle, Is.EqualTo(3));
                    Assert.That(ccep.DynamicParameters.SpanMax, Is.EqualTo(17));
                    Assert.That(((Column3DAnatomy)scene.Columns[0]).AnatomyParameters.InfluenceDistance, Is.EqualTo(23));
                    Assert.That(((Column3DStatic)scene.Columns[3]).SelectedLabelIndex, Is.EqualTo(1));
                    Assert.That(((Column3DMEG)scene.Columns[5]).SelectedMEGIndex, Is.EqualTo(1));
                    Assert.That(scene.TriangleEraser.CurrentMasks[0][0], Is.Zero);
                    Assert.That(scene.FMRIManager.DisplayIBCContrasts, Is.False);
                    Assert.That(scene.FMRIManager.SelectedIBCContrastID, Is.EqualTo(4));
                    Assert.That(scene.AtlasManager.AtlasAlpha, Is.EqualTo(.37f));
                    Assert.That(scene.AtlasManager.DisplayMarsAtlas, Is.True);
                    var expectedAtlasColors = Object3DManager.MarsAtlas.ConvertIndicesToColors(Object3DManager.MarsAtlas.GetSurfaceAreaLabels(scene.MeshManager.ReferenceSurface), -1);
                    foreach (var column in scene.Columns)
                    {
                        var mesh = column.BrainMesh.GetComponent<MeshFilter>().sharedMesh;
                        Assert.That(mesh.colors, Has.Length.EqualTo(mesh.vertexCount));
                        Assert.That(mesh.colors, Is.EqualTo(expectedAtlasColors));
                    }

                    var configuration = scene.CaptureConfiguration();
                    Assert.That(configuration.AtlasConfiguration.ID, Is.EqualTo(scene.Visualization.Configuration.AtlasConfiguration.ID));
                    Assert.That(configuration.RegionsOfInterest[0].Name, Is.EqualTo("Scientific ROI"));
                    Assert.That(configuration.RegionsOfInterest[0].Spheres[0].Radius, Is.EqualTo(12f));
                    Assert.That(configuration.AtlasConfiguration.IBCIndex, Is.EqualTo(4));
                    Assert.That(scene.Columns.All(column => column.NavigationTimeline == null || !column.NavigationTimeline.IsPlaying), Is.True);
                }

                if (!automatic)
                {
                    quest.Scene.UpdateGenerator();
                    await quest.Scene.PrepareRenderingAsync(token);
                    Assert.That(quest.Scene.IsGeneratorUpToDate, Is.True, "Manual calculation remains available with automatic calculation disabled.");
                    Assert.That(desktop.Scene.IsGeneratorUpToDate, Is.False);
                }
            }
            finally
            {
                if (quest != null) await quest.CloseAsync();
                if (desktop != null) await desktop.CloseAsync();
                await UniTask.NextFrame();
            }
        }

        [TestCase("scene-008")]
        [TestCase("scene-008-patient")]
        [Timeout(900000)]
        public async Task DesktopProject_LoadsSavesAndTransfersAllSixModalities(string fixtureName)
        {
            await FailOnUnexpectedCancellation(() => DesktopProjectScenarioAsync(fixtureName));
        }

        private static async Task DesktopProjectScenarioAsync(string fixtureName)
        {
            string fixture = Path.GetFullPath(".artifacts/scene-008/fixture/" + fixtureName + ".hibop");
            Assert.That(File.Exists(fixture), Is.True, "Run Tools/Prepare-SceneQualificationFixture.py.");
            using var temp = new PlayModeTempDirectoryScope();
            using var app = new PlayModeApplicationStateScope(temp.Path);
            typeof(ApplicationState).GetProperty(nameof(ApplicationState.DataPath), BindingFlags.Public | BindingFlags.Static).SetValue(null, Path.GetFullPath("Assets/Data"));
            using var settings = new PlayModePersistentDataScope(temp.Path);
            using var scope = new PlayModeSceneScope("FullDesktopScene");
            PersistentDataManager.UserPreferences.Visualization._3D.AutomaticEEGUpdate = true;
            await PrepareReferencesAsync();
            // Two complete buffer exports plus installed-reference hashing exceed four
            // minutes on the Editor's Mono runtime; Players are measured separately.
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(6));
            var clock = System.Diagnostics.Stopwatch.StartNew();
            var token = timeout.Token;
            var protocol = ClassLoaderSaver.LoadFromJson<Protocol>(Path.GetFullPath(".artifacts/scene-008/fixture/scene-008.prov"));
            Core.Database.DatabaseManager.Database.SetProtocols(new[] { protocol });
            var info = new ProjectInfo(fixture);
            var project = new Project(info.Name, new ProjectPreferences("placeholder"));
            ApplicationState.LoadedProject = project;
            ApplicationState.LoadedProjectLocation = temp.Path;
            await project.LoadAsync(info, (_, _, _) => { }, token);
            await project.CurrentLoadingOperation.EnsureValidatedAsync(token);
            Assert.That(project.StructuralRecoveryReport.HasIssues, Is.False);
            var model = project.Visualizations.Single();
            Assert.That(model.IsVisualizable, Is.True, "Incompatible columns: " + string.Join(", ", model.Columns.Where(c => !c.IsCompatible(model.Patients)).Select(c => c.Name)));
            await model.LoadAsync((_, _, _) => { }, token);
            Debug.Log($"Scene qualification {fixtureName}: project/model loaded in {clock.Elapsed.TotalSeconds:F1}s.");
            await UniTask.SwitchToMainThread();
            var desktop = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/3D/Scenes/Scene 3D.prefab"), scope.Root.transform).GetComponent<Base3DScene>();
            var quest = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/QuestAnatomy.prefab"), scope.Root.transform).GetComponent<QuestAnatomyView>();
            try
            {
                desktop.Initialize(model);
                await desktop.InitializeAsync(model, (_, _, _) => { }, token);
                await desktop.CompleteInitializationAsync(null, null, token);
                await desktop.PrepareRenderingAsync(token);
                Debug.Log($"Scene qualification {fixtureName}: Desktop prepared at {clock.Elapsed.TotalSeconds:F1}s.");
                Assert.That(desktop.Columns.Count, Is.EqualTo(6));
                Assert.That(desktop.Columns.All(c => c.Views.Count == 1), Is.True);
                var ccep = desktop.Columns.OfType<Column3DCCEP>().Single();
                ccep.SelectedSourceSite = ccep.Sources.First();
                await desktop.PrepareRenderingAsync(token);
                Assert.That(ccep.ActivityValues.Any(v => v != 0), Is.True);
                await SceneQualification.RunAsync(desktop, Path.GetFullPath(".artifacts/scene-008/editor-desktop-" + fixtureName), token);
                Debug.Log($"Scene qualification {fixtureName}: Desktop diagnostic completed at {clock.Elapsed.TotalSeconds:F1}s.");
                var globals = new PairingContext(new GlobalDataPayload { Preferences = PersistentDataManager.UserPreferences, Tags = PersistentDataManager.Tags, Protocols = new() { protocol }, Aliases = PersistentDataManager.Aliases, Grid = Core.DLL.ActivityProjectionSettings.VolumeGridDimension, Interpolation = Core.DLL.ActivityProjectionSettings.VolumeInterpolation });
                using var delivery = await DesktopSceneCapture.CaptureDeliveryAsync(desktop, "desktop", "qualification", 1, globals, token);
                string file = (string)typeof(SceneDelivery).GetField("file", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(delivery);
                var archive = new SceneArchive(Path.Combine(temp.Path, "quest"), true, globals);
                await quest.ApplyAsync(archive.Read(file), archive, token);
                byte[] digest = Enumerable.Range(0, 32).Select(index => Convert.ToByte(delivery.ContentHash.Substring(index * 2, 2), 16)).ToArray();
                var receipt = new HBP.Transfer.Transport.DeliveryReceipt(digest, HBP.Transfer.Transport.DeliveryStatus.Published);
                Guid epoch = Guid.NewGuid();
                var desktopBinding = PreparedSceneDeliveryBinding.FromSent(delivery, receipt);
                var questBinding = PreparedSceneDeliveryBinding.FromPublished(receipt, quest.PublishedScene);
                LiveGeometryStateAdapter desktopAdapter = new(desktop, epoch, desktopBinding);
                LiveGeometryStateAdapter questAdapter = new(quest.Scene, epoch, questBinding);
                StateSnapshot openingState = desktopAdapter.Capture(0);
                questAdapter.BindInitialState(openingState);
                Assert.That(SharedStateCodec.Encode(questAdapter.Capture(0)), Is.EqualTo(SharedStateCodec.Encode(openingState)), "The live Desktop scene and published Quest scene must bind to the same captured manifest.");
                Debug.Log($"Scene qualification {fixtureName}: Quest scene restored at {clock.Elapsed.TotalSeconds:F1}s.");
                Assert.That(quest.Scene.Columns.Count, Is.EqualTo(6));
                await SceneQualification.RunAsync(quest.Scene, Path.GetFullPath(".artifacts/scene-008/editor-restored-" + fixtureName), token);
                Debug.Log($"Scene qualification {fixtureName}: restored diagnostic completed at {clock.Elapsed.TotalSeconds:F1}s.");
                // Save a test-owned project, then resolve it afresh through the normal project loader.
                await project.SaveAsync(temp.Path, (_, _, _) => { }, token);
                var savedInfo = new ProjectInfo(Path.Combine(temp.Path, project.FileName));
                var reloaded = new Project(savedInfo.Name, new ProjectPreferences("reload"));
                ApplicationState.LoadedProject = reloaded;
                await reloaded.LoadAsync(savedInfo, (_, _, _) => { }, token);
                await reloaded.CurrentLoadingOperation.EnsureValidatedAsync(token);
                Assert.That(reloaded.StructuralRecoveryReport.HasIssues, Is.False);
                Assert.That(reloaded.Visualizations.Single().Columns.Select(c => c.GetType()), Is.EqualTo(model.Columns.Select(c => c.GetType())));
                Debug.Log($"Scene qualification {fixtureName}: save/reload verified at {clock.Elapsed.TotalSeconds:F1}s.");
            }
            finally
            {
                await quest.ClearAsync();
                await desktop.CleanAsync();
                Object.Destroy(quest.gameObject);
                await UniTask.NextFrame();
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        [Timeout(900000)]
        public async Task CompleteScene_RestoresNativeModalitiesAndIndependentColumns_ThenRecaptures(bool blocks)
        {
            await FailOnUnexpectedCancellation(() => CompleteSceneScenarioAsync(blocks));
        }

        private static async Task CompleteSceneScenarioAsync(bool blocks)
        {
            string root = Path.Combine(Path.GetTempPath(), "hibop-scene-runtime-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);

            using var settings = new PlayModePersistentDataScope(root);
            using var scope = new PlayModeSceneScope("SceneTransfer");
            PersistentDataManager.UserPreferences.Visualization._3D.AutomaticEEGUpdate = true;
            await PrepareReferencesAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(4));
            var token = timeout.Token;
            var view = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/QuestAnatomy.prefab"), scope.Root.transform).GetComponent<QuestAnatomyView>();
            try
            {
                await UniTask.SwitchToMainThread();
                using var source = new SceneArchive(Path.Combine(root, "source"), deferResourceWrites: blocks);
                ScenePayload payload = CreateFixture(source);
                string file = Path.Combine(root, "fixture.hbscene");
                source.Write(payload, file); // Also retained for the later invalid-replacement scenarios.
                var archive = new SceneArchive(Path.Combine(root, "received"), true, source.Globals);
                ScenePayload received;
                if (blocks)
                {
                    var packets = new List<byte[]>();
                    HBP.Transfer.Transport.BlockContainer.Produce(source.CaptureBlockResources(source.CaptureMetadata(payload)), packets.Add, token);
                    using var input = new MemoryStream(packets.SelectMany(p => p).ToArray());
                    input.Position = 4;
                    await HBP.Transfer.Transport.BlockContainer.ReceiveAsync(input, archive, token);
                    received = archive.ReadPrepared();
                }
                else
                {
                    received = archive.Read(file);
                }

                await view.ApplyAsync(received, archive, token);
                var scene = view.Scene;
                Assert.That(scene.DesktopPresentation, Is.Null);
                Assert.That(scene.Columns.Select(c => c.GetType()), Is.EqualTo(new[] { typeof(Column3DAnatomy), typeof(Column3DIEEG), typeof(Column3DCCEP), typeof(Column3DStatic), typeof(Column3DFMRI), typeof(Column3DMEG) }));
                Assert.That(scene.Columns.All(c => c.Views.Count == 0), Is.True);
                Assert.That(scene.GetComponentsInChildren<Camera>(true), Is.Empty);
                Assert.That(view.Columns.Count, Is.EqualTo(6));
                foreach (var column in scene.Columns)
                {
                    Assert.That(column.BrainMesh.GetComponent<MeshFilter>().sharedMesh.vertexCount, Is.GreaterThan(0), column.Name);
                    Assert.That(column.Sites.Count, Is.EqualTo(2), column.Name);
                    if (column.NavigationTimeline != null) Assert.That(column.NavigationTimeline.IsPlaying, Is.False, column.Name);
                }

                var ieeg = scene.Columns.OfType<Column3DIEEG>().Single();
                var ccep = scene.Columns.OfType<Column3DCCEP>().Single();
                Assert.That(scene.TriangleEraser.CurrentMasks[0][0], Is.Zero, "Restored erasure must survive geometry preparation.");
                Assert.That(ccep.SelectedSourceSite.Information.FullID, Is.EqualTo("patient_A1"));
                Assert.That(ccep.ActivityValues, Does.Contain(8f));
                await SceneQualification.RunAsync(scene, Path.GetFullPath(".artifacts/scene-008/editor-six-modalities"), token);
                Assert.That(ieeg.Timeline, Is.Not.SameAs(ccep.Timeline));
                int otherIndex = ccep.Timeline.CurrentIndex;
                ieeg.Timeline.CurrentIndex = 2;
                await scene.PrepareRenderingAsync(token);
                Assert.That(ccep.Timeline.CurrentIndex, Is.EqualTo(otherIndex));
                Assert.That(ieeg.ColumnIEEGData.Data.DataByChannelID["patient_A1"].Trials[1].IsValid, Is.False);
                var originalSites = ieeg.Sites.Select(s => s.transform.localPosition).ToArray();
                Vector3 otherPose = view.Columns[0].transform.position;
                view.Columns[1].transform.SetPositionAndRotation(new Vector3(1, 2, 3), Quaternion.Euler(10, 20, 30));
                view.Columns[1].transform.localScale = Vector3.one * 2;
                Assert.That(ieeg.Sites.Select(s => s.transform.localPosition), Is.EqualTo(originalSites));
                Assert.That(view.Columns[0].transform.position, Is.EqualTo(otherPose));
                Assert.That(scene.Columns.Select(c => c.BrainMesh.GetComponent<MeshFilter>().sharedMesh).Distinct().Count(), Is.EqualTo(6));

                var stat = scene.Columns.OfType<Column3DStatic>().Single();
                stat.SelectedLabelIndex = 1;
                var fmri = scene.Columns.OfType<Column3DFMRI>().Single();
                fmri.Timeline.CurrentIndex = fmri.Timeline.Length - 1;
                var meg = scene.Columns.OfType<Column3DMEG>().Single();
                meg.SelectedMEGIndex = 1;
                await scene.PrepareRenderingAsync(token);
                Assert.That(meg.SelectedFMRI.Volumes.Count, Is.GreaterThan(0));
                Assert.That(stat.SelectedLabelIndex, Is.EqualTo(1));
                var cut = scene.AddCutPlane();
                scene.UpdateCutPlane(cut);
                await scene.PrepareRenderingAsync(token);
                Assert.That(scene.Columns.All(c => c.BrainCutMeshes.Count == 1), Is.True);

                using var delivery = await DesktopSceneCapture.CaptureDeliveryAsync(scene, "recaptured", "runtime", 2, source.Globals, token);
                string capturedFile = (string)typeof(SceneDelivery).GetField("file", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(delivery);
                using var capturedArchive = new SceneArchive(Path.Combine(root, "recaptured"), true, source.Globals);
                var captured = capturedArchive.Read(capturedFile);
                Assert.That(captured.Columns.Count, Is.EqualTo(6));
                Assert.That(((StaticColumn)captured.Visualization.Columns[3]).StaticConfiguration.SelectedResourceIndex, Is.EqualTo(1));
                Assert.That(((MEGColumn)captured.Visualization.Columns[5]).MEGConfiguration.SelectedResourceIndex, Is.EqualTo(1));
                Assert.That(((IEEGColumn)captured.Visualization.Columns[1]).Data.ProcessedValuesByChannel["patient_A1"], Is.EqualTo(new[] { 1f, 2f, 3f, 4f }));
                Assert.That(captured.Visualization.Configuration.Cuts.Count, Is.EqualTo(1));

                // A failed replacement must leave the current common scene and poses alive.
                var invalidArchive = new SceneArchive(Path.Combine(root, "invalid"), true, source.Globals);
                var invalid = invalidArchive.Read(file);
                invalid.StandardFiles["IRM/MNI.nii"] = new string('0', 64);
                Exception failure = null;
                try
                {
                    await view.ApplyAsync(invalid, invalidArchive, token);
                }
                catch (Exception exception)
                {
                    failure = exception;
                }
                finally
                {
                    invalidArchive.Dispose();
                }

                Assert.That(failure, Is.TypeOf<InvalidDataException>());
                Assert.That(view.Scene, Is.SameAs(scene));
                Assert.That(view.Columns[1].transform.localScale, Is.EqualTo(Vector3.one * 2));
                using var incompatibleArchive = new SceneArchive(Path.Combine(root, "incompatible-topology"), true, source.Globals);
                var incompatible = incompatibleArchive.Read(file);
                incompatible.Visualization.Configuration.ErasedTriangles = new[] { 1 };
                UnityEngine.TestTools.LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("InvalidDataException: Configured erasure does not match"));
                Exception topologyFailure = null;
                try
                {
                    await view.ApplyAsync(incompatible, incompatibleArchive, token);
                }
                catch (Exception exception)
                {
                    topologyFailure = exception;
                }

                Assert.That(topologyFailure, Is.TypeOf<InvalidDataException>());
                Assert.That(view.Scene, Is.SameAs(scene), "An invalid configuration must not replace the published scene.");
                // Cancel a real preparation after it has started reading its native inputs.
                using var cancel = new CancellationTokenSource();
                var cancelledArchive = new SceneArchive(Path.Combine(root, "cancelled"), true, source.Globals);
                Task replacement = view.ApplyAsync(cancelledArchive.Read(file), cancelledArchive, cancel.Token);
                await UniTask.NextFrame();
                cancel.Cancel();
                Exception cancellation = null;
                try
                {
                    await replacement;
                }
                catch (Exception exception)
                {
                    cancellation = exception;
                }
                finally
                {
                    cancelledArchive.Dispose();
                }

                Assert.That(cancellation, Is.InstanceOf<OperationCanceledException>());
                Assert.That(view.Scene, Is.SameAs(scene));
                await scene.PrepareRenderingAsync(token);
                Assert.That(scene.Columns.All(c => c.BrainMesh.GetComponent<MeshFilter>().sharedMesh.vertexCount > 0), Is.True);
                Debug.Log($"Native common scene completed: six modalities, {scene.Columns.Sum(c => c.Sites.Count)} rendered sites, recapture {delivery.EncodedBytes} bytes.");
                await view.ClearAsync();
            }
            finally
            {
                await view.ClearAsync();
                Object.Destroy(view.gameObject);
                await UniTask.NextFrame();
                Directory.Delete(root, true);
            }
        }

        [Test]
        public async Task UnexpectedCancellation_IsReportedAsFailure()
        {
            Exception failure = null;
            try
            {
                await FailOnUnexpectedCancellation(() => Task.FromCanceled(new CancellationToken(true)));
            }
            catch (Exception exception)
            {
                failure = exception;
            }

            Assert.That(failure, Is.TypeOf<AssertionException>());
        }

        private static async Task FailOnUnexpectedCancellation(Func<Task> scenario)
        {
            // Unity Test Framework's Task wrapper only checks IsFaulted, not IsCanceled.
            // Turn unexpected cancellation into a fault so an incomplete scenario cannot pass.
            try
            {
                await scenario();
            }
            catch (OperationCanceledException exception)
            {
                throw new AssertionException("Qualification cancelled before completion: " + exception);
            }
        }

        private static async UniTask PrepareReferencesAsync()
        {
            // Shared native preparation cannot be cancelled. Await it before starting the
            // scenario timeout; the NUnit guard leaves room for cold preparation and cleanup.
            var clock = System.Diagnostics.Stopwatch.StartNew();
            Debug.Log("Scene qualification: preparing standard references.");
            await Base3DScene.PrepareStandardResourcesAsync();
            await UniTask.SwitchToMainThread();
            Debug.Log($"Scene qualification: standard references prepared in {clock.Elapsed.TotalSeconds:F1}s.");
        }

        private static ScenePayload CreateFixture(SceneArchive archive, bool marsTags = false)
        {
            var ev = new Event("event", new[] { 1 }, MainSecondaryEnum.Main, "event");
            var sub = new SubBloc("sub", 0, MainSecondaryEnum.Main, new TimeWindow(0, 30), new TimeWindow(0, 0), new[] { ev }, Array.Empty<Icon>(), Array.Empty<Treatment>(), "sub");
            var bloc = new Bloc("bloc", 0, "", "sub_event_CODE", new[] { sub }, "bloc");
            var protocol = new Protocol("SCENE-008 synthetic", new[] { bloc }, "scene-protocol");
            var dataset = new Dataset("synthetic", protocol, Array.Empty<DataInfo>(), "dataset");
            var ieeg = new IEEGColumn("iEEG", new BaseConfiguration(), dataset, "signal", bloc, new DynamicConfiguration(), "ieeg");
            var ccep = new CCEPColumn("CCEP", new BaseConfiguration(), dataset, "signal", bloc, new CCEPConfiguration(), "ccep");
            var stats = new Dictionary<SubBloc, List<SubBlocEventsStatistics>> { [sub] = new() { new SubBlocEventsStatistics { StatisticsByEvent = new() { [ev] = new EventStatistics() } } } };
            var indices = new Dictionary<SubBloc, int> { [sub] = 0 };
            ieeg.Data.Timeline = new Timeline(bloc, stats, indices, new Frequency(200));
            ieeg.Data.ProjectionTimeline = new Timeline(bloc, stats, indices, new Frequency(100));
            ccep.Data.Timeline = ieeg.Data.Timeline;
            ccep.Data.ProjectionTimeline = ieeg.Data.ProjectionTimeline;
            ieeg.Data.ProcessedValuesByChannel["patient_A1"] = new[] { 1f, 2f, 3f, 4f };
            ieeg.Data.UnitByChannelID["patient_A1"] = "uV";
            ieeg.Data.DataByChannelID["patient_A1"] = new BlocChannelData(new[]
            {
                new ChannelTrial(new Dictionary<SubBloc, ChannelSubTrial> { [sub] = new ChannelSubTrial(new[] { 1f, 2f, 3f, 4f }, "uV", true, new() { [ev] = new EventInformation(Array.Empty<EventInformation.EventOccurence>()) }) }, true),
                new ChannelTrial(new Dictionary<SubBloc, ChannelSubTrial> { [sub] = new ChannelSubTrial(new[] { 5f, 6f }, "uV", false, new()) }, false)
            });
            var stat = new StaticColumn { ID = "static", Name = "Static" };
            ccep.Data.ProcessedValuesByChannelIDByStimulatedChannelID["patient_A1"] = new() { ["patient_A1"] = new[] { 2f, 4f, 6f, 8f } };
            ccep.Data.UnityByChannelIDByStimulatedChannelID["patient_A1"] = new() { ["patient_A1"] = "uV" };
            ccep.Data.DataByChannelIDByStimulatedChannelID["patient_A1"] = new() { ["patient_A1"] = ieeg.Data.DataByChannelID["patient_A1"] };
            ccep.Data.StatisticsByChannelIDByStimulatedChannelID["patient_A1"] = new();
            if (marsTags)
            {
                ccep.Data.ProcessedValuesByChannelIDByStimulatedChannelID["patient_A1"]["patient_A2"] = new[] { 3f, 5f, 7f, 9f };
                ccep.Data.UnityByChannelIDByStimulatedChannelID["patient_A1"]["patient_A2"] = "uV";
                ccep.Data.DataByChannelIDByStimulatedChannelID["patient_A1"]["patient_A2"] = ieeg.Data.DataByChannelID["patient_A1"];
            }

            stat.Data.ValueByChannelIDByLabel["first"] = new() { ["patient_A1"] = 7 };
            stat.Data.ValueByChannelIDByLabel["second"] = new() { ["patient_A1"] = -3 };
            var patient = new Patient { ID = "patient", Name = "Synthetic" };
            var marsTag = marsTags ? new StringTag("MarsAtlas", "s2-mars-atlas-tag") : null;
            int[] marsLabels = marsTags ? Object3DManager.MarsAtlas.Labels() : Array.Empty<int>();
            BaseTagValue[] FirstSiteTags() => marsTags ? new BaseTagValue[] { new StringTagValue(marsTag, $"{Object3DManager.MarsAtlas.Hemisphere(marsLabels[0])}_{Object3DManager.MarsAtlas.Name(marsLabels[0])}") } : Array.Empty<BaseTagValue>();
            BaseTagValue[] SecondSiteTags() => marsTags ? new BaseTagValue[] { new StringTagValue(marsTag, $"{Object3DManager.MarsAtlas.Hemisphere(marsLabels[1])}_{Object3DManager.MarsAtlas.Name(marsLabels[1])}") } : Array.Empty<BaseTagValue>();
            patient.Sites.Add(new Core.Data.Site("A1", new[] { new Coordinate("MNI", new Vector3(-31.25f, -18.5f, 26.75f)) }, FirstSiteTags(), "site1"));
            patient.Sites.Add(new Core.Data.Site("A2", new[] { new Coordinate("MNI", new Vector3(-27.5f, -15.25f, 29)) }, SecondSiteTags(), "site2"));
            var columns = new Column[] { new AnatomicColumn { ID = "anatomy", Name = "Density" }, ieeg, ccep, stat, new FMRIColumn { ID = "fmri", Name = "fMRI" }, new MEGColumn { ID = "meg", Name = "MEG" } };
            var model = new Visualization("SCENE-008 six modalities", new[] { patient }, columns);
            model.Configuration.MeshName = Object3DManager.MNI.GreyMatter.Name;
            model.Configuration.MRIName = Object3DManager.MNI.MRI.Name;
            model.Configuration.ImplantationName = "MNI";
            model.Configuration.FirstColumnToSelect = -1;
            TagCollection tags = marsTags ? new TagCollection(PersistentDataManager.Tags.GeneralTags, PersistentDataManager.Tags.PatientsTags, PersistentDataManager.Tags.SitesTags.Concat(new BaseTag[] { marsTag }), PersistentDataManager.Tags.ID) : PersistentDataManager.Tags;
            archive.Globals = new PairingContext(new GlobalDataPayload { Preferences = PersistentDataManager.UserPreferences, Tags = tags, Protocols = new() { protocol }, Aliases = PersistentDataManager.Aliases, Grid = Core.DLL.ActivityProjectionSettings.VolumeGridDimension, Interpolation = Core.DLL.ActivityProjectionSettings.VolumeInterpolation });
            var payload = new ScenePayload { TransferId = "fixture", SessionId = "runtime", Revision = 1, GlobalContextId = archive.Globals.Id, Visualization = model, StandardFiles = new(Object3DManager.MNI.ResourceHashes) };
            var mesh = Object3DManager.MNI.GreyMatter;
            payload.Meshes.Add(new MeshResource { Name = mesh.Name, Standard = "grey", Type = MeshType.MNI, StandardBothMask = mesh.Both.VisibilityMask, StandardLeftMask = mesh.Left.VisibilityMask, StandardRightMask = mesh.Right.VisibilityMask, SimplifiedBoth = archive.AddSurface(mesh.SimplifiedBoth), SimplifiedLeft = archive.AddSurface(mesh.SimplifiedLeft), SimplifiedRight = archive.AddSurface(mesh.SimplifiedRight) });
            payload.MRIs.Add(new VolumeResource { Name = Object3DManager.MNI.MRI.Name, Standard = "MNI" });
            payload.Visualization.Configuration.ErasedTriangles = Enumerable.Repeat(1, mesh.Both.NumberOfTriangles).ToArray();
            payload.Visualization.Configuration.ErasedSimplifiedTriangles = Enumerable.Repeat(1, mesh.SimplifiedBoth.NumberOfTriangles).ToArray();
            payload.Visualization.Configuration.ErasedTriangles[0] = 0;
            foreach (var column in columns)
            {
                var state = new ColumnState { Id = column.ID };
                if (column == ccep) ccep.CCEPConfiguration = new CCEPConfiguration { SiteID = "patient_A1" };
                payload.Columns.Add(state);
            }

            string fmri = Path.GetFullPath("Assets/Tests/Fixtures/Native/Nifti/fmri_4d.nii.gz");
            string image = archive.AddFile(fmri, StandardData.HashFile(fmri));
            payload.Columns[4].Functional.Add(new FunctionalResource { Name = "fMRI", File = image });
            var channels = new FunctionalResource { Name = "channels", Values = new() { ["A1"] = new[] { 1f, 2f, 3f } }, Units = new() { ["A1"] = "fT" }, Frequency = 100 };
            payload.Columns[5].Functional.Add(channels);
            var volume = new FunctionalResource { Name = "volume", File = image, Values = new(), Units = new() };
            payload.Columns[5].Functional.Add(volume);
            return payload;
        }
    }
}
#endif
