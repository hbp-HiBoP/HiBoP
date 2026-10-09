using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace HBP.Sync.Scene
{
    public enum V2ConflictChoice
    {
        Desktop,
        Quest
    }

    public sealed class V2CheckpointConflict
    {
        public string Description { get; }
        public string DesktopSummary { get; }
        public string QuestSummary { get; }
        internal string[] Keys { get; }

        internal V2CheckpointConflict(string description, IEnumerable<string> keys, string desktopSummary, string questSummary)
        {
            Description = description;
            DesktopSummary = desktopSummary;
            QuestSummary = questSummary;
            Keys = keys.ToArray();
        }
    }

    /// <summary>Detached semantic state. Protocol generations, clock origins and local poses are excluded.</summary>
    public sealed class V2SceneCheckpointState
    {
        private static readonly SceneId ScopeScene = new SceneId(Guid.Parse("70000000-0000-0000-0000-000000000001"));
        private static readonly IncarnationId ScopeIncarnation = new IncarnationId(Guid.Parse("70000000-0000-0000-0000-000000000002"));
        private readonly byte[] m_Hash = new byte[32];
        internal Action<string, Cell> BeforeChange;
        internal readonly Dictionary<string, Cell> Cells = new Dictionary<string, Cell>(StringComparer.Ordinal);

        internal sealed class Cell
        {
            internal byte Section;
            internal byte[] Bytes;
            internal V2Mutation Mutation;

            internal Cell(byte section, byte[] bytes, V2Mutation mutation = null)
            {
                Section = section;
                Bytes = bytes;
                Mutation = mutation;
            }
        }

        private V2SceneCheckpointState()
        {
        }

        public V2SceneCheckpointState Clone()
        {
            var result = new V2SceneCheckpointState();
            foreach (var entry in Cells) result.Cells.Add(entry.Key, entry.Value);
            Array.Copy(m_Hash, result.m_Hash, m_Hash.Length);
            return result;
        }

        public static V2SceneCheckpointState FromCheckpoint(V2SceneMutationCheckpoint checkpoint)
        {
            var state = new V2SceneCheckpointState();
            foreach (var record in checkpoint.SiteColors) state.Put(record.Value, 0);
            foreach (var record in checkpoint.CutDefinitions) state.Put(record.Value, 1);
            foreach (var record in checkpoint.TimelineAnchors) state.Put(record.Value, 2);
            foreach (var record in checkpoint.T09Records) state.Put(record.Value, 3);
            foreach (var record in checkpoint.T10Records.OrderBy(record => record.Value is CreateRoi roi ? roi.Order : record.Value is CreateCut cut ? cut.Order : 0)) state.Put(record.Value, 4);
            foreach (var record in checkpoint.T11Records) state.Put(record.Value, 5);
            foreach (var record in checkpoint.T12Records) state.SetCell("filters", new Cell(6, record.Encode()));
            foreach (var record in checkpoint.T13Records) state.SetCell("correlations", new Cell(7, record.Encode()));
            return state;
        }

        private static string Id(string id) => Convert.ToBase64String(Encoding.UTF8.GetBytes(id));
        private static string Cut(string id) => "cut/" + Id(id) + "/";
        private static string Roi(string id) => "roi/" + Id(id) + "/";
        private static string Site(ColumnId column, SiteId site) => "site/" + Id(column.Value) + "/" + Id(site.Value) + "/";

        internal void SetCell(string key, Cell value)
        {
            Cells.TryGetValue(key, out var previous);
            BeforeChange?.Invoke(key, previous);

            void Mix(Cell cell)
            {
                if (cell == null) return;
                using var sha = HBP.Core.Tools.Sha256.Create();
                byte[] prefix = Encoding.UTF8.GetBytes(key + "\0" + cell.Section);
                sha.TransformBlock(prefix, 0, prefix.Length, prefix, 0);
                sha.TransformFinalBlock(cell.Bytes, 0, cell.Bytes.Length);
                for (int i = 0; i < m_Hash.Length; i++) m_Hash[i] ^= sha.Hash[i];
            }

            Mix(previous);
            Mix(value);
            if (value == null) Cells.Remove(key);
            else Cells[key] = value;
        }

        private void Store(string key, V2Mutation mutation, byte section) => SetCell(key, new Cell(section, V2MutationPayloadCodec.Encode(mutation), mutation));
        private static SetCutDefinition CutTemplate(CutId id, float position = 0, uint count = 1) => new SetCutDefinition(id, V2CutOrientation.Custom, false, count, position, 0, 1, 0);

        private void Put(V2Mutation mutation, byte section)
        {
            switch (mutation)
            {
                case SetConfigurationTransaction transaction:
                    foreach (var child in transaction.Mutations) ApplyCanonical(child);
                    return;
                case SetSiteConfigurationBatch batch:
                    foreach (var value in batch.Assignments)
                    {
                        Put(new SetSiteColor(value.ColumnId, value.SiteId, value.Red, value.Green, value.Blue, value.Alpha), 0);
                        Put(new SetSiteBlacklist(value.ColumnId, value.SiteId, value.Blacklisted), 5);
                        Put(new SetSiteHighlight(value.ColumnId, value.SiteId, value.Highlighted), 3);
                        Put(new SetSiteLabels(value.ColumnId, value.SiteId, value.Labels), 3);
                    }

                    return;
                case SetSiteColor value:
                    Store(Site(value.ColumnId, value.FullSiteId) + "color", value, 0);
                    return;
                case SetSiteBlacklist value:
                    Store(Site(value.ColumnId, value.SiteId) + "blacklist", value, 5);
                    return;
                case SetSiteHighlight value:
                    Store(Site(value.ColumnId, value.SiteId) + "highlight", value, 3);
                    return;
                case SetSiteLabels value:
                    Store(Site(value.ColumnId, value.SiteId) + "labels", value, 3);
                    return;
                case CreateCut value:
                    Store(Cut(value.CutId.Value) + "exists", new CreateCut(value.CutId, CutTemplate(value.CutId), 0), 4);
                    Put(value.Definition, 1);
                    var existingOrder = Cells.Values.Select(cell => cell.Mutation).OfType<SetCutOrder>().SingleOrDefault()?.CutIds.ToList() ?? new List<CutId>();
                    existingOrder.RemoveAll(id => id.Equals(value.CutId));
                    existingOrder.Insert(Math.Min(value.Order, existingOrder.Count), value.CutId);
                    Put(new SetCutOrder(existingOrder), 4);
                    return;
                case DeleteCut value:
                    RemovePrefix(Cut(value.CutId.Value));
                    if (Cells.TryGetValue("cut-order", out var order)) Put(new SetCutOrder(((SetCutOrder)order.Mutation).CutIds.Where(id => !id.Equals(value.CutId))), 4);
                    return;
                case SetCutDefinition value:
                    Store(Cut(value.CutId.Value) + "position", CutTemplate(value.CutId, value.Position), 1);
                    Store(Cut(value.CutId.Value) + "count", CutTemplate(value.CutId, count: value.NumberOfCuts), 1);
                    Store(Cut(value.CutId.Value) + "orientation", new SetCutDefinition(value.CutId, value.Orientation, value.Flip, 1, 0, value.NormalX, value.NormalY, value.NormalZ), 1);
                    return;
                case SetCutOrder value:
                    Store("cut-order", value, 4);
                    return;
                case CreateRoi value:
                    Store(Roi(value.RoiId.Value) + "exists", new CreateRoi(value.RoiId, "ROI", Array.Empty<V2RoiSphereDefinition>(), 0), 4);
                    Put(new RenameRoi(value.RoiId, value.Name), 4);
                    InsertOrder("roi-order", value.RoiId.Value, value.Order);
                    StoreOrder(Roi(value.RoiId.Value) + "sphere-order", value.Spheres.Select(sphere => sphere.SphereId.Value));
                    RemovePrefix(Roi(value.RoiId.Value) + "sphere/");
                    foreach (var sphere in value.Spheres) Put(new SetRoiSphereDefinition(value.RoiId, sphere), 4);
                    return;
                case RenameRoi value:
                    Store(Roi(value.RoiId.Value) + "name", value, 4);
                    return;
                case DeleteRoi value:
                    RemovePrefix(Roi(value.RoiId.Value));
                    foreach (var selection in Cells.Values.Select(cell => cell.Mutation).OfType<SetSelectedRoiSphere>().Where(selected => selected.RoiId == value.RoiId.Value).ToArray()) SetCell(MutationKey(selection), null);
                    StoreOrder("roi-order", ReadOrder("roi-order").Where(id => id != value.RoiId.Value));
                    return;
                case CreateRoiSphere value:
                    Put(new SetRoiSphereDefinition(value.RoiId, value.Definition), 4);
                    InsertOrder(Roi(value.RoiId.Value) + "sphere-order", value.Definition.SphereId.Value, value.Order);
                    return;
                case DeleteRoiSphere value:
                    SetCell(Roi(value.RoiId.Value) + "sphere/" + Id(value.SphereId.Value), null);
                    StoreOrder(Roi(value.RoiId.Value) + "sphere-order", ReadOrder(Roi(value.RoiId.Value) + "sphere-order").Where(id => id != value.SphereId.Value));
                    return;
                case SetRoiSphereDefinition value:
                    Store(Roi(value.RoiId.Value) + "sphere/" + Id(value.Definition.SphereId.Value), value, 4);
                    return;
                case SetTimelineAnchor value:
                    Store("timeline/" + Id(value.ColumnId.Value), new SetTimelineAnchor(value.ColumnId, value.Index, value.Playing, value.Looping, value.Step, 0, 1, value.Playing ? V2TimelineAnchorIntent.Play : V2TimelineAnchorIntent.Pause), 2);
                    return;
                case SetSelectedSite value:
                    if (value.SiteId != null)
                    {
                        foreach (var selected in Cells.Values.Select(cell => cell.Mutation).OfType<SetSelectedSite>().Where(selected => !selected.ColumnId.Equals(value.ColumnId)).ToArray())
                            Store(MutationKey(selected), new SetSelectedSite(selected.ColumnId, null), 3);
                        Put(new SetSelectedColumn(value.ColumnId), 3);
                    }

                    Store(MutationKey(value), value, 3);
                    return;
                case SetSiteFilterResult value:
                    SetCell("filters", new Cell(6, new V2SiteFilterCheckpointRecord(value.RosterHash, value.SiteCount, value.InclusionBits).Encode()));
                    return;
                case SetCorrelationResult value:
                    SetCell("correlations", new Cell(7, new V2CorrelationCheckpointRecord(value.ResultBytes, true).Encode()));
                    return;
            }

            Store(MutationKey(mutation), mutation, section);
        }

        private void StoreOrder(string key, IEnumerable<string> ids)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
            foreach (string id in ids) writer.Write(id);
            SetCell(key, new Cell(8, stream.ToArray()));
        }

        internal List<string> ReadOrder(string key)
        {
            var result = new List<string>();
            if (!Cells.TryGetValue(key, out var cell)) return result;
            using var stream = new MemoryStream(cell.Bytes, false);
            using var reader = new BinaryReader(stream, Encoding.UTF8, true);
            while (stream.Position < stream.Length) result.Add(reader.ReadString());
            return result;
        }

        private void InsertOrder(string key, string id, int index)
        {
            var ids = ReadOrder(key);
            ids.Remove(id);
            ids.Insert(Math.Min(index, ids.Count), id);
            StoreOrder(key, ids);
        }

        private static string MutationKey(V2Mutation mutation) => "value/" + Convert.ToBase64String(V2ScheduleDescriptor.ForMutation(ScopeScene, ScopeIncarnation, mutation).EncodeTouchedKeyFingerprints());

        public void ApplyCanonical(V2Mutation mutation)
        {
            byte section = mutation.Type switch
            {
                V2OperationType.SetSiteColor => 0,
                V2OperationType.SetCutDefinition => 1,
                V2OperationType.SetTimelineAnchor => 2,
                >= V2OperationType.CreateCut and <= V2OperationType.ApplyTriangleMask => 4,
                >= V2OperationType.SetSiteBlacklist and <= V2OperationType.SetSiteConfigurationBatch => 5,
                _ => 3
            };
            Put(mutation, section);
        }

        private void RemovePrefix(string prefix)
        {
            foreach (string key in Cells.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).ToArray()) SetCell(key, null);
        }

        public string Hash() => Convert.ToBase64String(m_Hash) + ":" + Cells.Count;

        public V2SceneMutationCheckpoint ToCheckpoint()
        {
            var colors = new List<SiteColorCheckpointRecord>();
            var cuts = new List<CutDefinitionCheckpointRecord>();
            var timelines = new List<TimelineAnchorCheckpointRecord>();
            var t09 = new List<V2T09CheckpointRecord>();
            var t10 = new List<V2T10CheckpointRecord>();
            var t11 = new List<V2T11CheckpointRecord>();
            var t12 = new List<V2SiteFilterCheckpointRecord>();
            var t13 = new List<V2CorrelationCheckpointRecord>();
            var packedSites = new HashSet<string>(StringComparer.Ordinal);
            var assignments = new List<V2SiteConfigurationAssignment>();
            foreach (var group in Cells.Where(entry => entry.Key.StartsWith("site/", StringComparison.Ordinal)).GroupBy(entry => entry.Key.Substring(0, entry.Key.LastIndexOf('/') + 1)))
            {
                var values = group.Select(entry => entry.Value.Mutation).ToArray();
                var color = values.OfType<SetSiteColor>().SingleOrDefault();
                var blacklist = values.OfType<SetSiteBlacklist>().SingleOrDefault();
                var highlight = values.OfType<SetSiteHighlight>().SingleOrDefault();
                var labels = values.OfType<SetSiteLabels>().SingleOrDefault();
                if (color == null || blacklist == null || highlight == null || labels == null) continue;
                assignments.Add(new V2SiteConfigurationAssignment(color.ColumnId, color.FullSiteId, blacklist.Blacklisted, highlight.Highlighted, color.Red, color.Green, color.Blue, color.Alpha, labels.Labels));
                foreach (var entry in group) packedSites.Add(entry.Key);
            }

            if (assignments.Count > 0) t11.Add(new V2T11CheckpointRecord(new SetSiteConfigurationBatch(assignments)));
            var orderedCuts = Cells.Values.Select(cell => cell.Mutation).OfType<SetCutOrder>().SingleOrDefault()?.CutIds.Select(id => id.Value).ToList() ?? new List<string>();
            string[] cutIds = Cells.Values.Select(cell => cell.Mutation).OfType<CreateCut>().Select(value => value.CutId.Value).OrderBy(id => orderedCuts.Contains(id) ? orderedCuts.IndexOf(id) : int.MaxValue).ThenBy(id => id, StringComparer.Ordinal).ToArray();
            for (int i = 0; i < cutIds.Length; i++)
            {
                string prefix = Cut(cutIds[i]);
                var position = (SetCutDefinition)Cells[prefix + "position"].Mutation;
                var count = (SetCutDefinition)Cells[prefix + "count"].Mutation;
                var orientation = (SetCutDefinition)Cells[prefix + "orientation"].Mutation;
                var definition = new SetCutDefinition(position.CutId, orientation.Orientation, orientation.Flip, count.NumberOfCuts, position.Position, orientation.NormalX, orientation.NormalY, orientation.NormalZ);
                t10.Add(new V2T10CheckpointRecord(new CreateCut(definition.CutId, definition, i)));
                cuts.Add(new CutDefinitionCheckpointRecord(definition));
            }

            if (Cells.ContainsKey("cut-order")) t10.Add(new V2T10CheckpointRecord(new SetCutOrder(cutIds.Select(id => new CutId(id)))));
            int roiOrder = 0;
            var roiIds = ReadOrder("roi-order");
            foreach (var roi in Cells.Values.Select(cell => cell.Mutation).OfType<CreateRoi>().OrderBy(value => roiIds.Contains(value.RoiId.Value) ? roiIds.IndexOf(value.RoiId.Value) : int.MaxValue).ThenBy(value => value.RoiId.Value, StringComparer.Ordinal))
            {
                string prefix = Roi(roi.RoiId.Value);
                var name = (RenameRoi)Cells[prefix + "name"].Mutation;
                var sphereIds = ReadOrder(prefix + "sphere-order");
                var spheres = Cells.Where(entry => entry.Key.StartsWith(prefix + "sphere/", StringComparison.Ordinal)).Select(entry => ((SetRoiSphereDefinition)entry.Value.Mutation).Definition).OrderBy(value => sphereIds.Contains(value.SphereId.Value) ? sphereIds.IndexOf(value.SphereId.Value) : int.MaxValue).ThenBy(value => value.SphereId.Value, StringComparer.Ordinal);
                t10.Add(new V2T10CheckpointRecord(new CreateRoi(roi.RoiId, name.Name, spheres, roiOrder++)));
            }

            foreach (var entry in Cells)
            {
                string key = entry.Key;
                Cell cell = entry.Value;
                if (packedSites.Contains(key) || key.StartsWith("cut/", StringComparison.Ordinal) || key.StartsWith("roi/", StringComparison.Ordinal) || key == "cut-order" || cell.Section == 8) continue;
                switch (cell.Section)
                {
                    case 0: colors.Add(new SiteColorCheckpointRecord((SetSiteColor)cell.Mutation)); break;
                    case 2: timelines.Add(new TimelineAnchorCheckpointRecord((SetTimelineAnchor)cell.Mutation)); break;
                    case 3: t09.Add(V2T09CheckpointRecord.FromMutation(cell.Mutation)); break;
                    case 4: t10.Add(new V2T10CheckpointRecord(cell.Mutation)); break;
                    case 5: t11.Add(new V2T11CheckpointRecord(cell.Mutation)); break;
                    case 6: t12.Add(V2SiteFilterCheckpointRecord.Decode(cell.Bytes)); break;
                    case 7: t13.Add(V2CorrelationCheckpointRecord.Decode(cell.Bytes)); break;
                }
            }

            return new V2SceneMutationCheckpoint(colors, cuts, timelines, t09, t10, t11, t12, t13);
        }
    }

    public sealed class V2SceneCheckpointMerge
    {
        private readonly V2SceneCheckpointState m_Desktop, m_Quest, m_Merged;
        public IReadOnlyList<V2CheckpointConflict> Conflicts { get; }

        public V2SceneCheckpointMerge(V2SceneCheckpointState baseline, V2SceneCheckpointState desktop, V2SceneCheckpointState quest)
        {
            m_Desktop = desktop;
            m_Quest = quest;
            m_Merged = desktop.Clone();
            var keys = new HashSet<string>(desktop.Cells.Keys, StringComparer.Ordinal);
            keys.UnionWith(quest.Cells.Keys);
            if (baseline != null) keys.UnionWith(baseline.Cells.Keys);
            var conflicts = new HashSet<string>(StringComparer.Ordinal);
            foreach (string key in keys)
            {
                if (Equal(desktop, quest, key)) continue;
                if (baseline == null)
                {
                    conflicts.Add(key);
                    continue;
                }

                bool d = !Equal(baseline, desktop, key), q = !Equal(baseline, quest, key);
                if (d && q && !((key == "cut-order" || key.EndsWith("-order", StringComparison.Ordinal)) && CompatibleOrders(desktop, quest, key))) conflicts.Add(key);
                else if (q) Copy(quest, m_Merged, key);
            }

            var groups = keys.ToDictionary(key => key, key => key, StringComparer.Ordinal);

            string Root(string key)
            {
                while (groups[key] != key) key = groups[key];
                return key;
            }

            void Join(IEnumerable<string> related)
            {
                string[] group = related.ToArray();
                if (group.Length == 0) return;
                string root = Root(group[0]);
                foreach (string key in group.Skip(1)) groups[Root(key)] = root;
            }

            bool Changed(V2SceneCheckpointState side, string key) => baseline == null || !Equal(baseline, side, key);
            // Existence conflicts include every dependent edit, including selections.
            foreach (string exists in keys.Where(key => key.EndsWith("/exists", StringComparison.Ordinal)))
            {
                string prefix = exists.Substring(0, exists.Length - "exists".Length);
                bool deletedD = baseline?.Cells.ContainsKey(exists) == true && !desktop.Cells.ContainsKey(exists);
                bool deletedQ = baseline?.Cells.ContainsKey(exists) == true && !quest.Cells.ContainsKey(exists);
                var roi = Cell(baseline, exists)?.Mutation as CreateRoi;
                bool ReferencesRoi(V2Mutation value) => roi != null && (value is SetActiveRoi active && Equals(active.RoiId, roi.RoiId) || value is SetSelectedRoiSphere selected && selected.RoiId == roi.RoiId.Value);
                string[] related = keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal) || ReferencesRoi(Cell(desktop, key)?.Mutation) || ReferencesRoi(Cell(quest, key)?.Mutation) || ReferencesRoi(Cell(baseline, key)?.Mutation)).ToArray();
                if (deletedD && related.Any(key => Changed(quest, key) && !Equal(desktop, quest, key)) || deletedQ && related.Any(key => Changed(desktop, key) && !Equal(desktop, quest, key)))
                {
                    Join(related);
                    conflicts.UnionWith(related);
                    string[] selections = exists.StartsWith("roi/", StringComparison.Ordinal) ? keys.Where(key => Cell(desktop, key)?.Mutation is SetActiveRoi or SetSelectedRoiSphere || Cell(quest, key)?.Mutation is SetActiveRoi or SetSelectedRoiSphere).ToArray() : Array.Empty<string>();
                    Join(related.Concat(selections));
                }
            }

            foreach (string sphereKey in keys.Where(key => Cell(baseline, key)?.Mutation is SetRoiSphereDefinition))
            {
                var sphere = (SetRoiSphereDefinition)Cell(baseline, sphereKey).Mutation;
                bool deletedD = !desktop.Cells.ContainsKey(sphereKey), deletedQ = !quest.Cells.ContainsKey(sphereKey);
                bool ReferencesSphere(V2Mutation value) => value is SetSelectedRoiSphere selected && selected.RoiId == sphere.RoiId.Value && selected.SphereId == sphere.Definition.SphereId.Value;
                var related = keys.Where(key => key == sphereKey || ReferencesSphere(Cell(desktop, key)?.Mutation) || ReferencesSphere(Cell(quest, key)?.Mutation) || ReferencesSphere(Cell(baseline, key)?.Mutation)).ToArray();
                if (deletedD && related.Any(key => Changed(quest, key) && !Equal(desktop, quest, key)) || deletedQ && related.Any(key => Changed(desktop, key) && !Equal(desktop, quest, key)))
                {
                    Join(related);
                    conflicts.UnionWith(related);
                }
            }

            var selectionKeys = keys.Where(key => Cell(desktop, key)?.Mutation is SetSelectedColumn or SetSelectedSite || Cell(quest, key)?.Mutation is SetSelectedColumn or SetSelectedSite).ToArray();
            Join(selectionKeys);
            if (selectionKeys.Any(key => Changed(desktop, key)) && selectionKeys.Any(key => Changed(quest, key)) && selectionKeys.Any(key => !Equal(desktop, quest, key))) conflicts.UnionWith(selectionKeys);

            // Resource selection and its dependent geometry/scientific values are one choice.
            foreach (var resource in keys.Where(key => IsResource(Cell(desktop, key)?.Mutation) || IsResource(Cell(quest, key)?.Mutation)))
            {
                if (Equal(desktop, quest, resource) || !Changed(desktop, resource) && !Changed(quest, resource)) continue;
                var resourceValue = Cell(desktop, resource)?.Mutation ?? Cell(quest, resource)?.Mutation;
                string[] dependent = keys.Where(key => DependsOn(resourceValue, Cell(desktop, key)?.Mutation, key) || DependsOn(resourceValue, Cell(quest, key)?.Mutation, key)).Append(resource).Distinct().ToArray();
                if (Changed(desktop, resource) && dependent.Any(key => Changed(quest, key) && !Equal(desktop, quest, key)) || Changed(quest, resource) && dependent.Any(key => Changed(desktop, key) && !Equal(desktop, quest, key)))
                {
                    Join(dependent.Append(resource));
                    conflicts.UnionWith(dependent.Append(resource));
                }
            }

            if (baseline == null && conflicts.Count > 0) Join(keys);
            Conflicts = keys.GroupBy(Root).Where(group => group.Any(conflicts.Contains)).Select(group => new V2CheckpointConflict(baseline == null ? "The last common state is no longer available. Choose which scene version to keep for this reconnection." : Describe(group), group, Summarize(desktop, group), Summarize(quest, group))).ToArray();
        }

        private static V2SceneCheckpointState.Cell Cell(V2SceneCheckpointState state, string key) => state != null && state.Cells.TryGetValue(key, out var cell) ? cell : null;

        private static bool Equal(V2SceneCheckpointState a, V2SceneCheckpointState b, string key)
        {
            var x = Cell(a, key);
            var y = Cell(b, key);
            return x == null ? y == null : y != null && x.Section == y.Section && x.Bytes.SequenceEqual(y.Bytes);
        }

        private static void Copy(V2SceneCheckpointState source, V2SceneCheckpointState target, string key)
        {
            if (source.Cells.TryGetValue(key, out var value)) target.SetCell(key, value);
            else target.SetCell(key, null);
        }

        private static bool IsSelection(V2Mutation value) => value is SetSelectedColumn or SetSelectedSite or SetActiveRoi or SetSelectedRoiSphere;

        private static bool CompatibleOrders(V2SceneCheckpointState desktop, V2SceneCheckpointState quest, string key)
        {
            var d = key == "cut-order" ? (Cell(desktop, key)?.Mutation as SetCutOrder)?.CutIds.Select(id => id.Value).ToArray() ?? Array.Empty<string>() : desktop.ReadOrder(key).ToArray();
            var q = key == "cut-order" ? (Cell(quest, key)?.Mutation as SetCutOrder)?.CutIds.Select(id => id.Value).ToArray() ?? Array.Empty<string>() : quest.ReadOrder(key).ToArray();
            var common = new HashSet<string>(d.Intersect(q), StringComparer.Ordinal);
            return d.Where(common.Contains).SequenceEqual(q.Where(common.Contains));
        }

        private static bool IsResource(V2Mutation value) => value is SetMeshDisplay or SetSelectedMri or SetImplantation or SetColumnResource;

        private static bool DependsOn(V2Mutation resource, V2Mutation value, string key)
        {
            if (resource is SetColumnResource column)
                return key == "correlations" || value is SetColumnSpan span && span.ColumnId.Equals(column.ColumnId) || value is SetFunctionalDisplay display && display.ColumnId.Equals(column.ColumnId) || value is SetCcepSource source && source.ColumnId.Equals(column.ColumnId) || value is SetTimelineAnchor timeline && timeline.ColumnId.Equals(column.ColumnId);
            if (resource is SetImplantation) return key.StartsWith("site/", StringComparison.Ordinal) || key is "filters" or "correlations" || value is MoveSites or SetCcepSource or SetSelectedSite;
            if (resource is SetMeshDisplay) return value is ApplyTriangleMask or MoveSites or SetIbcDifumoDisplay || key == "filters";
            if (resource is SetSelectedMri) return value is MoveSites or SetMriCalibration || key == "filters";
            return false;
        }

        private static string Summarize(V2SceneCheckpointState state, IEnumerable<string> keys)
        {
            string DescribeValue(string key)
            {
                var value = Cell(state, key)?.Mutation;
                if (!state.Cells.ContainsKey(key)) return "Item deleted or missing";
                return value switch
                {
                    SetSiteColor site => $"Site {site.FullSiteId.Value}: color ({site.Red:0.##}, {site.Green:0.##}, {site.Blue:0.##})",
                    SetSiteLabels site => $"Site {site.SiteId.Value}: labels [{string.Join(", ", site.Labels)}]",
                    SetSiteHighlight site => $"Site {site.SiteId.Value}: highlight {(site.Highlighted ? "enabled" : "disabled")}",
                    SetSiteBlacklist site => $"Site {site.SiteId.Value}: exclusion {(site.Blacklisted ? "enabled" : "disabled")}",
                    SetCutDefinition cut when key.EndsWith("position", StringComparison.Ordinal) => $"Cut: position {cut.Position:0.###}",
                    SetCutDefinition cut when key.EndsWith("count", StringComparison.Ordinal) => $"Cut: {cut.NumberOfCuts} planes",
                    SetCutDefinition cut => $"Cut: orientation {cut.Orientation}, flipped={cut.Flip}",
                    CreateCut => "Cut retained",
                    CreateRoi => "Region of interest retained",
                    RenameRoi roi => $"Region of interest: {roi.Name}",
                    SetRoiSphereDefinition sphere => $"Sphere: center ({sphere.Definition.X:0.##}, {sphere.Definition.Y:0.##}, {sphere.Definition.Z:0.##}), radius {sphere.Definition.InfluenceRadius:0.##}",
                    SetActiveRoi roi => roi.RoiId == null ? "No active region of interest" : "Active region of interest changed",
                    SetSelectedRoiSphere sphere => string.IsNullOrEmpty(sphere.SphereId) ? "No sphere selected" : "Selected sphere changed",
                    SetSelectedSite site => site.SiteId == null ? "No site selected in this column" : $"Selected site: {site.SiteId.Value}",
                    SetSelectedColumn column => column.ColumnId == null ? "No column selected" : "Selected column changed",
                    SetSceneBoolean property => $"{property.Property}: {(property.Value ? "enabled" : "disabled")}",
                    SetSceneFloat property => $"{property.Property} : {property.Value:0.###}",
                    SetTimelineAnchor time => $"Navigation: sample {time.Index}, {(time.Playing ? "playing" : "paused")}, looping={time.Looping}",
                    SetMeshDisplay mesh => $"Mesh: {mesh.Part}, representation {mesh.Representation}",
                    SetSelectedMri => "Selected MRI changed",
                    SetColumnResource => "Selected column data changed",
                    SetImplantation => "Selected implantation changed",
                    MoveSites move => $"Site placement: {(move.Command == V2SiteMoveCommand.Reset ? "original" : move.Command == V2SiteMoveCommand.Left ? "left" : "right")}",
                    ApplyTriangleMask => "Triangle mask changed",
                    _ => key == "filters" ? "Site filtering" : key == "correlations" ? "Correlation results and display" : null
                };
            }

            return string.Join("\n", keys.Select(DescribeValue).Where(text => text != null).Distinct().Take(16));
        }

        private static string Describe(IEnumerable<string> keys)
        {
            string first = keys.First();
            if (first.StartsWith("cut/", StringComparison.Ordinal)) return "A cut was changed differently on both devices, or deleted on one and changed on the other.";
            if (first.StartsWith("roi/", StringComparison.Ordinal)) return "A region of interest or its spheres have conflicting changes.";
            if (first.StartsWith("site/", StringComparison.Ordinal)) return "A site attribute or related data have conflicting changes.";
            if (first.StartsWith("timeline/", StringComparison.Ordinal)) return "Timeline navigation was changed on both devices.";
            return "These settings have conflicting changes or depend on a changed resource.";
        }

        public V2SceneCheckpointState Resolve(IReadOnlyList<V2ConflictChoice> choices)
        {
            if (choices == null || choices.Count != Conflicts.Count) throw new ArgumentException("Every conflict group requires one choice.", nameof(choices));
            var result = m_Merged.Clone();
            for (int i = 0; i < choices.Count; i++)
            {
                if (choices[i] != V2ConflictChoice.Desktop && choices[i] != V2ConflictChoice.Quest) throw new ArgumentOutOfRangeException(nameof(choices));
                foreach (string key in Conflicts[i].Keys) Copy(choices[i] == V2ConflictChoice.Desktop ? m_Desktop : m_Quest, result, key);
            }

            return result;
        }
    }
}
