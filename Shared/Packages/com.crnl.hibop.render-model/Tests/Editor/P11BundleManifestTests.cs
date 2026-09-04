using System;
using CRNL.HiBoP.Contracts;
using NUnit.Framework;

namespace CRNL.HiBoP.RenderModel.Tests
{
    public class P11BundleManifestTests
    {
        [Test]
        public void Bundle_RejectsMissingRequiredSitesAndCutOverlay()
        {
            ContractId columnId = RenderModelValidationTests.Id(10);
            ContractId cutId = RenderModelValidationTests.Id(11);
            DynamicColumnExpectation expectation = new(columnId, DynamicColumnContent.Surface | DynamicColumnContent.Sites, new[] { cutId });
            RenderTemporalSample sample = new(0, 0.75f);
            StateRevision revision = new(1);
            SurfaceFrame surface = Surface(RenderModelValidationTests.Hash(20), revision, sample, TemporalApplication.SampleAndHold);
            ColumnFrame incomplete = new(columnId, surface.SurfaceAssetHash, new ScopeRevision(1), Optional<SurfaceFrame>.Some(surface), Optional<SiteRenderFrame>.None, Array.Empty<CutOverlayFrame>());

            Assert.Throws<ArgumentException>(() => Bundle(sample, revision, expectation, incomplete));
        }

        [Test]
        public void Bundle_RejectsSurfaceInterpolationThatViolatesP03()
        {
            ContractId columnId = RenderModelValidationTests.Id(10);
            DynamicColumnExpectation expectation = new(columnId, DynamicColumnContent.Surface, Array.Empty<ContractId>());
            RenderTemporalSample sample = new(0, 0.75f);
            StateRevision revision = new(1);
            SurfaceFrame surface = Surface(RenderModelValidationTests.Hash(20), revision, sample, TemporalApplication.Linear);
            ColumnFrame frame = new(columnId, surface.SurfaceAssetHash, new ScopeRevision(1), Optional<SurfaceFrame>.Some(surface), Optional<SiteRenderFrame>.None, Array.Empty<CutOverlayFrame>());

            Assert.Throws<ArgumentException>(() => Bundle(sample, revision, expectation, frame));
        }

        private static DynamicFrameBundle Bundle(RenderTemporalSample sample, StateRevision revision, DynamicColumnExpectation expectation, ColumnFrame frame)
        {
            return new DynamicFrameBundle(new SessionEpoch(RenderModelValidationTests.Id(1), 1), RenderModelValidationTests.Id(2), new ScopeRevision(1), 1, 0.75, sample, revision, new[] { expectation }, new[] { frame });
        }

        private static SurfaceFrame Surface(AssetHash hash, StateRevision revision, RenderTemporalSample sample, TemporalApplication temporal)
        {
            return new SurfaceFrame(hash, revision, sample, temporal, RenderBuffer<float>.TakeOwnership(new[] { 1f }), RenderBuffer<float>.TakeOwnership(new[] { 0.5f }), RenderBuffer<byte>.TakeOwnership(new byte[] { 1 }));
        }
    }
}
