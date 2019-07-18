using System.ComponentModel;
using System.Runtime.Serialization;
using Tools.CSharp;
using Tools.CSharp.EEG;

namespace HBP.Data.Experience.Protocol
{
    [DataContract, DisplayName("Clamp")]
    public class ClampTreatment : Treatment
    {
        #region Properties
        [DataMember] public bool UseMinClamp { get; set; }
        [DataMember] public float Min { get; set; }
        [DataMember] public bool UseMaxClamp { get; set; }
        [DataMember] public float Max { get; set; }
        #endregion

        public override float[] Apply(float[] values, int mainEventIndex, Frequency frequency)
        {
            int startIndex = mainEventIndex - frequency.ConvertToCeiledNumberOfSamples(Window.Start);
            int endIndex = mainEventIndex + frequency.ConvertToFlooredNumberOfSamples(Window.End);
            if (UseMinClamp && !UseMaxClamp)
            {
                for (int i = startIndex; i <= endIndex; i++)
                {
                    if (values[i] < Min) values[i] = Min;
                }
            }
            else if (!UseMinClamp && UseMaxClamp)
            {
                for (int i = startIndex; i <= endIndex; i++)
                {
                    if (values[i] > Max) values[i] = Max;
                }
            }
            else if (UseMinClamp && UseMaxClamp)
            {
                for (int i = startIndex; i <= endIndex; i++)
                {
                    if (values[i] > Max)
                    {
                        values[i] = Max;
                        continue;
                    }
                    if (values[i] < Min)
                    {
                        values[i] = Min;
                        continue;
                    }
                }
            }
            return values;
        }
    }
}