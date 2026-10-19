using Newtonsoft.Json;

namespace HBP.Core.Tools
{
    /// <summary>Validates the optional, indivisible triplet used by saved activity thresholds.</summary>
    public static class ThresholdRatioUtility
    {
        public static bool Validate(float? min, float? middle, float? max)
        {
            if (!min.HasValue && !middle.HasValue && !max.HasValue) return false;
            if (!min.HasValue || !middle.HasValue || !max.HasValue ||
                !IsFinite(min.Value) || !IsFinite(middle.Value) || !IsFinite(max.Value) ||
                min.Value > middle.Value || middle.Value > max.Value)
                throw new JsonSerializationException("Threshold ratios must contain three finite, ordered values.");
            return true;
        }

        public static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
