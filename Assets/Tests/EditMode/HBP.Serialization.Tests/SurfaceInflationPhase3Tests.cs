using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using HBP.Core.DLL;
using HBP.Core.DLL.HbpCore;
using HBP.Tests.Serialization.Helpers;
using NUnit.Framework;
using UnityEngine;

namespace HBP.Tests.Serialization
{
    public class SurfaceInflationPhase3Tests
    {
        [Test]
        public void InteropStructures_MatchNativeAbiLayout()
        {
            Assert.That((int)HbpCoreStatus.Cancelled, Is.EqualTo(5));
            Assert.That(Marshal.SizeOf<SurfaceInflationOptions>(), Is.EqualTo(56));
            Assert.That(Marshal.OffsetOf<SurfaceInflationOptions>(nameof(SurfaceInflationOptions.Method)).ToInt32(), Is.EqualTo(4));
            Assert.That(Marshal.OffsetOf<SurfaceInflationOptions>(nameof(SurfaceInflationOptions.SmoothingStrength)).ToInt32(), Is.EqualTo(16));
            Assert.That(Marshal.OffsetOf<SurfaceInflationOptions>(nameof(SurfaceInflationOptions.FixBoundaryVerticesValue)).ToInt32(), Is.EqualTo(52));
            Assert.That(Marshal.SizeOf<SurfaceInflationDistribution>(), Is.EqualTo(40));
            Assert.That(Marshal.SizeOf<SurfaceInflationVector3>(), Is.EqualTo(24));
            Assert.That(Marshal.SizeOf<SurfaceInflationReport>(), Is.EqualTo(416));

            SurfaceInflationOptions options = SurfaceInflationOptions.Inflated;
            Assert.That(options.StructSize, Is.EqualTo(56));
            Assert.That(options.Method, Is.EqualTo(SurfaceInflationMethod.MetricRegularized));
            Assert.That(options.Rescale, Is.EqualTo(SurfaceInflationRescale.PreserveRmsRadius));
            Assert.That(options.FixBoundaryVertices, Is.True);
        }

        [Test]
        [Category("NativeDll")]
        public void Operation_SnapshotsSourceAndTransfersResultOwnership()
        {
            RequireInflationLibrary();
            Surface source = CreateOctahedron();
            using SurfaceInflationOperation operation = new(source, TestOptions());
            source.Dispose();

            SurfaceInflationExecution execution = operation.Execute();
            Assert.That(execution.Status, Is.EqualTo(HbpCoreStatus.Ok), execution.Error);
            Assert.That(operation.Progress, Is.EqualTo(1.0f));
            Assert.That(operation.GetReport().VertexCount, Is.EqualTo(6));

            using Surface result = operation.TakeResult();
            Assert.That(result.NumberOfVertices, Is.EqualTo(6));
            Assert.That(result.NumberOfTriangles, Is.EqualTo(8));
            Assert.Throws<InvalidOperationException>(() => operation.TakeResult());
        }

        [Test]
        [Category("NativeDll")]
        public async Task InflateAsync_ReturnsOwnedSurfaceReportAndCurrentCoordinateSpace()
        {
            RequireInflationLibrary();
            using Surface source = CreateOctahedron();
            SurfaceInflationResult result = await source.InflateAsync(TestOptions());
            using (result.Surface)
            {
                Assert.That(result.CoordinateSpace, Is.EqualTo(SurfaceInflationCoordinateSpace.CurrentSurfaceCoordinates));
                Assert.That(result.Report.StructSize, Is.EqualTo(416));
                Assert.That(result.Report.VertexCount, Is.EqualTo(source.NumberOfVertices));
                Assert.That(result.Report.TriangleCount, Is.EqualTo(source.NumberOfTriangles));
                Assert.That(result.Surface.NumberOfVertices, Is.EqualTo(source.NumberOfVertices));
            }
        }

        [Test]
        [Category("NativeDll")]
        public async Task InflateAsync_PropagatesNativeValidationErrorAndReport()
        {
            RequireInflationLibrary();
            using Surface source = new();
            source.SetBuffers(new[] { Vector3.zero, Vector3.right, Vector3.right * 2.0f }, new[] { 0, 1, 2 });

            SurfaceInflationException exception = null;
            try
            {
                await source.InflateAsync(TestOptions());
            }
            catch (SurfaceInflationException caught)
            {
                exception = caught;
            }

            Assert.That(exception, Is.Not.Null);
            Assert.That(exception.Message, Does.Contain("degenerate").IgnoreCase);
            Assert.That(exception.Report.StructSize, Is.EqualTo(416));
        }

        [Test]
        [Category("NativeDll")]
        public async Task InflateAsync_ForwardsPreRequestedCancellationWithoutResult()
        {
            RequireInflationLibrary();
            using Surface source = CreateOctahedron();
            using CancellationTokenSource cancellation = new();
            cancellation.Cancel();

            SurfaceInflationCanceledException exception = null;
            try
            {
                await source.InflateAsync(TestOptions(), cancellationToken: cancellation.Token);
            }
            catch (SurfaceInflationCanceledException caught)
            {
                exception = caught;
            }

            Assert.That(exception, Is.Not.Null);
            Assert.That(exception.CancellationToken, Is.EqualTo(cancellation.Token));
            Assert.That(exception.Report.StructSize, Is.EqualTo(416));
        }

        [Test]
        [Category("NativeDll")]
        public async Task InflateGIIFileAsync_InflatesBeforeApplyingAnisotropicTransformation()
        {
            RequireInflationLibrary();
            using TempDirectoryScope temp = new();
            string transformationPath = temp.GetPath("anisotropic.trm");
            File.WriteAllText(transformationPath, string.Join(Environment.NewLine, "5 7 11", "2 0 0", "0 1 0", "0 0 0.5"));
            string giftiPath = temp.GetPath("octahedron.gii");
            WriteOctahedronGifti(giftiPath);
            SurfaceInflationOptions options = TestOptions();

            SurfaceInflationResult actual = await Surface.InflateGIIFileAsync(giftiPath, transformationPath, options);
            using Surface nativeSource = new();
            Assert.That(nativeSource.LoadGIIFile(giftiPath), Is.True);
            SurfaceInflationResult expected = await nativeSource.InflateAsync(options);
            using Transformation3 transformation = Transformation3.FromFile(transformationPath);
            expected.Surface.ApplyTransformation(transformation);

            using (actual.Surface)
            using (expected.Surface)
            {
                Assert.That(actual.CoordinateSpace, Is.EqualTo(SurfaceInflationCoordinateSpace.NativeGiftiThenTransformed));
                Vector3[] actualVertices = CopyVertices(actual.Surface);
                Vector3[] expectedVertices = CopyVertices(expected.Surface);
                Assert.That(actualVertices.Length, Is.EqualTo(expectedVertices.Length));
                for (int index = 0; index < actualVertices.Length; ++index)
                {
                    Assert.That(Vector3.Distance(actualVertices[index], expectedVertices[index]), Is.LessThan(1e-5f), $"vertex {index}");
                }
            }
        }

        private static SurfaceInflationOptions TestOptions()
        {
            SurfaceInflationOptions options = SurfaceInflationOptions.Inflated;
            options.IterationCount = 20;
            options.ConvergenceTolerance = 1e-8;
            return options;
        }

        private static Surface CreateOctahedron()
        {
            Surface surface = new();
            surface.SetBuffers(new[]
            {
                new Vector3(0.0f, 1.45f, 0.0f),
                new Vector3(0.0f, -0.85f, 0.0f),
                new Vector3(1.20f, 0.0f, 0.0f),
                new Vector3(0.0f, 0.0f, 0.90f),
                new Vector3(-0.80f, 0.0f, 0.0f),
                new Vector3(0.0f, 0.0f, -1.10f)
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

        private static void WriteOctahedronGifti(string path)
        {
            const string identity = "1 0 0 0 0 1 0 0 0 0 1 0 0 0 0 1";
            const string vertices = "0 1.45 0  0 -0.85 0  1.2 0 0  0 0 0.9  -0.8 0 0  0 0 -1.1";
            const string triangles = "0 2 3  0 3 4  0 4 5  0 5 2  1 3 2  1 4 3  1 5 4  1 2 5";
            File.WriteAllText(path, string.Join(Environment.NewLine, "<?xml version=\"1.0\" encoding=\"UTF-8\"?>", "<GIFTI Version=\"1.0\" NumberOfDataArrays=\"2\"><MetaData /><LabelTable />", "<DataArray Intent=\"NIFTI_INTENT_POINTSET\" DataType=\"NIFTI_TYPE_FLOAT32\" ArrayIndexingOrder=\"RowMajorOrder\" Dimensionality=\"2\" Encoding=\"ASCII\" Endian=\"LittleEndian\" ExternalFileName=\"\" ExternalFileOffset=\"0\" Dim0=\"6\" Dim1=\"3\">", $"<MetaData /><CoordinateSystemTransformMatrix><DataSpace>NIFTI_XFORM_UNKNOWN</DataSpace><TransformedSpace>NIFTI_XFORM_UNKNOWN</TransformedSpace><MatrixData>{identity}</MatrixData></CoordinateSystemTransformMatrix><Data>{vertices}</Data></DataArray>", "<DataArray Intent=\"NIFTI_INTENT_TRIANGLE\" DataType=\"NIFTI_TYPE_INT32\" ArrayIndexingOrder=\"RowMajorOrder\" Dimensionality=\"2\" Encoding=\"ASCII\" Endian=\"LittleEndian\" ExternalFileName=\"\" ExternalFileOffset=\"0\" Dim0=\"8\" Dim1=\"3\">", $"<MetaData /><Data>{triangles}</Data></DataArray></GIFTI>"));
        }

        private static Vector3[] CopyVertices(Surface surface)
        {
            Mesh mesh = new();
            try
            {
                surface.UpdateMeshFromDLL(mesh);
                return mesh.vertices.ToArray();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(mesh);
            }
        }

        private static void RequireInflationLibrary()
        {
            if (!HbpCoreRuntime.TryGetVersion(out _, out string error))
            {
                Assert.Ignore($"hbp_core is unavailable: {error}");
            }
        }
    }
}
