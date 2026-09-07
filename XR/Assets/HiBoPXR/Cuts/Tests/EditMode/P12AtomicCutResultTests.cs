using System;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.Protocol;
using CRNL.HiBoP.RenderModel;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CRNL.HiBoP.XR.Cuts.Tests
{
    public class P12AtomicCutResultTests
    {
        private const long Budget = 16 * 1024 * 1024;
        private static readonly SessionEpoch Session = new(Id(1), 1);
        private static readonly ContractId CutId = Id(2);
        private static readonly ContractId ColumnId = Id(3);

        [Test]
        public void DelayedAndDuplicateResults_NeverRollbackTheLatestRequest()
        {
            var atomic = new AtomicCutResult<object>(Session, CutId, _ => new object());
            ContractId interaction = Id(10);
            Command firstCommand = Command(interaction, 1, 0);
            Command latestCommand = Command(interaction, 2, 0);
            Assert.That(atomic.ObserveRequest(firstCommand), Is.True);
            Assert.That(atomic.ObserveRequest(latestCommand), Is.True);

            Assert.That(atomic.TryPrepareAndCommit(Publication(interaction, 1, 1), out _), Is.EqualTo(CutResultApplyResult.Stale));
            Assert.That(atomic.TryPrepareAndCommit(Publication(interaction, 2, 2), out Exception error), Is.EqualTo(CutResultApplyResult.Committed), error?.ToString());
            Assert.That(atomic.TryPrepareAndCommit(Publication(interaction, 2, 2), out _), Is.EqualTo(CutResultApplyResult.Stale));
            Assert.That(atomic.TryRead(out CommittedCutResult<object> current), Is.True);
            Assert.That(current.Publication.Result.Sequence, Is.EqualTo(new InteractionSequence(2)));
        }

        [Test]
        public void NewInteractionSequenceOne_InvalidatesLateResultsFromPreviousGesture()
        {
            var atomic = new AtomicCutResult<object>(Session, CutId, _ => new object());
            ContractId previous = Id(10);
            ContractId current = Id(11);
            atomic.ObserveRequest(Command(previous, 10, 0));
            atomic.ObserveRequest(Command(current, 1, 0));

            Assert.That(atomic.TryPrepareAndCommit(Publication(previous, 10, 10), out _), Is.EqualTo(CutResultApplyResult.Stale));
            Assert.That(atomic.TryPrepareAndCommit(Publication(current, 1, 11), out _), Is.EqualTo(CutResultApplyResult.Committed));
        }

        [Test]
        public void LatestFailure_KeepsLastCanonicalResultVisible()
        {
            var atomic = new AtomicCutResult<object>(Session, CutId, _ => new object());
            ContractId interaction = Id(10);
            atomic.ObserveRequest(Command(interaction, 1, 0));
            atomic.TryPrepareAndCommit(Publication(interaction, 1, 1), out _);
            atomic.ObserveRequest(Command(interaction, 2, 1));

            Assert.That(atomic.MarkError(interaction, new InteractionSequence(2), new InvalidOperationException("remote failure")), Is.True);
            Assert.That(atomic.Feedback, Is.EqualTo(CutFeedbackState.Error));
            Assert.That(atomic.TryRead(out CommittedCutResult<object> current), Is.True);
            Assert.That(current.Publication.Result.Sequence, Is.EqualTo(new InteractionSequence(1)));
        }

        [Test]
        public void StablePlanPreload_IsAdmittedAndPlanMismatchIsRejected()
        {
            ContractId interaction = Id(10);
            CutRenderResult result = Result(interaction, 1, 1, 3);
            CutResultManifest manifest = new(new[] { ColumnId }, 2, 2);
            PreloadedDynamicTimeline matching = Timeline(3);

            Assert.DoesNotThrow(() => new CutResultPublication(Session, result, manifest, matching, new CutResourceBudget(Budget, Budget)));
            Assert.Throws<ArgumentException>(() => new CutResultPublication(Session, result, manifest, Timeline(4), new CutResourceBudget(Budget, Budget)));
        }

        [Test]
        public void StablePlanPreload_RejectsMissingOrUnexpectedColumns()
        {
            ContractId interaction = Id(10);
            ContractId missingColumn = Id(4);
            CutRenderResult firstColumn = Result(interaction, 1, 1, 3);
            var missingOverlay = new CutOverlayFrame(CutId, missingColumn, firstColumn.SourceStateRevision, 2, 2, firstColumn.Sample, TemporalApplication.SampleAndHold, new ScopeRevision(3), Pixels(2));
            var result = new CutRenderResult(firstColumn.CutId, firstColumn.InteractionId, firstColumn.Sequence, firstColumn.CutRevision, firstColumn.RenderRevision, firstColumn.SourceStateRevision, firstColumn.Sample, firstColumn.Plane, firstColumn.GeometryHash, firstColumn.Geometry, firstColumn.BaseTextureHash, firstColumn.BaseTexture, new[] { firstColumn.Overlays[0], missingOverlay });

            Assert.Throws<ArgumentException>(() => new CutResultPublication(Session, result, new CutResultManifest(new[] { ColumnId, missingColumn }, 2, 2), Timeline(3), new CutResourceBudget(Budget, Budget)));
        }

        [Test]
        public void BudgetRefusal_ReportsContributorsWithoutTruncation()
        {
            CutRenderResult result = Result(Id(10), 1, 1, 3);
            CutResourceAdmissionException exception = Assert.Throws<CutResourceAdmissionException>(() => new CutResultPublication(Session, result, new CutResultManifest(new[] { ColumnId }, 2, 2), Timeline(3), new CutResourceBudget(1, 1)));

            Assert.That(exception.Message, Does.Contain("No data was truncated or paged"));
            Assert.That(exception.Cost.CpuBytes, Is.GreaterThan(exception.Budget.MaximumCpuBytes));
        }

        [Test]
        public void Prefab_SerializesGizmoRendererAndXriReferences()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/HiBoPXR/Cuts/Prefabs/P12Cut.prefab");
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponentInChildren<P12CutGizmo>(true), Is.Not.Null);
            Assert.That(prefab.GetComponentInChildren<P12CutResultRenderer>(true), Is.Not.Null);
            Assert.That(prefab.GetComponentInChildren<XRGrabInteractable>(true), Is.Not.Null);
        }

        private static Command Command(ContractId interaction, ulong sequence, ulong baseRevision)
        {
            return CutCommands.Create(Session, Id(100 + sequence), Id(200 + sequence), new ScopeKey(ScopeType.Cut, CutId), new ScopeRevision(baseRevision), new CutPlaneIntent(new Plane3F(new Float3(0, 0, 1), -(float)sequence)), interaction, new InteractionSequence(sequence));
        }

        private static CutResultPublication Publication(ContractId interaction, ulong sequence, ulong renderRevision)
        {
            CutRenderResult result = Result(interaction, sequence, renderRevision, 3);
            return new CutResultPublication(Session, result, new CutResultManifest(new[] { ColumnId }, 2, 2), null, new CutResourceBudget(Budget, Budget));
        }

        private static CutRenderResult Result(ContractId interaction, ulong sequence, ulong renderRevision, ulong mappingRevision)
        {
            StateRevision state = new(5);
            RenderTemporalSample sample = new(0, 0.5f);
            CutGeometryAsset geometry = Geometry(Hash(20));
            TextureAsset texture = Texture(Hash(30));
            var overlay = new CutOverlayFrame(CutId, ColumnId, state, 2, 2, sample, TemporalApplication.SampleAndHold, new ScopeRevision(mappingRevision), Pixels((byte)sequence));
            return new CutRenderResult(CutId, interaction, new InteractionSequence(sequence), new ScopeRevision(sequence), new ScopeRevision(renderRevision), state, sample, new Plane3F(new Float3(0, 0, 1), -(float)sequence), geometry.Hash, Optional<CutGeometryAsset>.Some(geometry), texture.Hash, Optional<TextureAsset>.Some(texture), new[] { overlay });
        }

        private static PreloadedDynamicTimeline Timeline(ulong mappingRevision)
        {
            var builder = new PreloadedDynamicTimelineBuilder(Budget);
            for (int index = 0; index < 2; index++)
            {
                RenderTemporalSample sample = new(index, 0.5f);
                StateRevision state = new(5);
                var expectation = new DynamicColumnExpectation(ColumnId, DynamicColumnContent.None, new[] { CutId });
                var overlay = new CutOverlayFrame(CutId, ColumnId, state, 2, 2, sample, TemporalApplication.SampleAndHold, new ScopeRevision(mappingRevision), Pixels((byte)index));
                var column = new ColumnFrame(ColumnId, Hash(40), new ScopeRevision(1), Optional<SurfaceFrame>.None, Optional<SiteRenderFrame>.None, new[] { overlay });
                builder.AddFrame(new DynamicFrameBundle(Session, Id(50), new ScopeRevision((ulong)index + 1), (ulong)index + 1, index, sample, state, new[] { expectation }, new[] { column }));
            }

            return builder.Build();
        }

        private static CutGeometryAsset Geometry(AssetHash hash)
        {
            return new CutGeometryAsset(hash, CoordinateSpace.DesktopUnityMillimetersV1, new Bounds3F(new Float3(0, 0, 0), new Float3(1, 1, 0)), RenderBuffer<Float3>.TakeOwnership(new[] { new Float3(0, 0, 0), new Float3(1, 0, 0), new Float3(0, 1, 0) }), RenderBuffer<Float3>.TakeOwnership(new[] { new Float3(0, 0, 1), new Float3(0, 0, 1), new Float3(0, 0, 1) }), RenderBuffer<Float2>.TakeOwnership(new[] { new Float2(0, 0), new Float2(1, 0), new Float2(0, 1) }), RenderBuffer<uint>.TakeOwnership(new uint[] { 0, 1, 2 }));
        }

        private static TextureAsset Texture(AssetHash hash) => new(hash, 2, 2, TextureColorSpace.Srgb, Pixels(1));
        private static RenderBuffer<Rgba32> Pixels(byte value) => RenderBuffer<Rgba32>.TakeOwnership(new[] { new Rgba32(value, 0, 0, 255), new Rgba32(0, value, 0, 255), new Rgba32(0, 0, value, 255), new Rgba32(value, value, value, 255) });
        private static ContractId Id(ulong value) => new(value, value + 1);
        private static AssetHash Hash(ulong value) => new(value, value + 1, value + 2, value + 3);
    }
}
