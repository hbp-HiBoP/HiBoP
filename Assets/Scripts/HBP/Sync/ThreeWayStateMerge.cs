using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace HBP.Sync
{
    public enum ConflictReason
    {
        SameGroup,
        DeleteVersusEdit,
        ResourceDependency
    }

    public sealed class StateConflict
    {
        public EntityKind Entity { get; }
        public string ParentId { get; }
        public string Id { get; }
        public ushort Group { get; }
        public ConflictReason Reason { get; }
        public IReadOnlyDictionary<StateKey, byte[]> Baseline { get; }
        public IReadOnlyDictionary<StateKey, byte[]> Desktop { get; }
        public IReadOnlyDictionary<StateKey, byte[]> Quest { get; }

        internal StateConflict(GroupKey group, ConflictReason reason, StateSnapshot baseline, StateSnapshot desktop, StateSnapshot quest)
        {
            Entity = group.Entity;
            ParentId = group.ParentId;
            Id = group.Id;
            Group = group.Group;
            Reason = reason;
            Baseline = ThreeWayStateMerge.GroupValues(baseline, group);
            Desktop = ThreeWayStateMerge.GroupValues(desktop, group);
            Quest = ThreeWayStateMerge.GroupValues(quest, group);
        }
    }

    public sealed class MergeResult
    {
        public StateSnapshot Merged { get; }
        public IReadOnlyList<StateConflict> Conflicts { get; }

        internal MergeResult(StateSnapshot merged, IReadOnlyList<StateConflict> conflicts)
        {
            Merged = merged;
            Conflicts = conflicts;
        }
    }

    internal readonly struct GroupKey : IEquatable<GroupKey>
    {
        public readonly EntityKind Entity;
        public readonly string ParentId;
        public readonly string Id;
        public readonly ushort Group;

        public GroupKey(StateKey key, ushort group)
        {
            Entity = key.Entity;
            ParentId = key.ParentId;
            Id = key.Id;
            Group = group;
        }

        public bool Equals(GroupKey other) => Entity == other.Entity && ParentId == other.ParentId && Id == other.Id && Group == other.Group;
        public override bool Equals(object obj) => obj is GroupKey other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Entity, ParentId, Id, Group);
    }

    /// <summary>Detached three-way merge. A conflict keeps Desktop in Merged and retains all candidates.</summary>
    public static class ThreeWayStateMerge
    {
        public static string Hash(StateSnapshot state)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(SharedStateCodec.Encode(state))).Replace("-", "").ToLowerInvariant();
        }

        public static MergeResult Merge(StateSnapshot baseline, StateSnapshot desktop, StateSnapshot quest, string expectedBaselineHash)
        {
            StateValidator.Validate(baseline);
            StateValidator.Validate(desktop);
            StateValidator.Validate(quest);
            if (Hash(baseline) != expectedBaselineHash || baseline.EpochId != desktop.EpochId || baseline.EpochId != quest.EpochId || baseline.VisualizationId != desktop.VisualizationId || baseline.VisualizationId != quest.VisualizationId || baseline.ManifestHash != desktop.ManifestHash || baseline.ManifestHash != quest.ManifestHash || desktop.CommonRevision < baseline.CommonRevision || quest.CommonRevision != baseline.CommonRevision)
                throw new InvalidDataException("Stale or mismatched merge base");

            var bGroups = BuildGroups(baseline);
            var dGroups = BuildGroups(desktop);
            var qGroups = BuildGroups(quest);
            var groups = new HashSet<GroupKey>(bGroups.Keys);
            groups.UnionWith(dGroups.Keys);
            groups.UnionWith(qGroups.Keys);
            var mergedGroups = new Dictionary<GroupKey, SortedDictionary<StateKey, byte[]>>(dGroups);
            var conflicts = new List<StateConflict>();
            var conflictGroups = new HashSet<GroupKey>();
            foreach (GroupKey group in groups)
            {
                var b = Values(bGroups, group);
                var d = Values(dGroups, group);
                var q = Values(qGroups, group);
                bool dChanged = !Equal(b, d);
                bool qChanged = !Equal(b, q);
                if (!qChanged || Equal(d, q)) continue;
                if (dChanged)
                {
                    AddConflict(group, ConflictReason.SameGroup);
                    continue;
                }

                SetGroup(group, q);
            }

            // A tombstone conflicts with edits to any field of the entity or a child.
            foreach (GroupKey group in groups.Where(g => g.Group == 1 && g.Entity != EntityKind.Scene))
            {
                bool bExists = Exists(baseline, group);
                bool dExists = Exists(desktop, group);
                bool qExists = Exists(quest, group);
                if (!bExists || dExists == qExists) continue;
                StateSnapshot editor = dExists ? desktop : quest;
                if (!ChangedUnder(baseline, editor, group)) continue;
                foreach (GroupKey affected in groups.Where(g => IsSelfOrChild(g, group)))
                {
                    AddConflict(affected, ConflictReason.DeleteVersusEdit);
                    SetGroup(affected, Values(dGroups, affected));
                }
            }

            foreach (ushort resourceGroup in new ushort[] { 9, 10, 12 })
            {
                var resource = new GroupKey(new StateKey(EntityKind.Scene, "", "", 0), resourceGroup);
                bool desktopChanged = Changed(desktop, resource);
                bool questChanged = Changed(quest, resource);
                if (desktopChanged && questChanged)
                {
                    if (Equal(Values(dGroups, resource), Values(qGroups, resource))) continue;
                    foreach (GroupKey dependent in groups.Where(g => DependsOn(resourceGroup, g) && Changed(quest, g)))
                    {
                        AddConflict(dependent, ConflictReason.ResourceDependency);
                        SetGroup(dependent, Values(dGroups, dependent));
                    }

                    continue;
                }

                if (!desktopChanged && !questChanged) continue;
                StateSnapshot other = desktopChanged ? quest : desktop;
                foreach (GroupKey dependent in groups.Where(g => DependsOn(resourceGroup, g) && Changed(other, g)))
                {
                    AddConflict(resource, ConflictReason.ResourceDependency);
                    AddConflict(dependent, ConflictReason.ResourceDependency);
                    SetGroup(resource, Values(dGroups, resource));
                    SetGroup(dependent, Values(dGroups, dependent));
                }
            }

            StateSnapshot result = desktop.WithFields(mergedGroups.Values.SelectMany(values => values), desktop.CommonRevision);
            StateValidator.Validate(result);
            ValidateReferences(result);
            return new MergeResult(result, conflicts);

            void AddConflict(GroupKey group, ConflictReason reason)
            {
                if (conflictGroups.Add(group))
                    conflicts.Add(new StateConflict(group, reason, baseline, desktop, quest));
            }

            bool Changed(StateSnapshot branch, GroupKey group) => !Equal(Values(bGroups, group), Values(branch == desktop ? dGroups : qGroups, group));

            void SetGroup(GroupKey group, SortedDictionary<StateKey, byte[]> values)
            {
                if (values.Count == 0) mergedGroups.Remove(group);
                else mergedGroups[group] = values;
            }
        }

        private static Dictionary<GroupKey, SortedDictionary<StateKey, byte[]>> BuildGroups(StateSnapshot state)
        {
            var groups = new Dictionary<GroupKey, SortedDictionary<StateKey, byte[]>>();
            foreach (var entry in state.RawFields)
            {
                SharedStateSchema.TryGet(entry.Key.Entity, entry.Key.FieldId, out FieldDefinition definition);
                var group = new GroupKey(entry.Key, definition.Group);
                if (!groups.TryGetValue(group, out var values))
                    groups.Add(group, values = new SortedDictionary<StateKey, byte[]>());
                values.Add(entry.Key, entry.Value);
            }

            return groups;
        }

        private static SortedDictionary<StateKey, byte[]> Values(Dictionary<GroupKey, SortedDictionary<StateKey, byte[]>> groups, GroupKey group) => groups.TryGetValue(group, out var values) ? values : new SortedDictionary<StateKey, byte[]>();

        private static bool DependsOn(ushort resourceGroup, GroupKey group)
        {
            if (resourceGroup == 10)
                return group.Entity == EntityKind.Cut || group.Entity == EntityKind.Scene && group.Group == 11;
            if (resourceGroup == 12)
                return group.Entity == EntityKind.Site || group.Entity == EntityKind.Column || group.Entity == EntityKind.Roi || group.Entity == EntityKind.Sphere || group.Entity == EntityKind.Cut;
            return group.Entity == EntityKind.Site || group.Entity == EntityKind.Column || group.Entity == EntityKind.Roi || group.Entity == EntityKind.Sphere || group.Entity == EntityKind.Cut;
        }

        internal static SortedDictionary<StateKey, byte[]> GroupValues(StateSnapshot state, GroupKey group)
        {
            var values = new SortedDictionary<StateKey, byte[]>();
            foreach (var entry in state.RawFields)
            {
                SharedStateSchema.TryGet(entry.Key.Entity, entry.Key.FieldId, out FieldDefinition definition);
                if (new GroupKey(entry.Key, definition.Group).Equals(group)) values.Add(entry.Key, (byte[])entry.Value.Clone());
            }

            return values;
        }

        private static bool Equal(SortedDictionary<StateKey, byte[]> left, SortedDictionary<StateKey, byte[]> right) => left.Count == right.Count && left.All(entry => right.TryGetValue(entry.Key, out byte[] value) && entry.Value.SequenceEqual(value));

        private static bool Exists(StateSnapshot state, GroupKey group)
        {
            var key = new StateKey(group.Entity, group.ParentId, group.Id, 1);
            return state.RawFields.TryGetValue(key, out byte[] value) && value[0] == 1;
        }

        private static bool ChangedUnder(StateSnapshot baseline, StateSnapshot editor, GroupKey parent)
        {
            var keys = new HashSet<StateKey>(baseline.RawFields.Keys);
            keys.UnionWith(editor.RawFields.Keys);
            foreach (StateKey key in keys)
            {
                SharedStateSchema.TryGet(key.Entity, key.FieldId, out FieldDefinition definition);
                if (!IsSelfOrChild(new GroupKey(key, definition.Group), parent) || key.Entity == parent.Entity && key.ParentId == parent.ParentId && key.Id == parent.Id && definition.Group == 1) continue;
                baseline.RawFields.TryGetValue(key, out byte[] b);
                editor.RawFields.TryGetValue(key, out byte[] e);
                if (b == null ? e != null : e == null || !b.SequenceEqual(e)) return true;
            }

            return false;
        }

        private static bool IsSelfOrChild(GroupKey candidate, GroupKey parent)
        {
            if (candidate.Entity == parent.Entity && candidate.ParentId == parent.ParentId && candidate.Id == parent.Id)
                return true;
            return parent.Entity == EntityKind.Column && candidate.Entity == EntityKind.Site && candidate.ParentId == parent.Id || parent.Entity == EntityKind.Roi && candidate.Entity == EntityKind.Sphere && candidate.ParentId == parent.Id;
        }

        internal static void ValidateReferences(StateSnapshot state)
        {
            Check(EntityKind.Scene, "", "", 1, EntityKind.Column, "");
            Check(EntityKind.Scene, "", "", 2, EntityKind.Roi, "");
            foreach (StateKey key in state.RawFields.Keys.Where(k => k.Entity == EntityKind.Column && k.FieldId == 4))
                Check(EntityKind.Column, "", key.Id, 4, EntityKind.Site, key.Id);

            void Check(EntityKind source, string parent, string id, ushort field, EntityKind target, string targetParent)
            {
                if (!state.RawFields.TryGetValue(new StateKey(source, parent, id, field), out byte[] bytes)) return;
                string targetId = Encoding.UTF8.GetString(bytes);
                if (targetId.Length == 0) return;
                if (!state.RawFields.TryGetValue(new StateKey(target, targetParent, targetId, 1), out byte[] exists) || exists[0] != 1) throw new InvalidDataException("Dangling shared-state reference");
            }
        }
    }
}
