using System;

namespace HBP.Core.Data
{
    public enum RawTagValueStatus
    {
        Success,
        Ignored,
        Incompatible
    }

    public readonly struct RawTagValueResult
    {
        public RawTagValueStatus Status { get; }
        public BaseTagValue Value { get; }
        public string Error { get; }

        private RawTagValueResult(RawTagValueStatus status, BaseTagValue value, string error)
        {
            Status = status;
            Value = value;
            Error = error;
        }

        public static RawTagValueResult Succeeded(BaseTagValue value) => new(RawTagValueStatus.Success, value, null);
        public static RawTagValueResult Ignored() => new(RawTagValueStatus.Ignored, null, null);
        public static RawTagValueResult Incompatible(string error) => new(RawTagValueStatus.Incompatible, null, error);
    }

    public static class RawTagValueFactory
    {
        public static RawTagValueResult TryCreate(BaseTag tag, string rawValue, TagParsingPolicy policy, bool allowEnumExpansion = true)
        {
            if (tag == null) throw new ArgumentNullException(nameof(tag));
            policy ??= TagParsingPolicy.Default;
            if (string.IsNullOrWhiteSpace(rawValue) || policy.IsIgnored(rawValue)) return RawTagValueResult.Ignored();

            string value = rawValue.Trim();
            switch (tag)
            {
                case StringTag stringTag:
                    return RawTagValueResult.Succeeded(new StringTagValue(stringTag, rawValue));
                case BoolTag boolTag when policy.TryParseBoolean(value, out bool boolean):
                    return RawTagValueResult.Succeeded(new BoolTagValue(boolTag, boolean));
                case IntTag intTag when policy.TryParseInteger(value, out int integer) && (!intTag.Clamped || integer >= intTag.Min && integer <= intTag.Max):
                    return RawTagValueResult.Succeeded(new IntTagValue(intTag, integer));
                case FloatTag floatTag when policy.TryParseFloat(value, out float number) && (!floatTag.Clamped || number >= floatTag.Min && number <= floatTag.Max):
                    return RawTagValueResult.Succeeded(new FloatTagValue(floatTag, number));
                case EnumTag enumTag when enumTag.TryGetValueIndex(rawValue, out int enumIndex):
                    return RawTagValueResult.Succeeded(new EnumTagValue(enumTag, enumIndex));
                case EnumTag enumTag when allowEnumExpansion:
                    return RawTagValueResult.Succeeded(new EnumTagValue(enumTag, enumTag.GetOrAddValue(rawValue)));
                case EmptyTag emptyTag:
                    return RawTagValueResult.Succeeded(new EmptyTagValue(emptyTag));
                default:
                    return RawTagValueResult.Incompatible($"Value '{rawValue}' is incompatible with {tag.GetType().Name} tag '{tag.Name}'.");
            }
        }
    }
}
