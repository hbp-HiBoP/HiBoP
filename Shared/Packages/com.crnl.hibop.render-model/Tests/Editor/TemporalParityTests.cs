using CRNL.HiBoP.Contracts;
using NUnit.Framework;

namespace CRNL.HiBoP.RenderModel.Tests
{
    public class TemporalParityTests
    {
        [Test]
        public void D5_RecordsDesktopSiteLinearAndSurfaceSampleAndHoldSemantics()
        {
            RenderTemporalSample sample = new(0, 0.75f);
            float siteValue = sample.EvaluateLinear(0f, 10f);
            SurfaceFrame surface = new(RenderModelValidationTests.Hash(10), new StateRevision(1), sample, TemporalApplication.SampleAndHold, RenderBuffer<float>.TakeOwnership(new[] { 0f }), RenderBuffer<float>.TakeOwnership(new[] { 0.8f }), RenderBuffer<byte>.TakeOwnership(new byte[] { 1 }));
            SiteRenderFrame sites = new(RenderModelValidationTests.Hash(20), new StateRevision(1), sample, TemporalApplication.Linear, RenderBuffer<Float3>.TakeOwnership(new[] { new Float3(1f, 2f, 3f) }), RenderBuffer<Rgba32>.TakeOwnership(new[] { new Rgba32(255, 0, 0, 255) }), RenderBuffer<float>.TakeOwnership(new[] { siteValue }), RenderBuffer<byte>.TakeOwnership(new byte[] { 1 }), RenderBuffer<SiteRenderFlags>.TakeOwnership(new[] { SiteRenderFlags.Filtered }));

            Assert.That(siteValue, Is.EqualTo(7.5f).Within(0.000001f));
            Assert.That(sites.TemporalApplication, Is.EqualTo(TemporalApplication.Linear));
            Assert.That(surface.TemporalApplication, Is.EqualTo(TemporalApplication.SampleAndHold));
            Assert.That(surface.ActivityValues[0], Is.EqualTo(0f));
            Assert.That(surface.Sample.TemporalAlpha, Is.EqualTo(0.75f));
        }

        [Test]
        public void IndependentReconstructor_RebuildsDesktopUvSentinelsExactly()
        {
            SurfaceFrame frame = new(RenderModelValidationTests.Hash(30), new StateRevision(2), new RenderTemporalSample(4, 0.25f), TemporalApplication.SampleAndHold, RenderBuffer<float>.TakeOwnership(new[] { 0.25f, 0.5f }), RenderBuffer<float>.TakeOwnership(new[] { 0.8f, 0.01f }), RenderBuffer<byte>.TakeOwnership(new byte[] { 1, 0 }));

            SurfaceRenderStreams streams = RenderModelReconstructor.ReconstructSurfaceStreams(frame);

            Assert.That(streams.ActivityUvs.ToArray(), Is.EqualTo(new[] { new Float2(0.25f, 0f), new Float2(0.5f, 1f) }));
            Assert.That(streams.OpacityUvs.ToArray(), Is.EqualTo(new[] { new Float2(0.8f, 0f), new Float2(0.01f, 1f) }));
        }
    }
}
