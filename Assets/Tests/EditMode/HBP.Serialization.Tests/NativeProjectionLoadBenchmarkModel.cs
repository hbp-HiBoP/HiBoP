using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace HBP.Tests.Serialization
{
    [Serializable]
    internal sealed class NativeProjectionLoadWorkerReport
    {
        public string schemaVersion = "1.3";
        public string backend = "hbp_core";
        public string profile;
        public string startedUtc;
        public string finishedUtc;
        public string unityVersion;
        public string operatingSystem;
        public string machineName;
        public int processorCount;
        public int memorySamplingIntervalMilliseconds;
        public int timelineLength;
        public int repetitions;
        public int requestedParallelWorkerCount;
        public int requestedNeighborBatchSize;
        public string requestedVolumeInterpolation;
        public string surfacePath;
        public string volumePath;
        public bool includeExport;
        public bool succeeded;
        public string error;
        public List<NativeProjectionLoadScenarioResult> scenarios = new();
    }

    [Serializable]
    internal sealed class NativeProjectionLoadScenarioResult
    {
        public string name;
        public int dimension;
        public int siteCount;
        public int timelineLength;
        public float influenceDistance;
        public float[] influenceDistances;
        public int columnCount;
        public bool exportMeasured;
        public string workload;
        public string volumeInterpolation;
        public int cutTexturePixelCount;
        public long estimatedCutStencilPayloadBytes;
        public int surfaceVertexCount;
        public long generatedPointCount;
        public long activeSiteCount;
        public long neighborLinkCount;
        public long storedValueCount;
        public long storedWeightCount;
        public long spatialIndexCacheHitCount;
        public long spatialIndexCacheMissCount;
        public long maxSpatialIndexCacheEntryCount;
        public long maxSpatialIndexCacheBytes;
        public long spatialIndexGeometryVersion;
        public long parallelWorkerCount;
        public long neighborBatchSize;
        public long neighborBatchCount;
        public long maxTemporaryNeighborPeakBytes;
        public long temporaryNeighborBudgetBytes;
        public long estimatedCurrentValueAndWeightBytes;
        public double medianTotalWallMilliseconds;
        public double medianTotalCpuMilliseconds;
        public double medianComputeWallMilliseconds;
        public double medianComputeCpuMilliseconds;
        public double medianCutPreparationWallMilliseconds;
        public double medianCutTimelineUpdateWallMilliseconds;
        public double medianCutTimelineUpdateCpuMilliseconds;
        public double medianCutTimelineFillWallMilliseconds;
        public double medianCutTimelineCopyWallMilliseconds;
        public long maxPeakPrivateBytesDelta;
        public long maxPeakWorkingSetBytesDelta;
        public long maxRetainedPrivateBytesDelta;
        public long maxRetainedWorkingSetBytesDelta;
        public List<NativeProjectionLoadSampleResult> samples = new();
    }

    [Serializable]
    internal sealed class NativeProjectionLoadSampleResult
    {
        public int repetition;
        public double totalWallMilliseconds;
        public double totalCpuMilliseconds;
        public double generatorSurfaceWallMilliseconds;
        public double generatorSurfaceCpuMilliseconds;
        public double computeWallMilliseconds;
        public double computeCpuMilliseconds;
        public double displayUpdateWallMilliseconds;
        public double displayUpdateCpuMilliseconds;
        public double cutPreparationWallMilliseconds;
        public double cutPreparationCpuMilliseconds;
        public double cutTimelineUpdatesWallMilliseconds;
        public double cutTimelineUpdatesCpuMilliseconds;
        public double meanCutTimelineUpdateWallMilliseconds;
        public double meanCutTimelineUpdateCpuMilliseconds;
        public double meanCutTimelineFillWallMilliseconds;
        public double meanCutTimelineCopyWallMilliseconds;
        public int cutTimelineUpdateCount;
        public double exportWallMilliseconds;
        public double exportCpuMilliseconds;
        public double nativeTotalMilliseconds;
        public double allocationMilliseconds;
        public double spatialIndexMilliseconds;
        public double spatialIndexBuildMilliseconds;
        public double spatialIndexLookupMilliseconds;
        public double neighborQueryMilliseconds;
        public double accumulationMilliseconds;
        public double normalizationMilliseconds;
        public double nativeUnattributedMilliseconds;
        public double nativePhaseCoverage;
        public long generatedPointCount;
        public long activeSiteCount;
        public long neighborLinkCount;
        public long storedValueCount;
        public long storedWeightCount;
        public long spatialIndexCacheHitCount;
        public long spatialIndexCacheMissCount;
        public long maxSpatialIndexCacheEntryCount;
        public long maxSpatialIndexCacheBytes;
        public long spatialIndexGeometryVersion;
        public long parallelWorkerCount;
        public long neighborBatchSize;
        public long neighborBatchCount;
        public long temporaryNeighborPeakBytes;
        public long temporaryNeighborBudgetBytes;
        public long baselinePrivateBytes;
        public long baselineWorkingSetBytes;
        public long peakPrivateBytesDelta;
        public long peakWorkingSetBytesDelta;
        public long steadyPrivateBytesDelta;
        public long steadyWorkingSetBytesDelta;
        public long retainedPrivateBytesDelta;
        public long retainedWorkingSetBytesDelta;
        public long managedActivityInputBytes;
        public long estimatedCurrentValueAndWeightBytes;
        public int cutTexturePixelCount;
        public long estimatedCutStencilPayloadBytes;
        public long exportFileBytes;
        public long cacheFileBytes;
        public string cacheBackend = "none";
        public string checksum;
        public bool validationPassed;
        public string validationMessage;
    }

    internal sealed class NativeProjectionLoadScenarioDefinition
    {
        public NativeProjectionLoadScenarioDefinition(
            string name,
            int dimension,
            int siteCount,
            int timelineLength,
            float influenceDistance,
            int columnCount,
            bool measureExport,
            HBP.Core.Enums.VolumeInterpolation volumeInterpolation,
            float[] influenceDistances = null)
        {
            Name = name;
            Dimension = dimension;
            SiteCount = siteCount;
            TimelineLength = timelineLength;
            InfluenceDistance = influenceDistance;
            ColumnCount = columnCount;
            MeasureExport = measureExport;
            VolumeInterpolation = volumeInterpolation;
            InfluenceDistances = influenceDistances ?? Enumerable.Repeat(influenceDistance, columnCount).ToArray();
            if (InfluenceDistances.Length != columnCount)
            {
                throw new ArgumentException("One influence distance is required per sequential column.");
            }
        }

        public string Name { get; }
        public int Dimension { get; }
        public int SiteCount { get; }
        public int TimelineLength { get; }
        public float InfluenceDistance { get; }
        public float[] InfluenceDistances { get; }
        public int ColumnCount { get; }
        public bool MeasureExport { get; }
        public HBP.Core.Enums.VolumeInterpolation VolumeInterpolation { get; }

        public string Workload =>
            $"MNI; dimension {Dimension}; {SiteCount:N0} sites; {TimelineLength} instants; " +
            $"linear radius/radii {string.Join(",", InfluenceDistances.Select(value => value.ToString("R")))}; " +
            $"{ColumnCount} sequential column(s); {VolumeInterpolation}";
    }

    internal sealed class NativeProjectionProcessMemorySampler : IDisposable
    {
        public const int SampleIntervalMilliseconds = 10;

        private readonly Process m_Process;
        private readonly object m_SampleLock = new();
        private readonly Thread m_Thread;
        private readonly ManualResetEventSlim m_Started = new(false);
        private volatile bool m_Running;

        public NativeProjectionProcessMemorySampler(Process process, long baselinePrivate, long baselineWorkingSet)
        {
            m_Process = process;
            PeakPrivateBytes = baselinePrivate;
            PeakWorkingSetBytes = baselineWorkingSet;
            m_Thread = new Thread(Sample)
            {
                IsBackground = true,
                Name = "HBP projection-load memory sampler"
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
            if (m_Thread.IsAlive)
            {
                m_Thread.Join();
            }
        }

        public void SampleNow()
        {
            lock (m_SampleLock)
            {
                try
                {
                    NativeProjectionProcessMemorySnapshot snapshot = NativeProjectionProcessMemory.Read(m_Process);
                    PeakPrivateBytes = Math.Max(PeakPrivateBytes, snapshot.PrivateBytes);
                    PeakWorkingSetBytes = Math.Max(PeakWorkingSetBytes, snapshot.WorkingSetBytes);
                }
                catch (InvalidOperationException)
                {
                }
            }
        }

        public void Dispose()
        {
            Stop();
            m_Started.Dispose();
        }

        private void Sample()
        {
            m_Started.Set();
            while (m_Running)
            {
                SampleNow();
                Thread.Sleep(SampleIntervalMilliseconds);
            }
        }
    }

    internal readonly struct NativeProjectionProcessMemorySnapshot
    {
        public NativeProjectionProcessMemorySnapshot(long privateBytes, long workingSetBytes)
        {
            PrivateBytes = privateBytes;
            WorkingSetBytes = workingSetBytes;
        }

        public long PrivateBytes { get; }
        public long WorkingSetBytes { get; }
    }

    internal static class NativeProjectionProcessMemory
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct ProcessMemoryCountersEx
        {
            public uint cb;
            public uint pageFaultCount;
            public UIntPtr peakWorkingSetSize;
            public UIntPtr workingSetSize;
            public UIntPtr quotaPeakPagedPoolUsage;
            public UIntPtr quotaPagedPoolUsage;
            public UIntPtr quotaPeakNonPagedPoolUsage;
            public UIntPtr quotaNonPagedPoolUsage;
            public UIntPtr pagefileUsage;
            public UIntPtr peakPagefileUsage;
            public UIntPtr privateUsage;
        }

        [DllImport("psapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetProcessMemoryInfo(
            IntPtr process,
            ref ProcessMemoryCountersEx counters,
            uint size);

        public static NativeProjectionProcessMemorySnapshot Read(Process process)
        {
            ProcessMemoryCountersEx counters = new();
            counters.cb = checked((uint)Marshal.SizeOf<ProcessMemoryCountersEx>());
            if (!GetProcessMemoryInfo(process.Handle, ref counters, counters.cb))
            {
                throw new InvalidOperationException(
                    $"GetProcessMemoryInfo failed with Windows error {Marshal.GetLastWin32Error()}.");
            }
            return new NativeProjectionProcessMemorySnapshot(
                checked((long)counters.privateUsage.ToUInt64()),
                checked((long)counters.workingSetSize.ToUInt64()));
        }
    }
}
