using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using CRNL.HiBoP.Contracts;

namespace CRNL.HiBoP.Protocol
{
    public enum RemoteAssetKind : byte
    {
        Unknown = 0,
        Surface = 1,
        Site = 2,
        Texture = 3,
        CutGeometry = 4,
    }

    public enum RemoteAssetVariant : byte
    {
        None = 0,
        Anatomical = 1,
        Inflated = 2,
    }

    public enum RemoteAssetDependencyKind : byte
    {
        Unknown = 0,
        VariantBase = 1,
    }

    public sealed class RemoteAssetDependency
    {
        public RemoteAssetDependency(RemoteAssetDependencyKind kind, AssetHash hash)
        {
            if (kind == RemoteAssetDependencyKind.Unknown)
                throw new ArgumentOutOfRangeException(nameof(kind));
            if (!hash.IsValid)
                throw new ArgumentException("A valid dependency hash is required.", nameof(hash));

            Kind = kind;
            Hash = hash;
        }

        public RemoteAssetDependencyKind Kind { get; }

        public AssetHash Hash { get; }
    }

    public sealed class RemoteAssetCapabilities
    {
        public static readonly RemoteAssetCapabilities SafetyMaximums = new(RemoteAssetDescriptor.MaximumChunkBytes, RemoteAssetDescriptor.MaximumSurfaceBytes, RemoteAssetDescriptor.MaximumSiteBytes, RemoteAssetDescriptor.MaximumTextureBytes, RemoteAssetDescriptor.MaximumCutGeometryBytes);

        public RemoteAssetCapabilities(int maximumChunkBytes, int maximumSurfaceBytes, int maximumSiteBytes, int maximumTextureBytes, int maximumCutGeometryBytes)
        {
            MaximumChunkBytes = Validate(maximumChunkBytes, RemoteAssetDescriptor.MaximumChunkBytes, nameof(maximumChunkBytes));
            MaximumSurfaceBytes = Validate(maximumSurfaceBytes, RemoteAssetDescriptor.MaximumSurfaceBytes, nameof(maximumSurfaceBytes));
            MaximumSiteBytes = Validate(maximumSiteBytes, RemoteAssetDescriptor.MaximumSiteBytes, nameof(maximumSiteBytes));
            MaximumTextureBytes = Validate(maximumTextureBytes, RemoteAssetDescriptor.MaximumTextureBytes, nameof(maximumTextureBytes));
            MaximumCutGeometryBytes = Validate(maximumCutGeometryBytes, RemoteAssetDescriptor.MaximumCutGeometryBytes, nameof(maximumCutGeometryBytes));
        }

        public int MaximumChunkBytes { get; }

        public int MaximumSurfaceBytes { get; }

        public int MaximumSiteBytes { get; }

        public int MaximumTextureBytes { get; }

        public int MaximumCutGeometryBytes { get; }

        public static RemoteAssetCapabilities Negotiate(RemoteAssetCapabilities local, RemoteAssetCapabilities remote)
        {
            if (local == null)
                throw new ArgumentNullException(nameof(local));
            if (remote == null)
                throw new ArgumentNullException(nameof(remote));
            return new RemoteAssetCapabilities(Math.Min(local.MaximumChunkBytes, remote.MaximumChunkBytes), Math.Min(local.MaximumSurfaceBytes, remote.MaximumSurfaceBytes), Math.Min(local.MaximumSiteBytes, remote.MaximumSiteBytes), Math.Min(local.MaximumTextureBytes, remote.MaximumTextureBytes), Math.Min(local.MaximumCutGeometryBytes, remote.MaximumCutGeometryBytes));
        }

        public void Validate(RemoteAssetDescriptor descriptor)
        {
            if (descriptor == null)
                throw new ArgumentNullException(nameof(descriptor));
            int maximum = descriptor.Kind switch
            {
                RemoteAssetKind.Surface => MaximumSurfaceBytes,
                RemoteAssetKind.Site => MaximumSiteBytes,
                RemoteAssetKind.Texture => MaximumTextureBytes,
                RemoteAssetKind.CutGeometry => MaximumCutGeometryBytes,
                _ => throw new ArgumentOutOfRangeException(nameof(descriptor)),
            };
            if (descriptor.ChunkBytes > MaximumChunkBytes || descriptor.EncodedBytes > maximum)
                throw new InvalidOperationException("The asset descriptor exceeds the negotiated safety capabilities.");
        }

        private static int Validate(int value, int hardMaximum, string parameterName)
        {
            if (value <= 0 || value > hardMaximum)
                throw new ArgumentOutOfRangeException(parameterName);
            return value;
        }
    }

    public sealed class RemoteAssetDescriptor
    {
        public const int MaximumChunkBytes = 1024 * 1024;
        public const int MaximumDependencies = 16;
        public const int MaximumSurfaceBytes = 256 * 1024 * 1024;
        public const int MaximumSiteBytes = 64 * 1024 * 1024;
        public const int MaximumTextureBytes = 256 * 1024 * 1024;
        public const int MaximumCutGeometryBytes = 128 * 1024 * 1024;
        public const int MaximumSurfaceVertices = 2_000_000;
        public const int MaximumSurfaceIndices = 12_000_000;
        public const int MaximumSites = 1_000_000;
        public const int MaximumTextureDimension = 16_384;

        private readonly ReadOnlyCollection<RemoteAssetDependency> m_Dependencies;

        public RemoteAssetDescriptor(AssetReference asset, RemoteAssetKind kind, RemoteAssetVariant variant, int encodedBytes, int primaryCount, int secondaryCount, int tertiaryCount, int chunkBytes, IEnumerable<RemoteAssetDependency> dependencies)
        {
            Asset = asset ?? throw new ArgumentNullException(nameof(asset));
            if (kind == RemoteAssetKind.Unknown)
                throw new ArgumentOutOfRangeException(nameof(kind));
            if (encodedBytes <= 0)
                throw new ArgumentOutOfRangeException(nameof(encodedBytes));
            if (chunkBytes <= 0 || chunkBytes > MaximumChunkBytes)
                throw new ArgumentOutOfRangeException(nameof(chunkBytes));

            var dependencyList = new List<RemoteAssetDependency>(dependencies ?? throw new ArgumentNullException(nameof(dependencies)));
            if (dependencyList.Count > MaximumDependencies)
                throw new ArgumentException("The descriptor contains too many dependencies.", nameof(dependencies));

            var dependencyHashes = new HashSet<AssetHash>();
            foreach (RemoteAssetDependency dependency in dependencyList)
            {
                if (dependency == null || dependency.Hash == asset.Hash || !dependencyHashes.Add(dependency.Hash))
                    throw new ArgumentException("Dependencies must be non-null, unique and must not reference the asset itself.", nameof(dependencies));
            }

            dependencyList.Sort((left, right) =>
            {
                int kindComparison = left.Kind.CompareTo(right.Kind);
                return kindComparison != 0 ? kindComparison : left.Hash.CompareTo(right.Hash);
            });

            ValidateShape(kind, variant, encodedBytes, primaryCount, secondaryCount, tertiaryCount, dependencyList);
            Kind = kind;
            Variant = variant;
            EncodedBytes = encodedBytes;
            PrimaryCount = primaryCount;
            SecondaryCount = secondaryCount;
            TertiaryCount = tertiaryCount;
            ChunkBytes = chunkBytes;
            m_Dependencies = dependencyList.AsReadOnly();
        }

        public AssetReference Asset { get; }

        public RemoteAssetKind Kind { get; }

        public RemoteAssetVariant Variant { get; }

        public int EncodedBytes { get; }

        public int PrimaryCount { get; }

        public int SecondaryCount { get; }

        public int TertiaryCount { get; }

        public int ChunkBytes { get; }

        public IReadOnlyList<RemoteAssetDependency> Dependencies => m_Dependencies;

        public int ChunkCount => checked((EncodedBytes + ChunkBytes - 1) / ChunkBytes);

        internal bool IsCompatibleWith(RemoteAssetDescriptor other)
        {
            if (other == null || Asset.Hash != other.Asset.Hash || Asset.SchemaVersion != other.Asset.SchemaVersion || Kind != other.Kind || Variant != other.Variant || EncodedBytes != other.EncodedBytes || PrimaryCount != other.PrimaryCount || SecondaryCount != other.SecondaryCount || TertiaryCount != other.TertiaryCount || ChunkBytes != other.ChunkBytes || Dependencies.Count != other.Dependencies.Count)
                return false;

            for (int index = 0; index < Dependencies.Count; index++)
            {
                if (Dependencies[index].Kind != other.Dependencies[index].Kind || Dependencies[index].Hash != other.Dependencies[index].Hash)
                    return false;
            }

            return true;
        }

        private static void ValidateShape(RemoteAssetKind kind, RemoteAssetVariant variant, int encodedBytes, int primaryCount, int secondaryCount, int tertiaryCount, IReadOnlyList<RemoteAssetDependency> dependencies)
        {
            switch (kind)
            {
                case RemoteAssetKind.Surface:
                    if (encodedBytes > MaximumSurfaceBytes || primaryCount <= 0 || primaryCount > MaximumSurfaceVertices || secondaryCount <= 0 || secondaryCount > MaximumSurfaceIndices || secondaryCount % 3 != 0 || (tertiaryCount != 0 && tertiaryCount != primaryCount))
                        throw new ArgumentOutOfRangeException(nameof(encodedBytes), "Surface dimensions exceed the allocation safety limits.");
                    long exactSurfaceBytes = 37L + (24L * primaryCount) + (4L * secondaryCount) + (8L * tertiaryCount);
                    if (encodedBytes != exactSurfaceBytes)
                        throw new ArgumentException("The encoded length does not match the surface dimensions.", nameof(encodedBytes));
                    ValidateSurfaceVariant(variant, dependencies);
                    break;
                case RemoteAssetKind.Site:
                    if (variant != RemoteAssetVariant.None || dependencies.Count != 0 || encodedBytes > MaximumSiteBytes || primaryCount <= 0 || primaryCount > MaximumSites || secondaryCount != 0 || tertiaryCount != 0)
                        throw new ArgumentOutOfRangeException(nameof(encodedBytes), "Site dimensions exceed the allocation safety limits.");
                    break;
                case RemoteAssetKind.Texture:
                    if (variant != RemoteAssetVariant.None || dependencies.Count != 0 || encodedBytes > MaximumTextureBytes || primaryCount <= 0 || primaryCount > MaximumTextureDimension || secondaryCount <= 0 || secondaryCount > MaximumTextureDimension || tertiaryCount != 0 || checked((long)primaryCount * secondaryCount * 4L) > MaximumTextureBytes)
                        throw new ArgumentOutOfRangeException(nameof(encodedBytes), "Texture dimensions exceed the allocation safety limits.");
                    break;
                case RemoteAssetKind.CutGeometry:
                    if (variant != RemoteAssetVariant.None || dependencies.Count != 0 || encodedBytes > MaximumCutGeometryBytes || primaryCount <= 0 || primaryCount > MaximumSurfaceVertices || secondaryCount <= 0 || secondaryCount > MaximumSurfaceIndices || secondaryCount % 3 != 0 || tertiaryCount != primaryCount)
                        throw new ArgumentOutOfRangeException(nameof(encodedBytes), "Cut geometry dimensions exceed the allocation safety limits.");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        private static void ValidateSurfaceVariant(RemoteAssetVariant variant, IReadOnlyList<RemoteAssetDependency> dependencies)
        {
            if (variant == RemoteAssetVariant.Inflated)
            {
                int baseCount = 0;
                for (int index = 0; index < dependencies.Count; index++)
                {
                    if (dependencies[index].Kind == RemoteAssetDependencyKind.VariantBase)
                        baseCount++;
                }

                if (baseCount != 1)
                    throw new ArgumentException("An inflated surface must name exactly one anatomical variant base.", nameof(dependencies));
            }
            else if (variant == RemoteAssetVariant.Anatomical || variant == RemoteAssetVariant.None)
            {
                for (int index = 0; index < dependencies.Count; index++)
                {
                    if (dependencies[index].Kind == RemoteAssetDependencyKind.VariantBase)
                        throw new ArgumentException("Only an inflated surface may declare a variant base.", nameof(dependencies));
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(variant));
            }
        }
    }

    public sealed class SurfaceVariantSetDescriptor
    {
        public SurfaceVariantSetDescriptor(RemoteAssetDescriptor anatomical, RemoteAssetDescriptor inflated)
        {
            Anatomical = anatomical ?? throw new ArgumentNullException(nameof(anatomical));
            Inflated = inflated ?? throw new ArgumentNullException(nameof(inflated));
            if (anatomical.Kind != RemoteAssetKind.Surface || anatomical.Variant != RemoteAssetVariant.Anatomical)
                throw new ArgumentException("The anatomical descriptor has the wrong type or role.", nameof(anatomical));
            if (inflated.Kind != RemoteAssetKind.Surface || inflated.Variant != RemoteAssetVariant.Inflated)
                throw new ArgumentException("The inflated descriptor has the wrong type or role.", nameof(inflated));
            if (anatomical.Asset.Hash == inflated.Asset.Hash || anatomical.Asset.SchemaVersion != inflated.Asset.SchemaVersion)
                throw new ArgumentException("Surface variants must have distinct hashes and the same schema version.");
            int matchingBases = 0;
            for (int index = 0; index < inflated.Dependencies.Count; index++)
            {
                if (inflated.Dependencies[index].Kind == RemoteAssetDependencyKind.VariantBase && inflated.Dependencies[index].Hash == anatomical.Asset.Hash)
                    matchingBases++;
            }

            if (matchingBases != 1)
                throw new ArgumentException("The inflated surface must depend on the anatomical hash.", nameof(inflated));

            byte[] manifest = new byte[(AssetHash.ByteLength * 2) + sizeof(ushort)];
            anatomical.Asset.Hash.WriteBytes(manifest, 0);
            inflated.Asset.Hash.WriteBytes(manifest, AssetHash.ByteLength);
            manifest[^2] = (byte)(anatomical.Asset.SchemaVersion >> 8);
            manifest[^1] = (byte)anatomical.Asset.SchemaVersion;
            using SHA256 sha256 = SHA256.Create();
            ManifestHash = AssetHash.FromBytes(sha256.ComputeHash(manifest));
        }

        public RemoteAssetDescriptor Anatomical { get; }

        public RemoteAssetDescriptor Inflated { get; }

        public AssetHash ManifestHash { get; }
    }

    public sealed class InMemoryRemoteAssetProvider : IDisposable
    {
        private readonly object m_Gate = new();
        private readonly Dictionary<AssetHash, ProviderEntry> m_Entries = new();

        public int Count
        {
            get
            {
                lock (m_Gate)
                    return m_Entries.Count;
            }
        }

        public void Publish(RemoteAssetDescriptor descriptor, byte[] payload)
        {
            if (descriptor == null)
                throw new ArgumentNullException(nameof(descriptor));
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));
            byte[] ownedPayload = (byte[])payload.Clone();
            if (ownedPayload.Length != descriptor.EncodedBytes || ComputeHash(ownedPayload) != descriptor.Asset.Hash)
            {
                Array.Clear(ownedPayload, 0, ownedPayload.Length);
                throw new InvalidDataException("The provider payload does not match its descriptor.");
            }

            lock (m_Gate)
            {
                if (m_Entries.TryGetValue(descriptor.Asset.Hash, out ProviderEntry existing))
                {
                    if (!descriptor.IsCompatibleWith(existing.Descriptor) || !BytesEqual(existing.Payload, ownedPayload))
                        throw new InvalidOperationException("The immutable asset hash is already published with different content or metadata.");
                    Array.Clear(ownedPayload, 0, ownedPayload.Length);
                    return;
                }

                m_Entries.Add(descriptor.Asset.Hash, new ProviderEntry(descriptor, ownedPayload));
            }
        }

        public RemoteAssetDescriptor GetDescriptor(AssetHash hash)
        {
            lock (m_Gate)
                return GetEntry(hash).Descriptor;
        }

        public byte[] ReadRange(AssetHash hash, int offset, int count)
        {
            lock (m_Gate)
            {
                ProviderEntry entry = GetEntry(hash);
                if (offset < 0 || count <= 0 || offset > entry.Payload.Length - count)
                    throw new ArgumentOutOfRangeException(nameof(offset));

                var range = new byte[count];
                Buffer.BlockCopy(entry.Payload, offset, range, 0, count);
                return range;
            }
        }

        public void Remove(AssetHash hash)
        {
            lock (m_Gate)
            {
                if (!m_Entries.TryGetValue(hash, out ProviderEntry entry))
                    return;
                m_Entries.Remove(hash);
                Array.Clear(entry.Payload, 0, entry.Payload.Length);
            }
        }

        public void Dispose()
        {
            lock (m_Gate)
            {
                foreach (ProviderEntry entry in m_Entries.Values)
                    Array.Clear(entry.Payload, 0, entry.Payload.Length);
                m_Entries.Clear();
            }
        }

        private ProviderEntry GetEntry(AssetHash hash)
        {
            if (!m_Entries.TryGetValue(hash, out ProviderEntry entry))
                throw new KeyNotFoundException("The asset hash is not published.");
            return entry;
        }

        private static AssetHash ComputeHash(byte[] payload)
        {
            using SHA256 sha256 = SHA256.Create();
            return AssetHash.FromBytes(sha256.ComputeHash(payload));
        }

        private static bool BytesEqual(byte[] left, byte[] right)
        {
            if (left.Length != right.Length)
                return false;
            int difference = 0;
            for (int index = 0; index < left.Length; index++)
                difference |= left[index] ^ right[index];
            return difference == 0;
        }

        private sealed class ProviderEntry
        {
            public ProviderEntry(RemoteAssetDescriptor descriptor, byte[] payload)
            {
                Descriptor = descriptor;
                Payload = payload;
            }

            public RemoteAssetDescriptor Descriptor { get; }

            public byte[] Payload { get; }
        }
    }

    public enum AssetTransferStartStatus : byte
    {
        Started = 1,
        JoinedExisting = 2,
        AlreadyAvailable = 3,
        AssetExceedsBudget = 4,
        MemoryPressureRequiresUserAction = 5,
    }

    public sealed class AssetTransferStartResult
    {
        internal AssetTransferStartResult(AssetTransferStartStatus status, RemoteAssetTransfer transfer)
        {
            Status = status;
            Transfer = transfer;
        }

        public AssetTransferStartStatus Status { get; }

        public RemoteAssetTransfer Transfer { get; }

        public bool RequiresUserAction => Status == AssetTransferStartStatus.AssetExceedsBudget || Status == AssetTransferStartStatus.MemoryPressureRequiresUserAction;
    }

    public enum AssetTransferCompletion : byte
    {
        Incomplete = 1,
        Published = 2,
        Corrupt = 3,
        Cancelled = 4,
    }

    public readonly struct AssetRange
    {
        public AssetRange(int offset, int count)
        {
            Offset = offset;
            Count = count;
        }

        public int Offset { get; }

        public int Count { get; }
    }

    public enum AssetCacheLifecycleEvent : byte
    {
        ConnectionInterrupted = 1,
        ResumeLeaseExpired = 2,
        NewEpoch = 3,
        Backgrounded = 4,
        Closed = 5,
    }

    public sealed class AssetCacheLifecycleResult
    {
        internal AssetCacheLifecycleResult(int cancelledTransfers, int purgedInactiveAssets, int activeAssetsAwaitingExplicitRelease)
        {
            CancelledTransfers = cancelledTransfers;
            PurgedInactiveAssets = purgedInactiveAssets;
            ActiveAssetsAwaitingExplicitRelease = activeAssetsAwaitingExplicitRelease;
        }

        public int CancelledTransfers { get; }

        public int PurgedInactiveAssets { get; }

        public int ActiveAssetsAwaitingExplicitRelease { get; }
    }

    public sealed class InMemoryRemoteAssetCache : IDisposable
    {
        private readonly object m_Gate = new();
        private readonly Dictionary<AssetHash, CacheEntry> m_Entries = new();
        private readonly Dictionary<AssetHash, RemoteAssetTransfer> m_Transfers = new();
        private readonly long m_ByteBudget;
        private readonly RemoteAssetCapabilities m_Capabilities;
        private long m_Clock;
        private long m_CommittedBytes;
        private long m_StagingBytes;
        private long m_EvictedAssetCount;
        private bool m_Closed;

        public InMemoryRemoteAssetCache(long byteBudget, RemoteAssetCapabilities capabilities = null)
        {
            if (byteBudget <= 0)
                throw new ArgumentOutOfRangeException(nameof(byteBudget));
            m_ByteBudget = byteBudget;
            m_Capabilities = capabilities ?? RemoteAssetCapabilities.SafetyMaximums;
        }

        public long ByteBudget => m_ByteBudget;

        public RemoteAssetCapabilities Capabilities => m_Capabilities;

        public long CommittedBytes
        {
            get
            {
                lock (m_Gate)
                    return m_CommittedBytes;
            }
        }

        public long StagingBytes
        {
            get
            {
                lock (m_Gate)
                    return m_StagingBytes;
            }
        }

        public long ResidentBytes
        {
            get
            {
                lock (m_Gate)
                    return m_CommittedBytes + m_StagingBytes;
            }
        }

        public int StoredAssetCount
        {
            get
            {
                lock (m_Gate)
                    return m_Entries.Count;
            }
        }

        public int StagingTransferCount
        {
            get
            {
                lock (m_Gate)
                    return m_Transfers.Count;
            }
        }

        public long EvictedAssetCount
        {
            get
            {
                lock (m_Gate)
                    return m_EvictedAssetCount;
            }
        }

        public AssetTransferStartResult StartTransfer(RemoteAssetDescriptor descriptor)
        {
            if (descriptor == null)
                throw new ArgumentNullException(nameof(descriptor));
            lock (m_Gate)
            {
                ThrowIfClosed();
                m_Capabilities.Validate(descriptor);
                if (m_Entries.TryGetValue(descriptor.Asset.Hash, out CacheEntry entry))
                {
                    if (!descriptor.IsCompatibleWith(entry.Descriptor))
                        throw new InvalidOperationException("The cached asset hash has incompatible metadata.");
                    return new AssetTransferStartResult(AssetTransferStartStatus.AlreadyAvailable, null);
                }

                if (m_Transfers.TryGetValue(descriptor.Asset.Hash, out RemoteAssetTransfer existing))
                {
                    if (!descriptor.IsCompatibleWith(existing.Descriptor))
                        throw new InvalidOperationException("The staged asset hash has incompatible metadata.");
                    return new AssetTransferStartResult(AssetTransferStartStatus.JoinedExisting, existing);
                }

                if (descriptor.EncodedBytes > m_ByteBudget)
                    return new AssetTransferStartResult(AssetTransferStartStatus.AssetExceedsBudget, null);
                if (!MakeRoom(descriptor.EncodedBytes))
                    return new AssetTransferStartResult(AssetTransferStartStatus.MemoryPressureRequiresUserAction, null);

                var transfer = new RemoteAssetTransfer(this, descriptor);
                m_Transfers.Add(descriptor.Asset.Hash, transfer);
                m_StagingBytes += descriptor.EncodedBytes;
                return new AssetTransferStartResult(AssetTransferStartStatus.Started, transfer);
            }
        }

        public bool TryAcquire(AssetHash hash, out RemoteAssetLease lease)
        {
            lock (m_Gate)
            {
                ThrowIfClosed();
                if (!m_Entries.TryGetValue(hash, out CacheEntry entry) || entry.PurgePending)
                {
                    lease = null;
                    return false;
                }

                entry.ReferenceCount++;
                entry.LastAccess = ++m_Clock;
                lease = new RemoteAssetLease(this, hash, entry.Descriptor, entry.Payload);
                return true;
            }
        }

        public AssetCacheLifecycleResult ApplyLifecycle(AssetCacheLifecycleEvent lifecycleEvent)
        {
            lock (m_Gate)
            {
                if (lifecycleEvent < AssetCacheLifecycleEvent.ConnectionInterrupted || lifecycleEvent > AssetCacheLifecycleEvent.Closed)
                    throw new ArgumentOutOfRangeException(nameof(lifecycleEvent));
                if (lifecycleEvent == AssetCacheLifecycleEvent.ConnectionInterrupted)
                    return new AssetCacheLifecycleResult(0, 0, 0);

                int cancelled = CancelTransfers();
                int purged = 0;
                int active = 0;
                foreach (AssetHash hash in new List<AssetHash>(m_Entries.Keys))
                {
                    CacheEntry entry = m_Entries[hash];
                    if (entry.ReferenceCount == 0)
                    {
                        RemoveEntry(hash, entry);
                        purged++;
                    }
                    else
                    {
                        entry.PurgePending = true;
                        active++;
                    }
                }

                if (lifecycleEvent == AssetCacheLifecycleEvent.Closed)
                    m_Closed = true;
                return new AssetCacheLifecycleResult(cancelled, purged, active);
            }
        }

        public void Dispose()
        {
            ApplyLifecycle(AssetCacheLifecycleEvent.Closed);
        }

        internal void WriteChunk(RemoteAssetTransfer transfer, int chunkIndex, byte[] bytes)
        {
            lock (m_Gate)
            {
                EnsureCurrent(transfer);
                transfer.WriteChunkUnderLock(chunkIndex, bytes);
            }
        }

        internal AssetTransferCompletion Complete(RemoteAssetTransfer transfer)
        {
            lock (m_Gate)
            {
                if (transfer.ResultUnderLock.HasValue)
                    return transfer.ResultUnderLock.Value;
                EnsureCurrent(transfer);
                if (!transfer.IsCompleteUnderLock)
                    return AssetTransferCompletion.Incomplete;

                using SHA256 sha256 = SHA256.Create();
                AssetHash computed = AssetHash.FromBytes(sha256.ComputeHash(transfer.Payload));
                m_Transfers.Remove(transfer.Descriptor.Asset.Hash);
                m_StagingBytes -= transfer.Descriptor.EncodedBytes;
                if (computed != transfer.Descriptor.Asset.Hash)
                {
                    transfer.FinishUnderLock(AssetTransferCompletion.Corrupt, clearPayload: true);
                    return AssetTransferCompletion.Corrupt;
                }

                m_Entries.Add(transfer.Descriptor.Asset.Hash, new CacheEntry(transfer.Descriptor, transfer.DetachPayloadUnderLock(), ++m_Clock));
                m_CommittedBytes += transfer.Descriptor.EncodedBytes;
                transfer.FinishUnderLock(AssetTransferCompletion.Published, clearPayload: false);
                return AssetTransferCompletion.Published;
            }
        }

        internal void Cancel(RemoteAssetTransfer transfer)
        {
            lock (m_Gate)
            {
                if (transfer.ResultUnderLock.HasValue)
                    return;
                if (!m_Transfers.Remove(transfer.Descriptor.Asset.Hash))
                    return;
                m_StagingBytes -= transfer.Descriptor.EncodedBytes;
                transfer.FinishUnderLock(AssetTransferCompletion.Cancelled, clearPayload: true);
            }
        }

        internal void Release(AssetHash hash)
        {
            lock (m_Gate)
            {
                if (!m_Entries.TryGetValue(hash, out CacheEntry entry))
                    return;
                entry.ReferenceCount--;
                if (entry.ReferenceCount < 0)
                    throw new InvalidOperationException("The asset lease reference count became negative.");
                entry.LastAccess = ++m_Clock;
                if (entry.ReferenceCount == 0 && entry.PurgePending)
                    RemoveEntry(hash, entry);
            }
        }

        internal IReadOnlyList<AssetRange> GetMissingRanges(RemoteAssetTransfer transfer)
        {
            lock (m_Gate)
            {
                if (transfer.ResultUnderLock.HasValue)
                    return Array.Empty<AssetRange>();
                EnsureCurrent(transfer);
                return transfer.GetMissingRangesUnderLock();
            }
        }

        internal AssetTransferCompletion? GetResult(RemoteAssetTransfer transfer)
        {
            lock (m_Gate)
                return transfer.ResultUnderLock;
        }

        internal long GetDuplicateChunkCount(RemoteAssetTransfer transfer)
        {
            lock (m_Gate)
                return transfer.DuplicateChunkCountUnderLock;
        }

        private bool MakeRoom(int requiredBytes)
        {
            while (m_CommittedBytes + m_StagingBytes + requiredBytes > m_ByteBudget)
            {
                AssetHash oldestHash = default;
                CacheEntry oldest = null;
                foreach (KeyValuePair<AssetHash, CacheEntry> pair in m_Entries)
                {
                    if (pair.Value.ReferenceCount == 0 && (oldest == null || pair.Value.LastAccess < oldest.LastAccess))
                    {
                        oldestHash = pair.Key;
                        oldest = pair.Value;
                    }
                }

                if (oldest == null)
                    return false;
                RemoveEntry(oldestHash, oldest);
                m_EvictedAssetCount++;
            }

            return true;
        }

        private int CancelTransfers()
        {
            int count = m_Transfers.Count;
            foreach (RemoteAssetTransfer transfer in new List<RemoteAssetTransfer>(m_Transfers.Values))
            {
                m_Transfers.Remove(transfer.Descriptor.Asset.Hash);
                m_StagingBytes -= transfer.Descriptor.EncodedBytes;
                transfer.FinishUnderLock(AssetTransferCompletion.Cancelled, clearPayload: true);
            }

            return count;
        }

        private void RemoveEntry(AssetHash hash, CacheEntry entry)
        {
            m_Entries.Remove(hash);
            m_CommittedBytes -= entry.Payload.Length;
            Array.Clear(entry.Payload, 0, entry.Payload.Length);
        }

        private void EnsureCurrent(RemoteAssetTransfer transfer)
        {
            if (transfer == null)
                throw new ArgumentNullException(nameof(transfer));
            if (!m_Transfers.TryGetValue(transfer.Descriptor.Asset.Hash, out RemoteAssetTransfer current) || !ReferenceEquals(current, transfer))
                throw new InvalidOperationException("The asset transfer is no longer active.");
        }

        private void ThrowIfClosed()
        {
            if (m_Closed)
                throw new ObjectDisposedException(nameof(InMemoryRemoteAssetCache));
        }

        private sealed class CacheEntry
        {
            public CacheEntry(RemoteAssetDescriptor descriptor, byte[] payload, long lastAccess)
            {
                Descriptor = descriptor;
                Payload = payload;
                LastAccess = lastAccess;
            }

            public RemoteAssetDescriptor Descriptor { get; }

            public byte[] Payload { get; }

            public int ReferenceCount { get; set; }

            public long LastAccess { get; set; }

            public bool PurgePending { get; set; }
        }
    }

    public sealed class RemoteAssetTransfer : IDisposable
    {
        private readonly InMemoryRemoteAssetCache m_Owner;
        private readonly bool[] m_ReceivedChunks;
        private byte[] m_Payload;
        private int m_ReceivedCount;
        private long m_DuplicateChunkCount;
        private AssetTransferCompletion? m_Result;

        internal RemoteAssetTransfer(InMemoryRemoteAssetCache owner, RemoteAssetDescriptor descriptor)
        {
            m_Owner = owner;
            Descriptor = descriptor;
            m_Payload = new byte[descriptor.EncodedBytes];
            m_ReceivedChunks = new bool[descriptor.ChunkCount];
        }

        public RemoteAssetDescriptor Descriptor { get; }

        public long DuplicateChunkCount => m_Owner.GetDuplicateChunkCount(this);

        public AssetTransferCompletion? Result => m_Owner.GetResult(this);

        internal byte[] Payload => m_Payload;

        internal bool IsCompleteUnderLock => m_ReceivedCount == m_ReceivedChunks.Length;

        internal long DuplicateChunkCountUnderLock => m_DuplicateChunkCount;

        internal AssetTransferCompletion? ResultUnderLock => m_Result;

        public void WriteChunk(int chunkIndex, byte[] bytes)
        {
            m_Owner.WriteChunk(this, chunkIndex, bytes);
        }

        public IReadOnlyList<AssetRange> GetMissingRanges()
        {
            return m_Owner.GetMissingRanges(this);
        }

        internal IReadOnlyList<AssetRange> GetMissingRangesUnderLock()
        {
            var result = new List<AssetRange>();
            for (int chunkIndex = 0; chunkIndex < m_ReceivedChunks.Length; chunkIndex++)
            {
                if (m_ReceivedChunks[chunkIndex])
                    continue;
                int offset = checked(chunkIndex * Descriptor.ChunkBytes);
                int count = Math.Min(Descriptor.ChunkBytes, Descriptor.EncodedBytes - offset);
                result.Add(new AssetRange(offset, count));
            }

            return new ReadOnlyCollection<AssetRange>(result);
        }

        public AssetTransferCompletion Complete()
        {
            return m_Owner.Complete(this);
        }

        public void Cancel()
        {
            m_Owner.Cancel(this);
        }

        public void Dispose()
        {
            Cancel();
        }

        internal void WriteChunkUnderLock(int chunkIndex, byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));
            if (chunkIndex < 0 || chunkIndex >= m_ReceivedChunks.Length)
                throw new ArgumentOutOfRangeException(nameof(chunkIndex));
            int offset = checked(chunkIndex * Descriptor.ChunkBytes);
            int expectedBytes = Math.Min(Descriptor.ChunkBytes, Descriptor.EncodedBytes - offset);
            if (bytes.Length != expectedBytes)
                throw new ArgumentException("The range does not match the negotiated chunk boundaries.", nameof(bytes));

            if (m_ReceivedChunks[chunkIndex])
            {
                for (int index = 0; index < bytes.Length; index++)
                {
                    if (m_Payload[offset + index] != bytes[index])
                    {
                        m_Owner.Cancel(this);
                        throw new InvalidDataException("A duplicate range contains conflicting bytes.");
                    }
                }

                m_DuplicateChunkCount++;
                return;
            }

            Buffer.BlockCopy(bytes, 0, m_Payload, offset, bytes.Length);
            m_ReceivedChunks[chunkIndex] = true;
            m_ReceivedCount++;
        }

        internal byte[] DetachPayloadUnderLock()
        {
            byte[] payload = m_Payload;
            m_Payload = null;
            return payload;
        }

        internal void FinishUnderLock(AssetTransferCompletion result, bool clearPayload)
        {
            if (clearPayload && m_Payload != null)
                Array.Clear(m_Payload, 0, m_Payload.Length);
            m_Payload = null;
            m_Result = result;
        }
    }

    public sealed class RemoteAssetLease : IDisposable
    {
        private InMemoryRemoteAssetCache m_Owner;
        private readonly byte[] m_Payload;

        internal RemoteAssetLease(InMemoryRemoteAssetCache owner, AssetHash hash, RemoteAssetDescriptor descriptor, byte[] payload)
        {
            m_Owner = owner;
            Hash = hash;
            Descriptor = descriptor;
            m_Payload = payload;
        }

        public AssetHash Hash { get; }

        public RemoteAssetDescriptor Descriptor { get; }

        public Stream OpenRead()
        {
            if (Volatile.Read(ref m_Owner) == null)
                throw new ObjectDisposedException(nameof(RemoteAssetLease));
            return new MemoryStream(m_Payload, false);
        }

        public void Dispose()
        {
            InMemoryRemoteAssetCache owner = Interlocked.Exchange(ref m_Owner, null);
            if (owner == null)
                return;
            owner.Release(Hash);
        }
    }
}
