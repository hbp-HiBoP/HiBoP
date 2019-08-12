using System.ComponentModel;
using System.Runtime.Serialization;
using Tools.CSharp;
using Tools.CSharp.EEG;

namespace HBP.Data.Experience.Protocol
{
    [DataContract, DisplayName("Offset")]
    public class OffsetTreatment : Treatment
    {
        #region Properties
        [DataMember] public float Offset { get; set; }
        #endregion

        #region Constructors
        public OffsetTreatment() : base()
        {

        }
        public OffsetTreatment(bool useOnWindow, Window window, bool useOnBaseline, Window baseline, float offset, int order, string id) : base(useOnWindow, window, useOnBaseline, baseline, order, id)
        {
            Offset = offset;
        }
        #endregion

        #region Public Methods
        public override float[] Apply(float[] values, int mainEventIndex, Frequency frequency)
        {
            int startIndex = mainEventIndex + frequency.ConvertToCeiledNumberOfSamples(Window.Start);
            int endIndex = mainEventIndex + frequency.ConvertToFlooredNumberOfSamples(Window.End);
            for (int i = startIndex; i <= endIndex; i++)
            {
                values[i] = values[i] + Offset;
            }
            return values;
        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new OffsetTreatment(UseOnWindow, Window, UseOnBaseline, Baseline, Offset, Order, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            OffsetTreatment treatment = copy as OffsetTreatment;
            Offset = treatment.Offset;
        }
        #endregion
    }
}