using System;
using System.Collections.Generic;
using System.Linq;

namespace HBP.Core.Data
{
    public sealed class TagImportObservations
    {
        private readonly Dictionary<string, List<string>> m_PatientValues = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<string>> m_SiteValues = new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyDictionary<string, List<string>> PatientValues => m_PatientValues;
        public IReadOnlyDictionary<string, List<string>> SiteValues => m_SiteValues;

        public void AddPatientValue(string tagName, string value)
        {
            Add(m_PatientValues, tagName, value);
        }

        public void AddSiteValue(string tagName, string value)
        {
            Add(m_SiteValues, tagName, value);
        }

        public void Merge(TagImportObservations observations)
        {
            if (observations == null) return;
            foreach (var pair in observations.m_PatientValues)
            {
                foreach (string value in pair.Value) AddPatientValue(pair.Key, value);
            }

            foreach (var pair in observations.m_SiteValues)
            {
                foreach (string value in pair.Value) AddSiteValue(pair.Key, value);
            }
        }

        public IReadOnlyList<BaseTag> CreateMissingTags(TagCollection tags, TagParsingPolicy policy)
        {
            if (tags == null) throw new ArgumentNullException(nameof(tags));
            if (policy == null) throw new ArgumentNullException(nameof(policy));

            List<BaseTag> created = new();
            CreateMissing(tags, policy, m_PatientValues, tags.PatientsTags.Concat(tags.GeneralTags), tags.AddPatientTag, created);
            CreateMissing(tags, policy, m_SiteValues, tags.SitesTags.Concat(tags.GeneralTags), tags.AddSiteTag, created);
            return created;
        }

        private static void Add(IDictionary<string, List<string>> valuesByTag, string tagName, string value)
        {
            if (string.IsNullOrWhiteSpace(tagName)) return;
            string normalizedName = tagName.Trim();
            if (!valuesByTag.TryGetValue(normalizedName, out List<string> values))
            {
                values = new List<string>();
                valuesByTag.Add(normalizedName, values);
            }
            else
            {
                string storedName = valuesByTag.Keys.First(key => string.Equals(key, normalizedName, StringComparison.OrdinalIgnoreCase));
                if (string.Compare(normalizedName, storedName, StringComparison.Ordinal) < 0)
                {
                    valuesByTag.Remove(storedName);
                    valuesByTag.Add(normalizedName, values);
                }
            }

            values.Add(value);
        }

        private static void CreateMissing(TagCollection tags, TagParsingPolicy policy, IReadOnlyDictionary<string, List<string>> valuesByTag, IEnumerable<BaseTag> existingTags, Action<BaseTag, bool> add, ICollection<BaseTag> created)
        {
            HashSet<string> existingNames = new(existingTags.Where(tag => tag != null && !string.IsNullOrWhiteSpace(tag.Name)).Select(tag => tag.Name.Trim()), StringComparer.OrdinalIgnoreCase);
            foreach (var pair in valuesByTag.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase).ThenBy(pair => pair.Key, StringComparer.Ordinal))
            {
                if (existingNames.Contains(pair.Key)) continue;
                BaseTag tag = TagInferenceService.Infer(pair.Key, pair.Value, policy);
                add(tag, false);
                existingNames.Add(pair.Key);
                created.Add(tag);
            }
        }
    }
}
