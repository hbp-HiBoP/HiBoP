﻿using Tools.CSharp;
using Tools.CSharp.EEG;
using System;
using System.Runtime.Serialization;
using System.ComponentModel;

namespace HBP.Data.Experience.Protocol
{
    [DataContract, DisplayName("Mean")]
    public class MeanTreatment : Treatment
    {
        #region Constructors
        public MeanTreatment() : base()
        {

        }
        public MeanTreatment(bool useOnWindow, Window window, bool useOnBaseline, Window baseline, int order, string id) : base(useOnWindow, window, useOnBaseline, baseline, order, id)
        {
        }
        #endregion

        #region Public Methods
        public override void Apply(ref float[] values, ref float[] baseline, int mainEventIndex, Frequency frequency)
        {
            float[] windowSubArray = new float[0];
            float[] baselineSubArray = new float[0];
            int startWindow = mainEventIndex + frequency.ConvertToCeiledNumberOfSamples(Window.Start);
            int endWindow = mainEventIndex + frequency.ConvertToFlooredNumberOfSamples(Window.End);
            int startBaseline = mainEventIndex + frequency.ConvertToCeiledNumberOfSamples(Baseline.Start);
            int endBaseline = mainEventIndex + frequency.ConvertToFlooredNumberOfSamples(Baseline.End);
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
            float mean = subArray.Mean();
            if(UseOnWindow)
            {
                for (int i = startWindow; i <= endWindow; i++) values[i] = mean;
            }
            if(UseOnBaseline)
            {
                for (int i = startBaseline; i <= endBaseline; i++) baseline[i] = mean;
            }
        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new MeanTreatment(UseOnWindow, Window, UseOnBaseline, Baseline, Order, ID);
        }
        #endregion
    }
}