using System;
using System.Threading;
using System.Threading.Tasks;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.Protocol;
using CRNL.HiBoP.RenderModel;
using HBP.RenderModelAdapters;
using NUnit.Framework;

namespace HBP.Tests.Serialization
{
    public class P12DesktopCutSchedulerTests
    {
        private const long BudgetBytes = 1024 * 1024;
        private static readonly SessionEpoch Session = new(Id(1), 1);
        private static readonly ContractId CutId = Id(2);
        private static readonly ContractId ColumnId = Id(3);

        [Test]
        public async Task Scheduler_CoalescesAndAcceptsSequenceRestartForANewInteraction()
        {
            var firstStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseFirst = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            int calls = 0;
            using var scheduler = new DesktopCutComputationScheduler(async (request, _) =>
            {
                if (Interlocked.Increment(ref calls) == 1)
                {
                    firstStarted.SetResult(true);
                    await releaseFirst.Task;
                }

                return new DesktopCutComputation(Result(request.Command));
            });

            ContractId firstInteraction = Id(10);
            Task<LatestWinsOutcome<CutResultPublication>> first = scheduler.EnqueueAsync(Request(firstInteraction, 1));
            await firstStarted.Task;
            Task<LatestWinsOutcome<CutResultPublication>> middle = scheduler.EnqueueAsync(Request(firstInteraction, 2));
            Task<LatestWinsOutcome<CutResultPublication>> latest = scheduler.EnqueueAsync(Request(firstInteraction, 3));
            releaseFirst.SetResult(true);

            Assert.That((await first).Kind, Is.EqualTo(LatestWinsOutcomeKind.Superseded));
            Assert.That((await middle).Kind, Is.EqualTo(LatestWinsOutcomeKind.Superseded));
            Assert.That((await latest).Kind, Is.EqualTo(LatestWinsOutcomeKind.Completed));
            Assert.That(calls, Is.EqualTo(2));

            ContractId nextInteraction = Id(11);
            LatestWinsOutcome<CutResultPublication> next = await scheduler.EnqueueAsync(Request(nextInteraction, 1));
            Assert.That(next.Kind, Is.EqualTo(LatestWinsOutcomeKind.Completed));
            Assert.That(next.Result.Result.Sequence, Is.EqualTo(new InteractionSequence(1)));
            Assert.Throws<ArgumentException>(() => scheduler.EnqueueAsync(Request(firstInteraction, 4)));
        }

        [Test]
        public async Task Scheduler_DoesNotPublishDesktopResultAfterItWasSuperseded()
        {
            var firstStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseFirst = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            int calls = 0;
            using var scheduler = new DesktopCutComputationScheduler(async (request, _) =>
            {
                if (Interlocked.Increment(ref calls) == 1)
                {
                    firstStarted.SetResult(true);
                    await releaseFirst.Task;
                }

                return new DesktopCutComputation(Result(request.Command));
            });

            Task<LatestWinsOutcome<CutResultPublication>> first = scheduler.EnqueueAsync(Request(Id(10), 1));
            await firstStarted.Task;
            Task<LatestWinsOutcome<CutResultPublication>> latest = scheduler.EnqueueAsync(Request(Id(10), 2));
            releaseFirst.SetResult(true);

            Assert.That((await first).Result, Is.Null);
            Assert.That((await latest).Result.Result.Sequence, Is.EqualTo(new InteractionSequence(2)));
            Assert.That(scheduler.CompletedCount, Is.EqualTo(1));
        }

        private static DesktopCutComputationRequest Request(ContractId interactionId, ulong sequence)
        {
            Command command = CutCommands.Create(Session, Id(100 + sequence), Id(200 + sequence), new ScopeKey(ScopeType.Cut, CutId), new ScopeRevision(sequence), new CutPlaneIntent(new Plane3F(new Float3(0, 0, 1), -(float)sequence)), interactionId, new InteractionSequence(sequence));
            return new DesktopCutComputationRequest(command, new CutResultManifest(new[] { ColumnId }, 1, 1), new CutResourceBudget(BudgetBytes, BudgetBytes));
        }

        private static CutRenderResult Result(Command command)
        {
            CutCommands.TryRead(command, out CutPlaneIntent intent);
            var state = new StateRevision(1);
            var sample = new RenderTemporalSample(0, 0);
            var overlay = new CutOverlayFrame(CutId, ColumnId, state, 1, 1, sample, TemporalApplication.SampleAndHold, new ScopeRevision(1), RenderBuffer<Rgba32>.TakeOwnership(new[] { new Rgba32(1, 2, 3, 255) }));
            return new CutRenderResult(CutId, command.InteractionId.Value, command.Sequence.Value, new ScopeRevision(command.BaseScopeRevision.Value + 1), new ScopeRevision(command.Sequence.Value.Value), state, sample, intent.Plane, Hash(10), Optional<CutGeometryAsset>.None, Hash(20), Optional<TextureAsset>.None, new[] { overlay });
        }

        private static ContractId Id(ulong value) => new(value, value + 1);
        private static AssetHash Hash(ulong value) => new(value, value + 1, value + 2, value + 3);
    }
}
