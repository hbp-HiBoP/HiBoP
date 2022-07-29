using System.ComponentModel;
using System.Runtime.Serialization;
using Tools.CSharp;
using HBP.Core.Tools;

namespace HBP.Data.Experience.Protocol
{
    /// <summary>
    /// Class which define a max treatment to apply at a subBloc.
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
    [DataContract, DisplayName("Max")]
    public class MaxTreatment : Treatment
    {
        #region Constructors
        /// <summary>
        /// Create a new MaxTreatment instance with default values.
        /// </summary>
        public MaxTreatment() : base()
        {

        }
        /// <summary>
        /// Create a new MaxTreatment instance with default values and a specified unique identifier.
        /// </summary>
        /// <param name="ID">Unique identifier</param>
        public MaxTreatment(string ID) : base(ID)
        {

        }
        /// <summary>
        /// Create a new MaxTreatment instance.
        /// </summary>
        /// <param name="useOnWindow">True if we apply the treatment on the window, False otherwise</param>
        /// <param name="window">Temporal window to apply the treatment on the window of the subBloc</param>
        /// <param name="useOnBaseline">True if we apply the treatment on the baseline, False otherwise</param>
        /// <param name="baseline">Temporal window to apply the treatment on the baseline of the subBloc</param>
        /// <param name="order">Order of the treatment</param>
        /// <param name="ID">Unique identifier</param>
        public MaxTreatment(bool useOnWindow, Window window, bool useOnBaseline, Window baseline, int order, string ID) : base(useOnWindow, window, useOnBaseline, baseline, order, ID)
        {
        }
        #endregion

        #region Public Methods
        public override void Apply(ref float[] values, ref float[] baseline, int windowMainEventIndex, int baselineMainEventIndex, Frequency frequency)
        {
            int startWindow = windowMainEventIndex + frequency.ConvertToCeiledNumberOfSamples(Window.Start);
            int endWindow = windowMainEventIndex + frequency.ConvertToFlooredNumberOfSamples(Window.End);
            int startBaseline = baselineMainEventIndex + frequency.ConvertToCeiledNumberOfSamples(Baseline.Start);
            int endBaseline = baselineMainEventIndex + frequency.ConvertToFlooredNumberOfSamples(Baseline.End);
            float max = float.MinValue;
            if(UseOnWindow)
            {
                for (int i = startWindow; i <= endWindow; i++)
                {
                    if (max < values[i]) max = values[i];
                }
            }
            if (UseOnBaseline)
            {
                for (int i = startBaseline; i <= endBaseline; i++)
                {
                    if (max < baseline[i]) max = baseline[i];
                }
            }

            if(UseOnWindow)
            {
                for (int i = startWindow; i <= endWindow; i++)
                {
                    values[i] = max;
                }
            }
            if(UseOnBaseline)
            {
                for (int i = startBaseline; i <= endBaseline; i++)
                {
                    baseline[i] = max;
                }
            }
        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new MaxTreatment(UseOnWindow, Window, UseOnBaseline, Baseline, Order, ID);
        }
        #endregion
    }
}