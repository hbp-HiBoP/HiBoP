using System.ComponentModel;
using System.Runtime.Serialization;
using Tools.CSharp;
using HBP.Core.Tools;

namespace HBP.Core.Data
{
    /// <summary>
    /// Class which define a rescale treatment to apply at a subBloc.
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
    /// <term><b>BeforeMin</b></term> 
    /// <description>Minimum Value before rescaled the values.</description>
    /// </item>
    /// <item>
    /// <term><b>BeforeMax</b></term> 
    /// <description>Maximum Value before rescaled the values.</description>
    /// </item>
    /// <item>
    /// <term><b>AfterMin</b></term> 
    /// <description>Minimum Value after rescaled the values.</description>
    /// </item>
    /// <item>
    /// <term><b>AfterMax</b></term> 
    /// <description>Maximum Value after rescaled the values.</description>
    /// </item>
    /// <item>
    /// <term><b>UseOnWindow</b></term> 
    /// <description>True if we apply the treatment on the window, False otherwise.</description>
    /// </item>
    /// <item>
    /// <term><b>Window</b></term> 
    /// <description>Temporal window to apply the treatment on the window of the subBloc.</description>
    /// </item>
    /// <item>
    /// <term><b>UseOnBaseline</b></term> 
    /// <description>True if we apply the treatment on the baseline, False otherwise.</description>
    /// </item>
    /// <item>
    /// <term><b>Baseline</b></term> 
    /// <description>Temporal window to apply the treatment on the baseline of the subBloc</description>
    /// </item>
    /// </list>
    /// </remarks>
    [DataContract, DisplayName("Threshold")]
    public class ThresholdTreatment : Treatment
    {
        #region Properties
        /// <summary>
        /// True to set a minimun treshold, False otherwise.
        /// </summary>
        [DataMember] public bool UseMinTreshold { get; set; }
        /// <summary>
        /// Minimum treshold.
        /// </summary>
        [DataMember] public float Min { get; set; }
        /// <summary>
        /// True to set a maximum treshold, False otherwise.
        /// </summary>
        [DataMember] public bool UseMaxTreshold { get; set; }
        /// <summary>
        /// Maximum treshold.
        /// </summary>
        [DataMember] public float Max { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new ThresholdTreatment instance with default values.
        /// </summary>
        public ThresholdTreatment(): base()
        {
            UseMinTreshold = false;
            UseMaxTreshold = false;
            Min = 0;
            Max = 1;
        }
        /// <summary>
        /// Create a new ThresholdTreatment instance with default values and a specified unique identifier.
        /// </summary>
        /// <param name="ID">Unique identifier</param>
        public ThresholdTreatment(string ID) : base(ID)
        {
            UseMinTreshold = false;
            UseMaxTreshold = false;
            Min = 0;
            Max = 1;
        }
        /// <summary>
        /// Create a new MedianTreatment instance.
        /// </summary>
        /// <param name="useOnWindow">True if we apply the treatment on the window, False otherwise</param>
        /// <param name="window">Temporal window to apply the treatment on the window of the subBloc</param>
        /// <param name="useOnBaseline">True if we apply the treatment on the baseline, False otherwise</param>
        /// <param name="baseline">Temporal window to apply the treatment on the baseline of the subBloc</param>
        /// <param name="useMinTreshold">True to set a minimun treshold, False otherwise</param>
        /// <param name="min">Minimum treshold</param>
        /// <param name="useMaxTreshold">True to set a maximum treshold, False otherwise</param>
        /// <param name="max">Maximum treshold</param>
        /// <param name="order">Order of the treatment</param>
        /// <param name="ID">Unique identifier</param>
        public ThresholdTreatment(bool useOnWindow, TimeWindow window, bool useOnBaseline, TimeWindow baseline, bool useMinTreshold, float min, bool useMaxTreshold, float max, int order, string ID) : base(useOnWindow, window, useOnBaseline, baseline, order, ID)
        {
            UseMinTreshold = useMinTreshold;
            Min = min;
            UseMaxTreshold = useMaxTreshold;
            Max = max;
        }
        #endregion

        #region Public Methods
        public override void Apply(ref float[] values, ref float[] baseline, int windowMainEventIndex, int baselineMainEventIndex, Frequency frequency)
        {
            if(UseOnWindow)
            {
                int startIndex = windowMainEventIndex + frequency.ConvertToCeiledNumberOfSamples(Window.Start);
                int endIndex = windowMainEventIndex + frequency.ConvertToFlooredNumberOfSamples(Window.End);
                if (UseMinTreshold && !UseMaxTreshold)
                {
                    for (int i = startIndex; i <= endIndex; i++)
                    {
                        if (values[i] > Min) values[i] = 0;
                    }
                }
                else if (!UseMinTreshold && UseMaxTreshold)
                {
                    for (int i = startIndex; i <= endIndex; i++)
                    {
                        if (values[i] < Max) values[i] = 0;
                    }
                }
                else if (UseMinTreshold && UseMaxTreshold)
                {
                    float value;
                    for (int i = startIndex; i <= endIndex; i++)
                    {
                        value = values[i];
                        if (value < Max && value > Min) values[i] = 0;
                    }
                }
            }

            if(UseOnBaseline)
            {
                int startIndex = baselineMainEventIndex + frequency.ConvertToCeiledNumberOfSamples(Baseline.Start);
                int endIndex = baselineMainEventIndex + frequency.ConvertToFlooredNumberOfSamples(Baseline.End);
                if (UseMinTreshold && !UseMaxTreshold)
                {
                    for (int i = startIndex; i <= endIndex; i++)
                    {
                        if (baseline[i] > Min) baseline[i] = 0;
                    }
                }
                else if (!UseMinTreshold && UseMaxTreshold)
                {
                    for (int i = startIndex; i <= endIndex; i++)
                    {
                        if (baseline[i] < Max) baseline[i] = 0;
                    }
                }
                else if (UseMinTreshold && UseMaxTreshold)
                {
                    float value;
                    for (int i = startIndex; i <= endIndex; i++)
                    {
                        value = baseline[i];
                        if (value < Max && value > Min) baseline[i] = 0;
                    }
                }
            }

        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new ThresholdTreatment(UseOnWindow, Window, UseOnBaseline, Baseline, UseMinTreshold, Min, UseMaxTreshold, Max, Order, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if(copy is ThresholdTreatment tresholdTreatment)
            {
                UseMinTreshold = tresholdTreatment.UseMinTreshold;
                Min = tresholdTreatment.Min;
                UseMaxTreshold = tresholdTreatment.UseMaxTreshold;
                Max = tresholdTreatment.Max;
            }
            if(copy is ClampTreatment clampTreatment)
            {
                UseMinTreshold = clampTreatment.UseMinClamp;
                Min = clampTreatment.Min;
                UseMaxTreshold = clampTreatment.UseMaxClamp;
                Max = clampTreatment.Max;
            }
        }
        #endregion
    }
}