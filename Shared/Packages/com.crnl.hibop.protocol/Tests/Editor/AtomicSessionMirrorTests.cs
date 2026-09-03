using System;
using System.Collections.Generic;
using System.Threading;
using CRNL.HiBoP.Contracts;
using NUnit.Framework;

namespace CRNL.HiBoP.Protocol.Tests
{
    public class AtomicSessionMirrorTests
    {
        [Test]
        public void AbandonedSnapshotNeverBecomesVisible()
        {
            AtomicSessionMirror mirror = new();
            SessionSnapshot before = SessionTestFixture.Snapshot(stateRevision: 1, opacity: 0.1);
            SessionSnapshot after = SessionTestFixture.Snapshot(stateRevision: 2, opacity: 0.9);
            mirror.PrepareSnapshot(before).Commit();

            AtomicSessionMirror.MirrorTransaction interrupted = mirror.PrepareSnapshot(after);

            Assert.That(mirror.Current, Is.SameAs(before));
            Assert.That(interrupted.Candidate, Is.SameAs(after));
            interrupted.Commit();
            Assert.That(mirror.Current, Is.SameAs(after));
        }

        [Test]
        public void InvalidDeltaBatchLeavesVisibleStateUntouched()
        {
            AtomicSessionMirror mirror = new();
            SessionSnapshot before = SessionTestFixture.Snapshot();
            mirror.PrepareSnapshot(before).Commit();
            ScopeDelta scope = new(SessionTestFixture.ColumnScope, new ScopeRevision(99), new ScopeRevision(100), new[]
            {
                PropertyChange.Set(V1PropertyKeys.ColumnActivityOpacity, ContractValue.FromNumber(0.8)),
            });
            StateDelta invalid = new(before.Session, before.StateRevision, before.StateRevision.Next(), new[] { scope }, Array.Empty<AssetChange>());

            Assert.Throws<InvalidOperationException>(() => mirror.PrepareDeltas(new[] { invalid }));
            Assert.That(mirror.Current, Is.SameAs(before));
            Assert.That(SessionTestFixture.Opacity(mirror.Current), Is.EqualTo(0.25));
        }

        [Test]
        public void OutOfOrderResumeBatchLeavesVisibleStateUntouched()
        {
            AtomicSessionMirror mirror = new();
            SessionSnapshot before = SessionTestFixture.Snapshot();
            mirror.PrepareSnapshot(before).Commit();
            ScopeDelta firstScope = new(SessionTestFixture.ColumnScope, new ScopeRevision(1), new ScopeRevision(2), new[]
            {
                PropertyChange.Set(V1PropertyKeys.ColumnActivityOpacity, ContractValue.FromNumber(0.5)),
            });
            StateDelta first = new(before.Session, new StateRevision(1), new StateRevision(2), new[] { firstScope }, Array.Empty<AssetChange>());
            ScopeDelta secondScope = new(SessionTestFixture.ColumnScope, new ScopeRevision(2), new ScopeRevision(3), new[]
            {
                PropertyChange.Set(V1PropertyKeys.ColumnActivityOpacity, ContractValue.FromNumber(0.8)),
            });
            StateDelta second = new(before.Session, new StateRevision(2), new StateRevision(3), new[] { secondScope }, Array.Empty<AssetChange>());

            Assert.Throws<InvalidOperationException>(() => mirror.PrepareDeltas(new[] { second, first }));
            Assert.That(mirror.Current, Is.SameAs(before));
        }

        [Test]
        public void ConcurrentReadersObserveOnlyWholeSnapshots()
        {
            AtomicSessionMirror mirror = new();
            mirror.PrepareSnapshot(SessionTestFixture.Snapshot(stateRevision: 1, opacity: 1)).Commit();
            int inconsistent = 0;
            bool stop = false;
            ManualResetEventSlim started = new(false);
            Thread reader = new(() =>
            {
                started.Set();
                while (!Volatile.Read(ref stop))
                {
                    SessionSnapshot snapshot = mirror.Current;
                    double expected = snapshot.StateRevision.Value % 2;
                    if (SessionTestFixture.Opacity(snapshot) != expected)
                        Interlocked.Increment(ref inconsistent);
                }
            });
            reader.Start();
            Assert.That(started.Wait(1000), Is.True);

            for (ulong revision = 2; revision <= 10_000; revision++)
                mirror.PrepareSnapshot(SessionTestFixture.Snapshot(stateRevision: revision, opacity: revision % 2)).Commit();

            Volatile.Write(ref stop, true);
            Assert.That(reader.Join(2000), Is.True);
            Assert.That(inconsistent, Is.Zero);
        }

        [Test]
        public void StalePreparedTransactionCannotOverwriteNewerCommit()
        {
            AtomicSessionMirror mirror = new();
            mirror.PrepareSnapshot(SessionTestFixture.Snapshot(stateRevision: 1)).Commit();
            AtomicSessionMirror.MirrorTransaction stale = mirror.PrepareSnapshot(SessionTestFixture.Snapshot(stateRevision: 2));
            mirror.PrepareSnapshot(SessionTestFixture.Snapshot(stateRevision: 3)).Commit();

            Assert.Throws<InvalidOperationException>(() => stale.Commit());
            Assert.That(mirror.Current.StateRevision, Is.EqualTo(new StateRevision(3)));
        }

        [Test]
        public void OversizedSyntheticSnapshotIsRejectedBeforeVisibility()
        {
            ManualClock clock = new();
            SessionEpoch session = new(new ContractId(1, 1), 1);
            ScopeState huge = new(SessionTestFixture.ColumnScope, new ScopeRevision(1), new[]
            {
                new StateProperty(V1PropertyKeys.ColumnThresholds, ContractValue.FromNumbers(new double[9000])),
            });
            SessionSnapshot snapshot = new(ContractVersion.V1, session, new StateRevision(1), new[] { huge }, Array.Empty<AssetReference>());
            SyntheticSessionHost host = SessionTestFixture.Host(clock, snapshot);
            PairingResult pairing = host.Pair(SessionTestFixture.ClientId, SessionTestFixture.Sas, true);
            ServerHello hello = host.Handshake(SessionTestFixture.ClientId, pairing.Token.Value, SessionTestFixture.Hello());

            Assert.That(hello.Accepted, Is.True);
            Assert.Throws<ArgumentOutOfRangeException>(() => host.CaptureSnapshot(SessionTestFixture.ClientId, pairing.Token.Value));
            Assert.That(host.State, Is.EqualTo(HostSessionState.Synchronizing));
        }
    }
}
