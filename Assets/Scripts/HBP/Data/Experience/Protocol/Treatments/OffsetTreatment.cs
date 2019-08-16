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
            Offset = 0;
        }
        public OffsetTreatment(bool useOnWindow, Window window, bool useOnBaseline, Window baseline, float offset, int order, string id) : base(useOnWindow, window, useOnBaseline, baseline, order, id)
        {
            Offset = offset;
        }
        #endregion

        #region Public Methods
        public override void Apply(ref float[] values, ref float[] baseline, int mainEventIndex, Frequency frequency)
        {
            if(UseOnWindow)
            {
                int startIndex = mainEventIndex + frequency.ConvertToCeiledNumberOfSamples(Window.Start);
                int endIndex = mainEventIndex + frequency.ConvertToFlooredNumberOfSamples(Window.End);
                for (int i = startIndex; i <= endIndex; i++)
                {
                    values[i] += Offset;
                }
            }
            if (UseOnBaseline)
            {
                int startIndex = mainEventIndex + frequency.ConvertToCeiledNumberOfSamples(Baseline.Start);
                int endIndex = mainEventIndex + frequency.ConvertToFlooredNumberOfSamples(Baseline.End);
                for (int i = startIndex; i <= endIndex; i++)
                {
                    baseline[i] += Offset;
                }
            }
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
            if(copy is OffsetTreatment treatment)
            {
                Offset = treatment.Offset;
            }
        }
        #endregion
    }
}