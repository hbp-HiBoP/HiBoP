using HBP.Core.Exceptions;
using HBP.Core.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace HBP.Core.Data
{
    public sealed class LoadingReferenceIssue
    {
        public string ReferenceType { get; }
        public string ReferenceID { get; }
        public string Owner { get; }

        internal LoadingReferenceIssue(string referenceType, string referenceID, string owner)
        {
            ReferenceType = referenceType;
            ReferenceID = referenceID;
            Owner = owner;
        }

        public override string ToString()
        {
            return $"{Owner}: {ReferenceType} '{ReferenceID}' was not found.";
        }
    }

    public sealed class ReferenceResolutionException : HBPException
    {
        public ReadOnlyCollection<LoadingReferenceIssue> Issues { get; }

        public ReferenceResolutionException(IEnumerable<LoadingReferenceIssue> issues) : base(BuildMessage(issues))
        {
            Issues = new ReadOnlyCollection<LoadingReferenceIssue>(issues.ToList());
        }

        private static string BuildMessage(IEnumerable<LoadingReferenceIssue> issues)
        {
            LoadingReferenceIssue[] issueArray = issues.ToArray();
            return "Reference resolution failed:" + Environment.NewLine + string.Join(Environment.NewLine, issueArray.Select(issue => " - " + issue));
        }
    }

    public sealed class LegacyEnumReferenceWarning
    {
        public string TagID { get; }
        public string TagName { get; }
        public int ValueCount { get; private set; }
        public int FilterCount { get; private set; }

        internal LegacyEnumReferenceWarning(EnumTag tag)
        {
            TagID = tag.ID;
            TagName = tag.Name;
        }

        internal void Increment(bool isFilter)
        {
            if (isFilter)
            {
                FilterCount++;
            }
            else
            {
                ValueCount++;
            }
        }
    }

    /// <summary>
    /// Canonical indexes used to bind serialized IDs after a complete graph has
    /// been parsed. The context never reads application singletons.
    /// </summary>
    public sealed class LoadingContext
    {
        private readonly List<LoadingReferenceIssue> m_Issues = new();
        private readonly Dictionary<string, LegacyEnumReferenceWarning> m_LegacyEnumWarningsByTagID = new(StringComparer.Ordinal);
        private readonly bool m_LogLegacyEnumWarnings;
        private int m_LoggedLegacyEnumReferenceCount;

        public IReadOnlyDictionary<string, BaseTag> TagById { get; }
        public IReadOnlyDictionary<string, Protocol> ProtocolById { get; }
        public IReadOnlyDictionary<string, Patient> PatientById { get; }
        public IReadOnlyDictionary<string, Dataset> DatasetById { get; }
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, Bloc>> BlocByIdByProtocolId { get; }
        public ReadOnlyCollection<LoadingReferenceIssue> Issues => new(m_Issues);
        public ReadOnlyCollection<LegacyEnumReferenceWarning> LegacyEnumWarnings => new(m_LegacyEnumWarningsByTagID.Values.ToList());

        public LoadingContext(IEnumerable<BaseTag> tags, IEnumerable<Protocol> protocols, IEnumerable<Patient> patients = null, IEnumerable<Dataset> datasets = null, bool logLegacyEnumWarnings = true)
        {
            m_LogLegacyEnumWarnings = logLegacyEnumWarnings;
            TagById = BuildIndex(tags, "tag");
            ProtocolById = BuildIndex(protocols, "protocol");
            PatientById = BuildIndex(patients, "patient");
            DatasetById = BuildIndex(datasets, "dataset");
            BlocByIdByProtocolId = BuildBlocIndexes(ProtocolById.Values);
        }

        internal static List<T> ExcludeDuplicateIds<T>(IEnumerable<T> values, string kind, ICollection<LoadingRecoveryItem> recovered) where T : BaseData
        {
            List<T> snapshot = (values ?? Enumerable.Empty<T>()).Where(value => value != null).ToList();
            HashSet<T> excluded = new();
            foreach (IGrouping<string, T> group in snapshot.GroupBy(value => value.ID ?? string.Empty, StringComparer.Ordinal))
            {
                if (!string.IsNullOrEmpty(group.Key) && group.Count() == 1) continue;
                string reason = string.IsNullOrEmpty(group.Key) ? $"A {kind} has no ID." : $"Duplicate {kind} ID '{group.Key}' was found. Every colliding object was quarantined; none was selected implicitly.";
                foreach (T value in group)
                {
                    excluded.Add(value);
                    recovered?.Add(new LoadingRecoveryItem(kind, value, new[] { reason }));
                }
            }

            return snapshot.Where(value => !excluded.Contains(value)).ToList();
        }

        internal static List<Protocol> ExcludeProtocolsWithInvalidBlocIds(IEnumerable<Protocol> protocols, ICollection<LoadingRecoveryItem> recovered)
        {
            List<Protocol> active = new();
            foreach (Protocol protocol in protocols ?? Enumerable.Empty<Protocol>())
            {
                string[] invalidBlocIds = (protocol.Blocs ?? new()).Where(bloc => bloc == null || string.IsNullOrEmpty(bloc.ID)).Select(bloc => bloc?.ID ?? string.Empty).Concat((protocol.Blocs ?? new()).Where(bloc => bloc != null).GroupBy(bloc => bloc.ID, StringComparer.Ordinal).Where(group => group.Count() > 1).Select(group => group.Key)).Distinct(StringComparer.Ordinal).ToArray();
                if (invalidBlocIds.Length == 0)
                {
                    active.Add(protocol);
                    continue;
                }

                string ids = string.Join(", ", invalidBlocIds.Select(id => string.IsNullOrEmpty(id) ? "<empty>" : id));
                recovered?.Add(new LoadingRecoveryItem("protocol", protocol, new[] { $"Protocol '{protocol.ID}' contains missing or duplicate bloc IDs ({ids}) and was quarantined without choosing a bloc implicitly." }));
            }

            return active;
        }

        public void ResolveDatabase(IEnumerable<Patient> patients, IEnumerable<DataInfo> dataInfos)
        {
            foreach (Patient patient in patients ?? Enumerable.Empty<Patient>())
            {
                ResolvePatientTags(patient);
            }

            foreach (DataInfo dataInfo in dataInfos ?? Enumerable.Empty<DataInfo>())
            {
                dataInfo.ResolveReferences(this);
            }

            CompleteResolution();
        }

        public LoadingRecoveryReport ResolveDatabaseRecovering(IEnumerable<Patient> patients, List<DataInfo> dataInfos)
        {
            List<LoadingRecoveryItem> recovered = new();
            foreach (Patient patient in patients ?? Enumerable.Empty<Patient>()) ResolvePatientTags(patient);
            foreach (DataInfo dataInfo in (dataInfos ?? new()).ToArray())
            {
                if (TryResolveRecoverable(dataInfo, () => dataInfo.ResolveReferences(this), "data info", out LoadingRecoveryItem item)) continue;
                dataInfos.Remove(dataInfo);
                recovered.Add(item);
            }

            return new LoadingRecoveryReport(recovered);
        }

        public void ResolveProject(IEnumerable<Patient> patients, IEnumerable<Group> groups, IEnumerable<Dataset> datasets, IEnumerable<Visualization> visualizations)
        {
            foreach (Patient patient in patients ?? Enumerable.Empty<Patient>())
            {
                ResolvePatientTags(patient);
            }

            foreach (Dataset dataset in datasets ?? Enumerable.Empty<Dataset>())
            {
                dataset.ResolveReferences(this);
            }

            foreach (Group group in groups ?? Enumerable.Empty<Group>())
            {
                group.ResolveReferences(this);
            }

            foreach (Visualization visualization in visualizations ?? Enumerable.Empty<Visualization>())
            {
                visualization.ResolveReferences(this);
            }

            CompleteResolution();
        }

        public LoadingRecoveryReport ResolveProjectRecovering(List<Patient> patients, List<Group> groups, List<Dataset> datasets, List<Visualization> visualizations)
        {
            List<LoadingRecoveryItem> recovered = new();
            foreach (Patient patient in patients ?? new()) ResolvePatientTags(patient);
            foreach (Dataset dataset in (datasets ?? new()).ToArray())
            {
                if (TryResolveRecoverable(dataset, () => dataset.ResolveReferences(this), "dataset", out LoadingRecoveryItem item)) continue;
                datasets.Remove(dataset);
                recovered.Add(item);
            }

            LoadingContext dependentContext = new(TagById.Values, ProtocolById.Values, patients, datasets, false);
            foreach (Group group in (groups ?? new()).ToArray())
            {
                if (dependentContext.TryResolveRecoverable(group, () => group.ResolveReferences(dependentContext), "group", out LoadingRecoveryItem item)) continue;
                groups.Remove(group);
                recovered.Add(item);
            }

            foreach (Visualization visualization in (visualizations ?? new()).ToArray())
            {
                if (dependentContext.TryResolveRecoverable(visualization, () => visualization.ResolveReferences(dependentContext), "visualization", out LoadingRecoveryItem item)) continue;
                visualizations.Remove(visualization);
                recovered.Add(item);
            }

            return new LoadingRecoveryReport(recovered);
        }

        public void ResolveFilterConditions(FilterConditionsPresetCollection collection)
        {
            collection?.ResolveReferences(this);
            CompleteResolution();
        }

        public void ResolvePatientConfiguration(PatientConfiguration configuration)
        {
            configuration?.ResolveReferences(this);
            CompleteResolution();
        }

        internal void ResolvePatientTags(Patient patient)
        {
            if (patient == null)
            {
                return;
            }

            foreach (BaseTagValue tagValue in patient.Tags)
            {
                tagValue.ResolveReferences(this);
            }

            foreach (Site site in patient.Sites)
            {
                foreach (BaseTagValue tagValue in site.Tags)
                {
                    tagValue.ResolveReferences(this);
                }
            }
        }

        internal T ResolveRequired<T>(IReadOnlyDictionary<string, T> index, string id, string referenceType, string owner) where T : class
        {
            if (!string.IsNullOrEmpty(id) && index.TryGetValue(id, out T value))
            {
                return value;
            }

            m_Issues.Add(new LoadingReferenceIssue(referenceType, id ?? string.Empty, owner));
            return null;
        }

        internal void ReportLegacyEnumReference(EnumTag tag, bool isFilter)
        {
            if (!m_LegacyEnumWarningsByTagID.TryGetValue(tag.ID, out LegacyEnumReferenceWarning warning))
            {
                warning = new LegacyEnumReferenceWarning(tag);
                m_LegacyEnumWarningsByTagID.Add(tag.ID, warning);
            }

            warning.Increment(isFilter);
        }

        internal T ResolveOptional<T>(IReadOnlyDictionary<string, T> index, string id) where T : class
        {
            return !string.IsNullOrEmpty(id) && index.TryGetValue(id, out T value) ? value : null;
        }

        internal Bloc ResolveBloc(string protocolId, string blocId, string owner)
        {
            if (!string.IsNullOrEmpty(protocolId) && BlocByIdByProtocolId.TryGetValue(protocolId, out IReadOnlyDictionary<string, Bloc> blocs) && !string.IsNullOrEmpty(blocId) && blocs.TryGetValue(blocId, out Bloc bloc))
            {
                return bloc;
            }

            m_Issues.Add(new LoadingReferenceIssue("bloc", blocId ?? string.Empty, owner));
            return null;
        }

        internal void ResolveFilterCondition(BaseFilterCondition condition)
        {
            switch (condition)
            {
                case PatientTagFilterCondition patientTag:
                    patientTag.ResolveReferences(this);
                    break;
                case SiteTagFilterCondition siteTag:
                    siteTag.ResolveReferences(this);
                    break;
                case MultipleSiteTagsFilterCondition multipleTags:
                    multipleTags.ResolveReferences(this);
                    break;
                case AllFilterCondition all:
                    foreach (BaseFilterCondition child in all.Conditions ?? Enumerable.Empty<BaseFilterCondition>())
                    {
                        ResolveFilterCondition(child);
                    }

                    break;
                case AnyFilterCondition any:
                    foreach (BaseFilterCondition child in any.Conditions ?? Enumerable.Empty<BaseFilterCondition>())
                    {
                        ResolveFilterCondition(child);
                    }

                    break;
            }
        }

        private void ThrowIfInvalid()
        {
            if (m_Issues.Count > 0)
            {
                throw new ReferenceResolutionException(m_Issues);
            }
        }

        private bool TryResolveRecoverable(BaseData value, Action resolve, string kind, out LoadingRecoveryItem item)
        {
            int issueStart = m_Issues.Count;
            try
            {
                resolve();
            }
            catch (Exception exception)
            {
                List<string> reasons = m_Issues.Skip(issueStart).Select(issue => issue.ToString()).Append(exception.Message).ToList();
                if (m_Issues.Count > issueStart) m_Issues.RemoveRange(issueStart, m_Issues.Count - issueStart);
                item = new LoadingRecoveryItem(kind, value, reasons);
                return false;
            }

            if (m_Issues.Count == issueStart)
            {
                item = null;
                return true;
            }

            List<string> issueReasons = m_Issues.Skip(issueStart).Select(issue => issue.ToString()).ToList();
            m_Issues.RemoveRange(issueStart, m_Issues.Count - issueStart);
            item = new LoadingRecoveryItem(kind, value, issueReasons);
            return false;
        }

        private void CompleteResolution()
        {
            ThrowIfInvalid();

            int referenceCount = m_LegacyEnumWarningsByTagID.Values.Sum(warning => warning.ValueCount + warning.FilterCount);
            if (!m_LogLegacyEnumWarnings || referenceCount == m_LoggedLegacyEnumReferenceCount)
            {
                return;
            }

            m_LoggedLegacyEnumReferenceCount = referenceCount;
            string details = string.Join(", ", m_LegacyEnumWarningsByTagID.Values.Select(warning => $"{warning.TagName} ({warning.TagID}): {warning.ValueCount} value(s), {warning.FilterCount} filter(s)"));
            UnityEngine.Debug.LogWarning($"Legacy enum references were resolved from their current indices and may be incorrect if enum options were reordered in the past. {details}");
        }

        private static IReadOnlyDictionary<string, T> BuildIndex<T>(IEnumerable<T> values, string referenceType) where T : BaseData
        {
            Dictionary<string, T> result = new(StringComparer.Ordinal);
            foreach (T value in values ?? Enumerable.Empty<T>())
            {
                if (value == null)
                {
                    continue;
                }

                string id = value.ID;
                if (result.TryGetValue(id, out T existing))
                {
                    if (!ReferenceEquals(existing, value))
                    {
                        throw new InvalidOperationException($"Duplicate {referenceType} ID '{id}'.");
                    }

                    continue;
                }

                result.Add(id, value);
            }

            return result;
        }

        private static IReadOnlyDictionary<string, IReadOnlyDictionary<string, Bloc>> BuildBlocIndexes(IEnumerable<Protocol> protocols)
        {
            Dictionary<string, IReadOnlyDictionary<string, Bloc>> result = new(StringComparer.Ordinal);
            foreach (Protocol protocol in protocols)
            {
                result.Add(protocol.ID, BuildIndex(protocol.Blocs, $"bloc in protocol '{protocol.ID}'"));
            }

            return result;
        }
    }
}
