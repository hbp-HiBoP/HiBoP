using System;
using System.IO;
using HBP.Sync;
using HBP.Sync.Scene;
using HBP.Transfer.Scene;
using HBP.Transfer.Transport;
using NUnit.Framework;

namespace HBP.Tests.Transfer
{
    public sealed class SceneDisplayProtocolTests
    {
        [Test]
        public void DisplayControlRoundTripsWithoutChangingArchiveFormats()
        {
            var state = new SceneDisplayBatchState { Id = Guid.NewGuid().ToString("N"), Visualizations = new[] { "small", "single" }, Progress = .37f };
            state.Validate();
            var request = new SessionControlRequest(Guid.NewGuid(), 4, Guid.NewGuid(), SessionControlKind.DisplayBatch, body: SceneSessionState.Encode(state));
            var restored = SceneSessionState.Decode<SceneDisplayBatchState>(request.GetBody());
            restored.Validate();
            Assert.That(restored.Visualizations, Is.EqualTo(state.Visualizations));
            Assert.That(restored.Progress, Is.EqualTo(.37f));
            Assert.That(restored.Id, Is.EqualTo(state.Id));
        }

        [TestCase(float.NaN), TestCase(float.PositiveInfinity), TestCase(-.1f), TestCase(1.01f)]
        public void InvalidProgressCannotEnterThePresentation(float progress)
        {
            var state = new SceneDisplayBatchState { Id = Guid.NewGuid().ToString("N"), Visualizations = new[] { "small" }, Progress = progress };
            Assert.Throws<InvalidDataException>(state.Validate);
        }

        [Test]
        public void RepeatedOrMissingScenesAreRejected()
        {
            var state = new SceneDisplayBatchState { Id = Guid.NewGuid().ToString("N"), Visualizations = new[] { "small", "small" } };
            Assert.Throws<InvalidDataException>(state.Validate);
            state.Visualizations = Array.Empty<string>();
            Assert.Throws<InvalidDataException>(state.Validate);
        }

        [Test]
        public void RenderReadyIsCorrelatedToPolicyAndDistinctFromInitialApply()
        {
            var policy = new OperationId(Guid.NewGuid());
            byte[] bytes = PublicationControlCodec.EncodeRenderReady(policy);
            Assert.That(PublicationControlCodec.TryDecodeRenderReady(bytes, out var restored), Is.True);
            Assert.That(restored, Is.EqualTo(policy));
            Assert.That(PublicationControlCodec.TryDecodeAcknowledgement(bytes, out _), Is.False);
            Assert.That(PublicationControlCodec.TryDecodeRenderReady(PublicationControlCodec.EncodeAcknowledgement(policy), out _), Is.False);
            Array.Resize(ref bytes, bytes.Length - 1);
            Assert.That(PublicationControlCodec.TryDecodeRenderReady(bytes, out _), Is.False);
        }
    }
}
