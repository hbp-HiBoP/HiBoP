using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using ExcelDataReader;
using UnityEngine;
using UnityEngine.Scripting;

namespace HBP.Core.Tools
{
    /// <summary>
    /// Utility class for reading Excel files and converting them to formats 
    /// compatible with existing CSV tag generation logic
    /// </summary>
    [Preserve]
    public static class ExcelReader
    {
        /// <summary>
        /// Reads an Excel file and extracts data rows with reconstructed headers
        /// </summary>
        /// <param name="filePath">Path to the Excel file</param>
        /// <param name="sheetIndex">Sheet index to read (default: 0)</param>
        /// <returns>List of ExcelRowData objects representing the data rows</returns>
        public static List<ExcelRowData> ReadExcelFile(string filePath, int sheetIndex = 0)
        {
            return ReadExcelFileInternal(filePath, sheetIndex, TagType.None);
        }

        /// <summary>
        /// Reads an Excel file for patient tags with special naming rules
        /// </summary>
        /// <param name="filePath">Path to the Excel file</param>
        /// <param name="sheetIndex">Sheet index to read (default: 0)</param>
        /// <returns>List of ExcelRowData objects with merged patient tag data</returns>
        public static List<ExcelRowData> ReadExcelFileForPatientTags(string filePath, int sheetIndex = 0)
        {
            return ReadExcelFileInternal(filePath, sheetIndex, TagType.Patient);
        }

        /// <summary>
        /// Reads an Excel file for site tags with special naming rules
        /// </summary>
        /// <param name="filePath">Path to the Excel file</param>
        /// <param name="sheetIndex">Sheet index to read (default: 0)</param>
        /// <returns>List of ExcelRowData objects with processed site tag data</returns>
        public static List<ExcelRowData> ReadExcelFileForSiteTags(string filePath, int sheetIndex = 0)
        {
            return ReadExcelFileInternal(filePath, sheetIndex, TagType.Site);
        }

        /// <summary>
        /// Type of tags being processed
        /// </summary>
        public enum TagType
        {
            None,
            Patient,
            Site
        }

        /// <summary>
        /// Internal method that handles the actual Excel reading with tag-specific processing
        /// </summary>
        private static List<ExcelRowData> ReadExcelFileInternal(string filePath, int sheetIndex, TagType tagType)
        {
            List<ExcelRowData> result = new();
            
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"Excel file not found: {filePath}");
                return result;
            }

            try
            {
                using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
                {
                    using (var reader = ExcelReaderFactory.CreateReader(stream))
                    {
                        var dataSet = reader.AsDataSet();
                        
                        if (dataSet.Tables.Count <= sheetIndex)
                        {
                            Debug.LogWarning($"Sheet index {sheetIndex} not found in Excel file: {filePath}");
                            return result;
                        }

                        DataTable table = dataSet.Tables[sheetIndex];
                        if (table.Rows.Count < 3) // Need at least 2 header rows + 1 data row
                        {
                            Debug.LogWarning($"Excel file has insufficient rows: {filePath}");
                            return result;
                        }

                        // Reconstruct headers from first two rows
                        string[] reconstructedHeaders = ReconstructHeaders(table, tagType);
                        
                        if (reconstructedHeaders.Length == 0)
                        {
                            Debug.LogWarning($"No valid headers found in Excel file: {filePath}");
                            return result;
                        }

                        // Process data rows (starting from row 2, as rows 0 and 1 are headers)
                        for (int rowIndex = 2; rowIndex < table.Rows.Count; rowIndex++)
                        {
                            DataRow row = table.Rows[rowIndex];
                            
                            // Check if row is completely empty
                            if (IsEmptyRow(row))
                            {
                                continue;
                            }

                            // Extract name (first column)
                            string name = GetCellValue(row, 0);
                            
                            // Extract data for each header column
                            Dictionary<string, string> rowData = new();
                            for (int colIndex = 0; colIndex < reconstructedHeaders.Length; colIndex++)
                            {
                                string headerName = reconstructedHeaders[colIndex];
                                string cellValue = GetCellValue(row, colIndex + 1); // +1 because first column is name
                                rowData[headerName] = cellValue;
                            }

                            ExcelRowData excelRowData = new(name, reconstructedHeaders, rowData);
                            
                            // Only add rows that have meaningful data
                            if (excelRowData.HasData())
                            {
                                result.Add(excelRowData);
                            }
                        }

                        // Apply tag-specific post-processing
                        if (tagType == TagType.Patient)
                        {
                            result = ProcessPatientTagMerging(result);
                        }
                        else if (tagType == TagType.Site)
                        {
                            result = ProcessSiteTagFiltering(result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error reading Excel file {filePath}: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Reconstructs headers from the first two rows, handling merged cells
        /// </summary>
        private static string[] ReconstructHeaders(DataTable table, TagType tagType = TagType.None)
        {
            if (table.Rows.Count < 2)
            {
                return new string[0];
            }

            DataRow firstRow = table.Rows[0];
            DataRow secondRow = table.Rows[1];
            
            List<string> headers = new();
            string lastFirstRowValue = "";

            // Start from column 1 (skip name column at index 0)
            for (int colIndex = 1; colIndex < table.Columns.Count; colIndex++)
            {
                string firstRowValue = GetCellValue(firstRow, colIndex);
                string secondRowValue = GetCellValue(secondRow, colIndex);

                // Update last first row value if this cell has content
                if (!string.IsNullOrWhiteSpace(firstRowValue))
                {
                    lastFirstRowValue = firstRowValue.Trim();
                }

                // Build header name
                string headerName = "";
                string cleanSecondRow = "";
                
                if (!string.IsNullOrWhiteSpace(secondRowValue))
                {
                    cleanSecondRow = secondRowValue.Trim();
                    
                    // If we have both first and second row values, concatenate them
                    if (!string.IsNullOrWhiteSpace(lastFirstRowValue) && 
                        !string.Equals(lastFirstRowValue, cleanSecondRow, StringComparison.OrdinalIgnoreCase))
                    {
                        headerName = $"{lastFirstRowValue} - {cleanSecondRow}";
                    }
                    else
                    {
                        headerName = cleanSecondRow;
                    }
                }
                else if (!string.IsNullOrWhiteSpace(lastFirstRowValue))
                {
                    // Only first row has value, use it
                    headerName = lastFirstRowValue;
                }

                // Only add non-empty, valid header names
                if (!string.IsNullOrWhiteSpace(headerName) && 
                    !headerName.StartsWith("Unnamed", StringComparison.OrdinalIgnoreCase))
                {
                    // Clean up the header name
                    headerName = CleanHeaderName(headerName);
                    
                    // Apply tag-specific naming rules
                    headerName = ApplySpecialNamingRules(headerName, lastFirstRowValue, cleanSecondRow, tagType);
                    
                    // Skip headers that should be ignored
                    if (!ShouldIgnoreHeader(headerName, tagType))
                    {
                        headers.Add(headerName);
                    }
                }
            }

            return headers.ToArray();
        }

        /// <summary>
        /// Gets the string value of a cell, handling various data types
        /// </summary>
        private static string GetCellValue(DataRow row, int columnIndex)
        {
            if (columnIndex >= row.Table.Columns.Count)
            {
                return "";
            }

            object cellValue = row[columnIndex];
            
            if (cellValue == null || cellValue == DBNull.Value)
            {
                return "";
            }

            return cellValue.ToString().Trim();
        }

        /// <summary>
        /// Checks if a row is completely empty
        /// </summary>
        private static bool IsEmptyRow(DataRow row)
        {
            for (int i = 0; i < row.Table.Columns.Count; i++)
            {
                string cellValue = GetCellValue(row, i);
                if (!string.IsNullOrWhiteSpace(cellValue))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Cleans header names by removing invalid characters and normalizing
        /// </summary>
        private static string CleanHeaderName(string headerName)
        {
            if (string.IsNullOrWhiteSpace(headerName))
            {
                return "";
            }

            // Replace common problematic characters
            string cleaned = headerName
                .Replace("\n", " - ")
                .Replace("\r", " - ")
                .Replace("\t", " - ")
                .Replace("  ", " ")  // Replace double spaces with single
                .Trim();

            return cleaned;
        }

        /// <summary>
        /// Applies special naming rules based on tag type
        /// </summary>
        private static string ApplySpecialNamingRules(string headerName, string firstRowValue, string secondRowValue, TagType tagType)
        {
            switch (tagType)
            {
                case TagType.Patient:
                    return ApplyPatientTagNamingRules(headerName, firstRowValue, secondRowValue);
                case TagType.Site:
                    return ApplySiteTagNamingRules(headerName, firstRowValue, secondRowValue);
                default:
                    return headerName;
            }
        }

        /// <summary>
        /// Applies special naming rules for patient tags
        /// </summary>
        private static string ApplyPatientTagNamingRules(string headerName, string firstRowValue, string secondRowValue)
        {
            // For tags starting with "Neuropsychological exam", only consider the second row for the name
            if (!string.IsNullOrEmpty(firstRowValue) && firstRowValue.StartsWith("Neuropsychological exam", StringComparison.OrdinalIgnoreCase))
            {
                return !string.IsNullOrEmpty(secondRowValue) ? secondRowValue : headerName;
            }

            return headerName;
        }

        /// <summary>
        /// Applies special naming rules for site tags
        /// </summary>
        private static string ApplySiteTagNamingRules(string headerName, string firstRowValue, string secondRowValue)
        {
            // Handle "within the EZ" special cases
            if (!string.IsNullOrEmpty(firstRowValue) && firstRowValue.Equals("within the EZ", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(secondRowValue))
                {
                    if (secondRowValue.Equals("y/n/na", StringComparison.OrdinalIgnoreCase))
                    {
                        return "within the EZ";
                    }
                    else if (secondRowValue.Equals("EI value", StringComparison.OrdinalIgnoreCase))
                    {
                        return "EI value";
                    }
                }
            }

            // Handle spikes, ripples, and fast ripples patterns
            string[] wakenessSleepPatterns = { "spikes", "ripples", "fast ripples" };
            foreach (var pattern in wakenessSleepPatterns)
            {
                if (!string.IsNullOrEmpty(firstRowValue) && firstRowValue.StartsWith(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrEmpty(secondRowValue))
                    {
                        if (secondRowValue.Equals("y/n/nd", StringComparison.OrdinalIgnoreCase))
                        {
                            // Return the first row value as-is (e.g., "spikes (wakefulness)")
                            return firstRowValue;
                        }
                        else if (secondRowValue.Equals("rate", StringComparison.OrdinalIgnoreCase))
                        {
                            // Insert "rate" before the parentheses (e.g., "spikes rate (wakefulness)")
                            return InsertRateBeforeParentheses(firstRowValue);
                        }
                    }
                }
            }

            return headerName;
        }

        /// <summary>
        /// Inserts "rate" before the parentheses in a string
        /// </summary>
        private static string InsertRateBeforeParentheses(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            int parenIndex = input.IndexOf('(');
            if (parenIndex > 0)
            {
                string beforeParen = input.Substring(0, parenIndex).Trim();
                string afterParen = input.Substring(parenIndex).Trim();
                return $"{beforeParen} rate {afterParen}";
            }
            
            return $"{input} rate";
        }

        /// <summary>
        /// Determines if a header should be ignored based on tag type
        /// </summary>
        private static bool ShouldIgnoreHeader(string headerName, TagType tagType)
        {
            if (tagType == TagType.Site)
            {
                // For RFTC and Resected, ignore the "y/n/na" columns
                if (!string.IsNullOrEmpty(headerName))
                {
                    string lower = headerName.ToLower();
                    if ((lower.Contains("rftc") || lower.Contains("resected")) && lower.Contains("y/n/na"))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Processes patient tag merging for special cases
        /// </summary>
        private static List<ExcelRowData> ProcessPatientTagMerging(List<ExcelRowData> rows)
        {
            List<ExcelRowData> processedRows = new();

            foreach (var row in rows)
            {
                var newHeaders = new List<string>();
                var newData = new Dictionary<string, string>();

                // Handle Past-history merging (1, 2, 3 -> single tag)
                var pastHistoryValues = new List<string>();
                var pastHistoryHeaders = new List<string>();
                
                // Handle Comorbidity merging (1, 2, 3 -> single tag)
                var comorbidityValues = new List<string>();
                var comorbidityHeaders = new List<string>();

                foreach (var header in row.GetHeaders())
                {
                    if (IsPastHistoryTag(header))
                    {
                        pastHistoryHeaders.Add(header);
                        if (row.TryGetValue(header, out string value) && !string.IsNullOrWhiteSpace(value))
                        {
                            pastHistoryValues.Add(value);
                        }
                    }
                    else if (IsComorbidityTag(header))
                    {
                        comorbidityHeaders.Add(header);
                        if (row.TryGetValue(header, out string value) && !string.IsNullOrWhiteSpace(value))
                        {
                            comorbidityValues.Add(value);
                        }
                    }
                    else
                    {
                        // Keep other headers as-is
                        newHeaders.Add(header);
                        if (row.TryGetValue(header, out string value))
                        {
                            newData[header] = value;
                        }
                    }
                }

                // Add merged Past-history if any values were found
                if (pastHistoryValues.Count > 0)
                {
                    newHeaders.Add("Past-history");
                    newData["Past-history"] = string.Join(", ", pastHistoryValues);
                }

                // Add merged Comorbidity if any values were found
                if (comorbidityValues.Count > 0)
                {
                    newHeaders.Add("Comorbidity");
                    newData["Comorbidity"] = string.Join(", ", comorbidityValues);
                }

                var processedRow = new ExcelRowData(row.Name, newHeaders.ToArray(), newData);
                processedRows.Add(processedRow);
            }

            return processedRows;
        }

        /// <summary>
        /// Enhanced site tag filtering that considers relationships between columns
        /// </summary>
        private static List<ExcelRowData> ProcessSiteTagFiltering(List<ExcelRowData> rows)
        {
            List<ExcelRowData> processedRows = new();

            foreach (var row in rows)
            {
                var newHeaders = new List<string>();
                var newData = new Dictionary<string, string>();

                // First pass: identify which spikes/ripples patterns should be excluded
                var excludedPatterns = new HashSet<string>();
                
                foreach (var header in row.GetHeaders())
                {
                    if (row.TryGetValue(header, out string value))
                    {
                        // Check for "nd" values in y/n/nd columns
                        if (IsSpikesRipplesPattern(header) && IsYesNoNdColumn(header))
                        {
                            if (value.Equals("nd", StringComparison.OrdinalIgnoreCase))
                            {
                                // Extract the base pattern (e.g., "spikes (wakefulness)" from "spikes (wakefulness)")
                                string basePattern = GetBasePatternFromHeader(header);
                                excludedPatterns.Add(basePattern);
                            }
                            else
                            {
                                Debug.Log($"Including pattern '{header}' with value '{value}'");
                            }
                        }
                    }
                }

                // Second pass: include tags based on filtering rules
                foreach (var header in row.GetHeaders())
                {
                    if (row.TryGetValue(header, out string value))
                    {
                        bool shouldInclude = true;

                        // Rule: If spikes/ripples/fast ripples value is "nd", do not add this value nor the rate
                        if (IsSpikesRipplesPattern(header))
                        {
                            string basePattern = GetBasePatternFromHeader(header);
                            if (excludedPatterns.Contains(basePattern))
                            {
                                shouldInclude = false;
                            }
                        }

                        // Rule: If EI value is 0, do not add it
                        if (header.Equals("EI value", StringComparison.OrdinalIgnoreCase))
                        {
                            if (NumberExtension.TryParseFloat(value, out float eiValue) && eiValue == 0f)
                            {
                                shouldInclude = false;
                            }
                        }

                        // General empty value check
                        if (string.IsNullOrWhiteSpace(value))
                        {
                            shouldInclude = false;
                        }

                        if (shouldInclude)
                        {
                            newHeaders.Add(header);
                            newData[header] = value;
                        }
                    }
                }

                var processedRow = new ExcelRowData(row.Name, newHeaders.ToArray(), newData);
                processedRows.Add(processedRow);
            }

            return processedRows;
        }

        /// <summary>
        /// Checks if a header is related to spikes, ripples, or fast ripples patterns
        /// </summary>
        private static bool IsSpikesRipplesPattern(string headerName)
        {
            if (string.IsNullOrEmpty(headerName))
                return false;

            string lower = headerName.ToLower();
            return lower.Contains("spikes") || lower.Contains("ripples") || lower.Contains("fast ripples");
        }

        /// <summary>
        /// Checks if a header is a y/n/nd column (not a rate column)
        /// </summary>
        private static bool IsYesNoNdColumn(string headerName)
        {
            if (string.IsNullOrEmpty(headerName))
                return false;

            // If it contains "rate", it's a rate column, not a y/n/nd column
            if (headerName.ToLower().Contains("rate"))
                return false;

            // If it's a spikes/ripples pattern without "rate", it should be a y/n/nd column
            return IsSpikesRipplesPattern(headerName);
        }

        /// <summary>
        /// Extracts the base pattern from a header name for grouping related columns
        /// </summary>
        private static string GetBasePatternFromHeader(string headerName)
        {
            if (string.IsNullOrEmpty(headerName))
                return "";

            // Remove "rate" from the header to get the base pattern
            string basePattern = headerName.Replace(" rate ", " ").Trim();
            
            // For patterns like "spikes rate (wakefulness)", this becomes "spikes (wakefulness)"
            // For patterns like "spikes (wakefulness)", this stays the same
            return basePattern;
        }

        /// <summary>
        /// Checks if a header is a Past-history tag (1, 2, or 3)
        /// </summary>
        private static bool IsPastHistoryTag(string header)
        {
            if (string.IsNullOrEmpty(header))
                return false;

            return header.Equals("Past-history - 1", StringComparison.OrdinalIgnoreCase) ||
                   header.Equals("Past-history - 2", StringComparison.OrdinalIgnoreCase) ||
                   header.Equals("Past-history - 3", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Checks if a header is a Comorbidity tag (1, 2, or 3)
        /// </summary>
        private static bool IsComorbidityTag(string header)
        {
            if (string.IsNullOrEmpty(header))
                return false;

            return header.Equals("Comorbidity - 1", StringComparison.OrdinalIgnoreCase) ||
                   header.Equals("Comorbidity - 2", StringComparison.OrdinalIgnoreCase) ||
                   header.Equals("Comorbidity - 3", StringComparison.OrdinalIgnoreCase);
        }
    }
}