using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HBP.Quest;
using HBP.Sync;
using HBP.Transfer.Transport;
using NUnit.Framework;
using UnityEngine;

namespace HBP.Tests.Transfer.Scene
{
    [Category("Sync.SceneFocused")]
    public class QuestReplicaTelemetryFailureTests
    {
        [TestCase(4, TestName = "QuestReceiver_ReceivedAckWriteFailureRetainsReceiveFacts")]
        [TestCase(7, TestName = "QuestReceiver_RejectionAckWriteFailureRetainsReceiveFacts")]
        public async Task FailedAckRetainsProductionReceiveFacts(int failingWrite)
        {
            // The timeout is a hang guard, not the mechanism establishing test order.
            using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using var incoming = new MemoryStream();
            await ReplicaWire.WriteAsync(incoming, ReplicaFrameKind.Delta, ColorDeltaBody(), stop.Token);
            using var stream = new AckFailureStream(incoming.ToArray(), failingWrite);
            var sink = new BoundedSyncTelemetrySink(16);
            var owner = new GameObject("T00-Receiver-Test");
            try
            {
                var session = owner.AddComponent<QuestAnatomySession>();
                SetField(session, "mainThread", Thread.CurrentThread.ManagedThreadId);
                SynchronizationContext context = SynchronizationContext.Current;
                Assert.That(context, Is.Not.Null, "Run this test in the Unity EditMode runner.");
                SetField(session, "unityContext", context);
                using (SyncTelemetry.BeginCapture(sink))
                {
                    Exception failure = null;
                    try
                    {
                        await session.ReceiveReplicaAsync(stream, stop.Token);
                    }
                    catch (Exception exception)
                    {
                        failure = exception;
                    }

                    Assert.That(failure, Is.TypeOf<IOException>());
                }

                var milestones = Enumerable.Range(0, sink.Count).Select(index =>
                {
                    sink.TryGet(index, out SyncTelemetrySample sample);
                    return sample.Milestone;
                }).ToArray();
                Assert.That(milestones, Is.EqualTo(new[] { SyncMilestone.FirstByteReceived, SyncMilestone.LastByteReceived }));
                Assert.That(stream.WriteCount, Is.EqualTo(failingWrite));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }

        private static byte[] ColorDeltaBody()
        {
            // Current HBD1 codec: one removed site-color field. Application is intentionally never reached.
            using var bytes = new MemoryStream();
            using var writer = new BinaryWriter(bytes, Encoding.UTF8, true);
            writer.Write(0f); // Sender clock before the delta.
            writer.Write(0x31444248);
            writer.Write(1UL);
            writer.Write(2UL);
            writer.Write(0); // Assignments.
            writer.Write(1); // Removals.
            writer.Write((byte)EntityKind.Site);
            WriteText(writer, "column");
            WriteText(writer, "site");
            writer.Write((ushort)5);
            writer.Flush();
            return bytes.ToArray();
        }

        private static void WriteText(BinaryWriter writer, string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            writer.Write((ushort)bytes.Length);
            writer.Write(bytes);
        }

        private sealed class AckFailureStream : MemoryStream
        {
            private readonly int m_FailingWrite;
            public int WriteCount { get; private set; }
            public AckFailureStream(byte[] incoming, int failingWrite) : base(incoming, false) => m_FailingWrite = failingWrite;
            public override bool CanWrite => true;

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                if (++WriteCount == m_FailingWrite) return Task.FromException(new IOException("Deterministic ACK write failure."));
                return Task.CompletedTask; // Independent outgoing side; do not change the incoming position.
            }
        }
    }
}
