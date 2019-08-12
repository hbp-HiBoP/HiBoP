using System.ComponentModel;
using System.Runtime.Serialization;
using Tools.CSharp;
using Tools.CSharp.EEG;

namespace HBP.Data.Experience.Protocol
{
    [DataContract, DisplayName("Rescale")]
    public class RescaleTreatment : Treatment
    {
        #region Properties
        [DataMember] public float BeforeMin { get; set; }
        [DataMember] public float BeforeMax { get; set; }
        [DataMember] public float AfterMin { get; set; }
        [DataMember] public float AfterMax { get; set; }
        #endregion

        #region Constructors
        public RescaleTreatment() : base()
        {

        }
        public RescaleTreatment(bool useOnWindow, Window window, bool useOnBaseline, Window baseline, float beforeMin, float beforeMax, float afterMin, float afterMax, int order, string id) : base(useOnWindow, window, useOnBaseline, baseline, order, id)
        {
            BeforeMin = beforeMin;
            BeforeMax = beforeMax;
            AfterMin = afterMin;
            AfterMax = afterMax;
        }
        #endregion

        #region Public Methods
        public override float[] Apply(float[] values, int mainEventIndex, Frequency frequency)
        {
            int startIndex = mainEventIndex + frequency.ConvertToCeiledNumberOfSamples(Window.Start);
            int endIndex = mainEventIndex + frequency.ConvertToFlooredNumberOfSamples(Window.End);
            float ratio = (AfterMax - AfterMin) / (BeforeMax - BeforeMin);
            for (int i = startIndex; i <= endIndex; i++)
            {
                values[i] = ratio * (values[i] - BeforeMin) + AfterMin;  
            }
            return values;
        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new RescaleTreatment(UseOnWindow, Window, UseOnBaseline, Baseline, BeforeMin, BeforeMax, AfterMin, AfterMax, Order, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            RescaleTreatment treatment = copy as RescaleTreatment;
            BeforeMin = treatment.BeforeMin;
            BeforeMax = treatment.BeforeMax;
            AfterMin = treatment.AfterMin;
            AfterMax = treatment.AfterMax;
        }
        #endregion
    }
}