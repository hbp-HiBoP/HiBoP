using System.Runtime.Serialization;

namespace HBP.Data.Visualization
{
    [DataContract]
    public class FMRIConfiguration : BaseData
    {
        #region Properties
        [DataMember(Name = "Negative Min")] public float NegativeMin { get; set; }
        [DataMember(Name = "Negative Max")] public float NegativeMax { get; set; }
        [DataMember(Name = "Positive Min")] public float PositiveMin { get; set; }
        [DataMember(Name = "Positive Max")] public float PositiveMax { get; set; }
        #endregion

        #region Constructors
        public FMRIConfiguration(float negativeMin, float negativeMax, float positiveMin, float positiveMax) : base()
        {
            NegativeMin = negativeMin;
            NegativeMax = negativeMax;
            PositiveMin = positiveMin;
            PositiveMax = positiveMax;
        }
        public FMRIConfiguration(float negativeMin, float negativeMax, float positiveMin, float positiveMax, string ID) : base(ID)
        {
            NegativeMin = negativeMin;
            NegativeMax = negativeMax;
            PositiveMin = positiveMin;
            PositiveMax = positiveMax;
        }
        public FMRIConfiguration() : this(0.05f, 0.5f, 0.05f, 0.5f)
        {

        }
        #endregion

        #region Public Methods
        public override object Clone()
        {
            return new FMRIConfiguration(NegativeMin, NegativeMax, PositiveMin, PositiveMax, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is FMRIConfiguration fmriConfiguration)
            {
                NegativeMin = fmriConfiguration.NegativeMin;
                NegativeMax = fmriConfiguration.NegativeMax;
                PositiveMin = fmriConfiguration.PositiveMin;
                PositiveMax = fmriConfiguration.PositiveMax;
            }
        }
        #endregion

        #region Private Methods
        #endregion
    }
}