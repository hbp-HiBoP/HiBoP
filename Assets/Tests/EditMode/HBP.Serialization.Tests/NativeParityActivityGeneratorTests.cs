using System;
using HBP.Core.DLL;
using HBP.Core.Enums;
using HBP.Tests.Serialization.Helpers;
using NUnit.Framework;
using UnityEngine;

namespace HBP.Tests.Serialization
{
    public class NativeParityActivityGeneratorTests
    {
        [Test]
        [Category("NativeMigration")]
        [Category("NativeParity")]
        public void DensityActivitySurfaceUvs_MatchAcrossBackends()
        {
            NativeParityAssert.RequireHbpCore();

            ActivityUvs hbpExportUvs = ComputeDensityUvs(NativeBackend.HbpExport);
            ActivityUvs hbpCoreUvs = ComputeDensityUvs(NativeBackend.HbpCore);

            Assert.That(hbpCoreUvs.MaxDensity, Is.EqualTo(hbpExportUvs.MaxDensity).Within(0.0005f));
            NativeParityAssert.AssertSameVectorArray(hbpCoreUvs.ActivityUV, hbpExportUvs.ActivityUV, 0.0005f);
            NativeParityAssert.AssertSameVectorArray(hbpCoreUvs.AlphaUV, hbpExportUvs.AlphaUV, 0.0005f);
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeParity")]
        public void IEEGActivitySurfaceUvs_MatchAcrossBackends()
        {
            NativeParityAssert.RequireHbpCore();

            ActivityUvs hbpExportTimeline0 = ComputeIeegUvs(NativeBackend.HbpExport, timelineIndex: 0);
            ActivityUvs hbpCoreTimeline0 = ComputeIeegUvs(NativeBackend.HbpCore, timelineIndex: 0);
            ActivityUvs hbpExportTimeline1 = ComputeIeegUvs(NativeBackend.HbpExport, timelineIndex: 1);
            ActivityUvs hbpCoreTimeline1 = ComputeIeegUvs(NativeBackend.HbpCore, timelineIndex: 1);

            NativeParityAssert.AssertSameVectorArray(hbpCoreTimeline0.ActivityUV, hbpExportTimeline0.ActivityUV, 0.0005f);
            NativeParityAssert.AssertSameVectorArray(hbpCoreTimeline0.AlphaUV, hbpExportTimeline0.AlphaUV, 0.0005f);
            NativeParityAssert.AssertSameVectorArray(hbpCoreTimeline1.ActivityUV, hbpExportTimeline1.ActivityUV, 0.0005f);
            NativeParityAssert.AssertSameVectorArray(hbpCoreTimeline1.AlphaUV, hbpExportTimeline1.AlphaUV, 0.0005f);
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeParity")]
        public void FmriAndMegActivitySurfaceUvs_MatchAcrossBackends()
        {
            NativeParityAssert.RequireHbpCore();

            ActivityUvs hbpExportFmri = ComputeVolumeActivityUvs(NativeBackend.HbpExport, GeneratorKind.Fmri);
            ActivityUvs hbpCoreFmri = ComputeVolumeActivityUvs(NativeBackend.HbpCore, GeneratorKind.Fmri);
            ActivityUvs hbpExportMeg = ComputeVolumeActivityUvs(NativeBackend.HbpExport, GeneratorKind.Meg);
            ActivityUvs hbpCoreMeg = ComputeVolumeActivityUvs(NativeBackend.HbpCore, GeneratorKind.Meg);

            NativeParityAssert.AssertSameVectorArray(hbpCoreFmri.ActivityUV, hbpExportFmri.ActivityUV, 0.0005f);
            NativeParityAssert.AssertSameVectorArray(hbpCoreFmri.AlphaUV, hbpExportFmri.AlphaUV, 0.0005f);
            NativeParityAssert.AssertSameVectorArray(hbpCoreMeg.ActivityUV, hbpExportMeg.ActivityUV, 0.0005f);
            NativeParityAssert.AssertSameVectorArray(hbpCoreMeg.AlphaUV, hbpExportMeg.AlphaUV, 0.0005f);

            NativeParityAssert.AssertSameVectorArray(hbpCoreMeg.ActivityUV, hbpCoreFmri.ActivityUV, 0.0005f);
            NativeParityAssert.AssertSameVectorArray(hbpCoreMeg.AlphaUV, hbpCoreFmri.AlphaUV, 0.0005f);
        }

        private static ActivityUvs ComputeDensityUvs(NativeBackend backend)
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
                    density.ComputeActivity(rawSites, influenceDistance: 80.0f, SiteInfluenceByDistanceType.Quadratic);
                    using SurfaceGenerator surfaceGenerator = InitializeSurfaceGenerator(density);
                    surfaceGenerator.ComputeActivityUV(timelineIndex: 0, alpha: 0.35f);
                    return new ActivityUvs(surfaceGenerator.ActivityUV, surfaceGenerator.AlphaUV, density.MaxDensity);
                });
        }

        private static ActivityUvs ComputeIeegUvs(NativeBackend backend, int timelineIndex)
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
                    ieeg.ComputeActivity(rawSites, influenceDistance: 80.0f, activity, timelineLength, rawSites.NumberOfSites, SiteInfluenceByDistanceType.Linear);
                    ieeg.AdjustValues(middle: 0.0f, spanMin: -1.0f, spanMax: 1.0f);

                    using SurfaceGenerator surfaceGenerator = InitializeSurfaceGenerator(ieeg);
                    surfaceGenerator.ComputeActivityUV(timelineIndex, alpha: 0.4f);
                    return new ActivityUvs(surfaceGenerator.ActivityUV, surfaceGenerator.AlphaUV);
                });
        }

        private static ActivityUvs ComputeVolumeActivityUvs(NativeBackend backend, GeneratorKind generatorKind)
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
                rawSites.AddSite("S1", mesh.vertices[0], patientIndex: 0, index: 0);
                rawSites.AddSite("S2", mesh.vertices[1], patientIndex: 0, index: 1);
                rawSites.AddSite("S3", mesh.vertices[2], patientIndex: 0, index: 2);
                rawSites.UpdateMask(2, mask: true);
                return rawSites;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(mesh);
            }
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
    }
}
