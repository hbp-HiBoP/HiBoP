using System.ComponentModel;
using System.Runtime.Serialization;
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
    /// <description>Minimun Value before rescaled the values.</description>
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
    [DataContract, DisplayName("Rescale")]
    public class RescaleTreatment : Treatment
    {
        #region Properties
        /// <summary>
        /// Minimum Value before rescaled the values.
        /// </summary>
        [DataMember] public float BeforeMin { get; set; }
        /// <summary>
        /// Maximum Value before rescaled the values.
        /// </summary>
        [DataMember] public float BeforeMax { get; set; }
        /// <summary>
        /// Minimum Value after rescaled the values.
        /// </summary>
        [DataMember] public float AfterMin { get; set; }
        /// <summary>
        /// Maximum Value after rescaled the values.
        /// </summary>
        [DataMember] public float AfterMax { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new RescaleTreatment instance with default values.
        /// </summary>
        public RescaleTreatment() : base()
        {
            BeforeMin = 80;
            BeforeMax = 120;
            AfterMin = -1;
            AfterMax = 1;
        }
        /// <summary>
        /// Create a new RescaleTreatment instance with default values and a specified unique identifier.
        /// </summary>
        /// <param name="ID">Unique identifier</param>
        public RescaleTreatment(string ID) : base(ID)
        {
            BeforeMin = 80;
            BeforeMax = 120;
            AfterMin = -1;
            AfterMax = 1;
        }
        /// <summary>
        /// Create a new RescaleTreatment instance.
        /// </summary>
        /// <param name="useOnWindow">True if we apply the treatment on the window, False otherwise</param>
        /// <param name="window">Temporal window to apply the treatment on the window of the subBloc</param>
        /// <param name="useOnBaseline">True if we apply the treatment on the baseline, False otherwise</param>
        /// <param name="baseline">Temporal window to apply the treatment on the baseline of the subBloc</param>
        /// <param name="beforeMin">Minimum Value before rescaled the values</param>
        /// <param name="beforeMax">Maximum Value before rescaled the values</param>
        /// <param name="afterMin">Minimum Value after rescaled the values</param>
        /// <param name="afterMax">Maximum Value after rescaled the values</param>
        /// <param name="order">Order of the treatment</param>
        /// <param name="ID">Unique identifier</param>
        public RescaleTreatment(bool useOnWindow, TimeWindow window, bool useOnBaseline, TimeWindow baseline, float beforeMin, float beforeMax, float afterMin, float afterMax, int order, string ID) : base(useOnWindow, window, useOnBaseline, baseline, order, ID)
        {
            BeforeMin = beforeMin;
            BeforeMax = beforeMax;
            AfterMin = afterMin;
            AfterMax = afterMax;
        }
        #endregion

        #region Public Methods
        public override void Apply(ref float[] values, ref float[] baseline, int windowMainEventIndex, int baselineMainEventIndex, Frequency frequency)
        {
            float ratio = (AfterMax - AfterMin) / (BeforeMax - BeforeMin);
            if (UseOnWindow)
            {
                int startIndex = windowMainEventIndex + frequency.ConvertToCeiledNumberOfSamples(Window.Start);
                int endIndex = windowMainEventIndex + frequency.ConvertToFlooredNumberOfSamples(Window.End);
                for (int i = startIndex; i <= endIndex; i++)
                {
                    values[i] = ratio * (values[i] - BeforeMin) + AfterMin;
                }
            }
            if(UseOnBaseline)
            {
                int startIndex = baselineMainEventIndex + frequency.ConvertToCeiledNumberOfSamples(Baseline.Start);
                int endIndex = baselineMainEventIndex + frequency.ConvertToFlooredNumberOfSamples(Baseline.End);
                for (int i = startIndex; i <= endIndex; i++)
                {
                    baseline[i] = ratio * (baseline[i] - BeforeMin) + AfterMin;
                }
            }
        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new RescaleTreatment(UseOnWindow, Window, UseOnBaseline, Baseline, BeforeMin, BeforeMax, AfterMin, AfterMax, Order, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if(copy is RescaleTreatment treatment)
            {
                BeforeMin = treatment.BeforeMin;
                BeforeMax = treatment.BeforeMax;
                AfterMin = treatment.AfterMin;
                AfterMax = treatment.AfterMax;
            }
        }
        #endregion
    }
}