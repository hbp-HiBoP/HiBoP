#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HBP.Core.Data;
using HBP.Data.Module3D;
using HBP.Quest.Desktop;
using HBP.Quest;
using HBP.Core.Tools;
using HBP.Transfer.Transport;
using HBP.UI.Tools;
using Cysharp.Threading.Tasks;
using System.Threading;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;

namespace HBP.Tests.Quest
{
    public sealed class SceneDisplayOwnershipTests
    {
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

        [TestCase(false), TestCase(true)]
        public async Task DropWindowBlockerPolicyPreservesDefaultAndCleansUpOnDestruction(bool noBlocker)
        {
            var canvas = new GameObject("Drop window canvas", typeof(RectTransform), typeof(Canvas));
            try
            {
                var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CustomUI/DropWindow.prefab");
                var instance = UnityEngine.Object.Instantiate(prefab, canvas.transform);
                var help = instance.GetComponent<DropWindow>();
                help.NoBlocker = noBlocker;
                help.Open();
                help.Open();
                var blockers = canvas.GetComponentsInChildren<BlockerButton>();
                Assert.That(blockers.Length, Is.EqualTo(noBlocker ? 0 : 1), "Opening twice must not create duplicate blockers.");
                if (noBlocker) help.ChangeWindowState();
                else blockers[0].GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                Assert.That(help.Window.activeSelf, Is.False);
                await UniTask.NextFrame();
                Assert.That(canvas.GetComponentsInChildren<BlockerButton>(), Is.Empty);
                help.Open();
                UnityEngine.Object.Destroy(instance);
                await UniTask.NextFrame();
                await UniTask.NextFrame();
                Assert.That(canvas.GetComponentsInChildren<BlockerButton>(), Is.Empty, "Closing an owner must not leave a blocker in the canvas.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(canvas);
            }
        }

        [Test]
        public void AutomaticModeAfterManualAddressAllowsLaterDiscoveryToSelectAQuest()
        {
            var root = new GameObject("Quest selection fixture");
            root.SetActive(false);
            var manager = root.AddComponent<QuestManager>();
            try
            {
                manager.SelectManual("192.168.1.25");
                manager.SelectAutomatic();
                Assert.That(manager.IsManualSelection, Is.False);
                Assert.That(manager.SelectedAddress, Is.Empty);
                Assert.That(manager.SelectedId, Is.Null);
                var device = new QuestDevice { Pin = new byte[32], Host = "192.168.1.30", Name = "Discovered Quest" };
                typeof(QuestManager).GetMethod("Observe", Private).Invoke(manager, new object[] { device });
                typeof(QuestManager).GetMethod("PublishPresence", Private).Invoke(manager, null);
                Assert.That(manager.SelectedId, Is.EqualTo(device.Id));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void QuestWindowSelectionTargetsListedScenesAndRemovesClosingScenes()
        {
            var root = new GameObject("Quest window selection fixture");
            root.SetActive(false);
            var module = root.AddComponent<Module3DMain>();
            var singleton = typeof(Singleton<Module3DMain>).GetField("m_Instance", BindingFlags.Static | BindingFlags.NonPublic);
            object previous = singleton.GetValue(null);
            singleton.SetValue(null, module);
            try
            {
                var a = root.AddComponent<Base3DScene>();
                var b = root.AddComponent<Base3DScene>();
                var first = new Visualization { Name = "First" };
                var second = new Visualization { Name = "Second" };
                typeof(Base3DScene).GetField("<Visualization>k__BackingField", Private).SetValue(a, first);
                typeof(Base3DScene).GetField("<Visualization>k__BackingField", Private).SetValue(b, second);
                typeof(Base3DScene).GetField("m_IsSelected", Private).SetValue(a, true);
                var open = (IList)typeof(Module3DMain).GetField("m_Scenes", Private).GetValue(module);
                open.Add(a);
                open.Add(b);
                var prefab = Resources.Load<GameObject>("Prefabs/UI/Windows/Quest window");
                var window = UnityEngine.Object.Instantiate(prefab, root.transform).GetComponent<HBP.UI.Quest.DesktopQuestWindow>();
                var type = window.GetType();
                type.GetMethod("AddScene", Private).Invoke(window, new object[] { a });
                type.GetMethod("AddScene", Private).Invoke(window, new object[] { b });
                var list = window.GetComponentInChildren<HBP.UI.Main.VisualizationList>(true);
                list.Select(second);
                var selected = (Base3DScene[])type.GetMethod("SelectedScenes", Private).Invoke(window, null);
                Assert.That(selected, Is.EqualTo(new[] { b }));
                Assert.That(Module3DMain.SelectedScene, Is.SameAs(a), "Choosing a transfer must not select another desktop scene.");
                list.Select(first);
                Assert.That(((Base3DScene[])type.GetMethod("SelectedScenes", Private).Invoke(window, null)).Length, Is.EqualTo(2));
                type.GetMethod("RemoveScene", Private).Invoke(window, new object[] { b });
                Assert.That(open.Contains(b), Is.True, "OnRemoveScene is emitted before the collection changes.");
                Assert.That(list.Objects, Is.EqualTo(new[] { first }));
                Assert.That((Base3DScene[])type.GetMethod("SelectedScenes", Private).Invoke(window, null), Is.EqualTo(new[] { a }));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                singleton.SetValue(null, previous);
            }
        }

        private static void SetPairedContext(QuestManager manager)
        {
            var type = typeof(QuestManager);
            type.GetField("connected", Private).SetValue(manager, true);
            type.GetField("credential", Private).SetValue(manager, new byte[32]);
            type.GetField("pin", Private).SetValue(manager, new byte[32]);
            type.GetField("autoSync", Private).SetValue(manager, true);
            var snapshotType = type.GetField("globals", Private).FieldType;
            var contextType = snapshotType.GetProperty("Context").PropertyType;
            object data = Activator.CreateInstance(contextType.GetProperty("Data").PropertyType);
            object context = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(contextType);
            contextType.GetField("<Data>k__BackingField", Private).SetValue(context, data);
            object snapshot = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(snapshotType);
            snapshotType.GetField("<Context>k__BackingField", Private).SetValue(snapshot, context);
            type.GetField("globals", Private).SetValue(manager, snapshot);
        }

        private static void ClearPairedContext(QuestManager manager) => typeof(QuestManager).GetField("globals", Private).SetValue(manager, null);

        [TestCase(false), TestCase(true)]
        public async Task CancellationDecisionBlocksUntilCloseOrExplicitTransferAfterDisconnect(bool disconnect)
        {
            var root = new GameObject("Cancellation decision fixture");
            root.SetActive(false);
            var manager = root.AddComponent<QuestManager>();
            SetPairedContext(manager);
            var module = root.AddComponent<Module3DMain>();
            var loading = root.AddComponent<LoadingManager>();
            var presenter = root.AddComponent<DisplayLoadingPresenterProbe>();
            typeof(LoadingManager).GetField("m_LoadingCircle", Private).SetValue(loading, presenter);
            var managerSingleton = typeof(Singleton<QuestManager>).GetField("m_Instance", BindingFlags.Static | BindingFlags.NonPublic);
            var loadingSingleton = typeof(Singleton<LoadingManager>).GetField("m_Instance", BindingFlags.Static | BindingFlags.NonPublic);
            var dialogSingleton = typeof(Singleton<DialogBoxManager>).GetField("m_Instance", BindingFlags.Static | BindingFlags.NonPublic);
            var moduleSingleton = typeof(Singleton<Module3DMain>).GetField("m_Instance", BindingFlags.Static | BindingFlags.NonPublic);
            object oldManager = managerSingleton.GetValue(null), oldLoading = loadingSingleton.GetValue(null), oldDialog = dialogSingleton.GetValue(null);
            object oldModule = moduleSingleton.GetValue(null);
            managerSingleton.SetValue(null, manager);
            loadingSingleton.SetValue(null, loading);
            dialogSingleton.SetValue(null, null);
            moduleSingleton.SetValue(null, module);
            var dialogs = UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Managers/Dialog Box Manager.prefab"));
            var sceneRoot = new GameObject("Cancelled Desktop visualization fixture");
            sceneRoot.SetActive(false);
            var scene = sceneRoot.AddComponent<Base3DScene>();
            var visualization = new Visualization { ID = "cancelled-display", Name = "Cancelled display" };
            typeof(Base3DScene).GetField("<Visualization>k__BackingField", Private).SetValue(scene, visualization);
            ((IList)typeof(Module3DMain).GetField("m_Scenes", Private).GetValue(module)).Add(scene);
            manager.RequireSceneSynchronizationRetry(scene, "Visualization transfer cancelled. Retry to continue.");
            var operationType = typeof(HBP.UI.Main.VisualizationGestion).Assembly.GetType("HBP.UI.Main.SceneDisplayOperation", true);
            object operation = Activator.CreateInstance(operationType, Private | BindingFlags.Public, null, new object[] { new[] { visualization }, null }, null);
            ((IList)operationType.GetField("loaded", Private).GetValue(operation)).Add(scene);
            using var reservation = manager.ReserveDisplay(new[] { visualization });
            operationType.GetField("reservation", Private).SetValue(operation, reservation);
            reservation.Dispose();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            int mainThread = Thread.CurrentThread.ManagedThreadId;
            UnityAction<Base3DScene> onRemove = removed => Assert.That(Thread.CurrentThread.ManagedThreadId, Is.EqualTo(mainThread), "Scene removal also invokes Unity toolbar consumers.");
            Module3DMain.OnRemoveScene.AddListener(onRemove);
            try
            {
                var decision = ((UniTask)operationType.GetMethod("ResolveCancellationAsync", Private).Invoke(operation, new object[] { timeout.Token })).AsTask();
                await UniTask.NextFrame();
                Assert.That(decision.IsCompleted, Is.False, "The unsynchronized visualization must not silently return to normal interaction.");
                var dialog = dialogs.GetComponentInChildren<DialogBox>();
                Assert.That(dialog, Is.Not.Null);
                var buttons = dialog.GetComponentsInChildren<UnityEngine.UI.Button>();
                Assert.That(buttons.Select(button => button.GetComponentInChildren<UnityEngine.UI.Text>().text), Is.EqualTo(new[] { "Transfer", "Close" }));
                if (disconnect) typeof(QuestManager).GetField("connected", Private).SetValue(manager, false);
                buttons[disconnect ? 0 : 1].onClick.Invoke();
                if (disconnect)
                {
                    await UniTask.NextFrame();
                    var notice = dialogs.GetComponentsInChildren<DialogBox>().Single();
                    notice.GetComponentInChildren<UnityEngine.UI.Button>().onClick.Invoke();
                }

                await decision;
                Assert.That(disconnect ? scene != null && !scene.IsClosing : scene == null || scene.IsClosing, Is.True);
                Assert.That(Module3DMain.Scenes.Count, Is.EqualTo(disconnect ? 1 : 0));
                Assert.That(presenter.Opens, Is.EqualTo(disconnect ? 0 : 1));
                Assert.That(presenter.Closes, Is.EqualTo(presenter.Opens));
            }
            finally
            {
                Module3DMain.OnRemoveScene.RemoveListener(onRemove);
                ClearPairedContext(manager);
                UnityEngine.Object.DestroyImmediate(dialogs);
                if (sceneRoot != null) UnityEngine.Object.DestroyImmediate(sceneRoot);
                UnityEngine.Object.DestroyImmediate(root);
                managerSingleton.SetValue(null, oldManager);
                loadingSingleton.SetValue(null, oldLoading);
                dialogSingleton.SetValue(null, oldDialog);
                moduleSingleton.SetValue(null, oldModule);
            }
        }

        [TestCase(false), TestCase(true)]
        public async Task InterruptedPublicationIsRetiredOnlyAfterExactQuestAcknowledgements(bool reject)
        {
            var root = new GameObject("Interrupted publication recovery fixture");
            root.SetActive(false);
            var manager = root.AddComponent<QuestManager>();
            SetPairedContext(manager);
            typeof(QuestManager).GetField("sessionGeneration", Private).SetValue(manager, 1L);
            var scene = root.AddComponent<Base3DScene>();
            typeof(Base3DScene).GetField("<Visualization>k__BackingField", Private).SetValue(scene, new Visualization { ID = "recovery-scene" });
            object entry = typeof(QuestManager).GetMethod("GetPublication", Private).Invoke(manager, new object[] { scene });
            var entryType = entry.GetType();
            string transfer = Guid.NewGuid().ToString("N"), uncertain = Guid.NewGuid().ToString("N");
            string file = Path.Combine(Application.temporaryCachePath, "recovery-" + transfer);
            File.WriteAllBytes(file, new byte[] { 1, 2, 3 });
            var offerType = entryType.GetField("Offer").FieldType;
            var constructor = offerType.GetConstructors().Single(value => value.GetParameters().Length == 5);
            using var offer = (IDisposable)constructor.Invoke(new[] { file, transfer, "session", "recovery fixture", Activator.CreateInstance(constructor.GetParameters()[4].ParameterType) });
            entryType.GetField("Offer").SetValue(entry, offer);
            entryType.GetField("OfferWasSent").SetValue(entry, true);
            var previous = (HashSet<string>)entryType.GetField("UncertainTransfers").GetValue(entry);
            previous.Add(uncertain);
            var closed = new List<string>();
            using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(25));
            using var receiver = new QuestPairing();
            var listener = new TcpListener(IPAddress.Loopback, QuestPairing.Port);
            listener.Start();
            var serving = receiver.ServeAsync(listener, stop.Token, (_, __) => throw new InvalidOperationException(), _ => { }, receiveSessionControl: (request, _) =>
            {
                Assert.That(request.Kind, Is.EqualTo(SessionControlKind.CloseScene));
                closed.Add(Encoding.UTF8.GetString(request.GetBody()));
                return Task.FromResult(new SessionControlResponse(request.OperationId, reject ? SessionControlStatus.Rejected : SessionControlStatus.Applied, "Recovery rejected by test receiver."));
            });
            byte[] credential = null;
            try
            {
                credential = await QuestPairing.PairAsync("127.0.0.1", receiver.Pin, receiver.Code, stop.Token);
                typeof(QuestManager).GetField("credential", Private).SetValue(manager, credential);
                typeof(QuestManager).GetField("pin", Private).SetValue(manager, receiver.Pin);
                typeof(QuestManager).GetField("endpoint", Private).SetValue(manager, "127.0.0.1");
                Exception failure = null;
                try
                {
                    await (Task)typeof(QuestManager).GetMethod("RetireInterruptedPublicationAsync", Private).Invoke(manager, new object[] { entry, stop.Token });
                }
                catch (Exception error)
                {
                    failure = error;
                }

                Assert.That(closed.All(value => value.Contains("recovery-scene")), Is.True);
                if (reject)
                {
                    Assert.That(failure, Is.InstanceOf<InvalidOperationException>());
                    Assert.That(entryType.GetField("Offer").GetValue(entry), Is.SameAs(offer));
                    Assert.That(entryType.GetField("OfferWasSent").GetValue(entry), Is.True);
                    Assert.That(previous, Does.Contain(uncertain));
                }
                else
                {
                    Assert.That(failure, Is.Null);
                    Assert.That(closed.Count, Is.EqualTo(2));
                    Assert.That(closed.Any(value => value.Contains(transfer)), Is.True);
                    Assert.That(closed.Any(value => value.Contains(uncertain)), Is.True);
                    Assert.That(entryType.GetField("Offer").GetValue(entry), Is.Null);
                    Assert.That(entryType.GetField("OfferWasSent").GetValue(entry), Is.False);
                    Assert.That(previous, Is.Empty);
                    Assert.That(File.Exists(file), Is.False);
                }
            }
            finally
            {
                stop.Cancel();
                await serving;
                ClearPairedContext(manager);
                UnityEngine.Object.DestroyImmediate(root);
                if (credential != null) Array.Clear(credential, 0, credential.Length);
            }
        }

        [Test]
        public void ToolbarWaitsForAnatomyAndRejectsClosingScenes()
        {
            var root = new GameObject("Toolbar native resource gate fixture");
            root.SetActive(false);
            var scene = root.AddComponent<Base3DScene>();
            var probe = root.AddComponent<DisplayNativeToolProbe>();
            probe.SelectedScene = scene;
            probe.SelectedColumn = root.AddComponent<Column3DAnatomy>();
            probe.SelectedView = root.AddComponent<View3D>();
            scene.SceneInformation.Initialized = true;
            var anatomy = new UniTaskCompletionSource();
            typeof(Base3DScene).GetField("m_AnatomyWork", Private).SetValue(scene, anatomy.Task);
            try
            {
                probe.UpdateTool();
                Assert.That(probe.Reads, Is.Zero);
                anatomy.TrySetResult();
                probe.UpdateTool();
                Assert.That(probe.Reads, Is.EqualTo(1));
                typeof(Base3DScene).GetField("m_DestroyRequested", Private).SetValue(scene, true);
                probe.UpdateTool();
                Assert.That(probe.Reads, Is.EqualTo(1), "A closing scene must no longer expose its native resources to toolbar readers.");
            }
            finally
            {
                anatomy.TrySetResult();
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [TestCase(false), TestCase(true)]
        public async Task DisplayAdmissionJoinsInventoryWithoutCancellingSharedWork(bool cancel)
        {
            var root = new GameObject("Queued Display fixture");
            root.SetActive(false);
            var manager = root.AddComponent<QuestManager>();
            SetPairedContext(manager);
            var shared = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            typeof(QuestManager).GetField("autoSync", Private).SetValue(manager, true);
            typeof(QuestManager).GetField("inventoryWork", Private).SetValue(manager, shared.Task);
            using var stop = new CancellationTokenSource();
            try
            {
                var waiting = manager.ReserveDisplayAsync(new[] { new Visualization() }, stop.Token).AsTask();
                await UniTask.NextFrame();
                Assert.That(waiting.IsCompleted, Is.False);
                if (cancel)
                {
                    stop.Cancel();
                    Exception failure = null;
                    try
                    {
                        await waiting;
                    }
                    catch (Exception error)
                    {
                        failure = error;
                    }

                    Assert.That(failure, Is.InstanceOf<OperationCanceledException>());
                    Assert.That(shared.Task.IsCompleted, Is.False, "Cancelling a queued Display must not cancel the shared inventory.");
                    shared.SetResult(true);
                }
                else
                {
                    shared.SetResult(true);
                    using var admission = await waiting;
                    Assert.That(admission, Is.Not.Null);
                }
            }
            finally
            {
                shared.TrySetResult(true);
                ClearPairedContext(manager);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [TestCase("busy"), TestCase("restoringScenes"), TestCase("reloadingScenes")]
        public async Task DisplayAdmissionWaitsForExistingQuestOperation(string field)
        {
            var root = new GameObject("Quest operation admission fixture");
            root.SetActive(false);
            var manager = root.AddComponent<QuestManager>();
            SetPairedContext(manager);
            var type = typeof(QuestManager);
            type.GetField("autoSync", Private).SetValue(manager, true);
            type.GetField(field, Private).SetValue(manager, true);
            try
            {
                var waiting = manager.ReserveDisplayAsync(new[] { new Visualization() }, CancellationToken.None).AsTask();
                await UniTask.NextFrame();
                Assert.That(waiting.IsCompleted, Is.False);
                type.GetField(field, Private).SetValue(manager, false);
                using var admission = await waiting;
                Assert.That(admission, Is.Not.Null);
            }
            finally
            {
                type.GetField(field, Private).SetValue(manager, false);
                ClearPairedContext(manager);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public async Task LocalDisplayDoesNotAdoptAQuestConnectingLater()
        {
            var root = new GameObject("Local Display admission fixture");
            root.SetActive(false);
            var manager = root.AddComponent<QuestManager>();
            typeof(QuestManager).GetField("autoSync", Private).SetValue(manager, true);
            typeof(QuestManager).GetField("busy", Private).SetValue(manager, true);
            try
            {
                Assert.That(await manager.ReserveDisplayAsync(new[] { new Visualization() }, CancellationToken.None), Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public async Task ExternalRecoveryWaitsWhileTheOwningRecoveryCanJoinItsOwnOperation()
        {
            var root = new GameObject("Recovery admission owner fixture");
            root.SetActive(false);
            var manager = root.AddComponent<QuestManager>();
            SetPairedContext(manager);
            var type = typeof(QuestManager);
            using var owner = new CancellationTokenSource();
            type.GetField("busy", Private).SetValue(manager, true);
            type.GetField("operation", Private).SetValue(manager, owner);
            try
            {
                var external = manager.ReserveDisplayAsync(new[] { new Visualization() }, CancellationToken.None, true).AsTask();
                await UniTask.NextFrame();
                Assert.That(external.IsCompleted, Is.False);
                using (var own = await manager.ReserveDisplayAsync(new[] { new Visualization() }, CancellationToken.None, true, owner.Token))
                    Assert.That(own, Is.Not.Null, "The operation must not wait for itself.");
                type.GetField("busy", Private).SetValue(manager, false);
                using var admitted = await external;
                Assert.That(admitted, Is.Not.Null);
            }
            finally
            {
                type.GetField("operation", Private).SetValue(manager, null);
                ClearPairedContext(manager);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public async Task PreparationBudgetFallbackKeepsTheSharedPublicationPendingUntilFinalDrain()
        {
            var publicationType = typeof(QuestManager).GetNestedType("Publication", BindingFlags.NonPublic);
            object entry = Activator.CreateInstance(publicationType, true);
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            publicationType.GetField("DisplayCompletion").SetValue(entry, completion);
            publicationType.GetField("Work").SetValue(entry, completion.Task);
            publicationType.GetField("PreparationStop").SetValue(entry, new CancellationTokenSource());
            var reservation = (QuestManager.DisplayReservation)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(QuestManager.DisplayReservation));
            var abandon = typeof(QuestManager.DisplayReservation).GetMethod("AbandonPreparationAsync", Private);
            await (Task)abandon.Invoke(reservation, new[] { entry, (object)false });
            Assert.That(completion.Task.IsCompleted, Is.False, "Serial fallback must preserve callers joined to the publication.");
            Assert.That(publicationType.GetProperty("HasPendingWork").GetValue(entry), Is.True);
            Assert.That(publicationType.GetField("PreparationStop").GetValue(entry), Is.Null);
            await (Task)abandon.Invoke(reservation, new[] { entry, (object)true });
            Assert.That(await completion.Task, Is.False);
            ((CancellationTokenSource)publicationType.GetField("Lifetime").GetValue(entry)).Dispose();
        }

        [TestCase(false), TestCase(true)]
        public async Task QuestBatchKeepsOnePresentationAndDrainsBeforeReleasingItsSceneGate(bool cancel)
        {
            var root = new GameObject("Quest Display presentation fixture");
            root.SetActive(false);
            var loading = root.AddComponent<LoadingManager>();
            var presenter = root.AddComponent<DisplayLoadingPresenterProbe>();
            typeof(LoadingManager).GetField("m_LoadingCircle", Private).SetValue(loading, presenter);
            var singleton = typeof(Singleton<LoadingManager>).GetField("m_Instance", BindingFlags.Static | BindingFlags.NonPublic);
            object previous = singleton.GetValue(null);
            singleton.SetValue(null, loading);
            var session = root.AddComponent<QuestAnatomySession>();
            var view = root.AddComponent<QuestAnatomyView>();
            var scene = root.AddComponent<Base3DScene>();
            typeof(Base3DScene).GetField("<Visualization>k__BackingField", Private).SetValue(scene, new Visualization { ID = "display-gate", Name = "Display gate" });
            var sessionType = typeof(QuestAnatomySession);
            var contextType = sessionType.GetField("globals", Private).FieldType;
            object context = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(contextType);
            object data = Activator.CreateInstance(contextType.GetProperty("Data").PropertyType);
            contextType.GetField("<Data>k__BackingField", Private).SetValue(context, data);
            string contextId = (string)data.GetType().GetField("Id").GetValue(data);
            sessionType.GetField("globals", Private).SetValue(session, context);
            sessionType.GetField("view", Private).SetValue(session, view);
            sessionType.GetField("mainThread", Private).SetValue(session, Thread.CurrentThread.ManagedThreadId);
            sessionType.GetField("unityContext", Private).SetValue(session, SynchronizationContext.Current);
            sessionType.GetField("controlGeneration", Private).SetValue(session, 7L);
            sessionType.GetField("<IsSessionConnected>k__BackingField", Private).SetValue(session, true);
            using var reception = new CancellationTokenSource();
            sessionType.GetField("reception", Private).SetValue(session, reception);
            string batch = Guid.NewGuid().ToString("N");
            SessionControlRequest Request(string id, bool finished = false, float progress = 0) => new(Guid.ParseExact(contextId, "N"), 7, Guid.NewGuid(), SessionControlKind.DisplayBatch, body: Encoding.UTF8.GetBytes("{\"Id\":\"" + id + "\",\"Visualizations\":[\"display-gate\"],\"ActiveVisualizationId\":\"display-gate\",\"IncompleteVisualizations\":" + (finished && cancel ? "[\"display-gate\"]" : "[]") + ",\"Finished\":" + (finished ? "true" : "false") + ",\"Progress\":" + progress.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}"));
            try
            {
                Assert.That((await session.ReceiveSessionControlAsync(Request(batch), CancellationToken.None)).Applied, Is.True);
                await UniTask.NextFrame();
                Assert.That(presenter.Opens, Is.EqualTo(1));
                Assert.That(QuestAnatomySession.CanInteractWith(scene), Is.False);
                await session.ReceiveSessionControlAsync(Request(batch, progress: .7f), CancellationToken.None);
                await session.ReceiveSessionControlAsync(Request(batch, progress: .2f), CancellationToken.None);
                Assert.That(presenter.Progress, Is.EqualTo(.7f));
                Assert.That((await session.ReceiveSessionControlAsync(Request(Guid.NewGuid().ToString("N")), CancellationToken.None)).Applied, Is.False);
                var gate = (SemaphoreSlim)sessionType.GetField("loadingGate", Private).GetValue(session);
                await gate.WaitAsync();
                var end = session.ReceiveSessionControlAsync(Request(batch, true, cancel ? .7f : 1), CancellationToken.None);
                Assert.That(end.IsCompleted, Is.False);
                Assert.That(reception.IsCancellationRequested, Is.EqualTo(cancel), "An unfinished owned receive must be cancelled before waiting for its loading gate.");
                Assert.That(presenter.Closes, Is.Zero);
                Assert.That(QuestAnatomySession.CanInteractWith(scene), Is.False);
                gate.Release();
                Assert.That((await end).Applied, Is.True);
                Assert.That(presenter.Closes, Is.EqualTo(1));
                Assert.That(QuestAnatomySession.CanInteractWith(scene), Is.True);
                Assert.That((await session.ReceiveSessionControlAsync(Request(batch), CancellationToken.None)).Status, Is.EqualTo(SessionControlStatus.Stale));
            }
            finally
            {
                sessionType.GetField("reception", Private).SetValue(session, null);
                sessionType.GetField("globals", Private).SetValue(session, null);
                UnityEngine.Object.DestroyImmediate(root);
                singleton.SetValue(null, previous);
            }
        }

        [Test]
        public async Task FailedPublicationSurvivesInventoryAndCannotRestartFromAutomaticSend()
        {
            var root = new GameObject("Display ownership fixture");
            root.SetActive(false);
            var manager = root.AddComponent<QuestManager>();
            var scene = root.AddComponent<Base3DScene>();
            typeof(Base3DScene).GetField("<Visualization>k__BackingField", Private).SetValue(scene, new Visualization { ID = "display-test", Name = "Display test" });
            try
            {
                object entry = typeof(QuestManager).GetMethod("GetPublication", Private).Invoke(manager, new object[] { scene });
                entry.GetType().GetField("UserRetryRequired").SetValue(entry, true);
                entry.GetType().GetField("Error").SetValue(entry, "Cancelled by the user");
                Assert.That(manager.CanRetryScenes, Is.True);
                var snapshot = (Array)typeof(QuestManager).GetMethod("SnapshotInventoryPublications", Private).Invoke(manager, null);
                Assert.That(snapshot.Length, Is.Zero, "An inventory must not admit a failed Display publication.");
                Assert.That(await manager.SendAsync(false, scene), Is.False, "An automatic send must preserve the manual recovery gate.");
                Assert.That(entry.GetType().GetField("Work").GetValue(entry), Is.Null);
                Assert.That(manager.IsSceneSynchronized(scene), Is.False);
                var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                entry.GetType().GetField("Work").SetValue(entry, completion.Task);
                Task<bool> joined = manager.SendAsync(false, scene);
                Assert.That(joined, Is.SameAs(completion.Task));
                Assert.That(joined.IsCompleted, Is.False, "Preparing a queued snapshot is not a completed publication.");
                completion.SetResult(false);
                Assert.That(await joined, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void TargetGenerationAndPreferenceInvalidateReservedDisplay()
        {
            var root = new GameObject("Display target fixture");
            root.SetActive(false);
            var manager = root.AddComponent<QuestManager>();
            var type = typeof(QuestManager);
            try
            {
                type.GetField("connected", Private).SetValue(manager, true);
                type.GetField("credential", Private).SetValue(manager, new byte[32]);
                type.GetField("pin", Private).SetValue(manager, new byte[32]);
                type.GetField("autoSync", Private).SetValue(manager, true);
                type.GetField("sessionGeneration", Private).SetValue(manager, 7L);
                // Reservation identity checks can be exercised without native scene preparation.
                var reservationType = type.GetNestedType("DisplayReservation");
                Assert.That(reservationType, Is.Not.Null);
                Assert.That(type.GetMethod("ReserveDisplay").Invoke(manager, new object[] { new[] { new Visualization() }, false }), Is.Null, "Missing global context must remain local-only.");
                var snapshotType = type.GetField("globals", Private).FieldType;
                var contextType = snapshotType.GetProperty("Context").PropertyType;
                var dataType = contextType.GetProperty("Data").PropertyType;
                object data = Activator.CreateInstance(dataType);
                object context = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(contextType);
                contextType.GetField("<Data>k__BackingField", Private).SetValue(context, data);
                object snapshot = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(snapshotType);
                snapshotType.GetField("<Context>k__BackingField", Private).SetValue(snapshot, context);
                type.GetField("globals", Private).SetValue(manager, snapshot);
                using var reservation = manager.ReserveDisplay(new[] { new Visualization() });
                Assert.That(reservation.IsCurrent, Is.True);
                type.GetField("sessionGeneration", Private).SetValue(manager, 8L);
                Assert.That(reservation.IsCurrent, Is.False);
                type.GetField("sessionGeneration", Private).SetValue(manager, 7L);
                type.GetField("autoSync", Private).SetValue(manager, false);
                Assert.That(reservation.IsCurrent, Is.False);
                reservation.Dispose();
                using var manualRetry = manager.ReserveDisplay(new[] { new Visualization() }, explicitRecovery: true);
                Assert.That(manualRetry, Is.Not.Null, "Manual recovery remains available with automatic sharing disabled.");
                Assert.That(manualRetry.IsCurrent, Is.True);
                type.GetField("autoSync", Private).SetValue(manager, true);
                Assert.That(manualRetry.IsCurrent, Is.False, "A preference change still invalidates the pinned operation.");
            }
            finally
            {
                type.GetField("globals", Private).SetValue(manager, null);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void BatchPercentageCannotRegressOrReachSuccessBeforeCompletion()
        {
            var operationType = typeof(HBP.UI.Main.VisualizationGestion).Assembly.GetType("HBP.UI.Main.SceneDisplayOperation", true);
            object operation = Activator.CreateInstance(operationType, Private | BindingFlags.Public, null, new object[] { new[] { new Visualization(), new Visualization() }, null }, null);
            float last = -1;
            operationType.GetField("update", Private).SetValue(operation, new Action<float, float, HBP.Core.Tools.LoadingText>((value, duration, text) =>
            {
                Assert.That(value, Is.GreaterThanOrEqualTo(last));
                last = value;
            }));
            var report = operationType.GetMethod("Report", Private);
            foreach (float value in new[] { .7f, .2f, .9f, 1f })
            {
                operationType.GetField("localProgress", Private).SetValue(operation, value);
                report.Invoke(operation, new object[] { new HBP.Core.Tools.LoadingText("Preparing"), false });
                Assert.That(last, Is.LessThan(1));
            }

            report.Invoke(operation, new object[] { new HBP.Core.Tools.LoadingText("Ready"), true });
            Assert.That(last, Is.EqualTo(1));
        }
    }

    public sealed class DisplayNativeToolProbe : HBP.UI.Toolbar.Tool
    {
        public int Reads;

        public override void Initialize()
        {
        }

        public override void DefaultState()
        {
        }

        public override void UpdateInteractable()
        {
        }

        public override void UpdateStatus() => Reads++;
    }

    public sealed class DisplayLoadingPresenterProbe : MonoBehaviour, ILoadingPresenter
    {
        public int Opens, Closes;
        public float Progress;
        public UnityEvent OnCancel { get; } = new();

        public void Initialize()
        {
        }

        public void Open(bool showInformations = true, bool cancelable = false) => Opens++;

        public void SetOptions(bool showInformations, bool cancelable)
        {
        }

        public void Close() => Closes++;
        public void ChangePercentage(float progress, float durationInSeconds, LoadingText message) => Progress = progress;
    }
}
#endif
