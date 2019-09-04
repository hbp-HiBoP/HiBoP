using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using Tools.CSharp;
using Tools.CSharp.EEG;

namespace HBP.Data.Experience.Protocol
{
    [DataContract, DisplayName("Absolute")]
    public class AbsTreatment : Treatment
    {
        #region Public Methods
        public override void Apply(ref float[] values, ref float[] baseline, int windowMainEventIndex, int baselineMainEventIndex, Frequency frequency)
        {
            int start, end;
            if(UseOnWindow)
            {
                start = windowMainEventIndex + frequency.ConvertToCeiledNumberOfSamples(Window.Start);
                end = windowMainEventIndex + frequency.ConvertToFlooredNumberOfSamples(Window.End);
                for (int i = start; i <= end; i++)
                {
                    values[i] = Math.Abs(values[i]);
                }
            }
            if(UseOnBaseline)
            {
                start = baselineMainEventIndex + frequency.ConvertToCeiledNumberOfSamples(Baseline.Start);
                end = baselineMainEventIndex + frequency.ConvertToFlooredNumberOfSamples(Baseline.End);
                for (int i = start; i <= end; i++)
                {
                    baseline[i] = Math.Abs(baseline[i]);
                }
            }

        }
        #endregion

        #region Constructors
        public AbsTreatment() : base()
        {

        }
        public AbsTreatment(bool useOnWindow, Window window, bool useOnBaseline, Window baseline, int order, string id) : base(useOnWindow, window, useOnBaseline, baseline, order, id)
        {

        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new AbsTreatment(UseOnWindow, Window, UseOnBaseline, Baseline, Order, ID);
        }
        #endregion
    }
}