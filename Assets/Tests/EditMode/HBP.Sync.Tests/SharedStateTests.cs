using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HBP.Sync;
using NUnit.Framework;

namespace HBP.Tests.Sync
{
    public class SharedStateTests
    {
        private static readonly Guid Epoch = Guid.Parse("00112233-4455-6677-8899-aabbccddeeff");
        private static StateKey Cut(string id, ushort field) => new StateKey(EntityKind.Cut, "", id, field);
        private static StateKey Sphere(string roi, string id, ushort field) => new StateKey(EntityKind.Sphere, roi, id, field);
        private static StateKey Roi(string id, ushort field) => new StateKey(EntityKind.Roi, "", id, field);

        private static StateSnapshot Snapshot(ulong revision, params (StateKey key, byte[] value)[] fields) => new StateSnapshot(Epoch, "visualization-1", new string('a', 64), revision, fields.Select(field => new KeyValuePair<StateKey, byte[]>(field.key, field.value)));

        private static StateSnapshot Baseline() => Snapshot(7, (Cut("c1", 1), StateValue.Bool(true)), (Cut("c1", 2), StateValue.Int(0)), (Cut("c1", 3), StateValue.Int(0)), (Cut("c1", 4), StateValue.Vector3(0, 1, 0)), (Cut("c1", 5), StateValue.Bool(false)), (Cut("c1", 6), StateValue.Float(0)), (Cut("c2", 1), StateValue.Bool(true)), (Cut("c2", 2), StateValue.Int(1)), (Cut("c2", 6), StateValue.Float(0)));

        private static StateSnapshot Change(StateSnapshot state, ulong revision, StateKey key, byte[] value)
        {
            var fields = new SortedDictionary<StateKey, byte[]>(state.Fields) { [key] = value };
            return state.WithFields(fields, revision);
        }

        [Test]
        public void IndependentEditsMergeFromHistoricalBase()
        {
            StateSnapshot b = Baseline();
            StateSnapshot d = Change(b, 9, Cut("c1", 6), StateValue.Float(0.25f));
            StateSnapshot q = Change(b, 7, Cut("c2", 6), StateValue.Float(0.5f));
            MergeResult result = ThreeWayStateMerge.Merge(b, d, q, ThreeWayStateMerge.Hash(b));
            Assert.That(result.Conflicts, Is.Empty);
            Assert.That(result.Merged.Fields[Cut("c1", 6)], Is.EqualTo(d.Fields[Cut("c1", 6)]));
            Assert.That(result.Merged.Fields[Cut("c2", 6)], Is.EqualTo(q.Fields[Cut("c2", 6)]));
        }

        [Test]
        public void IdenticalAssignmentIsAcceptedOnce()
        {
            StateSnapshot b = Baseline();
            byte[] value = StateValue.Float(0.25f);
            MergeResult result = ThreeWayStateMerge.Merge(b, Change(b, 8, Cut("c1", 6), value), Change(b, 7, Cut("c1", 6), value), ThreeWayStateMerge.Hash(b));
            Assert.That(result.Conflicts, Is.Empty);
            Assert.That(result.Merged.Fields[Cut("c1", 6)], Is.EqualTo(value));
        }

        [Test]
        public void CutGeometryIsOneAtomicGroup()
        {
            StateSnapshot b = Baseline();
            StateSnapshot d = Change(b, 8, Cut("c1", 5), StateValue.Bool(true));
            StateSnapshot q = Change(b, 7, Cut("c1", 6), StateValue.Float(0.25f));
            MergeResult result = ThreeWayStateMerge.Merge(b, d, q, ThreeWayStateMerge.Hash(b));
            Assert.That(result.Conflicts, Has.Count.EqualTo(1));
            Assert.That(result.Conflicts[0].Reason, Is.EqualTo(ConflictReason.SameGroup));
            Assert.That(result.Conflicts[0].Quest[Cut("c1", 6)], Is.EqualTo(q.Fields[Cut("c1", 6)]));
            Assert.That(result.Merged.Fields[Cut("c1", 6)], Is.EqualTo(b.Fields[Cut("c1", 6)]));
        }

        [Test]
        public void DeleteVersusEditRetainsTombstoneAndBothCandidates()
        {
            StateSnapshot b = Baseline();
            StateSnapshot d = Change(b, 8, Cut("c1", 1), StateValue.Bool(false));
            StateSnapshot q = Change(b, 7, Cut("c1", 6), StateValue.Float(0.5f));
            MergeResult result = ThreeWayStateMerge.Merge(b, d, q, ThreeWayStateMerge.Hash(b));
            Assert.That(result.Conflicts.Any(c => c.Reason == ConflictReason.DeleteVersusEdit), Is.True);
            Assert.That(result.Merged.Fields[Cut("c1", 1)], Is.EqualTo(StateValue.Bool(false)));
            Assert.That(result.Conflicts.Any(c => c.Quest.ContainsKey(Cut("c1", 6))), Is.True);
        }

        [Test]
        public void ParentDeleteConflictsWithSphereEdit()
        {
            StateSnapshot b = Snapshot(7, (Roi("r1", 1), StateValue.Bool(true)), (Sphere("r1", "s1", 1), StateValue.Bool(true)), (Sphere("r1", "s1", 3), StateValue.Vector3(0, 0, 0)));
            StateSnapshot d = Change(b, 8, Roi("r1", 1), StateValue.Bool(false));
            StateSnapshot q = Change(b, 7, Sphere("r1", "s1", 3), StateValue.Vector3(1, 0, 0));
            MergeResult result = ThreeWayStateMerge.Merge(b, d, q, ThreeWayStateMerge.Hash(b));
            Assert.That(result.Conflicts.Any(c => c.Entity == EntityKind.Sphere && c.Reason == ConflictReason.DeleteVersusEdit), Is.True);
            Assert.That(result.Merged.Fields[Sphere("r1", "s1", 3)], Is.EqualTo(b.Fields[Sphere("r1", "s1", 3)]));
        }

        [Test]
        public void WrongBaseAndUnknownSchemaAreRejected()
        {
            StateSnapshot b = Baseline();
            Assert.Throws<InvalidDataException>(() => ThreeWayStateMerge.Merge(b, b, b, new string('0', 64)));
            StateSnapshot wrongVersion = new StateSnapshot(Epoch, "visualization-1", new string('a', 64), 7, b.Fields, 2);
            Assert.Throws<InvalidDataException>(() => SharedStateCodec.Encode(wrongVersion));
        }

        [Test]
        public void CodecIsDeterministicAndRejectsTrailingData()
        {
            StateSnapshot b = Baseline();
            byte[] encoded = SharedStateCodec.Encode(b);
            StateSnapshot decoded = SharedStateCodec.Decode(encoded);
            Assert.That(SharedStateCodec.Encode(decoded), Is.EqualTo(encoded));
            Assert.Throws<InvalidDataException>(() => SharedStateCodec.Decode(encoded.Concat(new byte[] { 1 }).ToArray()));
        }

        [Test]
        public void ReplicaDeltaRoundTripsAssignmentsRemovalsAndRejectsStaleBase()
        {
            StateKey changed = new StateKey(EntityKind.Scene, "", "", 3);
            StateKey removed = new StateKey(EntityKind.Scene, "", "", 5);
            StateSnapshot before = Snapshot(7, (changed, StateValue.Bool(false)), (removed, StateValue.Bool(true)));
            StateSnapshot after = Snapshot(8, (changed, StateValue.Bool(true)));
            ReplicaDelta delta = ReplicaDelta.Decode(ReplicaDelta.Between(before, after).Encode());
            Assert.That(SharedStateCodec.Encode(delta.Apply(before)), Is.EqualTo(SharedStateCodec.Encode(after)));
            Assert.Throws<InvalidDataException>(() => delta.Apply(after));
        }

        [Test]
        public void UnknownFieldAndNonFiniteFloatAreRejected()
        {
            Assert.Throws<InvalidDataException>(() => SharedStateCodec.Encode(Snapshot(7, (Cut("c1", 500), StateValue.Int(1)))));
            Assert.Throws<ArgumentOutOfRangeException>(() => StateValue.Float(float.NaN));
        }

        [Test]
        public void DanglingReferenceAndOutOfRangeValueAreRejected()
        {
            Assert.Throws<InvalidDataException>(() => SharedStateCodec.Encode(Snapshot(7, (new StateKey(EntityKind.Scene, "", "", 2), StateValue.Text("missing-roi")))));
            Assert.Throws<InvalidDataException>(() => SharedStateCodec.Encode(Snapshot(7, (new StateKey(EntityKind.Scene, "", "", 26), StateValue.Float(2)))));
        }

        [Test]
        public void IndependentlyValidBranchesCannotProduceDanglingSelection()
        {
            StateKey selection = new StateKey(EntityKind.Scene, "", "", 2);
            StateSnapshot b = Snapshot(7, (Roi("r1", 1), StateValue.Bool(true)), (Roi("r2", 1), StateValue.Bool(true)), (selection, StateValue.Text("r1")));
            StateSnapshot d = Change(b, 8, selection, StateValue.Text("r2"));
            StateSnapshot q = Change(b, 7, Roi("r2", 1), StateValue.Bool(false));
            Assert.Throws<InvalidDataException>(() => ThreeWayStateMerge.Merge(b, d, q, ThreeWayStateMerge.Hash(b)));
        }

        [Test]
        public void SnapshotDoesNotExposeMutableBackingValues()
        {
            StateSnapshot state = Baseline();
            string hash = ThreeWayStateMerge.Hash(state);
            var exposed = state.Fields;
            exposed[Cut("c1", 1)][0] = 0;
            exposed.Remove(Cut("c2", 1));
            Assert.That(ThreeWayStateMerge.Hash(state), Is.EqualTo(hash));
        }

        [Test]
        public void TopologyChangeConflictsWithOtherBranchCutEdit()
        {
            StateSnapshot b = Baseline();
            StateKey mesh = new StateKey(EntityKind.Scene, "", "", 11);
            StateSnapshot d = Change(b, 8, mesh, StateValue.Text("mesh:" + new string('b', 64) + ":1"));
            StateSnapshot q = Change(b, 7, Cut("c1", 6), StateValue.Float(0.75f));
            MergeResult result = ThreeWayStateMerge.Merge(b, d, q, ThreeWayStateMerge.Hash(b));
            Assert.That(result.Conflicts.Any(c => c.Reason == ConflictReason.ResourceDependency && c.Entity == EntityKind.Cut), Is.True);
            Assert.That(result.Merged.Fields[Cut("c1", 6)], Is.EqualTo(b.Fields[Cut("c1", 6)]));
        }

        [Test]
        public void DivergentTopologiesKeepDependentQuestCutPending()
        {
            StateKey mesh = new StateKey(EntityKind.Scene, "", "", 11);
            StateSnapshot b = Baseline();
            StateSnapshot d = Change(b, 8, mesh, StateValue.Text("mesh:" + new string('b', 64) + ":1"));
            StateSnapshot q = Change(b, 7, mesh, StateValue.Text("mesh:" + new string('c', 64) + ":1"));
            q = Change(q, 7, Cut("c1", 6), StateValue.Float(1f));
            MergeResult result = ThreeWayStateMerge.Merge(b, d, q, ThreeWayStateMerge.Hash(b));
            Assert.That(result.Conflicts.Any(c => c.Entity == EntityKind.Scene && c.Group == 9), Is.True);
            Assert.That(result.Conflicts.Any(c => c.Entity == EntityKind.Cut && c.Reason == ConflictReason.ResourceDependency), Is.True);
            Assert.That(result.Merged.Fields[Cut("c1", 6)], Is.EqualTo(b.Fields[Cut("c1", 6)]));
        }
    }
}
