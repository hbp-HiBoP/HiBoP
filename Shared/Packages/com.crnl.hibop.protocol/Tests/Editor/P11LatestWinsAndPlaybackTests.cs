using System.Threading.Tasks;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.RenderModel;
using NUnit.Framework;

namespace CRNL.HiBoP.Protocol.Tests
{
    public class P11LatestWinsAndPlaybackTests
    {
        [Test]
        public async Task Scheduler_KeepsOneActiveAndOnlyTheLatestPending()
        {
            var firstGate = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var thirdGate = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var scheduler = new LatestWinsScheduler<int, int, int>((request, token) => request == 1 ? firstGate.Task : thirdGate.Task);

            Task<LatestWinsOutcome<int>> first = scheduler.EnqueueAsync(7, 1, 1);
            Task<LatestWinsOutcome<int>> second = scheduler.EnqueueAsync(7, 2, 2);
            Task<LatestWinsOutcome<int>> third = scheduler.EnqueueAsync(7, 3, 3);

            scheduler.GetDepth(7, out int active, out int pending);
            Assert.That(active, Is.EqualTo(1));
            Assert.That(pending, Is.EqualTo(1));
            Assert.That((await second).Kind, Is.EqualTo(LatestWinsOutcomeKind.Superseded));

            firstGate.SetResult(1);
            Assert.That((await first).Kind, Is.EqualTo(LatestWinsOutcomeKind.Superseded));
            thirdGate.SetResult(3);
            LatestWinsOutcome<int> latest = await third;
            Assert.That(latest.Kind, Is.EqualTo(LatestWinsOutcomeKind.Completed));
            Assert.That(latest.Result, Is.EqualTo(3));
            Assert.That(scheduler.SupersededCount, Is.EqualTo(2));
            Assert.That(scheduler.CompletedCount, Is.EqualTo(1));
        }

        [Test]
        public async Task Scheduler_LogicalAutoplayAndScrubRunsNeverGrowBacklog()
        {
            await AssertCoalesces(6_000); // ten logical minutes at 10 source updates/s
            await AssertCoalesces(3_600); // sixty logical seconds at 60 scrub intents/s
        }

        [Test]
        public void AtomicMirror_RejectsDelayedFrameWithoutRollback()
        {
            DynamicFrameBundle first = P11DynamicFrameCodecTests.Bundle(1, 3, 10);
            DynamicFrameBundle delayed = P11DynamicFrameCodecTests.Bundle(1, 3, 9);
            var mirror = new AtomicDynamicFrameMirror(first.Session, first.TimelineId);

            Assert.That(mirror.TryCommit(first), Is.EqualTo(DynamicFrameCommitResult.Committed));
            Assert.That(mirror.TryCommit(delayed), Is.EqualTo(DynamicFrameCommitResult.Stale));
            Assert.That(mirror.TryRead(out DynamicFrameBundle current), Is.True);
            Assert.That(current.FrameSequence, Is.EqualTo(10));
        }

        [TestCase(TimelinePlaybackAction.Play)]
        [TestCase(TimelinePlaybackAction.Pause)]
        [TestCase(TimelinePlaybackAction.Scrub)]
        public void PlaybackCommand_RoundTripsDesktopOwnedIntent(TimelinePlaybackAction action)
        {
            SessionEpoch session = new(P11DynamicFrameCodecTests.Id(1), 1);
            ScopeKey timeline = new(ScopeType.Timeline, P11DynamicFrameCodecTests.Id(2));
            TimelinePlaybackIntent intent = new(action, 12.5, 1.25);
            Command command = TimelinePlaybackCommands.Create(session, P11DynamicFrameCodecTests.Id(3), P11DynamicFrameCodecTests.Id(4), timeline, new ScopeRevision(5), intent, P11DynamicFrameCodecTests.Id(6), new InteractionSequence(7));

            Assert.That(TimelinePlaybackCommands.TryRead(command, out TimelinePlaybackIntent decoded), Is.True);
            Assert.That(decoded.Action, Is.EqualTo(action));
            Assert.That(decoded.LogicalTime, Is.EqualTo(12.5));
            Assert.That(decoded.Speed, Is.EqualTo(1.25));
            Assert.That(command.Scope.Owner, Is.EqualTo(ScopeOwner.Desktop));
        }

        private static async Task AssertCoalesces(int requestCount)
        {
            var firstGate = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            int invocation = 0;
            var scheduler = new LatestWinsScheduler<int, int, int>((request, token) =>
            {
                invocation++;
                return invocation == 1 ? firstGate.Task : Task.FromResult(request);
            });

            Task<LatestWinsOutcome<int>> first = scheduler.EnqueueAsync(1, 1, 1);
            Task<LatestWinsOutcome<int>> latest = null;
            for (ulong sequence = 2; sequence <= (ulong)requestCount; sequence++)
                latest = scheduler.EnqueueAsync(1, sequence, (int)sequence);
            scheduler.GetDepth(1, out int active, out int pending);
            Assert.That(active, Is.EqualTo(1));
            Assert.That(pending, Is.EqualTo(1));

            firstGate.SetResult(1);
            Assert.That((await first).Kind, Is.EqualTo(LatestWinsOutcomeKind.Superseded));
            Assert.That((await latest).Kind, Is.EqualTo(LatestWinsOutcomeKind.Completed));
            Assert.That(scheduler.SupersededCount, Is.EqualTo(requestCount - 1));
            Assert.That(scheduler.CompletedCount, Is.EqualTo(1));
            Assert.That(scheduler.ScopeCount, Is.EqualTo(0));
        }
    }
}
