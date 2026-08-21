using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace HBP.Core.Data
{
    public sealed class TagValueRemoval
    {
        public TagMigrationIssueScope Scope { get; }
        public string PatientID { get; }
        public string PatientName { get; }
        public string TagID { get; }
        public string OwnerID { get; }
        public string ValueID { get; }
        public string SerializedType { get; }
        public string Reason { get; }

        internal TagValueRemoval(TagMigrationIssueScope scope, Patient patient, string tagID, string ownerID, BaseTagValue value, string reason)
        {
            Scope = scope;
            PatientID = patient?.ID ?? string.Empty;
            PatientName = patient?.Name ?? string.Empty;
            TagID = tagID ?? string.Empty;
            OwnerID = ownerID ?? string.Empty;
            ValueID = value?.ID ?? string.Empty;
            SerializedType = value?.GetType().Name ?? "null";
            Reason = reason ?? string.Empty;
        }
    }

    public enum FilterConditionRepairAction
    {
        Migrated,
        Removed,
        Simplified
    }

    public sealed class FilterConditionRepair
    {
        public string PresetID { get; }
        public string PresetName { get; }
        public string ConditionID { get; }
        public string TagID { get; }
        public FilterConditionRepairAction Action { get; }
        public string Message { get; }

        internal FilterConditionRepair(FilterConditionsPreset preset, string conditionID, string tagID, FilterConditionRepairAction action, string message)
        {
            PresetID = preset?.ID ?? string.Empty;
            PresetName = preset?.Name ?? string.Empty;
            ConditionID = conditionID ?? string.Empty;
            TagID = tagID ?? string.Empty;
            Action = action;
            Message = message ?? string.Empty;
        }
    }

    public sealed class FilterPresetRepairReport
    {
        public static FilterPresetRepairReport Empty { get; } = new(Array.Empty<FilterConditionRepair>(), 0, 0);

        public ReadOnlyCollection<FilterConditionRepair> Repairs { get; }
        public int MigratedPresetCount { get; }
        public int AffectedFilterCount { get; }
        public int RemovedConditionCount { get; }
        public bool HasChanges => AffectedFilterCount > 0 || Repairs.Count > 0 || MigratedPresetCount > 0;

        internal FilterPresetRepairReport(IEnumerable<FilterConditionRepair> repairs, int migratedPresetCount, int affectedFilterCount)
        {
            Repairs = new ReadOnlyCollection<FilterConditionRepair>((repairs ?? Enumerable.Empty<FilterConditionRepair>()).ToList());
            MigratedPresetCount = migratedPresetCount;
            AffectedFilterCount = affectedFilterCount;
            RemovedConditionCount = Repairs.Count(repair => repair.Action == FilterConditionRepairAction.Removed);
        }
    }

    public static class FilterPresetRepairService
    {
        public static FilterPresetRepairReport Repair(TagCollection tags, FilterConditionsPresetCollection filters, TagParsingPolicy policy = null, bool allowEnumExtensions = true)
        {
            if (tags == null) throw new ArgumentNullException(nameof(tags));
            if (filters == null) return FilterPresetRepairReport.Empty;
            policy ??= TagParsingPolicy.Default;

            FilterConditionsPresetCollection isolated = (FilterConditionsPresetCollection)filters.Clone();
            DeferredTagMigrationPlan plan = new DeferredTagMigrationService().Plan(DeferredTagMigrationScope.GlobalFilters, tags, Array.Empty<Patient>(), isolated, policy, allowEnumExtensions);
            if (!plan.RequiresConfirmation) return FilterPresetRepairReport.Empty;

            plan.Commit();
            plan.MarkPersistenceRequired();
            filters.Copy(isolated);
            filters.MarkTagMigrationUnsaved();

            int migratedPresetCount = filters.GetNamedPresetEntries().Select(entry => entry.Preset).Concat(filters.GetCurrentPresetEntries().Select(entry => entry.Preset)).Where(preset => preset != null).Distinct().Count(preset => plan.FilterRepairs.Any(repair => repair.PresetID == preset.ID));
            return new FilterPresetRepairReport(plan.FilterRepairs, migratedPresetCount, plan.FilterCount);
        }
    }
}
