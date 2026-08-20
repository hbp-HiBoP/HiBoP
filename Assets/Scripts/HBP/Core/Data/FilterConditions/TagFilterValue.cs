using System;
using HBP.Core.Enums;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Scripting;

namespace HBP.Core.Data
{
    [JsonObject(MemberSerialization.OptIn), Preserve]
    public abstract class TagFilterValue : BaseData
    {
        public abstract bool Compare(object value);
        public abstract string GetDescription(bool isNot);
    }

    [JsonObject(MemberSerialization.OptIn), Preserve]
    public class EmptyTagFilterValue : TagFilterValue
    {
        public override bool Compare(object value)
        {
            return true;
        }

        public override string GetDescription(bool isNot)
        {
            return "";
        }

        public override object Clone()
        {
            return new EmptyTagFilterValue { ID = ID };
        }

        public override void Copy(object copy)
        {
            base.Copy(copy);
        }
    }

    [JsonObject(MemberSerialization.OptIn), Preserve]
    public class BoolTagFilterValue : TagFilterValue
    {
        [JsonProperty("Value")] public bool Value { get; set; }

        public override bool Compare(object value)
        {
            if (value is not null and bool)
                return (bool)value == Value;
            return false;
        }

        public override string GetDescription(bool isNot)
        {
            return $" with value {(isNot ? !Value : Value)}";
        }

        public override object Clone()
        {
            return new BoolTagFilterValue { Value = Value, ID = ID };
        }

        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is BoolTagFilterValue boolTagFilterValue)
            {
                Value = boolTagFilterValue.Value;
            }
        }
    }

    [JsonObject(MemberSerialization.OptIn), Preserve]
    public class StringTagFilterValue : TagFilterValue
    {
        [JsonProperty("Value")] public string Value { get; set; } = "";
        [JsonProperty("ExactMatch")] public bool ExactMatch { get; set; }
        [JsonProperty("CaseSensitive")] public bool CaseSensitive { get; set; }

        public override bool Compare(object value)
        {
            if (value is not null and string)
            {
                string valueString = (string)value;
                StringComparison comparison = CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                if (ExactMatch)
                {
                    return string.Equals(valueString, Value, comparison);
                }
                else
                {
                    return valueString.IndexOf(Value, comparison) >= 0;
                }
            }

            return false;
        }

        public override string GetDescription(bool isNot)
        {
            return $" with value{(isNot ? " not" : "")} {(ExactMatch ? "equal to" : "containing")} \"{Value}\" (case {(CaseSensitive ? "sensitive" : "insensitive")})";
        }

        public override object Clone()
        {
            return new StringTagFilterValue { Value = Value, ExactMatch = ExactMatch, CaseSensitive = CaseSensitive, ID = ID };
        }

        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is StringTagFilterValue stringTagFilterValue)
            {
                Value = stringTagFilterValue.Value;
                ExactMatch = stringTagFilterValue.ExactMatch;
                CaseSensitive = stringTagFilterValue.CaseSensitive;
            }
        }
    }

    [JsonObject(MemberSerialization.OptIn), Preserve]
    public class NumberTagFilterValue : TagFilterValue
    {
        private const float Epsilon = 1e-6f;

        [JsonProperty("Type")] public NumberComparisonType Type { get; set; }
        [JsonProperty("Value")] public float Value { get; set; }
        [JsonProperty("Min")] public float Min { get; set; }
        [JsonProperty("Max")] public float Max { get; set; }

        private static bool ApproximatelyEqual(double a, double b, double epsilon = Epsilon)
        {
            return Math.Abs(a - b) < epsilon;
        }

        public override bool Compare(object value)
        {
            if (value is not null and float)
            {
                float floatValue = (float)value;
                switch (Type)
                {
                    case NumberComparisonType.Equal:
                        return ApproximatelyEqual(floatValue, Value);
                    case NumberComparisonType.Greater:
                        return floatValue > Value;
                    case NumberComparisonType.GreaterOrEqual:
                        return floatValue >= Value || ApproximatelyEqual(floatValue, Value);
                    case NumberComparisonType.Lower:
                        return floatValue < Value;
                    case NumberComparisonType.LowerOrEqual:
                        return floatValue <= Value || ApproximatelyEqual(floatValue, Value);
                    case NumberComparisonType.Range:
                        return (floatValue >= Min || ApproximatelyEqual(floatValue, Min)) && (floatValue <= Max || ApproximatelyEqual(floatValue, Max));
                }
            }

            if (value is not null and double)
            {
                double doubleValue = (double)value;
                switch (Type)
                {
                    case NumberComparisonType.Equal:
                        return ApproximatelyEqual(doubleValue, Value);
                    case NumberComparisonType.Greater:
                        return doubleValue > Value;
                    case NumberComparisonType.GreaterOrEqual:
                        return doubleValue >= Value || ApproximatelyEqual(doubleValue, Value);
                    case NumberComparisonType.Lower:
                        return doubleValue < Value;
                    case NumberComparisonType.LowerOrEqual:
                        return doubleValue <= Value || ApproximatelyEqual(doubleValue, Value);
                    case NumberComparisonType.Range:
                        return (doubleValue >= Min || ApproximatelyEqual(doubleValue, Min)) && (doubleValue <= Max || ApproximatelyEqual(doubleValue, Max));
                }
            }
            else if (value is not null and int)
            {
                int intValue = (int)value;
                switch (Type)
                {
                    case NumberComparisonType.Equal:
                        return ApproximatelyEqual(intValue, Value);
                    case NumberComparisonType.Greater:
                        return intValue > Value;
                    case NumberComparisonType.GreaterOrEqual:
                        return intValue >= Value;
                    case NumberComparisonType.Lower:
                        return intValue < Value;
                    case NumberComparisonType.LowerOrEqual:
                        return intValue <= Value;
                    case NumberComparisonType.Range:
                        return intValue >= Min && intValue <= Max;
                }
            }
            else if (value is not null and long)
            {
                long longValue = (long)value;
                switch (Type)
                {
                    case NumberComparisonType.Equal:
                        return ApproximatelyEqual(longValue, Value);
                    case NumberComparisonType.Greater:
                        return longValue > Value;
                    case NumberComparisonType.GreaterOrEqual:
                        return longValue >= Value;
                    case NumberComparisonType.Lower:
                        return longValue < Value;
                    case NumberComparisonType.LowerOrEqual:
                        return longValue <= Value;
                    case NumberComparisonType.Range:
                        return longValue >= Min && longValue <= Max;
                }
            }

            return false;
        }

        public override string GetDescription(bool isNot)
        {
            string description = $" with value{(isNot ? " not" : "")} ";
            switch (Type)
            {
                case NumberComparisonType.Equal:
                    description += $"equal to {Value}";
                    break;
                case NumberComparisonType.Greater:
                    description += $"greater than {Value}";
                    break;
                case NumberComparisonType.GreaterOrEqual:
                    description += $"greater or equal to {Value}";
                    break;
                case NumberComparisonType.Lower:
                    description += $"lower than {Value}";
                    break;
                case NumberComparisonType.LowerOrEqual:
                    description += $"lower or equal to {Value}";
                    break;
                case NumberComparisonType.Range:
                    description += $"between {Min} and {Max} (inclusive)";
                    break;
            }

            return description;
        }

        public override object Clone()
        {
            return new NumberTagFilterValue { Type = Type, Value = Value, Min = Min, Max = Max, ID = ID };
        }

        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is NumberTagFilterValue numberTagFilterValue)
            {
                Type = numberTagFilterValue.Type;
                Value = numberTagFilterValue.Value;
                Min = numberTagFilterValue.Min;
                Max = numberTagFilterValue.Max;
            }
        }
    }

    [JsonObject(MemberSerialization.OptIn), Preserve]
    public class EnumTagFilterValue : TagFilterValue
    {
        private int m_Value;

        [JsonProperty("Value")] public int Value
        {
            get => m_Value;
            set
            {
                m_Value = value;
                StringValue = null;
            }
        }

        [JsonProperty("StringValue")] public string StringValue { get; private set; }

        public EnumValueReference Reference => new(Value, StringValue);

        public override bool Compare(object value)
        {
            if (value is not null and int)
            {
                int intValue = (int)value;
                return intValue == Value;
            }

            return false;
        }

        public override string GetDescription(bool isNot)
        {
            return $" with value{(isNot ? " not" : "")} equal to ";
        }

        public override object Clone()
        {
            return new EnumTagFilterValue { Value = Value, StringValue = StringValue, ID = ID };
        }

        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is EnumTagFilterValue enumTagFilterValue)
            {
                Value = enumTagFilterValue.Value;
                StringValue = enumTagFilterValue.StringValue;
            }
        }

        public void SetValue(EnumTag tag, int value)
        {
            if (tag == null)
            {
                throw new ArgumentNullException(nameof(tag));
            }

            Value = tag.Clamp(value);
            StringValue = tag.Values[Value];
        }

        public string GetDisplayValue(EnumTag tag)
        {
            Resolve(tag, null, false);
            return StringValue;
        }

        internal void Resolve(EnumTag tag, LoadingContext context, bool reportLegacy)
        {
            if (tag == null)
            {
                throw new ArgumentNullException(nameof(tag));
            }

            if (StringValue != null)
            {
                if (!tag.TryGetValueIndex(StringValue, out int repairedIndex))
                {
                    throw new InvalidOperationException($"Enum filter value '{StringValue}' does not exist in enum tag '{tag.Name}' ({tag.ID}).");
                }

                m_Value = repairedIndex;
                return;
            }

            SetValue(tag, Value);
            if (reportLegacy)
            {
                context?.ReportLegacyEnumReference(tag, true);
            }
        }
    }
}
