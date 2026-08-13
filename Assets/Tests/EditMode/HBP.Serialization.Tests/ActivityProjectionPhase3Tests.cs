using System.Linq;
using HBP.Core.DLL;
using HBP.Core.Enums;
using HBP.Tests.Serialization.Helpers;
using NUnit.Framework;
using UnityEngine;

namespace HBP.Tests.Serialization
{
    public class ActivityProjectionPhase3Tests
    {
        [SetUp]
        public void SetUp()
        {
            NativeParityAssert.RequireHbpCore();
        }

        [Test]
        [Category("NativeMigration")]
        [Category("ActivityProjection.Phase3")]
        public void SurfaceProjectionBinding_IsLazyCachedAndRebuiltAfterGeometryChanges()
        {
            using Volume volume = LoadVolume();
            using BBox bounds = volume.BoundingBox;
            using ActivityProjectionGrid grid = new();
            grid.Initialize(volume, 8, VolumeInterpolation.Trilinear);

            using RawSiteList sites = new();
            sites.AddSite("S1", ToNative(volume.Center), 0, 0);
            sites.UpdateMask(0, false);
            using DensityGenerator density = new();
            density.Initialize(grid);
            density.ComputeActivity(sites, bounds.DiagonalLength * 2.0f, SiteInfluenceByDistanceType.Constant);

            float outsideOffset = bounds.DiagonalLength * 10.0f + 10.0f;
            Vector3 outside = bounds.Center + Vector3.one * outsideOffset;
            Vector3[] partialVertices =
            {
                bounds.Center + new Vector3(-0.1f, -0.1f, 0.0f),
                bounds.Center + new Vector3(0.1f, -0.1f, 0.0f),
                outside,
                outside + new Vector3(0.5f, 0.0f, 0.0f)
            };
            using Surface surface = CreateSurface(partialVertices);
            using SurfaceGenerator projection = new();
            projection.Initialize(density, surface);

            Assert.That(projection.ProjectionCoverage.classification, Is.EqualTo(SurfaceProjectionClassification.Unavailable));
            projection.ComputeMainUV(0.0f, 1.0f);
            Assert.That(projection.ProjectionCoverage.classification, Is.EqualTo(SurfaceProjectionClassification.Unavailable), "Anatomy sampling must not eagerly build the activity binding.");

            Mesh mesh = new();
            try
            {
                surface.UpdateMeshFromDLL(mesh);
                Assert.That(mesh.uv[2], Is.EqualTo(Vector2.zero));
                Assert.That(mesh.uv[3], Is.EqualTo(Vector2.zero));
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }

            projection.ComputeActivityUV(0, 0.25f);
            SurfaceProjectionCoverage partialCoverage = projection.ProjectionCoverage;
            Assert.That(partialCoverage.totalVertexCount, Is.EqualTo(4));
            Assert.That(partialCoverage.validVertexCount, Is.EqualTo(2));
            Assert.That(partialCoverage.invalidVertexCount, Is.EqualTo(2));
            Assert.That(partialCoverage.validRatio, Is.EqualTo(0.5f));
            Assert.That(partialCoverage.classification, Is.EqualTo(SurfaceProjectionClassification.Partial));
            Assert.That(projection.AlphaUV.Count(uv => uv.y == 0.0f), Is.EqualTo(2));
            Assert.That(projection.AlphaUV.Count(uv => uv == new Vector2(0.01f, 1.0f)), Is.EqualTo(2));

            projection.ComputeActivityUV(0, 0.5f);
            SurfaceProjectionCoverage cachedCoverage = projection.ProjectionCoverage;
            Assert.That(cachedCoverage.bindingVersion, Is.EqualTo(partialCoverage.bindingVersion));
            Assert.That(cachedCoverage.buildMilliseconds, Is.EqualTo(partialCoverage.buildMilliseconds));

            surface.SetBuffers(new[]
            {
                outside,
                outside + Vector3.right,
                outside + Vector3.up,
                outside + Vector3.forward
            }, TetrahedronTriangles);
            projection.Initialize(density, surface);
            Assert.That(projection.ProjectionCoverage.classification, Is.EqualTo(SurfaceProjectionClassification.Unavailable));
            projection.ComputeActivityUV(0, 0.5f);
            SurfaceProjectionCoverage disjointCoverage = projection.ProjectionCoverage;
            Assert.That(disjointCoverage.classification, Is.EqualTo(SurfaceProjectionClassification.None));
            Assert.That(disjointCoverage.validVertexCount, Is.Zero);
            Assert.That(disjointCoverage.invalidVertexCount, Is.EqualTo(4));
            Assert.That(disjointCoverage.bindingVersion, Is.GreaterThan(partialCoverage.bindingVersion));
            Assert.That(projection.ActivityUV, Has.All.EqualTo(new Vector2(0.5f, 1.0f)));
            Assert.That(projection.AlphaUV, Has.All.EqualTo(new Vector2(0.01f, 1.0f)));
        }

        [Test]
        [Category("ActivityProjection.Phase3")]
        public void SurfaceProjectionCoverage_UserMessageThresholdToleratesSmallBorderErrors()
        {
            SurfaceProjectionCoverage coverage = new()
            {
                totalVertexCount = 100,
                invalidVertexCount = 32,
                classification = SurfaceProjectionClassification.Partial
            };
            Assert.That(coverage.RequiresUserMessage, Is.False);

            coverage.invalidVertexCount = 33;
            Assert.That(coverage.RequiresUserMessage, Is.True);

            coverage.totalVertexCount = 10_001;
            coverage.invalidVertexCount = 100;
            Assert.That(coverage.RequiresUserMessage, Is.False);
            coverage.invalidVertexCount = 101;
            Assert.That(coverage.RequiresUserMessage, Is.True);

            coverage.classification = SurfaceProjectionClassification.Complete;
            Assert.That(coverage.RequiresUserMessage, Is.False);
            coverage.classification = SurfaceProjectionClassification.None;
            Assert.That(coverage.RequiresUserMessage, Is.True);
        }

        private static readonly int[] TetrahedronTriangles =
        {
            0, 1, 2,
            0, 3, 1,
            0, 2, 3,
            1, 3, 2
        };

        private static Volume LoadVolume()
        {
            Volume volume = new();
            try
            {
                string path = NativeParityAssert.NativePath("Nifti", "fmri_3d.nii");
                Assert.That(volume.LoadNIFTIFile(path), Is.True, path);
                return volume;
            }
            catch
            {
                volume.Dispose();
                throw;
            }
        }

        private static Surface CreateSurface(Vector3[] vertices)
        {
            Surface surface = new();
            try
            {
                surface.SetBuffers(vertices, TetrahedronTriangles);
                return surface;
            }
            catch
            {
                surface.Dispose();
                throw;
            }
        }

        private static Vector3 ToNative(Vector3 unity)
        {
            return new Vector3(-unity.x, unity.y, unity.z);
        }
    }
}
