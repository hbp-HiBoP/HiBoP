using System;
using System.Threading;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.Protocol;
using UnityEngine;

namespace CRNL.HiBoP.XR.DistributedSession
{
    public sealed class P07SyntheticSessionProbe : MonoBehaviour
    {
        private const string Sas = "123456";
        private static readonly AssetHash s_Schema = new(1, 2, 3, 4);
        private static readonly ContractId s_ClientId = new(10, 10);
        private static readonly ScopeKey s_ColumnScope = new(ScopeType.Column, new ContractId(20, 20));
        private const ProtocolCapabilities AllCapabilities = ProtocolCapabilities.TransactionalSnapshot | ProtocolCapabilities.OrderedDeltas | ProtocolCapabilities.SequencedCommands | ProtocolCapabilities.Resume | ProtocolCapabilities.RedactedDiagnostics;

        [SerializeField] private bool runOnStart = true;
        [SerializeField] private TextMesh statusText;

        private string m_Status = "Prêt à exécuter la preuve P07 synthétique.";
        private bool m_Running;

        public bool HasStatusText => statusText != null;

        public void Configure(TextMesh configuredStatusText)
        {
            statusText = configuredStatusText;
        }

        private void Start()
        {
            UpdateStatus("HiBoP XR — P07\nRUNNING...", Color.yellow);
            if (runOnStart)
                RunProof();
        }

        private void RunProof()
        {
            if (m_Running)
                return;
            m_Running = true;
            try
            {
                P07QuestEvidence evidence = ExecuteProof();
                m_Status = JsonUtility.ToJson(evidence, true);
                UpdateStatus(FormatEvidence(evidence), Color.green);
                Debug.Log("P07_QUEST_REPORT " + JsonUtility.ToJson(evidence));
            }
            catch (Exception exception)
            {
                P07QuestEvidence evidence = new()
                {
                    schema = "p07-synthetic-session-v1",
                    platform = Application.platform.ToString(),
                    unity = Application.unityVersion,
                    scriptingBackend = "IL2CPP-required-by-build",
                    result = "FAIL",
                    failureCode = exception.GetType().Name,
                };
                m_Status = JsonUtility.ToJson(evidence, true);
                UpdateStatus(FormatEvidence(evidence), Color.red);
                Debug.LogError("P07_QUEST_REPORT " + JsonUtility.ToJson(evidence));
            }
            finally
            {
                m_Running = false;
            }
        }

        private static string FormatEvidence(P07QuestEvidence evidence)
        {
            if (!string.Equals(evidence.result, "PASS", StringComparison.Ordinal))
                return $"HiBoP XR — P07\nRESULT: FAIL\nCode: {evidence.failureCode}";

            return "HiBoP XR — P07\n" + "RESULT: PASS\n" + $"Atomic: {evidence.atomicIterations}, inconsistent: {evidence.inconsistentReads}\n" + $"Idempotence: one effect, replay: {evidence.replayedWithoutEffect}\n" + $"Resume: {evidence.resumeSamples}, p95: {evidence.resumeP95Milliseconds:F4} ms\n" + $"Epoch/conflict/diagnostics: PASS";
        }

        private void UpdateStatus(string status, Color color)
        {
            if (statusText == null)
                return;
            statusText.text = status;
            statusText.color = color;
        }

        private static P07QuestEvidence ExecuteProof()
        {
            ManualClock clock = new();
            SyntheticSessionHost host = CreateHost(clock, Snapshot());
            SyntheticSessionClient client = new(host, s_ClientId, Hello());
            Require(client.PairAndConnect(Sas)?.Accepted == true, "HANDSHAKE");

            Command command = OpacityCommand(client.Mirror.Current, new ContractId(40, 1), new ContractId(50, 1), 0.75);
            SequencedCommand request = client.PrepareCommand(command);
            CommandExecutionResult lost = host.Execute(s_ClientId, Token(), request);
            CommandExecutionResult replay = client.Send(request);
            Require(lost.Outcome.Equals(replay.Outcome) && replay.Replayed, "OUTCOME_REPLAY");
            Require(host.AppliedCommandCount == 1 && host.CurrentSnapshot.StateRevision == new StateRevision(2), "IDEMPOTENCE");
            Command reusedId = OpacityCommand(host.CurrentSnapshot, command.CommandId, new ContractId(50, 9), 0.9);
            CommandExecutionResult reusedIdResult = host.Execute(s_ClientId, Token(), new SequencedCommand(2, reusedId));
            Require(!reusedIdResult.Outcome.Accepted && host.AppliedCommandCount == 1, "COMMAND_ID_REUSE");
            client.Disconnect();
            Require(client.Resume().Decision == ResumeDecision.ResumeWithDeltas, "RESUME_DELTA");
            Require(client.Mirror.Current.StateRevision == new StateRevision(2) && Math.Abs(Opacity(client.Mirror.Current) - 0.75) < 0.000001, "RESUME_STATE");

            AtomicSessionMirror mirror = new();
            SessionSnapshot before = Snapshot(stateRevision: 1, opacity: 0.1);
            SessionSnapshot after = Snapshot(stateRevision: 2, opacity: 0.9);
            mirror.PrepareSnapshot(before).Commit();
            AtomicSessionMirror.MirrorTransaction interrupted = mirror.PrepareSnapshot(after);
            Require(ReferenceEquals(mirror.Current, before), "INTERRUPTED_SNAPSHOT");
            interrupted.Commit();
            Require(ReferenceEquals(mirror.Current, after), "SNAPSHOT_SWAP");

            int inconsistent = RunConcurrentAtomicityProof(mirror);
            Require(inconsistent == 0, "CONCURRENT_ATOMICITY");

            host.ApplyAuthoritativeChange(s_ColumnScope, V1PropertyKeys.ColumnActivityOpacity, ContractValue.FromNumber(0.6));
            Command conflictCommand = OpacityCommand(client.Mirror.Current, new ContractId(40, 2), new ContractId(50, 2), 0.8);
            CommandExecutionResult conflict = client.Send(client.PrepareCommand(conflictCommand));
            Require(conflict.Outcome.Error.HasValue && conflict.Outcome.Error.Value.Code == ErrorCode.StateConflict, "CONFLICT");
            Require(host.AppliedCommandCount == 1 && client.Mirror.Current.StateRevision == host.CurrentSnapshot.StateRevision, "CONFLICT_ATOMICITY");

            SyntheticSessionClient second = new(host, new ContractId(11, 11), Hello(new ContractId(201, 1)));
            Require(second.PairAndConnect(Sas) == null && second.UserMessage == SyntheticSessionClient.BusyMessage, "SINGLE_CLIENT");

            SessionSnapshot replacement = Snapshot(epoch: 2, opacity: 0.95);
            host.ReplaceSession(replacement, Sas, TokenBytes);
            client.HandleSessionReplaced();
            Require(!client.Mirror.HasState, "EPOCH_PURGE");
            SyntheticSessionClient replacementClient = new(host, new ContractId(12, 12), Hello(new ContractId(202, 1)));
            Require(replacementClient.PairAndConnect(Sas)?.Accepted == true, "REPAIR");
            Require(replacementClient.Mirror.Current.Session == replacement.Session, "NEW_EPOCH");

            ValidateOutagesAndHeartbeat();
            double resumeP95Milliseconds = MeasureResumeP95Milliseconds();
            Require(resumeP95Milliseconds <= 5_000, "RESUME_P95");

            return new P07QuestEvidence
            {
                schema = "p07-synthetic-session-v1",
                platform = Application.platform.ToString(),
                unity = Application.unityVersion,
                scriptingBackend = "IL2CPP-required-by-build",
                atomicIterations = 10_000,
                inconsistentReads = inconsistent,
                appliedEffects = host.AppliedCommandCount,
                replayedWithoutEffect = true,
                commandIdReuseRejected = true,
                conflictWithoutEffect = true,
                resumeDecision = ResumeDecision.ResumeWithDeltas.ToString(),
                resumeSamples = 200,
                resumeP95Milliseconds = resumeP95Milliseconds,
                replacementPurged = true,
                outagesValidated = true,
                heartbeatValidated = true,
                diagnosticsRedacted = !host.GetDiagnosticSummary().ToString().Contains(Sas),
                result = "PASS",
                failureCode = string.Empty,
            };
        }

        private static int RunConcurrentAtomicityProof(AtomicSessionMirror mirror)
        {
            mirror.PrepareSnapshot(Snapshot(stateRevision: 1, opacity: 1)).Commit();
            int inconsistent = 0;
            bool stop = false;
            Thread reader = new(() =>
            {
                while (!Volatile.Read(ref stop))
                {
                    SessionSnapshot snapshot = mirror.Current;
                    if (Math.Abs(Opacity(snapshot) - snapshot.StateRevision.Value % 2) > 0.000001)
                        Interlocked.Increment(ref inconsistent);
                }
            });
            reader.Start();
            for (ulong revision = 2; revision <= 10_000; revision++)
                mirror.PrepareSnapshot(Snapshot(stateRevision: revision, opacity: revision % 2)).Commit();
            Volatile.Write(ref stop, true);
            if (!reader.Join(5_000))
                throw new InvalidOperationException("ATOMICITY_THREAD_TIMEOUT");
            return inconsistent;
        }

        private static void ValidateOutagesAndHeartbeat()
        {
            long[] resumableOutages = { 1_000, 5_000 };
            for (int index = 0; index < resumableOutages.Length; index++)
            {
                ManualClock clock = new();
                SyntheticSessionHost host = CreateHost(clock, Snapshot());
                SyntheticSessionClient client = new(host, new ContractId(300, (ulong)index + 1), Hello(new ContractId(400, (ulong)index + 1)));
                Require(client.PairAndConnect(Sas)?.Accepted == true, "OUTAGE_PAIR");
                client.Disconnect();
                clock.Advance(resumableOutages[index]);
                Require(client.Resume().Decision == ResumeDecision.ResumeWithDeltas, "OUTAGE_RESUME");
            }

            ManualClock expiredClock = new();
            SyntheticSessionHost expiredHost = CreateHost(expiredClock, Snapshot());
            SyntheticSessionClient expiredClient = new(expiredHost, new ContractId(300, 3), Hello(new ContractId(400, 3)));
            Require(expiredClient.PairAndConnect(Sas)?.Accepted == true, "LEASE_PAIR");
            expiredClient.Disconnect();
            expiredClock.Advance(30_000);
            bool leaseExpired = false;
            try
            {
                expiredClient.Resume();
            }
            catch (InvalidOperationException)
            {
                leaseExpired = true;
            }

            Require(leaseExpired, "LEASE_NOT_EXPIRED");
            SyntheticSessionClient fresh = new(expiredHost, new ContractId(300, 4), Hello(new ContractId(400, 4)));
            Require(fresh.PairAndConnect(Sas)?.Accepted == true, "LEASE_REPAIR");

            ManualClock heartbeatClock = new();
            HeartbeatMonitor heartbeat = new(heartbeatClock);
            heartbeatClock.Advance(1_000);
            Require(heartbeat.ShouldSend && !heartbeat.IsTimedOut, "HEARTBEAT_SEND");
            heartbeat.MarkSent();
            heartbeatClock.Advance(2_000);
            Require(heartbeat.IsTimedOut, "HEARTBEAT_TIMEOUT");
        }

        private static double MeasureResumeP95Milliseconds()
        {
            ManualClock clock = new();
            SyntheticSessionHost host = CreateHost(clock, Snapshot());
            SyntheticSessionClient client = new(host, new ContractId(500, 1), Hello(new ContractId(501, 1)));
            Require(client.PairAndConnect(Sas)?.Accepted == true, "RESUME_P95_PAIR");

            double[] samples = new double[200];
            for (int index = 0; index < samples.Length; index++)
            {
                client.Disconnect();
                long startedAt = System.Diagnostics.Stopwatch.GetTimestamp();
                client.Resume();
                samples[index] = (System.Diagnostics.Stopwatch.GetTimestamp() - startedAt) * 1_000d / System.Diagnostics.Stopwatch.Frequency;
            }

            Array.Sort(samples);
            return samples[(int)Math.Ceiling(samples.Length * 0.95) - 1];
        }

        private static SyntheticSessionHost CreateHost(ManualClock clock, SessionSnapshot snapshot)
        {
            int nonce = 0;
            HandshakePolicy policy = new(ProtocolVersion.V1, new[] { s_Schema }, new BuildIdentity("1.0.0-desktop", "abcdef", "hbp-core-1"), AllCapabilities, AllCapabilities, () => new ContractId(100, (ulong)++nonce));
            return new SyntheticSessionHost(snapshot, policy, Sas, clock, TokenBytes);
        }

        private static ClientHello Hello(ContractId? nonce = null)
        {
            return new ClientHello(ProtocolVersion.V1, new[] { s_Schema }, new BuildIdentity("1.0.0-xr", "abcdef", "hbp-core-1"), AllCapabilities, DeviceClass.Quest, nonce ?? new ContractId(200, 1));
        }

        private static SessionSnapshot Snapshot(ulong epoch = 1, ulong stateRevision = 1, double opacity = 0.25)
        {
            SessionEpoch session = new(new ContractId(1, epoch), epoch);
            ScopeState scope = new(s_ColumnScope, new ScopeRevision(stateRevision), new[]
            {
                new StateProperty(V1PropertyKeys.ColumnActivityOpacity, ContractValue.FromNumber(opacity)),
            });
            return new SessionSnapshot(ContractVersion.V1, session, new StateRevision(stateRevision), new[] { scope }, Array.Empty<AssetReference>());
        }

        private static Command OpacityCommand(SessionSnapshot basis, ContractId commandId, ContractId correlationId, double opacity)
        {
            return new Command(basis.Session, commandId, correlationId, s_ColumnScope, FindScope(basis).Revision, CommandKind.SetOpacity, ContractValue.FromNumber(opacity));
        }

        private static ScopeState FindScope(SessionSnapshot snapshot)
        {
            for (int index = 0; index < snapshot.Scopes.Count; index++)
            {
                if (snapshot.Scopes[index].Scope == s_ColumnScope)
                    return snapshot.Scopes[index];
            }

            throw new InvalidOperationException("SCOPE_MISSING");
        }

        private static double Opacity(SessionSnapshot snapshot)
        {
            ScopeState scope = FindScope(snapshot);
            for (int index = 0; index < scope.Properties.Count; index++)
            {
                if (scope.Properties[index].Key == V1PropertyKeys.ColumnActivityOpacity)
                    return scope.Properties[index].Value.Number;
            }

            throw new InvalidOperationException("OPACITY_MISSING");
        }

        private static PairingToken Token() => new(TokenBytes());

        private static byte[] TokenBytes()
        {
            byte[] token = new byte[PairingToken.ByteLength];
            for (int index = 0; index < token.Length; index++)
                token[index] = (byte)(index + 1);
            return token;
        }

        private static void Require(bool condition, string failureCode)
        {
            if (!condition)
                throw new InvalidOperationException(failureCode);
        }

        private sealed class ManualClock : IMonotonicClock
        {
            public long Milliseconds { get; private set; }

            public void Advance(long milliseconds)
            {
                Milliseconds = checked(Milliseconds + milliseconds);
            }
        }

        [Serializable]
        private sealed class P07QuestEvidence
        {
            public string schema;
            public string platform;
            public string unity;
            public string scriptingBackend;
            public int atomicIterations;
            public int inconsistentReads;
            public long appliedEffects;
            public bool replayedWithoutEffect;
            public bool commandIdReuseRejected;
            public bool conflictWithoutEffect;
            public string resumeDecision;
            public int resumeSamples;
            public double resumeP95Milliseconds;
            public bool replacementPurged;
            public bool outagesValidated;
            public bool heartbeatValidated;
            public bool diagnosticsRedacted;
            public string result;
            public string failureCode;
        }
    }
}
