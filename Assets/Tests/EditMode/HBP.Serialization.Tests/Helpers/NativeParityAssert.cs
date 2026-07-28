using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HBP.Core.DLL;
using HBP.Core.DLL.HbpCore;
using HBP.Core.Tools;
using NUnit.Framework;
using UnityEngine;

namespace HBP.Tests.Serialization.Helpers
{
    internal static class NativeParityAssert
    {
        public const float DefaultTolerance = 0.0001f;
        public const string StrictParity = "NativeParity.Strict";
        public const string NormalizedCoordinateParity = "NativeParity.NormalizedCoordinates";
        public const string IntentionalCorrection = "NativeParity.IntentionalCorrection";
        public const string IndependentOracle = "NativeParity.IndependentOracle";

        public static Vector3 NativeToUnity(Vector3 nativeValue)
        {
            return new Vector3(ReferenceSystemConversion.ConvertX(nativeValue.x), nativeValue.y, nativeValue.z);
        }

        public static void NativeBoundsToUnity(Vector3 nativeMin, Vector3 nativeMax, out Vector3 unityMin, out Vector3 unityMax)
        {
            Vector3 first = NativeToUnity(nativeMin);
            Vector3 second = NativeToUnity(nativeMax);
            unityMin = Vector3.Min(first, second);
            unityMax = Vector3.Max(first, second);
        }

        public static void RequireHbpCore()
        {
            if (!HbpCoreRuntime.TryGetVersion(out _, out string error))
            {
                Assert.Ignore($"hbp_core is not installed next to hbp_export yet: {error}");
            }
        }

        public static T WithBackend<T>(BenchmarkBackend backend, Func<T> action)
        {
            BenchmarkBackend previousBackend = OracleBackendContext.Current;
            OracleBackendContext.Current = backend;
            try
            {
                return ExecuteNativeOrIgnore(action, $"{backend} backend");
            }
            finally
            {
                OracleBackendContext.Current = previousBackend;
            }
        }

        public static void WithBackend(BenchmarkBackend backend, Action action)
        {
            WithBackend(backend, () =>
            {
                action();
                return true;
            });
        }

        public static T ExecuteNativeOrIgnore<T>(Func<T> action, string context)
        {
            try
            {
                return action();
            }
            catch (Exception exception) when (IsMissingNativeDependency(exception))
            {
                Assert.Ignore($"Native dependency unavailable for {context}: {exception.Message}");
                throw;
            }
        }

        public static string NativePath(params string[] parts)
        {
            string path = TestPathUtility.FixturePath("Native");
            foreach (string part in parts)
            {
                path = Path.Combine(path, part);
            }

            return path;
        }

        public static void AssertVector(Vector3 actual, Vector3 expected, float tolerance = DefaultTolerance, string context = null)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(tolerance), $"{context ?? "Vector3"}.x");
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(tolerance), $"{context ?? "Vector3"}.y");
            Assert.That(actual.z, Is.EqualTo(expected.z).Within(tolerance), $"{context ?? "Vector3"}.z");
        }

        public static void AssertUnityVectorMatchesLegacyNative(Vector3 actualUnity, Vector3 legacyNative, float tolerance = DefaultTolerance, string context = null)
        {
            Vector3 expectedUnity = NativeToUnity(legacyNative);
            AssertVector(actualUnity, expectedUnity, tolerance, $"{context ?? "coordinate"} (actual=hbp_core Unity; expected=R*hbp_export native; R=diag({(ReferenceSystemConversion.InvertX ? "-1" : "1")},1,1))");
        }

        public static void AssertUnityBoundsMatchLegacyNative(Vector3 actualUnityMin, Vector3 actualUnityMax, Vector3 legacyNativeMin, Vector3 legacyNativeMax, float tolerance = DefaultTolerance, string context = null)
        {
            NativeBoundsToUnity(legacyNativeMin, legacyNativeMax, out Vector3 expectedUnityMin, out Vector3 expectedUnityMax);
            string conversion = $"{context ?? "bounds"} (actual=hbp_core Unity; expected=R*hbp_export native with min/max reordered)";
            AssertVector(actualUnityMin, expectedUnityMin, tolerance, $"{conversion}.min");
            AssertVector(actualUnityMax, expectedUnityMax, tolerance, $"{conversion}.max");
        }

        public static void AssertVector(Vector2 actual, Vector2 expected, float tolerance = DefaultTolerance)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(tolerance));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(tolerance));
        }

        public static void AssertColor(Color actual, Color expected, float tolerance = DefaultTolerance)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(tolerance));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(tolerance));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(tolerance));
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(tolerance));
        }

        public static void AssertMesh(Mesh actual, Mesh expected, float tolerance = DefaultTolerance)
        {
            Assert.That(actual.vertexCount, Is.EqualTo(expected.vertexCount));
            AssertSameVectorArray(actual.vertices, expected.vertices, tolerance);
            AssertSameVectorArray(actual.normals, expected.normals, tolerance);
            AssertSameVectorArray(actual.uv, expected.uv, tolerance);
            AssertSameColorArray(actual.colors, expected.colors, tolerance);
            Assert.That(actual.triangles, Is.EqualTo(expected.triangles));
        }

        public static void AssertSameVectorArray(IReadOnlyList<Vector3> actual, IReadOnlyList<Vector3> expected, float tolerance = DefaultTolerance)
        {
            Assert.That(actual.Count, Is.EqualTo(expected.Count));
            for (int i = 0; i < expected.Count; ++i)
            {
                AssertVector(actual[i], expected[i], tolerance);
            }
        }

        public static void AssertSameVectorArray(IReadOnlyList<Vector2> actual, IReadOnlyList<Vector2> expected, float tolerance = DefaultTolerance)
        {
            Assert.That(actual.Count, Is.EqualTo(expected.Count));
            for (int i = 0; i < expected.Count; ++i)
            {
                AssertVector(actual[i], expected[i], tolerance);
            }
        }

        public static void AssertSameColorArray(IReadOnlyList<Color> actual, IReadOnlyList<Color> expected, float tolerance = DefaultTolerance)
        {
            Assert.That(actual.Count, Is.EqualTo(expected.Count));
            for (int i = 0; i < expected.Count; ++i)
            {
                AssertColor(actual[i], expected[i], tolerance);
            }
        }

        public static void AssertSameColor32Array(IReadOnlyList<Color32> actual, IReadOnlyList<Color32> expected, byte tolerance = 0)
        {
            Assert.That(actual.Count, Is.EqualTo(expected.Count));
            for (int i = 0; i < expected.Count; ++i)
            {
                Assert.That(Mathf.Abs(actual[i].r - expected[i].r), Is.LessThanOrEqualTo(tolerance), $"r[{i}]");
                Assert.That(Mathf.Abs(actual[i].g - expected[i].g), Is.LessThanOrEqualTo(tolerance), $"g[{i}]");
                Assert.That(Mathf.Abs(actual[i].b - expected[i].b), Is.LessThanOrEqualTo(tolerance), $"b[{i}]");
                Assert.That(Mathf.Abs(actual[i].a - expected[i].a), Is.LessThanOrEqualTo(tolerance), $"a[{i}]");
            }
        }

        public static void AssertSameVectorSet(IEnumerable<Vector3> actual, IEnumerable<Vector3> expected, float tolerance = DefaultTolerance)
        {
            List<Vector3> actualPoints = actual.ToList();
            List<Vector3> expectedPoints = expected.ToList();
            Assert.That(actualPoints, Has.Count.EqualTo(expectedPoints.Count));

            List<Vector3> remaining = new(actualPoints);
            foreach (Vector3 expectedPoint in expectedPoints)
            {
                int foundIndex = remaining.FindIndex(actualPoint => VectorsEqual(actualPoint, expectedPoint, tolerance));
                Assert.That(foundIndex, Is.GreaterThanOrEqualTo(0), $"Missing point {expectedPoint}");
                remaining.RemoveAt(foundIndex);
            }
        }

        public static void AssertUnityVectorSetMatchesLegacyNative(IEnumerable<Vector3> actualUnity, IEnumerable<Vector3> legacyNative, float tolerance = DefaultTolerance)
        {
            AssertSameVectorSet(actualUnity, legacyNative.Select(NativeToUnity), tolerance);
        }

        public static void NormalizeLegacyMeshToUnity(Mesh mesh)
        {
            mesh.vertices = mesh.vertices.Select(NativeToUnity).ToArray();
            mesh.normals = mesh.normals.Select(NativeToUnity).ToArray();
            mesh.triangles = ReferenceSystemConversion.ConvertTriangleWinding(mesh.triangles);
        }

        public static void AssertSameSegmentSet(IEnumerable<Segment3> actual, IEnumerable<Segment3> expected, float tolerance = DefaultTolerance)
        {
            List<Segment3> actualSegments = actual.ToList();
            List<Segment3> expectedSegments = expected.ToList();
            Assert.That(actualSegments, Has.Count.EqualTo(expectedSegments.Count));

            List<Segment3> remaining = new(actualSegments);
            foreach (Segment3 expectedSegment in expectedSegments)
            {
                int foundIndex = remaining.FindIndex(actualSegment => SegmentsEqual(actualSegment, expectedSegment, tolerance));
                Assert.That(foundIndex, Is.GreaterThanOrEqualTo(0), $"Missing segment {expectedSegment.End1} -> {expectedSegment.End2}");
                remaining.RemoveAt(foundIndex);
            }
        }

        public static void AssertUnitySegmentSetMatchesLegacyNative(IEnumerable<Segment3> actualUnity, IEnumerable<Segment3> legacyNative, float tolerance = DefaultTolerance)
        {
            List<Segment3> actualSegments = actualUnity.ToList();
            List<Segment3> remaining = new(actualSegments);
            foreach (Segment3 legacySegment in legacyNative)
            {
                Vector3 expectedEnd1 = NativeToUnity(legacySegment.End1);
                Vector3 expectedEnd2 = NativeToUnity(legacySegment.End2);
                int foundIndex = remaining.FindIndex(actualSegment => VectorsEqual(actualSegment.End1, expectedEnd1, tolerance) && VectorsEqual(actualSegment.End2, expectedEnd2, tolerance) || VectorsEqual(actualSegment.End1, expectedEnd2, tolerance) && VectorsEqual(actualSegment.End2, expectedEnd1, tolerance));
                Assert.That(foundIndex, Is.GreaterThanOrEqualTo(0), $"Missing Unity segment converted from legacy native {legacySegment.End1} -> {legacySegment.End2}");
                remaining.RemoveAt(foundIndex);
            }

            Assert.That(remaining, Is.Empty, "Unexpected additional Unity segments");
        }

        public static void AssertMriCalValues(MRICalValues actual, MRICalValues expected, float tolerance = DefaultTolerance)
        {
            Assert.That(actual.Min, Is.EqualTo(expected.Min).Within(tolerance));
            Assert.That(actual.Max, Is.EqualTo(expected.Max).Within(tolerance));
            Assert.That(actual.LoadedCalMin, Is.EqualTo(expected.LoadedCalMin).Within(tolerance));
            Assert.That(actual.LoadedCalMax, Is.EqualTo(expected.LoadedCalMax).Within(tolerance));
            Assert.That(actual.ComputedCalMin, Is.EqualTo(expected.ComputedCalMin).Within(tolerance));
            Assert.That(actual.ComputedCalMax, Is.EqualTo(expected.ComputedCalMax).Within(tolerance));
        }

        public static void DisposeSegments(IEnumerable<Segment3> segments)
        {
            foreach (Segment3 segment in segments)
            {
                segment.Dispose();
            }
        }

        private static bool IsMissingNativeDependency(Exception exception)
        {
            for (Exception current = exception; current != null; current = current.InnerException)
            {
                if (current is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool SegmentsEqual(Segment3 actual, Segment3 expected, float tolerance)
        {
            return VectorsEqual(actual.End1, expected.End1, tolerance) && VectorsEqual(actual.End2, expected.End2, tolerance) || VectorsEqual(actual.End1, expected.End2, tolerance) && VectorsEqual(actual.End2, expected.End1, tolerance);
        }

        private static bool VectorsEqual(Vector3 actual, Vector3 expected, float tolerance)
        {
            return Mathf.Abs(actual.x - expected.x) <= tolerance && Mathf.Abs(actual.y - expected.y) <= tolerance && Mathf.Abs(actual.z - expected.z) <= tolerance;
        }
    }
}
