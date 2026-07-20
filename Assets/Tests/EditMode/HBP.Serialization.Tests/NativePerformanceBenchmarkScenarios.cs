using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HBP.Core.DLL;
using HBP.Core.Enums;
using HBP.Core.Tools;
using UnityEngine;
using ActivityGenerator = HBP.Tests.Serialization.LegacyNative.ActivityGenerator;
using BBox = HBP.Tests.Serialization.LegacyNative.BBox;
using CutGenerator = HBP.Tests.Serialization.LegacyNative.CutGenerator;
using CutGeometryGenerator = HBP.Tests.Serialization.LegacyNative.CutGeometryGenerator;
using DensityGenerator = HBP.Tests.Serialization.LegacyNative.DensityGenerator;
using FMRIGenerator = HBP.Tests.Serialization.LegacyNative.FMRIGenerator;
using GeneratorSurface = HBP.Tests.Serialization.LegacyNative.GeneratorSurface;
using IEEGGenerator = HBP.Tests.Serialization.LegacyNative.IEEGGenerator;
using MarsAtlas = HBP.Tests.Serialization.LegacyNative.MarsAtlas;
using MEGGenerator = HBP.Tests.Serialization.LegacyNative.MEGGenerator;
using NIFTI = HBP.Tests.Serialization.LegacyNative.NIFTI;
using RawSiteList = HBP.Tests.Serialization.LegacyNative.RawSiteList;
using Surface = HBP.Tests.Serialization.LegacyNative.Surface;
using SurfaceGenerator = HBP.Tests.Serialization.LegacyNative.SurfaceGenerator;
using Volume = HBP.Tests.Serialization.LegacyNative.Volume;

namespace HBP.Tests.Serialization
{
    internal static class NativePerformanceBenchmarkScenarios
    {
        public static List<Func<NativePerformanceScenario>> Build(
            BenchmarkBackend backend,
            NativePerformanceBenchmarkFixtures fixtures,
            bool includeVideo = false,
            Func<string, string, bool> include = null)
        {
            List<Func<NativePerformanceScenario>> scenarios = new();
            void Add(string name, string domain, Func<NativePerformanceScenario> factory)
            {
                if (include == null || include(name, domain)) scenarios.Add(factory);
            }

            Add("native.create.handles", "lifecycle", () => CreateHandleScenario());
            Add("volume.load.nifti3d", "volume", () => LoadVolumeScenario(fixtures.LargeNifti, "volume.load.nifti3d", "64x64x64 float32"));
            Add("volume.load.nifti4d", "volume", () => LoadNifti4DScenario(fixtures.MultiNifti));
            Add("volume.sample.surface-batch", "volume", () => SampleVolumeScenario(fixtures.LargeNifti, fixtures.Grid128Obj));
            Add("volume.histogram.render", "volume", () => HistogramScenario(backend, fixtures.MultiNifti));
            Add("surface.load.obj", "surface", () => LoadSurfaceScenario(fixtures.RealSurfaceObj, false, "surface.load.obj", "MNI left hemisphere OBJ: 34,733 vertices / 69,470 triangles"));
            Add("surface.load.gifti", "surface", () => LoadSurfaceScenario(fixtures.Gifti, true, "surface.load.gifti", "MNI left hemisphere GIFTI: 34,733 vertices / 69,470 triangles"));
            Add("surface.clone", "surface", () => CloneSurfaceScenario(fixtures.Grid64Obj));
            Add("surface.append", "surface", () => AppendSurfaceScenario(fixtures.Grid64Obj, fixtures.Grid64OffsetObj));
            Add("surface.visibility", "surface", () => VisibilitySurfaceScenario(fixtures.Grid64Obj));
            Add("surface.simplify", "surface", () => SimplifySurfaceScenario(fixtures.Grid128Obj));
            Add("surface.copy.unity-mesh", "surface", () => CopySurfaceScenario(fixtures.Grid64Obj));
            Add("cut.surface.cube-one-plane", "cut", () => CutSurfaceScenario(fixtures.CubeObj, onePlane: true, "cube", 500));
            Add("cut.surface.cube-two-planes", "cut", () => CutSurfaceScenario(fixtures.CubeObj, onePlane: false, "cube", 500));
            Add("cut.surface.mni-lhemi-one-plane", "cut", () => CutSurfaceScenario(fixtures.RealSurfaceObj, onePlane: true, "mni-lhemi", 3));
            Add("atlas.load.mars", "atlas", () => LoadAtlasScenario(fixtures));
            Add("atlas.query.unit", "atlas", () => QueryAtlasScenario(fixtures));
            Add("atlas.query.surface-batch", "atlas", () => BatchAtlasScenario(fixtures));
            Add("activity.generator-surface.initialize", "activity", () => GeneratorSurfaceInitializationScenario(fixtures));
            Add("activity.density.vertices-4096-sites-256", "activity", () => DensityScenario(fixtures, 256, largeSurface: false));
            Add("activity.density.vertices-4096-sites-1024", "activity", () => DensityScenario(fixtures, 1024, largeSurface: false));
            Add("activity.density.vertices-16384-sites-256", "activity", () => DensityScenario(fixtures, 256, largeSurface: true));
            Add("activity.ieeg.vertices-4096-sites-256", "activity", () => IeegScenario(fixtures, 256, 4, largeSurface: false));
            Add("activity.ieeg.vertices-4096-sites-1024", "activity", () => IeegScenario(fixtures, 1024, 4, largeSurface: false));
            Add("activity.ieeg.vertices-16384-sites-256", "activity", () => IeegScenario(fixtures, 256, 4, largeSurface: true));
            Add("activity.fmri.single-small", "fmri", () => VolumeActivityScenario(fixtures, fmri: true, small: true));
            Add("activity.meg.single-small", "meg", () => VolumeActivityScenario(fixtures, fmri: false, small: true));
            Add("activity.fmri.multivolume", "fmri", () => VolumeActivityScenario(fixtures, fmri: true, small: false));
            Add("activity.meg.multivolume", "meg", () => VolumeActivityScenario(fixtures, fmri: false, small: false));
            Add("cut.texture.volume-blur", "cut-texture", () => CutTextureScenario(backend, fixtures, activityOverlay: false));
            Add("cut.texture.activity-blur", "cut-texture", () => CutTextureScenario(backend, fixtures, activityOverlay: true));
            Add("texture.colormap.1d", "texture", () => ColormapScenario(backend, twoDimensional: false));
            Add("texture.colormap.2d", "texture", () => ColormapScenario(backend, twoDimensional: true));
            Add("texture.rotate.512", "texture", () => RotateTextureScenario(backend, 512));
            Add("texture.pad-square.1024", "texture", () => ResizeTextureScenario(backend, 1024));
            if (includeVideo)
            {
                Add("video.mjpeg.512", "video", () => VideoScenario(backend, fixtures.Root, 512, 512, 5, "video.mjpeg.512"));
                Add("video.mjpeg.1080p", "video", () => VideoScenario(backend, fixtures.Root, 1920, 1080, 5, "video.mjpeg.1080p"));
            }
            return scenarios;
        }

        private static NativePerformanceScenario CreateHandleScenario()
        {
            const int operations = 100;
            return new NativePerformanceScenario(
                "native.create.handles",
                "lifecycle",
                "create/destroy",
                "100 Volume and 100 Surface handles per iteration",
                operations * 2,
                () =>
                {
                    ulong checksum = 0;
                    for (int i = 0; i < operations; ++i)
                    {
                        using Volume volume = new();
                        using Surface surface = new();
                        checksum += (ulong)(volume.IsLoaded ? 1 : 2);
                        checksum += (ulong)surface.NumberOfVertices;
                    }
                    return checksum;
                });
        }

        private static NativePerformanceScenario LoadVolumeScenario(string path, string name, string workload)
        {
            return new NativePerformanceScenario(
                name,
                "volume",
                "load",
                workload,
                1,
                () =>
                {
                    using Volume volume = new();
                    Require(volume.LoadNIFTIFile(path), $"Could not load {path}.");
                    Require(volume.IsLoaded, "Volume did not report its loaded state.");
                    Require(volume.ExtremeValues.Max > volume.ExtremeValues.Min, "Volume extrema are invalid.");
                    return Hash(volume.Center) ^ Hash(volume.ExtremeValues.Max);
                });
        }

        private static NativePerformanceScenario LoadNifti4DScenario(string path)
        {
            return new NativePerformanceScenario(
                "volume.load.nifti4d",
                "volume",
                "load",
                "48x48x48x4 float32; load reader and extract all four volumes",
                1,
                () =>
                {
                    using NIFTI nifti = new();
                    Require(nifti.Load(path), $"Could not load {path}.");
                    Require(nifti.NumberOfVolumes == 4, "Expected four temporal volumes.");
                    ulong checksum = (ulong)nifti.NumberOfVolumes;
                    for (int i = 0; i < nifti.NumberOfVolumes; ++i)
                    {
                        using Volume volume = nifti.ExtractVolume(i);
                        // hbp_export's historical wrapper does not update the managed
                        // IsLoaded flag when convertToVolume_NIFTI fills a Volume.
                        // Validate the extracted native data instead so the oracle is
                        // meaningful and identical for both backends.
                        Require(float.IsFinite(volume.Center.x), $"Temporal volume {i} has an invalid center.");
                        Require(volume.ExtremeValues.Max > volume.ExtremeValues.Min,
                            $"Temporal volume {i} has invalid extrema.");
                        checksum = Mix(checksum, Hash(volume.ExtremeValues.Min));
                    }
                    return checksum;
                });
        }

        private static NativePerformanceScenario SampleVolumeScenario(string volumePath, string surfacePath)
        {
            Volume volume = LoadVolume(volumePath);
            Surface surface = LoadSurface(surfacePath, gifti: false);
            return new NativePerformanceScenario(
                "volume.sample.surface-batch",
                "volume",
                "compute+copy",
                $"{surface.NumberOfVertices:N0} surface vertices sampled from a 64x64x64 volume",
                1,
                () =>
                {
                    float[] values = volume.GetVerticesValues(surface);
                    Require(values.Length == surface.NumberOfVertices, "Unexpected sampled-value count.");
                    Require(values.All(float.IsFinite), "Volume sampling produced a non-finite value.");
                    return Hash(values);
                },
                dispose: () =>
                {
                    surface.Dispose();
                    volume.Dispose();
                });
        }

        private static NativePerformanceScenario HistogramScenario(BenchmarkBackend backend, string path)
        {
            const int operations = 100;
            NIFTI nifti = new();
            Require(nifti.Load(path), "Could not load histogram NIFTI fixture.");
            const int width = 512;
            const int height = 256;
            return new NativePerformanceScenario(
                "volume.histogram.render",
                "volume",
                "compute+copy",
                "50-bin histogram of a 48x48x48x4 NIFTI rendered to 512x256 RGBA",
                operations,
                () =>
                {
                    ulong checksum = 0;
                    for (int operation = 0; operation < operations; ++operation)
                    {
                        Color32[] pixels;
                        if (backend == BenchmarkBackend.HbpExport)
                        {
                            using LegacyTextureBridge texture = LegacyTextureBridge.GenerateHistogram(nifti, height, width, true);
                            pixels = texture.GetPixels(out int actualWidth, out int actualHeight);
                            // hbp_export historically passes (height,width) to the
                            // cv::Size(width,height) constructor and therefore returns
                            // transposed dimensions. The pixel workload is identical.
                            Require(actualWidth * actualHeight == width * height, "Legacy histogram pixel count mismatch.");
                        }
                        else
                        {
                            int[] bins = nifti.GetHistogramBins(UnityTextureFactory.HistogramBinCount);
                            pixels = UnityTextureFactory.GenerateDistributionHistogramPixels(bins, height, width, true);
                        }
                        Require(pixels.Length == width * height, "Histogram pixel count mismatch.");
                        checksum = Mix(checksum, Hash(pixels));
                    }
                    return checksum;
                },
                dispose: nifti.Dispose);
        }

        private static NativePerformanceScenario ColormapScenario(BenchmarkBackend backend, bool twoDimensional)
        {
            const int operations = 5;
            return new NativePerformanceScenario(
                twoDimensional ? "texture.colormap.2d" : "texture.colormap.1d",
                "texture",
                "compute+copy",
                twoDimensional ? "Five 255x255 two-dimensional colormaps" : "Five 255x1 one-dimensional colormaps",
                operations,
                () =>
                {
                    ulong checksum = 0;
                    for (int operation = 0; operation < operations; ++operation)
                    {
                        Color32[] pixels;
                        if (backend == BenchmarkBackend.HbpExport)
                        {
                            using LegacyTextureBridge texture = twoDimensional
                                ? LegacyTextureBridge.Generate2D((int)ColorType.RedYellow, (int)ColorType.BlueGreen)
                                : LegacyTextureBridge.Generate1D((int)ColorType.BrainColor);
                            pixels = texture.GetPixels(out int width, out int height);
                            Require(width > 0 && height > 0, "Legacy colormap dimensions are invalid.");
                        }
                        else
                        {
                            pixels = twoDimensional
                                ? UnityTextureFactory.Generate2DColorPixels(ColorType.RedYellow, ColorType.BlueGreen)
                                : UnityTextureFactory.Generate1DColorPixels(ColorType.BrainColor);
                        }
                        Require(pixels.Length > 0, "Colormap is empty.");
                        checksum = Mix(checksum, Hash(pixels));
                    }
                    return checksum;
                });
        }

        private static NativePerformanceScenario RotateTextureScenario(BenchmarkBackend backend, int size)
        {
            const int operations = 10;
            Color32[] sourcePixels = CreateTexturePixels(size, size);
            LegacyTextureBridge legacySource = backend == BenchmarkBackend.HbpExport
                ? LegacyTextureBridge.CreateFromPixels(sourcePixels, size, size)
                : null;
            return new NativePerformanceScenario(
                $"texture.rotate.{size}",
                "texture",
                "compute+copy",
                $"10 aggregated rotations and managed RGBA copies of a {size}x{size} cut texture",
                operations,
                () =>
                {
                    ulong checksum = 0;
                    for (int operation = 0; operation < operations; ++operation)
                    {
                        Color32[] pixels;
                        if (backend == BenchmarkBackend.HbpExport)
                        {
                            using LegacyTextureBridge rotated = legacySource.Rotate(CutOrientation.Sagittal, true);
                            pixels = rotated.GetPixels(out int width, out int height);
                            Require(width == size && height == size, "Legacy rotated texture dimensions mismatch.");
                        }
                        else
                        {
                            pixels = UnityTextureFactory.RotateCutPixels(
                                sourcePixels, size, size, CutOrientation.Sagittal, true, out int width, out int height);
                            Require(width == size && height == size, "Managed rotated texture dimensions mismatch.");
                        }
                        checksum = Mix(checksum, Hash(pixels));
                    }
                    return checksum;
                },
                dispose: () => legacySource?.Dispose());
        }

        private static NativePerformanceScenario ResizeTextureScenario(BenchmarkBackend backend, int targetSize)
        {
            int sourceWidth = targetSize * 3 / 4;
            int sourceHeight = targetSize / 2;
            Color32[] sourcePixels = CreateTexturePixels(sourceWidth, sourceHeight);
            return new NativePerformanceScenario(
                $"texture.pad-square.{targetSize}",
                "texture",
                "compute+copy",
                $"Pad a {sourceWidth}x{sourceHeight} texture to {targetSize}x{targetSize} and copy RGBA",
                1,
                () =>
                {
                    Color32[] pixels;
                    if (backend == BenchmarkBackend.HbpExport)
                    {
                        using LegacyTextureBridge texture = LegacyTextureBridge.CreateFromPixels(sourcePixels, sourceWidth, sourceHeight);
                        texture.ResizeToSquare(targetSize);
                        pixels = texture.GetPixels(out int width, out int height);
                        Require(width == targetSize && height == targetSize, "Legacy padded texture dimensions mismatch.");
                        Texture2D unityTexture = CreateUnityTexture(pixels, width, height);
                        try
                        {
                            pixels = unityTexture.GetPixels32();
                        }
                        finally
                        {
                            UnityEngine.Object.DestroyImmediate(unityTexture);
                        }
                    }
                    else
                    {
                        Texture2D texture = CreateUnityTexture(sourcePixels, sourceWidth, sourceHeight);
                        try
                        {
                            UnityTextureFactory.ResizeToSquare(texture, targetSize);
                            pixels = texture.GetPixels32();
                        }
                        finally
                        {
                            UnityEngine.Object.DestroyImmediate(texture);
                        }
                    }
                    Require(pixels.Length == targetSize * targetSize, "Padded texture pixel count mismatch.");
                    return Hash(pixels);
                });
        }

        private static NativePerformanceScenario VideoScenario(
            BenchmarkBackend backend,
            string root,
            int width,
            int height,
            int frameCount,
            string name)
        {
            Color32[] sourcePixels = CreateTexturePixels(width, height);
            LegacyTextureBridge legacyFrame = backend == BenchmarkBackend.HbpExport
                ? LegacyTextureBridge.CreateFromPixels(sourcePixels, width, height)
                : null;
            Texture2D coreFrame = backend == BenchmarkBackend.HbpCore
                ? CreateUnityTexture(sourcePixels, width, height)
                : null;
            string path = Path.Combine(root, $"{name}-{backend}.avi");
            return new NativePerformanceScenario(
                name,
                "video",
                "encode+io",
                $"MJPEG AVI {width}x{height}; five frames per iteration (100 measured frames total)",
                frameCount,
                () =>
                {
                    if (backend == BenchmarkBackend.HbpExport)
                    {
                        using LegacyVideoStreamBridge stream = new();
                        stream.Open(path, width, height, 25.0f);
                        for (int frame = 0; frame < frameCount; ++frame)
                        {
                            stream.WriteFrame(legacyFrame);
                        }
                    }
                    else
                    {
                        using VideoStream stream = new();
                        stream.Open(path, width, height, 25.0f);
                        for (int frame = 0; frame < frameCount; ++frame)
                        {
                            stream.WriteFrame(coreFrame);
                        }
                    }
                    long length = new FileInfo(path).Length;
                    Require(length > 1024, "Encoded AVI is unexpectedly small.");
                    return unchecked((ulong)length);
                },
                dispose: () =>
                {
                    legacyFrame?.Dispose();
                    if (coreFrame != null)
                    {
                        UnityEngine.Object.DestroyImmediate(coreFrame);
                    }
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                });
        }

        private static NativePerformanceScenario LoadSurfaceScenario(
            string path,
            bool gifti,
            string name,
            string workload)
        {
            int operations = gifti ? 5 : 1;
            return new NativePerformanceScenario(
                name,
                "surface",
                "load",
                workload,
                operations,
                () =>
                {
                    ulong checksum = 0;
                    for (int i = 0; i < operations; ++i)
                    {
                        using Surface surface = LoadSurface(path, gifti);
                        Require(surface.NumberOfVertices > 0, "Loaded surface is empty.");
                        Require(surface.NumberOfTriangles > 0, "Loaded surface has no triangle.");
                        checksum = Mix(checksum, (ulong)surface.NumberOfVertices);
                        checksum = Mix(checksum, (ulong)surface.NumberOfTriangles);
                    }
                    return checksum;
                });
        }

        private static NativePerformanceScenario CloneSurfaceScenario(string path)
        {
            Surface source = LoadSurface(path, gifti: false);
            int expectedVertices = source.NumberOfVertices;
            int expectedTriangles = source.NumberOfTriangles;
            return new NativePerformanceScenario(
                "surface.clone",
                "surface",
                "compute",
                "Clone an OBJ grid with 4,096 vertices / 7,938 triangles",
                1,
                () =>
                {
                    using Surface clone = (Surface)source.Clone();
                    Require(clone.NumberOfVertices == expectedVertices, "Clone vertex count mismatch.");
                    Require(clone.NumberOfTriangles == expectedTriangles, "Clone triangle count mismatch.");
                    return (ulong)clone.NumberOfVertices ^ ((ulong)clone.NumberOfTriangles << 32);
                },
                dispose: source.Dispose);
        }

        private static NativePerformanceScenario AppendSurfaceScenario(string path, string addedPath)
        {
            Surface source = LoadSurface(path, gifti: false);
            Surface added = LoadSurface(addedPath, gifti: false);
            int expectedVertices = source.NumberOfVertices * 2;
            int expectedTriangles = source.NumberOfTriangles * 2;
            return new NativePerformanceScenario(
                "surface.append",
                "surface",
                "compute",
                "Append two OBJ grids of 4,096 vertices / 7,938 triangles",
                1,
                () =>
                {
                    using Surface target = (Surface)source.Clone();
                    target.Append(added);
                    Require(target.NumberOfVertices == expectedVertices, "Append vertex count mismatch.");
                    Require(target.NumberOfTriangles == expectedTriangles, "Append triangle count mismatch.");
                    return (ulong)target.NumberOfVertices ^ ((ulong)target.NumberOfTriangles << 32);
                },
                dispose: () =>
                {
                    added.Dispose();
                    source.Dispose();
                });
        }

        private static NativePerformanceScenario VisibilitySurfaceScenario(string path)
        {
            Surface source = LoadSurface(path, gifti: false);
            // The historical invisible-surface extractor expects one normal per
            // vertex. OBJ files without explicit normals must be normalized once
            // before applying a visibility mask.
            source.ComputeNormals();
            int[] mask = new int[source.NumberOfTriangles];
            for (int i = 0; i < mask.Length; ++i)
            {
                mask[i] = i % 3 == 0 ? 0 : 1;
            }
            return new NativePerformanceScenario(
                "surface.visibility",
                "surface",
                "compute",
                "Visibility mask over 7,938 triangles",
                1,
                () =>
                {
                    using Surface invisible = source.UpdateVisibilityMask(mask);
                    Require(invisible.NumberOfTriangles > 0, "Visibility extraction returned no triangle.");
                    return (ulong)invisible.NumberOfTriangles;
                },
                dispose: source.Dispose);
        }

        private static NativePerformanceScenario SimplifySurfaceScenario(string path)
        {
            Surface source = LoadSurface(path, gifti: false);
            const int targetTriangles = 4000;
            return new NativePerformanceScenario(
                "surface.simplify",
                "surface",
                "compute",
                "Simplify 16,384 vertices / 32,258 triangles to 4,000 triangles",
                1,
                () =>
                {
                    using Surface simplified = source.Simplify(targetTriangles, 7);
                    Require(simplified.NumberOfTriangles > 0, "Simplification returned no triangle.");
                    Require(simplified.NumberOfTriangles <= targetTriangles + 8, "Simplification missed its target.");
                    return (ulong)simplified.NumberOfTriangles;
                },
                dispose: source.Dispose);
        }

        private static NativePerformanceScenario CopySurfaceScenario(string path)
        {
            const int operations = 20;
            Surface source = LoadSurface(path, gifti: false);
            Mesh mesh = new() { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            return new NativePerformanceScenario(
                "surface.copy.unity-mesh",
                "surface",
                "copy",
                "20 aggregated copies of 4,096 vertices / 7,938 triangles from native surface to Unity Mesh",
                operations,
                () =>
                {
                    ulong checksum = 0;
                    for (int operation = 0; operation < operations; ++operation)
                    {
                        source.UpdateMeshFromDLL(mesh);
                        Require(mesh.vertexCount == source.NumberOfVertices, "Unity mesh vertex count mismatch.");
                        Require(mesh.triangles.Length == source.NumberOfTriangles * 3, "Unity mesh triangle count mismatch.");
                        checksum = Mix(checksum, (ulong)mesh.vertexCount ^ ((ulong)mesh.triangles.Length << 32));
                    }
                    return checksum;
                },
                dispose: () =>
                {
                    UnityEngine.Object.DestroyImmediate(mesh);
                    source.Dispose();
                });
        }

        private static NativePerformanceScenario CutSurfaceScenario(
            string path,
            bool onePlane,
            string fixtureName,
            int operations)
        {
            Surface source = LoadSurface(path, gifti: false);
            // hbp_export's cut implementation interpolates vertex normals.
            source.ComputeNormals();
            Vector3 center = source.Center;
            // Legacy BBox accessors expose native-space coordinates, while Cut
            // expects Unity-space coordinates and performs the X conversion at
            // the native boundary. This is observable on the offset MNI mesh.
            if (!source.UsesHbpCore)
            {
                center.x = -center.x;
            }
            HBP.Core.Object3D.Cut x = new(center, Vector3.right);
            HBP.Core.Object3D.Cut y = new(center, Vector3.up);
            HBP.Core.Object3D.Cut[] cuts = onePlane ? new[] { x } : new[] { x, y };
            return new NativePerformanceScenario(
                $"cut.surface.{fixtureName}-{(onePlane ? "one-plane" : "two-planes")}",
                "cut",
                "compute",
                $"{source.NumberOfVertices:N0}-vertex {fixtureName}, {(onePlane ? "one plane" : "two planes")}, strong cuts",
                operations,
                () =>
                {
                    ulong checksum = 0;
                    for (int operation = 0; operation < operations; ++operation)
                    {
                        Surface[] outputs = source.Cut(cuts, noHoles: false, strongCuts: true);
                        try
                        {
                            Require(outputs.Length > 0, "Surface cut returned no output.");
                            int triangles = outputs.Sum(output => output.NumberOfTriangles);
                            Require(triangles > 0, "Surface cut output contains no triangle.");
                            checksum = Mix(checksum, (ulong)triangles);
                        }
                        finally
                        {
                            foreach (Surface output in outputs)
                            {
                                output.Dispose();
                            }
                        }
                    }
                    return checksum;
                },
                dispose: () =>
                {
                    y.Dispose();
                    x.Dispose();
                    source.Dispose();
                });
        }

        private static NativePerformanceScenario LoadAtlasScenario(NativePerformanceBenchmarkFixtures fixtures)
        {
            return new NativePerformanceScenario(
                "atlas.load.mars",
                "atlas",
                "load",
                "32x32x32 label volume and 124 MarsAtlas entries",
                1,
                () =>
                {
                    using MarsAtlas atlas = LoadAtlas(fixtures);
                    int[] labels = atlas.Labels();
                    Require(labels.Length >= 124, "MarsAtlas labels are incomplete.");
                    return Hash(labels);
                });
        }

        private static NativePerformanceScenario QueryAtlasScenario(NativePerformanceBenchmarkFixtures fixtures)
        {
            MarsAtlas atlas = LoadAtlas(fixtures);
            const int queries = 5000;
            Vector3[] points = new Vector3[queries];
            for (int i = 0; i < points.Length; ++i)
            {
                points[i] = new Vector3(-(i * 13 % 32), i * 7 % 32, i * 3 % 32);
            }
            return new NativePerformanceScenario(
                "atlas.query.unit",
                "atlas",
                "compute",
                "5,000 deterministic closest-area queries",
                queries,
                () =>
                {
                    ulong checksum = 0;
                    for (int i = 0; i < points.Length; ++i)
                    {
                        int label = atlas.GetClosestAreaIndex(points[i], 0);
                        Require(label >= -1 && label <= 124, "Atlas query returned an invalid label.");
                        checksum = Mix(checksum, unchecked((ulong)(label + 1)));
                    }
                    return checksum;
                },
                dispose: atlas.Dispose);
        }

        private static NativePerformanceScenario BatchAtlasScenario(NativePerformanceBenchmarkFixtures fixtures)
        {
            MarsAtlas atlas = LoadAtlas(fixtures);
            Surface surface = LoadSurface(fixtures.Grid64Obj, gifti: false);
            return new NativePerformanceScenario(
                "atlas.query.surface-batch",
                "atlas",
                "compute+copy",
                "Area labels for 4,096 surface vertices",
                1,
                () =>
                {
                    int[] labels = atlas.GetSurfaceAreaLabels(surface);
                    Require(labels.Length == surface.NumberOfVertices, "Atlas batch result length mismatch.");
                    Require(labels.All(label => label >= -1 && label <= 124), "Atlas batch returned an invalid label.");
                    return Hash(labels);
                },
                dispose: () =>
                {
                    surface.Dispose();
                    atlas.Dispose();
                });
        }

        private static NativePerformanceScenario GeneratorSurfaceInitializationScenario(
            NativePerformanceBenchmarkFixtures fixtures)
        {
            const int operations = 3;
            Surface surface = LoadSurface(fixtures.Grid128Obj, gifti: false);
            Volume volume = LoadVolume(fixtures.LargeNifti);
            return new NativePerformanceScenario(
                "activity.generator-surface.initialize",
                "activity",
                "initialize",
                "Create three generator surfaces from 16,384 vertices and a 64x64x64 reference volume",
                operations,
                () =>
                {
                    ulong checksum = 0;
                    for (int operation = 0; operation < operations; ++operation)
                    {
                        using GeneratorSurface generatorSurface = new();
                        generatorSurface.Initialize(surface, volume, 8);
                        checksum = Mix(checksum, (ulong)surface.NumberOfVertices);
                    }
                    return checksum;
                },
                dispose: () =>
                {
                    volume.Dispose();
                    surface.Dispose();
                });
        }

        private static NativePerformanceScenario DensityScenario(
            NativePerformanceBenchmarkFixtures fixtures,
            int siteCount,
            bool largeSurface)
        {
            string surfacePath = largeSurface ? fixtures.Grid128Obj : fixtures.Grid64Obj;
            int vertexCount = largeSurface ? 16384 : 4096;
            Surface surface = LoadSurface(surfacePath, gifti: false);
            Volume volume = LoadVolume(fixtures.LargeNifti);
            GeneratorSurface generatorSurface = new();
            generatorSurface.Initialize(surface, volume, 8);
            RawSiteList sites = CreateSites(siteCount);
            DensityGenerator generator = new();
            generator.Initialize(generatorSurface);
            return new NativePerformanceScenario(
                $"activity.density.vertices-{vertexCount}-sites-{siteCount}",
                "activity",
                "compute",
                $"{vertexCount:N0} surface vertices + 8^3 generated grid; {siteCount} sites; linear radius 8",
                1,
                () =>
                {
                    generator.ComputeActivity(sites, 8.0f, SiteInfluenceByDistanceType.Linear);
                    Require(generator.Progress == 1.0f, "Density progress did not complete.");
                    Require(float.IsFinite(generator.MaxDensity) && generator.MaxDensity > 0.0f, "Density maximum is invalid.");
                    return Hash(generator.MaxDensity);
                },
                dispose: () =>
                {
                    generator.Dispose();
                    sites.Dispose();
                    generatorSurface.Dispose();
                    volume.Dispose();
                    surface.Dispose();
                });
        }

        private static NativePerformanceScenario IeegScenario(
            NativePerformanceBenchmarkFixtures fixtures,
            int siteCount,
            int timelineLength,
            bool largeSurface)
        {
            string surfacePath = largeSurface ? fixtures.Grid128Obj : fixtures.Grid64Obj;
            int vertexCount = largeSurface ? 16384 : 4096;
            Surface surface = LoadSurface(surfacePath, gifti: false);
            Volume volume = LoadVolume(fixtures.LargeNifti);
            GeneratorSurface generatorSurface = new();
            generatorSurface.Initialize(surface, volume, 8);
            RawSiteList sites = CreateSites(siteCount);
            float[] activity = new float[siteCount * timelineLength];
            for (int t = 0; t < timelineLength; ++t)
            {
                for (int site = 0; site < siteCount; ++site)
                {
                    activity[t * siteCount + site] = (float)Math.Sin(site * 0.03 + t * 0.7);
                }
            }
            IEEGGenerator generator = new();
            generator.Initialize(generatorSurface);
            SurfaceGenerator output = new();
            output.Initialize(generator);
            return new NativePerformanceScenario(
                $"activity.ieeg.vertices-{vertexCount}-sites-{siteCount}",
                "activity",
                "compute",
                $"{vertexCount:N0} surface vertices + 8^3 generated grid; {siteCount} sites; {timelineLength} instants; quadratic radius 8",
                1,
                () =>
                {
                    generator.ComputeActivity(sites, 8.0f, activity, timelineLength, siteCount, SiteInfluenceByDistanceType.Quadratic);
                    Require(generator.Progress == 1.0f, "iEEG progress did not complete.");
                    return Hash(generator.Progress);
                },
                validate: () =>
                {
                    generator.AdjustValues(0.0f, -1.0f, 1.0f);
                    output.ComputeActivityUV(timelineLength - 1, 0.25f);
                    Require(output.ActivityUV.Length == surface.NumberOfVertices, "iEEG UV output length mismatch.");
                    Require(output.ActivityUV.All(value => float.IsFinite(value.x) && float.IsFinite(value.y)), "iEEG UV output is non-finite.");
                    return "Finite UV buffers validated independently after a full iEEG computation.";
                },
                dispose: () =>
                {
                    output.Dispose();
                    generator.Dispose();
                    sites.Dispose();
                    generatorSurface.Dispose();
                    volume.Dispose();
                    surface.Dispose();
                });
        }

        private static NativePerformanceScenario VolumeActivityScenario(
            NativePerformanceBenchmarkFixtures fixtures,
            bool fmri,
            bool small)
        {
            const int operations = 25;
            Surface surface = LoadSurface(fixtures.Grid64Obj, gifti: false);
            Volume reference = LoadVolume(fixtures.LargeNifti);
            List<Volume> volumes = new();
            NIFTI nifti = null;
            if (small)
            {
                volumes.Add(LoadVolume(fixtures.SmallNifti));
            }
            else
            {
                nifti = new NIFTI();
                Require(nifti.Load(fixtures.MultiNifti), "Could not load multi-volume activity fixture.");
                for (int i = 0; i < nifti.NumberOfVolumes; ++i)
                {
                    volumes.Add(nifti.ExtractVolume(i));
                }
            }
            List<(Volume, Volume)> pairs = volumes.Select(volume => (volume, volume)).ToList();
            GeneratorSurface generatorSurface = new();
            generatorSurface.Initialize(surface, reference, 8);
            ActivityGenerator generator = fmri ? new FMRIGenerator() : new MEGGenerator();
            generator.Initialize(generatorSurface);
            SurfaceGenerator output = new();
            output.Initialize(generator);
            return new NativePerformanceScenario(
                $"activity.{(fmri ? "fmri" : "meg")}.{(small ? "single-small" : "multivolume")}",
                fmri ? "fmri" : "meg",
                "compute",
                small
                    ? "One 32x32x32 activity/mask volume over 4,096 surface vertices + 8^3 generated grid"
                    : "Four 48x48x48 activity/mask volumes over 4,096 surface vertices + 8^3 generated grid",
                operations,
                () =>
                {
                    for (int operation = 0; operation < operations; ++operation)
                    {
                        if (generator is FMRIGenerator fmriGenerator)
                        {
                            fmriGenerator.ComputeActivity(pairs);
                        }
                        else
                        {
                            ((MEGGenerator)generator).ComputeActivity(pairs);
                        }
                    }
                    Require(generator.Progress == 1.0f, "Volume activity progress did not complete.");
                    return Hash(generator.Progress);
                },
                validate: () =>
                {
                    if (generator is FMRIGenerator fmriGenerator)
                    {
                        fmriGenerator.AdjustValues(0.25f, 0.75f, 0.25f, 0.75f);
                        fmriGenerator.HideExtremeValues(false, false, false);
                    }
                    else
                    {
                        MEGGenerator meg = (MEGGenerator)generator;
                        meg.AdjustValues(0.25f, 0.75f, 0.25f, 0.75f);
                        meg.HideExtremeValues(false, false, false);
                    }
                    output.ComputeActivityUV(small ? 0 : 3, 0.25f);
                    Require(output.ActivityUV.Length == surface.NumberOfVertices, "Volume activity UV output length mismatch.");
                    Require(output.ActivityUV.All(value => float.IsFinite(value.x) && float.IsFinite(value.y)), "Volume activity UV output is non-finite.");
                    return $"Finite UV buffers validated independently after {(small ? "single-volume" : "four-volume")} activity computation.";
                },
                dispose: () =>
                {
                    output.Dispose();
                    generator.Dispose();
                    generatorSurface.Dispose();
                    foreach (Volume volume in volumes)
                    {
                        volume.Dispose();
                    }
                    nifti?.Dispose();
                    reference.Dispose();
                    surface.Dispose();
                });
        }

        private static NativePerformanceScenario CutTextureScenario(
            BenchmarkBackend backend,
            NativePerformanceBenchmarkFixtures fixtures,
            bool activityOverlay)
        {
            const int operations = 100;
            Volume volume = LoadVolume(fixtures.LargeNifti);
            Surface surface = LoadSurface(fixtures.Grid64Obj, gifti: false);
            GeneratorSurface generatorSurface = new();
            generatorSurface.Initialize(surface, volume, 8);
            RawSiteList sites = CreateSites(256);
            DensityGenerator activity = new();
            activity.Initialize(generatorSurface);
            activity.ComputeActivity(sites, 8.0f, SiteInfluenceByDistanceType.Linear);
            HBP.Core.Object3D.Cut cut = new(volume.Center, volume.GetOrientationVector(CutOrientation.Axial, false))
            {
                Orientation = CutOrientation.Axial,
                Flip = false,
                Position = 0.5f
            };
            CutGeometryGenerator geometry = new();
            geometry.Initialize(volume, cut, 512);
            CutGenerator generator = new();
            generator.Initialize(activity, geometry, 4);
            LegacyTextureBridge legacyColorScheme = backend == BenchmarkBackend.HbpExport
                ? LegacyTextureBridge.Generate1D((int)(activityOverlay ? ColorType.MatLab : ColorType.Grayscale))
                : null;
            Color32[] coreColorScheme = backend == BenchmarkBackend.HbpCore
                ? UnityTextureFactory.Generate1DColorPixels(activityOverlay ? ColorType.MatLab : ColorType.Grayscale)
                : null;
            LegacyTextureBridge legacyBaseColorScheme = null;
            if (activityOverlay)
            {
                if (backend == BenchmarkBackend.HbpExport)
                {
                    legacyBaseColorScheme = LegacyTextureBridge.Generate1D((int)ColorType.Grayscale);
                    LegacyCutGeneratorBridge.FillTextureWithVolume(generator, legacyBaseColorScheme, -2.0f, 2.0f);
                }
                else
                {
                    Color32[] coreBaseColorScheme = UnityTextureFactory.Generate1DColorPixels(ColorType.Grayscale);
                    generator.FillTextureWithVolume(coreBaseColorScheme, -2.0f, 2.0f);
                }
            }

            return new NativePerformanceScenario(
                activityOverlay ? "cut.texture.activity-blur" : "cut.texture.volume-blur",
                "cut-texture",
                "compute+copy",
                activityOverlay
                    ? "100 aggregated 64x64 density overlays with blur factor 4 and managed RGBA copy"
                    : "100 aggregated 64x64 volume cuts with blur factor 4 and managed RGBA copy",
                operations,
                () =>
                {
                    ulong checksum = 0;
                    for (int operation = 0; operation < operations; ++operation)
                    {
                        Color32[] pixels;
                        if (backend == BenchmarkBackend.HbpExport)
                        {
                            using LegacyTextureBridge output = new();
                            if (activityOverlay)
                            {
                                LegacyCutGeneratorBridge.FillTextureWithActivity(generator, legacyColorScheme, 0, 0.5f);
                                LegacyCutGeneratorBridge.UpdateTextureWithActivity(generator, output);
                            }
                            else
                            {
                                LegacyCutGeneratorBridge.FillTextureWithVolume(generator, legacyColorScheme, -2.0f, 2.0f);
                                LegacyCutGeneratorBridge.UpdateTextureWithVolume(generator, output);
                            }
                            pixels = output.GetPixels(out int width, out int height);
                            Require(width > 0 && height > 0, "Legacy cut texture dimensions are invalid.");
                        }
                        else if (activityOverlay)
                        {
                            generator.FillTextureWithActivity(coreColorScheme, 0, 0.5f);
                            pixels = generator.CopyOverlayPixels();
                        }
                        else
                        {
                            generator.FillTextureWithVolume(coreColorScheme, -2.0f, 2.0f);
                            pixels = generator.CopyBasePixels();
                        }
                        Require(pixels.Length > 0, "Cut texture is empty.");
                        checksum = Mix(checksum, Hash(pixels));
                    }
                    return checksum;
                },
                dispose: () =>
                {
                    legacyBaseColorScheme?.Dispose();
                    legacyColorScheme?.Dispose();
                    generator.Dispose();
                    geometry.Dispose();
                    cut.Dispose();
                    activity.Dispose();
                    sites.Dispose();
                    generatorSurface.Dispose();
                    surface.Dispose();
                    volume.Dispose();
                });
        }

        private static RawSiteList CreateSites(int siteCount)
        {
            RawSiteList sites = new();
            for (int i = 0; i < siteCount; ++i)
            {
                float x = (i * 37 % 6300) / 100.0f;
                float y = (i * 53 % 6300) / 100.0f;
                float z = 31.5f + 2.0f * (float)Math.Sin(i * 0.11);
                sites.AddSite($"S{i}", new Vector3(x, y, z), 0, i);
                sites.UpdateMask(i, false);
            }
            return sites;
        }

        private static Volume LoadVolume(string path)
        {
            Volume volume = new();
            if (!volume.LoadNIFTIFile(path))
            {
                volume.Dispose();
                throw new InvalidOperationException($"Could not load volume {path}.");
            }
            return volume;
        }

        private static Surface LoadSurface(string path, bool gifti)
        {
            Surface surface = new();
            bool loaded = gifti ? surface.LoadGIIFile(path) : surface.LoadOBJFile(path);
            if (!loaded)
            {
                surface.Dispose();
                throw new InvalidOperationException($"Could not load surface {path}.");
            }
            return surface;
        }

        private static MarsAtlas LoadAtlas(NativePerformanceBenchmarkFixtures fixtures)
        {
            MarsAtlas atlas = new();
            if (!atlas.Load(fixtures.MarsIndex, fixtures.Brodmann, fixtures.AtlasNifti))
            {
                atlas.Dispose();
                throw new InvalidOperationException("Could not load MarsAtlas benchmark fixture.");
            }
            return atlas;
        }

        private static ulong Hash(float value)
        {
            return unchecked((ulong)(uint)Mathf.RoundToInt(value * 100000.0f));
        }

        private static ulong Hash(Vector3 value)
        {
            return Mix(Mix(Hash(value.x), Hash(value.y)), Hash(value.z));
        }

        private static ulong Hash(float[] values)
        {
            ulong checksum = (ulong)values.Length;
            int stride = Math.Max(1, values.Length / 64);
            for (int i = 0; i < values.Length; i += stride)
            {
                checksum = Mix(checksum, Hash(values[i]));
            }
            return checksum;
        }

        private static ulong Hash(int[] values)
        {
            ulong checksum = (ulong)values.Length;
            int stride = Math.Max(1, values.Length / 64);
            for (int i = 0; i < values.Length; i += stride)
            {
                checksum = Mix(checksum, unchecked((ulong)(uint)values[i]));
            }
            return checksum;
        }

        private static ulong Hash(Color32[] values)
        {
            ulong checksum = (ulong)values.Length;
            int stride = Math.Max(1, values.Length / 64);
            for (int i = 0; i < values.Length; i += stride)
            {
                Color32 color = values[i];
                ulong packed = color.r | ((ulong)color.g << 8) | ((ulong)color.b << 16) | ((ulong)color.a << 24);
                checksum = Mix(checksum, packed);
            }
            return checksum;
        }

        private static Color32[] CreateTexturePixels(int width, int height)
        {
            Color32[] pixels = new Color32[width * height];
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    int index = y * width + x;
                    pixels[index] = new Color32(
                        (byte)((x * 17 + y * 3) & 0xff),
                        (byte)((x * 5 + y * 11) & 0xff),
                        (byte)((x * 7 + y * 13) & 0xff),
                        255);
                }
            }
            return pixels;
        }

        private static Texture2D CreateUnityTexture(Color32[] pixels, int width, int height)
        {
            Texture2D texture = new(width, height, TextureFormat.RGBA32, false, false);
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private static ulong Mix(ulong seed, ulong value)
        {
            return (seed ^ value) * 1099511628211UL;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
