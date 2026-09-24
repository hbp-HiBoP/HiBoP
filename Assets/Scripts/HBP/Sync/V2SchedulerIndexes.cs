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
        private readonly byte[][] m_FamilyDigests = { new byte[32], new byte[32], new byte[32] };
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
                writer.Write(Encoding.ASCII.GetBytes("HBPCHK1"));
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
            if (index < 0 || index > 2)
                throw new ArgumentOutOfRangeException(nameof(kind));
            return index;
        }

        private static void Xor(byte[] target, byte[] value)
        {
            for (int i = 0; i < target.Length; i++)
                target[i] ^= value[i];
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
}
