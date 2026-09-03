using System;
using System.Linq;
using System.Threading;
using CRNL.HiBoP.Contracts;
using NUnit.Framework;

namespace CRNL.HiBoP.Protocol.Tests
{
    public class CommandAndResumeTests
    {
        [Test]
        public void LostOutcomeReplayHasOneEffectAndSameLogicalOutcome()
        {
            SyntheticSessionHost host = SessionTestFixture.Host();
            SyntheticSessionClient client = SessionTestFixture.ConnectedClient(host);
            Command command = SessionTestFixture.OpacityCommand(client.Mirror.Current, new ContractId(40, 1), new ContractId(50, 1), 0.75);
            SequencedCommand request = client.PrepareCommand(command);

            CommandExecutionResult lost = host.Execute(SessionTestFixture.ClientId, Token(), request);
            CommandExecutionResult replay = client.Send(request);

            Assert.That(lost.Outcome, Is.EqualTo(replay.Outcome));
            Assert.That(replay.Replayed, Is.True);
            Assert.That(host.AppliedCommandCount, Is.EqualTo(1));
            Assert.That(host.CurrentSnapshot.StateRevision, Is.EqualTo(new StateRevision(2)));
            Assert.That(client.Mirror.Current.StateRevision, Is.EqualTo(new StateRevision(1)), "A replayed outcome must not invent the lost delta.");

            client.Disconnect();
            ResumeResponse resumed = client.Resume();
            Assert.That(resumed.Decision, Is.EqualTo(ResumeDecision.ResumeWithDeltas));
            Assert.That(client.Mirror.Current.StateRevision, Is.EqualTo(new StateRevision(2)));
            Assert.That(SessionTestFixture.Opacity(client.Mirror.Current), Is.EqualTo(0.75));
        }

        [Test]
        public void ConcurrentDuplicateCommandCommitsOnce()
        {
            SyntheticSessionHost host = SessionTestFixture.Host();
            _ = SessionTestFixture.ConnectedClient(host);
            Command command = SessionTestFixture.OpacityCommand(host.CurrentSnapshot, new ContractId(40, 2), new ContractId(50, 2), 0.5);
            SequencedCommand request = new(1, command);
            CommandExecutionResult first = null;
            CommandExecutionResult second = null;
            Thread left = new(() => first = host.Execute(SessionTestFixture.ClientId, Token(), request));
            Thread right = new(() => second = host.Execute(SessionTestFixture.ClientId, Token(), request));

            left.Start();
            right.Start();
            Assert.That(left.Join(2000), Is.True);
            Assert.That(right.Join(2000), Is.True);

            Assert.That(host.AppliedCommandCount, Is.EqualTo(1));
            Assert.That(new[] { first.Replayed, second.Replayed }, Is.EquivalentTo(new[] { false, true }));
            Assert.That(first.Outcome, Is.EqualTo(second.Outcome));
        }

        [Test]
        public void ReusingCommandIdWithNewSequenceIsRejectedWithoutEffect()
        {
            SyntheticSessionHost host = SessionTestFixture.Host();
            _ = SessionTestFixture.ConnectedClient(host);
            ContractId commandId = new(40, 20);
            Command firstCommand = SessionTestFixture.OpacityCommand(host.CurrentSnapshot, commandId, new ContractId(50, 20), 0.5);
            Assert.That(host.Execute(SessionTestFixture.ClientId, Token(), new SequencedCommand(1, firstCommand)).Outcome.Accepted, Is.True);

            Command reusedId = SessionTestFixture.OpacityCommand(host.CurrentSnapshot, commandId, new ContractId(50, 21), 0.8);
            CommandExecutionResult result = host.Execute(SessionTestFixture.ClientId, Token(), new SequencedCommand(2, reusedId));

            Assert.That(result.Outcome.Accepted, Is.False);
            Assert.That(result.Outcome.Error.Value.Code, Is.EqualTo(ErrorCode.CommandInvalid));
            Assert.That(host.AppliedCommandCount, Is.EqualTo(1));
            Assert.That(SessionTestFixture.Opacity(host.CurrentSnapshot), Is.EqualTo(0.5));
        }

        [Test]
        public void ConflictIsRecordedWithoutMutationThenClientResynchronizes()
        {
            SyntheticSessionHost host = SessionTestFixture.Host();
            SyntheticSessionClient client = SessionTestFixture.ConnectedClient(host);
            host.ApplyAuthoritativeChange(SessionTestFixture.ColumnScope, V1PropertyKeys.ColumnActivityOpacity, ContractValue.FromNumber(0.6));
            Command command = SessionTestFixture.OpacityCommand(client.Mirror.Current, new ContractId(40, 3), new ContractId(50, 3), 0.8);
            SequencedCommand request = client.PrepareCommand(command);

            CommandExecutionResult conflict = client.Send(request);

            Assert.That(conflict.Outcome.Error.Value.Code, Is.EqualTo(ErrorCode.StateConflict));
            Assert.That(conflict.Outcome.Error.Value.Retryable, Is.True);
            Assert.That(host.AppliedCommandCount, Is.Zero);
            Assert.That(host.CurrentSnapshot.StateRevision, Is.EqualTo(new StateRevision(2)));
            Assert.That(client.Mirror.Current.StateRevision, Is.EqualTo(new StateRevision(2)));
            Assert.That(client.UserMessage, Is.EqualTo(SyntheticSessionClient.ConflictMessage));

            CommandExecutionResult duplicate = host.Execute(SessionTestFixture.ClientId, Token(), request);
            Assert.That(duplicate.Replayed, Is.True);
            Assert.That(duplicate.Outcome, Is.EqualTo(conflict.Outcome));
            Assert.That(host.CurrentSnapshot.StateRevision, Is.EqualTo(new StateRevision(2)));
        }

        [Test]
        public void SequenceGapAndExpiredReplayNeverExecute()
        {
            ManualClock clock = new();
            SyntheticSessionHost host = SessionTestFixture.Host(clock);
            _ = SessionTestFixture.ConnectedClient(host);
            Command firstCommand = SessionTestFixture.OpacityCommand(host.CurrentSnapshot, new ContractId(40, 4), new ContractId(50, 4), 0.4);

            CommandExecutionResult gap = host.Execute(SessionTestFixture.ClientId, Token(), new SequencedCommand(2, firstCommand));
            Assert.That(gap.Outcome.Error.Value.Code, Is.EqualTo(ErrorCode.CommandInvalid));
            Assert.That(gap.Outcome.Error.Value.Retryable, Is.True);
            Assert.That(host.AppliedCommandCount, Is.Zero);

            SequencedCommand first = new(1, firstCommand);
            Assert.That(host.Execute(SessionTestFixture.ClientId, Token(), first).Outcome.Accepted, Is.True);
            clock.Advance(15 * 60 * 1000);
            CommandExecutionResult expired = host.Execute(SessionTestFixture.ClientId, Token(), first);

            Assert.That(expired.Outcome.Accepted, Is.False);
            Assert.That(expired.Outcome.Error.Value.Code, Is.EqualTo(ErrorCode.CommandInvalid));
            Assert.That(expired.Outcome.Error.Value.Retryable, Is.False);
            Assert.That(host.AppliedCommandCount, Is.EqualTo(1));
        }

        [Test]
        public void ResumeUsesContiguousDeltasThenFallsBackAfterEviction()
        {
            SyntheticSessionHost deltaHost = SessionTestFixture.Host();
            SyntheticSessionClient deltaClient = SessionTestFixture.ConnectedClient(deltaHost);
            deltaClient.Disconnect();
            deltaHost.ApplyAuthoritativeChange(SessionTestFixture.ColumnScope, V1PropertyKeys.ColumnActivityOpacity, ContractValue.FromNumber(0.7));

            ResumeResponse deltaResume = deltaClient.Resume();
            Assert.That(deltaResume.Decision, Is.EqualTo(ResumeDecision.ResumeWithDeltas));
            Assert.That(deltaResume.Deltas, Has.Count.EqualTo(1));

            SyntheticSessionHost snapshotHost = SessionTestFixture.Host();
            SyntheticSessionClient snapshotClient = SessionTestFixture.ConnectedClient(snapshotHost);
            for (int index = 0; index < 513; index++)
            {
                snapshotHost.ApplyAuthoritativeChange(SessionTestFixture.ColumnScope, V1PropertyKeys.ColumnActivityOpacity, ContractValue.FromNumber((index % 100) / 100d));
            }

            snapshotClient.Disconnect();

            ResumeResponse snapshotResume = snapshotClient.Resume();
            Assert.That(snapshotResume.Decision, Is.EqualTo(ResumeDecision.FullSnapshotRequired));
            Assert.That(snapshotClient.Mirror.Current.StateRevision, Is.EqualTo(snapshotHost.CurrentSnapshot.StateRevision));
        }

        [TestCase(1_000)]
        [TestCase(5_000)]
        public void ShortCutUsesSameEpochResume(long outageMilliseconds)
        {
            ManualClock clock = new();
            SyntheticSessionHost host = SessionTestFixture.Host(clock);
            SyntheticSessionClient client = SessionTestFixture.ConnectedClient(host);
            client.Disconnect();
            clock.Advance(outageMilliseconds);

            ResumeResponse response = client.Resume();

            Assert.That(response.Decision, Is.EqualTo(ResumeDecision.ResumeWithDeltas));
            Assert.That(response.Deltas, Is.Empty);
            Assert.That(client.State, Is.EqualTo(ClientSessionState.Active));
        }

        [Test]
        public void NominalResumeP95IsBelowFiveSeconds()
        {
            SyntheticSessionHost host = SessionTestFixture.Host();
            SyntheticSessionClient client = SessionTestFixture.ConnectedClient(host);
            double[] samples = new double[200];
            for (int index = 0; index < samples.Length; index++)
            {
                client.Disconnect();
                long startedAt = System.Diagnostics.Stopwatch.GetTimestamp();
                client.Resume();
                samples[index] = (System.Diagnostics.Stopwatch.GetTimestamp() - startedAt) * 1_000d / System.Diagnostics.Stopwatch.Frequency;
            }

            Array.Sort(samples);
            double p95 = samples[(int)Math.Ceiling(samples.Length * 0.95) - 1];
            Assert.That(p95, Is.LessThanOrEqualTo(5_000));
        }

        [Test]
        public void ThirtySecondCutExpiresLeaseAndRequiresFreshPairing()
        {
            ManualClock clock = new();
            SyntheticSessionHost host = SessionTestFixture.Host(clock);
            SyntheticSessionClient client = SessionTestFixture.ConnectedClient(host);
            client.Disconnect();
            clock.Advance(30_000);

            Assert.Throws<InvalidOperationException>(() => client.Resume());
            SyntheticSessionClient replacement = new(host, new ContractId(13, 13), SessionTestFixture.Hello(nonce: new ContractId(203, 1)));
            Assert.That(replacement.PairAndConnect(SessionTestFixture.Sas)?.Accepted, Is.True);
            Assert.That(replacement.Mirror.Current.Session, Is.EqualTo(host.CurrentSnapshot.Session));
        }

        [Test]
        public void InvalidResumeMetadataForcesFullSnapshot()
        {
            SyntheticSessionHost host = SessionTestFixture.Host();
            _ = SessionTestFixture.ConnectedClient(host);
            host.Suspend(SessionTestFixture.ClientId);
            var wrongScopes = ResumeRequest.FromSnapshot(SessionTestFixture.Snapshot(stateRevision: 1, opacity: 0.1)).ScopeRevisions.ToDictionary(pair => pair.Key, pair => pair.Value);
            wrongScopes[SessionTestFixture.ColumnScope] = new ScopeRevision(99);
            ResumeRequest request = new(SessionTestFixture.Snapshot().Session, new StateRevision(1), wrongScopes, Array.Empty<AssetHash>());

            ResumeResponse response = host.Resume(SessionTestFixture.ClientId, Token(), request);

            Assert.That(response.Decision, Is.EqualTo(ResumeDecision.FullSnapshotRequired));
        }

        [Test]
        public void ReplacementPurgesOldClientMirrorAndAllowsNewPairing()
        {
            SyntheticSessionHost host = SessionTestFixture.Host();
            SyntheticSessionClient oldClient = SessionTestFixture.ConnectedClient(host);
            SessionSnapshot replacement = SessionTestFixture.Snapshot(epoch: 2, stateRevision: 1, opacity: 0.9);

            host.ReplaceSession(replacement, SessionTestFixture.Sas, SessionTestFixture.TokenFactory);
            oldClient.HandleSessionReplaced();
            SyntheticSessionClient newClient = SessionTestFixture.ConnectedClient(host, new ContractId(12, 12));

            Assert.That(oldClient.Mirror.HasState, Is.False);
            Assert.That(oldClient.State, Is.EqualTo(ClientSessionState.Refused));
            Assert.That(newClient.Mirror.Current.Session, Is.EqualTo(replacement.Session));
            Assert.That(SessionTestFixture.Opacity(newClient.Mirror.Current), Is.EqualTo(0.9));
        }

        [Test]
        public void DiagnosticsAreBoundedAndRedacted()
        {
            ManualClock clock = new();
            SessionDiagnostics diagnostics = new(clock);
            ContractId correlation = new(77, 77);
            for (int index = 0; index < SessionDiagnostics.Capacity + 10; index++)
                diagnostics.Record(DiagnosticEventCode.CommandRejected, Optional<ContractId>.Some(correlation), Optional<ErrorCode>.Some(ErrorCode.CommandInvalid));

            string joined = string.Join("\n", diagnostics.Snapshot().Select(entry => entry.ToString()));
            Assert.That(diagnostics.Snapshot(), Has.Count.EqualTo(SessionDiagnostics.Capacity));
            Assert.That(joined, Does.Not.Contain(SessionTestFixture.Sas));
            Assert.That(joined, Does.Not.Contain("0.25"));
            Assert.That(joined, Does.Not.Contain("token"));

            SyntheticSessionHost host = SessionTestFixture.Host();
            _ = SessionTestFixture.ConnectedClient(host);
            string summary = host.GetDiagnosticSummary().ToString();
            Assert.That(summary, Does.Contain("deltaDepth="));
            Assert.That(summary, Does.Not.Contain(SessionTestFixture.Sas));
        }

        private static PairingToken Token() => new(SessionTestFixture.TokenFactory());
    }
}
