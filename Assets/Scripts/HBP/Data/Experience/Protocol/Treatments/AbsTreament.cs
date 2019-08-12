using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using Tools.CSharp;
using Tools.CSharp.EEG;

namespace HBP.Data.Experience.Protocol
{
    [DataContract, DisplayName("Absolute")]
    public class AbsTreatment : Treatment
    {
        #region Public Methods
        public override float[] Apply(float[] values, int mainEventIndex, Frequency frequency)
        {
            int startIndex = mainEventIndex + frequency.ConvertToCeiledNumberOfSamples(Window.Start);
            int endIndex = mainEventIndex + frequency.ConvertToFlooredNumberOfSamples(Window.End);
            for (int i = startIndex; i <= endIndex; i++)
            {
                values[i] = Math.Abs(values[i]);
            }
            return values;
        }
        #endregion

        #region Constructors
        public AbsTreatment() : base()
        {

        }
        public AbsTreatment(Window window, int order, string id) : base(window, order, id)
        {

        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new AbsTreatment(Window, Order, ID);
        }
        #endregion
    }
}