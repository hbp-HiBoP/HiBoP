using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HBP.Core.Tools
{
    /// <summary>
    /// Represents a parsed BIDS-formatted file.
    /// </summary>
    public class BIDSFile
    {
        /// <summary>Full path to the file.</summary>
        public string Path { get; }

        /// <summary>
        /// Suffix: the last underscore-delimited token of the filename stem
        /// (e.g. "white", "T1w", "ieeg", "electrodes").
        /// </summary>
        public string Suffix { get; }

        /// <summary>
        /// Full extension starting from the first dot that appears after the last
        /// underscore in the filename (e.g. ".surf.gii", ".nii.gz", ".vhdr").
        /// </summary>
        public string Extension { get; }

        /// <summary>
        /// All BIDS entities parsed from the filename (e.g. "sub" -> "P01", "hemi" -> "L").
        /// </summary>
        public IReadOnlyDictionary<string, string> Entities { get; }

        internal BIDSFile(string path, string suffix, string extension, Dictionary<string, string> entities)
        {
            Path = path;
            Suffix = suffix;
            Extension = extension;
            Entities = entities;
        }

        /// <summary>
        /// Returns true if this file and <paramref name="other"/> share the same values
        /// for every key in <paramref name="entityKeys"/>, and optionally the same suffix.
        /// </summary>
        public bool HasSameEntities(BIDSFile other, IEnumerable<string> entityKeys, bool includeSuffix = false)
        {
            if (other == null) return false;
            foreach (var key in entityKeys)
            {
                Entities.TryGetValue(key, out string a);
                other.Entities.TryGetValue(key, out string b);
                if (!string.Equals(a, b, StringComparison.Ordinal))
                    return false;
            }
            if (includeSuffix && !string.Equals(Suffix, other.Suffix, StringComparison.Ordinal))
                return false;
            return true;
        }
    }

    /// <summary>
    /// Generic, regex-free parser for BIDS-formatted filenames.
    /// </summary>
    public static class BIDSParser
    {
        /// <summary>
        /// Attempts to parse a BIDS filename.
        /// </summary>
        /// <param name="filePath">Full path to the file.</param>
        /// <param name="allowedSuffixes">
        /// Accepted suffix values. Pass null or empty to accept any suffix.
        /// </param>
        /// <param name="allowedExtensions">
        /// Accepted extensions using ends-with matching (e.g. ".gii" matches ".surf.gii").
        /// Pass null or empty to accept any extension.
        /// </param>
        /// <param name="result">The parsed <see cref="BIDSFile"/> on success.</param>
        /// <returns>True if parsing succeeded and all filters matched.</returns>
        public static bool TryParse(string filePath, IEnumerable<string> allowedSuffixes, IEnumerable<string> allowedExtensions, out BIDSFile result)
        {
            result = null;
            if (string.IsNullOrEmpty(filePath)) return false;

            string fileName = System.IO.Path.GetFileName(filePath);

            // Split stem from extension at the first '.' that comes after the last '_'.
            int lastUnderscore = fileName.LastIndexOf('_');
            if (lastUnderscore < 0) return false;
            int firstDotAfterSuffix = fileName.IndexOf('.', lastUnderscore);
            if (firstDotAfterSuffix < 0) return false;

            string stem = fileName[..firstDotAfterSuffix];
            string extension = fileName[firstDotAfterSuffix..];

            // Extension filter (ends-with).
            if (allowedExtensions != null)
            {
                var extList = allowedExtensions as IList<string> ?? allowedExtensions.ToList();
                if (extList.Count > 0 && !extList.Any(e => extension.EndsWith(e, StringComparison.OrdinalIgnoreCase)))
                    return false;
            }

            // Split stem into tokens on '_'.
            string[] parts = stem.Split('_');
            if (parts.Length < 2) return false;

            string suffix = parts[^1];

            // Suffix filter.
            if (allowedSuffixes != null)
            {
                var suffixList = allowedSuffixes as IList<string> ?? allowedSuffixes.ToList();
                if (suffixList.Count > 0 && !suffixList.Any(s => string.Equals(s, suffix, StringComparison.Ordinal)))
                    return false;
            }

            // Parse all entity key-value pairs from all tokens except the last (suffix).
            var entities = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int i = 0; i < parts.Length - 1; i++)
            {
                int dash = parts[i].IndexOf('-');
                if (dash < 1) continue;
                string key = parts[i][..dash];
                string value = parts[i][(dash + 1)..];
                entities[key] = value;
            }

            // A valid BIDS file must have a "sub" entity.
            if (!entities.ContainsKey("sub")) return false;

            result = new BIDSFile(filePath, suffix, extension, entities);
            return true;
        }

        /// <summary>
        /// Finds BIDS files under <paramref name="directoryPath"/> that match the given
        /// suffix and extension filters. The filesystem glob is derived automatically
        /// from the filters.
        /// </summary>
        public static IEnumerable<BIDSFile> FindFiles(string directoryPath, IEnumerable<string> allowedSuffixes, IEnumerable<string> allowedExtensions, SearchOption searchOption = SearchOption.AllDirectories)
        {
            if (string.IsNullOrEmpty(directoryPath)) return Enumerable.Empty<BIDSFile>();
            if (!Directory.Exists(directoryPath)) return Enumerable.Empty<BIDSFile>();

            var suffixList = allowedSuffixes?.ToList() ?? new List<string>();
            var extList = allowedExtensions?.ToList() ?? new List<string>();

            // Derive the tightest glob we can.
            string glob;
            if (suffixList.Count == 1 && extList.Count == 1)
                glob = "*_" + suffixList[0] + extList[0];
            else if (extList.Count == 1)
                glob = "*" + extList[0];
            else
                glob = "*";

            var results = new List<BIDSFile>();
            foreach (var file in Directory.GetFiles(directoryPath, glob, searchOption))
            {
                if (TryParse(file, suffixList.Count > 0 ? suffixList : null, extList.Count > 0 ? extList : null, out BIDSFile bidsFile))
                    results.Add(bidsFile);
            }
            return results;
        }
    }
}
