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

    public enum DeferredTagMigrationDecision
    {
        Cancel,
        Apply,
        ApplyAndRemoveIncompatibleValues,
        ApplyWithRecovery
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
        public ReadOnlyCollection<string> Warnings { get; }
        public ReadOnlyCollection<DeferredTagMigrationChange> Changes { get; }
        public int PatientValueCount { get; }
        public int SiteValueCount { get; }
        public int FilterCount { get; }
        public int LossyConversionCount { get; }
        public int DestructiveRemovalCount => Issues.Count(issue => issue.Scope is TagMigrationIssueScope.PatientValue or TagMigrationIssueScope.SiteValue);
        public int RecoveryCount => DestructiveRemovalCount;
        public int EnumAdditionCount { get; }
        public bool RequiresConfirmation => PatientValueCount > 0 || SiteValueCount > 0 || FilterCount > 0 || Issues.Count > 0 || EnumAdditionCount > 0;
        public bool CanRemoveIncompatibleValues => Issues.Count > 0 && Issues.All(issue => issue.Scope is TagMigrationIssueScope.PatientValue or TagMigrationIssueScope.SiteValue);

        internal DeferredTagMigrationPlan(DeferredTagMigrationScope scope, TagCollection canonicalTags, List<DeferredTagOwnerMutation> ownerMutations, IReadOnlyDictionary<string, EnumTag> stagedEnums, FilterConditionsPresetCollection targetFilters, FilterConditionsPresetCollection preparedFilters, IEnumerable<TagMigrationIssue> issues, IEnumerable<string> warnings, IEnumerable<DeferredTagMigrationChange> changes, int patientValueCount, int siteValueCount, int filterCount, int lossyConversionCount)
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
            Warnings = new ReadOnlyCollection<string>(warnings.Distinct(StringComparer.Ordinal).ToList());
            Changes = new ReadOnlyCollection<DeferredTagMigrationChange>(changes.ToList());
            PatientValueCount = patientValueCount;
            SiteValueCount = siteValueCount;
            FilterCount = filterCount;
            LossyConversionCount = lossyConversionCount;
            EnumAdditionCount = m_StagedEnums.Sum(pair => pair.Value.Values.Length - ((EnumTag)m_ExpectedTags[pair.Key]).Values.Length);
        }

        public void Commit(DeferredTagMigrationDecision decision)
        {
            if (m_Committed) throw new InvalidOperationException("The deferred tag migration plan has already been committed.");
            if (decision == DeferredTagMigrationDecision.Cancel) throw new OperationCanceledException("The deferred tag migration was cancelled.");
            bool recoverIncompatibleValues = decision == DeferredTagMigrationDecision.ApplyWithRecovery;
            bool removeIncompatibleValues = decision == DeferredTagMigrationDecision.ApplyAndRemoveIncompatibleValues;
            if (Issues.Count > 0 && !recoverIncompatibleValues && !removeIncompatibleValues)
            {
                throw new InvalidOperationException("The deferred tag migration contains incompatible values.");
            }

            if (Issues.Count > 0 && !CanRemoveIncompatibleValues)
            {
                throw new InvalidOperationException("Filter migration issues cannot be resolved by removing tag values.");
            }

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

                foreach (DeferredTagOwnerMutation mutation in m_OwnerMutations) mutation.Apply(m_ExpectedTags, recoverIncompatibleValues);
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

        public DeferredTagMigrationPlan Plan(DeferredTagMigrationScope scope, TagCollection canonicalTags, IEnumerable<Patient> patients, FilterConditionsPresetCollection filters, TagParsingPolicy policy)
        {
            if (canonicalTags == null) throw new ArgumentNullException(nameof(canonicalTags));
            policy ??= TagParsingPolicy.Default;
            Dictionary<string, BaseTag> canonicalByID = CreateCanonicalIndex(canonicalTags);
            Dictionary<string, EnumTag> stagedEnums = new(StringComparer.Ordinal);
            List<TagMigrationIssue> issues = new();
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
                    DeferredTagOwnerMutation mutation = PrepareOwner(patient, patient.Tags, TagMigrationIssueScope.PatientValue, canonicalByID, stagedEnums, policy, issues, warnings, changes, ref patientCount, ref lossyCount);
                    if (mutation != null) mutations.Add(mutation);
                }

                foreach (Site site in patient.Sites ?? Enumerable.Empty<Site>())
                {
                    if (site == null || !visited.Add(site)) continue;
                    DeferredTagOwnerMutation mutation = PrepareOwner(site, site.Tags, TagMigrationIssueScope.SiteValue, canonicalByID, stagedEnums, policy, issues, warnings, changes, ref siteCount, ref lossyCount);
                    if (mutation != null) mutations.Add(mutation);
                }
            }

            FilterConditionsPresetCollection preparedFilters = filters == null ? null : (FilterConditionsPresetCollection)filters.Clone();
            if (preparedFilters != null)
            {
                foreach (BaseFilterCondition condition in preparedFilters.EnumerateConditions())
                {
                    PrepareFilterCondition(condition, canonicalByID, stagedEnums, policy, issues, warnings, changes, ref filterCount, ref lossyCount);
                }
            }

            List<DeferredTagMigrationChange> migrationChanges = changes.Select(pair => pair.Value.Build(pair.Key, canonicalByID.TryGetValue(pair.Key, out BaseTag tag) ? tag : null)).ToList();
            return new DeferredTagMigrationPlan(scope, canonicalTags, mutations, stagedEnums, filters, preparedFilters, issues, warnings, migrationChanges, patientCount, siteCount, filterCount, lossyCount);
        }

        private DeferredTagOwnerMutation PrepareOwner(object owner, IEnumerable<BaseTagValue> values, TagMigrationIssueScope scope, IReadOnlyDictionary<string, BaseTag> canonicalByID, IDictionary<string, EnumTag> stagedEnums, TagParsingPolicy policy, ICollection<TagMigrationIssue> issues, ICollection<string> warnings, IDictionary<string, ChangeAccumulator> changes, ref int convertedCount, ref int lossyCount)
        {
            List<BaseTagValue> sourceValues = (values ?? Enumerable.Empty<BaseTagValue>()).ToList();
            List<BaseTagValue> preparedValues = new(sourceValues.Count);
            List<TagValueRecoveryEntry> recoveryEntries = new();
            bool changed = false;
            foreach (BaseTagValue source in sourceValues)
            {
                string tagID = source?.TagReferenceID;
                if (source == null || string.IsNullOrEmpty(tagID) || !canonicalByID.TryGetValue(tagID, out BaseTag canonical))
                {
                    string reason = $"Tag definition '{tagID ?? string.Empty}' was not found.";
                    issues.Add(new TagMigrationIssue(scope, tagID, GetOwnerID(owner), reason));
                    if (source != null) recoveryEntries.Add(new TagValueRecoveryEntry(source, reason));
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
                    issues.Add(new TagMigrationIssue(scope, tagID, GetOwnerID(owner), conversion.Error));
                    recoveryEntries.Add(new TagValueRecoveryEntry(source, conversion.Error));
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

            return changed ? new DeferredTagOwnerMutation(owner, sourceValues, preparedValues, recoveryEntries) : null;
        }

        private void PrepareFilterCondition(BaseFilterCondition condition, IReadOnlyDictionary<string, BaseTag> canonicalByID, IDictionary<string, EnumTag> stagedEnums, TagParsingPolicy policy, ICollection<TagMigrationIssue> issues, ICollection<string> warnings, IDictionary<string, ChangeAccumulator> changes, ref int filterCount, ref int lossyCount)
        {
            switch (condition)
            {
                case PatientTagFilterCondition patientTag:
                    PrepareFilter(patientTag.ID, patientTag.TagReferenceID, patientTag.Tag, patientTag.Value, (tag, value) =>
                    {
                        patientTag.Tag = tag;
                        patientTag.Value = value;
                    }, canonicalByID, stagedEnums, policy, issues, warnings, changes, ref filterCount, ref lossyCount);
                    break;
                case SiteTagFilterCondition siteTag:
                    PrepareFilter(siteTag.ID, siteTag.TagReferenceID, siteTag.Tag, siteTag.Value, (tag, value) =>
                    {
                        siteTag.Tag = tag;
                        siteTag.Value = value;
                    }, canonicalByID, stagedEnums, policy, issues, warnings, changes, ref filterCount, ref lossyCount);
                    break;
                case MultipleSiteTagsFilterCondition multiple:
                    foreach (SingleTagFilter single in multiple.TagFilters ?? Enumerable.Empty<SingleTagFilter>())
                    {
                        PrepareFilter(single.ID, single.TagReferenceID, single.Tag, single.Value, (tag, value) =>
                        {
                            single.Tag = tag;
                            single.Value = value;
                        }, canonicalByID, stagedEnums, policy, issues, warnings, changes, ref filterCount, ref lossyCount);
                    }

                    break;
                case AllFilterCondition all:
                    foreach (BaseFilterCondition child in all.Conditions ?? Enumerable.Empty<BaseFilterCondition>()) PrepareFilterCondition(child, canonicalByID, stagedEnums, policy, issues, warnings, changes, ref filterCount, ref lossyCount);
                    break;
                case AnyFilterCondition any:
                    foreach (BaseFilterCondition child in any.Conditions ?? Enumerable.Empty<BaseFilterCondition>()) PrepareFilterCondition(child, canonicalByID, stagedEnums, policy, issues, warnings, changes, ref filterCount, ref lossyCount);
                    break;
            }
        }

        private void PrepareFilter(string ownerID, string tagID, BaseTag sourceTag, TagFilterValue sourceValue, Action<BaseTag, TagFilterValue> apply, IReadOnlyDictionary<string, BaseTag> canonicalByID, IDictionary<string, EnumTag> stagedEnums, TagParsingPolicy policy, ICollection<TagMigrationIssue> issues, ICollection<string> warnings, IDictionary<string, ChangeAccumulator> changes, ref int filterCount, ref int lossyCount)
        {
            if (string.IsNullOrEmpty(tagID) || !canonicalByID.TryGetValue(tagID, out BaseTag canonical))
            {
                issues.Add(new TagMigrationIssue(TagMigrationIssueScope.Filter, tagID, ownerID, $"Tag definition '{tagID ?? string.Empty}' was not found for a filter."));
                RegisterChange(changes, tagID ?? string.Empty, sourceValue?.GetType().Name ?? "null", true);
                return;
            }

            if (!NeedsFilterMigration(sourceValue, canonical))
            {
                apply(canonical, sourceValue);
                return;
            }

            BaseTag stagedTarget = GetStagedTarget(canonical, stagedEnums);
            TagFilterValueConversionResult conversion = m_FilterConverter.TryConvert(sourceValue, sourceTag ?? canonical, stagedTarget, policy);
            if (!conversion.Success)
            {
                issues.Add(new TagMigrationIssue(TagMigrationIssueScope.Filter, tagID, ownerID, conversion.Error));
                RegisterChange(changes, tagID, sourceValue?.GetType().Name ?? "null", true);
                return;
            }

            apply(stagedTarget, conversion.Value);
            filterCount++;
            RegisterChange(changes, tagID, sourceValue.GetType().Name, true);
            if (conversion.Impact == TagConversionImpact.Lossy) lossyCount++;
            if (conversion.Warning != null) warnings.Add(conversion.Warning);
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
        private readonly List<TagValueRecoveryEntry> m_RecoveryEntries;
        private readonly List<TagValueRecoveryEntry> m_ExpectedRecoveryEntries;
        private readonly TagValueRecoveryEntry[] m_ExpectedRecoveryEntryItems;
        private readonly Dictionary<BaseTagValue, BaseTag> m_OriginalBindings = new(ReferenceEqualityComparer.Instance);

        public DeferredTagOwnerMutation(object owner, List<BaseTagValue> expectedValues, List<BaseTagValue> preparedValues, List<TagValueRecoveryEntry> recoveryEntries)
        {
            m_Owner = owner;
            m_ExpectedValues = GetValues();
            m_ExpectedEntries = expectedValues.ToArray();
            m_PreparedValues = preparedValues;
            m_RecoveryEntries = recoveryEntries;
            m_ExpectedRecoveryEntries = GetRecoveryEntries();
            m_ExpectedRecoveryEntryItems = m_ExpectedRecoveryEntries.ToArray();
        }

        public void CaptureOriginal()
        {
            List<BaseTagValue> current = GetValues();
            if (!ReferenceEquals(current, m_ExpectedValues) || current.Count != m_ExpectedEntries.Length || current.Where((value, index) => !ReferenceEquals(value, m_ExpectedEntries[index])).Any())
            {
                throw new InvalidOperationException("A detached tag value collection changed while its migration was being reviewed.");
            }

            List<TagValueRecoveryEntry> currentRecovery = GetRecoveryEntries();
            if (!ReferenceEquals(currentRecovery, m_ExpectedRecoveryEntries) || currentRecovery.Count != m_ExpectedRecoveryEntryItems.Length || currentRecovery.Where((entry, index) => !ReferenceEquals(entry, m_ExpectedRecoveryEntryItems[index])).Any())
            {
                throw new InvalidOperationException("A detached tag recovery collection changed while its migration was being reviewed.");
            }
        }

        public void Apply(IReadOnlyDictionary<string, BaseTag> canonicalByID, bool preserveInRecovery)
        {
            foreach (BaseTagValue value in m_PreparedValues)
            {
                if (!canonicalByID.TryGetValue(value.TagReferenceID, out BaseTag canonical)) throw new InvalidOperationException($"Tag definition '{value.TagReferenceID}' disappeared before migration commit.");
                m_OriginalBindings[value] = value.Tag;
                value.BindTag(canonical);
            }

            SetValues(m_PreparedValues);
            if (preserveInRecovery && m_RecoveryEntries.Count > 0)
            {
                SetRecoveryEntries(m_ExpectedRecoveryEntries.Concat(m_RecoveryEntries).ToList());
            }
        }

        public void Restore()
        {
            foreach ((BaseTagValue value, BaseTag tag) in m_OriginalBindings) value.Tag = tag;
            m_OriginalBindings.Clear();
            SetValues(m_ExpectedValues);
            SetRecoveryEntries(m_ExpectedRecoveryEntries);
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

        private List<TagValueRecoveryEntry> GetRecoveryEntries()
        {
            return m_Owner switch
            {
                Patient patient => patient.QuarantinedTagValues ??= new(),
                Site site => site.QuarantinedTagValues ??= new(),
                _ => throw new InvalidOperationException("Unsupported detached tag value owner.")
            };
        }

        private void SetRecoveryEntries(List<TagValueRecoveryEntry> entries)
        {
            switch (m_Owner)
            {
                case Patient patient:
                    patient.QuarantinedTagValues = entries;
                    break;
                case Site site:
                    site.QuarantinedTagValues = entries;
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
