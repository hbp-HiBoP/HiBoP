using HBP.Core.Database;
using HBP.Core.Object3D;
using HBP.Core.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace HBP.Core.Data
{
    public static class TagImportScanner
    {
        private static readonly Regex s_CsvParser = new(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");

        public static TagImportObservations Scan(IEnumerable<DatabaseReference> references, CancellationToken token = default)
        {
            TagImportObservations observations = new();
            foreach (DatabaseReference reference in references ?? Enumerable.Empty<DatabaseReference>())
            {
                token.ThrowIfCancellationRequested();
                if (reference == null || string.IsNullOrWhiteSpace(reference.Path)) continue;
                switch (reference.Type)
                {
                    case DatabaseType.Brainvisa:
                        ScanIntranat(reference.Path, observations, token);
                        break;
                    case DatabaseType.BIDS:
                        ScanBids(reference.Path, observations, token);
                        break;
                    case DatabaseType.Tags:
                        ScanTagsDatabase(reference.Path, observations, token);
                        break;
                }
            }

            return observations;
        }

        public static void ScanDelimitedFile(string path, char separator, bool patientTags, TagImportObservations observations)
        {
            if (observations == null) throw new ArgumentNullException(nameof(observations));
            if (!File.Exists(path)) return;
            string[] lines = File.ReadAllLines(path).Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
            if (lines.Length == 0) return;
            string[] headers = Split(lines[0], separator);
            for (int row = 1; row < lines.Length; row++)
            {
                string[] values = Split(lines[row], separator);
                for (int column = 1; column < headers.Length && column < values.Length; column++)
                {
                    if (patientTags) observations.AddPatientValue(headers[column], values[column]);
                    else observations.AddSiteValue(headers[column], values[column]);
                }
            }
        }

        private static void ScanIntranat(string path, TagImportObservations observations, CancellationToken token)
        {
            DirectoryInfo root = new(path);
            if (!root.Exists) return;
            foreach (DirectoryInfo patientDirectory in root.GetDirectories().Where(directory => Patient.IsPatientDirectory(directory.FullName)))
            {
                token.ThrowIfCancellationRequested();
                string[] nameParts = patientDirectory.Name.Split(new[] { '_' }, 3);
                if (nameParts.Length == 3 && int.TryParse(nameParts[1], out _))
                {
                    observations.AddPatientValue("Place", nameParts[0]);
                    observations.AddPatientValue("Date", nameParts[1]);
                }

                DirectoryInfo implantation = new(Path.Combine(patientDirectory.FullName, "implantation"));
                if (!implantation.Exists) continue;
                foreach (FileInfo file in implantation.GetFiles("*.csv", SearchOption.TopDirectoryOnly))
                {
                    token.ThrowIfCancellationRequested();
                    ScanIntranatSiteFile(file.FullName, observations);
                }
            }
        }

        private static void ScanBids(string path, TagImportObservations observations, CancellationToken token)
        {
            if (!Directory.Exists(path)) return;
            ScanDelimitedFile(Path.Combine(path, "participants.tsv"), '\t', true, observations);
            foreach (BIDSFile file in BIDSParser.FindFiles(path, new[] { "electrodes" }, new[] { ".tsv" }))
            {
                token.ThrowIfCancellationRequested();
                ScanBidsSiteFile(file.Path, observations);
            }
        }

        private static void ScanTagsDatabase(string path, TagImportObservations observations, CancellationToken token)
        {
            DirectoryInfo root = new(path);
            if (!root.Exists) return;

            ScanDelimitedFile(Path.Combine(path, "patients.csv"), ',', true, observations);
            ScanExcel(Path.Combine(path, "patients.xlsx"), true, observations);
            DirectoryInfo patientsDirectory = new(Path.Combine(path, "patients"));
            if (patientsDirectory.Exists)
            {
                foreach (FileInfo file in patientsDirectory.GetFiles("*.csv", SearchOption.AllDirectories))
                {
                    token.ThrowIfCancellationRequested();
                    ScanDelimitedFile(file.FullName, ',', true, observations);
                }

                foreach (FileInfo file in patientsDirectory.GetFiles("*.xlsx", SearchOption.AllDirectories))
                {
                    token.ThrowIfCancellationRequested();
                    ScanExcel(file.FullName, true, observations);
                }
            }

            foreach (FileInfo file in root.GetFiles("*.csv", SearchOption.TopDirectoryOnly).Where(file => !file.Name.Equals("patients.csv", StringComparison.OrdinalIgnoreCase)))
            {
                token.ThrowIfCancellationRequested();
                ScanTagsSiteFile(file.FullName, observations);
            }

            foreach (FileInfo file in root.GetFiles("*.xlsx", SearchOption.TopDirectoryOnly).Where(file => !file.Name.Equals("patients.xlsx", StringComparison.OrdinalIgnoreCase)))
            {
                token.ThrowIfCancellationRequested();
                ScanExcel(file.FullName, false, observations);
            }

            foreach (DirectoryInfo directory in root.GetDirectories().Where(directory => !directory.Name.Equals("patients", StringComparison.OrdinalIgnoreCase)))
            {
                foreach (FileInfo file in directory.GetFiles("*.csv", SearchOption.AllDirectories))
                {
                    token.ThrowIfCancellationRequested();
                    ScanTagsSiteFile(file.FullName, observations);
                }

                foreach (FileInfo file in directory.GetFiles("*.xlsx", SearchOption.AllDirectories))
                {
                    token.ThrowIfCancellationRequested();
                    ScanExcel(file.FullName, false, observations);
                }
            }
        }

        private static void ScanExcel(string path, bool patientTags, TagImportObservations observations)
        {
            if (!File.Exists(path)) return;
            List<ExcelRowData> rows = patientTags ? ExcelReader.ReadExcelFileForPatientTags(path) : ExcelReader.ReadExcelFileForSiteTags(path);
            foreach (ExcelRowData row in rows)
            {
                foreach (string header in row.GetHeaders())
                {
                    if (!row.TryGetValue(header, out string value)) continue;
                    if (patientTags) observations.AddPatientValue(header, value);
                    else observations.AddSiteValue(header, value);
                }
            }
        }

        private static void ScanBidsSiteFile(string path, TagImportObservations observations)
        {
            ScanSiteTable(path, '\t', new[] { "name", "x", "y", "z" }, observations);
            if (!Object3DManager.MarsAtlas.Loaded || !File.Exists(path)) return;
            string[] lines = File.ReadAllLines(path).Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
            if (lines.Length == 0) return;
            string[] headers = Split(lines[0], '\t');
            int marsAtlasIndex = Array.IndexOf(headers, "MarsAtlas");
            if (marsAtlasIndex < 0) return;
            for (int row = 1; row < lines.Length; row++)
            {
                string[] values = Split(lines[row], '\t');
                if (marsAtlasIndex >= values.Length) continue;
                AddMarsAtlasValues(observations, Object3DManager.MarsAtlas.Label(values[marsAtlasIndex]), false);
            }
        }

        private static void ScanIntranatSiteFile(string path, TagImportObservations observations)
        {
            if (!File.Exists(path)) return;
            string[] lines = File.ReadAllLines(path).Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
            int headerIndex = FindIntranatHeaderIndex(lines);
            if (headerIndex < 0) return;
            string[] tableLines = lines.Skip(headerIndex).ToArray();
            ScanSiteTable(tableLines, '\t', new[] { "contact", "mni", "t1pre scanner based" }, observations);
            if (!Object3DManager.MarsAtlas.Loaded || tableLines.Length == 0) return;
            string[] headers = Split(tableLines[0], '\t');
            int marsAtlasIndex = Array.IndexOf(headers, "MarsAtlas");
            int intranatMarsAtlasIndex = Array.IndexOf(headers, "IntrAnat-MarsAtlas");
            int mniMarsAtlasIndex = Array.IndexOf(headers, "MNI-MarsAtlas");
            if (marsAtlasIndex < 0 && intranatMarsAtlasIndex < 0 && mniMarsAtlasIndex < 0) return;
            for (int row = 1; row < tableLines.Length; row++)
            {
                string[] values = Split(tableLines[row], '\t');
                int label = TryGetMarsAtlasLabel(values, marsAtlasIndex, intranatMarsAtlasIndex, mniMarsAtlasIndex);
                AddMarsAtlasValues(observations, label, true);
            }
        }

        private static void ScanTagsSiteFile(string path, TagImportObservations observations)
        {
            string[] lines = File.Exists(path) ? File.ReadAllLines(path).Where(line => !string.IsNullOrWhiteSpace(line)).ToArray() : Array.Empty<string>();
            if (FindIntranatHeaderIndex(lines) >= 0)
            {
                ScanIntranatSiteFile(path, observations);
            }
            else
            {
                ScanDelimitedFile(path, ',', false, observations);
            }
        }

        private static int FindIntranatHeaderIndex(IReadOnlyList<string> lines)
        {
            for (int index = 0; index < lines.Count; index++)
            {
                string[] values = lines[index].Split('\t');
                if (values.Length > 1 && string.Equals(values[0].Trim(), "contact", StringComparison.OrdinalIgnoreCase)) return index;
            }

            return -1;
        }

        private static int TryGetMarsAtlasLabel(IReadOnlyList<string> values, params int[] indices)
        {
            foreach (int index in indices)
            {
                if (index >= 0 && index < values.Count && !values[index].Equals("n/a", StringComparison.OrdinalIgnoreCase))
                {
                    return Object3DManager.MarsAtlas.Label(values[index]);
                }
            }

            return -1;
        }

        private static void AddMarsAtlasValues(TagImportObservations observations, int label, bool intranat)
        {
            if (label < 0 && intranat)
            {
                observations.AddSiteValue("Hemisphere-MarsAtlas", "N/A");
                observations.AddSiteValue("Lobe-MarsAtlas", "N/A");
                observations.AddSiteValue("NameFS-MarsAtlas", "N/A");
                observations.AddSiteValue("Fullname-MarsAtlas", "N/A");
                observations.AddSiteValue("Brodmann-MarsAtlas", "N/A");
                return;
            }

            observations.AddSiteValue("Hemisphere-MarsAtlas", Object3DManager.MarsAtlas.Hemisphere(label));
            observations.AddSiteValue("Lobe-MarsAtlas", Object3DManager.MarsAtlas.Lobe(label));
            observations.AddSiteValue("NameFS-MarsAtlas", Object3DManager.MarsAtlas.NameFS(label));
            observations.AddSiteValue("Fullname-MarsAtlas", Object3DManager.MarsAtlas.FullName(label));
            observations.AddSiteValue(intranat ? "Brodmann-MarsAtlas" : "BrodmannArea-MarsAtlas", Object3DManager.MarsAtlas.BrodmannArea(label));
        }

        private static void ScanSiteTable(string path, char separator, IEnumerable<string> excludedHeaders, TagImportObservations observations)
        {
            if (!File.Exists(path)) return;
            ScanSiteTable(File.ReadAllLines(path).Where(line => !string.IsNullOrWhiteSpace(line)).ToArray(), separator, excludedHeaders, observations);
        }

        private static void ScanSiteTable(string[] lines, char separator, IEnumerable<string> excludedHeaders, TagImportObservations observations)
        {
            if (lines.Length == 0) return;
            HashSet<string> excluded = new(excludedHeaders, StringComparer.OrdinalIgnoreCase);
            string[] headers = Split(lines[0], separator).Select(header => header.Trim()).ToArray();
            for (int row = 1; row < lines.Length; row++)
            {
                string[] values = Split(lines[row], separator);
                for (int column = 0; column < headers.Length && column < values.Length; column++)
                {
                    if (!excluded.Contains(headers[column])) observations.AddSiteValue(headers[column], values[column]);
                }
            }
        }

        private static string[] Split(string line, char separator)
        {
            return separator == ',' ? s_CsvParser.Split(line) : line.Split(separator);
        }
    }
}
