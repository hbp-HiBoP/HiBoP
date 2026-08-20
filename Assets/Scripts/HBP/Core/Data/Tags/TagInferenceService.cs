using System;
using System.Collections.Generic;
using System.Linq;

namespace HBP.Core.Data
{
    public static class TagInferenceService
    {
        public static BaseTag Infer(string tagName, IEnumerable<string> values, TagParsingPolicy policy)
        {
            if (string.IsNullOrWhiteSpace(tagName)) throw new ArgumentException("A tag name is required.", nameof(tagName));
            if (policy == null) throw new ArgumentNullException(nameof(policy));

            List<string> meaningfulValues = (values ?? Enumerable.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value) && !policy.IsIgnored(value)).Select(value => value.Trim()).ToList();

            if (meaningfulValues.Count == 0) return new StringTag(tagName);
            if (meaningfulValues.All(value => policy.TryParseBoolean(value, out _))) return new BoolTag(tagName);
            if (meaningfulValues.All(value => policy.TryParseInteger(value, out _))) return new IntTag(tagName);
            if (meaningfulValues.All(value => policy.TryParseFloat(value, out _))) return new FloatTag(tagName);
            return new StringTag(tagName);
        }
    }
}
