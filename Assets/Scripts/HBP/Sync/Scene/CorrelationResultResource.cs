using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using HBP.Data.Module3D;
using HBP.Sync;

namespace HBP.Sync.Scene
{
    /// <summary>Canonical, content-addressed scientific correlation values for a prepared scene.</summary>
    public sealed class CorrelationResultResource
    {
        public const int MaximumBytes = 8 * 1024 * 1024;
        private static readonly UTF8Encoding Utf8 = new(false, true);
        private readonly ColumnResult[] m_Columns;

        private sealed class ColumnResult
        {
            public string Id;
            public string[] CorrelationRows;
            public PairValue[] Correlations;
            public string[] MeanRows;
            public PairValue[] Means;
            public CorrelationProvenance Provenance;
        }

        private readonly struct PairValue
        {
            public readonly string From;
            public readonly string To;
            public readonly float Value;

            public PairValue(string from, string to, float value)
            {
                From = from;
                To = to;
                Value = value;
            }
        }

        private CorrelationResultResource(ColumnResult[] columns) => m_Columns = columns;

        public static CorrelationResultResource Capture(Base3DScene scene)
        {
            var columns = scene.ColumnsIEEG.OrderBy(column => column.ColumnData.ID, StringComparer.Ordinal).Select(column => new ColumnResult
            {
                Id = column.ColumnData.ID,
                CorrelationRows = Rows(column, column.CorrelationBySitePair),
                Correlations = Flatten(column, column.CorrelationBySitePair),
                MeanRows = Rows(column, column.CorrelationMeanBySitePair),
                Means = Flatten(column, column.CorrelationMeanBySitePair),
                Provenance = column.CorrelationProvenance ?? CorrelationProvenance.Unknown(column.ColumnData.ID)
            }).ToArray();
            return columns.Any(column => column.CorrelationRows.Length > 0 || column.MeanRows.Length > 0) ? new CorrelationResultResource(columns) : null;
        }

        public static CorrelationResultResource FromResults(Base3DScene scene, IEnumerable<CorrelationResultData> results)
        {
            if (scene == null) throw new ArgumentNullException(nameof(scene));
            if (results == null) throw new ArgumentNullException(nameof(results));
            var sceneColumns = scene.ColumnsIEEG.ToDictionary(column => column.ColumnData.ID, StringComparer.Ordinal);
            CorrelationResultData[] values = results.ToArray();
            if (values.Length != sceneColumns.Count || values.Select(value => value.ColumnId).Distinct(StringComparer.Ordinal).Count() != sceneColumns.Count || values.Any(value => !sceneColumns.ContainsKey(value.ColumnId)))
                throw new InvalidDataException("Correlation results do not match the prepared IEEG column roster.");

            ColumnResult[] columns = values.OrderBy(value => value.ColumnId, StringComparer.Ordinal).Select(value =>
            {
                Column3DIEEG column = sceneColumns[value.ColumnId];
                if (value.Provenance == null || !StringComparer.Ordinal.Equals(value.Provenance.ColumnId, value.ColumnId)) throw new InvalidDataException("Correlation provenance does not match its column.");
                var owned = new HashSet<HBP.Core.Object3D.Site>(column.Sites);
                ValidateMatrixOwnership(value.Correlations, owned);
                ValidateMatrixOwnership(value.Means, owned);
                return new ColumnResult
                {
                    Id = value.ColumnId,
                    CorrelationRows = Rows(column, value.Correlations),
                    Correlations = Flatten(column, value.Correlations),
                    MeanRows = Rows(column, value.Means),
                    Means = Flatten(column, value.Means),
                    Provenance = value.Provenance
                };
            }).ToArray();
            if (!columns.Any(column => column.CorrelationRows.Length > 0 || column.MeanRows.Length > 0)) throw new InvalidDataException("A correlation result cannot be empty.");
            return new CorrelationResultResource(columns);
        }

        public byte[] Encode()
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream, Utf8, true);
            writer.Write(0x32524348); // HCR2
            writer.Write(m_Columns.Length);
            foreach (ColumnResult column in m_Columns)
            {
                WriteText(writer, column.Id);
                WriteProvenance(writer, column.Provenance);
                WriteRows(writer, column.CorrelationRows);
                WritePairs(writer, column.Correlations);
                WriteRows(writer, column.MeanRows);
                WritePairs(writer, column.Means);
            }

            if (stream.Length > MaximumBytes) throw new InvalidDataException("Correlation result exceeds the resource limit.");
            return stream.ToArray();
        }

        public static CorrelationResultResource Decode(byte[] bytes)
        {
            if (bytes == null || bytes.Length > MaximumBytes) throw new InvalidDataException("Invalid correlation resource length.");
            try
            {
                using var stream = new MemoryStream(bytes, false);
                using var reader = new BinaryReader(stream, Utf8, true);
                if (reader.ReadInt32() != 0x32524348) throw new InvalidDataException("Invalid correlation resource magic.");
                int count = reader.ReadInt32();
                if (count < 0 || count > 4096) throw new InvalidDataException("Invalid correlation column count.");
                var columns = new ColumnResult[count];
                for (int i = 0; i < count; i++)
                {
                    string id = ReadText(reader);
                    CorrelationProvenance provenance = ReadProvenance(reader);
                    if (!StringComparer.Ordinal.Equals(id, provenance.ColumnId)) throw new InvalidDataException("Correlation provenance does not match its column.");
                    columns[i] = new ColumnResult { Id = id, Provenance = provenance, CorrelationRows = ReadRows(reader), Correlations = ReadPairs(reader), MeanRows = ReadRows(reader), Means = ReadPairs(reader) };
                }

                if (stream.Position != stream.Length) throw new InvalidDataException("Trailing correlation resource data.");
                var result = new CorrelationResultResource(columns);
                if (!columns.Any(column => column.CorrelationRows.Length > 0 || column.MeanRows.Length > 0)) throw new InvalidDataException("Empty correlation resource.");
                if (!result.Encode().SequenceEqual(bytes)) throw new InvalidDataException("Noncanonical correlation resource.");
                return result;
            }
            catch (EndOfStreamException exception)
            {
                throw new InvalidDataException("Truncated correlation resource.", exception);
            }
            catch (DecoderFallbackException exception)
            {
                throw new InvalidDataException("Invalid correlation resource text.", exception);
            }
        }

        public void ValidateFor(Base3DScene scene, IReadOnlyCollection<string> preparedSiteIds)
        {
            if (scene == null) throw new ArgumentNullException(nameof(scene));
            var prepared = new HashSet<string>(preparedSiteIds ?? throw new ArgumentNullException(nameof(preparedSiteIds)), StringComparer.Ordinal);
            string[] expectedColumns = scene.ColumnsIEEG.Select(column => column.ColumnData.ID).OrderBy(id => id, StringComparer.Ordinal).ToArray();
            if (!m_Columns.Select(column => column.Id).SequenceEqual(expectedColumns)) throw new InvalidDataException("Correlation columns differ from the prepared scene.");
            foreach (ColumnResult result in m_Columns)
            {
                Column3DIEEG column = scene.ColumnsIEEG.Single(item => item.ColumnData.ID == result.Id);
                if (result.Provenance == null || !StringComparer.Ordinal.Equals(result.Provenance.ColumnId, result.Id)) throw new InvalidDataException("Correlation provenance does not match its column.");
                var sites = new HashSet<string>(column.Sites.Select(site => site.Information.FullID), StringComparer.Ordinal);
                if (sites.Any(id => !prepared.Contains(id)) || result.CorrelationRows.Concat(result.MeanRows).Any(id => !sites.Contains(id)) || result.Correlations.Concat(result.Means).Any(pair => !sites.Contains(pair.From) || !sites.Contains(pair.To)) || result.Correlations.Any(pair => !result.CorrelationRows.Contains(pair.From)) || result.Means.Any(pair => !result.MeanRows.Contains(pair.From)))
                    throw new InvalidDataException("Correlation site differs from its prepared column.");
            }
        }

        public void Apply(Base3DScene scene)
        {
            ValidateFor(scene, scene.Columns.SelectMany(column => column.Sites).Select(site => site.Information.FullID).ToArray());
            var results = new List<CorrelationResultData>(m_Columns.Length);
            foreach (ColumnResult result in m_Columns)
            {
                Column3DIEEG column = scene.ColumnsIEEG.Single(item => item.ColumnData.ID == result.Id);
                var sites = column.Sites.ToDictionary(site => site.Information.FullID, StringComparer.Ordinal);
                results.Add(new CorrelationResultData(result.Id, Expand(result.CorrelationRows, result.Correlations, sites), Expand(result.MeanRows, result.Means, sites), result.Provenance));
            }

            scene.ApplyCorrelationResults(results);
        }

        public static string Reference(byte[] bytes)
        {
            using SHA256 sha = HBP.Core.Tools.Sha256.Create();
            return "correlation:" + BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant() + ":2";
        }

        private static PairValue[] Flatten<TMap>(Column3DIEEG column, IEnumerable<KeyValuePair<HBP.Core.Object3D.Site, TMap>> matrix) where TMap : IEnumerable<KeyValuePair<HBP.Core.Object3D.Site, float>>
        {
            var owned = new HashSet<HBP.Core.Object3D.Site>(column.Sites);
            var values = new List<PairValue>();
            foreach (var row in matrix)
            {
                if (!owned.Contains(row.Key)) throw new InvalidDataException("Correlation source site is outside the prepared column.");
                foreach (var pair in row.Value)
                {
                    if (!owned.Contains(pair.Key)) throw new InvalidDataException("Correlation target site is outside the prepared column.");
                    values.Add(new PairValue(row.Key.Information.FullID, pair.Key.Information.FullID, pair.Value));
                }
            }

            return values.OrderBy(pair => pair.From, StringComparer.Ordinal).ThenBy(pair => pair.To, StringComparer.Ordinal).ToArray();
        }

        private static string[] Rows<TMap>(Column3DIEEG column, IEnumerable<KeyValuePair<HBP.Core.Object3D.Site, TMap>> matrix) where TMap : IEnumerable<KeyValuePair<HBP.Core.Object3D.Site, float>>
        {
            var owned = new HashSet<HBP.Core.Object3D.Site>(column.Sites);
            KeyValuePair<HBP.Core.Object3D.Site, TMap>[] rows = matrix.ToArray();
            if (rows.Any(row => !owned.Contains(row.Key))) throw new InvalidDataException("Correlation row is outside the prepared column.");
            return rows.Select(row => row.Key.Information.FullID).OrderBy(id => id, StringComparer.Ordinal).ToArray();
        }

        private static void ValidateMatrixOwnership(IReadOnlyDictionary<HBP.Core.Object3D.Site, IReadOnlyDictionary<HBP.Core.Object3D.Site, float>> matrix, HashSet<HBP.Core.Object3D.Site> owned)
        {
            foreach (var row in matrix)
            {
                if (!row.Key || !owned.Contains(row.Key) || row.Value == null) throw new InvalidDataException("Correlation row is outside its prepared column.");
                foreach (var pair in row.Value)
                    if (!pair.Key || !owned.Contains(pair.Key) || float.IsNaN(pair.Value) || float.IsInfinity(pair.Value))
                        throw new InvalidDataException("Correlation pair is invalid or outside its prepared column.");
            }
        }

        private static Dictionary<HBP.Core.Object3D.Site, Dictionary<HBP.Core.Object3D.Site, float>> Expand(string[] rows, PairValue[] pairs, Dictionary<string, HBP.Core.Object3D.Site> sites)
        {
            var matrix = new Dictionary<HBP.Core.Object3D.Site, Dictionary<HBP.Core.Object3D.Site, float>>();
            foreach (string row in rows) matrix.Add(sites[row], new Dictionary<HBP.Core.Object3D.Site, float>());
            foreach (PairValue pair in pairs)
            {
                var source = sites[pair.From];
                matrix[source].Add(sites[pair.To], pair.Value);
            }

            return matrix;
        }

        private static void WriteProvenance(BinaryWriter writer, CorrelationProvenance provenance)
        {
            if (provenance == null) throw new InvalidDataException("A correlation result requires provenance.");
            writer.Write((byte)provenance.Source);
            WriteOptionalText(writer, provenance.PatientId);
            WriteOptionalText(writer, provenance.PatientName);
            WriteOptionalText(writer, provenance.ColumnId);
            WriteOptionalText(writer, provenance.DatasetId);
            WriteOptionalText(writer, provenance.DatasetName);
            WriteOptionalText(writer, provenance.ProtocolId);
            WriteOptionalText(writer, provenance.ProtocolName);
            WriteOptionalText(writer, provenance.BlocId);
            WriteOptionalText(writer, provenance.BlocName);
            WriteOptionalText(writer, provenance.DataInfoId);
            WriteOptionalText(writer, provenance.DataName);
            writer.Write((int)provenance.Normalization);
            writer.Write(StateValue.Float(provenance.CorrelationThreshold));
            writer.Write((byte)(provenance.UsesBonferroniCorrection ? 1 : 0));
        }

        private static CorrelationProvenance ReadProvenance(BinaryReader reader)
        {
            byte sourceValue = reader.ReadByte();
            if (sourceValue < (byte)CorrelationResultSource.Computed || sourceValue > (byte)CorrelationResultSource.Unknown) throw new InvalidDataException("Invalid correlation provenance source.");
            string patientId = ReadOptionalText(reader);
            string patientName = ReadOptionalText(reader);
            string columnId = ReadOptionalText(reader);
            string datasetId = ReadOptionalText(reader);
            string datasetName = ReadOptionalText(reader);
            string protocolId = ReadOptionalText(reader);
            string protocolName = ReadOptionalText(reader);
            string blocId = ReadOptionalText(reader);
            string blocName = ReadOptionalText(reader);
            string dataInfoId = ReadOptionalText(reader);
            string dataName = ReadOptionalText(reader);
            int normalization = reader.ReadInt32();
            float threshold = reader.ReadSingle();
            byte bonferroni = reader.ReadByte();
            if (!Enum.IsDefined(typeof(HBP.Core.Enums.NormalizationType), normalization) || bonferroni > 1) throw new InvalidDataException("Invalid correlation provenance settings.");
            try
            {
                return new CorrelationProvenance((CorrelationResultSource)sourceValue, patientId, patientName, columnId, datasetId, datasetName, protocolId, protocolName, blocId, blocName, dataInfoId, dataName, (HBP.Core.Enums.NormalizationType)normalization, threshold, bonferroni != 0);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidDataException("Invalid correlation provenance values.", exception);
            }
        }

        private static void WriteOptionalText(BinaryWriter writer, string value)
        {
            value ??= string.Empty;
            byte[] bytes = Utf8.GetBytes(value);
            if (bytes.Length > 256 || !value.IsNormalized(NormalizationForm.FormC) || value.Any(char.IsControl)) throw new InvalidDataException("Correlation provenance text exceeds its bound or is not canonical.");
            writer.Write(bytes.Length);
            writer.Write(bytes);
        }

        private static string ReadOptionalText(BinaryReader reader)
        {
            int length = reader.ReadInt32();
            if (length < 0 || length > 256 || reader.BaseStream.Length - reader.BaseStream.Position < length) throw new InvalidDataException("Invalid correlation provenance text length.");
            string value = Utf8.GetString(reader.ReadBytes(length));
            if (!value.IsNormalized(NormalizationForm.FormC) || value.Any(char.IsControl)) throw new InvalidDataException("Correlation provenance text is not canonical.");
            return value;
        }

        private static void WriteRows(BinaryWriter writer, string[] rows)
        {
            writer.Write(rows.Length);
            foreach (string row in rows) WriteText(writer, row);
        }

        private static string[] ReadRows(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            if (count < 0 || count > 4096) throw new InvalidDataException("Invalid correlation row count.");
            var rows = new string[count];
            for (int i = 0; i < count; i++)
            {
                rows[i] = ReadText(reader);
                if (i > 0 && StringComparer.Ordinal.Compare(rows[i - 1], rows[i]) >= 0) throw new InvalidDataException("Correlation rows are not in canonical order.");
            }

            return rows;
        }

        private static void WritePairs(BinaryWriter writer, PairValue[] pairs)
        {
            writer.Write(pairs.Length);
            foreach (PairValue pair in pairs)
            {
                WriteText(writer, pair.From);
                WriteText(writer, pair.To);
                writer.Write(BitConverter.ToSingle(StateValue.Float(pair.Value), 0));
            }
        }

        private static PairValue[] ReadPairs(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            if (count < 0 || count > 1000000 || count > (reader.BaseStream.Length - reader.BaseStream.Position) / 14) throw new InvalidDataException("Invalid correlation pair count.");
            var pairs = new PairValue[count];
            string previousFrom = null, previousTo = null;
            for (int i = 0; i < count; i++)
            {
                string from = ReadText(reader), to = ReadText(reader);
                if (previousFrom != null && (StringComparer.Ordinal.Compare(previousFrom, from) > 0 || previousFrom == from && StringComparer.Ordinal.Compare(previousTo, to) >= 0))
                    throw new InvalidDataException("Correlation pairs are not in canonical order.");
                float value = reader.ReadSingle();
                if (float.IsNaN(value) || float.IsInfinity(value)) throw new InvalidDataException("Nonfinite correlation value.");
                pairs[i] = new PairValue(from, to, value);
                previousFrom = from;
                previousTo = to;
            }

            return pairs;
        }

        private static void WriteText(BinaryWriter writer, string value)
        {
            byte[] bytes = Utf8.GetBytes(value);
            if (!ValidId(value)) throw new InvalidDataException("Invalid correlation ID.");
            writer.Write(bytes.Length);
            writer.Write(bytes);
        }

        private static string ReadText(BinaryReader reader)
        {
            int length = reader.ReadInt32();
            if (length <= 0 || length > 256 || reader.BaseStream.Length - reader.BaseStream.Position < length)
                throw new InvalidDataException("Invalid correlation ID length.");
            string value = Utf8.GetString(reader.ReadBytes(length));
            if (!ValidId(value)) throw new InvalidDataException("Invalid correlation ID.");
            return value;
        }

        private static bool ValidId(string value) => value != null && value.Length > 0 && value.Length <= 256 && Utf8.GetByteCount(value) <= 256 && value.IsNormalized(NormalizationForm.FormC) && value.All(character => !char.IsControl(character));
    }
}
