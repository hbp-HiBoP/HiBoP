using System;
using System.Globalization;

namespace HBP.Core.Data
{
    public enum TagConversionImpact
    {
        Exact,
        Lossy,
        Destructive
    }

    public sealed class TagValueConversionResult
    {
        public bool Success => Value != null;
        public BaseTagValue Value { get; }
        public TagConversionImpact Impact { get; }
        public string Warning { get; }
        public string Error { get; }

        private TagValueConversionResult(BaseTagValue value, TagConversionImpact impact, string warning, string error)
        {
            Value = value;
            Impact = impact;
            Warning = warning;
            Error = error;
        }

        internal static TagValueConversionResult Succeeded(BaseTagValue value, TagConversionImpact impact = TagConversionImpact.Exact, string warning = null)
        {
            return new TagValueConversionResult(value, impact, warning, null);
        }

        internal static TagValueConversionResult Failed(string error)
        {
            return new TagValueConversionResult(null, TagConversionImpact.Exact, null, error);
        }
    }

    public sealed class TagValueConversionService
    {
        public TagValueConversionResult TryConvert(BaseTagValue source, BaseTag target, TagParsingPolicy policy)
        {
            return TryConvert(source, target, policy, null);
        }

        internal TagValueConversionResult TryConvert(BaseTagValue source, BaseTag target, TagParsingPolicy policy, BaseTag sourceDefinition)
        {
            if (source == null) return TagValueConversionResult.Failed("The source tag value is null.");
            if (target == null) return TagValueConversionResult.Failed("The target tag definition is null.");
            policy ??= TagParsingPolicy.Default;

            if (source is EmptyTagValue && target is not EmptyTag)
            {
                return TagValueConversionResult.Failed($"Empty tag value '{source.ID}' cannot be converted to '{target.GetType().Name}'.");
            }

            if (target is EmptyTag emptyTarget)
            {
                return TagValueConversionResult.Succeeded(new EmptyTagValue(emptyTarget, source.ID), source is EmptyTagValue ? TagConversionImpact.Exact : TagConversionImpact.Destructive);
            }

            if (!TryGetSemanticValue(source, target, sourceDefinition, out object semanticValue, out string warning, out string error))
            {
                return TagValueConversionResult.Failed(error);
            }

            if (semanticValue is string text && target is not StringTag && policy.IsIgnored(text))
            {
                return TagValueConversionResult.Failed($"Tag value '{source.ID}' contains ignored token '{text}' and cannot be migrated automatically.");
            }

            try
            {
                BaseTagValue converted;
                TagConversionImpact impact = TagConversionImpact.Exact;
                switch (target)
                {
                    case StringTag stringTag:
                        converted = new StringTagValue(stringTag, ToCanonicalString(semanticValue), source.ID);
                        break;
                    case BoolTag boolTag when TryConvertToBool(semanticValue, policy, out bool boolValue):
                        converted = new BoolTagValue(boolTag, boolValue, source.ID);
                        break;
                    case IntTag intTag when TryConvertToInt(semanticValue, policy, out int intValue):
                        int clampedInt = intTag.Clamp(intValue);
                        converted = new IntTagValue(intTag, clampedInt, source.ID);
                        if (clampedInt != intValue) impact = TagConversionImpact.Lossy;
                        break;
                    case FloatTag floatTag when TryConvertToFloat(semanticValue, policy, out float floatValue):
                        float clampedFloat = floatTag.Clamp(floatValue);
                        converted = new FloatTagValue(floatTag, clampedFloat, source.ID);
                        if (clampedFloat != floatValue) impact = TagConversionImpact.Lossy;
                        break;
                    case EnumTag enumTag:
                        string enumLabel = ToCanonicalString(semanticValue);
                        int enumIndex = enumTag.GetOrAddValue(enumLabel);
                        converted = new EnumTagValue(enumTag, enumIndex, source.ID);
                        break;
                    default:
                        return TagValueConversionResult.Failed($"Tag value '{source.ID}' cannot be converted from '{source.GetType().Name}' to '{target.GetType().Name}'.");
                }

                return TagValueConversionResult.Succeeded(converted, impact, warning);
            }
            catch (Exception exception)
            {
                return TagValueConversionResult.Failed($"Tag value '{source.ID}' cannot be converted from '{source.GetType().Name}' to '{target.GetType().Name}': {exception.Message}");
            }
        }

        internal static bool TryGetSemanticValue(BaseTagValue source, BaseTag target, out object value, out string warning, out string error)
        {
            return TryGetSemanticValue(source, target, null, out value, out warning, out error);
        }

        private static bool TryGetSemanticValue(BaseTagValue source, BaseTag target, BaseTag sourceDefinition, out object value, out string warning, out string error)
        {
            warning = null;
            error = null;
            if (source is not EnumTagValue enumValue)
            {
                value = source.Value;
                if (value == null)
                {
                    error = $"Tag value '{source.ID}' contains null and cannot be migrated automatically.";
                    return false;
                }

                return true;
            }

            if (enumValue.StringValue != null)
            {
                value = enumValue.StringValue;
                return true;
            }

            if (target is not EnumTag)
            {
                value = null;
                error = $"Legacy enum tag value '{source.ID}' cannot be converted to a non-enum type without its historical label.";
                return false;
            }

            EnumTag enumSourceDefinition = enumValue.Tag ?? sourceDefinition as EnumTag;
            int index = enumValue.Value;
            if (enumSourceDefinition == null || index < 0 || index >= enumSourceDefinition.Values.Length)
            {
                value = null;
                error = $"Legacy enum tag value '{source.ID}' with index {index} cannot be resolved from its current definition.";
                return false;
            }

            value = enumSourceDefinition.Values[index];
            warning = $"Legacy enum tag value '{source.ID}' was resolved from its current index and may be incorrect.";
            return true;
        }

        internal static string ToCanonicalString(object value)
        {
            return value switch
            {
                null => string.Empty,
                bool boolValue => boolValue ? "true" : "false",
                float floatValue => floatValue.ToString("R", CultureInfo.InvariantCulture),
                double doubleValue => doubleValue.ToString("R", CultureInfo.InvariantCulture),
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString()
            };
        }

        internal static bool TryConvertToInt(object value, out int result)
        {
            return TryConvertToInt(value, TagParsingPolicy.Default, out result);
        }

        internal static bool TryConvertToInt(object value, TagParsingPolicy policy, out int result)
        {
            switch (value)
            {
                case int intValue:
                    result = intValue;
                    return true;
                case bool boolValue:
                    result = boolValue ? 1 : 0;
                    return true;
                case float floatValue when float.IsFinite(floatValue) && floatValue == MathF.Truncate(floatValue) && floatValue >= int.MinValue && floatValue <= int.MaxValue:
                    result = (int)floatValue;
                    return true;
                case double doubleValue when double.IsFinite(doubleValue) && doubleValue == Math.Truncate(doubleValue) && doubleValue >= int.MinValue && doubleValue <= int.MaxValue:
                    result = (int)doubleValue;
                    return true;
                case long longValue when longValue >= int.MinValue && longValue <= int.MaxValue:
                    result = (int)longValue;
                    return true;
                case string stringValue:
                    return policy.TryParseInteger(stringValue, out result);
                default:
                    result = default;
                    return false;
            }
        }

        internal static bool TryConvertToFloat(object value, out float result)
        {
            return TryConvertToFloat(value, TagParsingPolicy.Default, out result);
        }

        internal static bool TryConvertToFloat(object value, TagParsingPolicy policy, out float result)
        {
            switch (value)
            {
                case bool boolValue:
                    result = boolValue ? 1 : 0;
                    return true;
                case int intValue:
                    result = intValue;
                    return (int)result == intValue;
                case float floatValue when float.IsFinite(floatValue):
                    result = floatValue;
                    return true;
                case double doubleValue when double.IsFinite(doubleValue) && doubleValue >= -float.MaxValue && doubleValue <= float.MaxValue:
                    result = (float)doubleValue;
                    return result == doubleValue;
                case string stringValue:
                    return policy.TryParseFloat(stringValue, out result);
                default:
                    result = default;
                    return false;
            }
        }

        internal static bool TryConvertToBool(object value, TagParsingPolicy policy, out bool result)
        {
            switch (value)
            {
                case bool boolValue:
                    result = boolValue;
                    return true;
                case int intValue when intValue is 0 or 1:
                    result = intValue == 1;
                    return true;
                case float floatValue when floatValue is 0 or 1:
                    result = floatValue == 1;
                    return true;
                case string stringValue:
                    return policy.TryParseBoolean(stringValue, out result);
                default:
                    result = default;
                    return false;
            }
        }
    }
}
