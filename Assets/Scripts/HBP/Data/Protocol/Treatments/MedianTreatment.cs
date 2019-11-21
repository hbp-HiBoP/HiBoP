using Tools.CSharp;
using Tools.CSharp.EEG;
using System;
using System.Runtime.Serialization;
using System.ComponentModel;

namespace HBP.Data.Experience.Protocol
{
    [DataContract, DisplayName("Median")]
    public class MedianTreatment : Treatment
    {
        #region Constructors
        public MedianTreatment() : base()
        {

        }
        public MedianTreatment(string ID) : base()
        {

        }
        public MedianTreatment(bool useOnWindow, Window window, bool useOnBaseline, Window baseline, int order, string ID) : base(useOnWindow, window, useOnBaseline, baseline, order, ID)
        {
        }
        #endregion

        #region Public Methods
        public override void Apply(ref float[] values, ref float[] baseline, int windowMainEventIndex, int baselineMainEventIndex, Frequency frequency)
        {
            float[] windowSubArray = new float[0];
            float[] baselineSubArray = new float[0];
            int startWindow = windowMainEventIndex + frequency.ConvertToCeiledNumberOfSamples(Window.Start);
            int endWindow = windowMainEventIndex + frequency.ConvertToFlooredNumberOfSamples(Window.End);
            int startBaseline = baselineMainEventIndex + frequency.ConvertToCeiledNumberOfSamples(Baseline.Start);
            int endBaseline = baselineMainEventIndex + frequency.ConvertToFlooredNumberOfSamples(Baseline.End);
            if (UseOnWindow)
            {
                windowSubArray = new float[endWindow - startWindow + 1];
                Array.Copy(values, startWindow, windowSubArray, 0, windowSubArray.Length);
            }
            if (UseOnBaseline)
            {
                baselineSubArray = new float[endBaseline - startBaseline + 1];
                Array.Copy(baseline, startBaseline, baselineSubArray, 0, baselineSubArray.Length);
            }
            float[] subArray = new float[windowSubArray.Length + baselineSubArray.Length];
            windowSubArray.CopyTo(subArray, 0);
            baselineSubArray.CopyTo(subArray, windowSubArray.Length);
            float median = subArray.Median();
            if (UseOnWindow)
            {
                for (int i = startWindow; i <= endWindow; i++) values[i] = median;
            }
            if (UseOnBaseline)
            {
                for (int i = startBaseline; i <= endBaseline; i++) baseline[i] = median;
            }
        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new MedianTreatment(UseOnWindow, Window, UseOnBaseline, Baseline, Order, ID);
        }
        #endregion
    }
}