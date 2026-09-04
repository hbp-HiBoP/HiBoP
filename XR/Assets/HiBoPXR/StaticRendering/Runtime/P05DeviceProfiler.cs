using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;

namespace CRNL.HiBoP.XR.StaticRendering
{
    public sealed class P05DeviceProfiler : MonoBehaviour
    {
        [SerializeField] private int warmupFrames = 360;
        [SerializeField] private int sampleFrames = 720;

        private IEnumerator Start()
        {
            for (int frame = 0; frame < warmupFrames; frame++)
            {
                yield return null;
            }

            using ProfilerRecorder mainThread = StartRecorder(ProfilerCategory.Internal, "Main Thread");
            using ProfilerRecorder renderThread = StartRecorder(ProfilerCategory.Internal, "Render Thread");
            using ProfilerRecorder gpu = StartRecorder(ProfilerCategory.Render, "GPU Frame Time");
            using ProfilerRecorder gc = StartRecorder(ProfilerCategory.Memory, "GC Allocated In Frame");
            var frameMilliseconds = new List<double>(sampleFrames);
            var mainMilliseconds = new List<double>(sampleFrames);
            var renderMilliseconds = new List<double>(sampleFrames);
            var gpuMilliseconds = new List<double>(sampleFrames);
            var gcBytes = new List<double>(sampleFrames);

            for (int frame = 0; frame < sampleFrames; frame++)
            {
                yield return null;
                frameMilliseconds.Add(Time.unscaledDeltaTime * 1000.0);
                AddRecorder(mainThread, mainMilliseconds, 1e-6);
                AddRecorder(renderThread, renderMilliseconds, 1e-6);
                AddRecorder(gpu, gpuMilliseconds, 1e-6);
                AddRecorder(gc, gcBytes, 1.0, true);
            }

            var profile = new Profile
            {
                schema = "P05-quest-profile-v1",
                unity = Application.unityVersion,
                platform = Application.platform.ToString(),
                deviceModel = SystemInfo.deviceModel,
                operatingSystem = SystemInfo.operatingSystem,
                graphicsDevice = SystemInfo.graphicsDeviceName,
                graphicsApi = SystemInfo.graphicsDeviceType.ToString(),
                colorSpace = QualitySettings.activeColorSpace.ToString(),
                warmupFrames = warmupFrames,
                sampleFrames = sampleFrames,
                frameIntervalMs = Statistics.From(frameMilliseconds),
                mainThreadMs = Statistics.From(mainMilliseconds),
                renderThreadMs = Statistics.From(renderMilliseconds),
                gpuFrameMs = Statistics.From(gpuMilliseconds),
                gcAllocatedBytes = Statistics.From(gcBytes),
                totalAllocatedMemoryBytes = Profiler.GetTotalAllocatedMemoryLong(),
                totalReservedMemoryBytes = Profiler.GetTotalReservedMemoryLong(),
                monoUsedMemoryBytes = Profiler.GetMonoUsedSizeLong(),
                surfaceMeshCount = SurfaceMeshCache.ActiveMeshCount,
            };
            string json = JsonUtility.ToJson(profile, true);
            string path = Path.Combine(Application.persistentDataPath, "p05-profile.json");
            File.WriteAllText(path, json);
            Debug.Log("P05_PROFILE_COMPLETE " + JsonUtility.ToJson(profile));
        }

        private static ProfilerRecorder StartRecorder(ProfilerCategory category, string name)
        {
            ProfilerRecorder recorder = ProfilerRecorder.StartNew(category, name, 1, ProfilerRecorderOptions.Default);
            return recorder.Valid ? recorder : default;
        }

        private static void AddRecorder(ProfilerRecorder recorder, ICollection<double> values, double scale, bool includeZero = false)
        {
            if (recorder.Valid && (includeZero || recorder.LastValue > 0))
            {
                values.Add(recorder.LastValue * scale);
            }
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
            public string colorSpace;
            public int warmupFrames;
            public int sampleFrames;
            public Statistics frameIntervalMs;
            public Statistics mainThreadMs;
            public Statistics renderThreadMs;
            public Statistics gpuFrameMs;
            public Statistics gcAllocatedBytes;
            public long totalAllocatedMemoryBytes;
            public long totalReservedMemoryBytes;
            public long monoUsedMemoryBytes;
            public int surfaceMeshCount;
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
                {
                    return new Statistics();
                }

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
