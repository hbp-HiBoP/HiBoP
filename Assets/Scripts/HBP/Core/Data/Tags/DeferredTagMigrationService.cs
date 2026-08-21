using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace HBP.Core.Data
{
    public enum DeferredTagMigrationScope
    {
        Project,
        Workspace,
        GlobalFilters
    }

    public sealed class DeferredTagMigrationChange
    {
        public string TagID { get; }
        public string Name { get; }
        public string CurrentType { get; }
        public ReadOnlyCollection<string> SerializedTypes { get; }
        public int ValueCount { get; }
        public int FilterCount { get; }

        internal DeferredTagMigrationChange(string tagID, string name, string currentType, IEnumerable<string> serializedTypes, int valueCount, int filterCount)
        {
            TagID = tagID;
            Name = name;
            CurrentType = currentType;
            SerializedTypes = new ReadOnlyCollection<string>(serializedTypes.OrderBy(type => type, StringComparer.Ordinal).ToList());
            ValueCount = valueCount;
            FilterCount = filterCount;
        }
    }

    public sealed class DeferredTagMigrationPlan
    {
        private readonly TagCollection m_CanonicalTags;
        private readonly Dictionary<string, BaseTag> m_ExpectedTags;
        private readonly BaseTag[] m_ExpectedTagEntries;
        private readonly string m_ExpectedTagSignature;
        private readonly List<DeferredTagOwnerMutation> m_OwnerMutations;
        private readonly Dictionary<string, EnumTag> m_StagedEnums;
        private readonly Dictionary<string, string[]> m_OriginalEnumValues = new(StringComparer.Ordinal);
        private readonly FilterConditionsPresetCollection m_TargetFilters;
        private readonly FilterConditionsPresetCollection m_PreparedFilters;
        private readonly string m_ExpectedFilterSignature;
        private object m_OriginalFilterState;
        private bool m_Committed;

        public DeferredTagMigrationScope Scope { get; }
        public ReadOnlyCollection<TagMigrationIssue> Issues { get; }
        public ReadOnlyCollection<TagValueRemoval> RemovedValues { get; }
        public ReadOnlyCollection<FilterConditionRepair> FilterRepairs { get; }
        public ReadOnlyCollection<string> Warnings { get; }
        public ReadOnlyCollection<DeferredTagMigrationChange> Changes { get; }
        public int PatientValueCount { get; }
        public int SiteValueCount { get; }
        public int FilterCount { get; }
        public int LossyConversionCount { get; }
        public int RemovedValueCount => RemovedValues.Count;
        public int EnumAdditionCount { get; }
        public bool RequiresConfirmation => PatientValueCount > 0 || SiteValueCount > 0 || FilterCount > 0 || RemovedValueCount > 0 || EnumAdditionCount > 0;

        internal DeferredTagMigrationPlan(DeferredTagMigrationScope scope, TagCollection canonicalTags, List<DeferredTagOwnerMutation> ownerMutations, IReadOnlyDictionary<string, EnumTag> stagedEnums, FilterConditionsPresetCollection targetFilters, FilterConditionsPresetCollection preparedFilters, IEnumerable<TagMigrationIssue> issues, IEnumerable<TagValueRemoval> removedValues, IEnumerable<FilterConditionRepair> filterRepairs, IEnumerable<string> warnings, IEnumerable<DeferredTagMigrationChange> changes, int patientValueCount, int siteValueCount, int filterCount, int lossyConversionCount)
        {
            Scope = scope;
            m_CanonicalTags = canonicalTags;
            m_ExpectedTags = CreateCanonicalIndex(canonicalTags);
            m_ExpectedTagEntries = canonicalTags.AllTags.ToArray();
            m_ExpectedTagSignature = CreateSignature(canonicalTags);
            m_OwnerMutations = ownerMutations;
            m_StagedEnums = stagedEnums.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
            m_TargetFilters = targetFilters;
            m_PreparedFilters = preparedFilters;
            m_ExpectedFilterSignature = targetFilters?.GetMigrationSignature();
            Issues = new ReadOnlyCollection<TagMigrationIssue>(issues.ToList());
            RemovedValues = new ReadOnlyCollection<TagValueRemoval>(removedValues.ToList());
            FilterRepairs = new ReadOnlyCollection<FilterConditionRepair>(filterRepairs.ToList());
            Warnings = new ReadOnlyCollection<string>(warnings.Distinct(StringComparer.Ordinal).ToList());
            Changes = new ReadOnlyCollection<DeferredTagMigrationChange>(changes.ToList());
            PatientValueCount = patientValueCount;
            SiteValueCount = siteValueCount;
            FilterCount = filterCount;
            LossyConversionCount = lossyConversionCount;
            EnumAdditionCount = m_StagedEnums.Sum(pair => pair.Value.Values.Length - ((EnumTag)m_ExpectedTags[pair.Key]).Values.Length);
        }

        public void Commit()
        {
            if (m_Committed) throw new InvalidOperationException("The deferred tag migration plan has already been committed.");
            if (Issues.Count > 0) throw new InvalidOperationException("An invalid deferred tag migration plan cannot be committed.");

            ValidateCurrentState();
            foreach (DeferredTagOwnerMutation mutation in m_OwnerMutations) mutation.CaptureOriginal();
            if (FilterCount > 0) m_OriginalFilterState = m_TargetFilters.CaptureMigrationState();

            try
            {
                foreach ((string tagID, EnumTag staged) in m_StagedEnums)
                {
                    EnumTag canonical = (EnumTag)m_ExpectedTags[tagID];
                    m_OriginalEnumValues.Add(tagID, canonical.Values.ToArray());
                    canonical.Values = staged.Values;
                }

                foreach (DeferredTagOwnerMutation mutation in m_OwnerMutations) mutation.Apply(m_ExpectedTags);
                if (FilterCount > 0)
                {
                    m_TargetFilters.Copy(m_PreparedFilters);
                    new LoadingContext(m_CanonicalTags.AllTags, Array.Empty<Protocol>(), logLegacyEnumWarnings: false).ResolveFilterConditions(m_TargetFilters);
                }

                m_Committed = true;
            }
            catch
            {
                RollbackInternal();
                throw;
            }
        }

        public void Rollback()
        {
            if (!m_Committed) return;
            RollbackInternal();
            m_Committed = false;
        }

        internal void MarkPersistenceRequired()
        {
            if (EnumAdditionCount > 0) m_CanonicalTags.MarkTagMigrationUnsaved();
            if (FilterCount > 0) m_TargetFilters.MarkTagMigrationUnsaved();
        }

        private void ValidateCurrentState()
        {
            if (m_CanonicalTags.AllTags.Count != m_ExpectedTagEntries.Length || m_CanonicalTags.AllTags.Where((tag, index) => !ReferenceEquals(tag, m_ExpectedTagEntries[index])).Any() || !string.Equals(CreateSignature(m_CanonicalTags), m_ExpectedTagSignature, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The global tag definitions changed while the migration was being reviewed.");
            }

            if (FilterCount > 0 && !string.Equals(m_TargetFilters.GetMigrationSignature(), m_ExpectedFilterSignature, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The filter presets changed while the migration was being reviewed.");
            }
        }

        private void RollbackInternal()
        {
            foreach (DeferredTagOwnerMutation mutation in m_OwnerMutations) mutation.Restore();
            foreach ((string tagID, string[] values) in m_OriginalEnumValues)
            {
                if (m_ExpectedTags.TryGetValue(tagID, out BaseTag tag) && tag is EnumTag enumTag) enumTag.Values = values;
            }

            m_OriginalEnumValues.Clear();
            if (m_TargetFilters != null && m_OriginalFilterState != null) m_TargetFilters.RestoreMigrationState(m_OriginalFilterState);
        }

        private static string CreateSignature(TagCollection tags)
        {
            return JsonConvert.SerializeObject(tags, Formatting.None, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });
        }

        private static Dictionary<string, BaseTag> CreateCanonicalIndex(TagCollection tags)
        {
            Dictionary<string, BaseTag> result = new(StringComparer.Ordinal);
            foreach (BaseTag tag in tags.AllTags)
            {
                if (tag == null) continue;
                if (result.TryGetValue(tag.ID, out BaseTag existing))
                {
                    if (!ReferenceEquals(existing, tag)) throw new InvalidOperationException($"Duplicate tag ID '{tag.ID}'.");
                    continue;
                }

                result.Add(tag.ID, tag);
            }

            return result;
        }
    }

    public sealed class DeferredTagMigrationService
    {
        private readonly TagValueConversionService m_ValueConverter = new();
        private readonly TagFilterValueConversionService m_FilterConverter = new();

        public DeferredTagMigrationPlan Plan(DeferredTagMigrationScope scope, TagCollection canonicalTags, IEnumerable<Patient> patients, FilterConditionsPresetCollection filters, TagParsingPolicy policy, bool allowEnumExtensions = true)
        {
            if (canonicalTags == null) throw new ArgumentNullException(nameof(canonicalTags));
            policy ??= TagParsingPolicy.Default;
            Dictionary<string, BaseTag> canonicalByID = CreateCanonicalIndex(canonicalTags);
            Dictionary<string, EnumTag> stagedEnums = new(StringComparer.Ordinal);
            List<TagMigrationIssue> issues = new();
            List<TagValueRemoval> removedValues = new();
            List<FilterConditionRepair> filterRepairs = new();
            List<string> warnings = new();
            List<DeferredTagOwnerMutation> mutations = new();
            Dictionary<string, ChangeAccumulator> changes = new(StringComparer.Ordinal);
            int patientCount = 0;
            int siteCount = 0;
            int filterCount = 0;
            int lossyCount = 0;
            HashSet<object> visited = new(ReferenceEqualityComparer.Instance);

            foreach (Patient patient in patients ?? Enumerable.Empty<Patient>())
            {
                if (patient == null) continue;
                if (visited.Add(patient))
                {
                    DeferredTagOwnerMutation mutation = PrepareOwner(patient, patient, patient.Tags, TagMigrationIssueScope.PatientValue, canonicalByID, stagedEnums, policy, removedValues, warnings, changes, ref patientCount, ref lossyCount);
                    if (mutation != null) mutations.Add(mutation);
                }

                foreach (Site site in patient.Sites ?? Enumerable.Empty<Site>())
                {
                    if (site == null || !visited.Add(site)) continue;
                    DeferredTagOwnerMutation mutation = PrepareOwner(patient, site, site.Tags, TagMigrationIssueScope.SiteValue, canonicalByID, stagedEnums, policy, removedValues, warnings, changes, ref siteCount, ref lossyCount);
                    if (mutation != null) mutations.Add(mutation);
                }
            }

            FilterConditionsPresetCollection preparedFilters = filters == null ? null : (FilterConditionsPresetCollection)filters.Clone();
            if (preparedFilters != null)
            {
                IEnumerable<FilterConditionsPreset> presets = preparedFilters.GetNamedPresetEntries().Select(entry => entry.Preset).Concat(preparedFilters.GetCurrentPresetEntries().Select(entry => entry.Preset)).Where(preset => preset != null).Distinct();

                foreach (FilterConditionsPreset preset in presets)
                {
                    List<BaseFilterCondition> repaired = new();
                    foreach (BaseFilterCondition condition in preset.Conditions ?? Enumerable.Empty<BaseFilterCondition>())
                    {
                        BaseFilterCondition result = PrepareFilterCondition(condition, preset, canonicalByID, stagedEnums, policy, allowEnumExtensions, filterRepairs, warnings, changes, ref filterCount, ref lossyCount);
                        if (result != null) repaired.Add(result);
                    }

                    preset.Conditions = repaired;
                }
            }

            List<DeferredTagMigrationChange> migrationChanges = changes.Select(pair => pair.Value.Build(pair.Key, canonicalByID.TryGetValue(pair.Key, out BaseTag tag) ? tag : null)).ToList();
            return new DeferredTagMigrationPlan(scope, canonicalTags, mutations, stagedEnums, filters, preparedFilters, issues, removedValues, filterRepairs, warnings, migrationChanges, patientCount, siteCount, filterCount, lossyCount);
        }

        private DeferredTagOwnerMutation PrepareOwner(Patient patient, object owner, IEnumerable<BaseTagValue> values, TagMigrationIssueScope scope, IReadOnlyDictionary<string, BaseTag> canonicalByID, IDictionary<string, EnumTag> stagedEnums, TagParsingPolicy policy, ICollection<TagValueRemoval> removedValues, ICollection<string> warnings, IDictionary<string, ChangeAccumulator> changes, ref int convertedCount, ref int lossyCount)
        {
            List<BaseTagValue> sourceValues = (values ?? Enumerable.Empty<BaseTagValue>()).ToList();
            List<BaseTagValue> preparedValues = new(sourceValues.Count);
            bool changed = false;
            foreach (BaseTagValue source in sourceValues)
            {
                string tagID = source?.TagReferenceID;
                if (source == null || string.IsNullOrEmpty(tagID) || !canonicalByID.TryGetValue(tagID, out BaseTag canonical))
                {
                    string reason = $"Tag definition '{tagID ?? string.Empty}' was not found.";
                    removedValues.Add(new TagValueRemoval(scope, patient, tagID, GetOwnerID(owner), source, reason));
                    RegisterChange(changes, tagID ?? string.Empty, source?.GetType().Name ?? "null", false);
                    changed = true;
                    continue;
                }

                if (!NeedsValueMigration(source, canonical))
                {
                    preparedValues.Add(source);
                    continue;
                }

                BaseTag stagedTarget = GetStagedTarget(canonical, stagedEnums);
                BaseTag legacySourceDefinition = source is EnumTagValue { StringValue: null } && canonical is EnumTag ? canonical : null;
                TagValueConversionResult conversion = m_ValueConverter.TryConvert(source, stagedTarget, policy, legacySourceDefinition);
                if (!conversion.Success)
                {
                    removedValues.Add(new TagValueRemoval(scope, patient, tagID, GetOwnerID(owner), source, conversion.Error));
                    changed = true;
                    RegisterChange(changes, tagID, source.GetType().Name, false);
                    continue;
                }

                preparedValues.Add(conversion.Value);
                convertedCount++;
                changed = true;
                RegisterChange(changes, tagID, source.GetType().Name, false);
                if (conversion.Impact == TagConversionImpact.Lossy) lossyCount++;
                if (conversion.Warning != null) warnings.Add(conversion.Warning);
            }

            return changed ? new DeferredTagOwnerMutation(owner, sourceValues, preparedValues) : null;
        }

        private BaseFilterCondition PrepareFilterCondition(BaseFilterCondition condition, FilterConditionsPreset preset, IReadOnlyDictionary<string, BaseTag> canonicalByID, IDictionary<string, EnumTag> stagedEnums, TagParsingPolicy policy, bool allowEnumExtensions, ICollection<FilterConditionRepair> repairs, ICollection<string> warnings, IDictionary<string, ChangeAccumulator> changes, ref int filterCount, ref int lossyCount)
        {
            if (condition == null) return null;
            switch (condition)
            {
                case PatientTagFilterCondition patientTag:
                    return TryPrepareFilter(patientTag.ID, patientTag.TagReferenceID, patientTag.Tag, patientTag.Value, (tag, value) =>
                    {
                        patientTag.Tag = tag;
                        patientTag.Value = value;
                    }, preset, canonicalByID, stagedEnums, policy, allowEnumExtensions, repairs, warnings, changes, ref filterCount, ref lossyCount) ? condition : null;

                case SiteTagFilterCondition siteTag:
                    return TryPrepareFilter(siteTag.ID, siteTag.TagReferenceID, siteTag.Tag, siteTag.Value, (tag, value) =>
                    {
                        siteTag.Tag = tag;
                        siteTag.Value = value;
                    }, preset, canonicalByID, stagedEnums, policy, allowEnumExtensions, repairs, warnings, changes, ref filterCount, ref lossyCount) ? condition : null;

                case MultipleSiteTagsFilterCondition multiple:
                    List<SingleTagFilter> repairedTags = new();
                    foreach (SingleTagFilter single in multiple.TagFilters ?? Enumerable.Empty<SingleTagFilter>())
                    {
                        if (single != null && TryPrepareFilter(single.ID, single.TagReferenceID, single.Tag, single.Value, (tag, value) =>
                            {
                                single.Tag = tag;
                                single.Value = value;
                            }, preset, canonicalByID, stagedEnums, policy, allowEnumExtensions, repairs, warnings, changes, ref filterCount, ref lossyCount))
                        {
                            repairedTags.Add(single);
                        }
                    }

                    multiple.TagFilters = repairedTags;
                    if (repairedTags.Count > 0) return multiple;
                    repairs.Add(new FilterConditionRepair(preset, multiple.ID, string.Empty, FilterConditionRepairAction.Removed, "Removed condition because it no longer contains any valid site tag filter."));
                    filterCount++;
                    return null;

                case AllFilterCondition all:
                    return RepairGroup(all, all.Conditions, conditions => all.Conditions = conditions, preset, canonicalByID, stagedEnums, policy, allowEnumExtensions, repairs, warnings, changes, ref filterCount, ref lossyCount);

                case AnyFilterCondition any:
                    return RepairGroup(any, any.Conditions, conditions => any.Conditions = conditions, preset, canonicalByID, stagedEnums, policy, allowEnumExtensions, repairs, warnings, changes, ref filterCount, ref lossyCount);

                default:
                    return condition;
            }
        }

        private BaseFilterCondition RepairGroup(BaseFilterCondition group, IEnumerable<BaseFilterCondition> children, Action<List<BaseFilterCondition>> assign, FilterConditionsPreset preset, IReadOnlyDictionary<string, BaseTag> canonicalByID, IDictionary<string, EnumTag> stagedEnums, TagParsingPolicy policy, bool allowEnumExtensions, ICollection<FilterConditionRepair> repairs, ICollection<string> warnings, IDictionary<string, ChangeAccumulator> changes, ref int filterCount, ref int lossyCount)
        {
            List<BaseFilterCondition> repaired = new();
            foreach (BaseFilterCondition child in children ?? Enumerable.Empty<BaseFilterCondition>())
            {
                BaseFilterCondition result = PrepareFilterCondition(child, preset, canonicalByID, stagedEnums, policy, allowEnumExtensions, repairs, warnings, changes, ref filterCount, ref lossyCount);
                if (result != null) repaired.Add(result);
            }

            if (repaired.Count >= 2)
            {
                assign(repaired);
                return group;
            }

            filterCount++;
            if (repaired.Count == 0)
            {
                repairs.Add(new FilterConditionRepair(preset, group.ID, string.Empty, FilterConditionRepairAction.Removed, "Removed condition group because it no longer contains any valid sub-condition."));
                return null;
            }

            BaseFilterCondition replacement = repaired[0];
            replacement.IsNot ^= group.IsNot;
            repairs.Add(new FilterConditionRepair(preset, group.ID, string.Empty, FilterConditionRepairAction.Simplified, $"Replaced condition group with its only valid sub-condition '{replacement.ID}'."));
            return replacement;
        }

        private bool TryPrepareFilter(string conditionID, string tagID, BaseTag sourceTag, TagFilterValue sourceValue, Action<BaseTag, TagFilterValue> apply, FilterConditionsPreset preset, IReadOnlyDictionary<string, BaseTag> canonicalByID, IDictionary<string, EnumTag> stagedEnums, TagParsingPolicy policy, bool allowEnumExtensions, ICollection<FilterConditionRepair> repairs, ICollection<string> warnings, IDictionary<string, ChangeAccumulator> changes, ref int filterCount, ref int lossyCount)
        {
            if (string.IsNullOrEmpty(tagID) || !canonicalByID.TryGetValue(tagID, out BaseTag canonical))
            {
                string reason = $"Removed condition because tag definition '{tagID ?? string.Empty}' was not found.";
                repairs.Add(new FilterConditionRepair(preset, conditionID, tagID, FilterConditionRepairAction.Removed, reason));
                RegisterChange(changes, tagID ?? string.Empty, sourceValue?.GetType().Name ?? "null", true);
                filterCount++;
                return false;
            }

            if (!NeedsFilterMigration(sourceValue, canonical)) return true;

            BaseTag conversionTarget = GetStagedTarget(canonical, stagedEnums);
            EnumTag candidateEnum = null;
            int enumValueCount = 0;
            if (!allowEnumExtensions && conversionTarget is EnumTag stagedEnum)
            {
                candidateEnum = (EnumTag)stagedEnum.Clone();
                conversionTarget = candidateEnum;
                enumValueCount = stagedEnum.Values.Length;
            }

            TagFilterValueConversionResult conversion = m_FilterConverter.TryConvert(sourceValue, sourceTag ?? canonical, conversionTarget, policy);
            if (!conversion.Success || candidateEnum != null && candidateEnum.Values.Length > enumValueCount)
            {
                string reason = conversion.Success ? "Removed condition because it requires an enum option that is not part of the target tag definition." : $"Removed condition because {conversion.Error}";
                repairs.Add(new FilterConditionRepair(preset, conditionID, tagID, FilterConditionRepairAction.Removed, reason));
                RegisterChange(changes, tagID, sourceValue?.GetType().Name ?? "null", true);
                filterCount++;
                return false;
            }

            apply(canonical, conversion.Value);
            filterCount++;
            RegisterChange(changes, tagID, sourceValue.GetType().Name, true);
            repairs.Add(new FilterConditionRepair(preset, conditionID, tagID, FilterConditionRepairAction.Migrated, "Migrated condition to the current tag definition."));
            if (conversion.Impact == TagConversionImpact.Lossy) lossyCount++;
            if (conversion.Warning != null) warnings.Add(conversion.Warning);
            return true;
        }

        private static bool NeedsValueMigration(BaseTagValue value, BaseTag canonical)
        {
            if (!value.CanBindTag(canonical)) return true;
            if (value is not EnumTagValue enumValue || canonical is not EnumTag enumTag) return false;
            if (enumValue.StringValue == null) return true;
            return !enumTag.TryGetValueIndex(enumValue.StringValue, out int index) || index != enumValue.Value;
        }

        private static bool NeedsFilterMigration(TagFilterValue value, BaseTag canonical)
        {
            bool compatible = canonical switch
            {
                EmptyTag => value is EmptyTagFilterValue,
                BoolTag => value is BoolTagFilterValue,
                StringTag => value is StringTagFilterValue,
                IntTag or FloatTag => value is NumberTagFilterValue,
                EnumTag => value is EnumTagFilterValue,
                _ => false
            };
            if (!compatible) return true;
            if (value is not EnumTagFilterValue enumValue || canonical is not EnumTag enumTag) return false;
            if (enumValue.StringValue == null) return true;
            return !enumTag.TryGetValueIndex(enumValue.StringValue, out int index) || index != enumValue.Value;
        }

        private static BaseTag GetStagedTarget(BaseTag canonical, IDictionary<string, EnumTag> stagedEnums)
        {
            if (canonical is not EnumTag enumTag) return canonical;
            if (!stagedEnums.TryGetValue(canonical.ID, out EnumTag staged))
            {
                staged = (EnumTag)enumTag.Clone();
                stagedEnums.Add(canonical.ID, staged);
            }

            return staged;
        }

        private static void RegisterChange(IDictionary<string, ChangeAccumulator> changes, string tagID, string serializedType, bool isFilter)
        {
            if (!changes.TryGetValue(tagID, out ChangeAccumulator change))
            {
                change = new ChangeAccumulator();
                changes.Add(tagID, change);
            }

            change.SerializedTypes.Add(serializedType);
            if (isFilter) change.FilterCount++;
            else change.ValueCount++;
        }

        private static string GetOwnerID(object owner) => owner is BaseData data ? data.ID : string.Empty;

        private static Dictionary<string, BaseTag> CreateCanonicalIndex(TagCollection tags)
        {
            Dictionary<string, BaseTag> result = new(StringComparer.Ordinal);
            foreach (BaseTag tag in tags.AllTags)
            {
                if (tag == null) continue;
                if (result.TryGetValue(tag.ID, out BaseTag existing))
                {
                    if (!ReferenceEquals(existing, tag)) throw new InvalidOperationException($"Duplicate tag ID '{tag.ID}'.");
                    continue;
                }

                result.Add(tag.ID, tag);
            }

            return result;
        }

        private sealed class ChangeAccumulator
        {
            public HashSet<string> SerializedTypes { get; } = new(StringComparer.Ordinal);
            public int ValueCount { get; set; }
            public int FilterCount { get; set; }

            public DeferredTagMigrationChange Build(string tagID, BaseTag canonical)
            {
                return new DeferredTagMigrationChange(tagID, canonical?.Name ?? tagID, canonical?.GetType().Name ?? "Missing", SerializedTypes, ValueCount, FilterCount);
            }
        }
    }

    internal sealed class DeferredTagOwnerMutation
    {
        private readonly object m_Owner;
        private readonly List<BaseTagValue> m_ExpectedValues;
        private readonly BaseTagValue[] m_ExpectedEntries;
        private readonly List<BaseTagValue> m_PreparedValues;
        private readonly Dictionary<BaseTagValue, BaseTag> m_OriginalBindings = new(ReferenceEqualityComparer.Instance);

        public DeferredTagOwnerMutation(object owner, List<BaseTagValue> expectedValues, List<BaseTagValue> preparedValues)
        {
            m_Owner = owner;
            m_ExpectedValues = GetValues();
            m_ExpectedEntries = expectedValues.ToArray();
            m_PreparedValues = preparedValues;
        }

        public void CaptureOriginal()
        {
            List<BaseTagValue> current = GetValues();
            if (!ReferenceEquals(current, m_ExpectedValues) || current.Count != m_ExpectedEntries.Length || current.Where((value, index) => !ReferenceEquals(value, m_ExpectedEntries[index])).Any())
            {
                throw new InvalidOperationException("A detached tag value collection changed while its migration was being reviewed.");
            }
        }

        public void Apply(IReadOnlyDictionary<string, BaseTag> canonicalByID)
        {
            foreach (BaseTagValue value in m_PreparedValues)
            {
                if (!canonicalByID.TryGetValue(value.TagReferenceID, out BaseTag canonical)) throw new InvalidOperationException($"Tag definition '{value.TagReferenceID}' disappeared before migration commit.");
                m_OriginalBindings[value] = value.Tag;
                value.BindTag(canonical);
            }

            SetValues(m_PreparedValues);
        }

        public void Restore()
        {
            foreach ((BaseTagValue value, BaseTag tag) in m_OriginalBindings) value.Tag = tag;
            m_OriginalBindings.Clear();
            SetValues(m_ExpectedValues);
        }

        private List<BaseTagValue> GetValues()
        {
            return m_Owner switch
            {
                Patient patient => patient.Tags,
                Site site => site.Tags,
                _ => throw new InvalidOperationException("Unsupported detached tag value owner.")
            };
        }

        private void SetValues(List<BaseTagValue> values)
        {
            switch (m_Owner)
            {
                case Patient patient:
                    patient.Tags = values;
                    break;
                case Site site:
                    site.Tags = values;
                    break;
            }
        }
    }

    internal sealed class ReferenceEqualityComparer : IEqualityComparer<object>, IEqualityComparer<BaseTagValue>
    {
        public static ReferenceEqualityComparer Instance { get; } = new();
        public new bool Equals(object x, object y) => ReferenceEquals(x, y);
        public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        public bool Equals(BaseTagValue x, BaseTagValue y) => ReferenceEquals(x, y);
        public int GetHashCode(BaseTagValue obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}
