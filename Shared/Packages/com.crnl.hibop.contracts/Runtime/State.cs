using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CRNL.HiBoP.Contracts
{
    public sealed class StateProperty : IEquatable<StateProperty>
    {
        public StateProperty(PropertyKey key, ContractValue value)
        {
            if (!key.IsValid)
                throw new ArgumentException("A valid property key is required.", nameof(key));

            Key = key;
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        public PropertyKey Key { get; }

        public ContractValue Value { get; }

        public bool Equals(StateProperty other)
        {
            return !ReferenceEquals(other, null) && Key.Equals(other.Key) && Value.Equals(other.Value);
        }

        public override bool Equals(object obj) => Equals(obj as StateProperty);

        public override int GetHashCode()
        {
            unchecked
            {
                return (Key.GetHashCode() * 397) ^ Value.GetHashCode();
            }
        }

        public override string ToString() => $"StateProperty(key={Key}, valueKind={Value.Kind})";
    }

    public sealed class ScopeState
    {
        private readonly ReadOnlyCollection<StateProperty> m_Properties;

        public ScopeState(ScopeKey scope, ScopeRevision revision, IEnumerable<StateProperty> properties)
        {
            if (!scope.IsValid)
                throw new ArgumentException("A valid scope is required.", nameof(scope));
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));

            List<StateProperty> copy = new(properties);
            if (copy.Exists(property => property == null))
                throw new ArgumentException("Properties cannot contain null.", nameof(properties));

            copy.Sort((left, right) => left.Key.CompareTo(right.Key));
            EnsureUniqueProperties(copy, nameof(properties));

            Scope = scope;
            Revision = revision;
            m_Properties = copy.AsReadOnly();
        }

        public ScopeKey Scope { get; }

        public ScopeRevision Revision { get; }

        public IReadOnlyList<StateProperty> Properties => m_Properties;

        public override string ToString()
        {
            return $"ScopeState(scope={Scope}, revision={Revision}, propertyCount={m_Properties.Count})";
        }

        internal static void EnsureUniqueProperties<T>(IReadOnlyList<T> properties, string parameterName) where T : class
        {
            for (int index = 1; index < properties.Count; index++)
            {
                PropertyKey previous = GetPropertyKey(properties[index - 1]);
                PropertyKey current = GetPropertyKey(properties[index]);
                if (previous == current)
                    throw new ArgumentException($"Property key {current.Value} occurs more than once.", parameterName);
            }
        }

        private static PropertyKey GetPropertyKey<T>(T property) where T : class
        {
            if (property is StateProperty stateProperty)
                return stateProperty.Key;
            if (property is PropertyChange propertyChange)
                return propertyChange.Key;
            throw new ArgumentException("Unsupported property type.");
        }
    }

    public sealed class SessionSnapshot
    {
        private readonly ReadOnlyCollection<AssetReference> m_Assets;
        private readonly ReadOnlyCollection<ScopeState> m_Scopes;

        public SessionSnapshot(ContractVersion contractVersion, SessionEpoch session, StateRevision stateRevision, IEnumerable<ScopeState> scopes, IEnumerable<AssetReference> assets)
        {
            if (!contractVersion.IsValid)
                throw new ArgumentException("A valid contract version is required.", nameof(contractVersion));
            if (!session.IsValid)
                throw new ArgumentException("A valid session epoch is required.", nameof(session));
            if (scopes == null)
                throw new ArgumentNullException(nameof(scopes));
            if (assets == null)
                throw new ArgumentNullException(nameof(assets));

            List<ScopeState> copy = new(scopes);
            if (copy.Exists(scope => scope == null))
                throw new ArgumentException("Scopes cannot contain null.", nameof(scopes));

            copy.Sort((left, right) => left.Scope.CompareTo(right.Scope));
            for (int index = 1; index < copy.Count; index++)
            {
                if (copy[index - 1].Scope == copy[index].Scope)
                    throw new ArgumentException($"Scope {copy[index].Scope} occurs more than once.", nameof(scopes));
            }

            List<AssetReference> assetCopy = new(assets);
            if (assetCopy.Exists(asset => asset == null))
                throw new ArgumentException("Assets cannot contain null.", nameof(assets));
            assetCopy.Sort();
            for (int index = 1; index < assetCopy.Count; index++)
            {
                if (assetCopy[index - 1].Hash == assetCopy[index].Hash)
                    throw new ArgumentException($"Asset hash {assetCopy[index].Hash} occurs more than once.", nameof(assets));
            }

            HashSet<ContractId> assetIds = new();
            for (int index = 0; index < assetCopy.Count; index++)
            {
                if (!assetIds.Add(assetCopy[index].AssetId))
                    throw new ArgumentException($"Asset ID {assetCopy[index].AssetId} occurs more than once.", nameof(assets));
            }

            ContractVersion = contractVersion;
            Session = session;
            StateRevision = stateRevision;
            m_Scopes = copy.AsReadOnly();
            m_Assets = assetCopy.AsReadOnly();
        }

        public ContractVersion ContractVersion { get; }

        public SessionEpoch Session { get; }

        public StateRevision StateRevision { get; }

        public IReadOnlyList<ScopeState> Scopes => m_Scopes;

        public IReadOnlyList<AssetReference> Assets => m_Assets;

        public override string ToString()
        {
            return $"SessionSnapshot(version={ContractVersion}, session={Session}, stateRevision={StateRevision}, scopeCount={m_Scopes.Count}, assetCount={m_Assets.Count})";
        }
    }

    public enum AssetChangeKind : byte
    {
        Unknown = 0,
        Add = 1,
        Remove = 2,
    }

    public sealed class AssetChange : IComparable<AssetChange>
    {
        private AssetChange(AssetChangeKind kind, AssetHash hash, Optional<AssetReference> asset)
        {
            Kind = kind;
            Hash = hash;
            Asset = asset;
        }

        public AssetChangeKind Kind { get; }

        public AssetHash Hash { get; }

        public Optional<AssetReference> Asset { get; }

        public static AssetChange Add(AssetReference asset)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));
            return new AssetChange(AssetChangeKind.Add, asset.Hash, Optional<AssetReference>.Some(asset));
        }

        public static AssetChange Remove(AssetHash hash)
        {
            if (!hash.IsValid)
                throw new ArgumentException("A valid asset hash is required.", nameof(hash));
            return new AssetChange(AssetChangeKind.Remove, hash, Optional<AssetReference>.None);
        }

        public int CompareTo(AssetChange other)
        {
            return ReferenceEquals(other, null) ? 1 : Hash.CompareTo(other.Hash);
        }

        public override string ToString()
        {
            return $"AssetChange(kind={Kind}, hash={Hash})";
        }
    }

    public enum PropertyChangeKind : byte
    {
        Unknown = 0,
        Set = 1,
        Remove = 2,
    }

    public sealed class PropertyChange
    {
        private PropertyChange(PropertyChangeKind kind, PropertyKey key, Optional<ContractValue> value)
        {
            Kind = kind;
            Key = key;
            Value = value;
        }

        public PropertyChangeKind Kind { get; }

        public PropertyKey Key { get; }

        public Optional<ContractValue> Value { get; }

        public static PropertyChange Set(PropertyKey key, ContractValue value)
        {
            if (!key.IsValid)
                throw new ArgumentException("A valid property key is required.", nameof(key));
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            return new PropertyChange(PropertyChangeKind.Set, key, Optional<ContractValue>.Some(value));
        }

        public static PropertyChange Remove(PropertyKey key)
        {
            if (!key.IsValid)
                throw new ArgumentException("A valid property key is required.", nameof(key));
            return new PropertyChange(PropertyChangeKind.Remove, key, Optional<ContractValue>.None);
        }

        public override string ToString()
        {
            return $"PropertyChange(kind={Kind}, key={Key})";
        }
    }

    public enum ScopeDeltaKind : byte
    {
        Unknown = 0,
        Update = 1,
        Remove = 2,
    }

    public sealed class ScopeDelta
    {
        private readonly ReadOnlyCollection<PropertyChange> m_Changes;

        public ScopeDelta(ScopeKey scope, ScopeRevision baseRevision, ScopeRevision resultingRevision, IEnumerable<PropertyChange> changes) : this(ScopeDeltaKind.Update, scope, baseRevision, resultingRevision, changes)
        {
        }

        private ScopeDelta(ScopeDeltaKind kind, ScopeKey scope, ScopeRevision baseRevision, ScopeRevision resultingRevision, IEnumerable<PropertyChange> changes)
        {
            if (!scope.IsValid)
                throw new ArgumentException("A valid scope is required.", nameof(scope));
            if (resultingRevision <= baseRevision)
                throw new ArgumentException("A scope delta must advance its revision.", nameof(resultingRevision));
            if (changes == null)
                throw new ArgumentNullException(nameof(changes));

            List<PropertyChange> copy = new(changes);
            if (kind == ScopeDeltaKind.Update && copy.Count == 0)
                throw new ArgumentException("A scope delta must contain at least one change.", nameof(changes));
            if (kind == ScopeDeltaKind.Remove && copy.Count != 0)
                throw new ArgumentException("A removed scope cannot contain property changes.", nameof(changes));
            if (copy.Exists(change => change == null))
                throw new ArgumentException("Changes cannot contain null.", nameof(changes));

            copy.Sort((left, right) => left.Key.CompareTo(right.Key));
            ScopeState.EnsureUniqueProperties(copy, nameof(changes));

            Kind = kind;
            Scope = scope;
            BaseRevision = baseRevision;
            ResultingRevision = resultingRevision;
            m_Changes = copy.AsReadOnly();
        }

        public ScopeDeltaKind Kind { get; }

        public ScopeKey Scope { get; }

        public ScopeRevision BaseRevision { get; }

        public ScopeRevision ResultingRevision { get; }

        public IReadOnlyList<PropertyChange> Changes => m_Changes;

        public static ScopeDelta Remove(ScopeKey scope, ScopeRevision baseRevision, ScopeRevision resultingRevision)
        {
            return new ScopeDelta(ScopeDeltaKind.Remove, scope, baseRevision, resultingRevision, Array.Empty<PropertyChange>());
        }

        public override string ToString()
        {
            return $"ScopeDelta(kind={Kind}, scope={Scope}, base={BaseRevision}, resulting={ResultingRevision}, changeCount={m_Changes.Count})";
        }
    }

    public sealed class StateDelta
    {
        private readonly ReadOnlyCollection<AssetChange> m_Assets;
        private readonly ReadOnlyCollection<ScopeDelta> m_Scopes;

        public StateDelta(SessionEpoch session, StateRevision baseStateRevision, StateRevision resultingStateRevision, IEnumerable<ScopeDelta> scopes, IEnumerable<AssetChange> assets)
        {
            if (!session.IsValid)
                throw new ArgumentException("A valid session epoch is required.", nameof(session));
            if (resultingStateRevision <= baseStateRevision)
                throw new ArgumentException("A state delta must advance its revision.", nameof(resultingStateRevision));
            if (scopes == null)
                throw new ArgumentNullException(nameof(scopes));
            if (assets == null)
                throw new ArgumentNullException(nameof(assets));

            List<ScopeDelta> copy = new(scopes);
            if (copy.Exists(scope => scope == null))
                throw new ArgumentException("Scope deltas cannot contain null.", nameof(scopes));

            copy.Sort((left, right) => left.Scope.CompareTo(right.Scope));
            for (int index = 1; index < copy.Count; index++)
            {
                if (copy[index - 1].Scope == copy[index].Scope)
                    throw new ArgumentException($"Scope {copy[index].Scope} occurs more than once.", nameof(scopes));
            }

            List<AssetChange> assetCopy = new(assets);
            if (assetCopy.Exists(asset => asset == null))
                throw new ArgumentException("Asset changes cannot contain null.", nameof(assets));
            assetCopy.Sort();
            for (int index = 1; index < assetCopy.Count; index++)
            {
                if (assetCopy[index - 1].Hash == assetCopy[index].Hash)
                    throw new ArgumentException($"Asset hash {assetCopy[index].Hash} changes more than once.", nameof(assets));
            }

            if (copy.Count == 0 && assetCopy.Count == 0)
                throw new ArgumentException("A state delta must contain at least one scope or asset change.");

            Session = session;
            BaseStateRevision = baseStateRevision;
            ResultingStateRevision = resultingStateRevision;
            m_Scopes = copy.AsReadOnly();
            m_Assets = assetCopy.AsReadOnly();
        }

        public SessionEpoch Session { get; }

        public StateRevision BaseStateRevision { get; }

        public StateRevision ResultingStateRevision { get; }

        public IReadOnlyList<ScopeDelta> Scopes => m_Scopes;

        public IReadOnlyList<AssetChange> Assets => m_Assets;

        public override string ToString()
        {
            return $"StateDelta(session={Session}, base={BaseStateRevision}, resulting={ResultingStateRevision}, scopeCount={m_Scopes.Count}, assetCount={m_Assets.Count})";
        }
    }
}
