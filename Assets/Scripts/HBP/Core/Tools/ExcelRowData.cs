using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Scripting;

namespace HBP.Core.Tools
{
    /// <summary>
    /// Wraps Excel row data to provide CSV-like string array access
    /// while maintaining the exact API needed for existing CSV tag generation logic
    /// </summary>
    [Preserve]
    public class ExcelRowData
    {
        private Dictionary<string, string> m_Data;
        private string[] m_Headers;
        private string m_Name;

        public ExcelRowData(string name, string[] headers, Dictionary<string, string> data)
        {
            m_Name = name ?? "";
            m_Headers = headers ?? new string[0];
            m_Data = data ?? new Dictionary<string, string>();
        }

        /// <summary>
        /// Gets the name/identifier for this row (usually first column)
        /// </summary>
        public string Name => m_Name;

        /// <summary>
        /// Gets the number of columns including the name column
        /// </summary>
        public int Length => m_Headers.Length + 1; // +1 for name column

        /// <summary>
        /// Gets a value by column index (0 = name, 1+ = header columns)
        /// This mimics the string[] access pattern used by CSV parsing
        /// </summary>
        public string this[int index]
        {
            get
            {
                if (index == 0)
                {
                    return m_Name;
                }

                int headerIndex = index - 1;
                if (headerIndex >= 0 && headerIndex < m_Headers.Length)
                {
                    string headerName = m_Headers[headerIndex];
                    return m_Data.TryGetValue(headerName, out string value) ? value : "";
                }

                return "";
            }
        }

        /// <summary>
        /// Tries to get a value by header name
        /// </summary>
        public bool TryGetValue(string headerName, out string value)
        {
            return m_Data.TryGetValue(headerName, out value);
        }

        /// <summary>
        /// Converts this row to a string array format compatible with CSV parsing
        /// </summary>
        public string[] ToStringArray()
        {
            string[] result = new string[Length];
            result[0] = m_Name;

            for (int i = 0; i < m_Headers.Length; i++)
            {
                string headerName = m_Headers[i];
                result[i + 1] = m_Data.TryGetValue(headerName, out string value) ? value : "";
            }

            return result;
        }

        /// <summary>
        /// Gets all available headers (excluding name column)
        /// </summary>
        public string[] GetHeaders()
        {
            return m_Headers.ToArray();
        }

        /// <summary>
        /// Checks if the row has any meaningful data (not just empty values)
        /// </summary>
        public bool HasData()
        {
            if (!string.IsNullOrWhiteSpace(m_Name))
            {
                return true;
            }

            return m_Data.Values.Any(v => !string.IsNullOrWhiteSpace(v));
        }
    }
}
