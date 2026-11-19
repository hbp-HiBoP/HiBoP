using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.DLL;
using HBP.Core.DLL.HbpCore;
using HBP.Core.Enums;
using HBP.Core.Object3D;
using HBP.Data.Module3D;
using HBP.Tests.Serialization.Helpers;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HBP.Tests.Serialization
{
    public class SurfaceInflationPhase4Tests
    {
        [Test]
        [Category("NativeDll")]
        public async Task SingleMesh_CachesSelectsClearsAndRegeneratesDerivedRepresentation()
        {
            RequireInflationLibrary();
            TestSingleMesh3D mesh = new(CreateOctahedron());
            Mesh3DInflatedRepresentation first = null;
            Mesh3DInflatedRepresentation second = null;
            Mesh3DInflatedRepresentation geometryChanged = null;
            Mesh3DInflatedRepresentation regenerated = null;
            try
            {
                Surface anatomical = mesh.Both;
                Mesh3DInflationSettings settings = FastSettings(iterationCount: 4);

                first = await mesh.GenerateInflatedRepresentationAsync(settings);
                Mesh3DInflatedRepresentation cached = await mesh.GenerateInflatedRepresentationAsync(settings);

                Assert.That(cached, Is.SameAs(first));
                Assert.That(mesh.InflatedRepresentationCacheCount, Is.EqualTo(1));
                Assert.That(first.CacheKey.AlgorithmVersion, Is.EqualTo(Mesh3DInflationSettings.AlgorithmVersion));
                Assert.That(first.CacheKey.Preset, Is.EqualTo(SurfaceInflationPreset.Custom));
                Assert.That(first.CacheKey.IterationCount, Is.EqualTo(4));
                Assert.That(first.CacheKey.FixBoundaryVertices, Is.True);
                Assert.That(mesh.Representation, Is.EqualTo(SurfaceRepresentation.Anatomical));
                Assert.That(mesh.GetSurface(), Is.SameAs(anatomical));

                mesh.SelectRepresentation(SurfaceRepresentation.Inflated);
                Assert.That(mesh.GetSurface(), Is.SameAs(first.Both));
                Assert.That(mesh.GetSurface(simplified: true), Is.SameAs(first.SimplifiedBoth));

                second = await mesh.GenerateInflatedRepresentationAsync(FastSettings(iterationCount: 5));
                SurfaceInflationResult anatomicalResult = await anatomical.InflateAsync(FastSettings(5).Options);
                using (anatomicalResult.Surface) AssertSurfaceBuffersEqual(second.Both, anatomicalResult.Surface);
                Assert.That(second, Is.Not.SameAs(first));
                Assert.That(second.CacheKey, Is.Not.EqualTo(first.CacheKey));
                Assert.That(mesh.InflatedRepresentationCacheCount, Is.EqualTo(2));
                Assert.That(mesh.GetSurface(), Is.SameAs(second.Both));

                anatomical.FlipTriangles();
                anatomical.FlipTriangles();
                Assert.That(mesh.HasInflatedRepresentation, Is.False);
                Assert.That(mesh.Representation, Is.EqualTo(SurfaceRepresentation.Anatomical));
                Assert.Throws<InvalidOperationException>(() => mesh.SelectRepresentation(SurfaceRepresentation.Inflated));
                Assert.Throws<InvalidOperationException>(() => mesh.GetSurface(SurfaceRepresentation.Inflated));
                geometryChanged = await mesh.GenerateInflatedRepresentationAsync(FastSettings(iterationCount: 5));
                Assert.That(geometryChanged, Is.Not.SameAs(second));
                Assert.That(geometryChanged.CacheKey.SourceGeometryIdentity, Is.Not.EqualTo(second.CacheKey.SourceGeometryIdentity));
                Assert.That(mesh.InflatedRepresentationCacheCount, Is.EqualTo(1));

                Surface firstSurface = first.Both;
                Surface secondSurface = second.Both;
                Surface geometryChangedSurface = geometryChanged.Both;
                mesh.ClearInflatedRepresentations();

                Assert.That(mesh.Representation, Is.EqualTo(SurfaceRepresentation.Anatomical));
                Assert.That(mesh.HasInflatedRepresentation, Is.False);
                Assert.That(mesh.InflatedRepresentationCacheCount, Is.Zero);
                Assert.That(firstSurface.getHandle().Handle, Is.EqualTo(IntPtr.Zero));
                Assert.That(secondSurface.getHandle().Handle, Is.EqualTo(IntPtr.Zero));
                Assert.That(geometryChangedSurface.getHandle().Handle, Is.EqualTo(IntPtr.Zero));
                Assert.That(anatomical.getHandle().Handle, Is.Not.EqualTo(IntPtr.Zero));
                Assert.Throws<InvalidOperationException>(() => mesh.SelectRepresentation(SurfaceRepresentation.Inflated));

                regenerated = await mesh.GenerateInflatedRepresentationAsync(settings);
                Assert.That(regenerated, Is.Not.SameAs(first));
                Assert.That(regenerated.Both.getHandle().Handle, Is.Not.EqualTo(IntPtr.Zero));
            }
            finally
            {
                mesh.Clean();
            }
        }

        [Test]
        [Category("NativeDll")]
        public async Task LeftRightMesh_PublishesBothHemispheresAndMergedSurfaceTogether()
        {
            RequireInflationLibrary();
            Surface left = CreateOctahedron(new Vector3(-2f, 0f, 0f));
            Surface right = CreateOctahedron(new Vector3(2f, 0f, 0f));
            TestLeftRightMesh3D mesh = new(left, right);
            try
            {
                Mesh3DInflatedRepresentation representation = await mesh.GenerateInflatedRepresentationAsync(FastSettings(4));

                Assert.That(representation.Left, Is.Not.Null);
                Assert.That(representation.Right, Is.Not.Null);
                Assert.That(representation.Both, Is.Not.Null);
                Assert.That(representation.SimplifiedLeft, Is.Not.Null);
                Assert.That(representation.SimplifiedRight, Is.Not.Null);
                Assert.That(representation.SimplifiedBoth, Is.Not.Null);
                Assert.That(representation.Left.NumberOfVertices, Is.EqualTo(left.NumberOfVertices));
                Assert.That(representation.Right.NumberOfVertices, Is.EqualTo(right.NumberOfVertices));
                Assert.That(representation.Both.NumberOfVertices, Is.EqualTo(left.NumberOfVertices + right.NumberOfVertices));
                Assert.That(representation.Both.NumberOfTriangles, Is.EqualTo(left.NumberOfTriangles + right.NumberOfTriangles));
                Assert.That(representation.LeftReport.HasValue, Is.True);
                Assert.That(representation.RightReport.HasValue, Is.True);
                Assert.That(representation.BothReport.HasValue, Is.False);

                mesh.SelectRepresentation(SurfaceRepresentation.Inflated);
                Assert.That(mesh.GetSurface(MeshPart.Left), Is.SameAs(representation.Left));
                Assert.That(mesh.GetSurface(MeshPart.Right), Is.SameAs(representation.Right));
                Assert.That(mesh.GetSurface(MeshPart.Both), Is.SameAs(representation.Both));
                Assert.That(mesh.GetSurface(MeshPart.Left, simplified: true), Is.SameAs(representation.SimplifiedLeft));
            }
            finally
            {
                mesh.Clean();
            }
        }

        [Test]
        [Category("NativeDll")]
        public async Task LeftRightMesh_DoesNotPublishPartialRepresentationWhenRightInflationFails()
        {
            RequireInflationLibrary();
            TestLeftRightMesh3D mesh = new(CreateOctahedron(), CreateDegenerateSurface());
            try
            {
                SurfaceInflationException exception = null;
                try
                {
                    await mesh.GenerateInflatedRepresentationAsync(FastSettings(4));
                }
                catch (SurfaceInflationException caught)
                {
                    exception = caught;
                }

                Assert.That(exception, Is.Not.Null);
                Assert.That(mesh.HasInflatedRepresentation, Is.False);
                Assert.That(mesh.InflatedRepresentationCacheCount, Is.Zero);
                Assert.That(mesh.Representation, Is.EqualTo(SurfaceRepresentation.Anatomical));
            }
            finally
            {
                mesh.Clean();
            }
        }

        [Test]
        [Category("NativeDll")]
        public async Task DerivedRepresentation_RemainsAttachedToSourceMeshInsteadOfMeshManagerList()
        {
            RequireInflationLibrary();
            TestSingleMesh3D mesh = new(CreateOctahedron());
            GameObject managerObject = new("Inflated representation manager test");
            MeshManager manager = managerObject.AddComponent<MeshManager>();
            try
            {
                manager.Meshes.Add(mesh);
                await mesh.GenerateInflatedRepresentationAsync(FastSettings(4));

                Assert.That(manager.Meshes, Has.Count.EqualTo(1));
                Assert.That(manager.Meshes[0], Is.SameAs(mesh));
                Assert.That(mesh.HasInflatedRepresentation, Is.True);
            }
            finally
            {
                mesh.Clean();
                UnityEngine.Object.DestroyImmediate(managerObject);
            }
        }

        [Test]
        [Category("NativeDll")]
        public async Task ClearDuringInflation_CancelsWithoutRepublishingAfterInvalidation()
        {
            RequireInflationLibrary();
            TestSingleMesh3D mesh = new(CreateOctahedron());
            try
            {
                Cysharp.Threading.Tasks.UniTask<Mesh3DInflatedRepresentation> generation = mesh.GenerateInflatedRepresentationAsync(FastSettings(10000));
                mesh.ClearInflatedRepresentations();

                OperationCanceledException exception = null;
                try
                {
                    await generation;
                }
                catch (OperationCanceledException caught)
                {
                    exception = caught;
                }

                Assert.That(exception, Is.Not.Null);
                Assert.That(mesh.HasInflatedRepresentation, Is.False);
                Assert.That(mesh.InflatedRepresentationCacheCount, Is.Zero);
                Assert.That(mesh.Representation, Is.EqualTo(SurfaceRepresentation.Anatomical));
            }
            finally
            {
                mesh.Clean();
            }
        }

        [Test]
        [Category("NativeDll")]
        public async Task CachedProgressCallback_CannotReturnRepresentationClearedReentrantly()
        {
            RequireInflationLibrary();
            TestSingleMesh3D mesh = new(CreateOctahedron());
            try
            {
                Mesh3DInflationSettings settings = FastSettings(4);
                await mesh.GenerateInflatedRepresentationAsync(settings);
                ImmediateProgress progress = new(_ => mesh.ClearInflatedRepresentations());

                OperationCanceledException exception = null;
                try
                {
                    await mesh.GenerateInflatedRepresentationAsync(settings, progress);
                }
                catch (OperationCanceledException caught)
                {
                    exception = caught;
                }

                Assert.That(exception, Is.Not.Null);
                Assert.That(mesh.HasInflatedRepresentation, Is.False);
                Assert.That(mesh.InflatedRepresentationCacheCount, Is.Zero);
            }
            finally
            {
                mesh.Clean();
            }
        }

        [Test]
        [Category("NativeDll")]
        public async Task LeftRightReload_PublishesAtomicallyAndDisposesReplacedOwners()
        {
            RequireInflationLibrary();
            using TempDirectoryScope temp = new();
            string leftPath = temp.GetPath("left.gii");
            string rightPath = temp.GetPath("right.gii");
            WriteOctahedronGifti(leftPath);
            WriteOctahedronGifti(rightPath);
            LeftRightMesh source = new("Persistent left/right", string.Empty, leftPath, rightPath, string.Empty, string.Empty);
            LeftRightMesh3D mesh = new(source, MeshType.Patient, true);
            try
            {
                Mesh3DInflationSettings settings = FastSettings(4);
                Mesh3DInflatedRepresentation initialInflated = await mesh.GenerateInflatedRepresentationAsync(settings);
                Surface initialInflatedSurface = initialInflated.Both;
                mesh.SelectRepresentation(SurfaceRepresentation.Inflated);
                Surface previousLeft = mesh.Left;
                Surface previousRight = mesh.Right;
                Surface previousBoth = mesh.Both;

                mesh.Load();

                Assert.That(mesh.Left, Is.Not.SameAs(previousLeft));
                Assert.That(mesh.Right, Is.Not.SameAs(previousRight));
                Assert.That(mesh.Both, Is.Not.SameAs(previousBoth));
                Assert.That(previousLeft.getHandle().Handle, Is.EqualTo(IntPtr.Zero));
                Assert.That(previousRight.getHandle().Handle, Is.EqualTo(IntPtr.Zero));
                Assert.That(previousBoth.getHandle().Handle, Is.EqualTo(IntPtr.Zero));
                Assert.That(initialInflatedSurface.getHandle().Handle, Is.EqualTo(IntPtr.Zero));
                Assert.That(mesh.Representation, Is.EqualTo(SurfaceRepresentation.Anatomical));
                Assert.That(mesh.InflatedRepresentationCacheCount, Is.Zero);

                Surface currentLeft = mesh.Left;
                Surface currentRight = mesh.Right;
                Surface currentBoth = mesh.Both;
                Mesh3DInflatedRepresentation inflated = await mesh.GenerateInflatedRepresentationAsync(settings);
                mesh.SelectRepresentation(SurfaceRepresentation.Inflated);
                source.RightHemisphere = temp.GetPath("missing.gii");
                LogAssert.Expect(LogType.Error, new Regex("can't load GII file.*missing\\.gii"));

                mesh.Load();

                Assert.That(mesh.Left, Is.SameAs(currentLeft));
                Assert.That(mesh.Right, Is.SameAs(currentRight));
                Assert.That(mesh.Both, Is.SameAs(currentBoth));
                Assert.That(mesh.IsLoaded, Is.True);
                Assert.That(currentLeft.getHandle().Handle, Is.Not.EqualTo(IntPtr.Zero));
                Assert.That(currentRight.getHandle().Handle, Is.Not.EqualTo(IntPtr.Zero));
                Assert.That(currentBoth.getHandle().Handle, Is.Not.EqualTo(IntPtr.Zero));
                Assert.That(mesh.HasInflatedRepresentation, Is.True);
                Assert.That(mesh.Representation, Is.EqualTo(SurfaceRepresentation.Inflated));
                Assert.That(mesh.ActiveInflatedRepresentation, Is.SameAs(inflated));
                Assert.That(await mesh.GenerateInflatedRepresentationAsync(settings), Is.SameAs(inflated));
            }
            finally
            {
                mesh.Clean();
            }
        }

        [Test, Category("NativeDll")]
        public async Task PreparedAnatomicalSource_PreservesAnisotropicInflationAndScientificAttributesWithoutFiles()
        {
            RequireInflationLibrary();
            using TempDirectoryScope temp = new();
            string gifti = temp.GetPath("source.gii");
            string transform = temp.GetPath("source.trm");
            string parcels = temp.GetPath("atlas.gii");
            WriteOctahedronGifti(gifti);
            File.WriteAllText(parcels, "<?xml version=\"1.0\"?><GIFTI Version=\"1.0\" NumberOfDataArrays=\"1\"><MetaData /><LabelTable /><DataArray Intent=\"NIFTI_INTENT_LABEL\" DataType=\"NIFTI_TYPE_INT32\" ArrayIndexingOrder=\"RowMajorOrder\" Dimensionality=\"1\" Dim0=\"6\" Encoding=\"ASCII\" Endian=\"LittleEndian\" ExternalFileName=\"\" ExternalFileOffset=\"0\"><MetaData /><Data>1 2 3 4 5 6</Data></DataArray></GIFTI>");
            File.WriteAllText(transform, string.Join(Environment.NewLine, "5 7 11", "2 0 0", "0 1 0", "0 0 0.5"));
            var desktop = new SingleMesh3D(new SingleMesh("Native", transform, gifti, string.Empty), MeshType.Patient, true);
            Mesh3D restored = null;
            try
            {
                var buffers = desktop.Both.CopyTransferBuffers();
                var uv = new Vector2[buffers.vertices.Length];
                var colors = new Color[buffers.vertices.Length];
                for (int index = 0; index < uv.Length; ++index)
                {
                    uv[index] = new Vector2(index / 10f, (index + 1) / 10f);
                    colors[index] = new Color(index / 10f, 0.25f, 0.75f, 1f);
                }

                desktop.Both.SetBuffers(buffers.vertices, buffers.triangles, buffers.normals, uv, colors);
                using MarsAtlas atlas = new();
                string atlasDirectory = Path.GetFullPath("Assets/Data/Atlases/MarsAtlas");
                Assert.That(atlas.Load(Path.Combine(atlasDirectory, "mars_atlas_index.csv"), Path.Combine(atlasDirectory, "brodmann_areas.txt"), Path.Combine(atlasDirectory, "colin27_MNI_MarsAtlas.nii")), Is.True);
                Assert.That(desktop.Both.SearchMarsParcelFileAndUpdateColors(atlas, parcels), Is.True);
                int[] mask = desktop.Both.VisibilityMask;
                mask[0] = 0;
                desktop.Both.UpdateVisibilityMask(mask).Dispose();
                using Surface anatomicalBefore = (Surface)desktop.Both.Clone();
                long geometryVersion = desktop.Both.GeometryVersion;
                restored = Mesh3D.FromPrepared(desktop.Name, desktop.Type, (Surface)desktop.Both.Clone(), (Surface)desktop.SimplifiedBoth.Clone(), null, null, null, null, null, null, null, null, null, null);
                File.Delete(gifti);
                File.Delete(transform);
                File.Delete(parcels);
                Mesh3DInflatedRepresentation expected = await desktop.GenerateInflatedRepresentationAsync(FastSettings(20));
                Mesh3DInflatedRepresentation actual = await restored.GenerateInflatedRepresentationAsync(FastSettings(20));
                desktop.SelectRepresentation(SurfaceRepresentation.Inflated);
                restored.SelectRepresentation(SurfaceRepresentation.Inflated);
                Assert.That(actual.CoordinateSpace, Is.EqualTo(SurfaceInflationCoordinateSpace.CurrentSurfaceCoordinates));
                Assert.That(actual.Both.IsMarsAtlasLoaded, Is.True);
                AssertSurfaceBuffersEqual(desktop.Both, anatomicalBefore);
                Assert.That(desktop.Both.GeometryVersion, Is.EqualTo(geometryVersion));
                Mesh expectedMesh = new(), actualMesh = new();
                try
                {
                    expected.Both.UpdateMeshFromDLL(expectedMesh);
                    actual.Both.UpdateMeshFromDLL(actualMesh);
                    Assert.That(actualMesh.vertices, Is.EqualTo(expectedMesh.vertices));
                    Assert.That(actualMesh.colors, Is.EqualTo(expectedMesh.colors));
                    Assert.That(actualMesh.uv, Is.EqualTo(expectedMesh.uv));
                    Assert.That(actual.Both.VisibilityMask, Is.EqualTo(mask));
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(expectedMesh);
                    UnityEngine.Object.DestroyImmediate(actualMesh);
                }
            }
            finally
            {
                restored?.Clean();
                desktop.Clean();
            }
        }

        [TestCase(SurfaceInflationCoordinateSpace.NativeGifti)]
        [TestCase(SurfaceInflationCoordinateSpace.NativeGiftiThenTransformed)]
        [Category("NativeDll")]
        public void FromPrepared_RejectsObsoleteInflationWithoutTakingOwnership(SurfaceInflationCoordinateSpace coordinates)
        {
            RequireInflationLibrary();
            using Surface anatomical = CreateOctahedron();
            using Surface inflated = CreateOctahedron();
            var exception = Assert.Throws<InvalidOperationException>(() => Mesh3D.FromPrepared("obsolete", MeshType.Patient, anatomical, anatomical, null, null, null, null, inflated, inflated, null, null, null, null, coordinateSpace: coordinates));
            Assert.That(exception.Message, Does.Contain("Prepare and send"));
            Assert.That(anatomical.IsLoaded && inflated.IsLoaded, Is.True);
        }

        [Test, Category("NativeDll")]
        public async Task LoadedHemispheres_InflateIndependentlyWithoutFilesAndInvalidateCacheOnMutation()
        {
            RequireInflationLibrary();
            using TempDirectoryScope temp = new();
            string left = temp.GetPath("left.gii"), right = temp.GetPath("right.gii"), transform = temp.GetPath("surface.trm");
            WriteOctahedronGifti(left);
            WriteOctahedronGifti(right);
            File.WriteAllText(transform, string.Join(Environment.NewLine, "5 7 11", "2 0 0", "0 1 0", "0 0 0.5"));
            var mesh = new LeftRightMesh3D(new LeftRightMesh("Loaded hemispheres", transform, left, right, string.Empty, string.Empty), MeshType.Patient, true);
            try
            {
                using Surface leftBefore = (Surface)mesh.Left.Clone(), rightBefore = (Surface)mesh.Right.Clone(), bothBefore = (Surface)mesh.Both.Clone();
                File.Delete(left);
                File.Delete(right);
                File.Delete(transform);
                Mesh3DInflationSettings settings = FastSettings(20);
                var result = await mesh.GenerateInflatedRepresentationAsync(settings);
                var expectedLeft = await mesh.Left.InflateAsync(settings.Options);
                var expectedRight = await mesh.Right.InflateAsync(settings.Options);
                using (expectedLeft.Surface) AssertSurfaceBuffersEqual(result.Left, expectedLeft.Surface);
                using (expectedRight.Surface) AssertSurfaceBuffersEqual(result.Right, expectedRight.Surface);
                AssertSurfaceBuffersEqual(mesh.Left, leftBefore);
                AssertSurfaceBuffersEqual(mesh.Right, rightBefore);
                AssertSurfaceBuffersEqual(mesh.Both, bothBefore);
                Assert.That(await mesh.GenerateInflatedRepresentationAsync(settings), Is.SameAs(result));
                mesh.Left.FlipTriangles();
                mesh.Left.FlipTriangles();
                var regenerated = await mesh.GenerateInflatedRepresentationAsync(settings);
                Assert.That(regenerated, Is.Not.SameAs(result));
                Assert.That(regenerated.CacheKey.SourceGeometryIdentity, Is.Not.EqualTo(result.CacheKey.SourceGeometryIdentity));
            }
            finally
            {
                mesh.Clean();
            }
        }

        private static void AssertSurfaceBuffersEqual(Surface actual, Surface expected)
        {
            Mesh actualMesh = new(), expectedMesh = new();
            try
            {
                actual.UpdateMeshFromDLL(actualMesh);
                expected.UpdateMeshFromDLL(expectedMesh);
                Assert.That(actualMesh.vertices, Is.EqualTo(expectedMesh.vertices));
                Assert.That(actualMesh.triangles, Is.EqualTo(expectedMesh.triangles));
                Assert.That(actualMesh.colors, Is.EqualTo(expectedMesh.colors));
                Assert.That(actualMesh.uv, Is.EqualTo(expectedMesh.uv));
                Assert.That(actual.VisibilityMask, Is.EqualTo(expected.VisibilityMask));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(actualMesh);
                UnityEngine.Object.DestroyImmediate(expectedMesh);
            }
        }

        private static Mesh3DInflationSettings FastSettings(int iterationCount)
        {
            SurfaceInflationOptions options = SurfaceInflationOptions.Inflated;
            options.IterationCount = iterationCount;
            options.ConvergenceTolerance = 1e-8;
            return Mesh3DInflationSettings.Custom(options);
        }

        private static Surface CreateOctahedron(Vector3 offset = default)
        {
            Surface surface = new();
            surface.SetBuffers(new[]
            {
                offset + new Vector3(0.0f, 1.45f, 0.0f),
                offset + new Vector3(0.0f, -0.85f, 0.0f),
                offset + new Vector3(1.20f, 0.0f, 0.0f),
                offset + new Vector3(0.0f, 0.0f, 0.90f),
                offset + new Vector3(-0.80f, 0.0f, 0.0f),
                offset + new Vector3(0.0f, 0.0f, -1.10f)
            }, new[]
            {
                0, 2, 3,
                0, 3, 4,
                0, 4, 5,
                0, 5, 2,
                1, 3, 2,
                1, 4, 3,
                1, 5, 4,
                1, 2, 5
            });
            surface.ComputeNormals();
            return surface;
        }

        private static Surface CreateDegenerateSurface()
        {
            Surface surface = new();
            surface.SetBuffers(new[] { Vector3.zero, Vector3.right, Vector3.right * 2f }, new[] { 0, 1, 2 });
            return surface;
        }

        private static void WriteOctahedronGifti(string path)
        {
            const string identity = "1 0 0 0 0 1 0 0 0 0 1 0 0 0 0 1";
            const string vertices = "0 1.45 0  0 -0.85 0  1.2 0 0  0 0 0.9  -0.8 0 0  0 0 -1.1";
            const string triangles = "0 2 3  0 3 4  0 4 5  0 5 2  1 3 2  1 4 3  1 5 4  1 2 5";
            File.WriteAllText(path, string.Join(Environment.NewLine, "<?xml version=\"1.0\" encoding=\"UTF-8\"?>", "<GIFTI Version=\"1.0\" NumberOfDataArrays=\"2\"><MetaData /><LabelTable />", "<DataArray Intent=\"NIFTI_INTENT_POINTSET\" DataType=\"NIFTI_TYPE_FLOAT32\" ArrayIndexingOrder=\"RowMajorOrder\" Dimensionality=\"2\" Encoding=\"ASCII\" Endian=\"LittleEndian\" ExternalFileName=\"\" ExternalFileOffset=\"0\" Dim0=\"6\" Dim1=\"3\">", $"<MetaData /><CoordinateSystemTransformMatrix><DataSpace>NIFTI_XFORM_UNKNOWN</DataSpace><TransformedSpace>NIFTI_XFORM_UNKNOWN</TransformedSpace><MatrixData>{identity}</MatrixData></CoordinateSystemTransformMatrix><Data>{vertices}</Data></DataArray>", "<DataArray Intent=\"NIFTI_INTENT_TRIANGLE\" DataType=\"NIFTI_TYPE_INT32\" ArrayIndexingOrder=\"RowMajorOrder\" Dimensionality=\"2\" Encoding=\"ASCII\" Endian=\"LittleEndian\" ExternalFileName=\"\" ExternalFileOffset=\"0\" Dim0=\"8\" Dim1=\"3\">", $"<MetaData /><Data>{triangles}</Data></DataArray></GIFTI>"));
        }

        private static void RequireInflationLibrary()
        {
            if (!HbpCoreRuntime.TryGetVersion(out _, out string error))
            {
                Assert.Ignore($"hbp_core is unavailable: {error}");
            }
        }

        private sealed class TestSingleMesh3D : SingleMesh3D
        {
            public TestSingleMesh3D(Surface surface)
            {
                Name = "Synthetic single mesh";
                m_Both = surface;
                m_SimplifiedBoth = (Surface)surface.Clone();
            }
        }

        private sealed class ImmediateProgress : IProgress<float>
        {
            private readonly Action<float> m_Report;

            public ImmediateProgress(Action<float> report)
            {
                m_Report = report;
            }

            public void Report(float value)
            {
                m_Report(value);
            }
        }

        private sealed class TestLeftRightMesh3D : LeftRightMesh3D
        {
            public TestLeftRightMesh3D(Surface left, Surface right)
            {
                Name = "Synthetic left/right mesh";
                m_Left = left;
                m_Right = right;
                m_Both = (Surface)left.Clone();
                m_Both.Append(right);
                m_SimplifiedLeft = (Surface)left.Clone();
                m_SimplifiedRight = (Surface)right.Clone();
                m_SimplifiedBoth = (Surface)m_Both.Clone();
            }
        }
    }
}
