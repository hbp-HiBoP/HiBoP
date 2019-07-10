using System.Runtime.Serialization;
using Tools.CSharp;
using Tools.CSharp.EEG;

namespace HBP.Data.Experience.Protocol
{
    [DataContract]
    public class OffsetTreatment : Treatment
    {
        #region Properties
        [DataMember] public float Offset { get; set; }
        #endregion

        public override float[] Apply(float[] values, int mainEventIndex, Frequency frequency)
        {
            int startIndex = mainEventIndex - frequency.ConvertToCeiledNumberOfSamples(Window.Start);
            int endIndex = mainEventIndex + frequency.ConvertToFlooredNumberOfSamples(Window.End);
            for (int i = startIndex; i <= endIndex; i++)
            {
                values[i] = values[i] + Offset;
            }
            return values;
        }
    }
}