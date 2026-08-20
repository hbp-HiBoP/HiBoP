using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine.Scripting;

namespace HBP.Core.Data
{
    [JsonObject(MemberSerialization.OptIn), Preserve]
    public sealed class TagValueRecoveryEntry
    {
        [JsonProperty] public string TagID { get; private set; }
        [JsonProperty] public string ValueID { get; private set; }
        [JsonProperty] public string SerializedType { get; private set; }
        [JsonProperty] public string SerializedValue { get; private set; }
        [JsonProperty] public string Reason { get; private set; }

        public TagValueRecoveryEntry()
        {
        }

        internal TagValueRecoveryEntry(BaseTagValue value, string reason)
        {
            TagID = value?.TagReferenceID ?? string.Empty;
            ValueID = value?.ID ?? string.Empty;
            SerializedType = value?.GetType().AssemblyQualifiedName ?? string.Empty;
            SerializedValue = value == null ? "null" : JsonConvert.SerializeObject(value, Formatting.None, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });
            Reason = reason ?? string.Empty;
        }
    }

    public sealed class FilterPresetRecoveryIssue
    {
        public string PresetID { get; }
        public string PresetName { get; }
        public bool WasCurrentPreset { get; }
        public ReadOnlyCollection<string> Reasons { get; }

        internal FilterPresetRecoveryIssue(FilterConditionsPreset preset, bool wasCurrentPreset, IEnumerable<string> reasons)
        {
            PresetID = preset?.ID ?? string.Empty;
            PresetName = preset?.Name ?? string.Empty;
            WasCurrentPreset = wasCurrentPreset;
            Reasons = new ReadOnlyCollection<string>((reasons ?? Enumerable.Empty<string>()).ToList());
        }
    }

    public sealed class FilterPresetRecoveryReport
    {
        public static FilterPresetRecoveryReport Empty { get; } = new(Array.Empty<FilterPresetRecoveryIssue>(), 0);

        public ReadOnlyCollection<FilterPresetRecoveryIssue> Issues { get; }
        public int MigratedPresetCount { get; }
        public int AffectedFilterCount { get; }
        public int ResetCurrentPresetCount => Issues.Count(issue => issue.WasCurrentPreset);
        public int DisabledNamedPresetCount => Issues.Count(issue => !issue.WasCurrentPreset);
        public bool HasChanges => Issues.Count > 0 || MigratedPresetCount > 0;

        internal FilterPresetRecoveryReport(IEnumerable<FilterPresetRecoveryIssue> issues, int migratedPresetCount, int affectedFilterCount = 0)
        {
            Issues = new ReadOnlyCollection<FilterPresetRecoveryIssue>((issues ?? Enumerable.Empty<FilterPresetRecoveryIssue>()).ToList());
            MigratedPresetCount = migratedPresetCount;
            AffectedFilterCount = affectedFilterCount;
        }
    }

    public static class FilterPresetRecoveryService
    {
        public static FilterPresetRecoveryReport Recover(TagCollection tags, FilterConditionsPresetCollection filters, TagParsingPolicy policy = null, bool allowEnumExtensions = true)
        {
            if (tags == null) throw new ArgumentNullException(nameof(tags));
            if (filters == null) return FilterPresetRecoveryReport.Empty;
            policy ??= TagParsingPolicy.Default;
            List<FilterPresetRecoveryIssue> issues = new();
            int migratedCount = 0;
            int affectedFilterCount = 0;

            foreach ((Type type, FilterConditionsPreset preset) in filters.GetCurrentPresetEntries().ToArray())
            {
                PresetRecoveryResult result = RecoverPreset(tags, preset, policy, allowEnumExtensions);
                affectedFilterCount += result.AffectedFilterCount;
                if (result.Issues.Count > 0)
                {
                    filters.QuarantineCurrentPreset(type, preset);
                    issues.Add(new FilterPresetRecoveryIssue(preset, true, result.Issues));
                }
                else if (result.WasMigrated)
                {
                    filters.ReplaceCurrentPreset(type, result.Preset);
                    migratedCount++;
                }
            }

            foreach ((Type type, FilterConditionsPreset preset) in filters.GetNamedPresetEntries().ToArray())
            {
                PresetRecoveryResult result = RecoverPreset(tags, preset, policy, allowEnumExtensions);
                affectedFilterCount += result.AffectedFilterCount;
                if (result.Issues.Count > 0)
                {
                    filters.QuarantineNamedPreset(type, preset);
                    issues.Add(new FilterPresetRecoveryIssue(preset, false, result.Issues));
                }
                else if (result.WasMigrated)
                {
                    filters.ReplaceNamedPreset(type, preset, result.Preset);
                    migratedCount++;
                }
            }

            new LoadingContext(tags.AllTags, Array.Empty<Protocol>(), logLegacyEnumWarnings: true).ResolveFilterConditions(filters);

            FilterPresetRecoveryReport report = new(issues, migratedCount, affectedFilterCount);
            if (report.HasChanges) filters.MarkTagMigrationUnsaved();
            return report;
        }

        private static PresetRecoveryResult RecoverPreset(TagCollection tags, FilterConditionsPreset preset, TagParsingPolicy policy, bool allowEnumExtensions)
        {
            FilterConditionsPresetCollection isolated = new();
            Type isolatedType = typeof(FilterPresetRecoveryService);
            isolated.AddPreset((FilterConditionsPreset)preset.Clone(), isolatedType, false);
            DeferredTagMigrationPlan plan = new DeferredTagMigrationService().Plan(DeferredTagMigrationScope.GlobalFilters, tags, Array.Empty<Patient>(), isolated, policy);
            if (!allowEnumExtensions && plan.EnumAdditionCount > 0)
            {
                return new PresetRecoveryResult(preset, false, Math.Max(plan.FilterCount, 1), new[] { "The filter requires enum options that are not part of the proposed tag definition." });
            }

            if (plan.Issues.Count > 0)
            {
                return new PresetRecoveryResult(preset, false, Math.Max(plan.FilterCount, plan.Issues.Count), plan.Issues.Select(issue => issue.Message));
            }

            bool migrated = plan.RequiresConfirmation;
            if (migrated)
            {
                plan.Commit(DeferredTagMigrationDecision.Apply);
                plan.MarkPersistenceRequired();
            }

            return new PresetRecoveryResult(isolated.GetPresets(isolatedType).Single(), migrated, plan.FilterCount, Array.Empty<string>());
        }

        private sealed class PresetRecoveryResult
        {
            public FilterConditionsPreset Preset { get; }
            public bool WasMigrated { get; }
            public int AffectedFilterCount { get; }
            public IReadOnlyList<string> Issues { get; }

            public PresetRecoveryResult(FilterConditionsPreset preset, bool wasMigrated, int affectedFilterCount, IEnumerable<string> issues)
            {
                Preset = preset;
                WasMigrated = wasMigrated;
                AffectedFilterCount = affectedFilterCount;
                Issues = issues.ToArray();
            }
        }
    }
}
