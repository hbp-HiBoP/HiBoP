using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HBP.Core.DLL;
using HBP.Core.DLL.HbpCore;
using NUnit.Framework;

namespace HBP.Tests.Serialization
{
    public class NativeLogCallbackFunctionalTests
    {
        [Test]
        [Category("NativeMigration")]
        [Category("NativeDll")]
        [Category("HbpCoreOnly")]
        public void HbpCoreDebugCallback_PreservesExactUtf8ContentAndLogTypes()
        {
            ConcurrentQueue<(string Message, int Type)> received = new();
            DLLDebugManager.LoggerDelegate callback = (message, type) => received.Enqueue((message, type));
            RequireCallback(callback);

            (string Message, HbpCoreLogType Type)[] expected =
            {
                ("info exacte : électrode A'1", HbpCoreLogType.Info),
                ("warning exact\nsecond line", HbpCoreLogType.Warning),
                ("error exact Ω", HbpCoreLogType.Error)
            };

            try
            {
                foreach ((string message, HbpCoreLogType type) in expected)
                {
                    Assert.That(HbpCoreRuntime.DebugMessage(message, type), Is.EqualTo(HbpCoreStatus.Ok));
                }
            }
            finally
            {
                HbpCoreRuntime.TryResetDebugCallback(out _);
            }

            Assert.That(
                received.ToArray(),
                Is.EqualTo(expected.Select(item => (item.Message, (int)item.Type)).ToArray()));
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeDll")]
        [Category("HbpCoreOnly")]
        public async Task HbpCoreDebugCallback_IsLosslessAcrossConcurrentNativeCalls()
        {
            const int workerCount = 6;
            const int messagesPerWorker = 20;
            ConcurrentQueue<(string Message, int Type, int ThreadId)> received = new();
            DLLDebugManager.LoggerDelegate callback = (message, type) =>
                received.Enqueue((message, type, Thread.CurrentThread.ManagedThreadId));
            RequireCallback(callback);

            try
            {
                Task[] workers = Enumerable.Range(0, workerCount)
                    .Select(worker => Task.Factory.StartNew(() =>
                    {
                        for (int index = 0; index < messagesPerWorker; ++index)
                        {
                            string message = $"worker={worker:D2};message={index:D2}";
                            HbpCoreLogType type = (HbpCoreLogType)(index % 3);
                            HbpCoreStatus status = HbpCoreRuntime.DebugMessage(message, type);
                            if (status != HbpCoreStatus.Ok)
                            {
                                throw new InvalidOperationException($"Native log call failed with {status}: {HbpCoreRuntime.LastError}");
                            }
                        }
                    }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default))
                    .ToArray();

                await Task.WhenAll(workers);
            }
            finally
            {
                HbpCoreRuntime.TryResetDebugCallback(out _);
            }

            (string Message, int Type, int ThreadId)[] actual = received.ToArray();
            Assert.That(actual, Has.Length.EqualTo(workerCount * messagesPerWorker));
            Assert.That(actual.Select(item => item.Message).Distinct().Count(), Is.EqualTo(actual.Length));
            Assert.That(actual.Select(item => item.ThreadId).Distinct().Count(), Is.GreaterThan(1));

            foreach ((string message, int type, _) in actual)
            {
                string indexText = message.Substring(message.Length - 2);
                Assert.That(type, Is.EqualTo(int.Parse(indexText) % 3), message);
            }
        }

        private static void RequireCallback(DLLDebugManager.LoggerDelegate callback)
        {
            if (!HbpCoreRuntime.TrySetDebugCallback(callback, out string error))
            {
                Assert.Ignore($"hbp_core debug callback is unavailable: {error}");
            }
        }
    }
}
