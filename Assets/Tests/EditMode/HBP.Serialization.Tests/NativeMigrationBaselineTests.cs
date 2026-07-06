using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using HBP.Core.DLL;
using HBP.Core.DLL.HbpCore;
using HBP.Tests.Serialization.Helpers;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using HbpPlane = HBP.Core.DLL.Plane;
using HbpSegment3 = HBP.Core.DLL.Segment3;

namespace HBP.Tests.Serialization
{
    public class NativeMigrationBaselineTests
    {
        private static readonly Regex DllImportRegex = new(
            "\\[DllImport\\((?:\"(?<dll>[^\"]+)\"|NativeDll\\.(?<nativeDll>HbpExport|HbpCore))\\s*,\\s*EntryPoint\\s*=\\s*\"(?<entry>[^\"]+)\"",
            RegexOptions.Compiled);

        [Test]
        [Category("NativeMigration")]
        public void NativeBackendConstants_DeclareHistoricalAndCoreDllNames()
        {
            Assert.That(NativeDll.HbpExport, Is.EqualTo("hbp_export"));
            Assert.That(NativeDll.HbpCore, Is.EqualTo("hbp_core"));
            Assert.That(NativeBackend.HbpExport.ToString(), Is.EqualTo("HbpExport"));
            Assert.That(NativeBackend.HbpCore.ToString(), Is.EqualTo("HbpCore"));
            Assert.That(NativeBackendOptions.ExperimentalBackend, Is.EqualTo(NativeBackend.HbpExport));
            Assert.That(NativeBackendOptions.UsesHbpCore, Is.False);
        }

        [Test]
        [Category("NativeMigration")]
        public void CurrentDllImportInventory_KeepsHistoricalImportsAndAddsHbpCoreObjectWrappers()
        {
            List<DllImportSignature> imports = ReadCurrentDllImports();

            Assert.That(imports, Has.Count.EqualTo(350));
            Assert.That(imports.Count(imported => imported.Dll == NativeDll.HbpExport), Is.EqualTo(219));
            Assert.That(imports.Count(imported => imported.Dll == "EEGFormat"), Is.EqualTo(37));
            Assert.That(imports.Count(imported => imported.Dll == "hbp_math"), Is.EqualTo(17));
            string[] hbpCoreImportFiles = imports
                .Where(imported => imported.Dll == NativeDll.HbpCore)
                .Select(imported => imported.RelativeFile)
                .Distinct()
                .ToArray();
            Assert.That(hbpCoreImportFiles, Is.EquivalentTo(new[] { "BBox.cs", "HbpCore/HbpCoreRuntime.cs", "NIFTI.cs", "Plane.cs", "Segment3.cs", "Surface.cs", "Transformation3.cs", "Volume.cs" }));
            Assert.That(imports.Count(imported => imported.Dll == NativeDll.HbpCore), Is.EqualTo(77));
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeDll")]
        public void HistoricalWrapper_LoadsThroughHbpExportWithoutHbpCoreMigration()
        {
            BBox bbox = ExecuteNativeOrIgnore(() => new BBox(), "historical BBox wrapper");
            try
            {
                Assert.That(bbox.getHandle().Handle, Is.Not.EqualTo(IntPtr.Zero));
            }
            finally
            {
                bbox.Dispose();
            }
        }

        [Test]
        [Category("NativeMigration")]
        public void ExperimentalBackendOption_CreatesBBoxThroughHbpCore()
        {
            if (!HbpCoreRuntime.TryGetVersion(out _, out string error))
            {
                Assert.Ignore($"hbp_core is not installed next to hbp_export yet: {error}");
            }

            NativeBackendOptions.ExperimentalBackend = NativeBackend.HbpCore;
            try
            {
                BBox bbox = ExecuteNativeOrIgnore(() => new BBox(), "hbp_core BBox wrapper");
                try
                {
                    Assert.That(NativeBackendOptions.UsesHbpCore, Is.True);
                    Assert.That(bbox.getHandle().Handle, Is.Not.EqualTo(IntPtr.Zero));
                    Assert.DoesNotThrow(() => _ = bbox.Min);
                }
                finally
                {
                    bbox.Dispose();
                }
            }
            finally
            {
                NativeBackendOptions.Reset();
            }
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeDll")]
        public void HbpCoreSmoke_LoadsVersion_WhenLibraryIsPresent()
        {
            if (!HbpCoreRuntime.TryGetVersion(out string version, out string error))
            {
                Assert.Ignore($"hbp_core is not installed next to hbp_export yet: {error}");
            }

            Assert.That(version, Is.Not.Empty);
            Assert.That(HbpCoreRuntime.Init(), Is.EqualTo(HbpCoreStatus.Ok));
            Assert.That(HbpCoreRuntime.LastError, Is.Empty);
            Assert.That(HbpCoreRuntime.Shutdown(), Is.EqualTo(HbpCoreStatus.Ok));
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeDll")]
        public void DLLDebugManager_ReceivesHbpCoreDebugMessage_WhenLibraryIsPresent()
        {
            if (!DLLDebugManager.TryAttachHbpCoreLogger(out string attachError))
            {
                Assert.Ignore($"hbp_core debug callback is not available yet: {attachError}");
            }

            const string message = "hbp_core unity callback";
            try
            {
                LogAssert.Expect(LogType.Warning, message);
                Assert.That(HbpCoreRuntime.DebugMessage(message, HbpCoreLogType.Warning), Is.EqualTo(HbpCoreStatus.Ok));
            }
            finally
            {
                DLLDebugManager.TryResetHbpCoreLogger(out _);
            }
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeDll")]
        public void HbpCoreBBox_ReturnsBoundsAndPlaneIntersections_WhenLibraryIsPresent()
        {
            if (!HbpCoreRuntime.TryGetVersion(out _, out string error))
            {
                Assert.Ignore($"hbp_core is not installed next to hbp_export yet: {error}");
            }

            using BBox bbox = BBox.CreateHbpCore(new Vector3(-1, -2, -3), new Vector3(3, 4, 5));

            AssertVector(bbox.Min, new Vector3(-1, -2, -3));
            AssertVector(bbox.Max, new Vector3(3, 4, 5));
            AssertVector(bbox.Center, new Vector3(1, 1, 1));
            Assert.That(bbox.Points, Has.Count.EqualTo(8));
            List<HbpSegment3> bboxSegments = bbox.Segments;
            Assert.That(bboxSegments, Has.Count.EqualTo(12));
            DisposeSegments(bboxSegments);

            using HbpPlane plane = new(new Vector3(0, 0, 1), new Vector3(0, 0, 2));
            Assert.That(plane.PointSide(new Vector3(0, 0, 3)), Is.EqualTo(1));
            AssertVector(plane.ProjectPoint(new Vector3(2, 3, 4)), new Vector3(2, 3, 1));
            Assert.That(plane.IntersectSegment(new Vector3(0, 0, -1), new Vector3(0, 0, 3), out Vector3 planeSegmentPoint), Is.True);
            AssertVector(planeSegmentPoint, new Vector3(0, 0, 1));
            plane.Point = new Vector3(0, 0, 2);
            AssertVector(plane.ProjectPoint(new Vector3(2, 3, 4)), new Vector3(2, 3, 2));
            plane.Point = new Vector3(0, 0, 1);

            List<Vector3> intersections = bbox.IntersectionPointsWithPlane(plane);
            Assert.That(intersections, Has.Count.EqualTo(4));
            Assert.That(intersections.All(point => Mathf.Abs(point.z - 1) < 0.0001f), Is.True);

            List<HbpSegment3> segments = bbox.IntersectionLinesWithPlane(plane);
            Assert.That(segments, Has.Count.EqualTo(4));
            Assert.That(segments.All(segment => Mathf.Abs(segment.End1.z - 1) < 0.0001f && Mathf.Abs(segment.End2.z - 1) < 0.0001f), Is.True);
            DisposeSegments(segments);

            using HbpPlane planeA = new(new Vector3(1, 0, 0), Vector3.right);
            using HbpPlane planeB = new(new Vector3(0, 1, 0), Vector3.up);
            HbpSegment3 segment = bbox.IntersectionSegmentBetweenTwoPlanes(planeA, planeB);

            Assert.That(segment, Is.Not.Null);
            AssertVector(segment.End1, new Vector3(1, 1, -3));
            AssertVector(segment.End2, new Vector3(1, 1, 5));
            Assert.That(segment.Length, Is.EqualTo(8.0f).Within(0.0001f));
            segment.Dispose();

            using Transformation3 transformation = new(
                new[]
                {
                    0.0f, -1.0f, 0.0f,
                    1.0f, 0.0f, 0.0f,
                    0.0f, 0.0f, 1.0f
                },
                new Vector3(10, 20, 30));
            AssertVector(transformation.ApplyPoint(new Vector3(1, 2, 3)), new Vector3(8, 21, 33));

            using BBox transformed = BBox.CreateHbpCore(new Vector3(-1, -1, -1), new Vector3(1, 1, 1));
            transformed.Transform(transformation);
            AssertVector(transformed.Min, new Vector3(9, 19, 29));
            AssertVector(transformed.Max, new Vector3(11, 21, 31));
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeDll")]
        public void HbpCoreBBox_MatchesHbpExportBoundingBox_WhenUsingSameVolumeBounds()
        {
            if (!HbpCoreRuntime.TryGetVersion(out _, out string error))
            {
                Assert.Ignore($"hbp_core is not installed next to hbp_export yet: {error}");
            }

            using Volume volume = ExecuteNativeOrIgnore(() => new Volume(), "historical Volume wrapper");
            Assert.That(volume.LoadNIFTIFile(NativePath("Nifti", "fmri_3d.nii")), Is.True);

            using BBox hbpExportBBox = volume.BoundingBox;
            using BBox hbpCoreBBox = BBox.CreateHbpCore(hbpExportBBox.Min, hbpExportBBox.Max);

            AssertVector(hbpCoreBBox.Min, hbpExportBBox.Min);
            AssertVector(hbpCoreBBox.Max, hbpExportBBox.Max);
            AssertVector(hbpCoreBBox.Center, hbpExportBBox.Center);
            AssertSameVectorSet(hbpCoreBBox.Points, hbpExportBBox.Points);
            List<HbpSegment3> hbpCoreSegments = hbpCoreBBox.Segments;
            List<HbpSegment3> hbpExportSegments = hbpExportBBox.Segments;
            Assert.That(hbpCoreSegments, Has.Count.EqualTo(hbpExportSegments.Count));
            DisposeSegments(hbpCoreSegments);
            DisposeSegments(hbpExportSegments);

            using HbpPlane plane = new(hbpExportBBox.Center, Vector3.forward);
            AssertSameVectorSet(
                hbpCoreBBox.IntersectionPointsWithPlane(plane),
                hbpExportBBox.IntersectionPointsWithPlane(plane));

            using HbpPlane hbpCorePlaneA = new(hbpExportBBox.Center, Vector3.right);
            using HbpPlane hbpCorePlaneB = new(hbpExportBBox.Center, Vector3.up);
            HbpSegment3 hbpCoreSegment = hbpCoreBBox.IntersectionSegmentBetweenTwoPlanes(hbpCorePlaneA, hbpCorePlaneB);
            HbpSegment3 hbpExportSegment = hbpExportBBox.IntersectionSegmentBetweenTwoPlanes(hbpCorePlaneA, hbpCorePlaneB);

            Assert.That(hbpCoreSegment, Is.Not.Null);
            Assert.That(hbpExportSegment, Is.Not.Null);
            AssertSameVectorSet(
                new[] { hbpCoreSegment.End1, hbpCoreSegment.End2 },
                new[] { hbpExportSegment.End1, hbpExportSegment.End2 });
            hbpCoreSegment.Dispose();
            hbpExportSegment.Dispose();
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeDll")]
        public void HbpCoreVolumeAndNifti_LoadReadOnlyFixtures_WhenLibraryIsPresent()
        {
            if (!HbpCoreRuntime.TryGetVersion(out _, out string error))
            {
                Assert.Ignore($"hbp_core is not installed next to hbp_export yet: {error}");
            }

            using Volume hbpExportVolume = ExecuteNativeOrIgnore(() => new Volume(), "historical Volume wrapper");
            Assert.That(hbpExportVolume.LoadNIFTIFile(NativePath("Nifti", "fmri_3d.nii")), Is.True);

            NativeBackendOptions.ExperimentalBackend = NativeBackend.HbpCore;
            try
            {
                using Volume hbpCoreVolume = ExecuteNativeOrIgnore(() => new Volume(), "hbp_core Volume wrapper");
                Assert.That(hbpCoreVolume.LoadNIFTIFile(NativePath("Nifti", "fmri_3d.nii")), Is.True);
                Assert.That(hbpCoreVolume.IsLoaded, Is.True);
                AssertVector(hbpCoreVolume.Center, hbpExportVolume.Center);
                AssertVector(hbpCoreVolume.Spacing, hbpExportVolume.Spacing);
                Assert.That(hbpCoreVolume.GetValueFromPosition(new Vector3(-2, 3, 4)), Is.EqualTo(69.0f).Within(0.0001f));

                using BBox hbpExportBBox = hbpExportVolume.BoundingBox;
                using BBox hbpCoreBBox = hbpCoreVolume.BoundingBox;
                AssertVector(hbpCoreBBox.Min, hbpExportBBox.Min);
                AssertVector(hbpCoreBBox.Max, hbpExportBBox.Max);

                using HbpPlane cutPlane = new(hbpCoreVolume.Center, Vector3.forward);
                Assert.That(hbpCoreVolume.SizeOffsetCutPlane(cutPlane, 10), Is.GreaterThan(0.0f));

                using NIFTI nifti = ExecuteNativeOrIgnore(() => new NIFTI(), "hbp_core NIFTI wrapper");
                Assert.That(nifti.Load(NativePath("Nifti", "fmri_4d.nii.gz")), Is.True);
                Assert.That(nifti.NumberOfVolumes, Is.GreaterThan(1));
                Assert.That(nifti.TimeStep, Is.GreaterThan(0.0f));
                Assert.That(nifti.TimeUnit, Is.Not.Null);

                using Volume extractedVolume = nifti.ExtractVolume(1);
                Assert.That(extractedVolume.IsLoaded, Is.True);
                AssertVector(extractedVolume.Spacing, hbpCoreVolume.Spacing);
            }
            finally
            {
                NativeBackendOptions.Reset();
            }
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeDll")]
        public void HbpCoreSurface_CreatesUnityMeshFromBuffers_WhenLibraryIsPresent()
        {
            if (!HbpCoreRuntime.TryGetVersion(out _, out string error))
            {
                Assert.Ignore($"hbp_core is not installed next to hbp_export yet: {error}");
            }

            NativeBackendOptions.ExperimentalBackend = NativeBackend.HbpCore;
            Mesh mesh = new();
            try
            {
                using Surface surface = ExecuteNativeOrIgnore(() => new Surface(), "hbp_core Surface wrapper");
                surface.SetBuffers(
                    new[]
                    {
                        new Vector3(0, 0, 0),
                        new Vector3(1, 0, 0),
                        new Vector3(1, 1, 0),
                        new Vector3(0, 1, 0)
                    },
                    new[] { 0, 1, 2, 0, 2, 3 },
                    uv: new[]
                    {
                        new Vector2(0, 0),
                        new Vector2(1, 0),
                        new Vector2(1, 1),
                        new Vector2(0, 1)
                    },
                    colors: new[]
                    {
                        Color.red,
                        Color.green,
                        Color.blue,
                        Color.white
                    });
                surface.ComputeNormals();
                surface.UpdateMeshFromDLL(mesh);

                Assert.That(surface.NumberOfVertices, Is.EqualTo(4));
                Assert.That(surface.NumberOfTriangles, Is.EqualTo(2));
                Assert.That(mesh.vertexCount, Is.EqualTo(4));
                Assert.That(mesh.triangles, Is.EqualTo(new[] { 0, 1, 2, 0, 2, 3 }));
                AssertVector(mesh.vertices[2], new Vector3(1, 1, 0));
                AssertVector(mesh.normals[0], Vector3.forward);
                Assert.That(mesh.uv, Has.Length.EqualTo(4));
                Assert.That(mesh.colors, Has.Length.EqualTo(4));

                using BBox bbox = surface.BoundingBox;
                AssertVector(bbox.Min, Vector3.zero);
                AssertVector(bbox.Max, new Vector3(1, 1, 0));

                using Surface clone = (Surface)surface.Clone();
                clone.Append(surface);
                Assert.That(clone.NumberOfVertices, Is.EqualTo(8));
                Assert.That(clone.NumberOfTriangles, Is.EqualTo(4));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(mesh);
                NativeBackendOptions.Reset();
            }
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeDll")]
        public void HbpCoreSurface_LoadsGiftiFixture_WhenLibraryIsPresent()
        {
            if (!HbpCoreRuntime.TryGetVersion(out _, out string error))
            {
                Assert.Ignore($"hbp_core is not installed next to hbp_export yet: {error}");
            }

            NativeBackendOptions.ExperimentalBackend = NativeBackend.HbpCore;
            Mesh mesh = new();
            try
            {
                using Surface surface = ExecuteNativeOrIgnore(() => new Surface(), "hbp_core Surface wrapper");
                Assert.That(surface.LoadGIIFile(NativePath("Meshes", "single_surface.gii"), NativePath("Meshes", "MNI.trm")), Is.True);
                surface.UpdateMeshFromDLL(mesh);

                Assert.That(surface.NumberOfVertices, Is.EqualTo(4));
                Assert.That(surface.NumberOfTriangles, Is.EqualTo(4));
                Assert.That(mesh.vertexCount, Is.EqualTo(4));
                Assert.That(mesh.triangles, Has.Length.EqualTo(12));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(mesh);
                NativeBackendOptions.Reset();
            }
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeDll")]
        public void HbpCoreSurface_MatchesHbpExportObjCube_WhenLibraryIsPresent()
        {
            if (!HbpCoreRuntime.TryGetVersion(out _, out string error))
            {
                Assert.Ignore($"hbp_core is not installed next to hbp_export yet: {error}");
            }

            string objPath = Path.Combine(Path.GetTempPath(), "hbp_core_surface_cube_compare.obj");
            File.WriteAllText(objPath, CubeObjFixture());

            Mesh hbpExportMesh = new();
            Mesh hbpCoreMesh = new();
            try
            {
                NativeBackendOptions.Reset();
                using Surface hbpExportSurface = ExecuteNativeOrIgnore(() => new Surface(), "hbp_export Surface wrapper");
                Assert.That(hbpExportSurface.LoadOBJFile(objPath), Is.True);
                hbpExportSurface.UpdateMeshFromDLL(hbpExportMesh);

                NativeBackendOptions.ExperimentalBackend = NativeBackend.HbpCore;
                using Surface hbpCoreSurface = ExecuteNativeOrIgnore(() => new Surface(), "hbp_core Surface wrapper");
                Assert.That(hbpCoreSurface.LoadOBJFile(objPath), Is.True);
                hbpCoreSurface.UpdateMeshFromDLL(hbpCoreMesh);

                Assert.That(hbpCoreSurface.NumberOfVertices, Is.EqualTo(hbpExportSurface.NumberOfVertices));
                Assert.That(hbpCoreSurface.NumberOfTriangles, Is.EqualTo(hbpExportSurface.NumberOfTriangles));
                using BBox hbpExportBBox = hbpExportSurface.BoundingBox;
                using BBox hbpCoreBBox = hbpCoreSurface.BoundingBox;
                AssertVector(hbpCoreBBox.Min, hbpExportBBox.Min);
                AssertVector(hbpCoreBBox.Max, hbpExportBBox.Max);
                Assert.That(hbpCoreMesh.vertexCount, Is.EqualTo(hbpExportMesh.vertexCount));
                Assert.That(hbpCoreMesh.triangles.Length, Is.EqualTo(hbpExportMesh.triangles.Length));
                AssertVector(hbpCoreMesh.vertices[6], hbpExportMesh.vertices[6]);
                AssertVector(hbpCoreMesh.normals[0], hbpExportMesh.normals[0]);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(hbpExportMesh);
                UnityEngine.Object.DestroyImmediate(hbpCoreMesh);
                NativeBackendOptions.Reset();
                if (File.Exists(objPath))
                {
                    File.Delete(objPath);
                }
            }
        }

        private static List<DllImportSignature> ReadCurrentDllImports()
        {
            string dllFolder = Path.Combine(TestPathUtility.ProjectRoot, "Assets", "Scripts", "HBP", "Core", "DLL");
            return Directory
                .GetFiles(dllFolder, "*.cs", SearchOption.AllDirectories)
                .SelectMany(ReadDllImportsFromFile)
                .OrderBy(imported => imported.RelativeFile, StringComparer.Ordinal)
                .ThenBy(imported => imported.Entry, StringComparer.Ordinal)
                .ToList();
        }

        private static IEnumerable<DllImportSignature> ReadDllImportsFromFile(string file)
        {
            string dllFolder = Path.Combine(TestPathUtility.ProjectRoot, "Assets", "Scripts", "HBP", "Core", "DLL");
            string relativeFile = file.Substring(dllFolder.Length).TrimStart('\\', '/').Replace('\\', '/');

            foreach (Match match in DllImportRegex.Matches(File.ReadAllText(file)))
            {
                string dll = match.Groups["dll"].Success
                    ? match.Groups["dll"].Value
                    : NativeDllName(match.Groups["nativeDll"].Value);

                yield return new DllImportSignature(
                    dll,
                    match.Groups["entry"].Value,
                    relativeFile);
            }
        }

        private static string NativeDllName(string nativeDllConstant)
        {
            return nativeDllConstant switch
            {
                nameof(NativeBackend.HbpExport) => NativeDll.HbpExport,
                nameof(NativeBackend.HbpCore) => NativeDll.HbpCore,
                _ => throw new InvalidOperationException($"Unknown NativeDll constant: {nativeDllConstant}")
            };
        }

        private static T ExecuteNativeOrIgnore<T>(Func<T> action, string context)
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

        private static string NativePath(params string[] parts)
        {
            string path = TestPathUtility.FixturePath("Native");
            foreach (string part in parts)
            {
                path = Path.Combine(path, part);
            }
            return path;
        }

        private static string CubeObjFixture()
        {
            return string.Join(
                Environment.NewLine,
                "v 0 0 0 1 0 0",
                "v 1 0 0 0 1 0",
                "v 1 1 0 0 0 1",
                "v 0 1 0 1 1 0",
                "v 0 0 1 1 0 1",
                "v 1 0 1 0 1 1",
                "v 1 1 1 1 1 1",
                "v 0 1 1 0.5 0.5 0.5",
                "vn 0 0 1",
                "vn 0 0 1",
                "vn 0 0 1",
                "vn 0 0 1",
                "vn 0 0 1",
                "vn 0 0 1",
                "vn 0 0 1",
                "vn 0 0 1",
                "vt 0 0",
                "vt 1 0",
                "vt 1 1",
                "vt 0 1",
                "vt 0 0",
                "vt 1 0",
                "vt 1 1",
                "vt 0 1",
                "f 1/1/1 2/2/2 3/3/3",
                "f 1/1/1 3/3/3 4/4/4",
                "f 5/5/5 7/7/7 6/6/6",
                "f 5/5/5 8/8/8 7/7/7",
                "f 1/1/1 5/5/5 6/6/6",
                "f 1/1/1 6/6/6 2/2/2",
                "f 2/2/2 6/6/6 7/7/7",
                "f 2/2/2 7/7/7 3/3/3",
                "f 3/3/3 7/7/7 8/8/8",
                "f 3/3/3 8/8/8 4/4/4",
                "f 4/4/4 8/8/8 5/5/5",
                "f 4/4/4 5/5/5 1/1/1",
                string.Empty);
        }

        private static void AssertVector(Vector3 actual, Vector3 expected)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(0.0001f));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(0.0001f));
            Assert.That(actual.z, Is.EqualTo(expected.z).Within(0.0001f));
        }

        private static void AssertSameVectorSet(IReadOnlyCollection<Vector3> actual, IReadOnlyCollection<Vector3> expected)
        {
            Assert.That(actual.Count, Is.EqualTo(expected.Count));

            List<Vector3> remaining = new(actual);
            foreach (Vector3 expectedPoint in expected)
            {
                int foundIndex = remaining.FindIndex(actualPoint => VectorsEqual(actualPoint, expectedPoint));
                Assert.That(foundIndex, Is.GreaterThanOrEqualTo(0), $"Missing point {expectedPoint}");
                remaining.RemoveAt(foundIndex);
            }
        }

        private static void DisposeSegments(IEnumerable<HbpSegment3> segments)
        {
            foreach (HbpSegment3 segment in segments)
            {
                segment.Dispose();
            }
        }

        private static bool VectorsEqual(Vector3 actual, Vector3 expected)
        {
            return Mathf.Abs(actual.x - expected.x) <= 0.0001f
                && Mathf.Abs(actual.y - expected.y) <= 0.0001f
                && Mathf.Abs(actual.z - expected.z) <= 0.0001f;
        }

        private readonly struct DllImportSignature
        {
            public DllImportSignature(string dll, string entry, string relativeFile)
            {
                Dll = dll;
                Entry = entry;
                RelativeFile = relativeFile;
            }

            public string Dll { get; }
            public string Entry { get; }
            public string RelativeFile { get; }
        }
    }
}
