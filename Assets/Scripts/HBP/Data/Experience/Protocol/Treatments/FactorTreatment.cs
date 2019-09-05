using System.ComponentModel;
using System.Runtime.Serialization;
using Tools.CSharp;
using Tools.CSharp.EEG;

namespace HBP.Data.Experience.Protocol
{
    [DataContract, DisplayName("Factor")]
    public class FactorTreatment : Treatment
    {
        #region Properties
        [DataMember] public float Factor { get; set; }
        #endregion

        #region Constructors
        public FactorTreatment() : base()
        {
            Factor = 1;
        }
        public FactorTreatment(bool useOnWindow, Window window, bool useOnBaseline, Window baseline, float factor, int order, string id) : base(useOnWindow, window, useOnBaseline, baseline, order, id)
        {
            Factor = factor;
        }
        #endregion

        #region Public Methods
        public override void Apply(ref float[] values, ref float[] baseline, int windowMainEventIndex, int baselineMainEventIndex, Frequency frequency)
        {
            if(UseOnWindow)
            {
                int startIndex = windowMainEventIndex + frequency.ConvertToCeiledNumberOfSamples(Window.Start);
                int endIndex = windowMainEventIndex + frequency.ConvertToFlooredNumberOfSamples(Window.End);
                for (int i = startIndex; i <= endIndex; i++)
                {
                    values[i] *= Factor;
                }
            }
            if (UseOnBaseline)
            {
                int startIndex = baselineMainEventIndex + frequency.ConvertToCeiledNumberOfSamples(Baseline.Start);
                int endIndex = baselineMainEventIndex + frequency.ConvertToFlooredNumberOfSamples(Baseline.End);
                for (int i = startIndex; i <= endIndex; i++)
                {
                    baseline[i] *= Factor;
                }
            }
        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new FactorTreatment(UseOnWindow, Window, UseOnBaseline, Baseline, Factor, Order, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if(copy is FactorTreatment treatment)
            {
                Factor = treatment.Factor;
            }
        }
        #endregion
    }
}