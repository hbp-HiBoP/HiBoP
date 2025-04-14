using Newtonsoft.Json;

namespace HBP.Core.Data
{
    [JsonObject(MemberSerialization.OptIn)]
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
        #endregion

        #region Constructor
        public DynamicConfiguration(float maximumInfluence, float spanMin, float middle, float spanMax) : base()
        {
            MaximumInfluence = maximumInfluence;
            SpanMin = spanMin;
            Middle = middle;
            SpanMax = spanMax;
        }
        public DynamicConfiguration(float maximumInfluence, float spanMin, float middle, float spanMax, string ID) : base(ID)
        {
            MaximumInfluence = maximumInfluence;
            SpanMin = spanMin;
            Middle = middle;
            SpanMax = spanMax;
        }
        public DynamicConfiguration() : this(15, 0, 0, 0)
        {
        }
        #endregion

        #region Public Methods
        public override object Clone()
        {
            return new DynamicConfiguration(MaximumInfluence, SpanMin, Middle, SpanMax, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if(copy is DynamicConfiguration dynamicConfiguration)
            {
                MaximumInfluence = dynamicConfiguration.MaximumInfluence;
                SpanMin = dynamicConfiguration.SpanMin;
                Middle = dynamicConfiguration.Middle;
                SpanMax = dynamicConfiguration.SpanMax;
            }
        }
        #endregion
    }
}