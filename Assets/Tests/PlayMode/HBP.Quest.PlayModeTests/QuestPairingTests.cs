using AnatomyReceptionState = HBP.Quest.Legacy.AnatomyReceptionState;
using QuestAnatomyView = HBP.Quest.Legacy.QuestAnatomyView;
using QuestAnatomySession = HBP.Quest.Legacy.QuestAnatomySession;
using AnatomyMeshUploader = HBP.Quest.Legacy.AnatomyMeshUploader;
using QuestAnatomyDiagnostic = HBP.Quest.Legacy.QuestAnatomyDiagnostic;
#if UNITY_EDITOR
using System;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.IO;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using HBP.Quest;
using HBP.Quest.Legacy;
using HBP.Transfer.Anatomy;
using HBP.Transfer.Anatomy.Delivery;
using HBP.Transfer.Transport;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using HBP.UI.Quest;
using HBP.UI.Tools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HBP.Tests.Quest
{
    public class QuestPairingTests
    {
        private QuestPairing pairing;
        private CancellationTokenSource stop;
        private Task serving;
        private GameObject root;
        private QuestAnatomySession session;
        private QuestAnatomyView view;

        [SetUp]
        public void SetUp()
        {
            root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Tests/Support/QuestPrototype/QuestAnatomy.prefab"));
            session = root.GetComponent<QuestAnatomySession>();
            view = root.GetComponent<QuestAnatomyView>();
            pairing = new QuestPairing();
            stop = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var listener = new TcpListener(IPAddress.Loopback, QuestPairing.Port);
            listener.Start();
            serving = pairing.ServeAsync(listener, stop.Token, session.ReceiveStreamAsync, _ => { });
        }

        [TearDown]
        public async Task TearDown()
        {
            stop.Cancel();
            await serving;
            pairing.Dispose();
            stop.Dispose();
            Object.DestroyImmediate(root);
        }

        [Test]
        public void QuestPrefab_UsesNetworkPanelAndDisablesFixtureInjection()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/QuestBootstrap.prefab");
            var panel = prefab.GetComponentInChildren<QuestConnectionPanel>(true);
            Assert.That(panel, Is.Not.Null);
            var serialized = new SerializedObject(panel);
            Assert.That(serialized.FindProperty("session").objectReferenceValue, Is.Not.Null);
            Assert.That(serialized.FindProperty("statusPanel").objectReferenceValue, Is.Not.Null);
            Assert.That(serialized.FindProperty("session").objectReferenceValue, Is.SameAs(prefab.GetComponentInChildren<HBP.Quest.QuestAnatomySession>(true)));
            Assert.That(prefab.GetComponentInChildren<QuestAnatomyDiagnostic>(true), Is.Null);
            var status = prefab.GetComponentInChildren<QuestStatusPanel>(true);
            var statusFields = new SerializedObject(status);
            foreach (string name in new[] { "countdown", "lifetimeGauge", "retry", "newAssociation", "retryLabel", "newAssociationLabel" })
                Assert.That(statusFields.FindProperty(name).objectReferenceValue, Is.Not.Null, name);
            var input = prefab.GetComponentInChildren<QuestPairingUIInput>(true);
            Assert.That(input, Is.Not.Null);
            var inputFields = new SerializedObject(input);
            foreach (string name in new[] { "module", "trackingOrigin", "pairingCard", "pointer", "rightController" })
                Assert.That(inputFields.FindProperty(name).objectReferenceValue, Is.Not.Null, name);
            Assert.That(input.GetComponent<LineRenderer>().sharedMaterial, Is.Not.Null);
            Assert.That(status.GetComponent<Canvas>().worldCamera, Is.Not.Null);
        }

        [Test]
        public void PairingControllerActionsInitializeAndReenableFromAuthoredPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/QuestBootstrap.prefab");
            var ui = Object.Instantiate(prefab.GetComponentInChildren<QuestPairingUIInput>(true).gameObject);
            try
            {
                var module = ui.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                Assert.That(module.trackedDevicePosition.action.actionMap.asset, Is.Not.Null);
                Assert.That(module.trackedDeviceOrientation.action.actionMap.asset, Is.SameAs(module.trackedDevicePosition.action.actionMap.asset));
                Assert.That(module.leftClick.action.actionMap.asset, Is.SameAs(module.trackedDevicePosition.action.actionMap.asset));
                ui.SetActive(false);
                Assert.That(module.enabled, Is.False);
                ui.SetActive(true);
                Assert.That(module.trackedDevicePosition.action.actionMap.asset, Is.Not.Null);
                Assert.That(module.leftClick.action.enabled, Is.True);
            }
            finally { Object.DestroyImmediate(ui); }
        }

        [Test]
        public void PairingCardShowsCountdownRetryAndConfirmedReplacement()
        {
            var ui = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/Quest Status Panel.prefab"));
            try
            {
                var card = ui.GetComponent<QuestStatusPanel>();
                var fields = new SerializedObject(card);
                var countdown = (Text)fields.FindProperty("countdown").objectReferenceValue;
                var gauge = (Image)fields.FindProperty("lifetimeGauge").objectReferenceValue;
                var retry = (Button)fields.FindProperty("retry").objectReferenceValue;
                var replace = (Button)fields.FindProperty("newAssociation").objectReferenceValue;
                int retries = 0, replacements = 0;
                card.RetryRequested += () => retries++;
                card.NewAssociationRequested += () => replacements++;
                card.ShowPairing(new PairingStatus("123456", TimeSpan.FromSeconds(90), 4, PairingPhase.Available), "");
                Assert.That(countdown.text, Does.Contain("01:30"));
                Assert.That(((Text)fields.FindProperty("code").objectReferenceValue).text, Is.EqualTo("123 456"));
                Assert.That(gauge.fillAmount, Is.EqualTo(0.3f).Within(0.001));
                Assert.That(retry.gameObject.activeSelf, Is.False);
                Assert.That(replace.gameObject.activeSelf, Is.True);
                card.ShowPairing(new PairingStatus("123456", TimeSpan.FromSeconds(90), 4, PairingPhase.Authenticating), "");
                Assert.That(replace.gameObject.activeSelf, Is.False);
                card.ShowPairing(new PairingStatus("123456", TimeSpan.Zero, 0, PairingPhase.AttemptsExhausted), "");
                retry.onClick.Invoke();
                Assert.That(retries, Is.EqualTo(1));
                card.ShowWaiting();
                replace.onClick.Invoke();
                card.ShowWaiting();
                Assert.That(replacements, Is.Zero);
                retry.onClick.Invoke(); // Cancel confirmation.
                card.ShowWaiting();
                replace.onClick.Invoke();
                card.ShowWaiting();
                replace.onClick.Invoke();
                Assert.That(replacements, Is.EqualTo(1));
                card.Hide();
                Assert.That(ui.activeSelf, Is.False);
            }
            finally { Object.DestroyImmediate(ui); }
        }

        [Test]
        public void PairingCardContentFitsBackgroundAndGaugeGeometryShrinks()
        {
            var ui = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/Quest Status Panel.prefab"));
            try
            {
                var card = ui.GetComponent<QuestStatusPanel>();
                card.ShowPairing(new PairingStatus("123456", TimeSpan.FromSeconds(150), 5, PairingPhase.Available), "192.168.1.18");
                Canvas.ForceUpdateCanvases();
                var background = (RectTransform)ui.transform.Find("Background");
                foreach (var rect in ui.GetComponentsInChildren<RectTransform>(true))
                {
                    if (rect == ui.transform || rect == background) continue;
                    var corners = new Vector3[4];
                    rect.GetWorldCorners(corners);
                    foreach (var corner in corners)
                    {
                        Vector3 local = background.InverseTransformPoint(corner);
                        Assert.That(local.x, Is.InRange(background.rect.xMin, background.rect.xMax), rect.name);
                        Assert.That(local.y, Is.InRange(background.rect.yMin, background.rect.yMax), rect.name);
                    }
                }
                var gauge = (Image)new SerializedObject(card).FindProperty("lifetimeGauge").objectReferenceValue;
                var populate = typeof(Image).GetMethod("OnPopulateMesh", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, null, new[] { typeof(VertexHelper) }, null);
                float fullWidth = GaugeMeshWidth(gauge, populate, 1);
                Assert.That(fullWidth, Is.GreaterThan(0));
                Assert.That(GaugeMeshWidth(gauge, populate, 0.5f), Is.EqualTo(fullWidth / 2).Within(0.01f));
                Assert.That(gauge.color, Is.EqualTo(new Color(59f / 255, 122f / 255, 194f / 255, 1)));
            }
            finally { Object.DestroyImmediate(ui); }
        }

        private static float GaugeMeshWidth(Image gauge, System.Reflection.MethodInfo populate, float amount)
        {
            gauge.fillAmount = amount;
            using var vertices = new VertexHelper();
            populate.Invoke(gauge, new object[] { vertices });
            float min = float.PositiveInfinity, max = float.NegativeInfinity;
            for (int index = 0; index < vertices.currentVertCount; index++)
            {
                var vertex = new UIVertex();
                vertices.PopulateUIVertex(ref vertex, index);
                min = Mathf.Min(min, vertex.position.x);
                max = Mathf.Max(max, vertex.position.x);
            }
            return max - min;
        }

        [Test]
        public async Task InputDialog_ReturnsEnteredValueAndChosenAction()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/DialogBox/Input Dialog Box.prefab");
            var ui = Object.Instantiate(prefab);
            try
            {
                var dialog = ui.GetComponent<InputDialogBox>();
                var fields = new SerializedObject(dialog);
                var input = (InputField)fields.FindProperty("input").objectReferenceValue;
                var confirm = (Button)fields.FindProperty("confirm").objectReferenceValue;
                var pending = dialog.OpenAsync("Title", "Message", "Placeholder", "Accept", "Cancel");
                input.text = "custom value";
                confirm.onClick.Invoke();
                var result = await pending;
                Assert.That(result.Confirmed, Is.True);
                Assert.That(result.Value, Is.EqualTo("custom value"));
            }
            finally
            {
                if (ui != null) Object.DestroyImmediate(ui);
            }
        }

        [Test]
        public async Task PairPushRetryAndClose_UseRealTlsAndPublicationOwner()
        {
            byte[] pin = await QuestPairing.InspectAsync("127.0.0.1", stop.Token);
            Assert.That(QuestPairing.FormatPin(pin), Is.EqualTo(pairing.Fingerprint));
            Assert.That(pairing.IsPaired, Is.False);
            byte[] credential = await QuestPairing.PairAsync("127.0.0.1", pin, pairing.Code, stop.Token);
            var offer = new AnatomyDelivery(Snapshot());
            int progress = 0;
            DeliveryReceipt first = await QuestPairing.SendAsync("127.0.0.1", pin, credential, stop.Token, (stream, token) => offer.SendAsync(stream, token, bytes => progress = bytes));
            Assert.That(first.Status, Is.EqualTo(DeliveryStatus.Published));
            Assert.That(first.ContentHash, Is.EqualTo(offer.ContentHash));
            Assert.That(progress, Is.EqualTo(offer.EncodedBytes));
            Mesh mesh = view.SharedMesh;
            root.transform.position = Vector3.one;
            DeliveryReceipt retry = await QuestPairing.SendAsync("127.0.0.1", pin, credential, stop.Token, (stream, token) => offer.SendAsync(stream, token));
            Assert.That(retry.Status, Is.EqualTo(DeliveryStatus.AlreadyPublished));
            Assert.That(view.SharedMesh, Is.SameAs(mesh));
            Assert.That(root.transform.position, Is.EqualTo(Vector3.one));
            Assert.That(view.UploadCount, Is.EqualTo(1));
            session.CloseSession();
            DeliveryReceipt closed = await QuestPairing.SendAsync("127.0.0.1", pin, credential, stop.Token, (stream, token) => offer.SendAsync(stream, token));
            Assert.That(closed.Status, Is.EqualTo(DeliveryStatus.Closed));
            Assert.That(session.IsReady, Is.False);
        }

        [Test]
        public async Task WrongPin_DoesNotSpendCodeAttempt_AndSecondPairIsRefused()
        {
            byte[] pin = await QuestPairing.InspectAsync("127.0.0.1", stop.Token);
            byte[] badPin = (byte[])pin.Clone();
            badPin[0] ^= 1;
            Exception error = await Capture(() => QuestPairing.PairAsync("127.0.0.1", badPin, pairing.Code, stop.Token));
            Assert.That(error, Is.Not.Null);
            Assert.That(pairing.IsPaired, Is.False);
            await QuestPairing.PairAsync("127.0.0.1", pin, pairing.Code, stop.Token);
            error = await Capture(() => QuestPairing.PairAsync("127.0.0.1", pin, pairing.Code, stop.Token));
            Assert.That(error, Is.TypeOf<AuthenticationException>());
        }

        [Test]
        public async Task UnknownCommandAndCorruptPayload_DoNotStopListening()
        {
            using (var peer = new TcpClient())
            {
                await peer.ConnectAsync(IPAddress.Loopback, QuestPairing.Port);
                using var tls = new SslStream(peer.GetStream(), false, (_, __, ___, ____) => true);
                await tls.AuthenticateAsClientAsync("test", null, SslProtocols.Tls12, false);
                await tls.WriteAsync(new byte[] { 255 }, 0, 1);
            }

            byte[] pin = await QuestPairing.InspectAsync("127.0.0.1", stop.Token);
            byte[] credential = await QuestPairing.PairAsync("127.0.0.1", pin, pairing.Code, stop.Token);
            byte[] bytes = AnatomySnapshotCodec.Encode(Snapshot());
            Assert.That(await Capture(() => QuestPairing.SendAsync("127.0.0.1", pin, credential, stop.Token, (stream, token) => PinnedTlsTransfer.SendPayloadAsync(stream, bytes, token, corruptChunk: true))), Is.Not.Null);
            Assert.That(session.IsReady, Is.False);
            var offer = new AnatomyDelivery(Snapshot());
            var receipt = await QuestPairing.SendAsync("127.0.0.1", pin, credential, stop.Token, (s, t) => offer.SendAsync(s, t));
            Assert.That(receipt.Status, Is.EqualTo(DeliveryStatus.Published));
            Assert.That(session.IsReady, Is.True);
        }

        [Test]
        public async Task FifthCodeAttempt_CanStillPair()
        {
            byte[] pin = await QuestPairing.InspectAsync("127.0.0.1", stop.Token);
            string wrong = pairing.Code == "000000" ? "000001" : "000000";
            for (int i = 0; i < 4; i++) await Capture(() => QuestPairing.PairAsync("127.0.0.1", pin, wrong, stop.Token));
            await QuestPairing.PairAsync("127.0.0.1", pin, pairing.Code, stop.Token);
            Assert.That(pairing.IsPaired, Is.True);
        }

        [Test]
        public async Task FiveBadCodes_LockAcrossConnections()
        {
            byte[] pin = await QuestPairing.InspectAsync("127.0.0.1", stop.Token);
            string wrong = pairing.Code == "000000" ? "000001" : "000000";
            for (int i = 0; i < 5; i++) Assert.That(await Capture(() => QuestPairing.PairAsync("127.0.0.1", pin, wrong, stop.Token)), Is.TypeOf<AuthenticationException>());
            Assert.That(pairing.IsLocked, Is.True);
            Assert.That(await Capture(() => QuestPairing.PairAsync("127.0.0.1", pin, pairing.Code, stop.Token)), Is.TypeOf<AuthenticationException>());
            Assert.That(pairing.IsPaired, Is.False);
        }

        [Test]
        public async Task InvalidCredential_PreservesContent_ThenCorrectRetryWorks()
        {
            byte[] pin = await QuestPairing.InspectAsync("127.0.0.1", stop.Token);
            byte[] credential = await QuestPairing.PairAsync("127.0.0.1", pin, pairing.Code, stop.Token);
            var offer = new AnatomyDelivery(Snapshot());
            await QuestPairing.SendAsync("127.0.0.1", pin, credential, stop.Token, (s, t) => offer.SendAsync(s, t));
            Mesh mesh = view.SharedMesh;
            Assert.That(await Capture(() => QuestPairing.SendAsync("127.0.0.1", pin, new byte[32], stop.Token, (s, t) => offer.SendAsync(s, t))), Is.TypeOf<AuthenticationException>());
            Assert.That(view.SharedMesh, Is.SameAs(mesh));
            var receipt = await QuestPairing.SendAsync("127.0.0.1", pin, credential, stop.Token, (s, t) => offer.SendAsync(s, t));
            Assert.That(receipt.Status, Is.EqualTo(DeliveryStatus.AlreadyPublished));
        }

        [Test]
        public async Task CloseWhileIncomingStreamWaits_CancelsPublicationWithoutBlockingFrames()
        {
            byte[] pin = await QuestPairing.InspectAsync("127.0.0.1", stop.Token);
            byte[] credential = await QuestPairing.PairAsync("127.0.0.1", pin, pairing.Code, stop.Token);
            var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var offer = new AnatomyDelivery(Snapshot());
            Task sending = QuestPairing.SendAsync("127.0.0.1", pin, credential, stop.Token, async (stream, token) =>
            {
                await release.Task;
                return await offer.SendAsync(stream, token);
            });
            while (session.ReceptionState == AnatomyReceptionState.Idle)
            {
                stop.Token.ThrowIfCancellationRequested();
                await Task.Yield();
            }

            session.CloseSession();
            release.SetResult(true);
            Assert.That(await Capture(() => sending), Is.Not.Null);
            Assert.That(session.IsReady, Is.False);
            Assert.That(view.UploadCount, Is.Zero);
        }

        [TestCase("bad-address")]
        [TestCase("192.168.1.18:5555")]
        public async Task InvalidAddress_FailsBeforeNetwork(string host)
        {
            Assert.That(await Capture(() => QuestPairing.InspectAsync(host, stop.Token)), Is.TypeOf<ArgumentException>());
        }

        private static AnatomySnapshot Snapshot() => AnatomySnapshot.Create("pairing-test", "session", "viz", "column", 1, new AnatomyCoordinateSpace(AnatomyMeshUploader.FrameId, AnatomyHandedness.Left, AnatomyLengthUnit.Millimeter, 1, new float[] { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 }), AnatomyWinding.Clockwise, true, new[] { 0.2f, 0.4f, 0.7f, 1f }, new float[] { 0, 0, 0, 100, 0, 0, 0, 100, 0 }, new float[] { 0, 0, -1, 0, 0, -1, 0, 0, -1 }, new uint[] { 0, 1, 2 }, Array.Empty<float>());

        private static async Task<Exception> Capture(Func<Task> action)
        {
            try
            {
                await action();
                return null;
            }
            catch (Exception exception)
            {
                return exception;
            }
        }
    }
}
#endif
