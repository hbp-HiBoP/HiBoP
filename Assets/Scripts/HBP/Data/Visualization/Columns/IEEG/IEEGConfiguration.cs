using System;
using System.Runtime.Serialization;

namespace HBP.Data.Visualization
{
    [DataContract]
    public class IEEGConfiguration : ICloneable
    {
        #region Properties
        /// <summary>
        /// IEEG Sites Gain
        /// </summary>
        [DataMember(Name = "Site Gain")] public float Gain { get; set; }
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
        public IEEGConfiguration(float gain, float maximumInfluence, float alpha, float spanMin, float middle, float spanMax)
        {
            Gain = gain;
            MaximumInfluence = maximumInfluence;
            Alpha = alpha;
            SpanMin = spanMin;
            Middle = middle;
            SpanMax = spanMax;

        }
        public IEEGConfiguration() : this(1, 15, .8f,0,0,0)
        {
        }
        #endregion

        #region Public Methods
        public object Clone()
        {
            return new IEEGConfiguration(Gain,MaximumInfluence,Alpha,SpanMin,Middle,SpanMax);
        }
        #endregion
    }
}