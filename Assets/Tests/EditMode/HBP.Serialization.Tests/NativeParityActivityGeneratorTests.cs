using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using HBP.Core.DLL;
using HBP.Core.Data;
using HBP.Core.Enums;
using HBP.Tests.Serialization.Helpers;
using NUnit.Framework;
using UnityEngine;
using ActivityGenerator = HBP.Tests.Serialization.LegacyNative.ActivityGenerator;
using DensityGenerator = HBP.Tests.Serialization.LegacyNative.DensityGenerator;
using FMRIGenerator = HBP.Tests.Serialization.LegacyNative.FMRIGenerator;
using GeneratorSurface = HBP.Tests.Serialization.LegacyNative.GeneratorSurface;
using IEEGGenerator = HBP.Tests.Serialization.LegacyNative.IEEGGenerator;
using MarsAtlas = HBP.Tests.Serialization.LegacyNative.MarsAtlas;
using MEGGenerator = HBP.Tests.Serialization.LegacyNative.MEGGenerator;
using RawSiteList = HBP.Tests.Serialization.LegacyNative.RawSiteList;
using Surface = HBP.Tests.Serialization.LegacyNative.Surface;
using SurfaceGenerator = HBP.Tests.Serialization.LegacyNative.SurfaceGenerator;
using Volume = HBP.Tests.Serialization.LegacyNative.Volume;

namespace HBP.Tests.Serialization
{
    [LegacyParityOnly]
    public class NativeParityActivityGeneratorTests
    {
        [Test]
        [Category("NativeMigration")]
        [Category("NativeParity")]
        [Category(NativeParityAssert.StrictParity)]
        public void DensityActivitySurfaceUvs_MatchAcrossBackends()
        {
            NativeParityAssert.RequireHbpCore();

            ActivityUvs hbpExportUvs = ComputeDensityUvs(BenchmarkBackend.HbpExport);
            ActivityUvs hbpCoreUvs = ComputeDensityUvs(BenchmarkBackend.HbpCore);

            Assert.That(hbpCoreUvs.MaxDensity, Is.EqualTo(hbpExportUvs.MaxDensity).Within(0.0005f));
            NativeParityAssert.AssertSameVectorArray(hbpCoreUvs.ActivityUV, hbpExportUvs.ActivityUV, 0.0005f);
            NativeParityAssert.AssertSameVectorArray(hbpCoreUvs.AlphaUV, hbpExportUvs.AlphaUV, 0.0005f);
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeParity")]
        [Category(NativeParityAssert.StrictParity)]
        public void IEEGActivitySurfaceUvs_MatchAcrossBackends()
        {
            NativeParityAssert.RequireHbpCore();

            ActivityUvs hbpExportTimeline0 = ComputeIeegUvs(BenchmarkBackend.HbpExport, timelineIndex: 0);
            ActivityUvs hbpCoreTimeline0 = ComputeIeegUvs(BenchmarkBackend.HbpCore, timelineIndex: 0);
            ActivityUvs hbpExportTimeline1 = ComputeIeegUvs(BenchmarkBackend.HbpExport, timelineIndex: 1);
            ActivityUvs hbpCoreTimeline1 = ComputeIeegUvs(BenchmarkBackend.HbpCore, timelineIndex: 1);

            NativeParityAssert.AssertSameVectorArray(hbpCoreTimeline0.ActivityUV, hbpExportTimeline0.ActivityUV, 0.0005f);
            NativeParityAssert.AssertSameVectorArray(hbpCoreTimeline0.AlphaUV, hbpExportTimeline0.AlphaUV, 0.0005f);
            NativeParityAssert.AssertSameVectorArray(hbpCoreTimeline1.ActivityUV, hbpExportTimeline1.ActivityUV, 0.0005f);
            NativeParityAssert.AssertSameVectorArray(hbpCoreTimeline1.AlphaUV, hbpExportTimeline1.AlphaUV, 0.0005f);
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeParity")]
        [Category(NativeParityAssert.StrictParity)]
        public void IEEGNiftiExports_MatchAcrossBackends()
        {
            NativeParityAssert.RequireHbpCore();

            string tempDirectory = Path.Combine(Path.GetTempPath(), $"hibop_nifti_export_parity_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDirectory);
            try
            {
                string hbpExportActivityPath = Path.Combine(tempDirectory, "hbp_export_activity.nii.gz");
                string hbpExportMaskPath = Path.Combine(tempDirectory, "hbp_export_mask.nii.gz");
                string hbpCoreActivityPath = Path.Combine(tempDirectory, "hbp_core_activity.nii.gz");
                string hbpCoreMaskPath = Path.Combine(tempDirectory, "hbp_core_mask.nii.gz");

                ExportIeegNifti(BenchmarkBackend.HbpExport, hbpExportActivityPath, hbpExportMaskPath);
                ExportIeegNifti(BenchmarkBackend.HbpCore, hbpCoreActivityPath, hbpCoreMaskPath);

                NiftiFileSnapshot hbpExportActivity = NiftiFileSnapshot.Read(hbpExportActivityPath);
                NiftiFileSnapshot hbpCoreActivity = NiftiFileSnapshot.Read(hbpCoreActivityPath);
                AssertSameNiftiSnapshot(hbpCoreActivity, hbpExportActivity, compareTimeline: true);

                NiftiFileSnapshot hbpExportMask = NiftiFileSnapshot.Read(hbpExportMaskPath);
                NiftiFileSnapshot hbpCoreMask = NiftiFileSnapshot.Read(hbpCoreMaskPath);
                AssertSameNiftiSnapshot(hbpCoreMask, hbpExportMask, compareTimeline: false);
            }
            finally
            {
                if (Directory.Exists(tempDirectory))
                {
                    Directory.Delete(tempDirectory, recursive: true);
                }
            }
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeParity")]
        [Category(NativeParityAssert.StrictParity)]
        public void FmriAndMegActivitySurfaceUvs_MatchAcrossBackends()
        {
            NativeParityAssert.RequireHbpCore();

            ActivityUvs hbpExportFmri = ComputeVolumeActivityUvs(BenchmarkBackend.HbpExport, GeneratorKind.Fmri);
            ActivityUvs hbpCoreFmri = ComputeVolumeActivityUvs(BenchmarkBackend.HbpCore, GeneratorKind.Fmri);
            ActivityUvs hbpExportMeg = ComputeVolumeActivityUvs(BenchmarkBackend.HbpExport, GeneratorKind.Meg);
            ActivityUvs hbpCoreMeg = ComputeVolumeActivityUvs(BenchmarkBackend.HbpCore, GeneratorKind.Meg);

            NativeParityAssert.AssertSameVectorArray(hbpCoreFmri.ActivityUV, hbpExportFmri.ActivityUV, 0.0005f);
            NativeParityAssert.AssertSameVectorArray(hbpCoreFmri.AlphaUV, hbpExportFmri.AlphaUV, 0.0005f);
            NativeParityAssert.AssertSameVectorArray(hbpCoreMeg.ActivityUV, hbpExportMeg.ActivityUV, 0.0005f);
            NativeParityAssert.AssertSameVectorArray(hbpCoreMeg.AlphaUV, hbpExportMeg.AlphaUV, 0.0005f);

            NativeParityAssert.AssertSameVectorArray(hbpCoreMeg.ActivityUV, hbpCoreFmri.ActivityUV, 0.0005f);
            NativeParityAssert.AssertSameVectorArray(hbpCoreMeg.AlphaUV, hbpCoreFmri.AlphaUV, 0.0005f);
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeParity")]
        [Category(NativeParityAssert.StrictParity)]
        public void DensityAndIeegEveryDistanceMode_MatchAcrossBackends()
        {
            NativeParityAssert.RequireHbpCore();

            foreach (SiteInfluenceByDistanceType mode in Enum.GetValues(typeof(SiteInfluenceByDistanceType)))
            {
                ActivityUvs exportDensity = ComputeDensityUvs(BenchmarkBackend.HbpExport, mode);
                ActivityUvs coreDensity = ComputeDensityUvs(BenchmarkBackend.HbpCore, mode);
                Assert.That(coreDensity.MaxDensity, Is.EqualTo(exportDensity.MaxDensity).Within(0.0005f), $"density max, {mode}");
                NativeParityAssert.AssertSameVectorArray(coreDensity.ActivityUV, exportDensity.ActivityUV, 0.0005f);
                NativeParityAssert.AssertSameVectorArray(coreDensity.AlphaUV, exportDensity.AlphaUV, 0.0005f);

                ActivityUvs exportIeeg = ComputeIeegUvs(BenchmarkBackend.HbpExport, timelineIndex: 0, mode: mode);
                ActivityUvs coreIeeg = ComputeIeegUvs(BenchmarkBackend.HbpCore, timelineIndex: 0, mode: mode);
                NativeParityAssert.AssertSameVectorArray(coreIeeg.ActivityUV, exportIeeg.ActivityUV, 0.0005f);
                NativeParityAssert.AssertSameVectorArray(coreIeeg.AlphaUV, exportIeeg.AlphaUV, 0.0005f);
            }
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeParity")]
        [Category(NativeParityAssert.StrictParity)]
        public void SurfaceMainUvs_MatchAcrossBackends()
        {
            NativeParityAssert.RequireHbpCore();
            Vector2[] exportUvs = ComputeMainUvs(BenchmarkBackend.HbpExport);
            Vector2[] coreUvs = ComputeMainUvs(BenchmarkBackend.HbpCore);
            NativeParityAssert.AssertSameVectorArray(coreUvs, exportUvs, 0.0005f);
        }

        private static ActivityUvs ComputeDensityUvs(BenchmarkBackend backend, SiteInfluenceByDistanceType mode = SiteInfluenceByDistanceType.Quadratic)
        {
            return NativeParityAssert.WithBackend(
                backend,
                () =>
                {
                    using Surface surface = LoadSurface();
                    using Volume volume = LoadVolume("fmri_3d.nii");
                    using GeneratorSurface generatorSurface = InitializeGeneratorSurface(surface, volume);
                    using RawSiteList rawSites = CreateRawSites(surface);
                    using DensityGenerator density = new();
                    density.Initialize(generatorSurface);
                    density.ComputeActivity(rawSites, influenceDistance: 80.0f, mode);
                    using SurfaceGenerator surfaceGenerator = InitializeSurfaceGenerator(density);
                    surfaceGenerator.ComputeActivityUV(timelineIndex: 0, alpha: 0.35f);
                    return new ActivityUvs(surfaceGenerator.ActivityUV, surfaceGenerator.AlphaUV, density.MaxDensity);
                });
        }

        private static ActivityUvs ComputeIeegUvs(BenchmarkBackend backend, int timelineIndex, SiteInfluenceByDistanceType mode = SiteInfluenceByDistanceType.Linear)
        {
            return NativeParityAssert.WithBackend(
                backend,
                () =>
                {
                    using Surface surface = LoadSurface();
                    using Volume volume = LoadVolume("fmri_3d.nii");
                    using GeneratorSurface generatorSurface = InitializeGeneratorSurface(surface, volume);
                    using RawSiteList rawSites = CreateRawSites(surface);
                    using IEEGGenerator ieeg = new();
                    ieeg.Initialize(generatorSurface);

                    int timelineLength = 2;
                    float[] activity = new float[]
                    {
                        -1.0f, 0.25f, 1.0f,
                        0.75f, -0.5f, 0.0f
                    };
                    ieeg.ComputeActivity(rawSites, influenceDistance: 80.0f, activity, timelineLength, rawSites.NumberOfSites, mode);
                    ieeg.AdjustValues(middle: 0.0f, spanMin: -1.0f, spanMax: 1.0f);

                    using SurfaceGenerator surfaceGenerator = InitializeSurfaceGenerator(ieeg);
                    surfaceGenerator.ComputeActivityUV(timelineIndex, alpha: 0.4f);
                    return new ActivityUvs(surfaceGenerator.ActivityUV, surfaceGenerator.AlphaUV);
                });
        }

        private static void ExportIeegNifti(BenchmarkBackend backend, string activityPath, string maskPath)
        {
            NativeParityAssert.WithBackend(
                backend,
                () =>
                {
                    using Surface surface = LoadSurface();
                    using Volume volume = LoadVolume("fmri_3d.nii");
                    using GeneratorSurface generatorSurface = InitializeGeneratorSurface(surface, volume);
                    using RawSiteList rawSites = CreateRawSites(surface);
                    using IEEGGenerator ieeg = new();
                    ieeg.Initialize(generatorSurface);

                    int timelineLength = 2;
                    float[] activity = new float[]
                    {
                        -1.0f, 0.25f, 1.0f,
                        0.75f, -0.5f, 0.0f
                    };
                    ieeg.ComputeActivity(rawSites, influenceDistance: 80.0f, activity, timelineLength, rawSites.NumberOfSites, SiteInfluenceByDistanceType.Linear);

                    SubTimeline timeline = CreateTwoPointTimeline();
                    Assert.That(ieeg.SaveActivityAsNifti(activityPath, timeline, "IEEG activity parity"), Is.True);
                    Assert.That(ieeg.SaveMaskAsNifti(maskPath, "IEEG mask parity"), Is.True);
                    Assert.That(File.Exists(activityPath), Is.True);
                    Assert.That(File.Exists(maskPath), Is.True);
                });
        }

        private static SubTimeline CreateTwoPointTimeline()
        {
            HBP.Core.Object3D.FMRI fmri = new();
            try
            {
                fmri.Volumes.Add(new HBP.Core.DLL.Volume());
                return new SubTimeline(fmri);
            }
            finally
            {
                fmri.Clean();
            }
        }

        private static void AssertSameNiftiSnapshot(NiftiFileSnapshot actual, NiftiFileSnapshot expected, bool compareTimeline)
        {
            Assert.That(actual.NumberOfVolumes, Is.EqualTo(expected.NumberOfVolumes));
            Assert.That(actual.Dimensions, Is.EqualTo(expected.Dimensions));
            Assert.That(actual.Datatype, Is.EqualTo(expected.Datatype));
            Assert.That(actual.Bitpix, Is.EqualTo(expected.Bitpix));
            Assert.That(actual.IntentCode, Is.EqualTo(expected.IntentCode));
            Assert.That(actual.QformCode, Is.EqualTo(expected.QformCode));
            Assert.That(actual.SformCode, Is.EqualTo(expected.SformCode));
            Assert.That(actual.XyztUnits, Is.EqualTo(expected.XyztUnits));
            Assert.That(actual.Magic, Is.EqualTo(expected.Magic));
            Assert.That(actual.Description, Is.EqualTo(expected.Description));
            Assert.That(actual.ExtensionFlag, Is.EqualTo(expected.ExtensionFlag));

            Assert.That(actual.VoxelOffset, Is.EqualTo(expected.VoxelOffset).Within(0.0005f));
            Assert.That(actual.SclSlope, Is.EqualTo(expected.SclSlope).Within(0.0005f));
            Assert.That(actual.SclInter, Is.EqualTo(expected.SclInter).Within(0.0005f));
            Assert.That(actual.CalMin, Is.EqualTo(expected.CalMin).Within(0.0005f));
            Assert.That(actual.CalMax, Is.EqualTo(expected.CalMax).Within(0.0005f));
            AssertSameFloatArray(actual.PixDim, expected.PixDim, "pixdim");
            AssertSameFloatArray(actual.Quaternions, expected.Quaternions, "quaternions");
            AssertSameFloatArray(actual.QOffsets, expected.QOffsets, "qoffsets");
            AssertSameFloatArray(actual.SrowX, expected.SrowX, "srow_x");
            AssertSameFloatArray(actual.SrowY, expected.SrowY, "srow_y");
            AssertSameFloatArray(actual.SrowZ, expected.SrowZ, "srow_z");

            if (compareTimeline)
            {
                Assert.That(actual.StartTime, Is.EqualTo(expected.StartTime).Within(0.0005f));
                Assert.That(actual.TimeStep, Is.EqualTo(expected.TimeStep).Within(0.0005f));
                Assert.That(actual.TimeUnitCode, Is.EqualTo(expected.TimeUnitCode));
            }

            Assert.That(actual.Values, Has.Length.EqualTo(expected.Values.Length));
            for (int i = 0; i < expected.Values.Length; ++i)
            {
                Assert.That(actual.Values[i], Is.EqualTo(expected.Values[i]).Within(0.0005f), $"value[{i}]");
            }
        }

        private static void AssertSameFloatArray(IReadOnlyList<float> actual, IReadOnlyList<float> expected, string name)
        {
            Assert.That(actual.Count, Is.EqualTo(expected.Count), name);
            for (int i = 0; i < expected.Count; ++i)
            {
                Assert.That(actual[i], Is.EqualTo(expected[i]).Within(0.0005f), $"{name}[{i}]");
            }
        }

        private static ActivityUvs ComputeVolumeActivityUvs(BenchmarkBackend backend, GeneratorKind generatorKind)
        {
            return NativeParityAssert.WithBackend(
                backend,
                () =>
                {
                    using Surface surface = LoadSurface();
                    using Volume referenceVolume = LoadVolume("fmri_3d.nii");
                    using Volume activityVolume = LoadVolume("fmri_3d.nii");
                    using Volume maskVolume = LoadVolume("mask_binary.nii");
                    using GeneratorSurface generatorSurface = InitializeGeneratorSurface(surface, referenceVolume);
                    using ActivityGenerator activity = CreateVolumeActivityGenerator(generatorKind);
                    activity.Initialize(generatorSurface);

                    if (activity is FMRIGenerator fmri)
                    {
                        fmri.ComputeActivity(new[] { (activityVolume, maskVolume) });
                        fmri.AdjustValues(-1.0f, -0.25f, 0.25f, 1.0f);
                        fmri.HideExtremeValues(hideLower: false, hideMiddle: false, hideHigher: false);
                    }
                    else if (activity is MEGGenerator meg)
                    {
                        meg.ComputeActivity(new[] { (activityVolume, maskVolume) });
                        meg.AdjustValues(-1.0f, -0.25f, 0.25f, 1.0f);
                        meg.HideExtremeValues(hideLower: false, hideMiddle: false, hideHigher: false);
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException(nameof(generatorKind), generatorKind, null);
                    }

                    using SurfaceGenerator surfaceGenerator = InitializeSurfaceGenerator(activity);
                    surfaceGenerator.ComputeActivityUV(timelineIndex: 0, alpha: 0.25f);
                    return new ActivityUvs(surfaceGenerator.ActivityUV, surfaceGenerator.AlphaUV);
                });
        }

        private static ActivityGenerator CreateVolumeActivityGenerator(GeneratorKind generatorKind)
        {
            return generatorKind switch
            {
                GeneratorKind.Fmri => new FMRIGenerator(),
                GeneratorKind.Meg => new MEGGenerator(),
                _ => throw new ArgumentOutOfRangeException(nameof(generatorKind), generatorKind, null)
            };
        }

        private static Vector2[] ComputeMainUvs(BenchmarkBackend backend)
        {
            return NativeParityAssert.WithBackend(
                backend,
                () =>
                {
                    using Surface surface = LoadSurface();
                    using Volume volume = LoadVolume("fmri_3d.nii");
                    using GeneratorSurface generatorSurface = InitializeGeneratorSurface(surface, volume);
                    using DensityGenerator density = new();
                    density.Initialize(generatorSurface);
                    using SurfaceGenerator surfaceGenerator = InitializeSurfaceGenerator(density);
                    surfaceGenerator.ComputeMainUV(0.25f, 0.75f);
                    Mesh mesh = new();
                    try
                    {
                        surface.UpdateMeshFromDLL(mesh);
                        return (Vector2[])mesh.uv.Clone();
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(mesh);
                    }
                });
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

        private static GeneratorSurface InitializeGeneratorSurface(Surface surface, Volume volume)
        {
            GeneratorSurface generatorSurface = new();
            try
            {
                generatorSurface.Initialize(surface, volume, 8);
                return generatorSurface;
            }
            catch
            {
                generatorSurface.Dispose();
                throw;
            }
        }

        private static SurfaceGenerator InitializeSurfaceGenerator(ActivityGenerator activityGenerator)
        {
            SurfaceGenerator surfaceGenerator = new();
            try
            {
                surfaceGenerator.Initialize(activityGenerator);
                return surfaceGenerator;
            }
            catch
            {
                surfaceGenerator.Dispose();
                throw;
            }
        }

        private static RawSiteList CreateRawSites(Surface surface)
        {
            Mesh mesh = new();
            try
            {
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                surface.UpdateMeshFromDLL(mesh);
                Assert.That(mesh.vertexCount, Is.GreaterThanOrEqualTo(3));

                RawSiteList rawSites = new();
                Vector3 first = ToNativePosition(surface, mesh.vertices[0]);
                Vector3 second = ToNativePosition(surface, mesh.vertices[1]);
                Vector3 third = ToNativePosition(surface, mesh.vertices[2]);
                rawSites.AddSite("S1", first, patientIndex: 0, index: 0);
                rawSites.AddSite("S2", second, patientIndex: 0, index: 1);
                rawSites.AddSite("S3", third, patientIndex: 0, index: 2);
                rawSites.UpdateMask(0, mask: false);
                rawSites.UpdateMask(1, mask: false);
                rawSites.UpdateMask(2, mask: true);
                return rawSites;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(mesh);
            }
        }

        private static Vector3 ToNativePosition(Surface surface, Vector3 position)
        {
            return surface.Backend == BenchmarkBackend.HbpCore
                ? new Vector3(ReferenceSystemConversion.ConvertX(position.x), position.y, position.z)
                : position;
        }

        private enum GeneratorKind
        {
            Fmri,
            Meg
        }

        private readonly struct ActivityUvs
        {
            public ActivityUvs(Vector2[] activityUV, Vector2[] alphaUV, float maxDensity = 0.0f)
            {
                ActivityUV = (Vector2[])activityUV.Clone();
                AlphaUV = (Vector2[])alphaUV.Clone();
                MaxDensity = maxDensity;
            }

            public Vector2[] ActivityUV { get; }
            public Vector2[] AlphaUV { get; }
            public float MaxDensity { get; }
        }

        private readonly struct NiftiFileSnapshot
        {
            public static NiftiFileSnapshot Read(string path)
            {
                byte[] bytes = ReadNiftiBytes(path);
                Assert.That(ReadInt32(bytes, 0), Is.EqualTo(348), path);

                short[] dimensions = new short[8];
                for (int i = 0; i < dimensions.Length; ++i)
                {
                    dimensions[i] = ReadInt16(bytes, 40 + i * 2);
                }

                float[] pixDim = ReadFloatArray(bytes, 76, 8);
                float voxelOffset = ReadSingle(bytes, 108);
                short datatype = ReadInt16(bytes, 70);
                short bitpix = ReadInt16(bytes, 72);
                float sclSlope = ReadSingle(bytes, 112);
                float sclInter = ReadSingle(bytes, 116);
                byte xyztUnits = bytes[123];
                float calMax = ReadSingle(bytes, 124);
                float calMin = ReadSingle(bytes, 128);
                float startTime = ReadSingle(bytes, 136);
                short intentCode = ReadInt16(bytes, 68);
                string description = ReadAscii(bytes, 148, 80);
                short qformCode = ReadInt16(bytes, 252);
                short sformCode = ReadInt16(bytes, 254);
                float[] quaternions = ReadFloatArray(bytes, 256, 3);
                float[] qOffsets = ReadFloatArray(bytes, 268, 3);
                float[] srowX = ReadFloatArray(bytes, 280, 4);
                float[] srowY = ReadFloatArray(bytes, 296, 4);
                float[] srowZ = ReadFloatArray(bytes, 312, 4);
                string magic = ReadAscii(bytes, 344, 4);
                byte[] extensionFlag = new byte[4];
                Array.Copy(bytes, 348, extensionFlag, 0, extensionFlag.Length);
                float[] values = ReadValues(bytes, dimensions, datatype, voxelOffset);

                return new NiftiFileSnapshot(
                    dimensions,
                    pixDim,
                    voxelOffset,
                    datatype,
                    bitpix,
                    sclSlope,
                    sclInter,
                    xyztUnits,
                    calMin,
                    calMax,
                    startTime,
                    intentCode,
                    description,
                    qformCode,
                    sformCode,
                    quaternions,
                    qOffsets,
                    srowX,
                    srowY,
                    srowZ,
                    magic,
                    extensionFlag,
                    values);
            }

            private NiftiFileSnapshot(short[] dimensions, float[] pixDim, float voxelOffset, short datatype, short bitpix, float sclSlope, float sclInter, byte xyztUnits, float calMin, float calMax, float startTime, short intentCode, string description, short qformCode, short sformCode, float[] quaternions, float[] qOffsets, float[] srowX, float[] srowY, float[] srowZ, string magic, byte[] extensionFlag, float[] values)
            {
                Dimensions = (short[])dimensions.Clone();
                PixDim = (float[])pixDim.Clone();
                VoxelOffset = voxelOffset;
                Datatype = datatype;
                Bitpix = bitpix;
                SclSlope = sclSlope;
                SclInter = sclInter;
                XyztUnits = xyztUnits;
                CalMin = calMin;
                CalMax = calMax;
                StartTime = startTime;
                IntentCode = intentCode;
                Description = description;
                QformCode = qformCode;
                SformCode = sformCode;
                Quaternions = (float[])quaternions.Clone();
                QOffsets = (float[])qOffsets.Clone();
                SrowX = (float[])srowX.Clone();
                SrowY = (float[])srowY.Clone();
                SrowZ = (float[])srowZ.Clone();
                Magic = magic;
                ExtensionFlag = (byte[])extensionFlag.Clone();
                Values = (float[])values.Clone();
                NumberOfVolumes = Dimensions[0] >= 4 ? Math.Max(1, (int)Dimensions[4]) : 1;
            }

            public short[] Dimensions { get; }
            public float[] PixDim { get; }
            public float VoxelOffset { get; }
            public short Datatype { get; }
            public short Bitpix { get; }
            public float SclSlope { get; }
            public float SclInter { get; }
            public byte XyztUnits { get; }
            public int TimeUnitCode => XyztUnits & 0x38;
            public float CalMin { get; }
            public float CalMax { get; }
            public float StartTime { get; }
            public float TimeStep => PixDim.Length > 4 ? PixDim[4] : 0.0f;
            public short IntentCode { get; }
            public string Description { get; }
            public short QformCode { get; }
            public short SformCode { get; }
            public float[] Quaternions { get; }
            public float[] QOffsets { get; }
            public float[] SrowX { get; }
            public float[] SrowY { get; }
            public float[] SrowZ { get; }
            public string Magic { get; }
            public byte[] ExtensionFlag { get; }
            public float[] Values { get; }
            public int NumberOfVolumes { get; }

            private static byte[] ReadNiftiBytes(string path)
            {
                using FileStream file = File.OpenRead(path);
                Stream input = file;
                if (path.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
                {
                    input = new GZipStream(file, CompressionMode.Decompress);
                }

                using (input)
                using (MemoryStream memory = new())
                {
                    input.CopyTo(memory);
                    return memory.ToArray();
                }
            }

            private static float[] ReadValues(byte[] bytes, IReadOnlyList<short> dimensions, short datatype, float voxelOffset)
            {
                int dimensionCount = Math.Max(1, (int)dimensions[0]);
                int valueCount = 1;
                for (int i = 1; i <= dimensionCount; ++i)
                {
                    valueCount *= Math.Max(1, (int)dimensions[i]);
                }

                int dataOffset = (int)Math.Round(voxelOffset);
                float[] values = new float[valueCount];
                for (int i = 0; i < valueCount; ++i)
                {
                    values[i] = datatype switch
                    {
                        2 => bytes[dataOffset + i],
                        4 => ReadInt16(bytes, dataOffset + i * 2),
                        8 => ReadInt32(bytes, dataOffset + i * 4),
                        16 => ReadSingle(bytes, dataOffset + i * 4),
                        64 => (float)BitConverter.ToDouble(bytes, dataOffset + i * 8),
                        _ => throw new NotSupportedException($"Unsupported NIFTI datatype {datatype}")
                    };
                }

                return values;
            }

            private static float[] ReadFloatArray(byte[] bytes, int offset, int count)
            {
                float[] values = new float[count];
                for (int i = 0; i < count; ++i)
                {
                    values[i] = ReadSingle(bytes, offset + i * 4);
                }
                return values;
            }

            private static string ReadAscii(byte[] bytes, int offset, int count)
            {
                int end = offset;
                while (end < offset + count && bytes[end] != 0)
                {
                    ++end;
                }
                return Encoding.ASCII.GetString(bytes, offset, end - offset);
            }

            private static short ReadInt16(byte[] bytes, int offset) => BitConverter.ToInt16(bytes, offset);

            private static int ReadInt32(byte[] bytes, int offset) => BitConverter.ToInt32(bytes, offset);

            private static float ReadSingle(byte[] bytes, int offset) => BitConverter.ToSingle(bytes, offset);
        }
    }
}
