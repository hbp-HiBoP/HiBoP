using System;
using System.Collections.Generic;
using System.IO;
using HBP.Core.DLL;
using HBP.Core.Enums;
using HBP.Core.Tools;
using HBP.Tests.Serialization.Helpers;
using NUnit.Framework;
using UnityEngine;
using CutGenerator = HBP.Tests.Serialization.LegacyNative.CutGenerator;
using CutGeometryGenerator = HBP.Tests.Serialization.LegacyNative.CutGeometryGenerator;
using DensityGenerator = HBP.Tests.Serialization.LegacyNative.DensityGenerator;
using FMRIGenerator = HBP.Tests.Serialization.LegacyNative.FMRIGenerator;
using GeneratorSurface = HBP.Tests.Serialization.LegacyNative.GeneratorSurface;
using MarsAtlas = HBP.Tests.Serialization.LegacyNative.MarsAtlas;
using Surface = HBP.Tests.Serialization.LegacyNative.Surface;
using SurfaceGenerator = HBP.Tests.Serialization.LegacyNative.SurfaceGenerator;
using Volume = HBP.Tests.Serialization.LegacyNative.Volume;

namespace HBP.Tests.Serialization
{
    [LegacyParityOnly]
    public class NativeParityWorkflowArtifactTests
    {
        [Test]
        [Category("NativeMigration")]
        [Category("NativeParity")]
        [Category(NativeParityAssert.NormalizedCoordinateParity)]
        public void RepresentativeNativeWorkflowArtifact_MatchesAcrossBackends()
        {
            NativeParityAssert.RequireHbpCore();

            WorkflowArtifact hbpExportArtifact = CaptureWorkflowArtifact(BenchmarkBackend.HbpExport);
            WorkflowArtifact hbpCoreArtifact = CaptureWorkflowArtifact(BenchmarkBackend.HbpCore);

            NativeParityAssert.AssertVector(hbpCoreArtifact.VolumeCenter, hbpExportArtifact.VolumeCenter, context: "workflow normalized Unity volume center");
            NativeParityAssert.AssertVector(hbpCoreArtifact.VolumeCenter, new Vector3(-2.0f, 2.0f, 2.0f), context: "workflow fmri_3d fixture center in Unity");
            NativeParityAssert.AssertVector(hbpCoreArtifact.VolumeSpacing, hbpExportArtifact.VolumeSpacing);
            NativeParityAssert.AssertMriCalValues(hbpCoreArtifact.VolumeExtrema, hbpExportArtifact.VolumeExtrema);

            Assert.That(hbpCoreArtifact.SurfaceVertexCount, Is.EqualTo(hbpExportArtifact.SurfaceVertexCount));
            Assert.That(hbpCoreArtifact.SurfaceTriangleCount, Is.EqualTo(hbpExportArtifact.SurfaceTriangleCount));
            Assert.That(hbpCoreArtifact.SurfaceVertexHash, Is.EqualTo(hbpExportArtifact.SurfaceVertexHash));
            Assert.That(hbpCoreArtifact.SurfaceTriangleHash, Is.EqualTo(hbpExportArtifact.SurfaceTriangleHash));

            Assert.That(hbpCoreArtifact.CutTextureWidth, Is.EqualTo(hbpExportArtifact.CutTextureWidth));
            Assert.That(hbpCoreArtifact.CutTextureHeight, Is.EqualTo(hbpExportArtifact.CutTextureHeight));
            Assert.That(hbpCoreArtifact.CutBasePixelHash, Is.EqualTo(hbpExportArtifact.CutBasePixelHash));
            Assert.That(hbpCoreArtifact.CutSurfaceUvHash, Is.EqualTo(hbpExportArtifact.CutSurfaceUvHash));

            Assert.That(hbpCoreArtifact.FmriActivityUvHash, Is.EqualTo(hbpExportArtifact.FmriActivityUvHash));
            Assert.That(hbpCoreArtifact.FmriAlphaUvHash, Is.EqualTo(hbpExportArtifact.FmriAlphaUvHash));

            Assert.That(hbpCoreArtifact.MarsLabelsHash, Is.EqualTo(hbpExportArtifact.MarsLabelsHash));
            Assert.That(hbpCoreArtifact.MarsAreaNamesHash, Is.EqualTo(hbpExportArtifact.MarsAreaNamesHash));
            Assert.That(hbpCoreArtifact.MarsLabelOneName, Is.EqualTo(hbpExportArtifact.MarsLabelOneName));
        }

        private static WorkflowArtifact CaptureWorkflowArtifact(BenchmarkBackend backend)
        {
            return NativeParityAssert.WithBackend(
                backend,
                () =>
                {
                    using Volume volume = LoadVolume("fmri_3d.nii");
                    using Volume activityVolume = LoadVolume("fmri_3d.nii");
                    using Volume maskVolume = LoadVolume("mask_binary.nii");
                    using Surface surface = LoadSurface();
                    using MarsAtlas marsAtlas = LoadMarsAtlas();

                    Mesh mesh = new();
                    Mesh uvMesh = new();
                    try
                    {
                        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                        uvMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                        surface.UpdateMeshFromDLL(mesh);

                        Vector3 rawVolumeCenter = volume.Center;
                        Vector3 unityVolumeCenter = backend == BenchmarkBackend.HbpCore
                            ? rawVolumeCenter
                            : NativeParityAssert.NativeToUnity(rawVolumeCenter);
                        Vector3 rawOrientation = volume.GetOrientationVector(CutOrientation.Axial, flip: false);
                        Vector3 unityOrientation = backend == BenchmarkBackend.HbpCore
                            ? rawOrientation
                            : NativeParityAssert.NativeToUnity(rawOrientation);
                        TestContext.Progress.WriteLine(
                            $"workflow {backend}: raw center={rawVolumeCenter}; normalized Unity center={unityVolumeCenter}; conversion=R=diag({(ReferenceSystemConversion.InvertX ? "-1" : "1")},1,1)");
                        if (backend == BenchmarkBackend.HbpExport)
                        {
                            NativeParityAssert.NormalizeLegacyMeshToUnity(mesh);
                        }

                        using HBP.Core.Object3D.Cut cut = new(unityVolumeCenter, unityOrientation)
                        {
                            Orientation = CutOrientation.Axial,
                            Flip = false,
                            Position = 0.5f,
                            NumberOfCuts = 8
                        };
                        using CutGeometryGenerator cutGeometry = new();
                        cutGeometry.Initialize(volume, cut, maxTextureSize: 8);
                        cutGeometry.UpdateSurfaceUV(surface);
                        surface.UpdateMeshFromDLL(uvMesh, all: false, uv: true);

                        CutPixels cutPixels = RenderVolumeCut(backend, volume, surface, cutGeometry);
                        ActivityUvs fmriUvs = ComputeFmriUvs(surface, volume, activityVolume, maskVolume);

                        return new WorkflowArtifact(
                            unityVolumeCenter,
                            volume.Spacing,
                            volume.ExtremeValues,
                            mesh.vertexCount,
                            mesh.triangles.Length / 3,
                            HashVector3(mesh.vertices),
                            HashInts(mesh.triangles),
                            cutPixels.Width,
                            cutPixels.Height,
                            HashColor32(cutPixels.Pixels),
                            HashVector2(uvMesh.uv),
                            HashVector2(fmriUvs.ActivityUV),
                            HashVector2(fmriUvs.AlphaUV),
                            HashInts(marsAtlas.Labels()),
                            HashStrings(marsAtlas.AreaNames),
                            marsAtlas.GetAreaName(1));
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(mesh);
                        UnityEngine.Object.DestroyImmediate(uvMesh);
                    }
                });
        }

        private static CutPixels RenderVolumeCut(BenchmarkBackend backend, Volume volume, Surface surface, CutGeometryGenerator cutGeometry)
        {
            using GeneratorSurface generatorSurface = new();
            generatorSurface.Initialize(surface, volume, 8);
            using DensityGenerator activity = new();
            activity.Initialize(generatorSurface);
            using CutGenerator cutGenerator = new();
            cutGenerator.Initialize(activity, cutGeometry, 0);

            Color32[] colorScheme = UnityTextureFactory.Generate1DColorPixels(ColorType.BrainColor);
            if (backend == BenchmarkBackend.HbpCore)
            {
                cutGenerator.FillTextureWithVolume(colorScheme, 0.0f, 124.0f);
                Vector2Int size = cutGeometry.TextureSize;
                return new CutPixels(size.x, size.y, cutGenerator.CopyBasePixels());
            }

            using LegacyTextureBridge colorSchemeTexture = LegacyTextureBridge.CreateFromPixels(colorScheme, UnityTextureFactory.ColormapSize, 1);
            using LegacyTextureBridge outputTexture = new();
            LegacyCutGeneratorBridge.FillTextureWithVolume(cutGenerator, colorSchemeTexture, 0.0f, 124.0f);
            LegacyCutGeneratorBridge.UpdateTextureWithVolume(cutGenerator, outputTexture);
            Color32[] pixels = outputTexture.GetPixels(out int width, out int height);
            return new CutPixels(width, height, pixels);
        }

        private static ActivityUvs ComputeFmriUvs(Surface surface, Volume referenceVolume, Volume activityVolume, Volume maskVolume)
        {
            using GeneratorSurface generatorSurface = new();
            generatorSurface.Initialize(surface, referenceVolume, 8);
            using FMRIGenerator fmri = new();
            fmri.Initialize(generatorSurface);
            fmri.ComputeActivity(new[] { (activityVolume, maskVolume) });
            fmri.AdjustValues(-1.0f, -0.25f, 0.25f, 1.0f);

            using SurfaceGenerator surfaceGenerator = new();
            surfaceGenerator.Initialize(fmri);
            surfaceGenerator.ComputeActivityUV(timelineIndex: 0, alpha: 0.25f);
            return new ActivityUvs(surfaceGenerator.ActivityUV, surfaceGenerator.AlphaUV);
        }

        private static Surface LoadSurface()
        {
            Surface surface = new();
            try
            {
                Assert.That(surface.LoadGIIFile(NativeParityAssert.NativePath("Meshes", "single_surface.gii")), Is.True);
                return surface;
            }
            catch
            {
                surface.Dispose();
                throw;
            }
        }

        private static Volume LoadVolume(string fileName)
        {
            Volume volume = new();
            try
            {
                Assert.That(volume.LoadNIFTIFile(NativeParityAssert.NativePath("Nifti", fileName)), Is.True);
                return volume;
            }
            catch
            {
                volume.Dispose();
                throw;
            }
        }

        private static MarsAtlas LoadMarsAtlas()
        {
            MarsAtlas atlas = new();
            try
            {
                Assert.That(atlas.Load(AtlasPath("mars_atlas_index.csv"), AtlasPath("brodmann_areas.txt"), AtlasPath("colin27_MNI_MarsAtlas.nii")), Is.True);
                return atlas;
            }
            catch
            {
                atlas.Dispose();
                throw;
            }
        }

        private static string AtlasPath(string fileName)
        {
            return Path.Combine(TestPathUtility.ProjectRoot, "Assets", "Data", "Atlases", "MarsAtlas", fileName);
        }

        private static int HashVector3(Vector3[] values)
        {
            HashCode hash = new();
            foreach (Vector3 value in values)
            {
                hash.Add(Mathf.RoundToInt(value.x * 10000.0f));
                hash.Add(Mathf.RoundToInt(value.y * 10000.0f));
                hash.Add(Mathf.RoundToInt(value.z * 10000.0f));
            }
            return hash.ToHashCode();
        }

        private static int HashVector2(Vector2[] values)
        {
            HashCode hash = new();
            foreach (Vector2 value in values)
            {
                hash.Add(Mathf.RoundToInt(value.x * 10000.0f));
                hash.Add(Mathf.RoundToInt(value.y * 10000.0f));
            }
            return hash.ToHashCode();
        }

        private static int HashColor32(Color32[] values)
        {
            HashCode hash = new();
            foreach (Color32 value in values)
            {
                hash.Add(value.r);
                hash.Add(value.g);
                hash.Add(value.b);
                hash.Add(value.a);
            }
            return hash.ToHashCode();
        }

        private static int HashInts(int[] values)
        {
            HashCode hash = new();
            foreach (int value in values)
            {
                hash.Add(value);
            }
            return hash.ToHashCode();
        }

        private static int HashStrings(IEnumerable<string> values)
        {
            HashCode hash = new();
            foreach (string value in values)
            {
                hash.Add(value);
            }
            return hash.ToHashCode();
        }

        private readonly struct CutPixels
        {
            public CutPixels(int width, int height, Color32[] pixels)
            {
                Width = width;
                Height = height;
                Pixels = pixels;
            }

            public int Width { get; }
            public int Height { get; }
            public Color32[] Pixels { get; }
        }

        private readonly struct ActivityUvs
        {
            public ActivityUvs(Vector2[] activityUV, Vector2[] alphaUV)
            {
                ActivityUV = (Vector2[])activityUV.Clone();
                AlphaUV = (Vector2[])alphaUV.Clone();
            }

            public Vector2[] ActivityUV { get; }
            public Vector2[] AlphaUV { get; }
        }

        private readonly struct WorkflowArtifact
        {
            public WorkflowArtifact(
                Vector3 volumeCenter,
                Vector3 volumeSpacing,
                MRICalValues volumeExtrema,
                int surfaceVertexCount,
                int surfaceTriangleCount,
                int surfaceVertexHash,
                int surfaceTriangleHash,
                int cutTextureWidth,
                int cutTextureHeight,
                int cutBasePixelHash,
                int cutSurfaceUvHash,
                int fmriActivityUvHash,
                int fmriAlphaUvHash,
                int marsLabelsHash,
                int marsAreaNamesHash,
                string marsLabelOneName)
            {
                VolumeCenter = volumeCenter;
                VolumeSpacing = volumeSpacing;
                VolumeExtrema = volumeExtrema;
                SurfaceVertexCount = surfaceVertexCount;
                SurfaceTriangleCount = surfaceTriangleCount;
                SurfaceVertexHash = surfaceVertexHash;
                SurfaceTriangleHash = surfaceTriangleHash;
                CutTextureWidth = cutTextureWidth;
                CutTextureHeight = cutTextureHeight;
                CutBasePixelHash = cutBasePixelHash;
                CutSurfaceUvHash = cutSurfaceUvHash;
                FmriActivityUvHash = fmriActivityUvHash;
                FmriAlphaUvHash = fmriAlphaUvHash;
                MarsLabelsHash = marsLabelsHash;
                MarsAreaNamesHash = marsAreaNamesHash;
                MarsLabelOneName = marsLabelOneName;
            }

            public Vector3 VolumeCenter { get; }
            public Vector3 VolumeSpacing { get; }
            public MRICalValues VolumeExtrema { get; }
            public int SurfaceVertexCount { get; }
            public int SurfaceTriangleCount { get; }
            public int SurfaceVertexHash { get; }
            public int SurfaceTriangleHash { get; }
            public int CutTextureWidth { get; }
            public int CutTextureHeight { get; }
            public int CutBasePixelHash { get; }
            public int CutSurfaceUvHash { get; }
            public int FmriActivityUvHash { get; }
            public int FmriAlphaUvHash { get; }
            public int MarsLabelsHash { get; }
            public int MarsAreaNamesHash { get; }
            public string MarsLabelOneName { get; }
        }
    }
}
