using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace HBP.Core.Data
{
    public enum TagMigrationIssueScope
    {
        Definition,
        PatientValue,
        SiteValue,
        Filter
    }

    public sealed class TagMigrationIssue
    {
        public TagMigrationIssueScope Scope { get; }
        public string TagID { get; }
        public string OwnerID { get; }
        public string Message { get; }

        internal TagMigrationIssue(TagMigrationIssueScope scope, string tagID, string ownerID, string message)
        {
            Scope = scope;
            TagID = tagID;
            OwnerID = ownerID;
            Message = message;
        }

        public override string ToString()
        {
            return string.IsNullOrEmpty(OwnerID) ? Message : $"{OwnerID}: {Message}";
        }
    }

    public sealed class TagDefinitionChange
    {
        public string TagID { get; }
        public string Name { get; }
        public string PreviousType { get; }
        public string NewType { get; }
        public TagCategory Category { get; }

        internal TagDefinitionChange(string tagID, string name, string previousType, string newType, TagCategory category)
        {
            TagID = tagID;
            Name = name;
            PreviousType = previousType;
            NewType = newType;
            Category = category;
        }
    }

    public sealed class TagSchemaMigrationPlan
    {
        private readonly TagCollection m_TargetTags;
        private readonly TagCollection m_PreparedTags;
        private readonly FilterConditionsPresetCollection m_TargetFilters;
        private readonly FilterConditionsPresetCollection m_PreparedFilters;
        private readonly List<TagOwnerMutation> m_OwnerMutations;
        private readonly Dictionary<string, BaseTag> m_ExpectedTagDefinitions;
        private readonly string m_ExpectedTagSignature;
        private readonly string m_ExpectedFilterSignature;
        private readonly List<PatientGraphSnapshot> m_ExpectedPatientGraph;
        private TagCollection m_OriginalTags;
        private FilterConditionsPresetCollection m_OriginalFilters;
        private bool m_Committed;

        public ReadOnlyCollection<TagMigrationIssue> Issues { get; }
        public ReadOnlyCollection<TagValueRemoval> RemovedValues { get; }
        public ReadOnlyCollection<string> Warnings { get; }
        public ReadOnlyCollection<TagDefinitionChange> DefinitionChanges { get; }
        public bool IsValid => Issues.Count == 0;
        public int ChangedDefinitionCount { get; }
        public int PatientValueCount { get; }
        public int SiteValueCount { get; }
        public int FilterCount { get; }
        public int LossyConversionCount { get; }
        public int DestructiveConversionCount { get; }
        public int RemovedValueCount => RemovedValues.Count;

        internal TagSchemaMigrationPlan(TagCollection targetTags, TagCollection preparedTags, FilterConditionsPresetCollection targetFilters, FilterConditionsPresetCollection preparedFilters, List<TagOwnerMutation> ownerMutations, IEnumerable<Patient> patients, IEnumerable<TagMigrationIssue> issues, IEnumerable<TagValueRemoval> removedValues, IEnumerable<string> warnings, IEnumerable<TagDefinitionChange> definitionChanges, int patientValueCount, int siteValueCount, int filterCount, int lossyConversionCount, int destructiveConversionCount)
        {
            m_TargetTags = targetTags;
            m_PreparedTags = preparedTags;
            m_TargetFilters = targetFilters;
            m_PreparedFilters = preparedFilters;
            m_OwnerMutations = ownerMutations;
            m_ExpectedTagDefinitions = targetTags.AllTags.ToDictionary(tag => tag.ID, StringComparer.Ordinal);
            m_ExpectedTagSignature = CreateTagSignature(targetTags);
            m_ExpectedFilterSignature = targetFilters?.GetMigrationSignature();
            m_ExpectedPatientGraph = CreatePatientGraph(patients);
            Issues = new ReadOnlyCollection<TagMigrationIssue>(issues.ToList());
            RemovedValues = new ReadOnlyCollection<TagValueRemoval>(removedValues.ToList());
            Warnings = new ReadOnlyCollection<string>(warnings.Distinct(StringComparer.Ordinal).ToList());
            DefinitionChanges = new ReadOnlyCollection<TagDefinitionChange>(definitionChanges.ToList());
            ChangedDefinitionCount = DefinitionChanges.Count;
            PatientValueCount = patientValueCount;
            SiteValueCount = siteValueCount;
            FilterCount = filterCount;
            LossyConversionCount = lossyConversionCount;
            DestructiveConversionCount = destructiveConversionCount;
        }

        public void CopyPreparedTagsTo(TagCollection destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            destination.Copy(m_PreparedTags);
        }

        public bool MatchesOwnerGraph(IEnumerable<Patient> patients)
        {
            List<PatientGraphSnapshot> current = CreatePatientGraph(patients);
            if (current.Count != m_ExpectedPatientGraph.Count) return false;
            foreach (PatientGraphSnapshot expected in m_ExpectedPatientGraph)
            {
                PatientGraphSnapshot actual = current.FirstOrDefault(snapshot => ReferenceEquals(snapshot.Patient, expected.Patient));
                if (actual == null || !actual.Matches(expected)) return false;
            }

            return true;
        }

        internal void Commit()
        {
            if (m_Committed) throw new InvalidOperationException("The tag migration plan has already been committed.");
            if (!IsValid) throw new InvalidOperationException("An invalid tag migration plan cannot be committed.");

            if (m_TargetTags.AllTags.Count != m_ExpectedTagDefinitions.Count || m_TargetTags.AllTags.Any(tag => !m_ExpectedTagDefinitions.TryGetValue(tag.ID, out BaseTag expected) || !ReferenceEquals(tag, expected)))
            {
                throw new InvalidOperationException("The tag definitions changed after this migration plan was created.");
            }

            if (!string.Equals(CreateTagSignature(m_TargetTags), m_ExpectedTagSignature, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("A tag definition was edited after this migration plan was created.");
            }

            if (!MatchesOwnerGraph(m_ExpectedPatientGraph.Select(snapshot => snapshot.Patient)))
            {
                throw new InvalidOperationException("A patient or site changed after this migration plan was created.");
            }

            if (m_TargetFilters != null && !string.Equals(m_TargetFilters.GetMigrationSignature(), m_ExpectedFilterSignature, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The filter presets changed after this migration plan was created.");
            }

            m_OriginalTags = new TagCollection(m_TargetTags.GeneralTags, m_TargetTags.PatientsTags, m_TargetTags.SitesTags, m_TargetTags.ID);
            m_OriginalFilters = m_TargetFilters == null ? null : (FilterConditionsPresetCollection)m_TargetFilters.Clone();
            foreach (TagOwnerMutation mutation in m_OwnerMutations)
            {
                mutation.CaptureOriginal();
            }

            try
            {
                m_TargetTags.Copy(m_PreparedTags);
                foreach (TagOwnerMutation mutation in m_OwnerMutations)
                {
                    mutation.Apply();
                }

                m_TargetFilters?.Copy(m_PreparedFilters);
                ValidatePublishedReferences();
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

        private void ValidatePublishedReferences()
        {
            foreach (TagOwnerMutation mutation in m_OwnerMutations)
            {
                foreach (BaseTagValue value in mutation.PreparedValues)
                {
                    if (!m_TargetTags.TryGetTag(value.TagReferenceID, out BaseTag canonical) || !ReferenceEquals(value.Tag, canonical))
                    {
                        throw new InvalidOperationException($"Tag value '{value.ID}' was not rebound to its canonical tag definition.");
                    }
                }
            }
        }

        private void RollbackInternal()
        {
            if (m_OriginalTags != null) m_TargetTags.Copy(m_OriginalTags);
            foreach (TagOwnerMutation mutation in m_OwnerMutations)
            {
                mutation.Restore();
            }

            if (m_TargetFilters != null && m_OriginalFilters != null) m_TargetFilters.Copy(m_OriginalFilters);
        }

        private static string CreateTagSignature(TagCollection tags)
        {
            JsonSerializerSettings settings = new() { TypeNameHandling = TypeNameHandling.Auto };
            return JsonConvert.SerializeObject(tags, Formatting.None, settings);
        }

        private static List<PatientGraphSnapshot> CreatePatientGraph(IEnumerable<Patient> patients)
        {
            List<PatientGraphSnapshot> result = new();
            foreach (Patient patient in patients ?? Enumerable.Empty<Patient>())
            {
                if (patient != null && result.All(snapshot => !ReferenceEquals(snapshot.Patient, patient))) result.Add(new PatientGraphSnapshot(patient));
            }

            return result;
        }

        private sealed class PatientGraphSnapshot
        {
            public Patient Patient { get; }
            private readonly Site[] m_Sites;

            public PatientGraphSnapshot(Patient patient)
            {
                Patient = patient;
                m_Sites = patient.Sites?.ToArray() ?? Array.Empty<Site>();
            }

            public bool Matches(PatientGraphSnapshot other)
            {
                return m_Sites.Length == other.m_Sites.Length && m_Sites.Where((site, index) => !ReferenceEquals(site, other.m_Sites[index])).Any() == false;
            }
        }
    }

    public sealed class TagSchemaMigrationService
    {
        private readonly TagValueConversionService m_ValueConverter;

        public TagSchemaMigrationService() : this(new TagValueConversionService())
        {
        }

        public TagSchemaMigrationService(TagValueConversionService valueConverter)
        {
            m_ValueConverter = valueConverter ?? throw new ArgumentNullException(nameof(valueConverter));
        }

        public TagSchemaMigrationPlan Plan(TagCollection currentTags, TagCollection proposedTags, IEnumerable<Patient> patients, FilterConditionsPresetCollection filters, ISet<string> modifiedTagIds, TagParsingPolicy policy)
        {
            if (currentTags == null) throw new ArgumentNullException(nameof(currentTags));
            if (proposedTags == null) throw new ArgumentNullException(nameof(proposedTags));
            if (modifiedTagIds == null) throw new ArgumentNullException(nameof(modifiedTagIds));
            policy ??= TagParsingPolicy.Default;

            List<Patient> patientSnapshot = (patients ?? Enumerable.Empty<Patient>()).Where(patient => patient != null).ToList();
            Dictionary<string, BaseTag> currentByID = currentTags.AllTags.ToDictionary(tag => tag.ID, StringComparer.Ordinal);
            TagCollection preparedTags = BuildPreparedTags(proposedTags, currentByID, modifiedTagIds);
            Dictionary<string, BaseTag> preparedByID = preparedTags.AllTags.ToDictionary(tag => tag.ID, StringComparer.Ordinal);
            List<TagDefinitionChange> definitionChanges = BuildDefinitionChanges(currentTags, preparedTags, currentByID, preparedByID, modifiedTagIds);
            List<TagMigrationIssue> issues = new();
            List<TagValueRemoval> removedValues = new();
            List<string> warnings = new();
            List<TagOwnerMutation> mutations = new();
            int patientValueCount = 0;
            int siteValueCount = 0;
            int filterCount = 0;
            int lossyCount = 0;
            int destructiveCount = 0;

            foreach (string tagID in modifiedTagIds.Where(id => !string.IsNullOrEmpty(id)))
            {
                if (currentTags.TryGetCategory(tagID, out TagCategory currentCategory) && preparedTags.TryGetCategory(tagID, out TagCategory preparedCategory) && currentCategory != preparedCategory)
                {
                    issues.Add(new TagMigrationIssue(TagMigrationIssueScope.Definition, tagID, null, $"Tag '{tagID}' cannot move from category '{currentCategory}' to '{preparedCategory}'."));
                }
            }

            HashSet<object> visitedOwners = new(ReferenceComparer.Instance);
            foreach (Patient patient in patientSnapshot)
            {
                if (patient == null) continue;
                if (visitedOwners.Add(patient))
                {
                    mutations.Add(PrepareOwner(patient, patient, patient.Tags, TagMigrationIssueScope.PatientValue, preparedByID, modifiedTagIds, policy, removedValues, warnings, ref patientValueCount, ref lossyCount, ref destructiveCount));
                }

                foreach (Site site in patient.Sites ?? Enumerable.Empty<Site>())
                {
                    if (site != null && visitedOwners.Add(site))
                    {
                        mutations.Add(PrepareOwner(patient, site, site.Tags, TagMigrationIssueScope.SiteValue, preparedByID, modifiedTagIds, policy, removedValues, warnings, ref siteValueCount, ref lossyCount, ref destructiveCount));
                    }
                }
            }

            FilterConditionsPresetCollection preparedFilters = filters == null ? null : (FilterConditionsPresetCollection)filters.Clone();
            if (preparedFilters != null)
            {
                TagCollection filterPlanningTags = (TagCollection)preparedTags.Clone();
                FilterPresetRepairReport filterRepair = FilterPresetRepairService.Repair(filterPlanningTags, preparedFilters, policy, false);
                new LoadingContext(preparedTags.AllTags, Array.Empty<Protocol>(), logLegacyEnumWarnings: false).ResolveFilterConditions(preparedFilters);
                filterCount += filterRepair.AffectedFilterCount;
                destructiveCount += filterRepair.RemovedConditionCount;
                foreach (FilterConditionRepair repair in filterRepair.Repairs)
                {
                    string presetName = string.IsNullOrEmpty(repair.PresetName) ? repair.PresetID : repair.PresetName;
                    warnings.Add($"Filter preset '{presetName}': {repair.Message}");
                }
            }

            return new TagSchemaMigrationPlan(currentTags, preparedTags, filters, preparedFilters, mutations, patientSnapshot, issues, removedValues, warnings, definitionChanges, patientValueCount, siteValueCount, filterCount, lossyCount, destructiveCount);
        }

        public bool Validate(TagSchemaMigrationPlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            return plan.IsValid;
        }

        public void Commit(TagSchemaMigrationPlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            plan.Commit();
        }

        private TagOwnerMutation PrepareOwner(Patient patient, object owner, IEnumerable<BaseTagValue> sourceValues, TagMigrationIssueScope scope, IReadOnlyDictionary<string, BaseTag> preparedByID, ISet<string> modifiedTagIds, TagParsingPolicy policy, ICollection<TagValueRemoval> removedValues, ICollection<string> warnings, ref int convertedCount, ref int lossyCount, ref int destructiveCount)
        {
            List<BaseTagValue> preparedValues = new();
            foreach (BaseTagValue source in sourceValues ?? Enumerable.Empty<BaseTagValue>())
            {
                string tagID = source?.TagReferenceID;
                if (source == null || string.IsNullOrEmpty(tagID))
                {
                    const string reason = "A tag value has no tag reference.";
                    removedValues.Add(new TagValueRemoval(scope, patient, tagID, GetOwnerID(owner), source, reason));
                    destructiveCount++;
                    continue;
                }

                if (!preparedByID.TryGetValue(tagID, out BaseTag target))
                {
                    string reason = modifiedTagIds.Contains(tagID) ? $"Tag definition '{tagID}' was removed." : $"Tag definition '{tagID}' was not found in the proposed collection.";
                    removedValues.Add(new TagValueRemoval(scope, patient, tagID, GetOwnerID(owner), source, reason));
                    destructiveCount++;
                    continue;
                }

                if (modifiedTagIds.Contains(tagID) || !source.CanBindTag(target))
                {
                    TagValueConversionResult conversion = m_ValueConverter.TryConvert(source, target, policy);
                    if (!conversion.Success)
                    {
                        removedValues.Add(new TagValueRemoval(scope, patient, tagID, GetOwnerID(owner), source, conversion.Error));
                        destructiveCount++;
                        continue;
                    }

                    preparedValues.Add(conversion.Value);
                    convertedCount++;
                    CountImpact(conversion.Impact, ref lossyCount, ref destructiveCount);
                    if (conversion.Warning != null) warnings.Add(conversion.Warning);
                }
                else if (ReferenceEquals(source.Tag, target))
                {
                    preparedValues.Add(source);
                }
                else
                {
                    BaseTagValue clone = (BaseTagValue)source.Clone();
                    clone.BindTag(target);
                    preparedValues.Add(clone);
                }
            }

            return new TagOwnerMutation(owner, preparedValues);
        }

        private static string GetOwnerID(object owner)
        {
            return owner is BaseData data ? data.ID : string.Empty;
        }

        private static TagCollection BuildPreparedTags(TagCollection proposedTags, IReadOnlyDictionary<string, BaseTag> currentByID, ISet<string> modifiedTagIds)
        {
            BaseTag SelectDefinition(BaseTag proposed)
            {
                if (!modifiedTagIds.Contains(proposed.ID) && currentByID.TryGetValue(proposed.ID, out BaseTag current)) return current;
                return (BaseTag)proposed.Clone();
            }

            return new TagCollection(proposedTags.GeneralTags.Select(SelectDefinition), proposedTags.PatientsTags.Select(SelectDefinition), proposedTags.SitesTags.Select(SelectDefinition), proposedTags.ID);
        }

        private static List<TagDefinitionChange> BuildDefinitionChanges(TagCollection currentTags, TagCollection preparedTags, IReadOnlyDictionary<string, BaseTag> currentByID, IReadOnlyDictionary<string, BaseTag> preparedByID, IEnumerable<string> modifiedTagIds)
        {
            List<TagDefinitionChange> changes = new();
            foreach (string tagID in modifiedTagIds.Where(id => !string.IsNullOrEmpty(id)).Distinct(StringComparer.Ordinal))
            {
                currentByID.TryGetValue(tagID, out BaseTag current);
                preparedByID.TryGetValue(tagID, out BaseTag prepared);
                if (current == null && prepared == null) continue;
                TagCollection categorySource = prepared == null ? currentTags : preparedTags;
                categorySource.TryGetCategory(tagID, out TagCategory category);
                changes.Add(new TagDefinitionChange(tagID, prepared?.Name ?? current.Name, current?.GetType().Name ?? "New", prepared?.GetType().Name ?? "Deleted", category));
            }

            return changes;
        }

        private static void CountImpact(TagConversionImpact impact, ref int lossyCount, ref int destructiveCount)
        {
            if (impact == TagConversionImpact.Lossy) lossyCount++;
            if (impact == TagConversionImpact.Destructive) destructiveCount++;
        }

        private sealed class ReferenceComparer : IEqualityComparer<object>
        {
            public static ReferenceComparer Instance { get; } = new();
            public new bool Equals(object x, object y) => ReferenceEquals(x, y);
            public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
        }
    }

    internal sealed class TagOwnerMutation
    {
        private readonly object m_Owner;
        private readonly List<BaseTagValue> m_ExpectedValues;
        private readonly BaseTagValue[] m_ExpectedEntries;
        private List<BaseTagValue> m_OriginalValues;
        public List<BaseTagValue> PreparedValues { get; }

        public TagOwnerMutation(object owner, List<BaseTagValue> preparedValues)
        {
            m_Owner = owner;
            PreparedValues = preparedValues;
            m_ExpectedValues = GetValues();
            m_ExpectedEntries = m_ExpectedValues.ToArray();
        }

        public void CaptureOriginal()
        {
            List<BaseTagValue> currentValues = GetValues();
            if (!ReferenceEquals(currentValues, m_ExpectedValues) || currentValues.Count != m_ExpectedEntries.Length || currentValues.Where((value, index) => !ReferenceEquals(value, m_ExpectedEntries[index])).Any())
            {
                throw new InvalidOperationException("A tag value collection changed after this migration plan was created.");
            }

            m_OriginalValues = currentValues;
        }

        public void Apply()
        {
            SetValues(PreparedValues);
        }

        public void Restore()
        {
            if (m_OriginalValues != null) SetValues(m_OriginalValues);
        }

        private List<BaseTagValue> GetValues()
        {
            return m_Owner switch
            {
                Patient patient => patient.Tags,
                Site site => site.Tags,
                _ => throw new InvalidOperationException("Unsupported tag value owner.")
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
                default:
                    throw new InvalidOperationException("Unsupported tag value owner.");
            }
        }
    }
}
