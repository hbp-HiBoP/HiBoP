using System.Runtime.Serialization;

namespace HBP.Data.Visualization
{
    [DataContract]
    public class DynamicConfiguration : BaseData
    {
        #region Properties
        /// <summary>
        /// Maximum site influence
        /// </summary>
        [DataMember(Name = "Site Maximum Influence")] public float MaximumInfluence { get; set; }
        /// <summary>
        /// IEEG Transparency
        /// </summary>
        [DataMember(Name = "Transparency")] public float Alpha { get; set; }
        /// <summary>
        /// IEEG Span Min
        /// </summary>
        [DataMember(Name = "Span Min")] public float SpanMin { get; set; }
        /// <summary>
        /// IEEG Span Min
        /// </summary>
        [DataMember(Name = "Middle")] public float Middle { get; set; }
        /// <summary>
        /// IEEG Span Max
        /// </summary>
        [DataMember(Name = "Span Max")] public float SpanMax { get; set; }
        #endregion

        #region Constructor
        public DynamicConfiguration(float maximumInfluence, float alpha, float spanMin, float middle, float spanMax) : base()
        {
            MaximumInfluence = maximumInfluence;
            Alpha = alpha;
            SpanMin = spanMin;
            Middle = middle;
            SpanMax = spanMax;
        }
        public DynamicConfiguration(float maximumInfluence, float alpha, float spanMin, float middle, float spanMax, string ID) : base(ID)
        {
            MaximumInfluence = maximumInfluence;
            Alpha = alpha;
            SpanMin = spanMin;
            Middle = middle;
            SpanMax = spanMax;
        }
        public DynamicConfiguration() : this(15, .8f, 0, 0, 0)
        {
        }
        #endregion

        #region Public Methods
        public override object Clone()
        {
            return new DynamicConfiguration(MaximumInfluence, Alpha, SpanMin, Middle, SpanMax, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if(copy is DynamicConfiguration dynamicConfiguration)
            {
                MaximumInfluence = dynamicConfiguration.MaximumInfluence;
                Alpha = dynamicConfiguration.Alpha;
                SpanMin = dynamicConfiguration.SpanMin;
                Middle = dynamicConfiguration.Middle;
                SpanMax = dynamicConfiguration.SpanMax;
            }
        }
        #endregion
    }
}