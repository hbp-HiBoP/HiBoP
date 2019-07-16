using System.ComponentModel;
using System.Runtime.Serialization;
using Tools.CSharp;
using Tools.CSharp.EEG;

namespace HBP.Data.Experience.Protocol
{
    [DataContract, DisplayName("Treshold")]
    public class TresholdTreatment : Treatment
    {
        #region Properties
        [DataMember] public bool UseMinTreshold { get; set; }
        [DataMember] public float MinThreshold { get; set; }
        [DataMember] public bool UseMaxTreshold { get; set; }
        [DataMember] public float MaxTreshold { get; set; }
        #endregion

        public override float[] Apply(float[] values, int mainEventIndex, Frequency frequency)
        {
            int startIndex = mainEventIndex - frequency.ConvertToCeiledNumberOfSamples(Window.Start);
            int endIndex = mainEventIndex + frequency.ConvertToFlooredNumberOfSamples(Window.End);
            if(UseMinTreshold && !UseMaxTreshold)
            {
                for (int i = startIndex; i <= endIndex; i++)
                {
                    if(values[i] > MinThreshold) values[i] = 0;
                }
            }
            else if(!UseMinTreshold && UseMaxTreshold)
            {
                for (int i = startIndex; i <= endIndex; i++)
                {
                    if (values[i] < MaxTreshold) values[i] = 0;
                }
            }
            else if(UseMinTreshold && UseMaxTreshold)
            {
                float value;
                for (int i = startIndex; i <= endIndex; i++)
                {
                    value = values[i];
                    if (value < MaxTreshold && value > MinThreshold) values[i] = 0;
                }
            }
            return values;
        }
    }
}