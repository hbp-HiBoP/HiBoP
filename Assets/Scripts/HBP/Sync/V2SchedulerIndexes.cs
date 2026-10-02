using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace HBP.Sync
{
    public enum V2OperationAdmission : byte
    {
        Accepted = 0,
        Duplicate = 1,
        Conflicting = 2,
        OperationIdReused = 3,
        Overflow = 4,
        Invalid = 5
    }

    /// <summary>
    /// Bounded idempotency and per-key accepted-watermark index for one live
    /// synchronization session. Entries live for the owning incarnation.
    /// </summary>
    public sealed class V2OperationLedger
    {
        private readonly int m_MaximumOperations;
        private readonly int m_MaximumKeys;
        private readonly int m_MaximumSceneWatermarks;
        private readonly Dictionary<Guid, OperationEntry> m_Operations = new Dictionary<Guid, OperationEntry>();
        private readonly Dictionary<V2TouchedKey, ulong> m_LastAccepted = new Dictionary<V2TouchedKey, ulong>();
        private readonly Dictionary<SceneIncarnationScope, SceneWatermark> m_SceneWatermarks = new Dictionary<SceneIncarnationScope, SceneWatermark>();

        public int OperationCount => m_Operations.Count;
        public int IndexedKeyCount => m_LastAccepted.Count;
        public int SceneWatermarkCount => m_SceneWatermarks.Count;

        public V2OperationLedger(int maximumOperations = 4096, int maximumKeys = 65536, int maximumSceneWatermarks = 4096)
        {
            if (maximumOperations <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximumOperations));
            if (maximumKeys <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximumKeys));
            if (maximumSceneWatermarks <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximumSceneWatermarks));
            m_MaximumOperations = maximumOperations;
            m_MaximumKeys = maximumKeys;
            m_MaximumSceneWatermarks = maximumSceneWatermarks;
        }

        public V2OperationAdmission Accept(OperationId operationId, V2ScheduleDescriptor descriptor, ulong observedCanonicalSequence, ulong acceptedCanonicalSequence, byte[] canonicalPayload)
        {
            if (operationId == null || descriptor == null || canonicalPayload == null || acceptedCanonicalSequence == 0 || observedCanonicalSequence >= acceptedCanonicalSequence || descriptor.TouchedKeys.Count == 0 && descriptor.BarrierScope != V2BarrierScope.AllScene)
                return V2OperationAdmission.Invalid;

            byte[] digest;
            using (SHA256 sha = SHA256.Create())
                digest = sha.ComputeHash(canonicalPayload);

            if (m_Operations.TryGetValue(operationId.Value, out OperationEntry existing))
            {
                bool sameIncarnation = existing.SceneId.Equals(descriptor.SceneId) && existing.IncarnationId.Equals(descriptor.IncarnationId);
                return sameIncarnation && BytesEqual(existing.PayloadDigest, digest) ? V2OperationAdmission.Duplicate : V2OperationAdmission.OperationIdReused;
            }

            var sceneScope = new SceneIncarnationScope(descriptor.SceneId, descriptor.IncarnationId);
            bool hasSceneWatermark = m_SceneWatermarks.TryGetValue(sceneScope, out SceneWatermark sceneWatermark);
            bool isAllSceneBarrier = descriptor.BarrierScope == V2BarrierScope.AllScene;
            if (hasSceneWatermark && (isAllSceneBarrier ? sceneWatermark.LatestAcceptedSequence > observedCanonicalSequence : sceneWatermark.LatestAllSceneBarrierSequence > observedCanonicalSequence))
                return V2OperationAdmission.Conflicting;

            int newKeyCount = 0;
            for (int i = 0; i < descriptor.TouchedKeys.Count; i++)
            {
                V2TouchedKey key = descriptor.TouchedKeys[i];
                if (m_LastAccepted.TryGetValue(key, out ulong lastAccepted))
                {
                    if (lastAccepted > observedCanonicalSequence)
                        return V2OperationAdmission.Conflicting;
                }
                else
                {
                    bool duplicateInDescriptor = false;
                    for (int j = 0; j < i; j++)
                    {
                        if (descriptor.TouchedKeys[j].Equals(key))
                        {
                            duplicateInDescriptor = true;
                            break;
                        }
                    }

                    if (!duplicateInDescriptor)
                        newKeyCount++;
                }
            }

            if (m_Operations.Count >= m_MaximumOperations || newKeyCount > m_MaximumKeys - m_LastAccepted.Count || !hasSceneWatermark && m_SceneWatermarks.Count >= m_MaximumSceneWatermarks)
                return V2OperationAdmission.Overflow;

            for (int i = 0; i < descriptor.TouchedKeys.Count; i++)
                m_LastAccepted[descriptor.TouchedKeys[i]] = acceptedCanonicalSequence;
            m_Operations.Add(operationId.Value, new OperationEntry(descriptor.SceneId, descriptor.IncarnationId, digest));
            if (!hasSceneWatermark)
            {
                sceneWatermark = new SceneWatermark();
                m_SceneWatermarks.Add(sceneScope, sceneWatermark);
            }

            sceneWatermark.LatestAcceptedSequence = Math.Max(sceneWatermark.LatestAcceptedSequence, acceptedCanonicalSequence);
            if (isAllSceneBarrier)
                sceneWatermark.LatestAllSceneBarrierSequence = Math.Max(sceneWatermark.LatestAllSceneBarrierSequence, acceptedCanonicalSequence);
            return V2OperationAdmission.Accepted;
        }

        public bool TryGetLastAcceptedSequence(V2TouchedKey key, out ulong sequence)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            return m_LastAccepted.TryGetValue(key, out sequence);
        }

        public void CloseIncarnation(SceneId sceneId, IncarnationId incarnationId)
        {
            if (sceneId == null)
                throw new ArgumentNullException(nameof(sceneId));
            if (incarnationId == null)
                throw new ArgumentNullException(nameof(incarnationId));

            var keysToRemove = new List<V2TouchedKey>();
            foreach (KeyValuePair<V2TouchedKey, ulong> entry in m_LastAccepted)
            {
                if (entry.Key.SceneId.Equals(sceneId) && entry.Key.IncarnationId.Equals(incarnationId))
                    keysToRemove.Add(entry.Key);
            }

            for (int i = 0; i < keysToRemove.Count; i++)
                m_LastAccepted.Remove(keysToRemove[i]);

            var operationIdsToRemove = new List<Guid>();
            foreach (KeyValuePair<Guid, OperationEntry> entry in m_Operations)
            {
                if (entry.Value.SceneId.Equals(sceneId) && entry.Value.IncarnationId.Equals(incarnationId))
                    operationIdsToRemove.Add(entry.Key);
            }

            for (int i = 0; i < operationIdsToRemove.Count; i++)
                m_Operations.Remove(operationIdsToRemove[i]);

            m_SceneWatermarks.Remove(new SceneIncarnationScope(sceneId, incarnationId));
        }

        private static bool BytesEqual(byte[] left, byte[] right)
        {
            if (left.Length != right.Length)
                return false;
            int difference = 0;
            for (int i = 0; i < left.Length; i++)
                difference |= left[i] ^ right[i];
            return difference == 0;
        }

        private sealed class OperationEntry
        {
            public SceneId SceneId { get; }
            public IncarnationId IncarnationId { get; }
            public byte[] PayloadDigest { get; }

            public OperationEntry(SceneId sceneId, IncarnationId incarnationId, byte[] payloadDigest)
            {
                SceneId = sceneId;
                IncarnationId = incarnationId;
                PayloadDigest = payloadDigest;
            }
        }

        private readonly struct SceneIncarnationScope : IEquatable<SceneIncarnationScope>
        {
            public SceneId SceneId { get; }
            public IncarnationId IncarnationId { get; }

            public SceneIncarnationScope(SceneId sceneId, IncarnationId incarnationId)
            {
                SceneId = sceneId;
                IncarnationId = incarnationId;
            }

            public bool Equals(SceneIncarnationScope other) => SceneId.Equals(other.SceneId) && IncarnationId.Equals(other.IncarnationId);
            public override bool Equals(object obj) => obj is SceneIncarnationScope other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    return (SceneId.GetHashCode() * 397) ^ IncarnationId.GetHashCode();
                }
            }
        }

        private sealed class SceneWatermark
        {
            public ulong LatestAcceptedSequence;
            public ulong LatestAllSceneBarrierSequence;
        }
    }

    public sealed class V2CheckpointIdentity : IEquatable<V2CheckpointIdentity>
    {
        private readonly byte[] m_Digest;

        public int Length => m_Digest.Length;
        public byte[] GetDigestCopy() => (byte[])m_Digest.Clone();

        internal V2CheckpointIdentity(byte[] digest)
        {
            m_Digest = (byte[])digest.Clone();
        }

        public bool Equals(V2CheckpointIdentity other)
        {
            if (other == null || other.m_Digest.Length != m_Digest.Length)
                return false;
            for (int i = 0; i < m_Digest.Length; i++)
            {
                if (m_Digest[i] != other.m_Digest[i])
                    return false;
            }

            return true;
        }

        public override bool Equals(object obj) => Equals(obj as V2CheckpointIdentity);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                for (int i = 0; i < m_Digest.Length; i++)
                    hash = (hash * 31) ^ m_Digest[i];
                return hash;
            }
        }

        public override string ToString() => BitConverter.ToString(m_Digest).Replace("-", string.Empty);
    }

    /// <summary>
    /// Incrementally composes typed checkpoint family records. A replacement or
    /// removal updates one family accumulator in O(record size); identity creation
    /// combines three fixed-size family digests and never walks the record map.
    /// </summary>
    public sealed class V2CheckpointIdentityAccumulator
    {
        private readonly SceneId m_SceneId;
        private readonly IncarnationId m_IncarnationId;
        private readonly int m_MaximumRecords;
        private readonly int m_MaximumBytes;
        private readonly Dictionary<V2TouchedKey, CheckpointEntry> m_Entries = new Dictionary<V2TouchedKey, CheckpointEntry>();
        private readonly byte[][] m_FamilyDigests = CreateFamilyDigests();
        private int m_EncodedBytes;

        public int RecordCount => m_Entries.Count;
        public int EncodedBytes => m_EncodedBytes;

        public V2CheckpointIdentityAccumulator(SceneId sceneId, IncarnationId incarnationId, int maximumRecords = 65536, int maximumBytes = 64 * 1024 * 1024)
        {
            m_SceneId = sceneId ?? throw new ArgumentNullException(nameof(sceneId));
            m_IncarnationId = incarnationId ?? throw new ArgumentNullException(nameof(incarnationId));
            if (maximumRecords <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximumRecords));
            if (maximumBytes <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximumBytes));
            m_MaximumRecords = maximumRecords;
            m_MaximumBytes = maximumBytes;
        }

        public bool Add(SiteColorCheckpointRecord record) => AddRecord(record?.Value, V2TouchedKeyKind.SiteColor, record?.Encode());
        public bool Add(CutDefinitionCheckpointRecord record) => AddRecord(record?.Value, V2TouchedKeyKind.CutDefinition, record?.Encode());
        public bool Add(TimelineAnchorCheckpointRecord record) => AddRecord(record?.Value, V2TouchedKeyKind.TimelineAnchor, record?.Encode());

        public bool Add(V2T09CheckpointRecord record)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            V2TouchedKeyKind kind = new V2MutationDescriptor(m_SceneId, m_IncarnationId, record.Value).CoalescingKey.Kind;
            return AddRecord(record.Value, kind, record.Encode());
        }

        public bool Remove(V2TouchedKey key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (!key.SceneId.Equals(m_SceneId) || !key.IncarnationId.Equals(m_IncarnationId))
                return false;
            if (!m_Entries.TryGetValue(key, out CheckpointEntry entry))
                return false;

            Xor(m_FamilyDigests[FamilyIndex(entry.Kind)], entry.RecordDigest);
            m_EncodedBytes -= entry.EncodedLength;
            m_Entries.Remove(key);
            return true;
        }

        public V2CheckpointIdentity GetIdentity()
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                writer.Write(Encoding.ASCII.GetBytes("HBPCHK2"));
                writer.Write(m_SceneId.ToByteArray());
                writer.Write(m_IncarnationId.ToByteArray());
                writer.Write(checked((uint)m_Entries.Count));
                for (int i = 0; i < m_FamilyDigests.Length; i++)
                {
                    writer.Write(checked((byte)(i + 1)));
                    writer.Write(m_FamilyDigests[i]);
                }

                writer.Flush();
                using (SHA256 sha = SHA256.Create())
                    return new V2CheckpointIdentity(sha.ComputeHash(stream.ToArray()));
            }
        }

        private bool AddRecord(V2Mutation value, V2TouchedKeyKind kind, byte[] encoded)
        {
            if (value == null || encoded == null)
                throw new ArgumentNullException(nameof(value));
            V2TouchedKey key = new V2MutationDescriptor(m_SceneId, m_IncarnationId, value).CoalescingKey;
            byte[] recordDigest = HashRecord(key, kind, encoded);
            m_Entries.TryGetValue(key, out CheckpointEntry previous);
            int previousLength = previous?.EncodedLength ?? 0;
            int resultingBytes = checked(m_EncodedBytes - previousLength + encoded.Length);
            int resultingCount = m_Entries.Count + (previous == null ? 1 : 0);
            if (resultingCount > m_MaximumRecords || resultingBytes > m_MaximumBytes)
                return false;
            if (previous != null)
                Xor(m_FamilyDigests[FamilyIndex(previous.Kind)], previous.RecordDigest);
            Xor(m_FamilyDigests[FamilyIndex(kind)], recordDigest);
            m_Entries[key] = new CheckpointEntry(kind, recordDigest, encoded.Length);
            m_EncodedBytes = resultingBytes;
            return true;
        }

        private static byte[] HashRecord(V2TouchedKey key, V2TouchedKeyKind kind, byte[] encoded)
        {
            byte[] keyFingerprint = V2ScheduleDescriptor.Fingerprint(key);
            using (var stream = new MemoryStream(keyFingerprint.Length + encoded.Length + 1))
            {
                stream.WriteByte((byte)kind);
                stream.Write(keyFingerprint, 0, keyFingerprint.Length);
                stream.Write(encoded, 0, encoded.Length);
                using (SHA256 sha = SHA256.Create())
                    return sha.ComputeHash(stream.ToArray());
            }
        }

        private static int FamilyIndex(V2TouchedKeyKind kind)
        {
            int index = (int)kind - 1;
            if (index < 0 || index >= 27)
                throw new ArgumentOutOfRangeException(nameof(kind));
            return index;
        }

        private static void Xor(byte[] target, byte[] value)
        {
            for (int i = 0; i < target.Length; i++)
                target[i] ^= value[i];
        }

        private static byte[][] CreateFamilyDigests()
        {
            var digests = new byte[27][];
            for (int i = 0; i < digests.Length; i++) digests[i] = new byte[32];
            return digests;
        }

        private sealed class CheckpointEntry
        {
            public V2TouchedKeyKind Kind { get; }
            public byte[] RecordDigest { get; }
            public int EncodedLength { get; }

            public CheckpointEntry(V2TouchedKeyKind kind, byte[] recordDigest, int encodedLength)
            {
                Kind = kind;
                RecordDigest = recordDigest;
                EncodedLength = encodedLength;
            }
        }
    }

    public enum V2JobType : byte
    {
        Filter = 1,
        Correlation = 2,
        ActivityProjection = 3
    }

    public sealed class V2JobIdentity : IEquatable<V2JobIdentity>
    {
        public SceneId SceneId { get; }
        public IncarnationId IncarnationId { get; }
        public V2JobType JobType { get; }
        public OperationId JobId { get; }
        public ulong Generation { get; }
        public ulong InputGeneration { get; }

        internal V2JobIdentity(SceneId sceneId, IncarnationId incarnationId, V2JobType jobType, OperationId jobId, ulong generation, ulong inputGeneration)
        {
            SceneId = sceneId;
            IncarnationId = incarnationId;
            JobType = jobType;
            JobId = jobId;
            Generation = generation;
            InputGeneration = inputGeneration;
        }

        public bool Equals(V2JobIdentity other)
        {
            return other != null && SceneId.Equals(other.SceneId) && IncarnationId.Equals(other.IncarnationId) && JobType == other.JobType && JobId.Equals(other.JobId) && Generation == other.Generation && InputGeneration == other.InputGeneration;
        }

        public override bool Equals(object obj) => Equals(obj as V2JobIdentity);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = SceneId.GetHashCode();
                hash = (hash * 397) ^ IncarnationId.GetHashCode();
                hash = (hash * 397) ^ (int)JobType;
                hash = (hash * 397) ^ JobId.GetHashCode();
                hash = (hash * 397) ^ Generation.GetHashCode();
                return (hash * 397) ^ InputGeneration.GetHashCode();
            }
        }
    }

    public sealed class V2JobAttempt
    {
        public V2JobIdentity Identity { get; }
        public Guid AttemptId { get; }

        internal V2JobAttempt(V2JobIdentity identity, Guid attemptId)
        {
            Identity = identity;
            AttemptId = attemptId;
        }
    }

    /// <summary>
    /// Tracks the current job generation per scene incarnation and job type.
    /// Call IsCurrent immediately before publishing a result or accepting a chunk.
    /// </summary>
    public sealed class V2JobGenerationRegistry
    {
        private readonly int m_MaximumScopes;
        private readonly Func<Guid> m_GuidFactory;
        private readonly Dictionary<JobScope, JobState> m_States = new Dictionary<JobScope, JobState>();

        public int TrackedScopeCount => m_States.Count;

        public V2JobGenerationRegistry(int maximumScopes = 64, Func<Guid> guidFactory = null)
        {
            if (maximumScopes <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximumScopes));
            m_MaximumScopes = maximumScopes;
            m_GuidFactory = guidFactory ?? Guid.NewGuid;
        }

        public V2JobIdentity BeginJob(SceneId sceneId, IncarnationId incarnationId, V2JobType jobType, OperationId jobId, ulong inputGeneration = 0)
        {
            if (sceneId == null)
                throw new ArgumentNullException(nameof(sceneId));
            if (incarnationId == null)
                throw new ArgumentNullException(nameof(incarnationId));
            if (jobId == null)
                throw new ArgumentNullException(nameof(jobId));
            ValidateJobType(jobType);
            if (jobType == V2JobType.ActivityProjection && inputGeneration == 0)
                throw new ArgumentOutOfRangeException(nameof(inputGeneration), "Activity jobs must identify their frozen input generation.");

            var scope = new JobScope(sceneId, incarnationId, jobType);
            if (!m_States.TryGetValue(scope, out JobState state))
            {
                if (m_States.Count >= m_MaximumScopes)
                    return null;
                state = new JobState();
                m_States.Add(scope, state);
            }

            if (state.Generation == ulong.MaxValue)
                return null;
            state.Generation++;
            state.Identity = new V2JobIdentity(sceneId, incarnationId, jobType, jobId, state.Generation, inputGeneration);
            state.AttemptId = Guid.Empty;
            state.Cancelled = false;
            return state.Identity;
        }

        public V2JobAttempt BeginAttempt(V2JobIdentity identity)
        {
            if (!TryGetCurrentState(identity, out JobState state) || state.Cancelled)
                return null;
            Guid attemptId = m_GuidFactory();
            if (attemptId == Guid.Empty)
                throw new InvalidOperationException("The attempt identity factory returned an empty identity.");
            state.AttemptId = attemptId;
            return new V2JobAttempt(state.Identity, attemptId);
        }

        public bool Cancel(V2JobIdentity identity)
        {
            if (!TryGetCurrentState(identity, out JobState state) || state.Cancelled)
                return false;
            state.Cancelled = true;
            state.AttemptId = Guid.Empty;
            return true;
        }

        public bool IsCurrent(V2JobAttempt attempt)
        {
            return attempt != null && TryGetCurrentState(attempt.Identity, out JobState state) && !state.Cancelled && state.AttemptId != Guid.Empty && state.AttemptId == attempt.AttemptId;
        }

        public void CloseIncarnation(SceneId sceneId, IncarnationId incarnationId)
        {
            if (sceneId == null)
                throw new ArgumentNullException(nameof(sceneId));
            if (incarnationId == null)
                throw new ArgumentNullException(nameof(incarnationId));
            var scopes = new List<JobScope>();
            foreach (JobScope scope in m_States.Keys)
            {
                if (scope.SceneId.Equals(sceneId) && scope.IncarnationId.Equals(incarnationId))
                    scopes.Add(scope);
            }

            for (int i = 0; i < scopes.Count; i++)
                m_States.Remove(scopes[i]);
        }

        private bool TryGetCurrentState(V2JobIdentity identity, out JobState state)
        {
            if (identity == null)
            {
                state = null;
                return false;
            }

            return m_States.TryGetValue(new JobScope(identity.SceneId, identity.IncarnationId, identity.JobType), out state) && state.Identity != null && state.Identity.Equals(identity);
        }

        private static void ValidateJobType(V2JobType jobType)
        {
            if (jobType != V2JobType.Filter && jobType != V2JobType.Correlation && jobType != V2JobType.ActivityProjection)
                throw new ArgumentOutOfRangeException(nameof(jobType));
        }

        private readonly struct JobScope : IEquatable<JobScope>
        {
            public SceneId SceneId { get; }
            public IncarnationId IncarnationId { get; }
            public V2JobType JobType { get; }

            public JobScope(SceneId sceneId, IncarnationId incarnationId, V2JobType jobType)
            {
                SceneId = sceneId;
                IncarnationId = incarnationId;
                JobType = jobType;
            }

            public bool Equals(JobScope other)
            {
                return SceneId.Equals(other.SceneId) && IncarnationId.Equals(other.IncarnationId) && JobType == other.JobType;
            }

            public override bool Equals(object obj) => obj is JobScope other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((SceneId.GetHashCode() * 397) ^ IncarnationId.GetHashCode()) * 397 ^ (int)JobType;
                }
            }
        }

        private sealed class JobState
        {
            public ulong Generation;
            public V2JobIdentity Identity;
            public Guid AttemptId;
            public bool Cancelled;
        }
    }

    public enum V2ActivityProjectionControlKind : byte
    {
        Request = 1,
        Started = 2,
        Progress = 3,
        Ready = 4,
        Cancel = 5,
        Cancelled = 6,
        Failed = 7,
        Policy = 8,
        RemoveRequest = 9
    }

    /// <summary>Bounded controls for one locally computed activity-projection generation.</summary>
    public sealed class V2ActivityProjectionControl
    {
        public V2ActivityProjectionControlKind Kind { get; }
        public OperationId JobId { get; }
        public ulong Generation { get; }
        public ulong InputGeneration { get; }
        public ulong CanonicalSequence { get; }
        public bool ProjectionRequested { get; }
        public bool AutomaticPolicyEnabled { get; }
        public bool ExplicitRequest { get; }
        public float Progress { get; }
        public string Message { get; }
        public string FailureCode { get; }

        public V2ActivityProjectionControl(V2ActivityProjectionControlKind kind, OperationId jobId, ulong generation, ulong inputGeneration = 0, ulong canonicalSequence = 0, bool projectionRequested = false, bool automaticPolicyEnabled = false, bool explicitRequest = false, float progress = 0f, string message = null, string failureCode = null)
        {
            if (kind < V2ActivityProjectionControlKind.Request || kind > V2ActivityProjectionControlKind.RemoveRequest)
                throw new ArgumentOutOfRangeException(nameof(kind));
            JobId = jobId ?? throw new ArgumentNullException(nameof(jobId));
            if (kind is V2ActivityProjectionControlKind.Request or V2ActivityProjectionControlKind.Policy or V2ActivityProjectionControlKind.RemoveRequest or V2ActivityProjectionControlKind.Cancel)
            {
                if (generation != 0 && (kind == V2ActivityProjectionControlKind.Request || kind == V2ActivityProjectionControlKind.Policy || kind == V2ActivityProjectionControlKind.RemoveRequest))
                    throw new ArgumentOutOfRangeException(nameof(generation));
            }
            else if (generation == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(generation));
            }

            if (kind != V2ActivityProjectionControlKind.Policy && inputGeneration == 0)
                throw new ArgumentOutOfRangeException(nameof(inputGeneration));
            if (float.IsNaN(progress) || float.IsInfinity(progress) || progress < 0f || progress > 1f)
                throw new ArgumentOutOfRangeException(nameof(progress));
            if (kind != V2ActivityProjectionControlKind.Progress && (progress != 0f || message != null))
                throw new ArgumentException("Only progress controls carry progress values and messages.");
            if (kind == V2ActivityProjectionControlKind.Failed)
            {
                if (string.IsNullOrWhiteSpace(failureCode)) throw new ArgumentException("A failed projection requires a failure code.", nameof(failureCode));
                if (V2ActivityProjectionControlCodec.Utf8.GetByteCount(failureCode) > V2ActivityProjectionControlCodec.MaximumFailureCodeBytes)
                    throw new ArgumentOutOfRangeException(nameof(failureCode));
            }
            else if (failureCode != null)
            {
                throw new ArgumentException("Only failed controls carry a failure code.", nameof(failureCode));
            }

            if (message != null && V2ActivityProjectionControlCodec.Utf8.GetByteCount(message) > V2ActivityProjectionControlCodec.MaximumMessageBytes)
                throw new ArgumentOutOfRangeException(nameof(message));
            if (kind != V2ActivityProjectionControlKind.Progress && message != null)
                throw new ArgumentException("Only progress controls carry a message.", nameof(message));

            Kind = kind;
            Generation = generation;
            InputGeneration = inputGeneration;
            CanonicalSequence = canonicalSequence;
            ProjectionRequested = projectionRequested;
            AutomaticPolicyEnabled = automaticPolicyEnabled;
            ExplicitRequest = explicitRequest;
            Progress = progress;
            Message = message;
            FailureCode = failureCode;
        }
    }

    /// <summary>Releases a coordinated projection busy scope only after both local and remote work are terminal.</summary>
    public sealed class V2ActivityProjectionTerminalBarrier
    {
        public bool LocalTerminal { get; private set; }
        public bool RemoteTerminal { get; private set; }
        public bool IsComplete => LocalTerminal && RemoteTerminal;
        public void MarkLocalTerminal() => LocalTerminal = true;
        public void MarkRemoteTerminal() => RemoteTerminal = true;
    }

    public static class V2ActivityProjectionControlCodec
    {
        public const int MaximumMessageBytes = 256;
        public const int MaximumFailureCodeBytes = 128;
        public const int MaximumPayloadBytes = 512;
        private const ushort SchemaVersion = 1;
        private const int FixedHeaderBytes = 56;
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("HBAP");
        internal static readonly UTF8Encoding Utf8 = new(false, true);

        public static byte[] Encode(V2ActivityProjectionControl control)
        {
            if (control == null) throw new ArgumentNullException(nameof(control));
            byte[] message = control.Message == null ? Array.Empty<byte>() : Utf8.GetBytes(control.Message);
            byte[] failure = control.FailureCode == null ? Array.Empty<byte>() : Utf8.GetBytes(control.FailureCode);
            using var stream = new MemoryStream(FixedHeaderBytes + message.Length + failure.Length);
            using var writer = new BinaryWriter(stream, Utf8, true);
            writer.Write(Magic);
            writer.Write(SchemaVersion);
            writer.Write((byte)control.Kind);
            writer.Write(control.JobId.ToByteArray());
            writer.Write(control.Generation);
            writer.Write(control.InputGeneration);
            writer.Write(control.CanonicalSequence);
            writer.Write(control.Progress);
            byte flags = 0;
            if (control.ProjectionRequested) flags |= 1;
            if (control.AutomaticPolicyEnabled) flags |= 2;
            if (control.ExplicitRequest) flags |= 4;
            writer.Write(flags);
            writer.Write(checked((ushort)message.Length));
            writer.Write(checked((ushort)failure.Length));
            writer.Write(message);
            writer.Write(failure);
            writer.Flush();
            if (stream.Length > MaximumPayloadBytes) throw new InvalidDataException("Activity-projection control exceeds its session-control bound.");
            return stream.ToArray();
        }

        public static bool TryDecode(byte[] bytes, out V2ActivityProjectionControl control)
        {
            control = null;
            if (bytes == null || bytes.Length < FixedHeaderBytes || bytes.Length > MaximumPayloadBytes) return false;
            using var stream = new MemoryStream(bytes, false);
            using var reader = new BinaryReader(stream, Utf8, true);
            try
            {
                if (!reader.ReadBytes(Magic.Length).AsSpan().SequenceEqual(Magic) || reader.ReadUInt16() != SchemaVersion) return false;
                byte kindValue = reader.ReadByte();
                if (kindValue < (byte)V2ActivityProjectionControlKind.Request || kindValue > (byte)V2ActivityProjectionControlKind.RemoveRequest) return false;
                byte[] jobBytes = reader.ReadBytes(16);
                if (jobBytes.Length != 16) return false;
                ulong generation = reader.ReadUInt64();
                ulong inputGeneration = reader.ReadUInt64();
                ulong canonicalSequence = reader.ReadUInt64();
                float progress = reader.ReadSingle();
                byte flags = reader.ReadByte();
                if ((flags & ~7) != 0) return false;
                ushort messageLength = reader.ReadUInt16();
                ushort failureLength = reader.ReadUInt16();
                if (messageLength > MaximumMessageBytes || failureLength > MaximumFailureCodeBytes || messageLength + failureLength != stream.Length - stream.Position)
                    return false;
                string message = messageLength == 0 ? null : Utf8.GetString(reader.ReadBytes(messageLength));
                string failure = failureLength == 0 ? null : Utf8.GetString(reader.ReadBytes(failureLength));
                if (stream.Position != stream.Length) return false;
                control = new V2ActivityProjectionControl((V2ActivityProjectionControlKind)kindValue, new OperationId(new Guid(jobBytes)), generation, inputGeneration, canonicalSequence, (flags & 1) != 0, (flags & 2) != 0, (flags & 4) != 0, progress, message, failure);
                return true;
            }
            catch (Exception exception) when (exception is EndOfStreamException || exception is ArgumentException || exception is DecoderFallbackException || exception is OverflowException)
            {
                return false;
            }
        }
    }
}
