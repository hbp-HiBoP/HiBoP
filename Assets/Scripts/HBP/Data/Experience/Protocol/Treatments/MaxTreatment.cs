using System.ComponentModel;
using System.Runtime.Serialization;
using Tools.CSharp;
using Tools.CSharp.EEG;

namespace HBP.Data.Experience.Protocol
{
    [DataContract, DisplayName("Max")]
    public class MaxTreatment : Treatment
    {
        #region Constructors
        public MaxTreatment() : base()
        {

        }
        public MaxTreatment(Window window, int order, string id) : base(window, order, id)
        {
        }
        #endregion

        #region Public Methods
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
        #endregion

        #region Operators
        public override object Clone()
        {
            return new MaxTreatment(Window, Order, ID);
        }
        #endregion
    }
}