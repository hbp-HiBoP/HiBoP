using Newtonsoft.Json;
using UnityEngine;

namespace HBP.Core.Data
{
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class TagFilterValue : BaseData
    {
        public abstract bool Compare(object value);
        public abstract string GetDescription(bool isNot);
    }

    [JsonObject(MemberSerialization.OptIn)]
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
            return new EmptyTagFilterValue();
        }
        public override void Copy(object copy)
        {
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
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
            return new BoolTagFilterValue { Value = Value };
        }
        public override void Copy(object copy)
        {
            if (copy is BoolTagFilterValue boolTagFilterValue)
            {
                Value = boolTagFilterValue.Value;
            }
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
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
                if (!CaseSensitive)
                {
                    valueString = valueString.ToLower();
                    Value = Value.ToLower();
                }
                if (ExactMatch)
                {
                    return valueString == Value;
                }
                else
                {
                    return valueString.Contains(Value);
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
            return new StringTagFilterValue { Value = Value, ExactMatch = ExactMatch, CaseSensitive = CaseSensitive };
        }
        public override void Copy(object copy)
        {
            if (copy is StringTagFilterValue stringTagFilterValue)
            {
                Value = stringTagFilterValue.Value;
                ExactMatch = stringTagFilterValue.ExactMatch;
                CaseSensitive = stringTagFilterValue.CaseSensitive;
            }
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class NumberTagFilterValue : TagFilterValue
    {
        public enum ComparisonType { Equal, Greater, GreaterOrEqual, Lower, LowerOrEqual, Range }
        [JsonProperty("Type")] public ComparisonType Type { get; set; }
        [JsonProperty("Value")] public float Value { get; set; }
        [JsonProperty("Min")] public float Min { get; set; }
        [JsonProperty("Max")] public float Max { get; set; }

        public override bool Compare(object value)
        {
            if (value is not null and float)
            {
                float floatValue = (float)value;
                switch (Type)
                {
                    case ComparisonType.Equal:
                        return floatValue == Value;
                    case ComparisonType.Greater:
                        return floatValue > Value;
                    case ComparisonType.GreaterOrEqual:
                        return floatValue >= Value;
                    case ComparisonType.Lower:
                        return floatValue < Value;
                    case ComparisonType.LowerOrEqual:
                        return floatValue <= Value;
                    case ComparisonType.Range:
                        return floatValue >= Min && floatValue <= Max;
                }
            }
            if (value is not null and double)
            {
                double doubleValue = (double)value;
                switch (Type)
                {
                    case ComparisonType.Equal:
                        return doubleValue == Value;
                    case ComparisonType.Greater:
                        return doubleValue > Value;
                    case ComparisonType.GreaterOrEqual:
                        return doubleValue >= Value;
                    case ComparisonType.Lower:
                        return doubleValue < Value;
                    case ComparisonType.LowerOrEqual:
                        return doubleValue <= Value;
                    case ComparisonType.Range:
                        return doubleValue >= Min && doubleValue <= Max;
                }
            }
            else if (value is not null and int)
            {
                int intValue = (int)value;
                switch (Type)
                {
                    case ComparisonType.Equal:
                        return intValue == Value;
                    case ComparisonType.Greater:
                        return intValue > Value;
                    case ComparisonType.GreaterOrEqual:
                        return intValue >= Value;
                    case ComparisonType.Lower:
                        return intValue < Value;
                    case ComparisonType.LowerOrEqual:
                        return intValue <= Value;
                    case ComparisonType.Range:
                        return intValue >= Min && intValue <= Max;
                }
            }
            else if (value is not null and long)
            {
                long longValue = (long)value;
                switch (Type)
                {
                    case ComparisonType.Equal:
                        return longValue == Value;
                    case ComparisonType.Greater:
                        return longValue > Value;
                    case ComparisonType.GreaterOrEqual:
                        return longValue >= Value;
                    case ComparisonType.Lower:
                        return longValue < Value;
                    case ComparisonType.LowerOrEqual:
                        return longValue <= Value;
                    case ComparisonType.Range:
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
                case ComparisonType.Equal:
                    description += $"equal to {Value}";
                    break;
                case ComparisonType.Greater:
                    description += $"greater than {Value}";
                    break;
                case ComparisonType.GreaterOrEqual:
                    description += $"greater or equal to {Value}";
                    break;
                case ComparisonType.Lower:
                    description += $"lower than {Value}";
                    break;
                case ComparisonType.LowerOrEqual:
                    description += $"lower or equal to {Value}";
                    break;
                case ComparisonType.Range:
                    description += $"between {Min} and {Max} (inclusive)";
                    break;
            }
            return description;
        }
        public override object Clone()
        {
            return new NumberTagFilterValue { Type = Type, Value = Value, Min = Min, Max = Max };
        }
        public override void Copy(object copy)
        {
            if (copy is NumberTagFilterValue numberTagFilterValue)
            {
                Type = numberTagFilterValue.Type;
                Value = numberTagFilterValue.Value;
                Min = numberTagFilterValue.Min;
                Max = numberTagFilterValue.Max;
            }
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class EnumTagFilterValue : TagFilterValue
    {
        [JsonProperty("Value")] public int Value { get; set; }

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
            return new EnumTagFilterValue { Value = Value };
        }
        public override void Copy(object copy)
        {
            if (copy is EnumTagFilterValue enumTagFilterValue)
            {
                Value = enumTagFilterValue.Value;
            }
        }
    }
}