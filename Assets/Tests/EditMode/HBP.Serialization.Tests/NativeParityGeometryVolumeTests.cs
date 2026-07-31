using System;
using System.Collections.Generic;
using System.Linq;
using HBP.Core.DLL;
using HBP.Core.DLL.HbpCore;
using HBP.Core.Enums;
using HBP.Tests.Serialization.Helpers;
using NUnit.Framework;
using UnityEngine;
using BBox = HBP.Tests.Serialization.LegacyNative.BBox;
using NIFTI = HBP.Tests.Serialization.LegacyNative.NIFTI;
using Volume = HBP.Tests.Serialization.LegacyNative.Volume;
using HbpPlane = HBP.Core.DLL.Plane;
using HbpSegment3 = HBP.Core.DLL.Segment3;

namespace HBP.Tests.Serialization
{
    [LegacyParityOnly]
    public class NativeParityGeometryVolumeTests
    {
        [Test]
        [Category("NativeMigration")]
        [Category("NativeParity")]
        [Category(NativeParityAssert.NormalizedCoordinateParity)]
        public void BBoxPlaneIntersectionsMatchAndCutOffsetsUseExactBounds()
        {
            NativeParityAssert.RequireHbpCore();

            using Volume hbpExportVolume = LoadVolume(BenchmarkBackend.HbpExport, "fmri_3d.nii");
            using BBox hbpExportBBox = hbpExportVolume.BoundingBox;
            NativeParityAssert.NativeBoundsToUnity(hbpExportBBox.Min, hbpExportBBox.Max, out Vector3 unityMin, out Vector3 unityMax);
            using BBox hbpCoreBBox = BBox.CreateHbpCore(unityMin, unityMax);

            NativeParityAssert.AssertUnityBoundsMatchLegacyNative(hbpCoreBBox.Min, hbpCoreBBox.Max, hbpExportBBox.Min, hbpExportBBox.Max, context: "BBox algorithm fixture");
            NativeParityAssert.AssertUnityVectorMatchesLegacyNative(hbpCoreBBox.Center, hbpExportBBox.Center, context: "BBox center");
            NativeParityAssert.AssertUnityVectorSetMatchesLegacyNative(hbpCoreBBox.Points, hbpExportBBox.Points);

            List<HbpSegment3> hbpCoreSegments = hbpCoreBBox.Segments;
            List<HbpSegment3> hbpExportSegments = hbpExportBBox.Segments;
            try
            {
                Assert.That(hbpCoreSegments, Has.Count.EqualTo(hbpExportSegments.Count));
                AssertBBoxEdgesUseDistinctBufferPairs(hbpExportBBox.Points, hbpExportSegments);
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
                using HbpPlane plane = new(hbpCoreBBox.Center, normal);
                NativeParityAssert.AssertUnityVectorSetMatchesLegacyNative(hbpCoreBBox.IntersectionPointsWithPlane(plane), hbpExportBBox.IntersectionPointsWithPlane(plane), 0.0002f);

                List<HbpSegment3> coreIntersectionSegments = hbpCoreBBox.IntersectionLinesWithPlane(plane);
                List<HbpSegment3> exportIntersectionSegments = hbpExportBBox.IntersectionLinesWithPlane(plane);
                try
                {
                    // Both APIs expose the same intersection points (asserted above), but
                    // hbp_export and hbp_core decompose the polygon into segments differently.
                    // Segment count is the parity contract; legacy endpoints remain diagnostic.
                    Assert.That(coreIntersectionSegments, Has.Count.EqualTo(exportIntersectionSegments.Count), name);
                    TestContext.Progress.WriteLine($"{name} plane segments: hbp_core Unity count={coreIntersectionSegments.Count}; hbp_export native count={exportIntersectionSegments.Count}");
                }
                finally
                {
                    NativeParityAssert.DisposeSegments(coreIntersectionSegments);
                    NativeParityAssert.DisposeSegments(exportIntersectionSegments);
                }

                AssertCorrectedCutOffset(hbpCoreBBox.SizeOffsetCutPlane(plane, 4), hbpExportBBox.SizeOffsetCutPlane(plane, 4), hbpCoreBBox.Min, hbpCoreBBox.Max, normal, 4, name);
            }

            using HbpPlane planeA = new(hbpCoreBBox.Center, Vector3.right);
            using HbpPlane planeB = new(hbpCoreBBox.Center, Vector3.up);
            HbpSegment3 hbpCoreSegment = hbpCoreBBox.IntersectionSegmentBetweenTwoPlanes(planeA, planeB);
            HbpSegment3 hbpExportSegment = hbpExportBBox.IntersectionSegmentBetweenTwoPlanes(planeA, planeB);
            try
            {
                Assert.That(hbpCoreSegment, Is.Not.Null);
                Assert.That(hbpExportSegment, Is.Not.Null);
                NativeParityAssert.AssertUnitySegmentSetMatchesLegacyNative(new[] { hbpCoreSegment }, new[] { hbpExportSegment });
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
        [Category(NativeParityAssert.IndependentOracle)]
        public void VolumeReadOnlyPropertiesSamplingAndOrientations_MatchAcrossBackends()
        {
            NativeParityAssert.RequireHbpCore();

            using Volume hbpExportVolume = LoadVolume(BenchmarkBackend.HbpExport, "fmri_3d.nii");
            using Volume hbpCoreVolume = LoadVolume(BenchmarkBackend.HbpCore, "fmri_3d.nii");

            NativeParityAssert.AssertUnityVectorMatchesLegacyNative(hbpCoreVolume.Center, hbpExportVolume.Center, context: "fmri_3d center");
            NativeParityAssert.AssertVector(hbpCoreVolume.Center, new Vector3(-2.0f, 2.0f, 2.0f), context: "fmri_3d fixture center in Unity");
            NativeParityAssert.AssertVector(hbpCoreVolume.Spacing, hbpExportVolume.Spacing, context: "fmri_3d spacing (unsigned magnitude)");
            NativeParityAssert.AssertVector(hbpCoreVolume.Spacing, Vector3.one, context: "fmri_3d fixture spacing");
            NativeParityAssert.AssertMriCalValues(hbpCoreVolume.ExtremeValues, hbpExportVolume.ExtremeValues);

            using BBox hbpExportBBox = hbpExportVolume.BoundingBox;
            using BBox hbpCoreBBox = hbpCoreVolume.BoundingBox;
            NativeParityAssert.AssertUnityBoundsMatchLegacyNative(hbpCoreBBox.Min, hbpCoreBBox.Max, hbpExportBBox.Min, hbpExportBBox.Max, context: "fmri_3d bbox");
            NativeParityAssert.AssertVector(hbpCoreBBox.Min, new Vector3(-4.0f, 0.0f, 0.0f), context: "fmri_3d fixture bbox min in Unity");
            NativeParityAssert.AssertVector(hbpCoreBBox.Max, new Vector3(0.0f, 4.0f, 4.0f), context: "fmri_3d fixture bbox max in Unity");

            foreach (Vector3 position in new[]
                     {
                         Vector3.zero,
                         new Vector3(-1.0f, 1.0f, 1.0f),
                         new Vector3(-2.0f, 3.0f, 4.0f),
                         new Vector3(-4.0f, 4.0f, 4.0f)
                     })
            {
                float hbpCoreValue = hbpCoreVolume.GetValueFromPosition(position);
                float hbpExportValue = hbpExportVolume.GetValueFromPosition(position);
                Assert.That(hbpCoreValue, Is.EqualTo(hbpExportValue).Within(0.0001f), $"Unity sample {position}; both wrappers convert Unity->native");
                if (position == new Vector3(-2.0f, 3.0f, 4.0f))
                {
                    Assert.That(hbpCoreValue, Is.EqualTo(69.0f).Within(0.0001f), "fmri_3d fixture oracle at native (2,3,4) / Unity (-2,3,4)");
                }
            }

            using Volume hbpExportMaskVolume = LoadVolume(BenchmarkBackend.HbpExport, "fmri_3d.nii");
            using Volume hbpCoreMaskVolume = LoadVolume(BenchmarkBackend.HbpCore, "fmri_3d.nii");
            float[] hbpExportRawValues = new float[27];
            float[] hbpCoreRawValues = new float[27];
            int hbpExportActualLength = 0;
            int hbpCoreActualLength = 0;
            float hbpExportAverage = hbpExportVolume.GetAverageValueAroundPositionWithMask(new Vector3(-2.0f, 3.0f, 4.0f), 1, hbpExportMaskVolume, ref hbpExportRawValues, ref hbpExportActualLength);
            float hbpCoreAverage = hbpCoreVolume.GetAverageValueAroundPositionWithMask(new Vector3(-2.0f, 3.0f, 4.0f), 1, hbpCoreMaskVolume, ref hbpCoreRawValues, ref hbpCoreActualLength);

            float[] expectedFixtureValues =
            {
                38, 63, 88, 43, 68, 93, 48, 73, 98,
                39, 64, 89, 44, 69, 94, 49, 74, 99
            };
            Assert.That(hbpCoreActualLength, Is.EqualTo(expectedFixtureValues.Length), "3x3x2 in-bounds neighborhood at native voxel (2,3,4)");
            Assert.That(hbpCoreRawValues.Take(hbpCoreActualLength), Is.EqualTo(expectedFixtureValues).Within(0.0001f), "fmri_3d fixture raw neighborhood values");
            Assert.That(hbpCoreAverage, Is.EqualTo(68.5f).Within(0.0001f), "arithmetic mean of the fixture neighborhood");
            Assert.That(hbpCoreAverage, Is.EqualTo(hbpCoreRawValues.Take(hbpCoreActualLength).Average()).Within(0.0001f));
            TestContext.Progress.WriteLine($"masked average: hbp_core={hbpCoreAverage} ({hbpCoreActualLength} values, independent fixture oracle=68.5); hbp_export={hbpExportAverage} ({hbpExportActualLength} values, retained diagnostic only)");

            foreach (CutOrientation orientation in new[] { CutOrientation.Axial, CutOrientation.Coronal, CutOrientation.Sagittal })
            {
                foreach (bool flip in new[] { false, true })
                {
                    NativeParityAssert.AssertUnityVectorMatchesLegacyNative(hbpCoreVolume.GetOrientationVector(orientation, flip), hbpExportVolume.GetOrientationVector(orientation, flip), context: $"{orientation} flip={flip} normal");
                }
            }

            foreach (Vector3 normal in new[] { Vector3.right, Vector3.up, Vector3.forward, new Vector3(1.0f, 1.0f, 1.0f).normalized })
            {
                using HbpPlane plane = new(hbpExportVolume.Center, normal);
                AssertCorrectedCutOffset(hbpCoreVolume.SizeOffsetCutPlane(plane, 8), hbpExportVolume.SizeOffsetCutPlane(plane, 8), hbpCoreBBox.Min, hbpCoreBBox.Max, normal, 8, normal.ToString());
            }
        }

        private static void AssertCorrectedCutOffset(float hbpCoreOffset, float hbpExportOffset, Vector3 min, Vector3 max, Vector3 normal, int cutCount, string context)
        {
            Vector3 direction = normal.normalized;
            Vector3 center = (min + max) * 0.5f;
            float exitDistance = float.PositiveInfinity;

            for (int axis = 0; axis < 3; ++axis)
            {
                float component = direction[axis];
                if (Mathf.Abs(component) <= 1e-6f)
                {
                    continue;
                }

                float boundary = component > 0.0f ? max[axis] : min[axis];
                exitDistance = Mathf.Min(exitDistance, (boundary - center[axis]) / component);
            }

            float expected = 2.0f * exitDistance / cutCount;
            Assert.That(hbpCoreOffset, Is.EqualTo(expected).Within(1e-5f), $"{context}: exact bbox exit");

            // hbp_export advances by bboxDiagonal / 5000 and accumulates floating-point
            // error. Retain that stepped result as a bounded diagnostic, not as the oracle.
            float legacyStepBound = 2.0f * (max - min).magnitude / (5000.0f * cutCount) + 1e-5f;
            Assert.That(hbpExportOffset, Is.EqualTo(expected).Within(legacyStepBound), $"{context}: bounded legacy stepping error");
            TestContext.Progress.WriteLine($"{context} cut offset: exact hbp_core={hbpCoreOffset}; stepped hbp_export={hbpExportOffset}; oracle={expected}");
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeParity")]
        [Category(NativeParityAssert.IndependentOracle)]
        public void NiftiMetadataAndExtractedVolumes_MatchAcrossBackends()
        {
            NativeParityAssert.RequireHbpCore();

            using NIFTI hbpExportNifti = LoadNifti(BenchmarkBackend.HbpExport, "fmri_4d.nii.gz");
            using NIFTI hbpCoreNifti = LoadNifti(BenchmarkBackend.HbpCore, "fmri_4d.nii.gz");

            Assert.That(hbpCoreNifti.NumberOfVolumes, Is.EqualTo(hbpExportNifti.NumberOfVolumes));
            Assert.That(hbpCoreNifti.StartTime, Is.EqualTo(hbpExportNifti.StartTime).Within(0.0001f));
            Assert.That(hbpCoreNifti.TimeStep, Is.EqualTo(hbpExportNifti.TimeStep).Within(0.0001f));
            Assert.That(hbpCoreNifti.TimeUnit, Is.EqualTo(hbpExportNifti.TimeUnit));
            NativeParityAssert.AssertMriCalValues(hbpCoreNifti.ExtremeValues, hbpExportNifti.ExtremeValues);

            using Volume hbpExportFirstVolume = LoadVolume(BenchmarkBackend.HbpExport, "fmri_4d.nii.gz");
            using Volume hbpCoreFirstVolume = hbpCoreNifti.ExtractVolume(0);
            Assert.That(hbpCoreFirstVolume.IsLoaded, Is.True);
            NativeParityAssert.AssertUnityVectorMatchesLegacyNative(hbpCoreFirstVolume.Center, hbpExportFirstVolume.Center, context: "fmri_4d volume[0] center");
            NativeParityAssert.AssertVector(hbpCoreFirstVolume.Center, new Vector3(-2.0f, 2.0f, 2.0f), context: "fmri_4d fixture center in Unity");
            NativeParityAssert.AssertVector(hbpCoreFirstVolume.Spacing, hbpExportFirstVolume.Spacing, context: "fmri_4d spacing (unsigned magnitude)");
            NativeParityAssert.AssertMriCalValues(hbpCoreFirstVolume.ExtremeValues, hbpExportFirstVolume.ExtremeValues);
            Assert.That(hbpCoreFirstVolume.GetValueFromPosition(new Vector3(-2.0f, 3.0f, 4.0f)), Is.EqualTo(hbpExportFirstVolume.GetValueFromPosition(new Vector3(-2.0f, 3.0f, 4.0f))).Within(0.0001f));

            using Volume hbpCoreSecondVolume = hbpCoreNifti.ExtractVolume(Math.Min(1, hbpCoreNifti.NumberOfVolumes - 1));
            Assert.That(hbpCoreSecondVolume.IsLoaded, Is.True);
        }

        private static Volume LoadVolume(BenchmarkBackend backend, string fixtureName)
        {
            return NativeParityAssert.WithBackend(backend, () =>
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

        private static NIFTI LoadNifti(BenchmarkBackend backend, string fixtureName)
        {
            return NativeParityAssert.WithBackend(backend, () =>
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

        private static void AssertBBoxEdgesUseDistinctBufferPairs(IReadOnlyList<Vector3> corners, IReadOnlyList<HbpSegment3> segments)
        {
            HashSet<string> uniqueEdges = new();
            foreach (HbpSegment3 segment in segments)
            {
                int first = FindCorner(corners, segment.End1);
                int second = FindCorner(corners, segment.End2);
                Assert.That(first, Is.GreaterThanOrEqualTo(0), $"Legacy bbox segment start {segment.End1} is not a bbox corner");
                Assert.That(second, Is.GreaterThanOrEqualTo(0), $"Legacy bbox segment end {segment.End2} is not a bbox corner");
                Assert.That(first, Is.Not.EqualTo(second), "Legacy bbox segment must not be degenerate");

                Vector3 delta = segment.End2 - segment.End1;
                int differingAxes = (Mathf.Abs(delta.x) > 0.0001f ? 1 : 0) + (Mathf.Abs(delta.y) > 0.0001f ? 1 : 0) + (Mathf.Abs(delta.z) > 0.0001f ? 1 : 0);
                Assert.That(differingAxes, Is.EqualTo(1), "Every bbox edge must be axis aligned");

                uniqueEdges.Add(first < second ? $"{first}:{second}" : $"{second}:{first}");
            }

            Assert.That(uniqueEdges, Has.Count.EqualTo(12), "The legacy wrapper must consume each six-float segment pair exactly once");
        }

        private static int FindCorner(IReadOnlyList<Vector3> corners, Vector3 candidate)
        {
            for (int i = 0; i < corners.Count; ++i)
            {
                if (Vector3.Distance(corners[i], candidate) <= 0.0001f)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
