using System.ComponentModel;
using System.Runtime.Serialization;
using Tools.CSharp;
using HBP.Core.Tools;

namespace HBP.Core.Data
{
    /// <summary>
    /// Class which define a ClampTreatment to apply at a subBloc.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader>
    /// <term>Data</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term><b>ID</b></term>
    /// <description>Unique identifier.</description>
    /// </item>
    /// <item>
    /// <term><b>Order</b></term> 
    /// <description>Order of the treatment.</description>
    /// </item>
    /// <item>
    /// <term><b>UseOnWindow</b></term> 
    /// <description>True if  of the protocol.</description>
    /// </item>
    /// <item>
    /// <term><b>UseMinClamp</b></term> 
    /// <description>True to floor by a minimum value, False otherwise.</description>
    /// </item>
    /// <item>
    /// <term><b>Min</b></term> 
    /// <description>Minimum value to floor with.</description>
    /// </item>
    /// <item>
    /// <term><b>UseMaxClamp</b></term> 
    /// <description>True to cap by a maximum value, False otherwise.</description>
    /// </item>
    /// <item>
    /// <term><b>Max</b></term> 
    /// <description>Maximum value to cap with.</description>
    /// </item>
    /// </list>
    /// </remarks>
    [DataContract, DisplayName("Clamp")]
    public class ClampTreatment : Treatment
    {
        #region Properties
        /// <summary>
        /// True to floor by a minimum value, False otherwise.
        /// </summary>
        [DataMember] public bool UseMinClamp { get; set; }
        /// <summary>
        /// Minimum value to floor with.
        /// </summary>
        [DataMember] public float Min { get; set; }
        /// <summary>
        /// True to cap by a maximum value, False otherwise.
        /// </summary>
        [DataMember] public bool UseMaxClamp { get; set; }
        /// <summary>
        /// Maximum value to cap with.
        /// </summary>
        [DataMember] public float Max { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new ClampTreatment instance with default values.
        /// </summary>
        public ClampTreatment() : base()
        {
            UseMinClamp = false;
            UseMaxClamp = false;
            Min = 0;
            Max = 1;
        }
        /// <summary>
        /// Create a new ClampTreatment instance with a unique identifier.
        /// </summary>
        /// <param name="ID">Unique identifier</param>
        public ClampTreatment(string ID) :  base(ID)
        {
            UseMinClamp = false;
            UseMaxClamp = false;
            Min = 0;
            Max = 1;
        }
        /// <summary>
        /// Create a new ClampTreatment instance
        /// </summary>
        /// <param name="useOnWindow">True if we apply the treatment on the window, False otherwise</param>
        /// <param name="window">Temporal window to apply the treatment on the window of the subBloc</param>
        /// <param name="useOnBaseline">True if we apply the treatment on the baseline, False otherwise</param>
        /// <param name="baseline">Temporal window to apply the treatment on the baseline of the subBloc</param>
        /// <param name="useMinClamp"> True to floor by a minimum value, False otherwise.</param>
        /// <param name="min">Minimum value to floor with</param>
        /// <param name="useMaxClamp">True to cap by a maximum value, False otherwise.</param>
        /// <param name="max">Maximum value to cap with</param>
        /// <param name="order">Order of the treatmeants to apply to the subBloc</param>
        public ClampTreatment(bool useOnWindow, TimeWindow window, bool useOnBaseline, TimeWindow baseline, bool useMinClamp, float min, bool useMaxClamp, float max, int order) : base(useOnWindow, window, useOnBaseline, baseline, order)
        {
            UseMinClamp = useMinClamp;
            UseMaxClamp = useMaxClamp;
            Min = min;
            Max = max;
        }
        /// <summary>
        /// Create a new ClampTreatment instance
        /// </summary>
        /// <param name="useOnWindow">True if we apply the treatment on the window, False otherwise</param>
        /// <param name="window">Temporal window to apply the treatment on the window of the subBloc</param>
        /// <param name="useOnBaseline">True if we apply the treatment on the baseline, False otherwise</param>
        /// <param name="baseline">Temporal window to apply the treatment on the baseline of the subBloc</param>
        /// <param name="useMinClamp"> True to floor by a minimum value, False otherwise.</param>
        /// <param name="min">Minimum value to floor with</param>
        /// <param name="useMaxClamp">True to cap by a maximum value, False otherwise.</param>
        /// <param name="max">Maximum value to cap with</param>
        /// <param name="order">Order of the treatmeants to apply to the subBloc</param>
        /// <param name="ID">Unique identifier</param>
        public ClampTreatment(bool useOnWindow, TimeWindow window, bool useOnBaseline, TimeWindow baseline, bool useMinClamp, float min , bool useMaxClamp, float max, int order, string ID) : base(useOnWindow, window, useOnBaseline, baseline, order, ID)
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