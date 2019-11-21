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
            BeforeMin = 80;
            BeforeMax = 120;
            AfterMin = -1;
            AfterMax = 1;
        }
        public RescaleTreatment(string ID) : base(ID)
        {
            BeforeMin = 80;
            BeforeMax = 120;
            AfterMin = -1;
            AfterMax = 1;
        }
        public RescaleTreatment(bool useOnWindow, Window window, bool useOnBaseline, Window baseline, float beforeMin, float beforeMax, float afterMin, float afterMax, int order, string ID) : base(useOnWindow, window, useOnBaseline, baseline, order, ID)
        {
            BeforeMin = beforeMin;
            BeforeMax = beforeMax;
            AfterMin = afterMin;
            AfterMax = afterMax;
        }
        #endregion

        #region Public Methods
        public override void Apply(ref float[] values, ref float[] baseline, int windowMainEventIndex, int baselineMainEventIndex, Frequency frequency)
        {
            float ratio = (AfterMax - AfterMin) / (BeforeMax - BeforeMin);
            if (UseOnWindow)
            {
                int startIndex = windowMainEventIndex + frequency.ConvertToCeiledNumberOfSamples(Window.Start);
                int endIndex = windowMainEventIndex + frequency.ConvertToFlooredNumberOfSamples(Window.End);
                for (int i = startIndex; i <= endIndex; i++)
                {
                    values[i] = ratio * (values[i] - BeforeMin) + AfterMin;
                }
            }
            if(UseOnBaseline)
            {
                int startIndex = baselineMainEventIndex + frequency.ConvertToCeiledNumberOfSamples(Baseline.Start);
                int endIndex = baselineMainEventIndex + frequency.ConvertToFlooredNumberOfSamples(Baseline.End);
                for (int i = startIndex; i <= endIndex; i++)
                {
                    baseline[i] = ratio * (baseline[i] - BeforeMin) + AfterMin;
                }
            }
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
            if(copy is RescaleTreatment treatment)
            {
                BeforeMin = treatment.BeforeMin;
                BeforeMax = treatment.BeforeMax;
                AfterMin = treatment.AfterMin;
                AfterMax = treatment.AfterMax;
            }
        }
        #endregion
    }
}