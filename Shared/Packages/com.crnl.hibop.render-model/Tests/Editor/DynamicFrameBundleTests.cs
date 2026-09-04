using System;
using CRNL.HiBoP.Contracts;
using NUnit.Framework;

namespace CRNL.HiBoP.RenderModel.Tests
{
    public class DynamicFrameBundleTests
    {
        [Test]
        public void Bundle_RequiresEveryExpectedColumnExactlyOnce()
        {
            ContractId columnA = RenderModelValidationTests.Id(10);
            ContractId columnB = RenderModelValidationTests.Id(20);
            ColumnFrame frameA = Frame(columnA);

            Assert.Throws<ArgumentException>(() => new DynamicFrameBundle(new SessionEpoch(RenderModelValidationTests.Id(1), 1), RenderModelValidationTests.Id(2), new ScopeRevision(1), 0.25, new RenderTemporalSample(0, 0.5f), new StateRevision(1), new[] { columnA, columnB }, new[] { frameA }));
        }

        [Test]
        public void Bundle_AcceptsOneCompleteFramePerColumn()
        {
            ContractId columnA = RenderModelValidationTests.Id(10);
            ContractId columnB = RenderModelValidationTests.Id(20);

            DynamicFrameBundle bundle = new(new SessionEpoch(RenderModelValidationTests.Id(1), 1), RenderModelValidationTests.Id(2), new ScopeRevision(1), 0.25, new RenderTemporalSample(0, 0.5f), new StateRevision(1), new[] { columnA, columnB }, new[] { Frame(columnA), Frame(columnB) });

            Assert.That(bundle.ColumnFrames, Has.Count.EqualTo(2));
        }

        [Test]
        public void Bundle_LegacyConstructorInfersOverlayManifest()
        {
            ContractId columnId = RenderModelValidationTests.Id(10);
            ContractId cutId = RenderModelValidationTests.Id(30);
            RenderTemporalSample sample = new(0, 0.5f);
            CutOverlayFrame overlay = Overlay(cutId, columnId, new StateRevision(1), sample);
            ColumnFrame column = new(columnId, RenderModelValidationTests.Hash(10), new ScopeRevision(1), Optional<SurfaceFrame>.None, Optional<SiteRenderFrame>.None, new[] { overlay });

            DynamicFrameBundle bundle = new(new SessionEpoch(RenderModelValidationTests.Id(1), 1), RenderModelValidationTests.Id(2), new ScopeRevision(1), 0.25, sample, new StateRevision(1), new[] { columnId }, new[] { column });

            Assert.That(bundle.Expectations[0].CutIds, Is.EqualTo(new[] { cutId }));
        }

        [Test]
        public void Bundle_RejectsCutOverlayFromAnotherStateRevision()
        {
            ContractId columnId = RenderModelValidationTests.Id(10);
            RenderTemporalSample sample = new(0, 0.5f);
            CutOverlayFrame overlay = Overlay(RenderModelValidationTests.Id(30), columnId, new StateRevision(2), sample);
            ColumnFrame column = new(columnId, RenderModelValidationTests.Hash(10), new ScopeRevision(1), Optional<SurfaceFrame>.None, Optional<SiteRenderFrame>.None, new[] { overlay });

            Assert.Throws<ArgumentException>(() => new DynamicFrameBundle(new SessionEpoch(RenderModelValidationTests.Id(1), 1), RenderModelValidationTests.Id(2), new ScopeRevision(1), 0.25, sample, new StateRevision(1), new[] { columnId }, new[] { column }));
        }

        [Test]
        public void CutResult_RejectsOverlayWithDifferentCutIdentity()
        {
            ContractId cutId = RenderModelValidationTests.Id(30);
            ContractId otherCutId = RenderModelValidationTests.Id(31);
            RenderTemporalSample sample = new(0, 0.5f);
            CutOverlayFrame overlay = Overlay(otherCutId, RenderModelValidationTests.Id(10), new StateRevision(1), sample);

            Assert.Throws<ArgumentException>(() => new CutRenderResult(cutId, RenderModelValidationTests.Id(40), new InteractionSequence(1), new ScopeRevision(1), new ScopeRevision(1), new StateRevision(1), sample, new Plane3F(new Float3(0f, 0f, 1f), 0f), RenderModelValidationTests.Hash(50), Optional<CutGeometryAsset>.None, RenderModelValidationTests.Hash(60), Optional<TextureAsset>.None, new[] { overlay }));
        }

        private static ColumnFrame Frame(ContractId id)
        {
            return new ColumnFrame(id, RenderModelValidationTests.Hash(id.High), new ScopeRevision(1), Optional<SurfaceFrame>.None, Optional<SiteRenderFrame>.None, Array.Empty<CutOverlayFrame>());
        }

        private static CutOverlayFrame Overlay(ContractId cutId, ContractId columnId, StateRevision revision, RenderTemporalSample sample)
        {
            return new CutOverlayFrame(cutId, columnId, revision, 1, 1, sample, TemporalApplication.SampleAndHold, new ScopeRevision(1), RenderBuffer<Rgba32>.TakeOwnership(new[] { new Rgba32(1, 2, 3, 4) }));
        }
    }
}
