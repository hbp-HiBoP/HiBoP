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

        [TestCase("network"), TestCase("data"), TestCase("cancel"), TestCase("disconnect")]
        public async Task DisplayDefersOnlyNetworkFailuresAndExplicitCancellationWins(string kind)
        {
            var root = new GameObject("Display transport recovery fixture");
            root.SetActive(false);
            var manager = root.AddComponent<QuestManager>();
            SetPairedContext(manager);
            var type = typeof(QuestManager);
            type.GetField("reconnect", Private).SetValue(manager, true);
            type.GetField("automaticallyConnect", Private).SetValue(manager, false);
            var credential = type.GetField("credential", Private).GetValue(manager);
            var scene = root.AddComponent<Base3DScene>();
            var visualization = new Visualization { ID = "interrupted-display" };
            typeof(Base3DScene).GetField("<Visualization>k__BackingField", Private).SetValue(scene, visualization);
            using var user = new CancellationTokenSource();
            using var reservation = manager.ReserveDisplay(new[] { visualization });
            typeof(QuestManager.DisplayReservation).GetProperty("UserCancellation", Private).SetValue(reservation, user.Token);
            try
            {
                if (kind == "cancel") user.Cancel();
                if (kind == "disconnect") manager.Disconnect();
                Exception error = kind == "data" ? new InvalidDataException("Invalid scene payload") : new IOException("USB removed");
                bool deferred = reservation.RecoverConnection(error) && reservation.DeferScene(scene);
                Assert.That(deferred, Is.EqualTo(kind == "network"));
                Assert.That(manager.IsSceneRecoveryPending(scene), Is.EqualTo(kind == "network"));
                Assert.That(type.GetField("credential", Private).GetValue(manager), Is.SameAs(credential));
                if (kind == "network")
                {
                    Assert.That(manager.IsPaired, Is.False);
                    Assert.That(manager.CanRetryScenes, Is.False, "Transient loss must not impose a manual Retry gate.");
                    user.Cancel();
                    Assert.That(reservation.DeferScene(scene), Is.False);
                    reservation.RequireRetry(scene, "Display cancelled");
                    Assert.That(manager.IsSceneRecoveryPending(scene), Is.False);
                    Assert.That(manager.CanRetryScenes, Is.True);
                }
            }
            finally
            {
                type.GetMethod("DisposeDiscovery", Private).Invoke(manager, null);
                ((CancellationTokenSource)type.GetField("lifetime", Private).GetValue(manager)).Cancel();
                await (Task)type.GetField("listening", Private).GetValue(manager);
                ClearPairedContext(manager);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [TestCase("timeout"), TestCase("socket"), TestCase("cancel"), TestCase("data")]
        public async Task SleepingDuringSceneReconnectionKeepsAutomaticRetryWhileDataFailuresRequireAttention(string kind)
        {
            var root = new GameObject("Scene reconnect interruption fixture");
            root.SetActive(false);
            var manager = root.AddComponent<QuestManager>();
            var type = typeof(QuestManager);
            var publication = type.GetNestedType("Publication", BindingFlags.NonPublic);
            var entry = Activator.CreateInstance(publication, true);
            publication.GetField("VisualizationId").SetValue(entry, "atlas-scene");
            publication.GetField("Error").SetValue(entry, "old error");
            publication.GetField("UserRetryRequired").SetValue(entry, true);
            type.GetField("connected", Private).SetValue(manager, true);
            type.GetField("credential", Private).SetValue(manager, new byte[32]);
            type.GetField("reconnect", Private).SetValue(manager, true);
            Exception error = kind switch
            {
                "timeout" => new TimeoutException("Quest asleep"),
                "socket" => new IOException("Quest disconnected"),
                "cancel" => new OperationCanceledException("Old session retired"),
                _ => new InvalidDataException("Incompatible checkpoint")
            };
            try
            {
                type.GetMethod("RecordSceneReconnectionFailure", Private).Invoke(manager, new[] { entry, error });
                bool manual = kind == "data";
                Assert.That((bool)publication.GetField("UserRetryRequired").GetValue(entry), Is.EqualTo(manual));
                Assert.That(manager.CanRetryReconciliation, Is.EqualTo(manual));
                Assert.That(manager.IsPaired, Is.EqualTo(manual));
                Assert.That((bool)type.GetField("reconnect", Private).GetValue(manager), Is.True, "A transient interruption must retain automatic wake recovery.");
                if (!manual) Assert.That(publication.GetField("Error").GetValue(entry), Is.Null);
            }
            finally
            {
                type.GetMethod("DisposeDiscovery", Private).Invoke(manager, null);
                ((CancellationTokenSource)type.GetField("lifetime", Private).GetValue(manager)).Cancel();
                await (Task)type.GetField("listening", Private).GetValue(manager);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public async Task LocalOpeningFailureShowsVisualizationAndRecoveryInsteadOfTechnicalOrQuestError()
        {
            var root = new GameObject("Display error fixture");
            root.SetActive(false);
            var loading = root.AddComponent<LoadingManager>();
            var presenter = root.AddComponent<DisplayLoadingPresenterProbe>();
            typeof(LoadingManager).GetField("m_LoadingCircle", Private).SetValue(loading, presenter);
            var questSingleton = typeof(Singleton<QuestManager>).GetField("m_Instance", BindingFlags.Static | BindingFlags.NonPublic);
            var loadingSingleton = typeof(Singleton<LoadingManager>).GetField("m_Instance", BindingFlags.Static | BindingFlags.NonPublic);
            var dialogSingleton = typeof(Singleton<DialogBoxManager>).GetField("m_Instance", BindingFlags.Static | BindingFlags.NonPublic);
            object oldQuest = questSingleton.GetValue(null), oldLoading = loadingSingleton.GetValue(null), oldDialog = dialogSingleton.GetValue(null);
            var oldProject = ApplicationState.LoadedProject;
            questSingleton.SetValue(null, null);
            loadingSingleton.SetValue(null, loading);
            dialogSingleton.SetValue(null, null);
            ApplicationState.LoadedProject = null;
            var dialogs = UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Managers/Dialog Box Manager.prefab"));
            // A missing dataset triggers an unexpected local failure before any scene opens.
            var visualization = new Visualization { Name = "Patient activity", Columns = new() { new IEEGColumn() } };
            var operationType = typeof(HBP.UI.Main.VisualizationGestion).Assembly.GetType("HBP.UI.Main.SceneDisplayOperation", true);
            var operation = Activator.CreateInstance(operationType, Private | BindingFlags.Public, null, new object[] { new[] { visualization }, null }, null);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            Task display = null;
            try
            {
                UnityEngine.TestTools.LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("NullReferenceException"));
                display = ((UniTask)operationType.GetMethod("RunAsync", Private | BindingFlags.Public).Invoke(operation, new object[] { timeout.Token, true })).AsTask();
                await UniTask.WaitUntil(() => dialogs.GetComponentInChildren<DialogBox>() != null, cancellationToken: timeout.Token);
                var dialog = dialogs.GetComponentInChildren<DialogBox>();
                var titleText = typeof(DialogBox).GetField("m_Title", Private).GetValue(dialog);
                var messageText = typeof(DialogBox).GetField("m_Message", Private).GetValue(dialog);
                string title = (string)titleText.GetType().GetProperty("text").GetValue(titleText);
                string message = (string)messageText.GetType().GetProperty("text").GetValue(messageText);
                Assert.That(title, Is.EqualTo("Visualization could not be opened"));
                Assert.That(message, Does.Contain(visualization.Name).And.Contain("internal application error").And.Contain("Try opening").And.Contain("application log"));
                Assert.That(message, Does.Not.Contain("NullReferenceException").And.Not.Contain("Quest").And.Not.Contain("remain available"));
                Assert.That(presenter.Closes, Is.EqualTo(presenter.Opens));
                dialog.GetComponentInChildren<UnityEngine.UI.Button>().onClick.Invoke();
                await display;
            }
            finally
            {
                foreach (var dialog in dialogs.GetComponentsInChildren<DialogBox>())
                    dialog.GetComponentInChildren<UnityEngine.UI.Button>().onClick.Invoke();
                if (display != null) await display;
                UnityEngine.Object.DestroyImmediate(dialogs);
                UnityEngine.Object.DestroyImmediate(root);
                questSingleton.SetValue(null, oldQuest);
                loadingSingleton.SetValue(null, oldLoading);
                dialogSingleton.SetValue(null, oldDialog);
                ApplicationState.LoadedProject = oldProject;
            }
        }

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
                var device = new QuestDevice { Pin = new byte[32], Endpoint = QuestEndpoint.Parse("192.168.1.30"), Name = "Discovered Quest" };
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

        [TestCase(false, false), TestCase(true, false), TestCase(false, true), TestCase(true, true)]
        public async Task InterruptedPublicationIsRetiredOnlyAfterExactQuestAcknowledgements(bool reject, bool published)
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
            if (!published) previous.Add(uncertain);
            object binding = null;
            if (published)
            {
                var bindingType = entryType.GetField("Binding").FieldType;
                binding = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(bindingType);
                bindingType.GetField("<TransferId>k__BackingField", Private).SetValue(binding, transfer);
                entryType.GetField("Binding").SetValue(entry, binding);
            }

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
                    await (Task)typeof(QuestManager).GetMethod(published ? "RetireAbsentPublicationAsync" : "RetireInterruptedPublicationAsync", Private).Invoke(manager, new object[] { entry, stop.Token });
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
                    if (!published) Assert.That(previous, Does.Contain(uncertain));
                    else Assert.That(entryType.GetField("Binding").GetValue(entry), Is.SameAs(binding));
                }
                else
                {
                    Assert.That(failure, Is.Null);
                    Assert.That(closed.Count, Is.EqualTo(published ? 1 : 2));
                    Assert.That(closed.Any(value => value.Contains(transfer)), Is.True);
                    if (!published) Assert.That(closed.Any(value => value.Contains(uncertain)), Is.True);
                    else
                    {
                        Assert.That(entryType.GetField("Binding").GetValue(entry), Is.Null);
                        Assert.That(manager.IsSceneRecoveryPending(scene), Is.True, "A previously acknowledged scene missing after Quest restart must be republished automatically.");
                    }

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

        [TestCase(false, false), TestCase(true, false), TestCase(false, true), TestCase(true, true)]
        public async Task QuestBatchKeepsOnePresentationAndDrainsBeforeReleasingItsSceneGate(bool cancel, bool patientRequest)
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
            SessionControlRequest Request(string id, bool finished = false, float progress = 0, string transferMessage = null, float transferProgress = 0, bool transferring = false) => new(Guid.ParseExact(contextId, "N"), 7, Guid.NewGuid(), SessionControlKind.DisplayBatch, body: Encoding.UTF8.GetBytes("{\"Id\":\"" + id + "\",\"Visualizations\":[\"display-gate\"],\"SourceStatus\":{\"Prefix\":\"Loading data\",\"Suffix\":\" 3/119\",\"VisualizationId\":\"display-gate\"},\"ActiveVisualizationId\":\"display-gate\",\"IncompleteVisualizations\":" + (finished && cancel ? "[\"display-gate\"]" : "[]") + ",\"Finished\":" + (finished ? "true" : "false") + ",\"Progress\":" + progress.ToString(System.Globalization.CultureInfo.InvariantCulture) + (transferMessage == null ? "" : ",\"TransferStatus\":{\"Prefix\":\"" + transferMessage + "\",\"VisualizationId\":\"display-gate\",\"Transfer\":" + (transferring ? "true" : "false") + ",\"Progress\":" + transferProgress.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}") + "}"));
            try
            {
                if (patientRequest)
                {
                    sessionType.GetMethod("BeginPatientRequest", Private).Invoke(session, new object[] { Guid.ParseExact(batch, "N") });
                    Assert.That(presenter.Opens, Is.EqualTo(1), "Click feedback opens before any Desktop response or transfer.");
                    var unrelated = new HBP.Sync.SceneAction(new HBP.Sync.SceneId(Guid.NewGuid()), new HBP.Sync.IncarnationId(Guid.NewGuid()), HBP.Sync.SceneActionKind.Result);
                    sessionType.GetMethod("ReceivePatientResult", Private).Invoke(session, new object[] { unrelated });
                    Assert.That((Guid)sessionType.GetField("patientRequestOperation", Private).GetValue(session), Is.EqualTo(Guid.ParseExact(batch, "N")));
                }

                Assert.That((await session.ReceiveSessionControlAsync(Request(batch), CancellationToken.None)).Applied, Is.True);
                await UniTask.NextFrame();
                Assert.That(presenter.Opens, Is.EqualTo(1));
                Assert.That(presenter.Closes, Is.Zero, "Adopting click feedback must keep the same circle open.");
                Assert.That(presenter.Text.ToString(), Is.EqualTo("Loading data 3/119"));
                Assert.That(QuestAnatomySession.CanInteractWith(scene), Is.False);
                await session.ReceiveSessionControlAsync(Request(batch, progress: .7f), CancellationToken.None);
                await session.ReceiveSessionControlAsync(Request(batch, progress: .2f), CancellationToken.None);
                Assert.That(presenter.Progress, Is.EqualTo(.7f));
                await session.ReceiveSessionControlAsync(Request(batch, progress: .72f, transferMessage: "Sending scene", transferProgress: .99f, transferring: true), CancellationToken.None);
                Assert.That(presenter.Progress, Is.EqualTo(.72f), "A single transfer's percentage must not replace the batch's overall progress.");
                await session.ReceiveSessionControlAsync(Request(batch, progress: .73f, transferMessage: "Reading scene metadata", transferProgress: .85f), CancellationToken.None);
                Assert.That(presenter.Progress, Is.EqualTo(.73f), "Starting preparation after a transfer must not move the circle backwards.");
                await session.ReceiveSessionControlAsync(Request(batch, progress: .74f, transferMessage: "Sending another scene", transferProgress: .01f, transferring: true), CancellationToken.None);
                Assert.That(presenter.Progress, Is.EqualTo(.74f), "The next scene's transfer starts at zero without resetting the overall circle.");
                var statusType = sessionType.GetField("displayReceivingStatus", Private).FieldType;
                var receiving = Activator.CreateInstance(statusType);
                statusType.GetField("Prefix").SetValue(receiving, "Loading column ");
                statusType.GetField("Message").SetValue(receiving, "Memory");
                statusType.GetField("Suffix").SetValue(receiving, " [2/5]");
                statusType.GetField("VisualizationId").SetValue(receiving, "display-gate");
                sessionType.GetField("loadingProgress", Private).SetValue(session, new Action<float, LoadingText>((_, _) => { }));
                sessionType.GetField("displayReceivingStatus", Private).SetValue(session, receiving);
                await session.ReceiveSessionControlAsync(Request(batch, progress: .7f), CancellationToken.None);
                Assert.That(presenter.Text.ToString(), Is.EqualTo("Loading column Memory [2/5]"), "Desktop status must not overwrite the current Quest loading stage.");
                sessionType.GetField("loadingProgress", Private).SetValue(session, null);
                statusType.GetField("Prefix").SetValue(receiving, "Synchronizing scene");
                statusType.GetField("Message").SetValue(receiving, "");
                statusType.GetField("Suffix").SetValue(receiving, "");
                statusType.GetField("Progress").SetValue(receiving, 1f);
                await session.ReceiveSessionControlAsync(Request(batch, transferMessage: "Waiting for scene preparation on Quest", transferProgress: .85f), CancellationToken.None);
                Assert.That(presenter.Text.ToString(), Is.EqualTo("Synchronizing scene"), "Finishing reception must not restore an older Desktop preparation message.");
                await session.ReceiveSessionControlAsync(Request(batch, transferMessage: "Scene ready", transferProgress: 1f), CancellationToken.None);
                Assert.That(presenter.Text.ToString(), Is.EqualTo("Scene ready"), "The next explicit Desktop phase must take over after reception.");
                Assert.That((await session.ReceiveSessionControlAsync(Request(Guid.NewGuid().ToString("N")), CancellationToken.None)).Applied, Is.False);
                var gate = (SemaphoreSlim)sessionType.GetField("loadingGate", Private).GetValue(session);
                await gate.WaitAsync();
                if (patientRequest && cancel)
                {
                    sessionType.GetMethod("EndPatientRequest", Private).Invoke(session, new object[] { "The source visualization was closed." });
                    Assert.That((IEnumerable<string>)sessionType.GetField("finishedDisplayBatches", Private).GetValue(session), Does.Not.Contain(batch), "Closing the source must still allow the active batch to drain.");
                }

                var end = session.ReceiveSessionControlAsync(Request(batch, true, cancel ? .7f : 1), CancellationToken.None);
                Assert.That(end.IsCompleted, Is.False);
                Assert.That(reception.IsCancellationRequested, Is.EqualTo(cancel), "An unfinished owned receive must be cancelled before waiting for its loading gate.");
                Assert.That(presenter.Closes, Is.Zero);
                Assert.That(QuestAnatomySession.CanInteractWith(scene), Is.False);
                gate.Release();
                Assert.That((await end).Applied, Is.True);
                if (patientRequest)
                {
                    await (Task)sessionType.GetField("patientRequestPresentation", Private).GetValue(session);
                    Assert.That((Guid)sessionType.GetField("patientRequestOperation", Private).GetValue(session), Is.EqualTo(Guid.Empty));
                    var result = new HBP.Sync.SceneAction(new HBP.Sync.SceneId(Guid.NewGuid()), new HBP.Sync.IncarnationId(Guid.NewGuid()), HBP.Sync.SceneActionKind.Result, operation: Guid.ParseExact(batch, "N"), succeeded: !cancel, message: cancel ? "Patient opening cancelled by Desktop" : "Patient visualization ready on both devices.");
                    sessionType.GetMethod("ReceivePatientResult", Private).Invoke(session, new object[] { result });
                    Assert.That(session.ActionStatus, Is.EqualTo(result.Message), "The detailed result may arrive after the batch has drained.");
                }

                Assert.That(presenter.Closes, Is.EqualTo(1));
                Assert.That(QuestAnatomySession.CanInteractWith(scene), Is.True);
                Assert.That((await session.ReceiveSessionControlAsync(Request(batch), CancellationToken.None)).Status, Is.EqualTo(SessionControlStatus.Stale));
            }
            finally
            {
                sessionType.GetMethod("EndPatientRequest", Private).Invoke(session, new object[] { null });
                await (Task)sessionType.GetField("patientRequestPresentation", Private).GetValue(session);
                sessionType.GetField("reception", Private).SetValue(session, null);
                sessionType.GetField("globals", Private).SetValue(session, null);
                UnityEngine.Object.DestroyImmediate(root);
                singleton.SetValue(null, previous);
            }
        }

        [TestCase("failure"), TestCase("send"), TestCase("timeout"), TestCase("disconnect"), TestCase("source")]
        public async Task PatientClickFeedbackClosesWhenDesktopCannotStartTheBatch(string reason)
        {
            var root = new GameObject("Patient click feedback fixture");
            root.SetActive(false);
            var loading = root.AddComponent<LoadingManager>();
            var presenter = root.AddComponent<DisplayLoadingPresenterProbe>();
            typeof(LoadingManager).GetField("m_LoadingCircle", Private).SetValue(loading, presenter);
            var singleton = typeof(Singleton<LoadingManager>).GetField("m_Instance", BindingFlags.Static | BindingFlags.NonPublic);
            object previous = singleton.GetValue(null);
            singleton.SetValue(null, loading);
            var session = root.AddComponent<QuestAnatomySession>();
            var type = typeof(QuestAnatomySession);
            var operation = Guid.NewGuid();
            type.GetField("mainThread", Private).SetValue(session, Thread.CurrentThread.ManagedThreadId);
            type.GetField("<IsSessionConnected>k__BackingField", Private).SetValue(session, true);
            try
            {
                type.GetMethod("BeginPatientRequest", Private).Invoke(session, new object[] { operation });
                Assert.That(presenter.Opens, Is.EqualTo(1));
                if (reason == "failure")
                {
                    var result = new HBP.Sync.SceneAction(new HBP.Sync.SceneId(Guid.NewGuid()), new HBP.Sync.IncarnationId(Guid.NewGuid()), HBP.Sync.SceneActionKind.Result, operation: operation, succeeded: false, message: "Patient data unavailable");
                    type.GetMethod("ReceivePatientResult", Private).Invoke(session, new object[] { result });
                    Assert.That(session.ActionStatus, Is.EqualTo(result.Message));
                }
                else if (reason == "send") type.GetMethod("EndPatientRequest", Private).Invoke(session, new object[] { "Scene action queue is full." });
                else if (reason == "disconnect") session.Disconnect();
                else
                {
                    if (reason == "source")
                    {
                        var entryType = type.GetNestedType("Entry", BindingFlags.NonPublic);
                        object entry = Activator.CreateInstance(entryType, true);
                        ((CancellationTokenSource)entryType.GetField("Lifetime").GetValue(entry)).Cancel();
                        type.GetField("patientRequestSource", Private).SetValue(session, entry);
                    }
                    else type.GetField("patientRequestExpires", Private).SetValue(session, Time.realtimeSinceStartup - 1);

                    type.GetMethod("TickDisplayBatch", Private).Invoke(session, null);
                }

                await (Task)type.GetField("patientRequestPresentation", Private).GetValue(session);
                Assert.That(presenter.Closes, Is.EqualTo(1));
                Assert.That((Guid)type.GetField("patientRequestOperation", Private).GetValue(session), Is.EqualTo(Guid.Empty));
                Assert.That(session.ActionStatus, Is.Not.Empty);
                Assert.That(((IEnumerable<string>)type.GetField("finishedDisplayBatches", Private).GetValue(session)), Does.Contain(operation.ToString("N")), "A delayed batch must not reopen abandoned click feedback.");
            }
            finally
            {
                type.GetMethod("EndPatientRequest", Private).Invoke(session, new object[] { null });
                await (Task)type.GetField("patientRequestPresentation", Private).GetValue(session);
                UnityEngine.Object.DestroyImmediate(root);
                singleton.SetValue(null, previous);
            }
        }

        [Test]
        public async Task ExplicitPatientDisplayKeepsItsRequestIdentityWhenAutomaticSharingIsDisabled()
        {
            var root = new GameObject("Explicit patient Display admission fixture");
            root.SetActive(false);
            var manager = root.AddComponent<QuestManager>();
            SetPairedContext(manager);
            typeof(QuestManager).GetField("autoSync", Private).SetValue(manager, false);
            try
            {
                var operation = Guid.NewGuid();
                using var reservation = await manager.ReserveDisplayAsync(new[] { new Visualization() }, CancellationToken.None, explicitRecovery: true, displayOperation: operation);
                Assert.That(reservation, Is.Not.Null);
                Assert.That(typeof(QuestManager.DisplayReservation).GetField("batch", Private).GetValue(reservation), Is.EqualTo(operation.ToString("N")));
                Assert.That(reservation.IsCurrent, Is.True);
            }
            finally
            {
                ClearPairedContext(manager);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public async Task ExplicitPatientDisplayAnnouncesTheBatchBeforeLocalLoadingAndReportsFailure()
        {
            var root = new GameObject("Patient Display protocol fixture");
            root.SetActive(false);
            var loading = root.AddComponent<LoadingManager>();
            var presenter = root.AddComponent<DisplayLoadingPresenterProbe>();
            typeof(LoadingManager).GetField("m_LoadingCircle", Private).SetValue(loading, presenter);
            var manager = root.AddComponent<QuestManager>();
            SetPairedContext(manager);
            typeof(QuestManager).GetField("autoSync", Private).SetValue(manager, false);
            var questSingleton = typeof(Singleton<QuestManager>).GetField("m_Instance", BindingFlags.Static | BindingFlags.NonPublic);
            var loadingSingleton = typeof(Singleton<LoadingManager>).GetField("m_Instance", BindingFlags.Static | BindingFlags.NonPublic);
            object oldQuest = questSingleton.GetValue(null), oldLoading = loadingSingleton.GetValue(null);
            var oldProject = ApplicationState.LoadedProject;
            questSingleton.SetValue(null, manager);
            loadingSingleton.SetValue(null, loading);
            ApplicationState.LoadedProject = null;
            var batches = new System.Collections.Concurrent.ConcurrentQueue<string>();
            typeof(QuestManager).GetField("sessionGeneration", Private).SetValue(manager, 1L);
            using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(25));
            using var receiver = new QuestPairing();
            var listener = new TcpListener(IPAddress.Loopback, QuestPairing.Port);
            listener.Start();
            var serving = receiver.ServeAsync(listener, stop.Token, (_, __) => throw new InvalidOperationException("A failed local opening must not transfer scene data."), _ => { }, receiveSessionControl: (request, _) =>
            {
                batches.Enqueue(Encoding.UTF8.GetString(request.GetBody()));
                return Task.FromResult(new SessionControlResponse(request.OperationId, SessionControlStatus.Applied, body: request.GetBody()));
            });
            try
            {
                byte[] credential = await QuestPairing.PairAsync("127.0.0.1", receiver.Pin, receiver.Code, stop.Token);
                typeof(QuestManager).GetField("credential", Private).SetValue(manager, credential);
                typeof(QuestManager).GetField("pin", Private).SetValue(manager, receiver.Pin);
                typeof(QuestManager).GetField("endpoint", Private).SetValue(manager, "127.0.0.1");
                var visualization = new Visualization { Name = "Patient activity", Columns = new() { new IEEGColumn() } };
                var operationType = typeof(HBP.UI.Main.VisualizationGestion).Assembly.GetType("HBP.UI.Main.SceneDisplayOperation", true);
                object operation = Activator.CreateInstance(operationType, Private | BindingFlags.Public, null, new object[] { new[] { visualization }, null }, null);
                Guid operationId = Guid.NewGuid();
                operationType.GetProperty("ExplicitSharing", Private).SetValue(operation, true);
                operationType.GetProperty("OperationId", Private).SetValue(operation, operationId);
                bool announcedBeforeLoadFailed = false;

                void ObserveFailure(string condition, string trace, LogType kind)
                {
                    if (kind == LogType.Exception && condition.Contains("NullReferenceException")) announcedBeforeLoadFailed = !batches.IsEmpty;
                }

                Application.logMessageReceived += ObserveFailure;
                try
                {
                    UnityEngine.TestTools.LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("NullReferenceException"));
                    await (UniTask)operationType.GetMethod("RunAsync", Private).Invoke(operation, new object[] { stop.Token, false });
                }
                finally
                {
                    Application.logMessageReceived -= ObserveFailure;
                }

                Assert.That(announcedBeforeLoadFailed, Is.True, "The Quest must receive the Display batch before Desktop loading starts.");
                Assert.That((bool)operationType.GetProperty("Succeeded", Private).GetValue(operation), Is.False);
                Assert.That((string)operationType.GetProperty("Error", Private).GetValue(operation), Is.Not.Empty);
                var states = batches.Select(json => JsonUtility.FromJson<PatientDisplayBatchProbe>(json)).ToArray();
                Assert.That(states.All(state => state.Id == operationId.ToString("N")), Is.True);
                Assert.That(states.First().Finished, Is.False);
                Assert.That(states.Last().Finished, Is.True);
                Assert.That(states.Last().Progress, Is.LessThan(1), "An opening failure must not announce success.");
                Assert.That(presenter.Opens, Is.EqualTo(1));
                Assert.That(presenter.Closes, Is.EqualTo(1));
            }
            finally
            {
                stop.Cancel();
                await serving;
                ClearPairedContext(manager);
                UnityEngine.Object.DestroyImmediate(root);
                questSingleton.SetValue(null, oldQuest);
                loadingSingleton.SetValue(null, oldLoading);
                ApplicationState.LoadedProject = oldProject;
            }
        }

        [Serializable]
        private sealed class PatientDisplayBatchProbe
        {
            public string Id = "";
            public bool Finished = false;
            public float Progress = 0;
        }

        [TestCase(false), TestCase(true)]
        public void QueuedReceptionMessagesKeepTheirOperationAndDeliveryKind(bool globals)
        {
            var root = new GameObject("Queued loading messages fixture");
            root.SetActive(false);
            var session = root.AddComponent<QuestAnatomySession>();
            var type = typeof(QuestAnatomySession);
            var context = new QueuedLoadingContext();
            var texts = new List<string>();
            using var oldReception = new CancellationTokenSource();
            using var currentReception = new CancellationTokenSource();
            type.GetField("mainThread", Private).SetValue(session, Thread.CurrentThread.ManagedThreadId);
            type.GetField("unityContext", Private).SetValue(session, context);
            type.GetField("receivingGlobals", Private).SetValue(session, globals);
            type.GetField("loadingProgress", Private).SetValue(session, new Action<float, LoadingText>((_, text) => texts.Add(text.ToString())));
            try
            {
                type.GetField("reception", Private).SetValue(session, oldReception);
                var staleDecode = (Action<float, float, LoadingText>)type.GetMethod("SceneDecodeProgress", Private).Invoke(session, new object[] { oldReception.Token });
                type.GetMethod("ReportTransferProgress", Private).Invoke(session, new object[] { 10L, 100L });
                staleDecode(.86f, 0, new LoadingText("Restoring scene data"));
                oldReception.Cancel();
                type.GetField("reception", Private).SetValue(session, currentReception);
                type.GetMethod("ReportTransferProgress", Private).Invoke(session, new object[] { 50L, 100L });
                context.Drain();
                Assert.That(texts, Is.EqualTo(new[] { globals ? "Receiving pairing data - 50%" : "Receiving scene - 50%" }));
                Assert.That(session.ReceivedBytes, Is.EqualTo(50));
            }
            finally
            {
                type.GetField("reception", Private).SetValue(session, null);
                type.GetField("loadingProgress", Private).SetValue(session, null);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private sealed class QueuedLoadingContext : SynchronizationContext
        {
            private readonly Queue<(SendOrPostCallback Callback, object State)> posts = new();
            public override void Post(SendOrPostCallback callback, object state) => posts.Enqueue((callback, state));

            public void Drain()
            {
                while (posts.Count != 0)
                {
                    var next = posts.Dequeue();
                    next.Callback(next.State);
                }
            }
        }

        [TestCase(false), TestCase(true)]
        public async Task DelayedQuestStatusCannotReplaceANewerTransferPhaseOrScene(bool nextScene)
        {
            var root = new GameObject("Delayed remote status fixture");
            root.SetActive(false);
            var manager = root.AddComponent<QuestManager>();
            SetPairedContext(manager);
            var managerType = typeof(QuestManager);
            managerType.GetField("sessionGeneration", Private).SetValue(manager, 1L);
            using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            var received = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var cancelReceived = stop.Token.Register(() => received.TrySetException(new TimeoutException("The delayed progress request did not arrive.")));
            using var receiver = new QuestPairing();
            var listener = new TcpListener(IPAddress.Loopback, QuestPairing.Port);
            listener.Start();
            var serving = receiver.ServeAsync(listener, stop.Token, (_, __) => throw new InvalidOperationException(), _ => { }, receiveSessionControl: async (request, _) =>
            {
                received.TrySetResult(true);
                await release.Task;
                string body = Encoding.UTF8.GetString(request.GetBody()).Replace("\"ReceivingStatus\":null", "\"ReceivingStatus\":{\"VisualizationId\":\"first\",\"Prefix\":\"Checking received scene\",\"Progress\":0.9}");
                return new SessionControlResponse(request.OperationId, SessionControlStatus.Applied, body: Encoding.UTF8.GetBytes(body));
            });
            byte[] credential = null;
            QuestManager.DisplayReservation reservation = null;
            try
            {
                credential = await QuestPairing.PairAsync("127.0.0.1", receiver.Pin, receiver.Code, stop.Token);
                managerType.GetField("credential", Private).SetValue(manager, credential);
                managerType.GetField("pin", Private).SetValue(manager, receiver.Pin);
                managerType.GetField("endpoint", Private).SetValue(manager, "127.0.0.1");
                reservation = manager.ReserveDisplay(new[] { new Visualization { ID = "first" }, new Visualization { ID = "second" } });
                var type = reservation.GetType();
                var statusProperty = type.GetProperty("TransferStatus");

                object Status(string scene, string text, float progress)
                {
                    object status = Activator.CreateInstance(statusProperty.PropertyType);
                    statusProperty.PropertyType.GetField("VisualizationId").SetValue(status, scene);
                    statusProperty.PropertyType.GetField("Prefix").SetValue(status, text);
                    statusProperty.PropertyType.GetField("Progress").SetValue(status, progress);
                    return status;
                }

                type.GetField("activeVisualization", Private).SetValue(reservation, "first");
                statusProperty.SetValue(reservation, Status("first", "Waiting for scene preparation on Quest", .85f));
                var update = reservation.UpdateRemoteAsync(.5f, false, stop.Token);
                if (await Task.WhenAny(update, received.Task) == update) await update;
                await received.Task;
                string currentText = nextScene ? "Preparing scene for transfer" : "Synchronizing scene";
                type.GetField("activeVisualization", Private).SetValue(reservation, nextScene ? "second" : "first");
                statusProperty.SetValue(reservation, Status(nextScene ? "second" : "first", currentText, .97f));
                release.TrySetResult(true);
                await update;
                Assert.That(type.GetProperty("ReceivingStatus").GetValue(reservation), Is.Null, "A response to the previous phase must not restore its receiving message.");
                Assert.That(reservation.RemoteText.Prefix, Is.EqualTo(currentText));
            }
            finally
            {
                release.TrySetResult(true);
                reservation?.Dispose();
                stop.Cancel();
                await serving;
                ClearPairedContext(manager);
                UnityEngine.Object.DestroyImmediate(root);
                if (credential != null) Array.Clear(credential, 0, credential.Length);
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

        [Test]
        public void LegacyLocalProgressCannotExceedItsShareOfTheWholeBatch()
        {
            var visualizations = new[] { new Visualization(), new Visualization() };
            var operationType = typeof(HBP.UI.Main.VisualizationGestion).Assembly.GetType("HBP.UI.Main.SceneDisplayOperation", true);
            object operation = Activator.CreateInstance(operationType, Private | BindingFlags.Public, null, new object[] { visualizations, null }, null);
            var reservation = (QuestManager.DisplayReservation)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(QuestManager.DisplayReservation));
            var reservationType = reservation.GetType();
            reservationType.GetField("visualizations", Private).SetValue(reservation, visualizations);
            var sourceProperty = reservationType.GetProperty("SourceStatus");
            sourceProperty.SetValue(reservation, Activator.CreateInstance(sourceProperty.PropertyType));
            operationType.GetField("reservation", Private).SetValue(operation, reservation);
            operationType.GetField("localProgress", Private).SetValue(operation, 2f);
            var remote = (float[])operationType.GetField("remoteProgress", Private).GetValue(operation);
            remote[0] = remote[1] = .5f;
            float shown = 0;
            operationType.GetField("update", Private).SetValue(operation, new Action<float, float, LoadingText>((value, _, _) => shown = value));
            operationType.GetMethod("Report", Private).Invoke(operation, new object[] { new LoadingText("Preparing"), false });
            Assert.That(shown, Is.EqualTo(.575f).Within(.0001f), "Local loading owns 15% of the batch even when a legacy callback reports 200%.");
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
        public LoadingText Text;
        public UnityEvent OnCancel { get; } = new();

        public void Initialize()
        {
        }

        public void Open(bool showInformations = true, bool cancelable = false) => Opens++;

        public void SetOptions(bool showInformations, bool cancelable)
        {
        }

        public void Close() => Closes++;

        public void ChangePercentage(float progress, float durationInSeconds, LoadingText message)
        {
            Progress = progress;
            Text = message;
        }
    }
}
#endif
