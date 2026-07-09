using System;
using System.Collections.Generic;
using System.Linq;
using HBP.Core.DLL;
using HBP.Core.DLL.HbpCore;
using HBP.Core.Enums;
using HBP.Tests.Serialization.Helpers;
using NUnit.Framework;
using UnityEngine;
using HbpPlane = HBP.Core.DLL.Plane;
using HbpSegment3 = HBP.Core.DLL.Segment3;

namespace HBP.Tests.Serialization
{
    public class NativeParityGeometryVolumeTests
    {
        [Test]
        [Category("NativeMigration")]
        [Category("NativeParity")]
        public void BBoxPlaneIntersectionsAndCutOffsets_MatchAcrossBackends()
        {
            NativeParityAssert.RequireHbpCore();

            using Volume hbpExportVolume = LoadVolume(NativeBackend.HbpExport, "fmri_3d.nii");
            using BBox hbpExportBBox = hbpExportVolume.BoundingBox;
            using BBox hbpCoreBBox = BBox.CreateHbpCore(hbpExportBBox.Min, hbpExportBBox.Max);

            NativeParityAssert.AssertVector(hbpCoreBBox.Min, hbpExportBBox.Min);
            NativeParityAssert.AssertVector(hbpCoreBBox.Max, hbpExportBBox.Max);
            NativeParityAssert.AssertVector(hbpCoreBBox.Center, hbpExportBBox.Center);
            NativeParityAssert.AssertSameVectorSet(hbpCoreBBox.Points, hbpExportBBox.Points);

            List<HbpSegment3> hbpCoreSegments = hbpCoreBBox.Segments;
            List<HbpSegment3> hbpExportSegments = hbpExportBBox.Segments;
            try
            {
                Assert.That(hbpCoreSegments, Has.Count.EqualTo(hbpExportSegments.Count));
            }
            finally
            {
                NativeParityAssert.DisposeSegments(hbpCoreSegments);
                NativeParityAssert.DisposeSegments(hbpExportSegments);
            }

            foreach ((Vector3 normal, string name) in new[]
            {
                (Vector3.right, "sagittal"),
                (Vector3.up, "coronal"),
                (Vector3.forward, "axial"),
                (new Vector3(1.0f, 1.0f, 1.0f).normalized, "diagonal")
            })
            {
                using HbpPlane plane = new(hbpExportBBox.Center, normal);
                NativeParityAssert.AssertSameVectorSet(
                    hbpCoreBBox.IntersectionPointsWithPlane(plane),
                    hbpExportBBox.IntersectionPointsWithPlane(plane),
                    0.0002f);

                List<HbpSegment3> coreIntersectionSegments = hbpCoreBBox.IntersectionLinesWithPlane(plane);
                List<HbpSegment3> exportIntersectionSegments = hbpExportBBox.IntersectionLinesWithPlane(plane);
                try
                {
                    Assert.That(coreIntersectionSegments, Has.Count.EqualTo(exportIntersectionSegments.Count), name);
                }
                finally
                {
                    NativeParityAssert.DisposeSegments(coreIntersectionSegments);
                    NativeParityAssert.DisposeSegments(exportIntersectionSegments);
                }

                Assert.That(
                    hbpCoreBBox.SizeOffsetCutPlane(plane, 4),
                    Is.EqualTo(hbpExportBBox.SizeOffsetCutPlane(plane, 4)).Within(0.0002f),
                    name);
            }

            using HbpPlane planeA = new(hbpExportBBox.Center, Vector3.right);
            using HbpPlane planeB = new(hbpExportBBox.Center, Vector3.up);
            HbpSegment3 hbpCoreSegment = hbpCoreBBox.IntersectionSegmentBetweenTwoPlanes(planeA, planeB);
            HbpSegment3 hbpExportSegment = hbpExportBBox.IntersectionSegmentBetweenTwoPlanes(planeA, planeB);
            try
            {
                Assert.That(hbpCoreSegment, Is.Not.Null);
                Assert.That(hbpExportSegment, Is.Not.Null);
                NativeParityAssert.AssertSameSegmentSet(new[] { hbpCoreSegment }, new[] { hbpExportSegment });
            }
            finally
            {
                hbpCoreSegment?.Dispose();
                hbpExportSegment?.Dispose();
            }
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeParity")]
        public void VolumeReadOnlyPropertiesSamplingAndOrientations_MatchAcrossBackends()
        {
            NativeParityAssert.RequireHbpCore();

            using Volume hbpExportVolume = LoadVolume(NativeBackend.HbpExport, "fmri_3d.nii");
            using Volume hbpCoreVolume = LoadVolume(NativeBackend.HbpCore, "fmri_3d.nii");

            NativeParityAssert.AssertVector(hbpCoreVolume.Center, hbpExportVolume.Center);
            NativeParityAssert.AssertVector(hbpCoreVolume.Spacing, hbpExportVolume.Spacing);
            NativeParityAssert.AssertMriCalValues(hbpCoreVolume.ExtremeValues, hbpExportVolume.ExtremeValues);

            using BBox hbpExportBBox = hbpExportVolume.BoundingBox;
            using BBox hbpCoreBBox = hbpCoreVolume.BoundingBox;
            NativeParityAssert.AssertVector(hbpCoreBBox.Min, hbpExportBBox.Min);
            NativeParityAssert.AssertVector(hbpCoreBBox.Max, hbpExportBBox.Max);

            foreach (Vector3 position in new[]
            {
                Vector3.zero,
                new Vector3(-1.0f, 1.0f, 1.0f),
                new Vector3(-2.0f, 3.0f, 4.0f),
                new Vector3(-4.0f, 4.0f, 4.0f)
            })
            {
                Assert.That(
                    hbpCoreVolume.GetValueFromPosition(position),
                    Is.EqualTo(hbpExportVolume.GetValueFromPosition(position)).Within(0.0001f),
                    position.ToString());
            }

            using Volume hbpExportMaskVolume = LoadVolume(NativeBackend.HbpExport, "fmri_3d.nii");
            using Volume hbpCoreMaskVolume = LoadVolume(NativeBackend.HbpCore, "fmri_3d.nii");
            float[] hbpExportRawValues = new float[27];
            float[] hbpCoreRawValues = new float[27];
            int hbpExportActualLength = 0;
            int hbpCoreActualLength = 0;
            float hbpExportAverage = hbpExportVolume.GetAverageValueAroundPositionWithMask(
                new Vector3(-2.0f, 3.0f, 4.0f),
                1,
                hbpExportMaskVolume,
                ref hbpExportRawValues,
                ref hbpExportActualLength);
            float hbpCoreAverage = hbpCoreVolume.GetAverageValueAroundPositionWithMask(
                new Vector3(-2.0f, 3.0f, 4.0f),
                1,
                hbpCoreMaskVolume,
                ref hbpCoreRawValues,
                ref hbpCoreActualLength);

            Assert.That(hbpCoreAverage, Is.EqualTo(hbpExportAverage).Within(0.0001f));
            Assert.That(hbpCoreActualLength, Is.EqualTo(hbpExportActualLength));
            Assert.That(hbpCoreRawValues.Take(hbpCoreActualLength), Is.EqualTo(hbpExportRawValues.Take(hbpExportActualLength)).Within(0.0001f));

            foreach (CutOrientation orientation in new[] { CutOrientation.Axial, CutOrientation.Coronal, CutOrientation.Sagittal })
            {
                foreach (bool flip in new[] { false, true })
                {
                    NativeParityAssert.AssertVector(
                        hbpCoreVolume.GetOrientationVector(orientation, flip),
                        hbpExportVolume.GetOrientationVector(orientation, flip));
                }
            }

            foreach (Vector3 normal in new[] { Vector3.right, Vector3.up, Vector3.forward, new Vector3(1.0f, 1.0f, 1.0f).normalized })
            {
                using HbpPlane plane = new(hbpExportVolume.Center, normal);
                Assert.That(
                    hbpCoreVolume.SizeOffsetCutPlane(plane, 8),
                    Is.EqualTo(hbpExportVolume.SizeOffsetCutPlane(plane, 8)).Within(0.0002f),
                    normal.ToString());
            }
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeParity")]
        public void NiftiMetadataAndExtractedVolumes_MatchAcrossBackends()
        {
            NativeParityAssert.RequireHbpCore();

            using NIFTI hbpExportNifti = LoadNifti(NativeBackend.HbpExport, "fmri_4d.nii.gz");
            using NIFTI hbpCoreNifti = LoadNifti(NativeBackend.HbpCore, "fmri_4d.nii.gz");

            Assert.That(hbpCoreNifti.NumberOfVolumes, Is.EqualTo(hbpExportNifti.NumberOfVolumes));
            Assert.That(hbpCoreNifti.StartTime, Is.EqualTo(hbpExportNifti.StartTime).Within(0.0001f));
            Assert.That(hbpCoreNifti.TimeStep, Is.EqualTo(hbpExportNifti.TimeStep).Within(0.0001f));
            Assert.That(hbpCoreNifti.TimeUnit, Is.EqualTo(hbpExportNifti.TimeUnit));
            NativeParityAssert.AssertMriCalValues(hbpCoreNifti.ExtremeValues, hbpExportNifti.ExtremeValues);

            using Volume hbpExportFirstVolume = LoadVolume(NativeBackend.HbpExport, "fmri_4d.nii.gz");
            using Volume hbpCoreFirstVolume = hbpCoreNifti.ExtractVolume(0);
            Assert.That(hbpCoreFirstVolume.IsLoaded, Is.True);
            NativeParityAssert.AssertVector(hbpCoreFirstVolume.Center, hbpExportFirstVolume.Center);
            NativeParityAssert.AssertVector(hbpCoreFirstVolume.Spacing, hbpExportFirstVolume.Spacing);
            NativeParityAssert.AssertMriCalValues(hbpCoreFirstVolume.ExtremeValues, hbpExportFirstVolume.ExtremeValues);
            Assert.That(
                hbpCoreFirstVolume.GetValueFromPosition(new Vector3(-2.0f, 3.0f, 4.0f)),
                Is.EqualTo(hbpExportFirstVolume.GetValueFromPosition(new Vector3(-2.0f, 3.0f, 4.0f))).Within(0.0001f));

            using Volume hbpCoreSecondVolume = hbpCoreNifti.ExtractVolume(Math.Min(1, hbpCoreNifti.NumberOfVolumes - 1));
            Assert.That(hbpCoreSecondVolume.IsLoaded, Is.True);
        }

        private static Volume LoadVolume(NativeBackend backend, string fixtureName)
        {
            return NativeParityAssert.WithBackend(
                backend,
                () =>
                {
                    Volume volume = new();
                    try
                    {
                        Assert.That(volume.LoadNIFTIFile(NativeParityAssert.NativePath("Nifti", fixtureName)), Is.True);
                        return volume;
                    }
                    catch
                    {
                        volume.Dispose();
                        throw;
                    }
                });
        }

        private static NIFTI LoadNifti(NativeBackend backend, string fixtureName)
        {
            return NativeParityAssert.WithBackend(
                backend,
                () =>
                {
                    NIFTI nifti = new();
                    try
                    {
                        Assert.That(nifti.Load(NativeParityAssert.NativePath("Nifti", fixtureName)), Is.True);
                        return nifti;
                    }
                    catch
                    {
                        nifti.Dispose();
                        throw;
                    }
                });
        }
    }
}
