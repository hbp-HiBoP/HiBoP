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

        #region Constructors
        public ClampTreatment() : base()
        {
            UseMinClamp = false;
            UseMaxClamp = false;
            Min = 0;
            Max = 1;
        }
        public ClampTreatment(string ID) :  base(ID)
        {
            UseMinClamp = false;
            UseMaxClamp = false;
            Min = 0;
            Max = 1;
        }
        public ClampTreatment(bool useOnWindow, Window window, bool useOnBaseline, Window baseline, bool useMinClamp, float min, bool useMaxClamp, float max, int order) : base(useOnWindow, window, useOnBaseline, baseline, order)
        {
            UseMinClamp = useMinClamp;
            UseMaxClamp = useMaxClamp;
            Min = min;
            Max = max;
        }
        public ClampTreatment(bool useOnWindow, Window window, bool useOnBaseline, Window baseline, bool useMinClamp, float min , bool useMaxClamp, float max, int order, string ID) : base(useOnWindow, window, useOnBaseline, baseline, order, ID)
        {
            UseMinClamp = useMinClamp;
            UseMaxClamp = useMaxClamp;
            Min = min;
            Max = max;
        }
        #endregion

        #region Public Methods
        public override void Apply(ref float[] values, ref float[] baseline, int windowMainEventIndex, int baselineMainEventIndex, Frequency frequency)
        {
            int start, end;
            if(UseOnWindow)
            {
                start = windowMainEventIndex + frequency.ConvertToCeiledNumberOfSamples(Window.Start);
                end = windowMainEventIndex + frequency.ConvertToFlooredNumberOfSamples(Window.End);
                if (UseMinClamp && !UseMaxClamp)
                {
                    for (int i = start; i <= end; i++)
                    {
                        if (values[i] < Min) values[i] = Min;
                    }
                }
                else if (!UseMinClamp && UseMaxClamp)
                {
                    for (int i = start; i <= end; i++)
                    {
                        if (values[i] > Max) values[i] = Max;
                    }
                }
                else if (UseMinClamp && UseMaxClamp)
                {
                    for (int i = start; i <= end; i++)
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
            }

            if (UseOnBaseline)
            {
                start = baselineMainEventIndex + frequency.ConvertToCeiledNumberOfSamples(Baseline.Start);
                end = baselineMainEventIndex + frequency.ConvertToFlooredNumberOfSamples(Baseline.End);
                if (UseMinClamp && !UseMaxClamp)
                {
                    for (int i = start; i <= end; i++)
                    {
                        if (baseline[i] < Min) baseline[i] = Min;
                    }
                }
                else if (!UseMinClamp && UseMaxClamp)
                {
                    for (int i = start; i <= end; i++)
                    {
                        if (baseline[i] > Max) baseline[i] = Max;
                    }
                }
                else if (UseMinClamp && UseMaxClamp)
                {
                    for (int i = start; i <= end; i++)
                    {
                        if (baseline[i] > Max)
                        {
                            baseline[i] = Max;
                            continue;
                        }
                        if (baseline[i] < Min)
                        {
                            baseline[i] = Min;
                            continue;
                        }
                    }
                }
            }
        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new ClampTreatment(UseOnWindow, Window, UseOnBaseline, Baseline, UseMinClamp, Min, UseMaxClamp, Max, Order, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if(copy is ClampTreatment clampTreatment)
            {
                UseMinClamp = clampTreatment.UseMinClamp;
                Min = clampTreatment.Min;
                UseMaxClamp = clampTreatment.UseMaxClamp;
                Max = clampTreatment.Max;
            }
            if(copy is ThresholdTreatment tresholdTreatment)
            {
                UseMinClamp = tresholdTreatment.UseMinTreshold;
                Min = tresholdTreatment.Min;
                UseMaxClamp = tresholdTreatment.UseMaxTreshold;
                Max = tresholdTreatment.Max;
            }
        }
        #endregion
    }
}