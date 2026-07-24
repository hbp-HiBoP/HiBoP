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

        public ReferenceResolutionException(IEnumerable<LoadingReferenceIssue> issues)
            : base(BuildMessage(issues))
        {
            Issues = new ReadOnlyCollection<LoadingReferenceIssue>(issues.ToList());
        }

        private static string BuildMessage(IEnumerable<LoadingReferenceIssue> issues)
        {
            LoadingReferenceIssue[] issueArray = issues.ToArray();
            return "Reference resolution failed:" + Environment.NewLine
                + string.Join(Environment.NewLine, issueArray.Select(issue => " - " + issue));
        }
    }

    /// <summary>
    /// Canonical indexes used to bind serialized IDs after a complete graph has
    /// been parsed. The context never reads application singletons.
    /// </summary>
    public sealed class LoadingContext
    {
        private readonly List<LoadingReferenceIssue> m_Issues = new();

        public IReadOnlyDictionary<string, BaseTag> TagById { get; }
        public IReadOnlyDictionary<string, Protocol> ProtocolById { get; }
        public IReadOnlyDictionary<string, Patient> PatientById { get; }
        public IReadOnlyDictionary<string, Dataset> DatasetById { get; }
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, Bloc>> BlocByIdByProtocolId { get; }
        public ReadOnlyCollection<LoadingReferenceIssue> Issues => new(m_Issues);

        public LoadingContext(
            IEnumerable<BaseTag> tags,
            IEnumerable<Protocol> protocols,
            IEnumerable<Patient> patients = null,
            IEnumerable<Dataset> datasets = null)
        {
            TagById = BuildIndex(tags, "tag");
            ProtocolById = BuildIndex(protocols, "protocol");
            PatientById = BuildIndex(patients, "patient");
            DatasetById = BuildIndex(datasets, "dataset");
            BlocByIdByProtocolId = BuildBlocIndexes(ProtocolById.Values);
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

            ThrowIfInvalid();
        }

        public void ResolveProject(
            IEnumerable<Patient> patients,
            IEnumerable<Group> groups,
            IEnumerable<Dataset> datasets,
            IEnumerable<Visualization> visualizations)
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

            ThrowIfInvalid();
        }

        public void ResolveFilterConditions(FilterConditionsPresetCollection collection)
        {
            collection?.ResolveReferences(this);
            ThrowIfInvalid();
        }

        public void ResolvePatientConfiguration(PatientConfiguration configuration)
        {
            configuration?.ResolveReferences(this);
            ThrowIfInvalid();
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

        internal T ResolveRequired<T>(
            IReadOnlyDictionary<string, T> index,
            string id,
            string referenceType,
            string owner)
            where T : class
        {
            LoadingDiagnostics.RecordReferenceLookups(1);
            if (!string.IsNullOrEmpty(id) && index.TryGetValue(id, out T value))
            {
                return value;
            }

            m_Issues.Add(new LoadingReferenceIssue(referenceType, id ?? string.Empty, owner));
            return null;
        }

        internal T ResolveOptional<T>(IReadOnlyDictionary<string, T> index, string id)
            where T : class
        {
            LoadingDiagnostics.RecordReferenceLookups(1);
            return !string.IsNullOrEmpty(id) && index.TryGetValue(id, out T value) ? value : null;
        }

        internal Bloc ResolveBloc(string protocolId, string blocId, string owner)
        {
            LoadingDiagnostics.RecordReferenceLookups(1);
            if (!string.IsNullOrEmpty(protocolId)
                && BlocByIdByProtocolId.TryGetValue(protocolId, out IReadOnlyDictionary<string, Bloc> blocs)
                && !string.IsNullOrEmpty(blocId)
                && blocs.TryGetValue(blocId, out Bloc bloc))
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

        private static IReadOnlyDictionary<string, T> BuildIndex<T>(
            IEnumerable<T> values,
            string referenceType)
            where T : BaseData
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

        private static IReadOnlyDictionary<string, IReadOnlyDictionary<string, Bloc>> BuildBlocIndexes(
            IEnumerable<Protocol> protocols)
        {
            Dictionary<string, IReadOnlyDictionary<string, Bloc>> result = new(StringComparer.Ordinal);
            foreach (Protocol protocol in protocols)
            {
                result.Add(
                    protocol.ID,
                    BuildIndex(protocol.Blocs, $"bloc in protocol '{protocol.ID}'"));
            }
            return result;
        }
    }
}
