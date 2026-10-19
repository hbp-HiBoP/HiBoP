using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Database;
using HBP.Core.Preferences;
using HBP.Core.Tools;
using HBP.Quest;
using HBP.Transfer.Scene;
using HBP.Transfer.Transport;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace HBP.Tests.Transfer
{
    public sealed class SessionDefinitionsTests
    {
        private string folder;
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [SetUp]
        public void SetUp() => folder = Path.Combine(Path.GetTempPath(), "hibop-definitions-" + Guid.NewGuid().ToString("N"));

        [TearDown]
        public void TearDown() { if (Directory.Exists(folder)) Directory.Delete(folder, true); }

        [Test]
        public void UpdatedAndRemovedDefinitionsKeepEarlierSceneDeliveriesReadable()
        {
            using var source = new SceneArchive(Path.Combine(folder, "source"));
            var payload = PreparedSceneArchiveTests.Fixture(source);
            var previous = source.Globals;
            string scene = Path.Combine(folder, "old.hbscene");
            source.Write(payload, scene);
            var data = new GlobalDataPayload
            {
                Id = previous.Id, DefinitionsRevision = 1, Preferences = previous.Data.Preferences,
                Tags = (TagCollection)previous.Data.Tags.Clone(), Protocols = previous.Data.Protocols.Select(value => (Protocol)value.Clone()).ToList(),
                Aliases = new AliasCollection(), Grid = 32
            };
            data.Tags.AllTags.Single().Name = "updated tag";
            data.Protocols[0].Name = "updated protocol";
            var updated = new PairingContext(data);
            updated.RetainPreviousDefinitions(previous);
            using var target = new SceneArchive(Path.Combine(folder, "target"), true, updated);
            var restored = target.Read(scene);
            Assert.That(restored.Visualization.Patients.Single().Tags.Single().Tag.Name, Is.EqualTo("group"));
            var serializer = PreparedDataJson.Create(null, updated);
            var reference = JToken.FromObject(data.Tags.AllTags.Single(), serializer);
            Assert.That(reference.ToObject<StringTag>(serializer), Is.SameAs(data.Tags.AllTags.Single()));

            var removed = new PairingContext(new GlobalDataPayload
            {
                Id = previous.Id, Preferences = data.Preferences, Tags = new TagCollection(), Protocols = new List<Protocol>(), Aliases = new AliasCollection(), Grid = 32
            });
            removed.RetainPreviousDefinitions(updated);
            using var retry = new SceneArchive(Path.Combine(folder, "retry"), true, removed);
            Assert.That(retry.Read(scene).Visualization.Patients.Single().Tags.Single().Tag.Name, Is.EqualTo("group"));
            Assert.Throws<ArgumentException>(() => previous.RetainPreviousDefinitions(removed));
            var foreign = new PairingContext(new GlobalDataPayload { Preferences = data.Preferences, Tags = new TagCollection(), Protocols = new List<Protocol>(), Aliases = new AliasCollection(), Grid = 32 });
            Assert.Throws<ArgumentException>(() => foreign.RetainPreviousDefinitions(previous));
        }

        [Test, Timeout(30000)]
        public async Task AuthenticatedUpdatesReuseTheAssociationWithoutInstallingPairingAgain()
        {
            Directory.CreateDirectory(folder);
            string source = Path.Combine(folder, "globals"), target = Path.Combine(folder, "received");
            File.WriteAllText(source, "initial");
            using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(25));
            using var pairing = new QuestPairing();
            var listener = new TcpListener(IPAddress.Loopback, QuestPairing.Port);
            listener.Start();
            int initial = 0, updates = 0;
            string stage = "pairing";
            var context = Guid.NewGuid();
            Task serving = pairing.ServeAsync(listener, stop.Token, (_, __) => throw new InvalidOperationException(), _ => { },
                (stream, token) => PinnedTlsTransfer.ReceiveFileAsync(stream, target, token, (_, __, ___) => { initial++; return Task.FromResult(DeliveryStatus.Published); }),
                receiveDefinitions: (request, stream, token) =>
                {
                    Assert.That(request.ContextId, Is.EqualTo(context));
                    Assert.That(request.Generation, Is.EqualTo(3));
                    Assert.That(request.Revision, Is.EqualTo(2));
                    return PinnedTlsTransfer.ReceiveFileAsync(stream, target, token, (_, __, ___) => { updates++; return Task.FromResult(DeliveryStatus.Published); });
                });
            try
            {
                byte[] credential = await QuestPairing.PairAsync("127.0.0.1", pairing.Pin, pairing.Code, stop.Token,
                    (stream, token) => PinnedTlsTransfer.SendFileAsync(stream, source, token));
                File.WriteAllText(source, "updated definitions");
                File.Delete(target);
                stage = "definitions update";
                var request = new SessionControlRequest(context, 3, Guid.NewGuid(), SessionControlKind.Definitions, 2);
                var receipt = await QuestPairing.UpdateDefinitionsAsync("127.0.0.1", pairing.Pin, credential, request, stop.Token,
                    (stream, token) => PinnedTlsTransfer.SendFileAsync(stream, source, token));
                Assert.That(receipt.Status, Is.EqualTo(DeliveryStatus.Published));
                Assert.That(File.ReadAllText(target), Is.EqualTo("updated definitions"));
                Assert.That(initial, Is.EqualTo(1));
                Assert.That(updates, Is.EqualTo(1));
                stage = "ping";
                Assert.That(await QuestPairing.PingAsync("127.0.0.1", pairing.Pin, credential, stop.Token), Is.True);
                Exception denied = null;
                try { await QuestPairing.UpdateDefinitionsAsync("127.0.0.1", pairing.Pin, new byte[32], request, stop.Token, (_, __) => throw new InvalidOperationException()); }
                catch (Exception error) { denied = error; }
                Assert.That(denied, Is.TypeOf<System.Security.Authentication.AuthenticationException>());
                Assert.That(updates, Is.EqualTo(1));
            }
            catch (Exception error) { Assert.Fail(stage + ": " + error.ToString().Replace("\0", "")); }
            finally { stop.Cancel(); await serving; }
        }

        [Test, Timeout(30000)]
        public async Task AutomaticUpdatesJoinConcurrentSavesAndReplayHistoryAfterReceiverRestart()
        {
            Directory.CreateDirectory(folder);
            var root = new GameObject("automatic definitions integration test");
            root.SetActive(false);
            var persistent = root.AddComponent<PersistentDataManager>();
            var database = root.AddComponent<DatabaseManager>();
            var managerType = Type.GetType("HBP.Quest.Desktop.QuestManager, HBP.UI.Runtime", true);
            var manager = root.AddComponent(managerType);
            var persistentField = typeof(Singleton<PersistentDataManager>).GetField("m_Instance", BindingFlags.Static | BindingFlags.NonPublic);
            var databaseField = typeof(Singleton<DatabaseManager>).GetField("m_Instance", BindingFlags.Static | BindingFlags.NonPublic);
            var priorPersistent = persistentField.GetValue(null);
            var priorDatabase = databaseField.GetValue(null);
            var aliases = new AliasCollection();
            var tags = new TagCollection();
            var protocols = new GlobalDatabase();
            protocols.Settings.SetDefaultWorkspace();
            typeof(GlobalDatabase).GetField("m_PublishedWorkspaceID", Private).SetValue(protocols, protocols.Settings.SelectedWorkspace.ID);
            using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(25));
            using var pairing = new QuestPairing();
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            string host = "127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port;
            var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            int updates = 0;
            long remoteRevision = 0;
            var applied = new List<long>();
            Task serving = null;
            PairingSnapshot initial = null;
            string aliasPath = AliasCollection.PATH;
            try
            {
                persistentField.SetValue(null, persistent);
                databaseField.SetValue(null, database);
                typeof(DatabaseManager).GetField("m_Database", Private).SetValue(database, protocols);
                foreach (var entry in new Dictionary<string, object> { ["m_UserPreferences"] = new UserPreferences(), ["m_Tags"] = tags, ["m_Aliases"] = aliases, ["m_FilterConditionsPresets"] = new FilterConditionsPresetCollection() })
                    typeof(PersistentDataManager).GetField(entry.Key, Private).SetValue(persistent, entry.Value);
                AliasCollection.PATH = Path.Combine(folder, "Aliases.json");
                initial = await PairingSnapshot.CaptureAsync(stop.Token);
                serving = pairing.ServeAsync(listener, stop.Token, (_, __) => throw new InvalidOperationException(), _ => { },
                    (stream, token) => PinnedTlsTransfer.ReceiveFileAsync(stream, Path.Combine(folder, Guid.NewGuid().ToString("N")), token, (_, __, ___) => Task.FromResult(DeliveryStatus.Published)),
                    receiveSessionControl: (request, token) => Task.FromResult(new SessionControlResponse(request.OperationId, SessionControlStatus.Applied, Interlocked.Read(ref remoteRevision).ToString())),
                    receiveDefinitions: (request, stream, token) => PinnedTlsTransfer.ReceiveFileAsync(stream, Path.Combine(folder, Guid.NewGuid().ToString("N")), token, async (file, hash, cancellation) =>
                    {
                        int index = Interlocked.Increment(ref updates);
                        if (index == 1) { entered.TrySetResult(true); await release.Task; }
                        using var archive = new SceneArchive(Path.Combine(folder, Guid.NewGuid().ToString("N")));
                        var received = archive.ReadGlobalData(file, cancellation, inertPreferences: true);
                        Assert.That(received.Id, Is.EqualTo(initial.Context.Id));
                        Assert.That(received.DefinitionsRevision, Is.EqualTo(request.Revision));
                        applied.Add(received.DefinitionsRevision);
                        Interlocked.Exchange(ref remoteRevision, received.DefinitionsRevision);
                        return DeliveryStatus.Published;
                    }));
                byte[] credential = await QuestPairing.PairAsync(host, pairing.Pin, pairing.Code, stop.Token, (stream, token) => initial.Delivery.SendAsync(stream, token));
                foreach (var entry in new Dictionary<string, object> { ["globals"] = initial, ["connected"] = true, ["credential"] = credential, ["pin"] = pairing.Pin, ["endpoint"] = host, ["sessionGeneration"] = 3L })
                    managerType.GetField(entry.Key, Private).SetValue(manager, entry.Value);
                managerType.GetMethod("InitializeDefinitions", Private).Invoke(manager, null);
                aliases.AddAlias(new Alias("[test]", folder), false);
                aliases.Save();
                managerType.GetMethod("TickDefinitions", Private).Invoke(manager, null);
                await entered.Task;
                aliases.Save();
                Task flush = (Task)managerType.GetMethod("FlushDefinitionsAsync", Private).Invoke(manager, new object[] { stop.Token });
                Assert.That(flush.IsCompleted, Is.False);
                release.TrySetResult(true);
                await flush;
                Assert.That(updates, Is.EqualTo(2), "Concurrent callers must join a single transfer for each captured revision.");
                Assert.That(applied, Is.EqualTo(new[] { 1L, 2L }));
                Assert.That(managerType.GetProperty("IsPaired").GetValue(manager), Is.True);
                var latest = (PairingSnapshot)managerType.GetField("globals", Private).GetValue(manager);
                Assert.That(latest.Context.Data.Aliases.Aliases.Single().Key, Is.EqualTo("[test]"));

                // RestoreAsync sends the oldest archive first when Quest lost its in-memory session.
                var restoredRevisions = new List<long>();
                var sendGlobals = managerType.GetMethod("SendGlobalsAsync", Private);
                using var client = new TcpClient();
                var restoreListener = new TcpListener(IPAddress.Loopback, 0);
                restoreListener.Start();
                try
                {
                    var accepted = restoreListener.AcceptTcpClientAsync();
                    await client.ConnectAsync(IPAddress.Loopback, ((IPEndPoint)restoreListener.LocalEndpoint).Port);
                    using var server = await accepted;
                    Task<DeliveryReceipt> received = PinnedTlsTransfer.ReceiveFileAsync(server.GetStream(), Path.Combine(folder, "restore"), stop.Token, (file, hash, token) =>
                    {
                        using var archive = new SceneArchive(Path.Combine(folder, "restore-archive"));
                        restoredRevisions.Add(archive.ReadGlobalData(file, token, inertPreferences: true).DefinitionsRevision);
                        return Task.FromResult(DeliveryStatus.Published);
                    });
                    await (Task<DeliveryReceipt>)sendGlobals.Invoke(manager, new object[] { client.GetStream(), stop.Token });
                    await received;
                }
                finally { restoreListener.Stop(); }
                Assert.That(restoredRevisions, Is.EqualTo(new[] { 0L }));
                Interlocked.Exchange(ref remoteRevision, 0);
                await (Task)managerType.GetMethod("RestoreDefinitionsHistoryAsync", Private).Invoke(manager, new object[] { stop.Token });
                Assert.That(applied, Is.EqualTo(new[] { 1L, 2L, 1L, 2L }));
                Interlocked.Exchange(ref remoteRevision, 1);
                await (Task)managerType.GetMethod("RestoreDefinitionsHistoryAsync", Private).Invoke(manager, new object[] { stop.Token });
                Assert.That(applied, Is.EqualTo(new[] { 1L, 2L, 1L, 2L, 2L }), "A partial replay resumes from the last revision acknowledged by Quest.");
            }
            finally
            {
                release.TrySetResult(true);
                stop.Cancel();
                if (serving != null) await serving; else listener.Stop();
                managerType.GetMethod("DisposeDefinitions", Private).Invoke(manager, null);
                var work = (Task)managerType.GetField("definitionsWork", Private).GetValue(manager);
                try { await work; } catch (Exception) { }
                ((PairingSnapshot)managerType.GetField("globals", Private).GetValue(manager))?.Dispose();
                managerType.GetMethod("ClearDefinitions", Private).Invoke(manager, null);
                initial?.Dispose();
                AliasCollection.PATH = aliasPath;
                persistentField.SetValue(null, priorPersistent);
                databaseField.SetValue(null, priorDatabase);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test, Timeout(30000)]
        public async Task InstallingDefinitionsPreservesPreferencesAndHistoryAndRejectsStaleUpdates()
        {
            var root = new GameObject("definitions receiver test");
            root.SetActive(false);
            var persistent = root.AddComponent<PersistentDataManager>();
            var database = root.AddComponent<DatabaseManager>();
            var session = root.AddComponent<QuestAnatomySession>();
            var persistentField = typeof(Singleton<PersistentDataManager>).GetField("m_Instance", BindingFlags.Static | BindingFlags.NonPublic);
            var databaseField = typeof(Singleton<DatabaseManager>).GetField("m_Instance", BindingFlags.Static | BindingFlags.NonPublic);
            var priorPersistent = persistentField.GetValue(null);
            var priorDatabase = databaseField.GetValue(null);
            using var originalArchive = new SceneArchive(Path.Combine(folder, "original"));
            string originalImage = Path.Combine(originalArchive.DirectoryPath, "old.png");
            File.WriteAllText(originalImage, "old image");
            var preferences = new UserPreferences();
            var filters = new FilterConditionsPresetCollection();
            var original = new PairingContext(new GlobalDataPayload { Preferences = preferences, Tags = new TagCollection(), Protocols = new List<Protocol>(), Aliases = new AliasCollection(), Grid = 32 });
            using var package = new SceneArchive(Path.Combine(folder, "package"));
            var updated = new PairingContext(new GlobalDataPayload
            {
                Id = original.Id, DefinitionsRevision = 1, Preferences = new UserPreferences(), Tags = new TagCollection(new[] { new StringTag { Name = "new tag" } }, Array.Empty<BaseTag>(), Array.Empty<BaseTag>()),
                Protocols = new List<Protocol> { new Protocol("new protocol", Array.Empty<Bloc>()) }, Aliases = new AliasCollection(), Grid = 64
            });
            updated.CaptureFilterPresets(new FilterConditionsPresetCollection(), package);
            string file = Path.Combine(folder, "update.hbglobal");
            package.WriteGlobalData(updated.Data, file);
            try
            {
                persistentField.SetValue(null, persistent);
                databaseField.SetValue(null, database);
                typeof(DatabaseManager).GetField("m_Database", Private).SetValue(database, new GlobalDatabase());
                foreach (var entry in new Dictionary<string, object> { ["m_UserPreferences"] = preferences, ["m_Tags"] = original.Data.Tags, ["m_Aliases"] = original.Data.Aliases, ["m_FilterConditionsPresets"] = filters })
                    typeof(PersistentDataManager).GetField(entry.Key, Private).SetValue(persistent, entry.Value);
                foreach (var entry in new Dictionary<string, object> { ["globals"] = original, ["globalArchive"] = originalArchive, ["controlGeneration"] = 3L, ["unityContext"] = SynchronizationContext.Current })
                    typeof(QuestAnatomySession).GetField(entry.Key, Private).SetValue(session, entry.Value);
                var history = (System.Collections.IDictionary)typeof(QuestAnatomySession).GetField("sceneControls", Private).GetValue(session);
                var operation = Guid.NewGuid();
                history.Add(operation, (new byte[] { 1 }, Task.FromResult(new SessionControlResponse(operation, SessionControlStatus.Applied))));
                var request = new SessionControlRequest(Guid.ParseExact(original.Id, "N"), 3, Guid.NewGuid(), SessionControlKind.Definitions, 1);
                Task<DeliveryStatus> Install(SessionControlRequest value, string hash = "same") => (Task<DeliveryStatus>)typeof(QuestAnatomySession).GetMethod("InstallDefinitionsAsync", Private).Invoke(session, new object[] { value, file, hash, CancellationToken.None });
                Assert.That(await Install(request), Is.EqualTo(DeliveryStatus.Published));
                Assert.That(PersistentDataManager.Tags.AllTags.Single().Name, Is.EqualTo("new tag"));
                Assert.That(DatabaseManager.Database.Protocols.Single().Name, Is.EqualTo("new protocol"));
                Assert.That(PersistentDataManager.UserPreferences, Is.SameAs(preferences));
                Assert.That(PersistentDataManager.FilterConditionsPresets, Is.SameAs(filters));
                Assert.That(history.Count, Is.EqualTo(1));
                Assert.That(File.Exists(originalImage), Is.True, "An earlier scene can still need its protocol images.");
                Assert.That(await Install(request), Is.EqualTo(DeliveryStatus.AlreadyPublished), "A lost ACK must be retryable.");
                var revision = await session.ReceiveSessionControlAsync(new SessionControlRequest(request.ContextId, 3, Guid.NewGuid(), SessionControlKind.DefinitionsRevision), CancellationToken.None);
                Assert.That(revision.Applied, Is.True);
                Assert.That(revision.Message, Is.EqualTo("1"));
                foreach (var stale in new[]
                {
                    new SessionControlRequest(request.ContextId, 2, Guid.NewGuid(), SessionControlKind.Definitions, 2),
                    new SessionControlRequest(Guid.NewGuid(), 3, Guid.NewGuid(), SessionControlKind.Definitions, 2),
                    new SessionControlRequest(request.ContextId, 3, Guid.NewGuid(), SessionControlKind.Definitions, 0)
                })
                {
                    Exception failure = null;
                    try { await Install(stale); } catch (Exception error) { failure = error; }
                    Assert.That(failure, Is.TypeOf<InvalidOperationException>());
                }
                Exception reused = null;
                try { await Install(request, "different"); } catch (Exception error) { reused = error; }
                Assert.That(reused, Is.TypeOf<InvalidDataException>());
            }
            finally
            {
                ((SceneArchive)typeof(QuestAnatomySession).GetField("globalArchive", Private).GetValue(session))?.Dispose();
                typeof(QuestAnatomySession).GetMethod("ReleaseRetiredGlobalArchives", Private).Invoke(session, null);
                persistentField.SetValue(null, priorPersistent);
                databaseField.SetValue(null, priorDatabase);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
