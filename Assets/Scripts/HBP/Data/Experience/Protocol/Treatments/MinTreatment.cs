using System.ComponentModel;
using System.Runtime.Serialization;
using Tools.CSharp;
using Tools.CSharp.EEG;

namespace HBP.Data.Experience.Protocol
{
    [DataContract, DisplayName("Min")]
    public class MinTreatment : Treatment
    {
        #region Constructors
        public MinTreatment() : base()
        {

        }
        public MinTreatment(bool useOnWindow, Window window, bool useOnBaseline, Window baseline, int order, string id) : base(useOnWindow, window, useOnBaseline, baseline, order, id)
        {
        }
        #endregion

        #region Public Methods
        public override void Apply(ref float[] values, ref float[] baseline, int mainEventIndex, Frequency frequency)
        {
            int startWindow = mainEventIndex + frequency.ConvertToCeiledNumberOfSamples(Window.Start);
            int endWindow = mainEventIndex + frequency.ConvertToFlooredNumberOfSamples(Window.End);
            int startBaseline = mainEventIndex + frequency.ConvertToCeiledNumberOfSamples(Baseline.Start);
            int endBaseline = mainEventIndex + frequency.ConvertToFlooredNumberOfSamples(Baseline.End);
            float min = float.MaxValue;
            if (UseOnWindow)
            {
                for (int i = startWindow; i <= endWindow; i++)
                {
                    if (min > values[i]) min = values[i];
                }
            }
            if (UseOnBaseline)
            {
                for (int i = startBaseline; i <= endBaseline; i++)
                {
                    if (min > baseline[i]) min = baseline[i];
                }
            }

            if (UseOnWindow)
            {
                for (int i = startWindow; i <= endWindow; i++)
                {
                    values[i] = min;
                }
            }
            if (UseOnBaseline)
            {
                for (int i = startBaseline; i <= endBaseline; i++)
                {
                    baseline[i] = min;
                }
            }
        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new MinTreatment(UseOnWindow, Window, UseOnBaseline, Baseline, Order, ID);
        }
        #endregion
    }
}