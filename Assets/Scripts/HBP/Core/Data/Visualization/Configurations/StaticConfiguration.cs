using System.Runtime.Serialization;

namespace HBP.Core.Data
{
    [DataContract]
    public class StaticConfiguration : BaseData
    {
        #region Properties
        /// <summary>
        /// Maximum site influence
        /// </summary>
        [DataMember(Name = "Site Maximum Influence")] public float MaximumInfluence { get; set; }
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
        public StaticConfiguration(float maximumInfluence, float spanMin, float middle, float spanMax) : base()
        {
            MaximumInfluence = maximumInfluence;
            SpanMin = spanMin;
            Middle = middle;
            SpanMax = spanMax;
        }
        public StaticConfiguration(float maximumInfluence, float spanMin, float middle, float spanMax, string ID) : base(ID)
        {
            MaximumInfluence = maximumInfluence;
            SpanMin = spanMin;
            Middle = middle;
            SpanMax = spanMax;
        }
        public StaticConfiguration() : this(15, 0, 0, 0)
        {
        }
        #endregion

        #region Public Methods
        public override object Clone()
        {
            return new StaticConfiguration(MaximumInfluence, SpanMin, Middle, SpanMax, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is StaticConfiguration staticConfiguration)
            {
                MaximumInfluence = staticConfiguration.MaximumInfluence;
                SpanMin = staticConfiguration.SpanMin;
                Middle = staticConfiguration.Middle;
                SpanMax = staticConfiguration.SpanMax;
            }
        }
        #endregion
    }
}