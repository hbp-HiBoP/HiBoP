using System;
using System.IO;
using HBP.Sync;
using HBP.Sync.Scene;
using HBP.Transfer.Scene;
using HBP.Transfer.Transport;
using HBP.Core.Tools;
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
        public void SharedLoadingMessagesKeepTheirCountersAcrossTheWire()
        {
            var state = new SceneDisplayBatchState
            {
                Id = Guid.NewGuid().ToString("N"),
                Visualizations = new[] { "first", "second" },
                Names = new[] { "Anatomy", "Memory" },
                SourceStatus = SceneLoadingStatus.From(.4f, new LoadingText("Loading data", " ", "3/119"), "second")
            };
            var restored = SceneSessionState.Decode<SceneDisplayBatchState>(SceneSessionState.Encode(state));
            restored.Validate();
            var desktop = restored.SourceStatus.ToText(restored.Visualizations, restored.Names);
            var quest = restored.SourceStatus.ToText(restored.Visualizations, restored.Names, receiving: true);
            Assert.That(quest.ToString(), Is.EqualTo(desktop.ToString()).And.EqualTo("Loading data 3/119"));
            Assert.That(quest.Prefix, Is.EqualTo("Loading data"));
            Assert.That(quest.Message, Is.EqualTo(" "));
        }

        [Test]
        public void OnlyTransferDirectionChangesBetweenDevices()
        {
            var status = SceneLoadingStatus.From(.42f, new LoadingText(), "scene", true);
            var desktop = status.ToText(new[] { "first", "scene" }, new[] { "Anatomy", "Memory" });
            var quest = status.ToText(new[] { "first", "scene" }, new[] { "Anatomy", "Memory" }, receiving: true);
            Assert.That(desktop.ToString(), Is.EqualTo("Sending scene to Quest Memory - 42%"));
            Assert.That(quest.ToString(), Is.EqualTo("Receiving scene Memory - 42%"));
        }

        [TestCase(false), TestCase(true)]
        public void LegacySurfaceProgressCannotBlockDisplayControlOrCompletion(bool finished)
        {
            // The historical surface loader can report 2 after its final step.
            var state = new SceneDisplayBatchState
            {
                Id = Guid.NewGuid().ToString("N"),
                Visualizations = new[] { "scene" },
                Names = new[] { "VISU A" },
                Progress = finished ? 1 : .5f,
                Finished = finished,
                SourceStatus = SceneLoadingStatus.From(2f, new LoadingText("Inflating surface"), "scene")
            };
            var restored = SceneSessionState.Decode<SceneDisplayBatchState>(SceneSessionState.Encode(state));
            restored.Validate();
            Assert.That(restored.SourceStatus.Progress, Is.EqualTo(1));
            Assert.That(restored.SourceStatus.ToText(restored.Visualizations, restored.Names, receiving: true).ToString(), Is.EqualTo("Inflating surface"));
        }

        [Test]
        public void PresentationCannotReferToAnotherSceneOrUnboundedText()
        {
            var state = new SceneDisplayBatchState { Id = Guid.NewGuid().ToString("N"), Visualizations = new[] { "scene" } };
            state.SourceStatus = SceneLoadingStatus.From(.2f, new LoadingText("Loading"), "other");
            Assert.Throws<InvalidDataException>(state.Validate);
            state.SourceStatus.VisualizationId = "scene";
            state.SourceStatus.Prefix = new string('x', 2049);
            Assert.Throws<InvalidDataException>(state.Validate);
            state.SourceStatus = null;
            state.Names = new[] { "First", "Second" };
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
