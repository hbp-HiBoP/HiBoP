using System.ComponentModel;
using System.Runtime.Serialization;
using Tools.CSharp;
using Tools.CSharp.EEG;

namespace HBP.Data.Experience.Protocol
{
    [DataContract, DisplayName("Max")]
    public class MaxTreatment : Treatment
    {
        #region Constructors
        public MaxTreatment() : base()
        {

        }
        public MaxTreatment(bool useOnWindow, Window window, bool useOnBaseline, Window baseline, int order, string id) : base(useOnWindow, window, useOnBaseline, baseline, order, id)
        {
        }
        #endregion

        #region Public Methods
        public override void Apply(ref float[] values, ref float[] baseline, int windowMainEventIndex, int baselineMainEventIndex, Frequency frequency)
        {
            int startWindow = windowMainEventIndex + frequency.ConvertToCeiledNumberOfSamples(Window.Start);
            int endWindow = windowMainEventIndex + frequency.ConvertToFlooredNumberOfSamples(Window.End);
            int startBaseline = baselineMainEventIndex + frequency.ConvertToCeiledNumberOfSamples(Baseline.Start);
            int endBaseline = baselineMainEventIndex + frequency.ConvertToFlooredNumberOfSamples(Baseline.End);
            float max = float.MinValue;
            if(UseOnWindow)
            {
                for (int i = startWindow; i <= endWindow; i++)
                {
                    if (max < values[i]) max = values[i];
                }
            }
            if (UseOnBaseline)
            {
                for (int i = startBaseline; i <= endBaseline; i++)
                {
                    if (max < baseline[i]) max = baseline[i];
                }
            }

            if(UseOnWindow)
            {
                for (int i = startWindow; i <= endWindow; i++)
                {
                    values[i] = max;
                }
            }
            if(UseOnBaseline)
            {
                for (int i = startBaseline; i <= endBaseline; i++)
                {
                    baseline[i] = max;
                }
            }
        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new MaxTreatment(UseOnWindow, Window, UseOnBaseline, Baseline, Order, ID);
        }
        #endregion
    }
}