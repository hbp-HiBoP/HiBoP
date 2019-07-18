using System.ComponentModel;
using System.Runtime.Serialization;
using Tools.CSharp;
using Tools.CSharp.EEG;

namespace HBP.Data.Experience.Protocol
{
    [DataContract, DisplayName("Max")]
    public class MaxTreatment : Treatment
    {
        public override float[] Apply(float[] values, int mainEventIndex, Frequency frequency)
        {
            int startIndex = mainEventIndex - frequency.ConvertToCeiledNumberOfSamples(Window.Start);
            int endIndex = mainEventIndex + frequency.ConvertToFlooredNumberOfSamples(Window.End);
            float max = float.MinValue;
            for (int i = startIndex; i <= endIndex; i++)
            {
                if (max < values[i]) max = values[i];
            }
            for (int i = startIndex; i <= endIndex; i++)
            {
                values[i] = max;
            }
            return values;
        }
    }
}