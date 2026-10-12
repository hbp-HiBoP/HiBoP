#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Preferences;
using HBP.Quest;
using HBP.Sync;
using HBP.Transfer.Scene;
using HBP.Transfer.Transport;
using NUnit.Framework;
using UnityEngine;

namespace HBP.Tests.Quest
{
    public sealed class QuestSceneLifecycleTests
    {
        [Test]
        public async Task ClosingPreparingSceneWaitsForCleanupWithoutBlockingAnotherSceneAndRejectsStaleOperations()
        {
            var root = new GameObject("lifecycle fixture");
            root.SetActive(false);
            var view = root.AddComponent<QuestAnatomyView>();
            var session = root.AddComponent<QuestAnatomySession>();
            var context = new PairingContext(new GlobalDataPayload
            {
                Preferences = new UserPreferences(), Tags = new TagCollection(Array.Empty<BaseTag>(), Array.Empty<BaseTag>(), Array.Empty<BaseTag>()),
                Protocols = new List<Protocol>(), Aliases = new AliasCollection(), Grid = 32
            });
            var preparation = new TaskCompletionSource<bool>();
            try
            {
                Set(session, "view", view);
                Set(session, "globals", context);
                Set(session, "controlGeneration", 7L);
                Set(session, "mainThread", Thread.CurrentThread.ManagedThreadId);
                Set(session, "unityContext", SynchronizationContext.Current);
                var entries = (IDictionary)Get(session, "deliveries");
                var type = entries.GetType().GetGenericArguments()[1];
                var entry = Activator.CreateInstance(type, true);
                string a = Guid.NewGuid().ToString("N"), b = Guid.NewGuid().ToString("N");
                type.GetField("TransferId").SetValue(entry, a);
                type.GetField("VisualizationId").SetValue(entry, "scene-A");
                type.GetField("Preparation").SetValue(entry, preparation);
                entries.Add(a, entry);
                var closeA = Request(context, a, "scene-A");
                var pendingA = session.ReceiveSessionControlAsync(closeA, CancellationToken.None);
                Assert.That(pendingA.IsCompleted, Is.False, "Closing must await native preparation cleanup.");
                var secondCloseA = session.ReceiveSessionControlAsync(Request(context, a, "scene-A"), CancellationToken.None);
                Assert.That(secondCloseA.IsCompleted, Is.False, "Concurrent closes share the same cleanup barrier.");
                Assert.That(((CancellationTokenSource)type.GetField("Lifetime").GetValue(entry)).IsCancellationRequested, Is.True);
                var closeB = Request(context, b, "scene-B");
                Assert.That((await session.ReceiveSessionControlAsync(closeB, CancellationToken.None)).Applied, Is.True, "A different incarnation must remain independent.");
                preparation.SetResult(true);
                Assert.That((await pendingA).Applied, Is.True);
                Assert.That((await secondCloseA).Applied, Is.True);
                Assert.That((await session.ReceiveSessionControlAsync(closeA, CancellationToken.None)).Applied, Is.True, "Lost acknowledgement retries are idempotent.");
                var reused = Request(context, b, "scene-B", operation: closeA.OperationId);
                Assert.That((await session.ReceiveSessionControlAsync(reused, CancellationToken.None)).Status, Is.EqualTo(SessionControlStatus.Rejected));
                Assert.That((await session.ReceiveSessionControlAsync(Request(context, b, "scene-B", generation: 6), CancellationToken.None)).Status, Is.EqualTo(SessionControlStatus.Stale));
                Assert.That(type.GetField("Status").GetValue(entry), Is.EqualTo(DeliveryStatus.Closed), "Keep the tombstone to reject delayed publication.");
                Assert.That((bool)type.GetField("Shared").GetValue(entry), Is.False);
                var publish = typeof(QuestAnatomySession).GetMethod("PublishAsync", BindingFlags.Instance | BindingFlags.NonPublic);
                var delayed = (Task<DeliveryStatus>)publish.Invoke(session, new object[] { new ScenePayload { TransferId = a }, null, new string('0', 64), CancellationToken.None });
                Assert.That(await delayed, Is.EqualTo(DeliveryStatus.Closed), "A delayed full delivery cannot resurrect a closed incarnation.");
                using var routed = new MemoryStream();
                await V2ReplicaRoute.WriteAsync(routed, new SessionId(Guid.ParseExact(context.Id, "N")), new SceneId(Guid.NewGuid()), new IncarnationId(Guid.ParseExact(a, "N")), CancellationToken.None);
                routed.Position = 0;
                Exception rejected = null;
                try
                {
                    await session.ReceiveReplicaAsync(routed, CancellationToken.None);
                }
                catch (Exception error)
                {
                    rejected = error;
                }

                Assert.That(rejected, Is.TypeOf<InvalidDataException>(), "The production router rejects a delayed frame before entering any scene protocol.");

                string c = Guid.NewGuid().ToString("N");
                var reconciling = Activator.CreateInstance(type, true);
                type.GetField("TransferId").SetValue(reconciling, c);
                type.GetField("VisualizationId").SetValue(reconciling, "scene-C");
                type.GetField("Status").SetValue(reconciling, DeliveryStatus.Published);
                entries.Add(c, reconciling);
                var gate = (SemaphoreSlim)type.GetField("Gate").GetValue(reconciling);
                await gate.WaitAsync();
                Task<SessionControlResponse> detached;
                try
                {
                    detached = session.ReceiveSessionControlAsync(Request(context, c, "scene-C", kind: SessionControlKind.DetachScene), CancellationToken.None);
                    Assert.That(detached.IsCompleted, Is.False, "Detaching waits for the scene's reconciliation cleanup.");
                    Assert.That(((CancellationTokenSource)type.GetField("Lifetime").GetValue(reconciling)).IsCancellationRequested, Is.True);
                    Assert.That((await session.ReceiveSessionControlAsync(Request(context, b, "scene-B"), CancellationToken.None)).Applied, Is.True);
                }
                finally
                {
                    gate.Release();
                }

                Assert.That((await detached).Applied, Is.True);
                Assert.That(type.GetField("Status").GetValue(reconciling), Is.EqualTo(DeliveryStatus.Published), "Detaching preserves the local publication.");
            }
            finally
            {
                preparation.TrySetResult(true);
                Set(session, "globals", null);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static SessionControlRequest Request(PairingContext context, string transfer, string scene, long generation = 7, Guid? operation = null, SessionControlKind kind = SessionControlKind.CloseScene) => new(Guid.ParseExact(context.Id, "N"), generation, operation ?? Guid.NewGuid(), kind, body: SceneSessionState.Encode(new SceneSessionClose { TransferId = transfer, VisualizationId = scene, FinalSequence = 19, BulkThrough = 3 }));
        private static object Get(object target, string field) => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        private static void Set(object target, string field, object value) => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
#endif
