using System;
using System.Collections.Generic;
using System.Linq;
using HBP.Core.Enums;
using HBP.Core.Object3D;

namespace HBP.Data.Module3D
{
    public enum CorrelationResultSource : byte
    {
        Computed = 1,
        Imported = 2,
        Unknown = 3
    }

    /// <summary>Source identity and settings needed to interpret a correlation result.</summary>
    public sealed class CorrelationProvenance
    {
        public CorrelationResultSource Source { get; }
        public string PatientId { get; }
        public string PatientName { get; }
        public string ColumnId { get; }
        public string DatasetId { get; }
        public string DatasetName { get; }
        public string ProtocolId { get; }
        public string ProtocolName { get; }
        public string BlocId { get; }
        public string BlocName { get; }
        public string DataInfoId { get; }
        public string DataName { get; }
        public NormalizationType Normalization { get; }
        public float CorrelationThreshold { get; }
        public bool UsesBonferroniCorrection { get; }

        public CorrelationProvenance(CorrelationResultSource source, string patientId, string patientName, string columnId, string datasetId, string datasetName, string protocolId, string protocolName, string blocId, string blocName, string dataInfoId, string dataName, NormalizationType normalization, float correlationThreshold, bool usesBonferroniCorrection)
        {
            if (source < CorrelationResultSource.Computed || source > CorrelationResultSource.Unknown) throw new ArgumentOutOfRangeException(nameof(source));
            Source = source;
            PatientId = patientId ?? string.Empty;
            PatientName = patientName ?? string.Empty;
            ColumnId = columnId ?? string.Empty;
            DatasetId = datasetId ?? string.Empty;
            DatasetName = datasetName ?? string.Empty;
            ProtocolId = protocolId ?? string.Empty;
            ProtocolName = protocolName ?? string.Empty;
            BlocId = blocId ?? string.Empty;
            BlocName = blocName ?? string.Empty;
            DataInfoId = dataInfoId ?? string.Empty;
            DataName = dataName ?? string.Empty;
            if (!Enum.IsDefined(typeof(NormalizationType), normalization)) throw new ArgumentOutOfRangeException(nameof(normalization));
            if (float.IsNaN(correlationThreshold) || float.IsInfinity(correlationThreshold) || correlationThreshold < 0f) throw new ArgumentOutOfRangeException(nameof(correlationThreshold));
            Normalization = normalization;
            CorrelationThreshold = correlationThreshold;
            UsesBonferroniCorrection = usesBonferroniCorrection;
        }

        public static CorrelationProvenance Unknown(string columnId) => new(CorrelationResultSource.Unknown, string.Empty, string.Empty, columnId, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, NormalizationType.Auto, 0f, false);

        public bool Equals(CorrelationProvenance other) => other != null && Source == other.Source && PatientId == other.PatientId && PatientName == other.PatientName && ColumnId == other.ColumnId && DatasetId == other.DatasetId && DatasetName == other.DatasetName && ProtocolId == other.ProtocolId && ProtocolName == other.ProtocolName && BlocId == other.BlocId && BlocName == other.BlocName && DataInfoId == other.DataInfoId && DataName == other.DataName && Normalization == other.Normalization && CorrelationThreshold.Equals(other.CorrelationThreshold) && UsesBonferroniCorrection == other.UsesBonferroniCorrection;
    }

    /// <summary>Detached per-column matrices and provenance, ready for one atomic scene apply.</summary>
    public sealed class CorrelationResultData
    {
        public string ColumnId { get; }
        public IReadOnlyDictionary<Site, IReadOnlyDictionary<Site, float>> Correlations { get; }
        public IReadOnlyDictionary<Site, IReadOnlyDictionary<Site, float>> Means { get; }
        public CorrelationProvenance Provenance { get; }

        public CorrelationResultData(string columnId, IEnumerable<KeyValuePair<Site, Dictionary<Site, float>>> correlations, IEnumerable<KeyValuePair<Site, Dictionary<Site, float>>> means, CorrelationProvenance provenance)
        {
            if (string.IsNullOrEmpty(columnId)) throw new ArgumentException("A correlation result requires a column ID.", nameof(columnId));
            ColumnId = columnId;
            Correlations = Copy(correlations);
            Means = Copy(means);
            Provenance = provenance ?? throw new ArgumentNullException(nameof(provenance));
            if (provenance.ColumnId.Length > 0 && !StringComparer.Ordinal.Equals(provenance.ColumnId, columnId)) throw new ArgumentException("Correlation provenance belongs to a different column.", nameof(provenance));
        }

        private static IReadOnlyDictionary<Site, IReadOnlyDictionary<Site, float>> Copy(IEnumerable<KeyValuePair<Site, Dictionary<Site, float>>> values)
        {
            var result = new Dictionary<Site, IReadOnlyDictionary<Site, float>>();
            foreach (var row in values ?? Enumerable.Empty<KeyValuePair<Site, Dictionary<Site, float>>>())
            {
                if (row.Key == null || row.Value == null || result.ContainsKey(row.Key)) throw new ArgumentException("Correlation rows must have unique, non-null source sites.", nameof(values));
                result.Add(row.Key, new Dictionary<Site, float>(row.Value));
            }

            return result;
        }
    }
}
