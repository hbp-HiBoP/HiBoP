using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.Protocol;
using CRNL.HiBoP.RenderModel;
using CRNL.HiBoP.XR.Timeline.Rendering;
using UnityEngine;
using UnityEngine.Profiling;

namespace CRNL.HiBoP.XR.Timeline.Validation
{
    public sealed class P11TimelineDeviceProbe : MonoBehaviour
    {
        private const int IndexCount = 97;
        private const int MinimumRandomSelections = 64;
        private const double WorstCaseRandomSeconds = 60d;
        private const double WorstCaseAutoplaySeconds = 600d;
        private const int SurfaceVertices = 69_104;
        private const int OverlaySize = 64;
        private const long ProbeGpuBudgetBytes = 1_500_000_000L;
        private const long ProbeUniquePayloadBudgetBytes = ProbeGpuBudgetBytes;

        [SerializeField] private TextMesh statusText;

        private string m_Failure;

        public bool HasStatusText => statusText != null;

        public void Configure(TextMesh configuredStatusText)
        {
            statusText = configuredStatusText;
        }

        private IEnumerator Start()
        {
            SetStatus("HiBoP XR — D20 preload\nRUNNING...", Color.yellow);
            var phases = new List<PhaseProfile>();
            foreach (ProfileSpec spec in new[]
                     {
                         new ProfileSpec("D2", 1, 150),
                         new ProfileSpec("D2", 3, 150),
                         new ProfileSpec("D3", 8, 37_500),
                     })
            {
                PhaseProfile phase = null;
                yield return Measure(spec, value => phase = value);
                if (phase != null)
                    phases.Add(phase);
                if (!string.IsNullOrEmpty(m_Failure))
                    break;
                yield return null;
                GC.Collect();
            }

            Profile profile = new()
            {
                schema = "d20-timeline-preload-quest-profile-v3",
                unity = Application.unityVersion,
                platform = Application.platform.ToString(),
                deviceModel = SystemInfo.deviceModel,
                operatingSystem = SystemInfo.operatingSystem,
                graphicsDevice = SystemInfo.graphicsDeviceName,
                graphicsApi = SystemInfo.graphicsDeviceType.ToString(),
                indexCount = IndexCount,
                minimumRandomSelections = MinimumRandomSelections,
                worstCaseRandomSeconds = WorstCaseRandomSeconds,
                worstCaseAutoplaySeconds = WorstCaseAutoplaySeconds,
                explicitProbeGpuBudgetBytes = ProbeGpuBudgetBytes,
                explicitProbeUniquePayloadBudgetBytes = ProbeUniquePayloadBudgetBytes,
                result = string.IsNullOrEmpty(m_Failure) ? "PASS" : "FAIL",
                failure = m_Failure ?? string.Empty,
                phases = phases.ToArray(),
                totalAllocatedMemoryBytes = Profiler.GetTotalAllocatedMemoryLong(),
                totalReservedMemoryBytes = Profiler.GetTotalReservedMemoryLong(),
            };
            string json = JsonUtility.ToJson(profile, true);
            File.WriteAllText(Path.Combine(Application.persistentDataPath, "d20-timeline-profile.json"), json);
            SetStatus(profile.result == "PASS" ? "HiBoP XR — D20 preload\nPROFILE COMPLETE" : "HiBoP XR — D20 preload\nFAIL: " + profile.failure, profile.result == "PASS" ? Color.green : Color.red);
            UnityEngine.Debug.Log("D20_TIMELINE_PROFILE_COMPLETE " + JsonUtility.ToJson(profile));
        }

        private IEnumerator Measure(ProfileSpec spec, Action<PhaseProfile> completed)
        {
            string archivePath = Path.Combine(Application.persistentDataPath, $"d20-preload-{spec.Columns}-{spec.Sites}.bin");
            SetStatus($"HiBoP XR — D20 preload\n{spec.Columns} col. / {spec.Sites} sites", Color.yellow);
            Task<ArchivePreparation> archiveTask = Task.Run(() => PrepareArchive(spec, archivePath));
            while (!archiveTask.IsCompleted)
                yield return null;
            if (archiveTask.IsFaulted)
            {
                m_Failure = "ARCHIVE_" + (archiveTask.Exception?.GetBaseException().GetType().Name ?? "UNKNOWN");
                yield break;
            }

            ArchivePreparation archive = archiveTask.Result;
            bool worstCase = spec.Columns == 8 && spec.Sites == 37_500;
            var submit = new List<double>(worstCase ? 5_000 : MinimumRandomSelections);
            var frameVisible = new List<double>(worstCase ? 5_000 : MinimumRandomSelections);
            var autoplaySubmit = new List<double>(worstCase ? 45_000 : 0);
            var autoplayFrameVisible = new List<double>(worstCase ? 45_000 : 0);
            var watch = new Stopwatch();
            PreloadedTimelineGpuController controller = null;
            try
            {
                controller = new PreloadedTimelineGpuController(archive.Timeline.Session, archive.Timeline.TimelineId, ProbeGpuBudgetBytes);
                watch.Restart();
                PreloadedTimelineApplyResult prepareResult = controller.TryPrepareAndCommit(archive.Timeline, 0, new ScopeRevision(1), 1, out Exception prepareError);
                double prepareUploadMs = watch.Elapsed.TotalMilliseconds;
                if (prepareResult != PreloadedTimelineApplyResult.Ready || prepareError != null)
                {
                    m_Failure = "PREPARE_" + (prepareError?.GetType().Name ?? prepareResult.ToString());
                    yield break;
                }

                int randomMaximumFrameDelta = 0;
                int iteration = 0;
                ulong nextSequence = 2;
                double randomStart = Time.realtimeSinceStartupAsDouble;
                double randomDeadline = randomStart + (worstCase ? WorstCaseRandomSeconds : 0d);
                do
                {
                    int targetIndex = iteration == 0 ? 96 : iteration == 1 ? 2 : (iteration * 37 + 11) % IndexCount;
                    double start = Time.realtimeSinceStartupAsDouble;
                    int startFrame = Time.frameCount;
                    watch.Restart();
                    PreloadedTimelineApplyResult selectionResult = controller.TrySelect(targetIndex, new ScopeRevision(nextSequence), nextSequence, out Exception selectionError);
                    nextSequence++;
                    submit.Add(watch.Elapsed.TotalMilliseconds);
                    if (selectionResult != PreloadedTimelineApplyResult.Selected || selectionError != null || !controller.TryRead(out PreloadedTimelineSelection<PreloadedTimelineGpuResources> current) || current.Index != targetIndex || current.Prepared.SelectedIndex != targetIndex)
                    {
                        m_Failure = "SELECT_" + (selectionError?.GetType().Name ?? selectionResult.ToString());
                        yield break;
                    }

                    yield return new WaitForEndOfFrame();
                    frameVisible.Add((Time.realtimeSinceStartupAsDouble - start) * 1000d);
                    randomMaximumFrameDelta = Math.Max(randomMaximumFrameDelta, Time.frameCount - startFrame);
                    iteration++;
                } while (iteration < MinimumRandomSelections || Time.realtimeSinceStartupAsDouble < randomDeadline);

                double randomDurationSeconds = Time.realtimeSinceStartupAsDouble - randomStart;

                int autoplayMaximumFrameDelta = 0;
                double autoplayStart = Time.realtimeSinceStartupAsDouble;
                if (worstCase)
                {
                    SetStatus("HiBoP XR — D20 preload\n8 col. autoplay 10 min", Color.yellow);
                    double autoplayDeadline = autoplayStart + WorstCaseAutoplaySeconds;
                    int autoplayIteration = 0;
                    while (Time.realtimeSinceStartupAsDouble < autoplayDeadline)
                    {
                        int targetIndex = autoplayIteration % IndexCount;
                        double start = Time.realtimeSinceStartupAsDouble;
                        int startFrame = Time.frameCount;
                        watch.Restart();
                        PreloadedTimelineApplyResult selectionResult = controller.TrySelect(targetIndex, new ScopeRevision(nextSequence), nextSequence, out Exception selectionError);
                        nextSequence++;
                        autoplaySubmit.Add(watch.Elapsed.TotalMilliseconds);
                        if (selectionResult != PreloadedTimelineApplyResult.Selected || selectionError != null || !controller.TryRead(out PreloadedTimelineSelection<PreloadedTimelineGpuResources> current) || current.Index != targetIndex || current.Prepared.SelectedIndex != targetIndex)
                        {
                            m_Failure = "AUTOPLAY_" + (selectionError?.GetType().Name ?? selectionResult.ToString());
                            yield break;
                        }

                        yield return new WaitForEndOfFrame();
                        autoplayFrameVisible.Add((Time.realtimeSinceStartupAsDouble - start) * 1000d);
                        autoplayMaximumFrameDelta = Math.Max(autoplayMaximumFrameDelta, Time.frameCount - startFrame);
                        autoplayIteration++;
                    }
                }

                PreloadedTimelineApplyResult staleResult = controller.TrySelect(80, new ScopeRevision(nextSequence + 1), nextSequence - 1, out Exception staleError);
                if (staleResult != PreloadedTimelineApplyResult.Stale || staleError != null)
                {
                    m_Failure = "STALE_SELECTION_NOT_REJECTED";
                    yield break;
                }

                completed(new PhaseProfile
                {
                    dataset = spec.Dataset,
                    columns = spec.Columns,
                    sitesPerColumn = spec.Sites,
                    indexCount = IndexCount,
                    archiveBytes = archive.Descriptor.ByteLength,
                    naivePayloadBytes = archive.NaivePayloadBytes,
                    uniqueCpuPayloadBytes = archive.UniquePayloadBytes,
                    requiredGpuBytes = PreloadedTimelineGpuResources.EstimateRequiredBytes(archive.Timeline),
                    buildMs = archive.BuildMilliseconds,
                    archiveWriteAndHashMs = archive.WriteMilliseconds,
                    archiveReadAndHashMs = archive.ReadMilliseconds,
                    prepareUploadMs = prepareUploadMs,
                    randomSelectionDurationSeconds = randomDurationSeconds,
                    randomSelectionMaximumFrameDelta = randomMaximumFrameDelta,
                    selectionSubmitMs = Statistics.From(submit),
                    selectionToEndOfFrameMs = Statistics.From(frameVisible),
                    autoplayDurationSeconds = worstCase ? Time.realtimeSinceStartupAsDouble - autoplayStart : 0d,
                    autoplayMaximumFrameDelta = autoplayMaximumFrameDelta,
                    autoplaySubmitMs = Statistics.From(autoplaySubmit),
                    autoplayToEndOfFrameMs = Statistics.From(autoplayFrameVisible),
                    profilerAllocatedMemoryBytes = Profiler.GetTotalAllocatedMemoryLong(),
                    profilerReservedMemoryBytes = Profiler.GetTotalReservedMemoryLong(),
                    processResidentBytes = ReadProcStatusBytes("VmRSS:"),
                    processHighWaterBytes = ReadProcStatusBytes("VmHWM:"),
                });
            }
            finally
            {
                controller?.Dispose();
                if (File.Exists(archivePath))
                    File.Delete(archivePath);
            }
        }

        private static ArchivePreparation PrepareArchive(ProfileSpec spec, string archivePath)
        {
            ArchiveWrite write = WriteArchive(spec, archivePath);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            var watch = Stopwatch.StartNew();
            PreloadedDynamicTimeline timeline;
            using (var source = new FileStream(archivePath, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, FileOptions.SequentialScan))
                timeline = PreloadedDynamicTimelineCodec.Read(source, write.Descriptor, ProbeUniquePayloadBudgetBytes);
            watch.Stop();
            return new ArchivePreparation(timeline, write.Descriptor, write.NaivePayloadBytes, write.UniquePayloadBytes, write.BuildMilliseconds, write.WriteMilliseconds, watch.Elapsed.TotalMilliseconds);
        }

        private static ArchiveWrite WriteArchive(ProfileSpec spec, string archivePath)
        {
            SessionEpoch session = new(Id(1), 1);
            ContractId timelineId = Id(2);
            var builder = new PreloadedDynamicTimelineBuilder(ProbeUniquePayloadBudgetBytes);
            var watch = Stopwatch.StartNew();
            for (int index = 0; index < IndexCount; index++)
                builder.AddFrame(CreateBundle(session, timelineId, spec.Columns, spec.Sites, index));
            PreloadedDynamicTimeline timeline = builder.Build();
            watch.Stop();
            double buildMilliseconds = watch.Elapsed.TotalMilliseconds;

            watch.Restart();
            PreloadedTimelineDescriptor descriptor;
            using (var destination = new FileStream(archivePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 64 * 1024, FileOptions.SequentialScan))
                descriptor = PreloadedDynamicTimelineCodec.Write(destination, timeline);
            watch.Stop();
            return new ArchiveWrite(descriptor, timeline.NaivePayloadBytes, timeline.UniquePayloadBytes, buildMilliseconds, watch.Elapsed.TotalMilliseconds);
        }

        private static DynamicFrameBundle CreateBundle(SessionEpoch session, ContractId timeline, int columnCount, int siteCount, int timelineIndex)
        {
            var expectations = new DynamicColumnExpectation[columnCount];
            var frames = new ColumnFrame[columnCount];
            RenderTemporalSample sample = new(timelineIndex, 0.75f);
            StateRevision stateRevision = new(1);
            for (int column = 0; column < columnCount; column++)
            {
                ContractId columnId = Id((ulong)(10 + column));
                ContractId cutId = Id((ulong)(100 + column));
                AssetHash surfaceHash = Hash((ulong)(20 + column));
                expectations[column] = new DynamicColumnExpectation(columnId, DynamicColumnContent.Surface | DynamicColumnContent.Sites, new[] { cutId });
                frames[column] = new ColumnFrame(columnId, surfaceHash, new ScopeRevision(1), Optional<SurfaceFrame>.Some(CreateSurface(surfaceHash, stateRevision, sample, column)), Optional<SiteRenderFrame>.Some(CreateSites(Hash((ulong)(40 + column)), stateRevision, sample, siteCount, column, timelineIndex)), new[] { CreateOverlay(cutId, columnId, stateRevision, sample, column, timelineIndex) });
            }

            return new DynamicFrameBundle(session, timeline, new ScopeRevision((ulong)timelineIndex + 1), (ulong)timelineIndex + 1, timelineIndex / 10d, sample, stateRevision, expectations, frames);
        }

        private static SurfaceFrame CreateSurface(AssetHash hash, StateRevision revision, RenderTemporalSample sample, int column)
        {
            float[] activity = new float[SurfaceVertices];
            float[] opacity = new float[SurfaceVertices];
            byte[] active = new byte[SurfaceVertices];
            for (int index = 0; index < SurfaceVertices; index++)
            {
                activity[index] = ((index * 17 + column * 31 + sample.Index * 13) % 1009) / 1008f;
                opacity[index] = 1f;
                active[index] = 1;
            }

            return new SurfaceFrame(hash, revision, sample, TemporalApplication.SampleAndHold, RenderBuffer<float>.TakeOwnership(activity), RenderBuffer<float>.TakeOwnership(opacity), RenderBuffer<byte>.TakeOwnership(active));
        }

        private static SiteRenderFrame CreateSites(AssetHash hash, StateRevision revision, RenderTemporalSample sample, int count, int column, int timelineIndex)
        {
            Float3[] positions = new Float3[count];
            Rgba32[] colors = new Rgba32[count];
            float[] sizes = new float[count];
            byte[] visibility = new byte[count];
            SiteRenderFlags[] flags = new SiteRenderFlags[count];
            for (int index = 0; index < count; index++)
            {
                float value = ((index * 17 + column * 31 + timelineIndex * 13) % 1009) / 1008f;
                positions[index] = new Float3(index % 250, index / 250, column);
                colors[index] = new Rgba32((byte)(value * 255f), 64, 192, 255);
                sizes[index] = 2f + value;
                visibility[index] = 1;
                flags[index] = SiteRenderFlags.None;
            }

            return new SiteRenderFrame(hash, revision, sample, TemporalApplication.Linear, RenderBuffer<Float3>.TakeOwnership(positions), RenderBuffer<Rgba32>.TakeOwnership(colors), RenderBuffer<float>.TakeOwnership(sizes), RenderBuffer<byte>.TakeOwnership(visibility), RenderBuffer<SiteRenderFlags>.TakeOwnership(flags));
        }

        private static CutOverlayFrame CreateOverlay(ContractId cutId, ContractId columnId, StateRevision revision, RenderTemporalSample sample, int column, int timelineIndex)
        {
            Rgba32[] pixels = new Rgba32[OverlaySize * OverlaySize];
            for (int index = 0; index < pixels.Length; index++)
                pixels[index] = new Rgba32((byte)((index + column + timelineIndex) % 256), 32, 224, 255);
            return new CutOverlayFrame(cutId, columnId, revision, OverlaySize, OverlaySize, sample, TemporalApplication.SampleAndHold, new ScopeRevision(1), RenderBuffer<Rgba32>.TakeOwnership(pixels));
        }

        private void SetStatus(string value, Color color)
        {
            if (statusText == null)
                return;
            statusText.text = value;
            statusText.color = color;
        }

        private static long ReadProcStatusBytes(string key)
        {
            try
            {
                foreach (string line in File.ReadLines("/proc/self/status"))
                {
                    if (!line.StartsWith(key, StringComparison.Ordinal))
                        continue;
                    string[] fields = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                    return fields.Length >= 2 && long.TryParse(fields[1], out long kibibytes) ? kibibytes * 1024L : -1L;
                }
            }
            catch
            {
                // The external dumpsys capture remains authoritative when procfs is unavailable.
            }

            return -1L;
        }

        private static ContractId Id(ulong value) => new(value, value + 1);
        private static AssetHash Hash(ulong value) => new(value, value + 1, value + 2, value + 3);

        private readonly struct ProfileSpec
        {
            public ProfileSpec(string dataset, int columns, int sites)
            {
                Dataset = dataset;
                Columns = columns;
                Sites = sites;
            }

            public string Dataset { get; }
            public int Columns { get; }
            public int Sites { get; }
        }

        private sealed class ArchiveWrite
        {
            public ArchiveWrite(PreloadedTimelineDescriptor descriptor, long naivePayloadBytes, long uniquePayloadBytes, double buildMilliseconds, double writeMilliseconds)
            {
                Descriptor = descriptor;
                NaivePayloadBytes = naivePayloadBytes;
                UniquePayloadBytes = uniquePayloadBytes;
                BuildMilliseconds = buildMilliseconds;
                WriteMilliseconds = writeMilliseconds;
            }

            public PreloadedTimelineDescriptor Descriptor { get; }
            public long NaivePayloadBytes { get; }
            public long UniquePayloadBytes { get; }
            public double BuildMilliseconds { get; }
            public double WriteMilliseconds { get; }
        }

        private sealed class ArchivePreparation
        {
            public ArchivePreparation(PreloadedDynamicTimeline timeline, PreloadedTimelineDescriptor descriptor, long naivePayloadBytes, long uniquePayloadBytes, double buildMilliseconds, double writeMilliseconds, double readMilliseconds)
            {
                Timeline = timeline;
                Descriptor = descriptor;
                NaivePayloadBytes = naivePayloadBytes;
                UniquePayloadBytes = uniquePayloadBytes;
                BuildMilliseconds = buildMilliseconds;
                WriteMilliseconds = writeMilliseconds;
                ReadMilliseconds = readMilliseconds;
            }

            public PreloadedDynamicTimeline Timeline { get; }
            public PreloadedTimelineDescriptor Descriptor { get; }
            public long NaivePayloadBytes { get; }
            public long UniquePayloadBytes { get; }
            public double BuildMilliseconds { get; }
            public double WriteMilliseconds { get; }
            public double ReadMilliseconds { get; }
        }

        [Serializable]
        private sealed class Profile
        {
            public string schema;
            public string unity;
            public string platform;
            public string deviceModel;
            public string operatingSystem;
            public string graphicsDevice;
            public string graphicsApi;
            public int indexCount;
            public int minimumRandomSelections;
            public double worstCaseRandomSeconds;
            public double worstCaseAutoplaySeconds;
            public long explicitProbeGpuBudgetBytes;
            public long explicitProbeUniquePayloadBudgetBytes;
            public string result;
            public string failure;
            public PhaseProfile[] phases;
            public long totalAllocatedMemoryBytes;
            public long totalReservedMemoryBytes;
        }

        [Serializable]
        private sealed class PhaseProfile
        {
            public string dataset;
            public int columns;
            public int sitesPerColumn;
            public int indexCount;
            public long archiveBytes;
            public long naivePayloadBytes;
            public long uniqueCpuPayloadBytes;
            public long requiredGpuBytes;
            public double buildMs;
            public double archiveWriteAndHashMs;
            public double archiveReadAndHashMs;
            public double prepareUploadMs;
            public double randomSelectionDurationSeconds;
            public int randomSelectionMaximumFrameDelta;
            public Statistics selectionSubmitMs;
            public Statistics selectionToEndOfFrameMs;
            public double autoplayDurationSeconds;
            public int autoplayMaximumFrameDelta;
            public Statistics autoplaySubmitMs;
            public Statistics autoplayToEndOfFrameMs;
            public long profilerAllocatedMemoryBytes;
            public long profilerReservedMemoryBytes;
            public long processResidentBytes;
            public long processHighWaterBytes;
        }

        [Serializable]
        private sealed class Statistics
        {
            public bool available;
            public int count;
            public double p50;
            public double p95;
            public double maximum;

            public static Statistics From(IEnumerable<double> source)
            {
                double[] values = source.OrderBy(value => value).ToArray();
                if (values.Length == 0)
                    return new Statistics();
                return new Statistics
                {
                    available = true,
                    count = values.Length,
                    p50 = Percentile(values, 0.50),
                    p95 = Percentile(values, 0.95),
                    maximum = values[values.Length - 1],
                };
            }

            private static double Percentile(IReadOnlyList<double> values, double percentile)
            {
                int index = Mathf.Clamp(Mathf.CeilToInt((float)(values.Count * percentile)) - 1, 0, values.Count - 1);
                return values[index];
            }
        }
    }
}
