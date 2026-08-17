using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HBP.Core.DLL;
using HBP.Tests.Serialization.Helpers;
using NUnit.Framework;
using UnityEngine;
using BBox = HBP.Tests.Serialization.LegacyNative.BBox;
using CutGeometryGenerator = HBP.Tests.Serialization.LegacyNative.CutGeometryGenerator;
using Surface = HBP.Tests.Serialization.LegacyNative.Surface;
using Volume = HBP.Tests.Serialization.LegacyNative.Volume;

namespace HBP.Tests.Serialization
{
    [LegacyParityOnly]
    public class NativeParitySurfaceCutTests
    {
        [Test]
        [Category("NativeMigration")]
        [Category("NativeParity")]
        [Category(NativeParityAssert.IndependentOracle)]
        public void SinglePlaneCutOutputs_MatchAcrossBackends()
        {
            NativeParityAssert.RequireHbpCore();

            using TempSurfaceFixture fixture = new();
            Surface[] hbpExportCuts = CutCube(BenchmarkBackend.HbpExport, fixture.ObjPath, strongCuts: true, CreateHalfXCut);
            Surface[] hbpCoreCuts = CutCube(BenchmarkBackend.HbpCore, fixture.ObjPath, strongCuts: true, CreateHalfXCut);
            try
            {
                AssertSurfaceCollectionsMatch(hbpCoreCuts, hbpExportCuts, compareExactCounts: true);
            }
            finally
            {
                DisposeSurfaces(hbpExportCuts);
                DisposeSurfaces(hbpCoreCuts);
            }
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeParity")]
        [Category(NativeParityAssert.IndependentOracle)]
        public void GeneratedCutCapsAndRawCuts_MatchAcrossBackends()
        {
            NativeParityAssert.RequireHbpCore();

            using TempSurfaceFixture fixture = new();
            List<Surface> hbpExportCaps = GenerateCuts(BenchmarkBackend.HbpExport, fixture.ObjPath, raw: false);
            List<Surface> hbpCoreCaps = GenerateCuts(BenchmarkBackend.HbpCore, fixture.ObjPath, raw: false);
            List<Surface> hbpExportRaw = GenerateCuts(BenchmarkBackend.HbpExport, fixture.ObjPath, raw: true);
            List<Surface> hbpCoreRaw = GenerateCuts(BenchmarkBackend.HbpCore, fixture.ObjPath, raw: true);
            try
            {
                AssertSurfaceCollectionsMatch(hbpCoreCaps, hbpExportCaps, compareExactCounts: false);
                AssertSurfaceCollectionsMatch(hbpCoreRaw, hbpExportRaw, compareExactCounts: false);
            }
            finally
            {
                DisposeSurfaces(hbpExportCaps);
                DisposeSurfaces(hbpCoreCaps);
                DisposeSurfaces(hbpExportRaw);
                DisposeSurfaces(hbpCoreRaw);
            }
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeParity")]
        [Category(NativeParityAssert.IndependentOracle)]
        public void MultiPlaneStrongCutsModes_MatchAcrossBackends()
        {
            NativeParityAssert.RequireHbpCore();

            using TempSurfaceFixture fixture = new();
            foreach (bool strongCuts in new[] { true, false })
            {
                Surface[] hbpExportCuts = CutCube(BenchmarkBackend.HbpExport, fixture.ObjPath, strongCuts, CreateHalfXCut, CreateHalfYCut);
                Surface[] hbpCoreCuts = CutCube(BenchmarkBackend.HbpCore, fixture.ObjPath, strongCuts, CreateHalfXCut, CreateHalfYCut);
                try
                {
                    AssertSurfaceCollectionsMatch(hbpCoreCuts, hbpExportCuts, compareExactCounts: false);
                }
                finally
                {
                    DisposeSurfaces(hbpExportCuts);
                    DisposeSurfaces(hbpCoreCuts);
                }
            }
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeParity")]
        public void MniGreyMatterKnownAxialVertexCase_MatchesLegacyArea()
        {
            NativeParityAssert.RequireHbpCore();

            List<Surface> hbpExportCuts = GenerateKnownMniAxialCut(BenchmarkBackend.HbpExport);
            List<Surface> hbpCoreCuts = GenerateKnownMniAxialCut(BenchmarkBackend.HbpCore);
            try
            {
                double expectedArea = MeasureSurfaceArea(hbpExportCuts.Single());
                double actualArea = MeasureSurfaceArea(hbpCoreCuts.Single());
                Assert.That(actualArea, Is.EqualTo(expectedArea).Within(expectedArea * 0.001), "known MNI axial cap area");
            }
            finally
            {
                DisposeSurfaces(hbpExportCuts);
                DisposeSurfaces(hbpCoreCuts);
            }
        }

        private static Surface[] CutCube(BenchmarkBackend backend, string objPath, bool strongCuts, params Func<HBP.Core.Object3D.Cut>[] cutFactories)
        {
            return NativeParityAssert.WithBackend(backend, () =>
            {
                using Surface surface = LoadSurface(objPath);
                HBP.Core.Object3D.Cut[] cuts = new HBP.Core.Object3D.Cut[cutFactories.Length];
                try
                {
                    for (int i = 0; i < cutFactories.Length; ++i)
                    {
                        cuts[i] = cutFactories[i]();
                    }

                    return surface.Cut(cuts, noHoles: false, strongCuts: strongCuts);
                }
                finally
                {
                    foreach (HBP.Core.Object3D.Cut cut in cuts)
                    {
                        cut?.Dispose();
                    }
                }
            });
        }

        private static List<Surface> GenerateCuts(BenchmarkBackend backend, string objPath, bool raw)
        {
            return NativeParityAssert.WithBackend(backend, () =>
            {
                using Surface surface = LoadSurface(objPath);
                using HBP.Core.Object3D.Cut cut = CreateHalfXCut();
                List<HBP.Core.Object3D.Cut> cuts = new() { cut };
                return raw ? surface.GenerateRawCutSurfaces(cuts, noHoles: false, strongCuts: true) : surface.GenerateCutSurfaces(cuts, noHoles: false, strongCuts: true);
            });
        }

        private static List<Surface> GenerateKnownMniAxialCut(BenchmarkBackend backend)
        {
            return NativeParityAssert.WithBackend(backend, () =>
            {
                string meshDirectory = Path.Combine(TestPathUtility.ProjectRoot, "Assets", "Data", "Meshes");
                using Surface left = new();
                using Surface right = new();
                using Volume volume = new();
                Assert.That(left.LoadGIIFile(Path.Combine(meshDirectory, "MNI_Lhemi.gii"), Path.Combine(meshDirectory, "MNI.trm")), Is.True);
                Assert.That(right.LoadGIIFile(Path.Combine(meshDirectory, "MNI_Rhemi.gii"), Path.Combine(meshDirectory, "MNI.trm")), Is.True);
                Assert.That(volume.LoadNIFTIFile(Path.Combine(TestPathUtility.ProjectRoot, "Assets", "Data", "IRM", "MNI.nii")), Is.True);
                left.FlipTriangles();
                right.FlipTriangles();
                left.Append(right);

                using HBP.Core.Object3D.Cut cut = new(new Vector3(0.47999573f, -15.199997f, -14.565386f), Vector3.forward);
                List<Surface> cuts = left.GenerateCutSurfaces(new List<HBP.Core.Object3D.Cut> { cut }, noHoles: false, strongCuts: true);
                using CutGeometryGenerator geometry = new();
                geometry.Initialize(volume, cut, -1);
                geometry.UpdateSurfaceUV(cuts.Single());
                return cuts;
            });
        }

        private static double MeasureSurfaceArea(Surface surface)
        {
            Mesh mesh = new() { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            try
            {
                surface.UpdateMeshFromDLL(mesh);
                Vector3[] vertices = mesh.vertices;
                int[] triangles = mesh.triangles;
                double area = 0;
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    Vector3 a = vertices[triangles[i]];
                    Vector3 b = vertices[triangles[i + 1]];
                    Vector3 c = vertices[triangles[i + 2]];
                    area += Vector3.Cross(b - a, c - a).magnitude * 0.5;
                }

                return area;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(mesh);
            }
        }

        private static Surface LoadSurface(string objPath)
        {
            Surface surface = new();
            try
            {
                Assert.That(surface.LoadOBJFile(objPath), Is.True);
                return surface;
            }
            catch
            {
                surface.Dispose();
                throw;
            }
        }

        private static HBP.Core.Object3D.Cut CreateHalfXCut()
        {
            // The OBJ fixture is native/right-handed. Construct its plane from the
            // converted Unity bounds instead of reusing native X=+0.5 as Unity X.
            NativeParityAssert.NativeBoundsToUnity(Vector3.zero, Vector3.one, out Vector3 unityMin, out Vector3 unityMax);
            Vector3 unityCenter = (unityMin + unityMax) * 0.5f;
            return new HBP.Core.Object3D.Cut(unityCenter, NativeParityAssert.NativeToUnity(Vector3.right));
        }

        private static HBP.Core.Object3D.Cut CreateHalfYCut()
        {
            return new HBP.Core.Object3D.Cut(new Vector3(0.0f, 0.5f, 0.0f), Vector3.up);
        }

        private static void AssertSurfaceCollectionsMatch(IReadOnlyList<Surface> actual, IReadOnlyList<Surface> expected, bool compareExactCounts)
        {
            Assert.That(actual.Count, Is.EqualTo(expected.Count));
            for (int i = 0; i < expected.Count; ++i)
            {
                Assert.That(actual[i].NumberOfVertices, Is.GreaterThan(0), $"actual vertices {i}");
                Assert.That(actual[i].NumberOfTriangles, Is.GreaterThan(0), $"actual triangles {i}");
                if (compareExactCounts)
                {
                    Assert.That(actual[i].NumberOfVertices, Is.EqualTo(expected[i].NumberOfVertices), $"vertices {i}");
                    Assert.That(actual[i].NumberOfTriangles, Is.EqualTo(expected[i].NumberOfTriangles), $"triangles {i}");
                }

                using BBox actualBBox = actual[i].BoundingBox;
                using BBox expectedBBox = expected[i].BoundingBox;
                NativeParityAssert.AssertUnityBoundsMatchLegacyNative(actualBBox.Min, actualBBox.Max, expectedBBox.Min, expectedBBox.Max, 0.0005f, $"cut surface {i}");
            }
        }

        private static void DisposeSurfaces(IEnumerable<Surface> surfaces)
        {
            foreach (Surface surface in surfaces)
            {
                surface?.Dispose();
            }
        }

        private sealed class TempSurfaceFixture : IDisposable
        {
            public TempSurfaceFixture()
            {
                DirectoryPath = Path.Combine(Path.GetTempPath(), "hibop_native_parity_cut");
                Directory.CreateDirectory(DirectoryPath);
                ObjPath = Path.Combine(DirectoryPath, $"cube_{Guid.NewGuid():N}.obj");
                File.WriteAllText(ObjPath, CubeObjFixture());
            }

            public string DirectoryPath { get; }
            public string ObjPath { get; }

            public void Dispose()
            {
                if (File.Exists(ObjPath))
                {
                    File.Delete(ObjPath);
                }
            }
        }

        private static string CubeObjFixture()
        {
            return string.Join(Environment.NewLine, "v 0 0 0", "v 1 0 0", "v 1 1 0", "v 0 1 0", "v 0 0 1", "v 1 0 1", "v 1 1 1", "v 0 1 1", "vn 0 0 1", "vn 0 0 1", "vn 0 0 1", "vn 0 0 1", "vn 0 0 1", "vn 0 0 1", "vn 0 0 1", "vn 0 0 1", "vt 0 0", "vt 1 0", "vt 1 1", "vt 0 1", "vt 0 0", "vt 1 0", "vt 1 1", "vt 0 1", "f 1/1/1 2/2/2 3/3/3", "f 1/1/1 3/3/3 4/4/4", "f 5/5/5 7/7/7 6/6/6", "f 5/5/5 8/8/8 7/7/7", "f 1/1/1 5/5/5 6/6/6", "f 1/1/1 6/6/6 2/2/2", "f 2/2/2 6/6/6 7/7/7", "f 2/2/2 7/7/7 3/3/3", "f 3/3/3 7/7/7 8/8/8", "f 3/3/3 8/8/8 4/4/4", "f 4/4/4 8/8/8 5/5/5", "f 4/4/4 5/5/5 1/1/1", string.Empty);
        }
    }
}
