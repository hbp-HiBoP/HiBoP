using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace HBP.Core.Data
{
    public sealed class TagImportCreatedTag
    {
        internal TagImportCreatedTag(TagCategory category, BaseTag tag)
        {
            Category = category;
            TagID = tag.ID;
            TagName = tag.Name;
            TagType = tag.GetType().Name;
        }

        public TagCategory Category { get; }
        public string TagID { get; }
        public string TagName { get; }
        public string TagType { get; }
    }

    public sealed class TagImportEnumExtension
    {
        internal TagImportEnumExtension(EnumTag tag, IEnumerable<string> values)
        {
            TagID = tag.ID;
            TagName = tag.Name;
            Values = new ReadOnlyCollection<string>(values.ToList());
        }

        public string TagID { get; }
        public string TagName { get; }
        public ReadOnlyCollection<string> Values { get; }
    }

    public sealed class TagImportValueDiagnostic
    {
        internal TagImportValueDiagnostic(TagCategory category, string tagID, string tagName, string rawValue, string source, string owner, string reason)
        {
            Category = category;
            TagID = tagID ?? string.Empty;
            TagName = tagName ?? string.Empty;
            RawValue = rawValue ?? string.Empty;
            Source = source ?? string.Empty;
            Owner = owner ?? string.Empty;
            Reason = reason ?? string.Empty;
            Count = 1;
        }

        public TagCategory Category { get; }
        public string TagID { get; }
        public string TagName { get; }
        public string RawValue { get; }
        public string Source { get; }
        public string Owner { get; }
        public string Reason { get; }
        public int Count { get; internal set; }
    }

    public sealed class TagImportValueSummary
    {
        internal TagImportValueSummary(TagCategory category, string tagID, string tagName)
        {
            Category = category;
            TagID = tagID ?? string.Empty;
            TagName = tagName ?? string.Empty;
            Count = 1;
        }

        public TagCategory Category { get; }
        public string TagID { get; }
        public string TagName { get; }
        public int Count { get; internal set; }
    }

    public sealed class TagImportDiagnostics
    {
        private readonly List<TagImportCreatedTag> m_CreatedTags = new();
        private readonly List<TagImportEnumExtension> m_EnumExtensions = new();
        private readonly Dictionary<string, TagImportValueSummary> m_IgnoredValueSummaries = new(StringComparer.Ordinal);
        private readonly Dictionary<string, TagImportValueDiagnostic> m_IgnoredValues = new(StringComparer.Ordinal);
        private readonly Dictionary<string, TagImportValueSummary> m_IncompatibleValueSummaries = new(StringComparer.Ordinal);
        private readonly Dictionary<string, TagImportValueDiagnostic> m_IncompatibleValues = new(StringComparer.Ordinal);
        private readonly object m_Lock = new();

        public ReadOnlyCollection<TagImportCreatedTag> CreatedTags
        {
            get
            {
                lock (m_Lock)
                {
                    return new ReadOnlyCollection<TagImportCreatedTag>(m_CreatedTags.ToList());
                }
            }
        }

        public ReadOnlyCollection<TagImportEnumExtension> EnumExtensions
        {
            get
            {
                lock (m_Lock)
                {
                    return new ReadOnlyCollection<TagImportEnumExtension>(m_EnumExtensions.ToList());
                }
            }
        }

        public ReadOnlyCollection<TagImportValueDiagnostic> IgnoredValues
        {
            get
            {
                lock (m_Lock)
                {
                    return new ReadOnlyCollection<TagImportValueDiagnostic>(Order(m_IgnoredValues.Values).ToList());
                }
            }
        }

        public ReadOnlyCollection<TagImportValueDiagnostic> IncompatibleValues
        {
            get
            {
                lock (m_Lock)
                {
                    return new ReadOnlyCollection<TagImportValueDiagnostic>(Order(m_IncompatibleValues.Values).ToList());
                }
            }
        }

        public ReadOnlyCollection<TagImportValueSummary> IgnoredValueSummaries
        {
            get
            {
                lock (m_Lock)
                {
                    return new ReadOnlyCollection<TagImportValueSummary>(Order(m_IgnoredValueSummaries.Values).ToList());
                }
            }
        }

        public ReadOnlyCollection<TagImportValueSummary> IncompatibleValueSummaries
        {
            get
            {
                lock (m_Lock)
                {
                    return new ReadOnlyCollection<TagImportValueSummary>(Order(m_IncompatibleValueSummaries.Values).ToList());
                }
            }
        }

        public bool HasChanges
        {
            get
            {
                lock (m_Lock)
                {
                    return m_CreatedTags.Count > 0 || m_EnumExtensions.Count > 0 || m_IgnoredValues.Count > 0 || m_IncompatibleValues.Count > 0;
                }
            }
        }

        internal void AddCreatedTag(TagCategory category, BaseTag tag)
        {
            lock (m_Lock)
            {
                m_CreatedTags.Add(new TagImportCreatedTag(category, tag));
            }
        }

        internal void AddEnumExtension(EnumTag tag, IEnumerable<string> values)
        {
            lock (m_Lock)
            {
                m_EnumExtensions.Add(new TagImportEnumExtension(tag, values));
            }
        }

        internal void Record(RawTagValueStatus status, TagCategory category, BaseTag tag, string rawValue, string source, string owner, string reason)
        {
            if (status == RawTagValueStatus.Success) return;
            var target = status == RawTagValueStatus.Ignored ? m_IgnoredValues : m_IncompatibleValues;
            var summaryTarget = status == RawTagValueStatus.Ignored ? m_IgnoredValueSummaries : m_IncompatibleValueSummaries;
            var key = string.Join("\u001f", category, tag?.ID ?? string.Empty, tag?.Name ?? string.Empty, rawValue ?? string.Empty, source ?? string.Empty, owner ?? string.Empty, reason ?? string.Empty);
            var summaryKey = string.Join("\u001f", category, tag?.ID ?? string.Empty, tag?.Name ?? string.Empty);
            lock (m_Lock)
            {
                if (target.TryGetValue(key, out var diagnostic))
                    diagnostic.Count++;
                else
                    target.Add(key, new TagImportValueDiagnostic(category, tag?.ID, tag?.Name, rawValue, source, owner, reason));

                if (summaryTarget.TryGetValue(summaryKey, out var summary))
                    summary.Count++;
                else
                    summaryTarget.Add(summaryKey, new TagImportValueSummary(category, tag?.ID, tag?.Name));
            }
        }

        private static IOrderedEnumerable<TagImportValueDiagnostic> Order(IEnumerable<TagImportValueDiagnostic> diagnostics)
        {
            return diagnostics.OrderBy(item => item.Category).ThenBy(item => item.TagID, StringComparer.Ordinal).ThenBy(item => item.Source, StringComparer.Ordinal).ThenBy(item => item.Owner, StringComparer.Ordinal).ThenBy(item => item.RawValue, StringComparer.Ordinal);
        }

        private static IOrderedEnumerable<TagImportValueSummary> Order(IEnumerable<TagImportValueSummary> summaries)
        {
            return summaries.OrderBy(item => item.Category).ThenBy(item => item.TagName, StringComparer.OrdinalIgnoreCase).ThenBy(item => item.TagName, StringComparer.Ordinal).ThenBy(item => item.TagID, StringComparer.Ordinal);
        }
    }

    public sealed class TagImportContext
    {
        internal TagImportContext(TagCollection tags, TagParsingPolicy policy, TagImportDiagnostics diagnostics)
        {
            Tags = tags ?? throw new ArgumentNullException(nameof(tags));
            Policy = policy ?? throw new ArgumentNullException(nameof(policy));
            Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        }

        public TagCollection Tags { get; }
        public TagParsingPolicy Policy { get; }
        public TagImportDiagnostics Diagnostics { get; }

        public RawTagValueResult TryCreate(TagCategory category, BaseTag tag, string rawValue, string source = null, string owner = null)
        {
            var result = RawTagValueFactory.TryCreate(tag, rawValue, Policy, false);
            Diagnostics.Record(result.Status, category, tag, rawValue, source, owner, result.Error);
            return result;
        }
    }

    public sealed class TagImportDraft
    {
        private readonly Dictionary<string, string[]> m_EnumAdditions;
        private readonly Dictionary<string, string> m_OriginalDefinitionSignatures;
        private readonly Dictionary<string, string[]> m_OriginalEnumValues;
        private readonly BaseTag[] m_OriginalGeneralTags;
        private readonly BaseTag[] m_OriginalPatientTags;
        private readonly BaseTag[] m_OriginalSiteTags;

        private TagImportDraft(TagCollection canonicalTags, TagParsingPolicy policy, TagImportObservations observations)
        {
            m_OriginalGeneralTags = canonicalTags.GeneralTags.ToArray();
            m_OriginalPatientTags = canonicalTags.PatientsTags.ToArray();
            m_OriginalSiteTags = canonicalTags.SitesTags.ToArray();
            m_OriginalEnumValues = canonicalTags.AllTags.OfType<EnumTag>().ToDictionary(tag => tag.ID, tag => tag.Values.ToArray(), StringComparer.Ordinal);
            m_OriginalDefinitionSignatures = canonicalTags.AllTags.ToDictionary(tag => tag.ID, GetDefinitionSignature, StringComparer.Ordinal);
            Diagnostics = new TagImportDiagnostics();

            List<BaseTag> generalTags = new(m_OriginalGeneralTags);
            List<BaseTag> patientTags = new(m_OriginalPatientTags);
            List<BaseTag> siteTags = new(m_OriginalSiteTags);
            AddMissingTags(TagCategory.Patient, observations.PatientValues, policy, patientTags, generalTags);
            AddMissingTags(TagCategory.Site, observations.SiteValues, policy, siteTags, generalTags);

            Dictionary<string, SortedSet<string>> additions = new(StringComparer.Ordinal);
            CollectEnumAdditions(observations.PatientValues, patientTags.Concat(generalTags), policy, additions);
            CollectEnumAdditions(observations.SiteValues, siteTags.Concat(generalTags), policy, additions);
            m_EnumAdditions = additions.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray(), StringComparer.Ordinal);

            ReplaceExtendedEnums(generalTags);
            ReplaceExtendedEnums(patientTags);
            ReplaceExtendedEnums(siteTags);
            PreparedTags = new TagCollection(generalTags, patientTags, siteTags, canonicalTags.ID);
            Context = new TagImportContext(PreparedTags, policy, Diagnostics);
        }

        public TagCollection PreparedTags { get; }
        public TagImportDiagnostics Diagnostics { get; }
        public TagImportContext Context { get; }

        public static TagImportDraft Create(TagCollection canonicalTags, TagImportObservations observations, TagParsingPolicy policy)
        {
            if (canonicalTags == null) throw new ArgumentNullException(nameof(canonicalTags));
            if (observations == null) throw new ArgumentNullException(nameof(observations));
            return new TagImportDraft(canonicalTags, policy ?? TagParsingPolicy.Default, observations);
        }

        public TagImportCommit Commit(TagCollection canonicalTags)
        {
            if (canonicalTags == null) throw new ArgumentNullException(nameof(canonicalTags));
            ValidateStillCurrent(canonicalTags);
            try
            {
                foreach (var pair in m_EnumAdditions)
                {
                    var tag = (EnumTag)canonicalTags.AllTags.Single(item => item.ID == pair.Key);
                    tag.Values = tag.Values.Concat(pair.Value).ToArray();
                }

                var newGeneralTags = PreparedTags.GeneralTags.Where(tag => !m_OriginalGeneralTags.Contains(tag) && !m_OriginalEnumValues.ContainsKey(tag.ID)).ToArray();
                var newPatientTags = PreparedTags.PatientsTags.Where(tag => !m_OriginalPatientTags.Contains(tag) && !m_OriginalEnumValues.ContainsKey(tag.ID)).ToArray();
                var newSiteTags = PreparedTags.SitesTags.Where(tag => !m_OriginalSiteTags.Contains(tag) && !m_OriginalEnumValues.ContainsKey(tag.ID)).ToArray();
                canonicalTags.ApplyImportBatch(m_OriginalGeneralTags.Concat(newGeneralTags), m_OriginalPatientTags.Concat(newPatientTags), m_OriginalSiteTags.Concat(newSiteTags));
                var addedTagIDs = newGeneralTags.Concat(newPatientTags).Concat(newSiteTags).Select(tag => tag.ID).ToArray();
                var committedEnumValues = m_EnumAdditions.Keys.ToDictionary(id => id, id => ((EnumTag)canonicalTags.AllTags.Single(tag => tag.ID == id)).Values.ToArray(), StringComparer.Ordinal);
                return new TagImportCommit(canonicalTags, addedTagIDs, m_OriginalEnumValues, committedEnumValues);
            }
            catch
            {
                Restore(canonicalTags);
                throw;
            }
        }

        private void AddMissingTags(TagCategory category, IReadOnlyDictionary<string, List<string>> valuesByTag, TagParsingPolicy policy, ICollection<BaseTag> scopedTags, IEnumerable<BaseTag> generalTags)
        {
            foreach (var pair in Ordered(valuesByTag))
            {
                if (FindTag(scopedTags.Concat(generalTags), pair.Key) != null) continue;
                var tag = TagInferenceService.Infer(pair.Key, pair.Value, policy);
                scopedTags.Add(tag);
                Diagnostics.AddCreatedTag(category, tag);
            }
        }

        private void CollectEnumAdditions(IReadOnlyDictionary<string, List<string>> valuesByTag, IEnumerable<BaseTag> availableTags, TagParsingPolicy policy, IDictionary<string, SortedSet<string>> additions)
        {
            foreach (var pair in Ordered(valuesByTag))
            {
                if (FindTag(availableTags, pair.Key) is not EnumTag enumTag) continue;
                if (!additions.TryGetValue(enumTag.ID, out var values))
                {
                    values = new SortedSet<string>(StringComparer.Ordinal);
                    additions.Add(enumTag.ID, values);
                }

                foreach (var value in pair.Value)
                {
                    if (string.IsNullOrWhiteSpace(value) || policy.IsIgnored(value) || enumTag.TryGetValueIndex(value, out _)) continue;
                    values.Add(value);
                }
            }

            foreach (var id in additions.Where(pair => pair.Value.Count == 0).Select(pair => pair.Key).ToArray()) additions.Remove(id);
        }

        private void ReplaceExtendedEnums(IList<BaseTag> tags)
        {
            for (var index = 0; index < tags.Count; index++)
            {
                if (tags[index] is not EnumTag enumTag || !m_EnumAdditions.TryGetValue(enumTag.ID, out var addedValues)) continue;
                tags[index] = new EnumTag(enumTag.Name, enumTag.Values.Concat(addedValues), enumTag.ID);
                Diagnostics.AddEnumExtension(enumTag, addedValues);
            }
        }

        private void ValidateStillCurrent(TagCollection canonicalTags)
        {
            if (!canonicalTags.GeneralTags.SequenceEqual(m_OriginalGeneralTags) || !canonicalTags.PatientsTags.SequenceEqual(m_OriginalPatientTags) || !canonicalTags.SitesTags.SequenceEqual(m_OriginalSiteTags)) throw new InvalidOperationException("The tag collection changed while the database update was being prepared.");

            foreach (var pair in m_OriginalEnumValues)
                if (!canonicalTags.TryGetTag(pair.Key, out var tag) || tag is not EnumTag enumTag || !enumTag.Values.SequenceEqual(pair.Value))
                    throw new InvalidOperationException($"Enum tag '{pair.Key}' changed while the database update was being prepared.");

            if (canonicalTags.AllTags.Count != m_OriginalDefinitionSignatures.Count || canonicalTags.AllTags.Any(tag => !m_OriginalDefinitionSignatures.TryGetValue(tag.ID, out var signature) || !string.Equals(signature, GetDefinitionSignature(tag), StringComparison.Ordinal))) throw new InvalidOperationException("A tag definition changed while the database update was being prepared.");
        }

        private void Restore(TagCollection canonicalTags)
        {
            foreach (var pair in m_OriginalEnumValues)
                if (canonicalTags.TryGetTag(pair.Key, out var tag) && tag is EnumTag enumTag)
                    enumTag.Values = pair.Value;

            canonicalTags.ApplyImportBatch(m_OriginalGeneralTags, m_OriginalPatientTags, m_OriginalSiteTags);
        }

        private static BaseTag FindTag(IEnumerable<BaseTag> tags, string name)
        {
            return tags.FirstOrDefault(tag => string.Equals(tag?.Name?.Trim(), name?.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        private static IEnumerable<KeyValuePair<string, List<string>>> Ordered(IReadOnlyDictionary<string, List<string>> values)
        {
            return values.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase).ThenBy(pair => pair.Key, StringComparer.Ordinal);
        }

        private static string GetDefinitionSignature(BaseTag tag)
        {
            var details = tag switch
            {
                IntTag value => $"{value.Clamped}:{value.Min}:{value.Max}",
                FloatTag value => $"{value.Clamped}:{value.Min.ToString("R", CultureInfo.InvariantCulture)}:{value.Max.ToString("R", CultureInfo.InvariantCulture)}",
                EnumTag value => string.Join("\u001e", value.Values),
                _ => string.Empty
            };
            return string.Join("\u001f", tag.GetType().FullName, tag.ID, tag.Name, details);
        }
    }

    public sealed class TagImportCommit
    {
        private readonly HashSet<string> m_AddedTagIDs;
        private readonly IReadOnlyDictionary<string, string[]> m_CommittedEnumValues;
        private readonly IReadOnlyDictionary<string, string[]> m_EnumValues;
        private readonly TagCollection m_Tags;
        private bool m_RolledBack;

        internal TagImportCommit(TagCollection tags, IEnumerable<string> addedTagIDs, IReadOnlyDictionary<string, string[]> enumValues, IReadOnlyDictionary<string, string[]> committedEnumValues)
        {
            m_Tags = tags;
            m_AddedTagIDs = new HashSet<string>(addedTagIDs, StringComparer.Ordinal);
            m_EnumValues = enumValues;
            m_CommittedEnumValues = committedEnumValues;
        }

        public void Rollback()
        {
            if (m_RolledBack) return;
            foreach (var pair in m_EnumValues)
                if (m_Tags.TryGetTag(pair.Key, out var tag) && tag is EnumTag enumTag && m_CommittedEnumValues.TryGetValue(pair.Key, out var committedValues) && enumTag.Values.SequenceEqual(committedValues))
                    enumTag.Values = pair.Value;

            m_Tags.ApplyImportBatch(m_Tags.GeneralTags.Where(tag => !m_AddedTagIDs.Contains(tag.ID)), m_Tags.PatientsTags.Where(tag => !m_AddedTagIDs.Contains(tag.ID)), m_Tags.SitesTags.Where(tag => !m_AddedTagIDs.Contains(tag.ID)));
            m_RolledBack = true;
        }
    }
}
