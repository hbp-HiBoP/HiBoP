using System.Runtime.Serialization;
using Tools.CSharp;
using Tools.CSharp.EEG;

namespace HBP.Data.Experience.Protocol
{
    [DataContract]
    public class RescaleTreatment : Treatment
    {
        #region Properties
        [DataMember] public float Min { get; set; }
        [DataMember] public float Max { get; set; }
        #endregion

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
    }
}