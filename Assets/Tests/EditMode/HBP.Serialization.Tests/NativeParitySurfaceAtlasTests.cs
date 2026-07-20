using System;
using System.IO;
using System.Linq;
using HBP.Core.DLL;
using HBP.Tests.Serialization.Helpers;
using NUnit.Framework;
using UnityEngine;
using BBox = HBP.Tests.Serialization.LegacyNative.BBox;
using JuBrainAtlas = HBP.Tests.Serialization.LegacyNative.JuBrainAtlas;
using MarsAtlas = HBP.Tests.Serialization.LegacyNative.MarsAtlas;
using Surface = HBP.Tests.Serialization.LegacyNative.Surface;

namespace HBP.Tests.Serialization
{
    [LegacyParityOnly]
    public class NativeParitySurfaceAtlasTests
    {
        [Test]
        [Category("NativeMigration")]
        [Category("NativeParity")]
        [Category(NativeParityAssert.IndependentOracle)]
        public void ObjAndTriSurfaceBuffers_MatchAcrossBackends()
        {
            NativeParityAssert.RequireHbpCore();

            string tempDirectory = Path.Combine(Path.GetTempPath(), "hibop_native_parity_surface");
            Directory.CreateDirectory(tempDirectory);
            string objPath = Path.Combine(tempDirectory, "parity_surface.obj");
            string triPath = Path.Combine(tempDirectory, "parity_surface.tri");
            File.WriteAllText(objPath, CubeObjFixture());
            File.WriteAllText(triPath, TriFixture());

            try
            {
                AssertLoadedSurfaceMatches(objPath, surface => surface.LoadOBJFile(objPath), nativeFixtureMin: Vector3.zero, nativeFixtureMax: Vector3.one);
                AssertTriSurfaceMatchesFixtureOracle(triPath);
            }
            finally
            {
                if (File.Exists(objPath)) File.Delete(objPath);
                if (File.Exists(triPath)) File.Delete(triPath);
            }
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeParity")]
        [Category(NativeParityAssert.IntentionalCorrection)]
        public void ClosedSurfacePointContainment_MatchesAcrossBackends()
        {
            NativeParityAssert.RequireHbpCore();

            string tempDirectory = Path.Combine(Path.GetTempPath(), "hibop_native_parity_surface");
            Directory.CreateDirectory(tempDirectory);
            string objPath = Path.Combine(tempDirectory, "parity_closed_cube.obj");
            File.WriteAllText(objPath, CubeObjFixture());

            try
            {
                using Surface hbpExportSurface = LoadSurface(BenchmarkBackend.HbpExport, surface => surface.LoadOBJFile(objPath));
                using Surface hbpCoreSurface = LoadSurface(BenchmarkBackend.HbpCore, surface => surface.LoadOBJFile(objPath));

                foreach ((Vector3 point, bool expectedInside, string name) in new[]
                {
                    (new Vector3(-0.5f, 0.5f, 0.5f), true, "center"),
                    (new Vector3(-0.1f, 0.5f, 0.5f), true, "inside near face"),
                    (new Vector3(-1.5f, 0.5f, 0.5f), false, "outside x"),
                    (new Vector3(-0.5f, 1.5f, 0.5f), false, "outside y"),
                    (new Vector3(-0.5f, 0.5f, 1.5f), false, "outside z")
                })
                {
                    bool hbpCoreInside = hbpCoreSurface.IsPointInside(point);
                    bool hbpExportInside = hbpExportSurface.IsPointInside(point);
                    Assert.That(hbpCoreInside, Is.EqualTo(expectedInside), $"{name}: independent unit-cube oracle in Unity space");
                    TestContext.Progress.WriteLine($"point containment {name} at Unity {point}: oracle={expectedInside}, hbp_core={hbpCoreInside}, hbp_export={hbpExportInside}");
                }
            }
            finally
            {
                if (File.Exists(objPath)) File.Delete(objPath);
            }
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeParity")]
        [Category(NativeParityAssert.NormalizedCoordinateParity)]
        public void GiftiSurfaceBuffersAndTransform_MatchAcrossBackends()
        {
            NativeParityAssert.RequireHbpCore();

            string singleSurfacePath = NativeParityAssert.NativePath("Meshes", "single_surface.gii");
            string transformedSurfacePath = NativeParityAssert.NativePath("Meshes", "MNI_Lwhite.gii");
            string transformPath = NativeParityAssert.NativePath("Meshes", "MNI.trm");

            AssertLoadedSurfaceMatches(singleSurfacePath, surface => surface.LoadGIIFile(singleSurfacePath));
            AssertLoadedSurfaceMatches(transformedSurfacePath, surface => surface.LoadGIIFile(transformedSurfacePath, transformPath), 0.0005f);
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeParity")]
        [Category(NativeParityAssert.NormalizedCoordinateParity)]
        public void MarsAtlasMetadataColorsAndSurfaceLabels_MatchAcrossBackends()
        {
            NativeParityAssert.RequireHbpCore();

            using MarsAtlas hbpExportAtlas = LoadMarsAtlas(BenchmarkBackend.HbpExport);
            using MarsAtlas hbpCoreAtlas = LoadMarsAtlas(BenchmarkBackend.HbpCore);

            Assert.That(hbpCoreAtlas.Label("L_VCcm"), Is.EqualTo(hbpExportAtlas.Label("L_VCcm")));
            Assert.That(hbpCoreAtlas.Labels(), Is.EqualTo(hbpExportAtlas.Labels()));
            Assert.That(hbpCoreAtlas.AreaNames, Is.EqualTo(hbpExportAtlas.AreaNames));

            foreach (int label in new[] { 1, 2, hbpExportAtlas.Label("L_VCcm"), hbpExportAtlas.Label("R_VCcm") }.Distinct())
            {
                Assert.That(hbpCoreAtlas.Hemisphere(label), Is.EqualTo(hbpExportAtlas.Hemisphere(label)), label.ToString());
                Assert.That(hbpCoreAtlas.Lobe(label), Is.EqualTo(hbpExportAtlas.Lobe(label)), label.ToString());
                Assert.That(hbpCoreAtlas.NameFS(label), Is.EqualTo(hbpExportAtlas.NameFS(label)), label.ToString());
                Assert.That(hbpCoreAtlas.Name(label), Is.EqualTo(hbpExportAtlas.Name(label)), label.ToString());
                Assert.That(hbpCoreAtlas.FullName(label), Is.EqualTo(hbpExportAtlas.FullName(label)), label.ToString());
                Assert.That(hbpCoreAtlas.BrodmannArea(label), Is.EqualTo(hbpExportAtlas.BrodmannArea(label)), label.ToString());
                Assert.That(hbpCoreAtlas.GetInformation(label), Is.EqualTo(hbpExportAtlas.GetInformation(label)), label.ToString());
                Assert.That(hbpCoreAtlas.GetAreaName(label), Is.EqualTo(hbpExportAtlas.GetAreaName(label)), label.ToString());
            }

            int[] colorLabels = { 1, 2, -1, 999 };
            Color[] hbpCoreColors = hbpCoreAtlas.ConvertIndicesToColors(colorLabels, selectedArea: 1);
            Color[] hbpExportColors = hbpExportAtlas.ConvertIndicesToColors(colorLabels, selectedArea: 1);
            NativeParityAssert.AssertSameColorArray(hbpCoreColors, hbpExportColors);

            Vector3[] hbpExportCoordinates = hbpExportAtlas.GetAreaCoordinates(1);
            Vector3[] hbpCoreCoordinates = hbpCoreAtlas.GetAreaCoordinates(1);
            Assert.That(hbpCoreCoordinates, Has.Length.EqualTo(hbpExportCoordinates.Length));
            NativeParityAssert.AssertSameVectorSet(hbpCoreCoordinates.Take(32), hbpExportCoordinates.Take(32));
            Assert.That(hbpCoreAtlas.GetClosestAreaIndex(hbpExportCoordinates[0], 0), Is.EqualTo(hbpExportAtlas.GetClosestAreaIndex(hbpExportCoordinates[0], 0)));

            string surfacePath = NativeParityAssert.NativePath("Meshes", "single_surface.gii");
            using Surface hbpExportSurface = LoadSurface(BenchmarkBackend.HbpExport, surface => surface.LoadGIIFile(surfacePath));
            using Surface hbpCoreSurface = LoadSurface(BenchmarkBackend.HbpCore, surface => surface.LoadGIIFile(surfacePath));
            Assert.That(hbpCoreAtlas.GetSurfaceAreaLabels(hbpCoreSurface), Is.EqualTo(hbpExportAtlas.GetSurfaceAreaLabels(hbpExportSurface)));

            string tempDirectory = Path.Combine(Path.GetTempPath(), "hibop_native_parity_surface");
            Directory.CreateDirectory(tempDirectory);
            string parcelsPath = Path.Combine(tempDirectory, "parity_mars_parcels.gii");
            File.WriteAllText(parcelsPath, MarsParcelFixture(new[] { 1, 2, 1, 2 }));
            Mesh hbpExportMesh = new();
            Mesh hbpCoreMesh = new();
            try
            {
                hbpExportMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                hbpCoreMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                Assert.That(hbpExportSurface.SearchMarsParcelFileAndUpdateColors(hbpExportAtlas, parcelsPath), Is.True);
                Assert.That(hbpCoreSurface.SearchMarsParcelFileAndUpdateColors(hbpCoreAtlas, parcelsPath), Is.True);
                hbpExportSurface.UpdateMeshFromDLL(hbpExportMesh);
                hbpCoreSurface.UpdateMeshFromDLL(hbpCoreMesh);
                NativeParityAssert.AssertSameColorArray(hbpCoreMesh.colors, hbpExportMesh.colors);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(hbpExportMesh);
                UnityEngine.Object.DestroyImmediate(hbpCoreMesh);
                if (File.Exists(parcelsPath)) File.Delete(parcelsPath);
            }
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeParity")]
        [Category(NativeParityAssert.NormalizedCoordinateParity)]
        public void JuBrainAtlasMetadataColorsAndSpatialQueries_MatchAcrossBackends()
        {
            NativeParityAssert.RequireHbpCore();

            using JuBrainAtlas hbpExportAtlas = LoadJuBrainAtlas(BenchmarkBackend.HbpExport);
            using JuBrainAtlas hbpCoreAtlas = LoadJuBrainAtlas(BenchmarkBackend.HbpCore);

            Assert.That(hbpCoreAtlas.Loaded, Is.EqualTo(hbpExportAtlas.Loaded));
            Assert.That(hbpCoreAtlas.AreaNames, Is.EqualTo(hbpExportAtlas.AreaNames));
            Assert.That(hbpCoreAtlas.GetAreaName(1), Is.EqualTo(hbpExportAtlas.GetAreaName(1)));
            Assert.That(hbpCoreAtlas.GetInformation(1), Is.EqualTo(hbpExportAtlas.GetInformation(1)));

            int[] colorLabels = { 1, 3, 0, 999 };
            Color[] hbpCoreColors = hbpCoreAtlas.ConvertIndicesToColors(colorLabels, selectedArea: 1);
            Color[] hbpExportColors = hbpExportAtlas.ConvertIndicesToColors(colorLabels, selectedArea: 1);
            NativeParityAssert.AssertSameColorArray(hbpCoreColors, hbpExportColors);

            Vector3[] hbpExportCoordinates = hbpExportAtlas.GetAreaCoordinates(1);
            Vector3[] hbpCoreCoordinates = hbpCoreAtlas.GetAreaCoordinates(1);
            Assert.That(hbpCoreCoordinates, Has.Length.EqualTo(hbpExportCoordinates.Length));
            NativeParityAssert.AssertSameVectorSet(hbpCoreCoordinates.Take(32), hbpExportCoordinates.Take(32));
            Assert.That(hbpCoreAtlas.GetClosestAreaIndex(hbpExportCoordinates[0], 0), Is.EqualTo(hbpExportAtlas.GetClosestAreaIndex(hbpExportCoordinates[0], 0)));

            hbpCoreAtlas.Load();
            Assert.That(hbpCoreAtlas.Loaded, Is.True);
            Assert.That(hbpCoreAtlas.GetAreaName(1), Is.EqualTo(hbpExportAtlas.GetAreaName(1)));
            Assert.That(hbpCoreAtlas.GetInformation(1), Is.EqualTo(hbpExportAtlas.GetInformation(1)));
        }

        private static void AssertLoadedSurfaceMatches(
            string path,
            Func<Surface, bool> load,
            float tolerance = NativeParityAssert.DefaultTolerance,
            Vector3? nativeFixtureMin = null,
            Vector3? nativeFixtureMax = null)
        {
            bool isGifti = Path.GetExtension(path).Equals(".gii", StringComparison.OrdinalIgnoreCase);
            bool isObj = Path.GetExtension(path).Equals(".obj", StringComparison.OrdinalIgnoreCase);
            bool compareColors = !isGifti && !isObj;
            bool compareTriangles = !isGifti && !isObj;
            using Surface hbpExportSurface = LoadSurface(BenchmarkBackend.HbpExport, load);
            using Surface hbpCoreSurface = LoadSurface(BenchmarkBackend.HbpCore, load);

            Assert.That(hbpCoreSurface.NumberOfVertices, Is.EqualTo(hbpExportSurface.NumberOfVertices), path);
            Assert.That(hbpCoreSurface.NumberOfTriangles, Is.EqualTo(hbpExportSurface.NumberOfTriangles), path);
            Assert.That(hbpCoreSurface.NumberOfVisibleTriangles, Is.EqualTo(hbpExportSurface.NumberOfVisibleTriangles), path);
            Assert.That(hbpCoreSurface.VisibilityMask, Is.EqualTo(hbpExportSurface.VisibilityMask), path);

            using BBox hbpExportBBox = hbpExportSurface.BoundingBox;
            using BBox hbpCoreBBox = hbpCoreSurface.BoundingBox;
            NativeParityAssert.AssertUnityBoundsMatchLegacyNative(
                hbpCoreBBox.Min,
                hbpCoreBBox.Max,
                hbpExportBBox.Min,
                hbpExportBBox.Max,
                tolerance,
                path);
            if (nativeFixtureMin.HasValue && nativeFixtureMax.HasValue)
            {
                NativeParityAssert.NativeBoundsToUnity(nativeFixtureMin.Value, nativeFixtureMax.Value, out Vector3 expectedMin, out Vector3 expectedMax);
                NativeParityAssert.AssertVector(hbpCoreBBox.Min, expectedMin, tolerance, $"{path} fixture oracle min");
                NativeParityAssert.AssertVector(hbpCoreBBox.Max, expectedMax, tolerance, $"{path} fixture oracle max");
            }

            Mesh hbpExportMesh = new();
            Mesh hbpCoreMesh = new();
            try
            {
                hbpExportMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                hbpCoreMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                hbpExportSurface.UpdateMeshFromDLL(hbpExportMesh);
                hbpCoreSurface.UpdateMeshFromDLL(hbpCoreMesh);
                NativeParityAssert.NormalizeLegacyMeshToUnity(hbpExportMesh);
                AssertMesh(hbpCoreMesh, hbpExportMesh, path, tolerance, compareColors, compareTriangles);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(hbpExportMesh);
                UnityEngine.Object.DestroyImmediate(hbpCoreMesh);
            }
        }

        private static Surface LoadSurface(BenchmarkBackend backend, Func<Surface, bool> load)
        {
            return NativeParityAssert.WithBackend(
                backend,
                () =>
                {
                    Surface surface = new();
                    try
                    {
                        Assert.That(load(surface), Is.True);
                        return surface;
                    }
                    catch
                    {
                        surface.Dispose();
                        throw;
                    }
                });
        }

        private static void AssertTriSurfaceMatchesFixtureOracle(string triPath)
        {
            using Surface surface = LoadSurface(BenchmarkBackend.HbpCore, value => value.LoadTRIFile(triPath));
            Assert.That(surface.NumberOfVertices, Is.EqualTo(4));
            Assert.That(surface.NumberOfTriangles, Is.EqualTo(2));

            using BBox bbox = surface.BoundingBox;
            NativeParityAssert.AssertVector(bbox.Min, new Vector3(-1.0f, 0.0f, 0.0f), context: "TRI fixture bbox min in Unity");
            NativeParityAssert.AssertVector(bbox.Max, new Vector3(0.0f, 1.0f, 0.0f), context: "TRI fixture bbox max in Unity");

            Mesh mesh = new();
            try
            {
                surface.UpdateMeshFromDLL(mesh);
                NativeParityAssert.AssertSameVectorArray(
                    mesh.vertices,
                    new[]
                    {
                        new Vector3(0.0f, 0.0f, 0.0f),
                        new Vector3(-1.0f, 0.0f, 0.0f),
                        new Vector3(-1.0f, 1.0f, 0.0f),
                        new Vector3(0.0f, 1.0f, 0.0f)
                    });
                NativeParityAssert.AssertSameVectorArray(mesh.normals, Enumerable.Repeat(Vector3.forward, 4).ToArray());
                Assert.That(mesh.triangles, Is.EqualTo(new[] { 0, 2, 1, 0, 3, 2 }), "TRI fixture winding after right-handed to left-handed conversion");
                TestContext.Progress.WriteLine("TRI: hbp_core validated against fixture oracle; hbp_export comparison unavailable because the installed DLL has no load_TRI_file_Surface entry point.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(mesh);
            }
        }

        private static MarsAtlas LoadMarsAtlas(BenchmarkBackend backend)
        {
            return NativeParityAssert.WithBackend(
                backend,
                () =>
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
                });
        }

        private static JuBrainAtlas LoadJuBrainAtlas(BenchmarkBackend backend)
        {
            return NativeParityAssert.WithBackend(
                backend,
                () =>
                {
                    JuBrainAtlas atlas = new();
                    try
                    {
                        atlas.Load();
                        Assert.That(atlas.Loaded, Is.True);
                        return atlas;
                    }
                    catch
                    {
                        atlas.Dispose();
                        throw;
                    }
                });
        }

        private static string AtlasPath(string fileName)
        {
            return Path.Combine(TestPathUtility.ProjectRoot, "Assets", "Data", "Atlases", "MarsAtlas", fileName);
        }

        private static string CubeObjFixture()
        {
            return string.Join(
                Environment.NewLine,
                "v 0 0 0",
                "v 1 0 0",
                "v 1 1 0",
                "v 0 1 0",
                "v 0 0 1",
                "v 1 0 1",
                "v 1 1 1",
                "v 0 1 1",
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

        private static void AssertMesh(Mesh actual, Mesh expected, string context, float tolerance, bool compareColors, bool compareTriangles)
        {
            Assert.That(actual.vertexCount, Is.EqualTo(expected.vertexCount), context);
            NativeParityAssert.AssertSameVectorArray(actual.vertices, expected.vertices, tolerance);
            NativeParityAssert.AssertSameVectorArray(actual.normals, expected.normals, tolerance);
            NativeParityAssert.AssertSameVectorArray(actual.uv, expected.uv, tolerance);
            if (compareColors)
            {
                AssertColorArray(actual.colors, expected.colors, context, tolerance);
            }
            if (compareTriangles)
            {
                Assert.That(actual.triangles, Is.EqualTo(expected.triangles), context);
            }
        }

        private static void AssertColorArray(Color[] actual, Color[] expected, string context, float tolerance)
        {
            Assert.That(actual.Length, Is.EqualTo(expected.Length), context);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.That(actual[i].r, Is.EqualTo(expected[i].r).Within(tolerance), $"{context} color[{i}].r");
                Assert.That(actual[i].g, Is.EqualTo(expected[i].g).Within(tolerance), $"{context} color[{i}].g");
                Assert.That(actual[i].b, Is.EqualTo(expected[i].b).Within(tolerance), $"{context} color[{i}].b");
                Assert.That(actual[i].a, Is.EqualTo(expected[i].a).Within(tolerance), $"{context} color[{i}].a");
            }
        }

        private static string MarsParcelFixture(int[] labels)
        {
            return string.Join(
                Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>",
                "<GIFTI Version=\"1.0\" NumberOfDataArrays=\"1\">",
                "<MetaData />",
                "<LabelTable />",
                "<DataArray Intent=\"NIFTI_INTENT_NONE\" DataType=\"NIFTI_TYPE_INT32\" ArrayIndexingOrder=\"RowMajorOrder\" Dimensionality=\"1\" Encoding=\"ASCII\" Endian=\"LittleEndian\" ExternalFileName=\"\" ExternalFileOffset=\"0\" Dim0=\"" + labels.Length + "\">",
                "<MetaData />",
                "<Data>" + string.Join(" ", labels) + "</Data>",
                "</DataArray>",
                "</GIFTI>",
                string.Empty);
        }

        private static string TriFixture()
        {
            return string.Join(
                Environment.NewLine,
                "4 2",
                "0 0 0 0 0 1 1 0 0",
                "1 0 0 0 0 1 0 1 0",
                "1 1 0 0 0 1 0 0 1",
                "0 1 0 0 0 1 1 1 1",
                "- 2 0 0",
                "0 1 2",
                "0 2 3",
                string.Empty);
        }
    }
}
