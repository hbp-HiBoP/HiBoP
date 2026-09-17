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
        private const int MaxBytes = 8 * 1024 * 1024;
        private static readonly UTF8Encoding Utf8 = new(false, true);
        private readonly ColumnResult[] m_Columns;

        private sealed class ColumnResult
        {
            public string Id;
            public string[] CorrelationRows;
            public PairValue[] Correlations;
            public string[] MeanRows;
            public PairValue[] Means;
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
                Means = Flatten(column, column.CorrelationMeanBySitePair)
            }).ToArray();
            return columns.Any(column => column.CorrelationRows.Length > 0 || column.MeanRows.Length > 0) ? new CorrelationResultResource(columns) : null;
        }

        public byte[] Encode()
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream, Utf8, true);
            writer.Write(0x31524348); // HCR1
            writer.Write(m_Columns.Length);
            foreach (ColumnResult column in m_Columns)
            {
                WriteText(writer, column.Id);
                WriteRows(writer, column.CorrelationRows);
                WritePairs(writer, column.Correlations);
                WriteRows(writer, column.MeanRows);
                WritePairs(writer, column.Means);
            }

            if (stream.Length > MaxBytes) throw new InvalidDataException("Correlation result exceeds the resource limit.");
            return stream.ToArray();
        }

        public static CorrelationResultResource Decode(byte[] bytes)
        {
            if (bytes == null || bytes.Length > MaxBytes) throw new InvalidDataException("Invalid correlation resource length.");
            try
            {
                using var stream = new MemoryStream(bytes, false);
                using var reader = new BinaryReader(stream, Utf8, true);
                if (reader.ReadInt32() != 0x31524348) throw new InvalidDataException("Invalid correlation resource magic.");
                int count = reader.ReadInt32();
                if (count < 0 || count > 4096) throw new InvalidDataException("Invalid correlation column count.");
                var columns = new ColumnResult[count];
                for (int i = 0; i < count; i++)
                    columns[i] = new ColumnResult { Id = ReadText(reader), CorrelationRows = ReadRows(reader), Correlations = ReadPairs(reader), MeanRows = ReadRows(reader), Means = ReadPairs(reader) };
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
            string[] expectedColumns = scene.ColumnsIEEG.Select(column => column.ColumnData.ID).OrderBy(id => id, StringComparer.Ordinal).ToArray();
            if (!m_Columns.Select(column => column.Id).SequenceEqual(expectedColumns)) throw new InvalidDataException("Correlation columns differ from the prepared scene.");
            var sites = new HashSet<string>(preparedSiteIds, StringComparer.Ordinal);
            foreach (ColumnResult column in m_Columns)
                if (column.CorrelationRows.Concat(column.MeanRows).Any(id => !sites.Contains(id)) || column.Correlations.Concat(column.Means).Any(pair => !sites.Contains(pair.From) || !sites.Contains(pair.To)) || column.Correlations.Any(pair => !column.CorrelationRows.Contains(pair.From)) || column.Means.Any(pair => !column.MeanRows.Contains(pair.From)))
                    throw new InvalidDataException("Correlation site differs from the prepared implantation.");
        }

        public void Apply(Base3DScene scene)
        {
            foreach (ColumnResult result in m_Columns)
            {
                Column3DIEEG column = scene.ColumnsIEEG.Single(item => item.ColumnData.ID == result.Id);
                var sites = column.Sites.ToDictionary(site => site.Information.FullID, StringComparer.Ordinal);
                column.CorrelationBySitePair = Expand(result.CorrelationRows, result.Correlations, sites);
                column.CorrelationMeanBySitePair = Expand(result.MeanRows, result.Means, sites);
            }
        }

        public static string Reference(byte[] bytes)
        {
            using SHA256 sha = SHA256.Create();
            return "correlation:" + BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant() + ":1";
        }

        private static PairValue[] Flatten(Column3DIEEG column, Dictionary<HBP.Core.Object3D.Site, Dictionary<HBP.Core.Object3D.Site, float>> matrix)
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

        private static string[] Rows(Column3DIEEG column, Dictionary<HBP.Core.Object3D.Site, Dictionary<HBP.Core.Object3D.Site, float>> matrix)
        {
            var owned = new HashSet<HBP.Core.Object3D.Site>(column.Sites);
            if (matrix.Keys.Any(site => !owned.Contains(site))) throw new InvalidDataException("Correlation row is outside the prepared column.");
            return matrix.Keys.Select(site => site.Information.FullID).OrderBy(id => id, StringComparer.Ordinal).ToArray();
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
