using Newtonsoft.Json;
using UnityEngine.Scripting;
using HBP.Core.Tools;

namespace HBP.Core.Data
{
    [JsonObject(MemberSerialization.OptIn), Preserve]
    public class DynamicConfiguration : BaseData
    {
        #region Properties

        /// <summary>
        /// Maximum site influence
        /// </summary>
        [JsonProperty("Site Maximum Influence")] public float MaximumInfluence { get; set; }

        /// <summary>
        /// IEEG Span Min
        /// </summary>
        [JsonProperty("Span Min")] public float SpanMin { get; set; }

        /// <summary>
        /// IEEG Span Min
        /// </summary>
        [JsonProperty("Middle")] public float Middle { get; set; }

        /// <summary>
        /// IEEG Span Max
        /// </summary>
        [JsonProperty("Span Max")] public float SpanMax { get; set; }

        /// <summary>Relative thresholds. An absent triplet keeps historical absolute thresholds pending migration.</summary>
        [JsonProperty("Span Min Ratio", NullValueHandling = NullValueHandling.Ignore, Required = Required.DisallowNull)] public float? SpanMinRatio { get; set; }
        [JsonProperty("Middle Ratio", NullValueHandling = NullValueHandling.Ignore, Required = Required.DisallowNull)] public float? MiddleRatio { get; set; }
        [JsonProperty("Span Max Ratio", NullValueHandling = NullValueHandling.Ignore, Required = Required.DisallowNull)] public float? SpanMaxRatio { get; set; }

        public bool HasSpanRatios => ThresholdRatioUtility.Validate(SpanMinRatio, MiddleRatio, SpanMaxRatio);

        // Newtonsoft.Json automatically calls ShouldSerialize<PropertyName> methods by convention.
        // Keep legacy absolute thresholds in saved projects until ratios are available, then write
        // only ratios. Legacy fields remain readable for backward compatibility with old projects.
        // Preserve prevents IL2CPP stripping of these methods, which are discovered through reflection.
        [Preserve] public bool ShouldSerializeSpanMin() => !HasSpanRatios;
        [Preserve] public bool ShouldSerializeMiddle() => !HasSpanRatios;
        [Preserve] public bool ShouldSerializeSpanMax() => !HasSpanRatios;

        protected override void OnDeserialized()
        {
            base.OnDeserialized();
            ThresholdRatioUtility.Validate(SpanMinRatio, MiddleRatio, SpanMaxRatio);
        }

        protected override void OnSerializing()
        {
            base.OnSerializing();
            ThresholdRatioUtility.Validate(SpanMinRatio, MiddleRatio, SpanMaxRatio);
        }

        #endregion

        #region Constructor

        public DynamicConfiguration(float maximumInfluence, float spanMin, float middle, float spanMax, float? spanMinRatio = null, float? middleRatio = null, float? spanMaxRatio = null) : base()
        {
            MaximumInfluence = maximumInfluence;
            SpanMin = spanMin;
            Middle = middle;
            SpanMax = spanMax;
            SpanMinRatio = spanMinRatio;
            MiddleRatio = middleRatio;
            SpanMaxRatio = spanMaxRatio;
        }

        public DynamicConfiguration(float maximumInfluence, float spanMin, float middle, float spanMax, float? spanMinRatio, float? middleRatio, float? spanMaxRatio, string ID) : base(ID)
        {
            MaximumInfluence = maximumInfluence;
            SpanMin = spanMin;
            Middle = middle;
            SpanMax = spanMax;
            SpanMinRatio = spanMinRatio;
            MiddleRatio = middleRatio;
            SpanMaxRatio = spanMaxRatio;
        }

        public DynamicConfiguration() : this(15, 0, 0, 0)
        {
        }

        #endregion

        #region Public Methods

        public override object Clone()
        {
            return new DynamicConfiguration(MaximumInfluence, SpanMin, Middle, SpanMax, SpanMinRatio, MiddleRatio, SpanMaxRatio, ID);
        }

        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is DynamicConfiguration dynamicConfiguration)
            {
                MaximumInfluence = dynamicConfiguration.MaximumInfluence;
                SpanMin = dynamicConfiguration.SpanMin;
                Middle = dynamicConfiguration.Middle;
                SpanMax = dynamicConfiguration.SpanMax;
                SpanMinRatio = dynamicConfiguration.SpanMinRatio;
                MiddleRatio = dynamicConfiguration.MiddleRatio;
                SpanMaxRatio = dynamicConfiguration.SpanMaxRatio;
            }
        }

        #endregion
    }
}
