using HBP.Core.Data;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace HBP.Data.BIDS
{
    public static class BIDSUtility
    {
        /// <summary>
        /// Converts a collection of BIDS patients to BIDS participants.tsv format.
        /// </summary>
        /// <param name="bidsPatients">Collection of BIDS patients to convert</param>
        /// <returns>TSV content as string</returns>
        public static string PatientsToParticipantsTSV(IEnumerable<BIDSPatient> bidsPatients)
        {
            if (bidsPatients == null || !bidsPatients.Any())
            {
                return "participant_id\n";
            }

            var bidsPatientsList = bidsPatients.ToList();

            // Step 1: Collect all distinct tags from all patients
            var allTags = new HashSet<BaseTag>();
            foreach (var bidsPatient in bidsPatientsList)
            {
                if (bidsPatient.OriginalPatient.Tags != null)
                {
                    foreach (var tagValue in bidsPatient.OriginalPatient.Tags)
                    {
                        if (tagValue.Tag != null)
                        {
                            allTags.Add(tagValue.Tag);
                        }
                    }
                }
            }

            // Step 2: Convert tag names to snake_case and create headers
            var headers = new List<string> { "participant_id" }; // Start with mandatory participant_id column
            var tagToHeaderMap = new Dictionary<BaseTag, string>();

            foreach (var tag in allTags.OrderBy(t => t.Name))
            {
                string headerName = ToSnakeCase(tag.Name);
                headers.Add(headerName);
                tagToHeaderMap[tag] = headerName;
            }

            // Step 3: Build TSV content
            var tsvBuilder = new StringBuilder();

            // Add header line
            tsvBuilder.AppendLine(string.Join("\t", headers));

            // Step 4: Add data lines for each BIDS patient
            foreach (var bidsPatient in bidsPatientsList)
            {
                var rowValues = new List<string>();

                // Add participant_id (using BIDS patient ID)
                rowValues.Add(bidsPatient.ParticipantId);

                // Add tag values or "n/a" for missing tags
                foreach (var tag in allTags.OrderBy(t => t.Name))
                {
                    string value = "n/a"; // Default value for missing tags

                    if (bidsPatient.OriginalPatient.Tags != null)
                    {
                        var tagValue = bidsPatient.OriginalPatient.Tags.FirstOrDefault(tv => tv.Tag == tag);
                        if (tagValue != null && tagValue.DisplayableValue != null)
                        {
                            value = EscapeTsvValue(tagValue.DisplayableValue);
                        }
                    }

                    rowValues.Add(value);
                }

                tsvBuilder.AppendLine(string.Join("\t", rowValues));
            }

            return tsvBuilder.ToString();
        }

        /// <summary>
        /// Converts a string to snake_case format.
        /// </summary>
        /// <param name="input">Input string to convert</param>
        /// <returns>String in snake_case format</returns>
        private static string ToSnakeCase(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            // Replace spaces, hyphens, and other separators with underscores
            string result = input.Trim();
            
            // Replace common separators with underscores
            result = Regex.Replace(result, @"[\s\-\.]+", "_");
            
            // Handle camelCase and PascalCase by adding underscores before uppercase letters
            // that are followed by lowercase letters or preceded by lowercase letters
            result = Regex.Replace(result, @"([a-z])([A-Z])", "$1_$2");
            result = Regex.Replace(result, @"([A-Z])([A-Z][a-z])", "$1_$2");
            
            // Convert to lowercase
            result = result.ToLowerInvariant();
            
            // Remove multiple consecutive underscores
            result = Regex.Replace(result, @"_{2,}", "_");
            
            // Remove leading and trailing underscores
            result = result.Trim('_');
            
            // Ensure the result is not empty and starts with a letter or underscore
            if (string.IsNullOrEmpty(result) || (!char.IsLetter(result[0]) && result[0] != '_'))
            {
                result = "tag_" + result;
            }

            return result;
        }

        /// <summary>
        /// Escapes TSV values to handle special characters.
        /// </summary>
        /// <param name="value">Value to escape</param>
        /// <returns>Escaped value suitable for TSV format</returns>
        private static string EscapeTsvValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "n/a";

            // Replace tabs, newlines, and carriage returns with spaces
            string escaped = value.Replace('\t', ' ')
                                  .Replace('\n', ' ')
                                  .Replace('\r', ' ');

            // Remove extra whitespace
            escaped = Regex.Replace(escaped, @"\s+", " ").Trim();

            // Return "n/a" if the value becomes empty after cleaning
            return string.IsNullOrEmpty(escaped) ? "n/a" : escaped;
        }
    }
}