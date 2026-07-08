using System;
using HBP.Core.DLL;
using HBP.Core.Enums;
using HBP.Core.Tools;
using HBP.Tests.Serialization.Helpers;
using NUnit.Framework;
using UnityEngine;

namespace HBP.Tests.Serialization
{
    public class NativeParityCutVisualTests
    {
        [Test]
        [Category("NativeMigration")]
        [Category("NativeParity")]
        public void CutGeometryBoundingBoxesAndPositionRatios_MatchAcrossBackends()
        {
            NativeParityAssert.RequireHbpCore();

            foreach (CutOrientation orientation in new[] { CutOrientation.Axial, CutOrientation.Coronal, CutOrientation.Sagittal })
            {
                foreach (bool flip in new[] { false, true })
                {
                    using Volume hbpExportVolume = LoadVolume(NativeBackend.HbpExport);
                    using Volume hbpCoreVolume = LoadVolume(NativeBackend.HbpCore);
                    using HBP.Core.Object3D.Cut hbpExportCut = CreateCut(hbpExportVolume, orientation, flip);
                    using HBP.Core.Object3D.Cut hbpCoreCut = CreateCut(hbpCoreVolume, orientation, flip);
                    using CutGeometryGenerator hbpExportGeometry = InitializeCutGeometry(NativeBackend.HbpExport, hbpExportVolume, hbpExportCut, 8);
                    using CutGeometryGenerator hbpCoreGeometry = InitializeCutGeometry(NativeBackend.HbpCore, hbpCoreVolume, hbpCoreCut, 8);

                    using BBox hbpExportBBox = hbpExportGeometry.BoundingBox;
                    using BBox hbpCoreBBox = hbpCoreGeometry.BoundingBox;
                    NativeParityAssert.AssertVector(hbpCoreBBox.Min, hbpExportBBox.Min, 0.0002f);
                    NativeParityAssert.AssertVector(hbpCoreBBox.Max, hbpExportBBox.Max, 0.0002f);

                    Vector2Int textureSize = hbpCoreGeometry.TextureSize;
                    Assert.That(textureSize.x, Is.GreaterThan(0), $"{orientation} {flip}");
                    Assert.That(textureSize.y, Is.GreaterThan(0), $"{orientation} {flip}");
                    Assert.That(textureSize.x, Is.LessThanOrEqualTo(8), $"{orientation} {flip}");
                    Assert.That(textureSize.y, Is.LessThanOrEqualTo(8), $"{orientation} {flip}");

                    foreach (Vector3 point in new[] { hbpExportBBox.Center, hbpExportBBox.Min, hbpExportBBox.Max })
                    {
                        Vector2 hbpCoreRatio = hbpCoreGeometry.GetPositionRatioOnTexture(point);
                        Vector2 hbpExportRatio = hbpExportGeometry.GetPositionRatioOnTexture(point);
                        Assert.That(hbpCoreRatio.x, Is.EqualTo(hbpExportRatio.x).Within(0.0002f), $"{orientation} {flip} {point} ratio.x");
                        Assert.That(hbpCoreRatio.y, Is.EqualTo(hbpExportRatio.y).Within(0.0002f), $"{orientation} {flip} {point} ratio.y");
                    }
                }
            }
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeParity")]
        public void CutGeometrySurfaceUvs_MatchAcrossBackends()
        {
            NativeParityAssert.RequireHbpCore();

            using Volume hbpExportVolume = LoadVolume(NativeBackend.HbpExport);
            using Volume hbpCoreVolume = LoadVolume(NativeBackend.HbpCore);
            using HBP.Core.Object3D.Cut hbpExportCut = CreateCut(hbpExportVolume, CutOrientation.Axial, flip: false);
            using HBP.Core.Object3D.Cut hbpCoreCut = CreateCut(hbpCoreVolume, CutOrientation.Axial, flip: false);
            using CutGeometryGenerator hbpExportGeometry = InitializeCutGeometry(NativeBackend.HbpExport, hbpExportVolume, hbpExportCut, 8);
            using CutGeometryGenerator hbpCoreGeometry = InitializeCutGeometry(NativeBackend.HbpCore, hbpCoreVolume, hbpCoreCut, 8);
            using Surface hbpExportSurface = LoadSurface(NativeBackend.HbpExport);
            using Surface hbpCoreSurface = LoadSurface(NativeBackend.HbpCore);

            hbpExportGeometry.UpdateSurfaceUV(hbpExportSurface);
            hbpCoreGeometry.UpdateSurfaceUV(hbpCoreSurface);

            Mesh hbpExportMesh = new();
            Mesh hbpCoreMesh = new();
            try
            {
                hbpExportSurface.UpdateMeshFromDLL(hbpExportMesh, all: false, uv: true);
                hbpCoreSurface.UpdateMeshFromDLL(hbpCoreMesh, all: false, uv: true);
                NativeParityAssert.AssertSameVectorArray(hbpCoreMesh.uv, hbpExportMesh.uv, 0.0002f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(hbpExportMesh);
                UnityEngine.Object.DestroyImmediate(hbpCoreMesh);
            }
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeParity")]
        public void CutVolumeBasePixels_MatchAcrossBackends()
        {
            NativeParityAssert.RequireHbpCore();

            CutPixels hbpExportPixels = RenderVolumeCut(NativeBackend.HbpExport, CutOrientation.Axial, flip: false);
            CutPixels hbpCorePixels = RenderVolumeCut(NativeBackend.HbpCore, CutOrientation.Axial, flip: false);

            Assert.That(hbpCorePixels.Width, Is.EqualTo(hbpExportPixels.Width));
            Assert.That(hbpCorePixels.Height, Is.EqualTo(hbpExportPixels.Height));
            NativeParityAssert.AssertSameColor32Array(hbpCorePixels.Pixels, hbpExportPixels.Pixels, tolerance: 1);
        }

        private static CutPixels RenderVolumeCut(NativeBackend backend, CutOrientation orientation, bool flip)
        {
            return NativeParityAssert.WithBackend(
                backend,
                () =>
                {
                    using Volume volume = LoadVolume(backend);
                    using HBP.Core.Object3D.Cut cut = CreateCut(volume, orientation, flip);
                    using CutGeometryGenerator geometry = InitializeCutGeometry(backend, volume, cut, 8);
                    using Surface surface = LoadSurface(backend);
                    using GeneratorSurface generatorSurface = new();
                    generatorSurface.Initialize(surface, volume, 8);
                    using DensityGenerator activity = new();
                    activity.Initialize(generatorSurface);
                    using CutGenerator cutGenerator = new();
                    cutGenerator.Initialize(activity, geometry, 0);

                    Color32[] colorScheme = UnityTextureFactory.Generate1DColorPixels(ColorType.BrainColor);
                    if (backend == NativeBackend.HbpCore)
                    {
                        cutGenerator.FillTextureWithVolume(colorScheme, 0.0f, 124.0f);
                        Vector2Int size = geometry.TextureSize;
                        return new CutPixels(size.x, size.y, cutGenerator.CopyBasePixels());
                    }

                    using HBP.Core.DLL.Texture colorSchemeTexture = HBP.Core.DLL.Texture.CreateFromPixels(colorScheme, UnityTextureFactory.ColormapSize, 1);
                    using HBP.Core.DLL.Texture outputTexture = new();
                    cutGenerator.FillTextureWithVolume(colorSchemeTexture, 0.0f, 124.0f);
                    cutGenerator.UpdateTextureWithVolume(outputTexture);
                    Texture2D texture = new(1, 1, TextureFormat.RGBA32, false);
                    try
                    {
                        outputTexture.UpdateTexture2D(texture);
                        return new CutPixels(texture.width, texture.height, texture.GetPixels32());
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(texture);
                    }
                });
        }

        private static Volume LoadVolume(NativeBackend backend)
        {
            return NativeParityAssert.WithBackend(
                backend,
                () =>
                {
                    Volume volume = new();
                    try
                    {
                        Assert.That(volume.LoadNIFTIFile(NativeParityAssert.NativePath("Nifti", "fmri_3d.nii")), Is.True);
                        return volume;
                    }
                    catch
                    {
                        volume.Dispose();
                        throw;
                    }
                });
        }

        private static Surface LoadSurface(NativeBackend backend)
        {
            return NativeParityAssert.WithBackend(
                backend,
                () =>
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
                });
        }

        private static CutGeometryGenerator InitializeCutGeometry(NativeBackend backend, Volume volume, HBP.Core.Object3D.Cut cut, int maxTextureSize)
        {
            return NativeParityAssert.WithBackend(
                backend,
                () =>
                {
                    CutGeometryGenerator generator = new();
                    try
                    {
                        generator.Initialize(volume, cut, maxTextureSize);
                        return generator;
                    }
                    catch
                    {
                        generator.Dispose();
                        throw;
                    }
                });
        }

        private static HBP.Core.Object3D.Cut CreateCut(Volume volume, CutOrientation orientation, bool flip)
        {
            return new HBP.Core.Object3D.Cut(volume.Center, volume.GetOrientationVector(orientation, flip))
            {
                Orientation = orientation,
                Flip = flip,
                Position = 0.5f,
                NumberOfCuts = 8
            };
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
    }
}
