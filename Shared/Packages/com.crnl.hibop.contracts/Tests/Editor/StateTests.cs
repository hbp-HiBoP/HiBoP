using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace CRNL.HiBoP.Contracts.Tests
{
    public class StateTests
    {
        [Test]
        public void SnapshotNormalizesScopeAndPropertyOrder()
        {
            ScopeState later = Scope(ScopeType.Timeline, 2, new StateProperty(V1PropertyKeys.TimelineSpeed, ContractValue.FromNumber(1.0)), new StateProperty(V1PropertyKeys.TimelineLogicalTime, ContractValue.FromNumber(4.0)));
            ScopeState earlier = Scope(ScopeType.Project, 1, new StateProperty(V1PropertyKeys.ProjectVisualizationMembership, ContractValue.FromIds(Array.Empty<ContractId>())));

            SessionSnapshot snapshot = new(ContractVersion.V1, new SessionEpoch(new ContractId(10, 20), 1), new StateRevision(8), new[] { later, earlier }, Array.Empty<AssetReference>());

            Assert.That(snapshot.Scopes[0].Scope.Type, Is.EqualTo(ScopeType.Project));
            Assert.That(snapshot.Scopes[1].Scope.Type, Is.EqualTo(ScopeType.Timeline));
            Assert.That(snapshot.Scopes[1].Properties[0].Key, Is.EqualTo(V1PropertyKeys.TimelineLogicalTime));
            Assert.That(snapshot.Scopes[1].Properties[1].Key, Is.EqualTo(V1PropertyKeys.TimelineSpeed));
        }

        [Test]
        public void SnapshotCopiesCallerCollections()
        {
            List<StateProperty> properties = new()
            {
                new StateProperty(V1PropertyKeys.TimelineLooping, ContractValue.FromBoolean(false)),
            };
            ScopeState scope = new(new ScopeKey(ScopeType.Timeline, new ContractId(4, 4)), new ScopeRevision(1), properties);
            properties.Clear();

            List<ScopeState> scopes = new() { scope };
            SessionSnapshot snapshot = new(ContractVersion.V1, new SessionEpoch(new ContractId(1, 1), 1), new StateRevision(1), scopes, Array.Empty<AssetReference>());
            scopes.Clear();

            Assert.That(scope.Properties, Has.Count.EqualTo(1));
            Assert.That(snapshot.Scopes, Has.Count.EqualTo(1));
        }

        [Test]
        public void DuplicatePropertiesAndScopesAreRejected()
        {
            StateProperty property = new(V1PropertyKeys.TimelineLooping, ContractValue.FromBoolean(false));
            ScopeKey key = new(ScopeType.Timeline, new ContractId(2, 2));

            Assert.Throws<ArgumentException>(() => _ = new ScopeState(key, new ScopeRevision(1), new[] { property, property }));

            ScopeState scope = new(key, new ScopeRevision(1), new[] { property });
            Assert.Throws<ArgumentException>(() => _ = new SessionSnapshot(ContractVersion.V1, new SessionEpoch(new ContractId(1, 1), 1), new StateRevision(1), new[] { scope, scope }, Array.Empty<AssetReference>()));
        }

        [Test]
        public void DeltasMustAdvanceAndCannotChangePropertyTwice()
        {
            ScopeKey key = new(ScopeType.Column, new ContractId(5, 5));
            PropertyChange change = PropertyChange.Set(V1PropertyKeys.ColumnActivityOpacity, ContractValue.FromNumber(0.5));

            Assert.Throws<ArgumentException>(() => _ = new ScopeDelta(key, new ScopeRevision(2), new ScopeRevision(2), new[] { change }));
            Assert.Throws<ArgumentException>(() => _ = new ScopeDelta(key, new ScopeRevision(2), new ScopeRevision(3), new[] { change, PropertyChange.Remove(V1PropertyKeys.ColumnActivityOpacity) }));
        }

        [Test]
        public void SnapshotAssetInventoryIsImmutableAndRejectsDuplicateHashes()
        {
            AssetHash hash = new(1, 2, 3, 4);
            AssetReference first = new(new ContractId(9, 1), hash, 1);
            AssetReference duplicateHash = new(new ContractId(9, 2), hash, 1);
            List<AssetReference> assets = new() { first };

            SessionSnapshot snapshot = new(ContractVersion.V1, new SessionEpoch(new ContractId(1, 1), 1), new StateRevision(1), Array.Empty<ScopeState>(), assets);
            assets.Clear();

            Assert.That(snapshot.Assets, Has.Count.EqualTo(1));
            Assert.Throws<ArgumentException>(() => _ = new SessionSnapshot(ContractVersion.V1, new SessionEpoch(new ContractId(1, 1), 1), new StateRevision(1), Array.Empty<ScopeState>(), new[] { first, duplicateHash }));
        }

        [Test]
        public void StateDeltaCanRemoveScopeAndAssetExplicitly()
        {
            ScopeDelta removedScope = ScopeDelta.Remove(new ScopeKey(ScopeType.Cut, new ContractId(7, 7)), new ScopeRevision(3), new ScopeRevision(4));
            AssetChange removedAsset = AssetChange.Remove(new AssetHash(5, 6, 7, 8));

            StateDelta delta = new(new SessionEpoch(new ContractId(1, 1), 1), new StateRevision(10), new StateRevision(11), new[] { removedScope }, new[] { removedAsset });

            Assert.That(delta.Scopes[0].Kind, Is.EqualTo(ScopeDeltaKind.Remove));
            Assert.That(delta.Scopes[0].Changes, Is.Empty);
            Assert.That(delta.Assets[0].Kind, Is.EqualTo(AssetChangeKind.Remove));
            Assert.That(delta.Assets[0].Asset.HasValue, Is.False);
        }

        private static ScopeState Scope(ScopeType type, ulong id, params StateProperty[] properties)
        {
            return new ScopeState(new ScopeKey(type, new ContractId(0, id)), new ScopeRevision(1), properties);
        }
    }
}
