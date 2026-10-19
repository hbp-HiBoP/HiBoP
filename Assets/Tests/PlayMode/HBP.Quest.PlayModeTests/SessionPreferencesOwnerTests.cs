using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using DataManager = HBP.Core.Data.DataManager;
using Site = HBP.Core.Data.Site;
using HBP.Core.Enums;
using HBP.Core.DLL;
using HBP.Core.Object3D;
using HBP.Core.Preferences;
using HBP.Transfer.Transport;
using HBP.UI.Quest;
using HBP.Sync.Scene;
using NUnit.Framework;

namespace HBP.Tests.Quest
{
    public sealed class SessionPreferencesOwnerTests
    {
        [TestCase(false), TestCase(true)]
        public async Task SceneSessionReopenPreservesTheNativeAtlasLoadAndHandlesAnotherSleep(bool sleepsAgain)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var entered = new TaskCompletionSource<SessionControlRequest>();
            var finish = new TaskCompletionSource<SessionControlResponse>();
            using var watchdog = timeout.Token.Register(() =>
            {
                entered.TrySetCanceled();
                finish.TrySetCanceled();
            });
            DesktopSessionPreferences owner = null;
            Task reopening = null;
            int opens = 0, loads = 0, confirmations = 0, cancelledLoads = 0;
            try
            {
                Assert.That((await AtlasResources.LoadAsync("mars", timeout.Token)).Succeeded, Is.True);
                owner = new DesktopSessionPreferences(Guid.NewGuid(), 1, new UserPreferences(), async (request, token) =>
                {
                    if (request.Kind == SessionControlKind.Open) opens++;
                    if (request.Kind == SessionControlKind.ConfirmAtlas) confirmations++;
                    if (request.Kind == SessionControlKind.LoadAtlas)
                    {
                        loads++;
                        entered.TrySetResult(request);
                        using var cancellation = token.Register(() =>
                        {
                            cancelledLoads++;
                            finish.TrySetCanceled();
                        });
                        return await finish.Task;
                    }

                    return AtlasReply(request, inventoryState: AtlasLoadState.Unloaded);
                });
                await owner.StartAsync(timeout.Token);
                var load = await entered.Task;
                reopening = owner.ReopenAsync(timeout.Token).AsTask();
                await UniTask.NextFrame(cancellationToken: timeout.Token);
                Assert.That(reopening.IsCompleted, Is.False, "Reconnection must join a long native atlas load instead of disposing its owner.");
                Assert.That(cancelledLoads, Is.Zero);
                if (sleepsAgain) owner.Dispose();
                else finish.SetResult(AtlasReply(load));
                Exception failure = null;
                try
                {
                    await reopening;
                }
                catch (Exception error)
                {
                    failure = error;
                }

                await AtlasInventoryWork(owner);
                if (sleepsAgain) Assert.That(failure, Is.TypeOf<IOException>(), "Another sleep must remain an automatic connection retry.");
                else
                {
                    Assert.That(failure, Is.Null);
                    Assert.That(SessionAtlasCatalog.Active && SessionAtlasCatalog.CanUse("mars"), Is.True, "Reopening the same generation must preserve confirmed atlases.");
                }

                Assert.That(loads, Is.EqualTo(1));
                Assert.That(opens, Is.EqualTo(sleepsAgain ? 1 : 2));
                Assert.That(confirmations, Is.EqualTo(sleepsAgain ? 0 : 1));
                Assert.That(AtlasResources.IsLoaded("mars"), Is.True);
            }
            finally
            {
                owner?.Dispose();
                finish.TrySetCanceled();
                if (owner != null) await AtlasInventoryWork(owner);
                if (reopening != null)
                    try
                    {
                        await reopening;
                    }
                    catch (Exception)
                    {
                    }

                AtlasResources.Unload("mars");
            }
        }

        [TestCase("ibc", false, false), TestCase("difumo:64", false, false), TestCase("ibc", true, false), TestCase("ibc", true, true)]
        public async Task DesktopAtlasCompletesBeforeQuestAndLateLoadCannotReverseUnload(string id, bool unloadBeforeWake, bool sceneOwnsLoad)
        {
            var previousIBC = Object3DManager.IBC;
            var previousDiFuMo = Object3DManager.DiFuMo;
            var previousMars = Object3DManager.MarsAtlas;
            string previousPath = HBP.Core.Tools.ApplicationState.DataPath;
            var path = typeof(HBP.Core.Tools.ApplicationState).GetProperty("DataPath");
            var root = new UnityEngine.GameObject("Desktop first atlas session");
            root.SetActive(false);
            var manager = root.AddComponent<HBP.Quest.Desktop.QuestManager>();
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var intents = (Dictionary<string, bool>)manager.GetType().GetField("atlasIntents", flags).GetValue(manager);
            var entered = new TaskCompletionSource<SessionControlRequest>();
            var wake = new TaskCompletionSource<SessionControlResponse>();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            using var watchdog = timeout.Token.Register(() =>
            {
                entered.TrySetCanceled();
                wake.TrySetCanceled();
            });
            DesktopSessionPreferences owner = null;
            Task sceneReadiness = null;
            int loads = 0, confirmations = 0, unloads = 0, errors = 0;
            var remoteState = AtlasLoadState.Unloaded;
            try
            {
                path.SetValue(null, Path.GetFullPath("Assets/Data"));
                Object3DManager.IBC = new IBCObjects();
                Object3DManager.DiFuMo = new DiFuMoObjects();
                Object3DManager.MarsAtlas = new MarsAtlas();

                async Task<SessionControlResponse> Send(SessionControlRequest request, CancellationToken token)
                {
                    if (request.Kind == SessionControlKind.AtlasInventory)
                        return new(request.OperationId, SessionControlStatus.Applied, body: Encoding.UTF8.GetBytes("{\"Preparing\":false,\"Atlases\":{\"" + id + "\":{\"State\":" + (int)remoteState + "}}}"));
                    if (request.Kind == SessionControlKind.LoadAtlas)
                    {
                        loads++;
                        entered.TrySetResult(request);
                        // Deliberately deliver a stale reply even if Desktop has cancelled the old load.
                        var reply = await wake.Task;
                        remoteState = AtlasLoadState.Loaded;
                        return reply;
                    }

                    if (request.Kind == SessionControlKind.ConfirmAtlas) confirmations++;
                    if (request.Kind == SessionControlKind.CommitUnload)
                    {
                        unloads++;
                        remoteState = AtlasLoadState.Unloaded;
                    }

                    return AtlasReply(request);
                }

                owner = new DesktopSessionPreferences(Guid.NewGuid(), 1, new UserPreferences(), Send, atlasIntents: intents);
                owner.OnAtlasError.AddListener(_ => errors++);
                await owner.StartAsync(timeout.Token);
                await AtlasInventoryWork(owner);
                manager.GetType().GetField("sessionPreferences", flags).SetValue(manager, owner);
                manager.GetType().GetField("connected", flags).SetValue(manager, true);
                manager.GetType().GetField("credential", flags).SetValue(manager, new byte[32]);
                await manager.SetAtlasLoadedAsync(id, true);
                if (sceneOwnsLoad) sceneReadiness = owner.EnsureAtlasReadyAsync(id, timeout.Token).AsTask();
                var request = await entered.Task;
                Assert.That(AtlasResources.IsLoaded(id), Is.True);
                Assert.That(wake.Task.IsCompleted, Is.False, "Desktop completion must not wait for the headset.");
                Assert.DoesNotThrow(() => SessionAtlasCatalog.RequireLocal(id));
                Assert.That(SessionAtlasCatalog.CanUse(id), Is.False, "Quest display still requires confirmation.");
                Assert.That(manager.GetAtlasSynchronizationStatus(id), Does.Contain("usable on Desktop").And.Contain("pending"));
                if (unloadBeforeWake) await manager.SetAtlasLoadedAsync(id, false);
                wake.SetResult(AtlasReply(request));
                if (sceneReadiness != null)
                {
                    Exception interrupted = null;
                    try
                    {
                        await sceneReadiness;
                    }
                    catch (Exception error)
                    {
                        interrupted = error;
                    }

                    Assert.That(interrupted, Is.Not.Null, "The scene request must abandon its old Load when the user unloads locally.");
                }

                await AtlasInventoryWork(owner);
                Assert.That(AtlasResources.IsLoaded(id), Is.EqualTo(!unloadBeforeWake));
                Assert.That(remoteState, Is.EqualTo(unloadBeforeWake ? AtlasLoadState.Unloaded : AtlasLoadState.Loaded));
                Assert.That(confirmations, Is.EqualTo(unloadBeforeWake ? 0 : 1));
                Assert.That(unloads, Is.EqualTo(unloadBeforeWake ? 1 : 0));
                Assert.That(loads, Is.EqualTo(1));
                Assert.That(errors, Is.Zero);
            }
            finally
            {
                owner?.Dispose();
                wake.TrySetCanceled();
                if (sceneReadiness != null)
                    try
                    {
                        await sceneReadiness;
                    }
                    catch (Exception)
                    {
                    }

                if (owner != null) await AtlasInventoryWork(owner);
                UnityEngine.Object.DestroyImmediate(root);
                Object3DManager.IBC.Clean();
                Object3DManager.IBC = previousIBC;
                Object3DManager.DiFuMo.Clean();
                Object3DManager.DiFuMo = previousDiFuMo;
                Object3DManager.MarsAtlas.Dispose();
                Object3DManager.MarsAtlas = previousMars;
                path.SetValue(null, previousPath);
            }
        }

        [Test]
        public async Task SleepingInventoryTimesOutReleasesItsGateAndWakeSynchronizesLoadedAtlas()
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            int disconnected = 0, errors = 0, loads = 0, confirmations = 0;
            try
            {
                await AtlasResources.LoadAsync("mars", timeout.Token);
                using var asleep = new DesktopSessionPreferences(Guid.NewGuid(), 1, new UserPreferences(), async (request, token) =>
                {
                    if (request.Kind == SessionControlKind.AtlasInventory) await Task.Delay(Timeout.Infinite, token);
                    return AtlasReply(request);
                }, controlTimeout: TimeSpan.FromMilliseconds(50));
                asleep.OnConnectionLost.AddListener(() => disconnected++);
                asleep.OnAtlasError.AddListener(_ => errors++);
                await asleep.StartAsync(timeout.Token);
                await AtlasInventoryWork(asleep);
                Assert.That(disconnected, Is.EqualTo(1));
                Assert.That(errors, Is.Zero);
                Assert.That(asleep.AtlasStatus, Does.Contain("asleep"));
                Assert.That(((SemaphoreSlim)typeof(DesktopSessionPreferences).GetField("m_Gate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(asleep)).CurrentCount, Is.EqualTo(1));
                using var awake = new DesktopSessionPreferences(Guid.NewGuid(), 2, new UserPreferences(), (request, token) =>
                {
                    if (request.Kind == SessionControlKind.LoadAtlas) loads++;
                    if (request.Kind == SessionControlKind.ConfirmAtlas) confirmations++;
                    return Task.FromResult(AtlasReply(request, inventoryState: AtlasLoadState.Unloaded));
                });
                await awake.StartAsync(timeout.Token);
                asleep.Dispose();
                await AtlasInventoryWork(awake);
                Assert.That(loads, Is.EqualTo(1));
                Assert.That(confirmations, Is.EqualTo(1));
                Assert.That(SessionAtlasCatalog.Active && SessionAtlasCatalog.CanUse("mars"), Is.True, "Disposing an old connection must not clear the new generation.");
            }
            finally
            {
                AtlasResources.Unload("mars");
            }
        }

        [Test]
        public async Task SleepingDuringLongAtlasLoadStopsRemoteWorkWithoutLosingDesktopSuccess()
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            int disconnected = 0, probes = 0, errors = 0;
            try
            {
                await AtlasResources.LoadAsync("mars", timeout.Token);
                using var owner = new DesktopSessionPreferences(Guid.NewGuid(), 1, new UserPreferences(), async (request, token) =>
                {
                    if (request.Kind == SessionControlKind.LoadAtlas) await Task.Delay(Timeout.Infinite, token);
                    return AtlasReply(request, inventoryState: AtlasLoadState.Unloaded);
                }, peerReady: token =>
                {
                    probes++;
                    return Task.FromResult(false);
                });
                owner.OnConnectionLost.AddListener(() => disconnected++);
                owner.OnAtlasError.AddListener(_ => errors++);
                await owner.StartAsync(timeout.Token);
                await AtlasInventoryWork(owner);
                Assert.That(disconnected, Is.EqualTo(1));
                Assert.That(probes, Is.EqualTo(1));
                Assert.That(errors, Is.Zero);
                Assert.That(AtlasResources.IsLoaded("mars"), Is.True);
            }
            finally
            {
                AtlasResources.Unload("mars");
            }
        }

        [TestCase(false), TestCase(true)]
        public async Task UnloadIntentSurvivesReconnectionAndRemoteInventoryCannotReloadDesktop(bool remoteInitiallyInUse)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var intents = new Dictionary<string, bool> { ["mars"] = false };
            int loads = 0, unloads = 0;
            AtlasResources.Unload("mars");
            using var owner = new DesktopSessionPreferences(Guid.NewGuid(), 2, new UserPreferences(), (request, token) =>
            {
                if (request.Kind == SessionControlKind.LoadAtlas) loads++;
                if (request.Kind == SessionControlKind.CommitUnload) unloads++;
                return Task.FromResult(AtlasReply(request, failed: remoteInitiallyInUse && request.Kind == SessionControlKind.PrepareUnload, inventoryState: AtlasLoadState.Loaded));
            }, atlasIntents: intents);
            await owner.StartAsync(timeout.Token);
            await AtlasInventoryWork(owner);
            if (remoteInitiallyInUse)
            {
                Assert.That(unloads, Is.Zero, "Do not unload an atlas retained by the old Quest display.");
                remoteInitiallyInUse = false;
                owner.RetryPendingAtlasUnloads();
                await AtlasInventoryWork(owner);
            }

            Assert.That(loads, Is.Zero);
            Assert.That(unloads, Is.EqualTo(1));
            Assert.That(AtlasResources.IsLoaded("mars"), Is.False);
        }

        [Test]
        public async Task OldConfirmationCannotMakeReplacementSessionReady()
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var entered = new TaskCompletionSource<SessionControlRequest>();
            var late = new TaskCompletionSource<SessionControlResponse>();
            using var watchdog = timeout.Token.Register(() =>
            {
                entered.TrySetCanceled();
                late.TrySetCanceled();
            });
            DesktopSessionPreferences old = null, current = null;
            try
            {
                await AtlasResources.LoadAsync("mars", timeout.Token);
                old = new DesktopSessionPreferences(Guid.NewGuid(), 1, new UserPreferences(), (request, token) =>
                {
                    if (request.Kind == SessionControlKind.ConfirmAtlas)
                    {
                        entered.TrySetResult(request);
                        return late.Task;
                    }

                    return Task.FromResult(AtlasReply(request, inventoryState: AtlasLoadState.Loaded));
                });
                await old.StartAsync(timeout.Token);
                var confirmation = await entered.Task;
                old.Dispose();
                current = new DesktopSessionPreferences(Guid.NewGuid(), 2, new UserPreferences(), async (request, token) =>
                {
                    if (request.Kind == SessionControlKind.AtlasInventory) await Task.Delay(Timeout.Infinite, token);
                    return AtlasReply(request);
                });
                await current.StartAsync(timeout.Token);
                late.SetResult(AtlasReply(confirmation));
                await AtlasInventoryWork(old);
                Assert.That(SessionAtlasCatalog.Active, Is.True);
                Assert.That(SessionAtlasCatalog.CanUse("mars"), Is.False);
            }
            finally
            {
                old?.Dispose();
                current?.Dispose();
                late.TrySetCanceled();
                if (old != null) await AtlasInventoryWork(old);
                if (current != null) await AtlasInventoryWork(current);
                AtlasResources.Unload("mars");
            }
        }

        [TestCase(false), TestCase(true)]
        public async Task ToolbarAtlasRecoveryRequiresConfirmationAndOwnsItsFailureDialog(bool failRemoteLoad)
        {
            var root = new UnityEngine.GameObject("Toolbar atlas recovery session");
            root.SetActive(false);
            var manager = root.AddComponent<HBP.Quest.Desktop.QuestManager>();
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var type = manager.GetType();
            var previousAtlas = Object3DManager.MarsAtlas;
            var pathProperty = typeof(HBP.Core.Tools.ApplicationState).GetProperty("DataPath");
            string previousPath = HBP.Core.Tools.ApplicationState.DataPath;
            Object3DManager.MarsAtlas = new MarsAtlas();
            DesktopSessionPreferences owner = null;
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            int loads = 0, confirmations = 0, errors = 0;
            try
            {
                pathProperty.SetValue(null, Path.GetFullPath("Assets/Data"));
                owner = new DesktopSessionPreferences(Guid.NewGuid(), 1, new UserPreferences(), (request, token) =>
                {
                    if (request.Kind == SessionControlKind.LoadAtlas) loads++;
                    if (request.Kind == SessionControlKind.ConfirmAtlas) confirmations++;
                    return Task.FromResult(AtlasReply(request, failRemoteLoad && request.Kind == SessionControlKind.LoadAtlas));
                });
                owner.OnAtlasError.AddListener(_ => errors++);
                await owner.StartAsync(timeout.Token);
                await AtlasInventoryWork(owner);
                type.GetField("connected", flags).SetValue(manager, true);
                type.GetField("credential", flags).SetValue(manager, new byte[32]);
                type.GetField("sessionPreferences", flags).SetValue(manager, owner);
                Exception failure = null;
                try
                {
                    await manager.EnsureAtlasReadyAsync("mars", timeout.Token);
                }
                catch (Exception exception)
                {
                    failure = exception;
                }

                Assert.That(errors, Is.Zero, "The toolbar must own the only error dialog.");
                Assert.That(((SemaphoreSlim)type.GetField("sessionOperations", flags).GetValue(manager)).CurrentCount, Is.EqualTo(1));
                if (failRemoteLoad)
                {
                    Assert.That(failure, Is.TypeOf<InvalidOperationException>());
                    Assert.That(confirmations, Is.Zero);
                    Assert.That(loads, Is.EqualTo(3));
                }
                else
                {
                    Assert.That(failure, Is.Null);
                    Assert.That(loads, Is.EqualTo(1));
                    Assert.That(confirmations, Is.EqualTo(1));
                    await manager.EnsureAtlasReadyAsync("mars", timeout.Token);
                    Assert.That(confirmations, Is.EqualTo(1), "A confirmed atlas must be reusable without another transfer.");
                }
            }
            finally
            {
                owner?.Dispose();
                UnityEngine.Object.DestroyImmediate(root);
                AtlasResources.Unload("mars");
                Object3DManager.MarsAtlas.Dispose();
                Object3DManager.MarsAtlas = previousAtlas;
                pathProperty.SetValue(null, previousPath);
            }
        }

        [Test]
        public async Task ToolbarAtlasRecoveryCanCancelWhilePairingOwnsTheSessionGate()
        {
            var root = new UnityEngine.GameObject("Cancelled atlas recovery session");
            root.SetActive(false);
            var manager = root.AddComponent<HBP.Quest.Desktop.QuestManager>();
            var gate = (SemaphoreSlim)manager.GetType().GetField("sessionOperations", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(manager);
            using var stop = new CancellationTokenSource();
            await gate.WaitAsync();
            try
            {
                var work = manager.EnsureAtlasReadyAsync("mars", stop.Token).AsTask();
                Assert.That(work.IsCompleted, Is.False);
                stop.Cancel();
                Exception failure = null;
                try
                {
                    await work;
                }
                catch (Exception exception)
                {
                    failure = exception;
                }

                Assert.That(failure, Is.InstanceOf<OperationCanceledException>());
                Assert.That(gate.CurrentCount, Is.Zero, "Cancellation must not release the pairing operation's gate.");
            }
            finally
            {
                gate.Release();
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [TestCase(false), TestCase(true)]
        public async Task ConnectionAttemptJoinsExistingInventoryBeforeTakingTheSessionGate(bool cancel)
        {
            var root = new UnityEngine.GameObject("Quest inventory ownership test");
            root.SetActive(false);
            var manager = root.AddComponent<HBP.Quest.Desktop.QuestManager>();
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var type = typeof(HBP.Quest.Desktop.QuestManager);
            var inventory = new TaskCompletionSource<bool>();
            type.GetField("inventoryWork", flags).SetValue(manager, inventory.Task);
            type.GetField("automaticOperation", flags).SetValue(manager, true);
            bool invoked = false;
            Func<CancellationToken, Task> action = token =>
            {
                token.ThrowIfCancellationRequested();
                invoked = true;
                return Task.CompletedTask;
            };
            var operation = (Task)type.GetMethod("ExecuteAsync", flags).Invoke(manager, new object[] { action });
            try
            {
                Assert.That(invoked, Is.False);
                var gate = (SemaphoreSlim)type.GetField("sessionOperations", flags).GetValue(manager);
                Assert.That(gate.CurrentCount, Is.EqualTo(1), "Inventory may itself need the gate to finish a scene send.");
                if (cancel)
                {
                    manager.CancelConnectionAttempt();
                    await UniTask.NextFrame();
                    Assert.That(operation.IsCompleted, Is.False, "Cancellation must drain the existing inventory before closing its presentation.");
                }

                inventory.SetResult(true);
                await operation;
                Assert.That(invoked, Is.EqualTo(!cancel));
                Assert.That(manager.LastOperationCancelled, Is.EqualTo(cancel));
                Assert.That(gate.CurrentCount, Is.EqualTo(1));
            }
            finally
            {
                inventory.TrySetResult(true);
                await operation;
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void LostSessionCannotKeepAPendingManualSuccess()
        {
            var root = new UnityEngine.GameObject("Quest lost session test");
            root.SetActive(false);
            var manager = root.AddComponent<HBP.Quest.Desktop.QuestManager>();
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var type = typeof(HBP.Quest.Desktop.QuestManager);
            type.GetField("connected", flags).SetValue(manager, true);
            type.GetField("credential", flags).SetValue(manager, new byte[32]);
            type.GetField("<LastOperationSucceeded>k__BackingField", flags).SetValue(manager, true);
            try
            {
                type.GetMethod("SessionConnectionLost", flags).Invoke(manager, null);
                Assert.That(manager.IsPaired, Is.False);
                Assert.That(manager.LastOperationSucceeded, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public async Task UsbApplicationAppearingAfterCableConnectionUsesOnlyTheRetainedForward()
        {
            using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            using var receiver = new QuestPairing();
            var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            listener.Start();
            int port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, port);
            int commands = 0;
            var usb = new QuestUsbDiscovery("unused", (command, token) =>
            {
                commands++;
                return Task.FromResult(command == "devices -l" ? "SERIAL device usb:1 product:eureka" : command.Contains("forward tcp:0") ? port.ToString() : "");
            });
            Assert.That(await usb.FindAsync(stop.Token), Is.Empty);
            Assert.That(usb.HasForwards, Is.True);
            int afterInventory = commands;
            listener.Start();
            var serving = receiver.ServeAsync(listener, stop.Token, (_, __) => throw new InvalidOperationException(), _ => { });
            try
            {
                var found = await usb.ProbeAsync(stop.Token);
                Assert.That(found.Count, Is.EqualTo(1));
                Assert.That(found[0].Id, Is.EqualTo(Convert.ToBase64String(receiver.Pin)));
                Assert.That(found[0].UsbSerial, Is.EqualTo("SERIAL"));
                Assert.That(commands, Is.EqualTo(afterInventory), "Probing the retained endpoint must never launch ADB again.");
            }
            finally
            {
                stop.Cancel();
                await serving;
            }
        }

        [Test, Timeout(90000)]
        public async Task IdleAutomaticDiscoveryDoesNotPollAdbForOneMinute()
        {
            var root = new UnityEngine.GameObject("Quest idle discovery test");
            root.SetActive(false);
            var manager = root.AddComponent<HBP.Quest.Desktop.QuestManager>();
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var type = typeof(HBP.Quest.Desktop.QuestManager);
            string directory = Path.Combine(Path.GetTempPath(), "hibop-idle-discovery-" + Guid.NewGuid().ToString("N"));
            int commands = 0;
            type.GetField("store", flags).SetValue(manager, directory);
            type.GetField("sceneLifecyclePreferences", flags).SetValue(manager, new UserPreferences());
            type.GetField("usb", flags).SetValue(manager, new QuestUsbDiscovery("unused", (_, token) =>
            {
                Interlocked.Increment(ref commands);
                return Task.FromResult("");
            }));
            var tick = type.GetMethod("TickDiscovery", flags);
            try
            {
                type.GetMethod("InitializeDiscovery", flags).Invoke(manager, null);
                await (Task)type.GetField("discovering", flags).GetValue(manager);
                var warmup = System.Diagnostics.Stopwatch.StartNew();
                while (warmup.Elapsed.TotalSeconds < 3)
                {
                    tick.Invoke(manager, null);
                    await UniTask.Delay(100, DelayType.Realtime);
                }

                int initial = Volatile.Read(ref commands);
                Assert.That(initial, Is.GreaterThanOrEqualTo(2), string.Join("; ", manager.DiscoveryMessages));
                int scans = 0;
                Task previous = (Task)type.GetField("discovering", flags).GetValue(manager);
                var clock = System.Diagnostics.Stopwatch.StartNew();
                while (clock.Elapsed.TotalSeconds < 60)
                {
                    tick.Invoke(manager, null);
                    var current = (Task)type.GetField("discovering", flags).GetValue(manager);
                    if (!ReferenceEquals(current, previous))
                    {
                        scans++;
                        previous = current;
                    }

                    await UniTask.Delay(100, DelayType.Realtime);
                }

                int during = Volatile.Read(ref commands) - initial;
                string report = "{\"seconds\":60,\"initialAdbCommands\":" + initial + ",\"adbCommandsDuringMinute\":" + during + ",\"discoveryWindowsDuringMinute\":" + scans + "}";
                string output = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..", ".test-results", "quest-pairing"));
                Directory.CreateDirectory(output);
                File.WriteAllText(Path.Combine(output, "idle-discovery.json"), report);
                Assert.That(initial, Is.GreaterThanOrEqualTo(2), "The initial USB inventory must actually run.");
                Assert.That(during, Is.LessThanOrEqualTo(2), "Allow one external PnP event, but reject periodic ADB scans.");
            }
            finally
            {
                type.GetMethod("DisposeDiscovery", flags).Invoke(manager, null);
                ((CancellationTokenSource)type.GetField("lifetime", flags).GetValue(manager)).Cancel();
                await (Task)type.GetField("discovering", flags).GetValue(manager);
                await (Task)type.GetField("listening", flags).GetValue(manager);
                UnityEngine.Object.DestroyImmediate(root);
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        [Test]
        public void SelectingHeadsetIsNonDestructiveAndDisconnectRetainsAssociation()
        {
            var root = new UnityEngine.GameObject("Quest connection lifecycle test");
            root.SetActive(false);
            var manager = root.AddComponent<HBP.Quest.Desktop.QuestManager>();
            byte[] credential = new byte[32], pin = new byte[32];
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            typeof(HBP.Quest.Desktop.QuestManager).GetField("credential", flags).SetValue(manager, credential);
            typeof(HBP.Quest.Desktop.QuestManager).GetField("pin", flags).SetValue(manager, pin);
            typeof(HBP.Quest.Desktop.QuestManager).GetField("connected", flags).SetValue(manager, true);
            try
            {
                var other = new QuestDevice { Pin = new byte[32], Host = "127.0.0.1", Name = "Other Quest" };
                other.Pin[0] = 1;
                manager.Select(other);
                Assert.That(manager.IsPaired, Is.True);
                Assert.That(manager.IsSelectedConnected, Is.False);
                Assert.That(manager.ConnectedId, Is.EqualTo(Convert.ToBase64String(pin)));
                manager.Disconnect();
                Assert.That(manager.IsPaired, Is.False);
                Assert.That(typeof(HBP.Quest.Desktop.QuestManager).GetField("credential", flags).GetValue(manager), Is.SameAs(credential));
                Assert.That((bool)typeof(HBP.Quest.Desktop.QuestManager).GetField("autoConnectSuspended", flags).GetValue(manager), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public async Task CancellingSessionOpenCancelsTheNetworkRequestAndDrainsIt()
        {
            using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var entered = new TaskCompletionSource<bool>();
            var pending = new TaskCompletionSource<SessionControlResponse>();
            using var owner = new DesktopSessionPreferences(Guid.NewGuid(), 1, new UserPreferences(), async (request, token) =>
            {
                using var cancellation = token.Register(() => pending.TrySetCanceled());
                entered.TrySetResult(true);
                return await pending.Task;
            });
            var opening = owner.StartAsync(stop.Token).AsTask();
            await entered.Task;
            stop.Cancel();
            Exception failure = null;
            try
            {
                await opening;
            }
            catch (Exception error)
            {
                failure = error;
            }

            Assert.That(failure, Is.InstanceOf<OperationCanceledException>());
            Assert.That(pending.Task.IsCompleted, Is.True);
            Assert.That(owner.Status, Does.Not.Contain("Quest connected"));
        }

        [Test]
        public async Task OpeningFinishesWhileAtlasInventoryRemainsPending()
        {
            var pending = new TaskCompletionSource<SessionControlResponse>();
            var entered = new TaskCompletionSource<SessionControlRequest>();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using var cancellation = timeout.Token.Register(() =>
            {
                pending.TrySetCanceled();
                entered.TrySetCanceled();
            });
            using var owner = new DesktopSessionPreferences(Guid.NewGuid(), 1, new UserPreferences(), (request, token) =>
            {
                if (request.Kind == SessionControlKind.AtlasInventory)
                {
                    entered.TrySetResult(request);
                    return pending.Task;
                }

                return Task.FromResult(new SessionControlResponse(request.OperationId, SessionControlStatus.Applied, message: SessionControlCodec.AtlasInventoryCapability));
            });
            try
            {
                await owner.StartAsync();
                var request = await entered.Task;
                Assert.That(owner.Status, Does.Contain("Quest connected"));
                Assert.That(pending.Task.IsCompleted, Is.False);
                pending.SetResult(new SessionControlResponse(request.OperationId, SessionControlStatus.Applied, body: Encoding.UTF8.GetBytes("{\"Preparing\":false,\"Atlases\":{}}")));
            }
            finally
            {
                owner.Dispose();
                pending.TrySetCanceled();
            }
        }

        [Test]
        public async Task BackgroundInventoryConfirmsLoadedAtlasWithoutComparingLegacyContentHashes()
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var reported = new TaskCompletionSource<bool>();
            using var cancellation = timeout.Token.Register(() => reported.TrySetCanceled());
            var loaded = await AtlasResources.LoadAsync("mars", timeout.Token);
            Assert.That(loaded.Succeeded, Is.True, loaded.Error);
            int confirmations = 0, disconnections = 0;
            try
            {
                using var owner = new DesktopSessionPreferences(Guid.NewGuid(), 1, new UserPreferences(), (request, token) =>
                {
                    if (request.Kind == SessionControlKind.ConfirmAtlas)
                    {
                        confirmations++;
                        reported.TrySetResult(true);
                    }

                    string inventory = "{\"Preparing\":false,\"Atlases\":{\"mars\":{\"State\":" + (int)AtlasLoadState.Loaded + ",\"Preload\":true,\"Fingerprint\":\"" + new string('0', 64) + "\"}}}";
                    return Task.FromResult(new SessionControlResponse(request.OperationId, SessionControlStatus.Applied, message: SessionControlCodec.AtlasInventoryCapability, body: Encoding.UTF8.GetBytes(inventory)));
                });
                owner.OnConnectionLost.AddListener(() => disconnections++);
                await owner.StartAsync();
                await reported.Task;
                await Cysharp.Threading.Tasks.UniTask.NextFrame(cancellationToken: timeout.Token);
                Assert.That(confirmations, Is.EqualTo(1));
                Assert.That(disconnections, Is.Zero);
            }
            finally
            {
                AtlasResources.Unload("mars");
            }
        }

        [Test]
        public async Task ManualLoadConfirmsBothDevicesWithoutComparingContentHashes()
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            int confirmations = 0;
            try
            {
                using var owner = new DesktopSessionPreferences(Guid.NewGuid(), 1, new UserPreferences(), (request, token) =>
                {
                    if (request.Kind == SessionControlKind.ConfirmAtlas) confirmations++;
                    return Task.FromResult(new SessionControlResponse(request.OperationId, SessionControlStatus.Applied, message: SessionControlCodec.AtlasInventoryCapability, fingerprint: new string('0', 64), body: Encoding.UTF8.GetBytes("{\"Preparing\":false,\"Atlases\":{}}")));
                });
                await owner.StartAsync();
                await owner.SetAtlasLoadedAsync("mars", true);
                Assert.That(confirmations, Is.EqualTo(1));
                Assert.That(owner.AtlasStatus, Does.Not.Contain("incompatible"));
                Assert.That(AtlasResources.IsLoaded("mars"), Is.True);
                await owner.SetAtlasLoadedAsync("mars", false);
                Assert.That(AtlasResources.IsLoaded("mars"), Is.False);
            }
            finally
            {
                if (AtlasResources.IsLoaded("mars")) AtlasResources.Unload("mars");
            }
        }

        [Test]
        public async Task IncompatibleProtocolRequestsAnUpdate()
        {
            using var owner = new DesktopSessionPreferences(Guid.NewGuid(), 1, new UserPreferences(), (request, token) => Task.FromResult(new SessionControlResponse(request.OperationId, SessionControlStatus.Applied, body: Encoding.UTF8.GetBytes("{}"))));
            Exception failure = null;
            try
            {
                await owner.StartAsync();
            }
            catch (Exception exception)
            {
                failure = exception;
            }

            Assert.That(failure, Is.TypeOf<InvalidOperationException>());
            Assert.That(failure.Message, Does.Contain("Update HiBoP Desktop and Quest together"));
        }

        [TestCase(false), TestCase(true)]
        public async Task AtlasLoadRecoversPartialFailureWithoutReplacingTheLoadedDesktopAtlas(bool failConfirmation)
        {
            using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            int loads = 0, confirmations = 0, errors = 0;
            MarsAtlas firstLoaded = null;

            void Loaded(AtlasLoadResult result)
            {
                if (result.Id == "mars" && result.State == AtlasLoadState.Loaded) firstLoaded ??= Object3DManager.MarsAtlas;
            }

            AtlasResources.OnChanged.AddListener(Loaded);
            try
            {
                using var owner = new DesktopSessionPreferences(Guid.NewGuid(), 1, new UserPreferences(), (request, token) =>
                {
                    bool failed = request.Kind == SessionControlKind.LoadAtlas && ++loads == 1 && !failConfirmation;
                    if (request.Kind == SessionControlKind.ConfirmAtlas) failed = ++confirmations == 1 && failConfirmation;
                    return Task.FromResult(AtlasReply(request, failed));
                });
                owner.OnAtlasError.AddListener(_ => errors++);
                await owner.StartAsync(stop.Token);
                await owner.SetAtlasLoadedAsync("mars", true, stop.Token);
                Assert.That(loads, Is.EqualTo(2));
                Assert.That(confirmations, Is.EqualTo(failConfirmation ? 2 : 1));
                Assert.That(errors, Is.Zero);
                Assert.That(Object3DManager.MarsAtlas, Is.SameAs(firstLoaded));
                await owner.EnsureAtlasReadyAsync("mars", stop.Token);
                Assert.That(loads, Is.EqualTo(2), "The recovered atlas must be ready for scene delivery.");
            }
            finally
            {
                AtlasResources.OnChanged.RemoveListener(Loaded);
                AtlasResources.Unload("mars");
            }
        }

        [TestCase(false), TestCase(true)]
        public async Task ExhaustedAtlasRecoveryReportsOnceAndInventoryDoesNotRestartIt(bool background)
        {
            using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            int loads = 0;
            var errors = new List<string>();
            try
            {
                using var owner = new DesktopSessionPreferences(Guid.NewGuid(), 1, new UserPreferences(), (request, token) =>
                {
                    if (request.Kind == SessionControlKind.LoadAtlas) loads++;
                    return Task.FromResult(AtlasReply(request, request.Kind == SessionControlKind.LoadAtlas, AtlasLoadState.Failed));
                });
                owner.OnAtlasError.AddListener(errors.Add);
                await owner.StartAsync(stop.Token);
                if (!background) await owner.SetAtlasLoadedAsync("mars", true, stop.Token);
                await AtlasInventoryWork(owner);
                await AtlasResources.LoadAsync("mars", stop.Token);
                await AtlasInventoryWork(owner);
                Assert.That(loads, Is.EqualTo(3));
                Assert.That(errors.Count, Is.EqualTo(background ? 0 : 1), "Automatic retries update status quietly; explicit actions own their failure dialog.");
                Assert.That(owner.AtlasStatus, Does.Contain("mars").And.Contain("Temporary Quest failure"));
                Assert.That(AtlasResources.IsLoaded("mars"), Is.True, "Keep the Desktop success after Quest exhaustion.");
            }
            finally
            {
                AtlasResources.Unload("mars");
            }
        }

        [TestCase(false), TestCase(true)]
        public async Task CancellingAtlasRecoveryAbandonsBackoffWithoutErrorOrReplay(bool disconnect)
        {
            using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            using var cancel = new CancellationTokenSource();
            var entered = new TaskCompletionSource<bool>();
            int loads = 0, errors = 0;
            try
            {
                using var owner = new DesktopSessionPreferences(Guid.NewGuid(), 1, new UserPreferences(), (request, token) =>
                {
                    if (request.Kind == SessionControlKind.LoadAtlas)
                    {
                        loads++;
                        entered.TrySetResult(true);
                    }

                    return Task.FromResult(AtlasReply(request, request.Kind == SessionControlKind.LoadAtlas));
                });
                owner.OnAtlasError.AddListener(_ => errors++);
                await owner.StartAsync(stop.Token);
                var work = owner.SetAtlasLoadedAsync("mars", true, cancel.Token).AsTask();
                await entered.Task;
                await UniTask.NextFrame(cancellationToken: stop.Token);
                if (disconnect) owner.Dispose();
                else cancel.Cancel();
                await work;
                await AtlasInventoryWork(owner);
                Assert.That(loads, Is.EqualTo(1));
                Assert.That(errors, Is.Zero);
            }
            finally
            {
                AtlasResources.Unload("mars");
            }
        }

        [TestCase(false), TestCase(true)]
        public async Task BackgroundAtlasFailureAndConfirmationRecoverAutomatically(bool failConfirmation)
        {
            using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            int loads = 0, confirmations = 0, errors = 0;
            var remoteState = failConfirmation ? AtlasLoadState.Loaded : AtlasLoadState.Failed;
            try
            {
                await AtlasResources.LoadAsync("mars", stop.Token);
                using var owner = new DesktopSessionPreferences(Guid.NewGuid(), 1, new UserPreferences(), (request, token) =>
                {
                    bool failed = false;
                    if (request.Kind == SessionControlKind.LoadAtlas)
                    {
                        failed = ++loads == 1 && !failConfirmation;
                        if (!failed) remoteState = AtlasLoadState.Loaded;
                    }

                    if (request.Kind == SessionControlKind.ConfirmAtlas) failed = ++confirmations == 1 && failConfirmation;
                    return Task.FromResult(AtlasReply(request, failed, remoteState));
                });
                owner.OnAtlasError.AddListener(_ => errors++);
                await owner.StartAsync(stop.Token);
                await AtlasInventoryWork(owner);
                Assert.That(loads, Is.EqualTo(failConfirmation ? 1 : 2));
                Assert.That(confirmations, Is.EqualTo(failConfirmation ? 2 : 1));
                Assert.That(errors, Is.Zero);
                await owner.EnsureAtlasReadyAsync("mars", stop.Token);
                Assert.That(loads, Is.EqualTo(failConfirmation ? 1 : 2));
            }
            finally
            {
                AtlasResources.Unload("mars");
            }
        }

        [TestCase(SessionControlKind.PrepareUnload), TestCase(SessionControlKind.CommitUnload)]
        public async Task UnloadRecoveryAbortsAnUncertainReservationBeforeRetrying(SessionControlKind lostReply)
        {
            using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            int prepares = 0, aborts = 0, errors = 0;
            Guid? reservation = null;
            bool lost = false;
            try
            {
                await AtlasResources.LoadAsync("mars", stop.Token);
                using var owner = new DesktopSessionPreferences(Guid.NewGuid(), 1, new UserPreferences(), (request, token) =>
                {
                    if (request.Kind == SessionControlKind.PrepareUnload)
                    {
                        Assert.That(reservation, Is.Null, "Every retry must release the previous reservation first.");
                        reservation = request.OperationId;
                        prepares++;
                    }

                    if (request.Kind == SessionControlKind.AbortUnload)
                    {
                        Assert.That(new Guid(request.GetBody()), Is.EqualTo(reservation));
                        reservation = null;
                        aborts++;
                    }

                    if (request.Kind == lostReply && !lost)
                    {
                        lost = true;
                        return Task.FromException<SessionControlResponse>(new IOException("Reply lost"));
                    }

                    if (request.Kind == SessionControlKind.CommitUnload) reservation = null;
                    return Task.FromResult(AtlasReply(request));
                });
                owner.OnAtlasError.AddListener(_ => errors++);
                await owner.StartAsync(stop.Token);
                await owner.SetAtlasLoadedAsync("mars", false, stop.Token);
                Assert.That(prepares, Is.EqualTo(2));
                Assert.That(aborts, Is.EqualTo(1));
                Assert.That(reservation, Is.Null);
                Assert.That(errors, Is.Zero);
                Assert.That(AtlasResources.IsLoaded("mars"), Is.False);
            }
            finally
            {
                AtlasResources.Unload("mars");
            }
        }

        [Test]
        public async Task QueuedUnloadPreventsStaleInventoryFromReloadingTheAtlas()
        {
            using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            var entered = new TaskCompletionSource<bool>();
            var inventory = new TaskCompletionSource<SessionControlResponse>();
            SessionControlRequest captured = null;
            int loads = 0, errors = 0;
            try
            {
                await AtlasResources.LoadAsync("mars", stop.Token);
                using var owner = new DesktopSessionPreferences(Guid.NewGuid(), 1, new UserPreferences(), (request, token) =>
                {
                    if (request.Kind == SessionControlKind.LoadAtlas) loads++;
                    if (request.Kind == SessionControlKind.AtlasInventory && captured == null)
                    {
                        captured = request;
                        entered.TrySetResult(true);
                        return inventory.Task;
                    }

                    return Task.FromResult(AtlasReply(request));
                });
                owner.OnAtlasError.AddListener(_ => errors++);
                using var cancellation = stop.Token.Register(() =>
                {
                    entered.TrySetCanceled();
                    inventory.TrySetCanceled();
                });
                await owner.StartAsync(stop.Token);
                await entered.Task;
                var unload = owner.SetAtlasLoadedAsync("mars", false, stop.Token).AsTask();
                inventory.SetResult(AtlasReply(captured, inventoryState: AtlasLoadState.Failed));
                await unload;
                await AtlasInventoryWork(owner);
                Assert.That(loads, Is.Zero);
                Assert.That(errors, Is.Zero);
                Assert.That(AtlasResources.IsLoaded("mars"), Is.False);
            }
            finally
            {
                inventory.TrySetCanceled();
                AtlasResources.Unload("mars");
            }
        }

        [TestCase(false), TestCase(true)]
        public async Task SceneAtlasRecoveryReportsOnlyWhenCallerDoesNotOwnTheErrorDialog(bool callerOwnsDialog)
        {
            using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            int loads = 0, errors = 0;
            try
            {
                using var owner = new DesktopSessionPreferences(Guid.NewGuid(), 1, new UserPreferences(), (request, token) =>
                {
                    if (request.Kind == SessionControlKind.LoadAtlas) loads++;
                    return Task.FromResult(AtlasReply(request, request.Kind == SessionControlKind.LoadAtlas));
                });
                owner.OnAtlasError.AddListener(_ => errors++);
                await owner.StartAsync(stop.Token);
                Exception failure = null;
                try
                {
                    await owner.EnsureAtlasReadyAsync("mars", stop.Token, reportError: !callerOwnsDialog);
                }
                catch (Exception exception)
                {
                    failure = exception;
                }

                Assert.That(failure, Is.TypeOf<InvalidOperationException>());
                Assert.That(failure.Message, Does.Contain("Temporary Quest failure"));
                Assert.That(loads, Is.EqualTo(3));
                Assert.That(errors, Is.EqualTo(callerOwnsDialog ? 0 : 1), "Display owns its dialog; standalone sends need the atlas dialog.");
            }
            finally
            {
                AtlasResources.Unload("mars");
            }
        }

        [Test]
        public async Task AtlasRecoveryNeverUnloadsAResourceStillInUse()
        {
            using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            int unloads = 0, errors = 0;
            try
            {
                await AtlasResources.LoadAsync("mars", stop.Token);
                using var owner = new DesktopSessionPreferences(Guid.NewGuid(), 1, new UserPreferences(), (request, token) =>
                {
                    if (request.Kind == SessionControlKind.PrepareUnload) unloads++;
                    return Task.FromResult(AtlasReply(request));
                });
                owner.OnAtlasError.AddListener(_ => errors++);
                await owner.StartAsync(stop.Token);
                using (HBP.Core.Tools.ResourceRetention.Retain("mars"))
                    await owner.SetAtlasLoadedAsync("mars", false, stop.Token);
                Assert.That(unloads, Is.Zero);
                Assert.That(errors, Is.EqualTo(1));
                Assert.That(AtlasResources.IsLoaded("mars"), Is.True);
            }
            finally
            {
                AtlasResources.Unload("mars");
            }
        }

        [Test]
        public async Task PersistentTransportFailureReportsOnceBeforeDisconnectingTheSession()
        {
            using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            int loads = 0, errors = 0;
            bool disconnected = false;
            try
            {
                using var owner = new DesktopSessionPreferences(Guid.NewGuid(), 1, new UserPreferences(), (request, token) =>
                {
                    if (request.Kind == SessionControlKind.LoadAtlas)
                    {
                        loads++;
                        return Task.FromException<SessionControlResponse>(new IOException("Connection lost"));
                    }

                    return Task.FromResult(AtlasReply(request));
                });
                owner.OnAtlasError.AddListener(_ =>
                {
                    Assert.That(disconnected, Is.False);
                    errors++;
                });
                owner.OnConnectionLost.AddListener(() =>
                {
                    disconnected = true;
                    owner.Dispose();
                });
                await owner.StartAsync(stop.Token);
                await owner.SetAtlasLoadedAsync("mars", true, stop.Token);
                await AtlasInventoryWork(owner);
                Assert.That(loads, Is.EqualTo(3));
                Assert.That(errors, Is.EqualTo(1));
                Assert.That(disconnected, Is.True);
            }
            finally
            {
                AtlasResources.Unload("mars");
            }
        }

        private static SessionControlResponse AtlasReply(SessionControlRequest request, bool failed = false, AtlasLoadState? inventoryState = null)
        {
            string inventory = inventoryState.HasValue ? "{\"Preparing\":false,\"Atlases\":{\"mars\":{\"State\":" + (int)inventoryState.Value + "}}}" : "{\"Preparing\":false,\"Atlases\":{}}";
            return new SessionControlResponse(request.OperationId, failed ? SessionControlStatus.Failed : SessionControlStatus.Applied, message: failed ? "Temporary Quest failure" : SessionControlCodec.AtlasInventoryCapability, body: Encoding.UTF8.GetBytes(inventory));
        }

        private static Task AtlasInventoryWork(DesktopSessionPreferences owner) => (Task)typeof(DesktopSessionPreferences).GetField("m_InventoryWork", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(owner);

        [Test]
        public async Task SavesDuringOpeningAreCapturedOrderedAndAbandonedOnDispose()
        {
            string folder = Path.Combine(Path.GetTempPath(), "hibop-owner-test-" + Guid.NewGuid().ToString("N"));
            string originalPath = UserPreferences.PATH;
            var normalization = DataManager.DefaultNormalization;
            var averaging = DataManager.DefaultAveraging;
            var position = DataManager.DefaultPositionAveraging;
            bool correction = Site.SiteNameCorrection;
            DesktopSessionPreferences owner = null;
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var opening = new TaskCompletionSource<SessionControlResponse>();
            using var cancel = timeout.Token.Register(() => opening.TrySetCanceled());
            var sent = new List<SessionControlRequest>();
            try
            {
                Directory.CreateDirectory(folder);
                UserPreferences.PATH = Path.Combine(folder, "Preferences.txt");
                var general = new GeneralPreferences(new ProjectPreferences("Before", folder, folder), new ThemePreferences(), new LocalizationPreferences(), new SystemPreferences(), new MiscPreferences(), new QuestPreferences());
                var preferences = new UserPreferences(general, new DataPreferences(), new VisualizationPreferences());
                owner = new DesktopSessionPreferences(Guid.NewGuid(), 1, preferences, (request, token) =>
                {
                    if (request.Kind != SessionControlKind.AtlasInventory) sent.Add(request);
                    return request.Kind == SessionControlKind.Open ? opening.Task : Task.FromResult(new SessionControlResponse(request.OperationId, SessionControlStatus.Applied, body: Encoding.UTF8.GetBytes("{}")));
                });
                Task start = owner.StartAsync().AsTask();
                preferences.General.Project.DefaultName = "First save";
                preferences.Save();
                preferences.General.Project.DefaultName = "Second save";
                preferences.Save();
                Assert.That(sent.Count, Is.EqualTo(1));
                opening.SetResult(new SessionControlResponse(sent[0].OperationId, SessionControlStatus.Applied, message: SessionControlCodec.AtlasInventoryCapability, body: Encoding.UTF8.GetBytes("{}")));
                await start;
                await owner.FlushPreferencesAsync();
                Assert.That(sent.Count, Is.EqualTo(3));
                Assert.That(sent[1].Revision, Is.EqualTo(1));
                Assert.That(sent[2].Revision, Is.EqualTo(2));
                Assert.That(Encoding.UTF8.GetString(sent[1].GetBody()), Does.Contain("First save"));
                Assert.That(Encoding.UTF8.GetString(sent[2].GetBody()), Does.Contain("Second save"));
                owner.Dispose();
                preferences.General.Project.DefaultName = "Offline save";
                preferences.Save();
                Assert.That(sent.Count, Is.EqualTo(3));
                using var next = new DesktopSessionPreferences(Guid.NewGuid(), 2, preferences, (request, token) =>
                {
                    if (request.Kind != SessionControlKind.AtlasInventory) sent.Add(request);
                    return Task.FromResult(new SessionControlResponse(request.OperationId, SessionControlStatus.Applied, message: SessionControlCodec.AtlasInventoryCapability, body: Encoding.UTF8.GetBytes("{}")));
                });
                await next.StartAsync();
                await next.FlushPreferencesAsync();
                Assert.That(sent.Count, Is.EqualTo(4), "Opening another owner must not replay offline saves.");
                Assert.That(sent[3].Kind, Is.EqualTo(SessionControlKind.Open));
            }
            finally
            {
                owner?.Dispose();
                DataManager.DefaultNormalization = normalization;
                DataManager.DefaultAveraging = averaging;
                DataManager.DefaultPositionAveraging = position;
                Site.SiteNameCorrection = correction;
                UserPreferences.PATH = originalPath;
                if (Directory.Exists(folder)) Directory.Delete(folder, true);
            }
        }
    }
}
