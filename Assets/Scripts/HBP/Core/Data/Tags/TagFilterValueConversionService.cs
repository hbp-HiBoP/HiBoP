using HBP.Core.Enums;
using System;

namespace HBP.Core.Data
{
    public sealed class TagFilterValueConversionResult
    {
        public bool Success => Value != null;
        public TagFilterValue Value { get; }
        public TagConversionImpact Impact { get; }
        public string Warning { get; }
        public string Error { get; }

        private TagFilterValueConversionResult(TagFilterValue value, TagConversionImpact impact, string warning, string error)
        {
            Value = value;
            Impact = impact;
            Warning = warning;
            Error = error;
        }

        internal static TagFilterValueConversionResult Succeeded(TagFilterValue value, TagConversionImpact impact = TagConversionImpact.Exact, string warning = null)
        {
            return new TagFilterValueConversionResult(value, impact, warning, null);
        }

        internal static TagFilterValueConversionResult Failed(string error)
        {
            return new TagFilterValueConversionResult(null, TagConversionImpact.Exact, null, error);
        }
    }

    public sealed class TagFilterValueConversionService
    {
        public TagFilterValueConversionResult TryConvert(TagFilterValue source, BaseTag sourceTag, BaseTag targetTag, TagParsingPolicy policy)
        {
            if (source == null) return TagFilterValueConversionResult.Failed("The source tag filter value is null.");
            if (targetTag == null) return TagFilterValueConversionResult.Failed("The target tag definition is null.");
            policy ??= TagParsingPolicy.Default;

            if (source is EnumTagFilterValue { StringValue: null } && targetTag is not EnumTag)
            {
                return TagFilterValueConversionResult.Failed($"Legacy enum filter '{source.ID}' cannot be converted to a non-enum type without its historical label.");
            }

            try
            {
                TagFilterValue converted;
                TagConversionImpact impact = TagConversionImpact.Exact;
                string warning = null;

                if (targetTag is EmptyTag)
                {
                    converted = new EmptyTagFilterValue();
                    impact = source is EmptyTagFilterValue ? TagConversionImpact.Exact : TagConversionImpact.Destructive;
                }
                else if (source is EmptyTagFilterValue)
                {
                    return TagFilterValueConversionResult.Failed($"Empty filter '{source.ID}' cannot be converted to '{targetTag.GetType().Name}'.");
                }
                else if (targetTag is StringTag)
                {
                    if (!TryConvertToStringFilter(source, sourceTag, out StringTagFilterValue stringFilter, out warning, out string error))
                    {
                        return TagFilterValueConversionResult.Failed(error);
                    }

                    converted = stringFilter;
                    if (source is NumberTagFilterValue) impact = TagConversionImpact.Lossy;
                }
                else if (targetTag is BoolTag)
                {
                    if (!TryGetSingleSemanticValue(source, sourceTag, out object semanticValue, out warning, out string error) || !TagValueConversionService.TryConvertToBool(semanticValue, policy, out bool boolValue))
                    {
                        return TagFilterValueConversionResult.Failed(error ?? $"Filter '{source.ID}' cannot be converted to a boolean filter.");
                    }

                    converted = new BoolTagFilterValue { Value = boolValue };
                    if (source is StringTagFilterValue) impact = TagConversionImpact.Lossy;
                }
                else if (targetTag is IntTag intTag)
                {
                    if (!TryConvertToNumberFilter(source, sourceTag, value => TryConvertAndClampInt(value, intTag, policy), out NumberTagFilterValue numberFilter, out impact, out warning, out string error))
                    {
                        return TagFilterValueConversionResult.Failed(error);
                    }

                    converted = numberFilter;
                    if (source is StringTagFilterValue) impact = TagConversionImpact.Lossy;
                }
                else if (targetTag is FloatTag floatTag)
                {
                    if (!TryConvertToNumberFilter(source, sourceTag, value => TryConvertAndClampFloat(value, floatTag, policy), out NumberTagFilterValue numberFilter, out impact, out warning, out string error))
                    {
                        return TagFilterValueConversionResult.Failed(error);
                    }

                    converted = numberFilter;
                    if (source is StringTagFilterValue) impact = TagConversionImpact.Lossy;
                }
                else if (targetTag is EnumTag enumTag)
                {
                    if (!TryGetSingleSemanticValue(source, sourceTag, out object semanticValue, out warning, out string error))
                    {
                        return TagFilterValueConversionResult.Failed(error);
                    }

                    string label = TagValueConversionService.ToCanonicalString(semanticValue);
                    if (policy.IsIgnored(label)) return TagFilterValueConversionResult.Failed($"Filter '{source.ID}' contains ignored token '{label}' and cannot be migrated automatically.");
                    EnumTagFilterValue enumFilter = new();
                    enumFilter.SetValue(enumTag, enumTag.GetOrAddValue(label));
                    converted = enumFilter;
                    if (source is StringTagFilterValue stringSource && !stringSource.CaseSensitive) impact = TagConversionImpact.Lossy;
                }
                else
                {
                    return TagFilterValueConversionResult.Failed($"Unsupported target tag type '{targetTag.GetType().Name}'.");
                }

                converted.ID = source.ID;
                return TagFilterValueConversionResult.Succeeded(converted, impact, warning);
            }
            catch (Exception exception)
            {
                return TagFilterValueConversionResult.Failed($"Filter '{source.ID}' cannot be converted to '{targetTag.GetType().Name}': {exception.Message}");
            }
        }

        private static bool TryConvertToStringFilter(TagFilterValue source, BaseTag sourceTag, out StringTagFilterValue result, out string warning, out string error)
        {
            warning = null;
            error = null;
            if (source is StringTagFilterValue stringFilter)
            {
                result = new StringTagFilterValue { Value = stringFilter.Value, ExactMatch = stringFilter.ExactMatch, CaseSensitive = stringFilter.CaseSensitive };
                return true;
            }

            if (!TryGetSingleSemanticValue(source, sourceTag, out object value, out warning, out error))
            {
                result = null;
                return false;
            }

            result = new StringTagFilterValue { Value = TagValueConversionService.ToCanonicalString(value), ExactMatch = true, CaseSensitive = true };
            if (source is NumberTagFilterValue)
            {
                warning = $"Numeric filter '{source.ID}' became an exact text filter; numeric tolerance no longer applies.";
            }

            return true;
        }

        private static bool TryConvertToNumberFilter(TagFilterValue source, BaseTag sourceTag, Func<object, NumericConversion> converter, out NumberTagFilterValue result, out TagConversionImpact impact, out string warning, out string error)
        {
            impact = TagConversionImpact.Exact;
            warning = null;
            error = null;

            if (source is NumberTagFilterValue numberFilter)
            {
                NumericConversion value = numberFilter.Type == NumberComparisonType.Range ? new NumericConversion(true, numberFilter.Value, false) : converter(numberFilter.Value);
                NumericConversion min = numberFilter.Type == NumberComparisonType.Range ? converter(numberFilter.Min) : new NumericConversion(true, numberFilter.Min, false);
                NumericConversion max = numberFilter.Type == NumberComparisonType.Range ? converter(numberFilter.Max) : new NumericConversion(true, numberFilter.Max, false);
                if (!value.Success || !min.Success || !max.Success)
                {
                    result = null;
                    error = $"Numeric filter '{source.ID}' contains a value that cannot be represented by the target tag.";
                    return false;
                }

                result = new NumberTagFilterValue { Type = numberFilter.Type, Value = value.Value, Min = min.Value, Max = max.Value };
                if (value.Lossy || min.Lossy || max.Lossy) impact = TagConversionImpact.Lossy;
                return true;
            }

            if (!TryGetSingleSemanticValue(source, sourceTag, out object semanticValue, out warning, out error))
            {
                result = null;
                return false;
            }

            NumericConversion converted = converter(semanticValue);
            if (!converted.Success)
            {
                result = null;
                error = $"Filter '{source.ID}' cannot be represented by the target numeric tag.";
                return false;
            }

            result = new NumberTagFilterValue { Type = NumberComparisonType.Equal, Value = converted.Value, Min = converted.Value, Max = converted.Value };
            if (converted.Lossy) impact = TagConversionImpact.Lossy;
            return true;
        }

        private static bool TryGetSingleSemanticValue(TagFilterValue source, BaseTag sourceTag, out object value, out string warning, out string error)
        {
            warning = null;
            error = null;
            switch (source)
            {
                case BoolTagFilterValue boolFilter:
                    value = boolFilter.Value;
                    return true;
                case StringTagFilterValue stringFilter when stringFilter.ExactMatch:
                    value = stringFilter.Value;
                    if (value == null)
                    {
                        error = $"Text filter '{source.ID}' contains null and cannot be migrated automatically.";
                        return false;
                    }

                    return true;
                case StringTagFilterValue:
                    value = null;
                    error = $"Text filter '{source.ID}' uses contains matching and cannot be converted without changing its meaning.";
                    return false;
                case NumberTagFilterValue numberFilter when numberFilter.Type == NumberComparisonType.Equal:
                    value = numberFilter.Value;
                    return true;
                case NumberTagFilterValue:
                    value = null;
                    error = $"Numeric filter '{source.ID}' is not an equality filter and cannot be converted to a single value.";
                    return false;
                case EnumTagFilterValue enumFilter when enumFilter.StringValue != null:
                    value = enumFilter.StringValue;
                    return true;
                case EnumTagFilterValue enumFilter when sourceTag is EnumTag enumTag && enumFilter.Value >= 0 && enumFilter.Value < enumTag.Values.Length:
                    value = enumTag.Values[enumFilter.Value];
                    warning = $"Legacy enum filter '{source.ID}' was resolved from its current index and may be incorrect.";
                    return true;
                case EnumTagFilterValue:
                    value = null;
                    error = $"Legacy enum filter '{source.ID}' cannot be resolved from its current definition.";
                    return false;
                default:
                    value = null;
                    error = $"Unsupported filter value type '{source.GetType().Name}'.";
                    return false;
            }
        }

        private static NumericConversion TryConvertAndClampInt(object value, IntTag tag, TagParsingPolicy policy)
        {
            if (!TagValueConversionService.TryConvertToInt(value, policy, out int converted)) return default;
            int clamped = tag.Clamp(converted);
            return new NumericConversion(true, clamped, clamped != converted);
        }

        private static NumericConversion TryConvertAndClampFloat(object value, FloatTag tag, TagParsingPolicy policy)
        {
            if (!TagValueConversionService.TryConvertToFloat(value, policy, out float converted)) return default;
            float clamped = tag.Clamp(converted);
            return new NumericConversion(true, clamped, clamped != converted);
        }

        private readonly struct NumericConversion
        {
            public bool Success { get; }
            public float Value { get; }
            public bool Lossy { get; }

            public NumericConversion(bool success, float value, bool lossy)
            {
                Success = success;
                Value = value;
                Lossy = lossy;
            }
        }
    }
}
