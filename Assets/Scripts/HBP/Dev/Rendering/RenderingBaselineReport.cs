using System;
using System.Collections.Generic;
using UnityEngine;

namespace HBP.Dev.Rendering
{
    [Serializable]
    public sealed class RenderingBaselineReport
    {
        public const int CurrentSchemaVersion = 3;

        public int SchemaVersion = CurrentSchemaVersion;
        public string RunId;
        public string CreatedUtc;
        public string ProjectPath;
        public string Visualization;
        public string OutputDirectory;
        public RuntimeConfiguration Runtime = new();
        public SceneConfiguration Scene = new();
        public List<CaptureRecord> Captures = new();
        public List<PerformanceRecord> Performance = new();
        public List<MemorySnapshot> Memory = new();
        public List<SeamSample> SurfaceCutSamples = new();
        public PatchFixtureRecord PatchFixture = new();
        public List<string> Warnings = new();
    }

    [Serializable]
    public sealed class RuntimeConfiguration
    {
        public string UnityVersion;
        public string Platform;
        public string OperatingSystem;
        public string GraphicsDeviceName;
        public string GraphicsDeviceType;
        public string GraphicsDeviceVersion;
        public int GraphicsMemorySizeMb;
        public string ColorSpace;
        public int ScreenWidth;
        public int ScreenHeight;
        public int QualityLevel;
        public string QualityName;
        public int VSyncCount;
        public int TargetFrameRate;
        public string RenderPipeline;
    }

    [Serializable]
    public sealed class SceneConfiguration
    {
        public string Name;
        public int ColumnCount;
        public int ViewCount;
        public int CutCount;
        public int TotalSiteCount;
        public bool BrainTransparent;
        public float BrainAlpha;
        public bool Edges;
        public bool StrongCuts;
        public bool ShowAllSites;
        public string BrainColor;
        public string CutColor;
        public string Colormap;
        public List<ColumnConfiguration> Columns = new();
    }

    [Serializable]
    public sealed class ColumnConfiguration
    {
        public int Index;
        public string Name;
        public string Type;
        public string Layer;
        public int SiteCount;
        public float ActivityAlpha;
        public List<CameraConfiguration> Cameras = new();
    }

    [Serializable]
    public sealed class CameraConfiguration
    {
        public int ViewIndex;
        public bool Enabled;
        public bool Minimized;
        public bool Edges;
        public string ClearFlags;
        public Color Background;
        public Vector3 LocalPosition;
        public Quaternion LocalRotation;
        public Vector3 LocalTarget;
        public int TargetWidth;
        public int TargetHeight;
        public string TargetFormat;
        public bool TargetSrgb;
    }

    [Serializable]
    public sealed class CaptureRecord
    {
        public string Family;
        public string Scenario;
        public string Path;
        public int Width;
        public int Height;
        public bool TransparentBackground;
        public AlphaRecord Alpha;
    }

    [Serializable]
    public sealed class AlphaRecord
    {
        public bool HasAlphaChannel;
        public bool AllCornersZero;
        public int TransparentPixelCount;
        public int OpaquePixelCount;
        public int MinAlpha;
        public int MaxAlpha;
        public int TopLeftAlpha;
        public int TopRightAlpha;
        public int BottomLeftAlpha;
        public int BottomRightAlpha;
    }

    [Serializable]
    public sealed class PerformanceRecord
    {
        public string Scenario;
        public string Workload;
        public string ProjectPath;
        public string Visualization;
        public bool ApplicationFocusedAtStart;
        public bool ApplicationFocusedAtEnd;
        public bool IdleThrottled;
        public bool Normative;
        public string Note;
        public int WarmupFrames;
        public int SampleFrames;
        public int ColumnCount;
        public int EnabledViewCount;
        public long EnabledViewPixelCount;
        public int RenderedSiteCount;
        public int SiteGameObjectCount;
        public int SiteRendererCount;
        public int SiteColliderCount;
        public int UniqueSiteMaterialCount;
        public MetricStatistics FrameIntervalMs;
        public MetricStatistics CpuMainThreadMs;
        public MetricStatistics CpuRenderThreadMs;
        public MetricStatistics GpuFrameMs;
        public MetricStatistics DrawCalls;
        public MetricStatistics SetPassCalls;
        public MetricStatistics Triangles;
        public MetricStatistics Vertices;
        public MetricStatistics GcAllocatedBytesPerFrame;
    }

    [Serializable]
    public sealed class MemorySnapshot
    {
        public string Scenario;
        public string ProjectPath;
        public string Visualization;
        public int ColumnCount;
        public int ViewCount;
        public int EnabledViewCount;
        public int LiveRenderTextureCount;
        public int CreatedRenderTextureCount;
        public long CreatedRenderTexturePixelCount;
        public int SceneTargetRenderTextureCount;
        public long SceneTargetRenderTexturePixelCount;
        public int HbpViewRenderTextureCount;
        public long HbpViewRenderTexturePixelCount;
        public long TotalAllocatedMemoryBytes;
        public long TotalReservedMemoryBytes;
        public long GraphicsDriverAllocatedMemoryBytes;
    }

    [Serializable]
    public sealed class MetricStatistics
    {
        public bool Available;
        public string Unit;
        public int Count;
        public double Median;
        public double P95;
        public double P99;
        public double Minimum;
        public double Maximum;

        public static MetricStatistics From(IReadOnlyList<double> values, string unit)
        {
            MetricStatistics result = new() { Unit = unit };
            if (values == null || values.Count == 0)
            {
                return result;
            }

            double[] sorted = new double[values.Count];
            for (int i = 0; i < values.Count; i++)
            {
                sorted[i] = values[i];
            }

            Array.Sort(sorted);
            result.Available = true;
            result.Count = sorted.Length;
            result.Minimum = sorted[0];
            result.Maximum = sorted[^1];
            result.Median = Percentile(sorted, 0.5);
            result.P95 = Percentile(sorted, 0.95);
            result.P99 = Percentile(sorted, 0.99);
            return result;
        }

        private static double Percentile(double[] sorted, double percentile)
        {
            if (sorted.Length == 1)
            {
                return sorted[0];
            }

            double position = percentile * (sorted.Length - 1);
            int lower = (int)Math.Floor(position);
            int upper = (int)Math.Ceiling(position);
            double fraction = position - lower;
            return sorted[lower] + (sorted[upper] - sorted[lower]) * fraction;
        }
    }

    [Serializable]
    public sealed class SeamSample
    {
        public int CutIndex;
        public string CutOrientation;
        public int VertexIndex;
        public Vector3 LocalPosition;
        public float DistanceToCut;
        public Vector2 CutUv;
        public Vector2 CutTextureUv;
        public Vector2 SurfaceAnatomyUv;
        public Vector2 SurfaceAlphaUv;
        public Vector2 SurfaceColorUv;
        public Color SurfaceAnatomySample;
        public Color SurfaceAlphaSample;
        public float SurfaceBoostedAlpha;
        public float SurfaceEffectiveAlpha;
        public Color SurfaceColormapSample;
        public Color SurfaceComposedSample;
        public Color CutSample;
        public float ColormapRgbDistance;
        public float RgbDistance;
        public float AlphaDistance;
    }

    [Serializable]
    public sealed class PatchFixtureRecord
    {
        public string Path;
        public int Width;
        public int Height;
        public List<PatchSample> Samples = new();
    }

    [Serializable]
    public sealed class PatchSample
    {
        public string Name;
        public Vector2Int Pixel;
        public Color32 Value;
    }
}
