using System.ComponentModel;
using System.Runtime.Serialization;
using Tools.CSharp;
using Tools.CSharp.EEG;

namespace HBP.Data.Experience.Protocol
{
    [DataContract, DisplayName("Min")]
    public class MinTreatment : Treatment
    {
        #region Constructors
        public MinTreatment() : base()
        {

        }
        public MinTreatment(Window window, int order, string id) : base(window, order, id)
        {
        }
        #endregion

        #region Public Methods
        public override float[] Apply(float[] values, int mainEventIndex, Frequency frequency)
        {
            int startIndex = mainEventIndex - frequency.ConvertToCeiledNumberOfSamples(Window.Start);
            int endIndex = mainEventIndex + frequency.ConvertToFlooredNumberOfSamples(Window.End);
            float min = float.MaxValue;
            for (int i = startIndex; i <= endIndex; i++)
            {
                if (min > values[i]) min = values[i];
            }
            for (int i = startIndex; i <= endIndex; i++)
            {
                values[i] = min;
            }
            return values;
        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new MinTreatment(Window, Order, ID);
        }
        #endregion
    }
}