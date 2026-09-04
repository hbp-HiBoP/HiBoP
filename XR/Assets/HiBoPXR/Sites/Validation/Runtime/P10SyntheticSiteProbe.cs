using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.RenderModel;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.XR;
using ProviderXRStats = UnityEngine.XR.Provider.XRStats;

namespace CRNL.HiBoP.XR.Sites.Validation
{
    public sealed class P10SyntheticSiteProbe : MonoBehaviour
    {
        [SerializeField] private P10SiteRenderer[] renderers;
        [SerializeField] private int siteCount = 37_500;
        [SerializeField] private bool captureProfile = true;
        [SerializeField] private float warmupSeconds = 3f;
        [SerializeField] private float sampleSeconds = 10f;

        private SiteAsset m_Asset;
        private SiteRenderFrame m_FrameA;
        private SiteRenderFrame m_FrameB;
        private SiteDirtyRange[] m_EnduranceDirty;
        private int m_EnduranceFrame;
        private bool m_EnduranceActive;

        public void Configure(P10SiteRenderer[] siteRenderers, int count, bool profile)
        {
            renderers = siteRenderers;
            siteCount = count;
            captureProfile = profile;
        }

        private IEnumerator Start()
        {
            if (renderers == null || renderers.Length == 0)
                throw new InvalidOperationException("P10 validation renderers must be serialized.");
            CreateDataset();
            foreach (P10SiteRenderer renderer in renderers)
            {
                renderer.SetAsset(m_Asset);
                renderer.ApplyFrame(m_FrameA);
            }

            if (!captureProfile)
                yield break;

            var phases = new List<PhaseProfile>();
            foreach (int instanceCount in new[] { 1, 3, 8 })
            {
                if (instanceCount > renderers.Length)
                    continue;
                ConfigureActiveRenderers(instanceCount);
                float warmupDeadline = Time.realtimeSinceStartup + warmupSeconds;
                while (Time.realtimeSinceStartup < warmupDeadline)
                    yield return null;
                PhaseProfile phase = null;
                yield return CapturePhase(instanceCount, value => phase = value);
                phases.Add(phase);
            }

            var profile = new Profile
            {
                schema = "P10-quest-profile-v1",
                unity = Application.unityVersion,
                platform = Application.platform.ToString(),
                deviceModel = SystemInfo.deviceModel,
                operatingSystem = SystemInfo.operatingSystem,
                graphicsDevice = SystemInfo.graphicsDeviceName,
                graphicsApi = SystemInfo.graphicsDeviceType.ToString(),
                siteCount = siteCount,
                serializedRendererCount = renderers.Length,
                individualSiteObjectCount = CountIndividualSiteObjects(),
                staticBufferBytes = siteCount * 16L,
                dynamicBufferBytesPerInstance = siteCount * 16L,
                phases = phases.ToArray(),
            };
            string path = Path.Combine(Application.persistentDataPath, "p10-profile.json");
            File.WriteAllText(path, JsonUtility.ToJson(profile, true));
            UnityEngine.Debug.Log("P10_PROFILE_COMPLETE " + JsonUtility.ToJson(profile));
            m_EnduranceDirty = new[] { new SiteDirtyRange(0, Math.Min(256, siteCount)) };
            m_EnduranceActive = true;
        }

        private void Update()
        {
            if (!m_EnduranceActive)
                return;

            SiteRenderFrame source = (m_EnduranceFrame & 1) == 0 ? m_FrameB : m_FrameA;
            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                if (renderers[rendererIndex].isActiveAndEnabled)
                    renderers[rendererIndex].ApplyFrame(source, m_EnduranceDirty);
            }

            int expectedIndex = (m_EnduranceFrame * 7919) % siteCount;
            Vector3 localMillimeters = ToVector3(m_Asset.Positions[expectedIndex]);
            Vector3 world = renderers[0].transform.TransformPoint(localMillimeters * 0.001f);
            bool proximityHit = renderers[0].FindNearest(world, out SitePickResult proximity);
            bool rayHit = renderers[0].Raycast(new Ray(world, renderers[0].transform.forward), 0.003f, out SitePickResult ray);
            if (!proximityHit || !rayHit || proximity.SiteId != m_Asset.SiteIds[expectedIndex] || ray.SiteId != m_Asset.SiteIds[expectedIndex])
            {
                m_EnduranceActive = false;
                UnityEngine.Debug.LogError($"P10_ENDURANCE_FAILED frame={m_EnduranceFrame}");
                return;
            }

            m_EnduranceFrame++;
            if (m_EnduranceFrame % 4320 == 0)
                UnityEngine.Debug.Log($"P10_ENDURANCE_HEARTBEAT frames={m_EnduranceFrame} exact={m_EnduranceFrame}");
        }

        private IEnumerator CapturePhase(int instanceCount, Action<PhaseProfile> completed)
        {
            using ProfilerRecorder mainThread = StartRecorder(ProfilerCategory.Internal, "Main Thread");
            using ProfilerRecorder renderThread = StartRecorder(ProfilerCategory.Internal, "Render Thread");
            using ProfilerRecorder gpu = StartRecorder(ProfilerCategory.Render, "GPU Frame Time");
            using ProfilerRecorder drawCalls = StartRecorder(ProfilerCategory.Render, "Draw Calls Count");
            using ProfilerRecorder gc = StartRecorder(ProfilerCategory.Memory, "GC Allocated In Frame");
            int expectedCapacity = Mathf.CeilToInt(sampleSeconds * 72f);
            var frameMilliseconds = new List<double>(expectedCapacity);
            var mainMilliseconds = new List<double>(expectedCapacity);
            var renderMilliseconds = new List<double>(expectedCapacity);
            var cpuFrameMilliseconds = new List<double>(expectedCapacity);
            var cpuMainWorkMilliseconds = new List<double>(expectedCapacity);
            var cpuPresentWaitMilliseconds = new List<double>(expectedCapacity);
            var cpuRenderMilliseconds = new List<double>(expectedCapacity);
            var gpuMilliseconds = new List<double>(expectedCapacity);
            var drawCallCounts = new List<double>(expectedCapacity);
            var gcBytes = new List<double>(expectedCapacity);
            var pickMilliseconds = new List<double>(expectedCapacity * 2);
            var dirtyMilliseconds = new List<double>(expectedCapacity);
            int correctPicks = 0;
            var dirty = new[] { new SiteDirtyRange(0, Math.Min(256, siteCount)) };
            var frameTimings = new FrameTiming[1];
            var displays = new List<XRDisplaySubsystem>();
            SubsystemManager.GetSubsystems(displays);
            XRDisplaySubsystem display = displays.FirstOrDefault(candidate => candidate.running);

            int frame = 0;
            float sampleDeadline = Time.realtimeSinceStartup + sampleSeconds;
            while (Time.realtimeSinceStartup < sampleDeadline)
            {
                SiteRenderFrame source = (frame & 1) == 0 ? m_FrameB : m_FrameA;
                long start = Stopwatch.GetTimestamp();
                for (int rendererIndex = 0; rendererIndex < instanceCount; rendererIndex++)
                    renderers[rendererIndex].ApplyFrame(source, dirty);
                dirtyMilliseconds.Add(ElapsedMilliseconds(start));

                int expectedIndex = (frame * 7919) % siteCount;
                Vector3 localMillimeters = ToVector3(m_Asset.Positions[expectedIndex]);
                Vector3 world = renderers[0].transform.TransformPoint(localMillimeters * 0.001f);
                start = Stopwatch.GetTimestamp();
                renderers[0].FindNearest(world, out SitePickResult proximity);
                pickMilliseconds.Add(ElapsedMilliseconds(start));
                start = Stopwatch.GetTimestamp();
                renderers[0].Raycast(new Ray(world, renderers[0].transform.forward), 0.003f, out SitePickResult ray);
                pickMilliseconds.Add(ElapsedMilliseconds(start));
                if (proximity.SiteId == m_Asset.SiteIds[expectedIndex] && ray.SiteId == m_Asset.SiteIds[expectedIndex])
                    correctPicks++;

                FrameTimingManager.CaptureFrameTimings();
                yield return null;
                frameMilliseconds.Add(Time.unscaledDeltaTime * 1000.0);
                AddRecorder(mainThread, mainMilliseconds, 1e-6);
                AddRecorder(renderThread, renderMilliseconds, 1e-6);
                AddRecorder(gpu, gpuMilliseconds, 1e-6);
                AddRecorder(drawCalls, drawCallCounts, 1.0, true);
                AddRecorder(gc, gcBytes, 1.0, true);
                bool hasCpuMetric = TryAddXrMetric(display, "perfmetrics.appcputime", cpuFrameMilliseconds);
                bool hasGpuMetric = TryAddXrMetric(display, "perfmetrics.appgputime", gpuMilliseconds);
                if (FrameTimingManager.GetLatestTimings(1, frameTimings) > 0)
                {
                    double mainWork = Math.Max(0.0, frameTimings[0].cpuMainThreadFrameTime - frameTimings[0].cpuMainThreadPresentWaitTime);
                    double renderWork = frameTimings[0].cpuRenderThreadFrameTime;
                    AddPositive(mainWork, cpuMainWorkMilliseconds);
                    AddPositive(frameTimings[0].cpuMainThreadPresentWaitTime, cpuPresentWaitMilliseconds);
                    AddPositive(renderWork, cpuRenderMilliseconds);
                    if (!hasCpuMetric)
                        AddPositive(Math.Max(mainWork, renderWork), cpuFrameMilliseconds);
                    if (!hasGpuMetric)
                        AddPositive(frameTimings[0].gpuFrameTime, gpuMilliseconds);
                }

                frame++;
            }

            completed(new PhaseProfile
            {
                instanceCount = instanceCount,
                expectedDrawCalls = instanceCount,
                sampleFrames = frameMilliseconds.Count,
                correctPickCount = correctPicks,
                expectedPickCount = frameMilliseconds.Count,
                frameIntervalMs = Statistics.From(frameMilliseconds),
                mainThreadMs = Statistics.From(mainMilliseconds),
                renderThreadMs = Statistics.From(renderMilliseconds),
                cpuFrameMs = Statistics.From(cpuFrameMilliseconds),
                cpuMainThreadWorkMs = Statistics.From(cpuMainWorkMilliseconds),
                cpuMainThreadPresentWaitMs = Statistics.From(cpuPresentWaitMilliseconds),
                cpuRenderThreadMs = Statistics.From(cpuRenderMilliseconds),
                gpuFrameMs = Statistics.From(gpuMilliseconds),
                drawCalls = Statistics.From(drawCallCounts),
                pickingMs = Statistics.From(pickMilliseconds),
                dirty256UploadMs = Statistics.From(dirtyMilliseconds),
                gcAllocatedBytes = Statistics.From(gcBytes),
                totalAllocatedMemoryBytes = Profiler.GetTotalAllocatedMemoryLong(),
                totalReservedMemoryBytes = Profiler.GetTotalReservedMemoryLong(),
            });
        }

        private void CreateDataset()
        {
            var ids = new ContractId[siteCount];
            var positions = new Float3[siteCount];
            var colorsA = new Rgba32[siteCount];
            var colorsB = new Rgba32[siteCount];
            var sizesA = new float[siteCount];
            var sizesB = new float[siteCount];
            var visibility = new byte[siteCount];
            var flags = new SiteRenderFlags[siteCount];
            Vector3 minimum = new(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Vector3 maximum = new(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            for (int index = 0; index < siteCount; index++)
            {
                int x = index % 50;
                int y = index / 50 % 30;
                int z = index / 1500;
                Vector3 position = siteCount <= 8 ? new Vector3((index - (siteCount - 1) * 0.5f) * 8f, 0f, 0f) : new Vector3((x - 24.5f) * 4.5f, (y - 14.5f) * 4.5f, (z - 12f) * 4.5f);
                ids[index] = new ContractId(0x10, (ulong)index + 1);
                positions[index] = new Float3(position.x, position.y, position.z);
                colorsA[index] = new Rgba32(135, 38, 38, 255);
                colorsB[index] = index < 256 ? new Rgba32(38, 135, 70, 255) : colorsA[index];
                sizesA[index] = 1f;
                sizesB[index] = index < 256 ? 1.2f : 1f;
                visibility[index] = 1;
                minimum = Vector3.Min(minimum, position);
                maximum = Vector3.Max(maximum, position);
            }

            AssetHash hash = new(0x10, 0x20, 0x30, (ulong)siteCount);
            m_Asset = new SiteAsset(hash, CoordinateSpace.DesktopUnityMillimetersV1, new Bounds3F(new Float3(minimum.x, minimum.y, minimum.z), new Float3(maximum.x, maximum.y, maximum.z)), RenderBuffer<ContractId>.TakeOwnership(ids), RenderBuffer<Float3>.TakeOwnership(positions));
            m_FrameA = CreateFrame(colorsA, sizesA, visibility, flags);
            m_FrameB = CreateFrame(colorsB, sizesB, visibility, flags);
        }

        private SiteRenderFrame CreateFrame(Rgba32[] colors, float[] sizes, byte[] visibility, SiteRenderFlags[] flags)
        {
            return new SiteRenderFrame(m_Asset.Hash, new StateRevision(1), new RenderTemporalSample(0, 0f), TemporalApplication.Linear, RenderBuffer<Float3>.CopyFrom(m_Asset.Positions.ToArray()), RenderBuffer<Rgba32>.CopyFrom(colors), RenderBuffer<float>.CopyFrom(sizes), RenderBuffer<byte>.CopyFrom(visibility), RenderBuffer<SiteRenderFlags>.CopyFrom(flags));
        }

        private void ConfigureActiveRenderers(int count)
        {
            for (int index = 0; index < renderers.Length; index++)
                renderers[index].enabled = index < count;
        }

        private int CountIndividualSiteObjects()
        {
            return GetComponentsInChildren<Transform>(true).Length - renderers.Length - 1;
        }

        private static ProfilerRecorder StartRecorder(ProfilerCategory category, string name)
        {
            ProfilerRecorder recorder = ProfilerRecorder.StartNew(category, name, 1, ProfilerRecorderOptions.Default);
            return recorder.Valid ? recorder : default;
        }

        private static void AddRecorder(ProfilerRecorder recorder, ICollection<double> values, double scale, bool includeZero = false)
        {
            if (recorder.Valid && (includeZero || recorder.LastValue > 0))
                values.Add(recorder.LastValue * scale);
        }

        private static void AddPositive(double value, ICollection<double> values)
        {
            if (value > 0.0 && !double.IsNaN(value) && !double.IsInfinity(value))
                values.Add(value);
        }

        private static bool TryAddXrMetric(XRDisplaySubsystem display, string name, ICollection<double> values)
        {
            if (display == null || !ProviderXRStats.TryGetStat(display, name, out float value) || value <= 0f)
                return false;
            values.Add(value);
            return true;
        }

        private static Vector3 ToVector3(Float3 value) => new(value.X, value.Y, value.Z);

        private static double ElapsedMilliseconds(long start) => (Stopwatch.GetTimestamp() - start) * 1000d / Stopwatch.Frequency;

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
            public int siteCount;
            public int serializedRendererCount;
            public int individualSiteObjectCount;
            public long staticBufferBytes;
            public long dynamicBufferBytesPerInstance;
            public PhaseProfile[] phases;
        }

        [Serializable]
        private sealed class PhaseProfile
        {
            public int instanceCount;
            public int expectedDrawCalls;
            public int sampleFrames;
            public int correctPickCount;
            public int expectedPickCount;
            public Statistics frameIntervalMs;
            public Statistics mainThreadMs;
            public Statistics renderThreadMs;
            public Statistics cpuFrameMs;
            public Statistics cpuMainThreadWorkMs;
            public Statistics cpuMainThreadPresentWaitMs;
            public Statistics cpuRenderThreadMs;
            public Statistics gpuFrameMs;
            public Statistics drawCalls;
            public Statistics pickingMs;
            public Statistics dirty256UploadMs;
            public Statistics gcAllocatedBytes;
            public long totalAllocatedMemoryBytes;
            public long totalReservedMemoryBytes;
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
