using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using HBP.Core.Tools;

namespace HBP.Core.Data
{
    public sealed class TagParsingPolicy
    {
        private readonly HashSet<string> m_TrueValues;
        private readonly HashSet<string> m_FalseValues;
        private readonly HashSet<string> m_IgnoredValues;

        public static TagParsingPolicy Default { get; } = new(new[] { "true", "yes" }, new[] { "false", "no" }, new[] { "n/a", "na", "nan", "null", "none", "-", "not found" });

        public ReadOnlyCollection<string> TrueValues { get; }
        public ReadOnlyCollection<string> FalseValues { get; }
        public ReadOnlyCollection<string> IgnoredValues { get; }

        public TagParsingPolicy(IEnumerable<string> trueValues, IEnumerable<string> falseValues, IEnumerable<string> ignoredValues)
        {
            m_TrueValues = Normalize(trueValues);
            m_FalseValues = Normalize(falseValues);
            m_IgnoredValues = Normalize(ignoredValues);

            EnsureDisjoint(m_TrueValues, m_FalseValues, "true and false");
            EnsureDisjoint(m_TrueValues, m_IgnoredValues, "true and ignored");
            EnsureDisjoint(m_FalseValues, m_IgnoredValues, "false and ignored");

            TrueValues = new ReadOnlyCollection<string>(m_TrueValues.OrderBy(value => value, StringComparer.Ordinal).ToList());
            FalseValues = new ReadOnlyCollection<string>(m_FalseValues.OrderBy(value => value, StringComparer.Ordinal).ToList());
            IgnoredValues = new ReadOnlyCollection<string>(m_IgnoredValues.OrderBy(value => value, StringComparer.Ordinal).ToList());
        }

        public bool IsIgnored(string value)
        {
            return value != null && m_IgnoredValues.Contains(value.Trim());
        }

        public bool TryParseBoolean(string value, out bool result)
        {
            if (value != null)
            {
                string normalized = value.Trim();
                if (m_TrueValues.Contains(normalized))
                {
                    result = true;
                    return true;
                }

                if (m_FalseValues.Contains(normalized))
                {
                    result = false;
                    return true;
                }
            }

            result = default;
            return false;
        }

        public bool TryParseInteger(string value, out int result)
        {
            if (value != null && !IsIgnored(value)) return int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
            result = default;
            return false;
        }

        public bool TryParseFloat(string value, out float result)
        {
            if (value != null && !IsIgnored(value)) return NumberExtension.TryParseFloat(value.Trim(), out result) && float.IsFinite(result);
            result = default;
            return false;
        }

        private static HashSet<string> Normalize(IEnumerable<string> values)
        {
            return new HashSet<string>((values ?? Enumerable.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()), StringComparer.OrdinalIgnoreCase);
        }

        private static void EnsureDisjoint(ISet<string> first, IEnumerable<string> second, string description)
        {
            string overlap = second.FirstOrDefault(first.Contains);
            if (overlap != null)
            {
                throw new ArgumentException($"Tag parsing values must not overlap between {description} tokens ('{overlap}').");
            }
        }
    }
}
