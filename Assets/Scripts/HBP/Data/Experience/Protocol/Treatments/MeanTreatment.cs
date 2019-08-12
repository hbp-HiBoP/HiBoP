using Tools.CSharp;
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
        public MeanTreatment(Window window, int order, string id) : base(window, order, id)
        {
        }
        #endregion

        #region Public Methods
        public override float[] Apply(float[] values, int mainEventIndex, Frequency frequency)
        {
            int startIndex = mainEventIndex + frequency.ConvertToCeiledNumberOfSamples(Window.Start);
            int endIndex = mainEventIndex + frequency.ConvertToFlooredNumberOfSamples(Window.End);
            float[] subArray = new float[endIndex - startIndex + 1];
            Array.Copy(values, startIndex, subArray, 0, subArray.Length);
            float mean = subArray.Mean();
            for (int i = startIndex; i <= endIndex; i++) values[i] = mean;
            return values;
        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new MeanTreatment(Window, Order, ID);
        }
        #endregion
    }
}