using System.Runtime.Serialization;

namespace HBP.Core.Data
{
    [DataContract]
    public class MEGConfiguration : BaseData
    {
        #region Properties
        [DataMember(Name = "Negative Min")] public float NegativeMin { get; set; }
        [DataMember(Name = "Negative Max")] public float NegativeMax { get; set; }
        [DataMember(Name = "Positive Min")] public float PositiveMin { get; set; }
        [DataMember(Name = "Positive Max")] public float PositiveMax { get; set; }
        [DataMember(Name = "Hide Lower Values")] public bool HideLowerValues { get; set; }
        [DataMember(Name = "Hide Middle Values")] public bool HideMiddleValues { get; set; }
        [DataMember(Name = "Hide Higher Values")] public bool HideHigherValues { get; set; }
        #endregion

        #region Constructors
        public MEGConfiguration(float negativeMin, float negativeMax, float positiveMin, float positiveMax, bool lower, bool middle, bool higher) : base()
        {
            NegativeMin = negativeMin;
            NegativeMax = negativeMax;
            PositiveMin = positiveMin;
            PositiveMax = positiveMax;
            HideLowerValues = lower;
            HideMiddleValues = middle;
            HideHigherValues = higher;
        }
        public MEGConfiguration(float negativeMin, float negativeMax, float positiveMin, float positiveMax, bool lower, bool middle, bool higher, string ID) : base(ID)
        {
            NegativeMin = negativeMin;
            NegativeMax = negativeMax;
            PositiveMin = positiveMin;
            PositiveMax = positiveMax;
            HideLowerValues = lower;
            HideMiddleValues = middle;
            HideHigherValues = higher;
        }
        public MEGConfiguration() : this(0.05f, 0.5f, 0.05f, 0.5f, false, false, false)
        {

        }
        #endregion

        #region Public Methods
        public override object Clone()
        {
            return new MEGConfiguration(NegativeMin, NegativeMax, PositiveMin, PositiveMax, HideLowerValues, HideMiddleValues, HideHigherValues, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is MEGConfiguration fmriConfiguration)
            {
                NegativeMin = fmriConfiguration.NegativeMin;
                NegativeMax = fmriConfiguration.NegativeMax;
                PositiveMin = fmriConfiguration.PositiveMin;
                PositiveMax = fmriConfiguration.PositiveMax;
                HideLowerValues = fmriConfiguration.HideLowerValues;
                HideMiddleValues = fmriConfiguration.HideMiddleValues;
                HideHigherValues = fmriConfiguration.HideHigherValues;
            }
        }
        #endregion

        #region Private Methods
        #endregion
    }
}