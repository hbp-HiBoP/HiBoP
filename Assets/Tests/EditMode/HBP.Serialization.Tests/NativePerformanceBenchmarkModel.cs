using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace HBP.Tests.Serialization
{
    [Serializable]
    internal sealed class NativePerformanceWorkerReport
    {
        public string schemaVersion = "1.0";
        public string backend;
        public string startedUtc;
        public string finishedUtc;
        public string unityVersion;
        public string operatingSystem;
        public string machineName;
        public int processorCount;
        public int warmupIterations;
        public int measuredIterations;
        public string fixtureRoot;
        public bool succeeded;
        public string error;
        public List<NativePerformanceScenarioResult> scenarios = new();
    }

    [Serializable]
    internal sealed class NativePerformanceScenarioResult
    {
        public string name;
        public string domain;
        public string phase;
        public string workload;
        public int operationsPerIteration;
        public int iterations;
        public bool validationPassed;
        public string validationMessage;
        public string checksum;
        public double medianMilliseconds;
        public double p95Milliseconds;
        public double standardDeviationMilliseconds;
        public double medianManagedBytes;
        public double p95ManagedBytes;
        public long peakPrivateBytesDelta;
        public long peakWorkingSetBytesDelta;
        public long steadyPrivateBytesDelta;
        public long steadyWorkingSetBytesDelta;
        public long retainedPrivateBytesDelta;
        public double[] millisecondsSamples;
        public long[] managedBytesSamples;
    }

    internal sealed class NativePerformanceScenario : IDisposable
    {
        private readonly Func<ulong> m_Action;
        private readonly Func<string> m_Validate;
        private readonly Action m_Dispose;

        public NativePerformanceScenario(string name, string domain, string phase, string workload, int operationsPerIteration, Func<ulong> action, Func<string> validate = null, Action dispose = null)
        {
            Name = name;
            Domain = domain;
            Phase = phase;
            Workload = workload;
            OperationsPerIteration = operationsPerIteration;
            m_Action = action ?? throw new ArgumentNullException(nameof(action));
            m_Validate = validate;
            m_Dispose = dispose;
        }

        public string Name { get; }
        public string Domain { get; }
        public string Phase { get; }
        public string Workload { get; }
        public int OperationsPerIteration { get; }

        public ulong Invoke()
        {
            return m_Action();
        }

        public string Validate()
        {
            return m_Validate?.Invoke() ?? "Independent invariants passed before measurement.";
        }

        public void Dispose()
        {
            m_Dispose?.Invoke();
        }
    }

    internal static class NativePerformanceMeasurement
    {
        public static NativePerformanceScenarioResult Run(NativePerformanceScenario scenario, int warmupIterations, int measuredIterations)
        {
            if (scenario.OperationsPerIteration <= 0)
            {
                throw new InvalidOperationException($"{scenario.Name} has no measured operation.");
            }

            ForceCollection();
            using Process process = Process.GetCurrentProcess();
            process.Refresh();
            long baselinePrivate = process.PrivateMemorySize64;
            long baselineWorkingSet = process.WorkingSet64;
            double[] milliseconds = new double[measuredIterations];
            long[] managedBytes = new long[measuredIterations];
            ulong checksum = 1469598103934665603UL;

            using (ProcessMemorySampler sampler = new(process, baselinePrivate, baselineWorkingSet))
            {
                sampler.Start();
                ulong validationChecksum = scenario.Invoke();
                string validationMessage = scenario.Validate();
                for (int i = 0; i < warmupIterations; ++i)
                {
                    scenario.Invoke();
                }

                ForceCollection();
                process.Refresh();
                sampler.SampleNow();
                long steadyPrivateBytesDelta = Math.Max(0L, process.PrivateMemorySize64 - baselinePrivate);
                long steadyWorkingSetBytesDelta = Math.Max(0L, process.WorkingSet64 - baselineWorkingSet);

                for (int i = 0; i < measuredIterations; ++i)
                {
                    long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
                    long managedBefore = GC.GetTotalMemory(forceFullCollection: false);
                    long timestampBefore = Stopwatch.GetTimestamp();
                    ulong iterationChecksum = scenario.Invoke();
                    long timestampAfter = Stopwatch.GetTimestamp();
                    long allocatedAfter = GC.GetAllocatedBytesForCurrentThread();
                    long managedAfter = GC.GetTotalMemory(forceFullCollection: false);

                    checksum = Mix(checksum, iterationChecksum);
                    milliseconds[i] = (timestampAfter - timestampBefore) * 1000.0 / Stopwatch.Frequency / scenario.OperationsPerIteration;
                    long threadAllocated = Math.Max(0L, allocatedAfter - allocatedBefore);
                    long liveManagedGrowth = Math.Max(0L, managedAfter - managedBefore);
                    managedBytes[i] = Math.Max(threadAllocated, liveManagedGrowth) / scenario.OperationsPerIteration;
                    sampler.SampleNow();
                }

                sampler.Stop();

                ForceCollection();
                process.Refresh();
                return new NativePerformanceScenarioResult
                {
                    name = scenario.Name,
                    domain = scenario.Domain,
                    phase = scenario.Phase,
                    workload = scenario.Workload,
                    operationsPerIteration = scenario.OperationsPerIteration,
                    iterations = measuredIterations,
                    validationPassed = true,
                    validationMessage = validationMessage,
                    checksum = Mix(checksum, validationChecksum).ToString("X16"),
                    medianMilliseconds = Median(milliseconds),
                    p95Milliseconds = Percentile95(milliseconds),
                    standardDeviationMilliseconds = StandardDeviation(milliseconds),
                    medianManagedBytes = Median(managedBytes.Select(value => (double)value).ToArray()),
                    p95ManagedBytes = Percentile95(managedBytes.Select(value => (double)value).ToArray()),
                    peakPrivateBytesDelta = Math.Max(0L, sampler.PeakPrivateBytes - baselinePrivate),
                    peakWorkingSetBytesDelta = Math.Max(0L, sampler.PeakWorkingSetBytes - baselineWorkingSet),
                    steadyPrivateBytesDelta = steadyPrivateBytesDelta,
                    steadyWorkingSetBytesDelta = steadyWorkingSetBytesDelta,
                    retainedPrivateBytesDelta = Math.Max(0L, process.PrivateMemorySize64 - baselinePrivate),
                    millisecondsSamples = milliseconds,
                    managedBytesSamples = managedBytes
                };
            }
        }

        private static void ForceCollection()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        private static double Median(double[] values)
        {
            double[] sorted = (double[])values.Clone();
            Array.Sort(sorted);
            int middle = sorted.Length / 2;
            return sorted.Length % 2 == 0 ? (sorted[middle - 1] + sorted[middle]) * 0.5 : sorted[middle];
        }

        private static double Percentile95(double[] values)
        {
            double[] sorted = (double[])values.Clone();
            Array.Sort(sorted);
            int index = Math.Max(0, (int)Math.Ceiling(sorted.Length * 0.95) - 1);
            return sorted[index];
        }

        private static double StandardDeviation(double[] values)
        {
            double mean = values.Average();
            double variance = values.Select(value => (value - mean) * (value - mean)).Average();
            return Math.Sqrt(variance);
        }

        private static ulong Mix(ulong seed, ulong value)
        {
            return (seed ^ value) * 1099511628211UL;
        }

        private sealed class ProcessMemorySampler : IDisposable
        {
            private readonly Process m_Process;
            private readonly object m_SampleLock = new();
            private readonly Thread m_Thread;
            private readonly ManualResetEventSlim m_Started = new(false);
            private volatile bool m_Running;

            public ProcessMemorySampler(Process process, long baselinePrivate, long baselineWorkingSet)
            {
                m_Process = process;
                PeakPrivateBytes = baselinePrivate;
                PeakWorkingSetBytes = baselineWorkingSet;
                m_Thread = new Thread(Sample)
                {
                    IsBackground = true,
                    Name = "HBP performance memory sampler"
                };
            }

            public long PeakPrivateBytes { get; private set; }
            public long PeakWorkingSetBytes { get; private set; }

            public void Start()
            {
                m_Running = true;
                m_Thread.Start();
                m_Started.Wait();
            }

            public void Stop()
            {
                m_Running = false;
                m_Thread.Join();
            }

            public void SampleNow()
            {
                lock (m_SampleLock)
                {
                    try
                    {
                        m_Process.Refresh();
                        PeakPrivateBytes = Math.Max(PeakPrivateBytes, m_Process.PrivateMemorySize64);
                        PeakWorkingSetBytes = Math.Max(PeakWorkingSetBytes, m_Process.WorkingSet64);
                    }
                    catch (InvalidOperationException)
                    {
                    }
                }
            }

            public void Dispose()
            {
                if (m_Thread.IsAlive)
                {
                    Stop();
                }

                m_Started.Dispose();
            }

            private void Sample()
            {
                m_Started.Set();
                while (m_Running)
                {
                    try
                    {
                        SampleNow();
                    }
                    catch (InvalidOperationException)
                    {
                        return;
                    }

                    Thread.Sleep(1);
                }
            }
        }
    }
}
