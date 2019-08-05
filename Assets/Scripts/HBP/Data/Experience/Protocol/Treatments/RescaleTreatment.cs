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
        [DataMember] public float Min { get; set; }
        [DataMember] public float Max { get; set; }
        #endregion

        #region Constructors
        public RescaleTreatment() : base()
        {

        }
        public RescaleTreatment(Window window, float min, float max, int order, string id) : base(window, order, id)
        {
            Min = min;
            Max = max;
        }
        #endregion

        #region Public Methods
        public override float[] Apply(float[] values, int mainEventIndex, Frequency frequency)
        {
            int startIndex = mainEventIndex - frequency.ConvertToCeiledNumberOfSamples(Window.Start);
            int endIndex = mainEventIndex + frequency.ConvertToFlooredNumberOfSamples(Window.End);
            float min = float.MaxValue, max = float.MinValue;
            for (int i = startIndex; i <= endIndex; i++)
            {
                if (values[i] > max) max = values[i];
                if (values[i] < min) min = values[i];
            }
            for (int i = startIndex; i <= endIndex; i++)
            {
                values[i] = ((Max - Min) / (max - min)) * (values[i] - min) + Min;  
            }
            return values;
        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new RescaleTreatment(Window, Min, Max, Order, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            RescaleTreatment treatment = copy as RescaleTreatment;
            Min = treatment.Min;
            Max = treatment.Max;
        }
        #endregion
    }
}