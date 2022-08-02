using System.ComponentModel;
using System.Runtime.Serialization;
using Tools.CSharp;
using HBP.Core.Tools;

namespace HBP.Core.Data
{
    /// <summary>
    /// Class which define a min treatment to apply at a subBloc.
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
    [DataContract, DisplayName("Min")]
    public class MinTreatment : Treatment
    {
        #region Constructors
        /// <summary>
        /// Create a new MinTreatment instance with default values.
        /// </summary>
        public MinTreatment() : base()
        {

        }
        /// <summary>
        /// Create a new MinTreatment instance with default values and a specified unique identifier.
        /// </summary>
        /// <param name="ID">Unique identifier</param>
        public MinTreatment(string ID) : base(ID)
        {

        }
        /// <summary>
        /// Create a new MinTreatment instance.
        /// </summary>
        /// <param name="useOnWindow">True if we apply the treatment on the window, False otherwise</param>
        /// <param name="window">Temporal window to apply the treatment on the window of the subBloc</param>
        /// <param name="useOnBaseline">True if we apply the treatment on the baseline, False otherwise</param>
        /// <param name="baseline">Temporal window to apply the treatment on the baseline of the subBloc</param>
        /// <param name="order">Order of the treatment</param>
        /// <param name="ID">Unique identifier</param>
        public MinTreatment(bool useOnWindow, Window window, bool useOnBaseline, Window baseline, int order, string ID) : base(useOnWindow, window, useOnBaseline, baseline, order, ID)
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
            float min = float.MaxValue;
            if (UseOnWindow)
            {
                for (int i = startWindow; i <= endWindow; i++)
                {
                    if (min > values[i]) min = values[i];
                }
            }
            if (UseOnBaseline)
            {
                for (int i = startBaseline; i <= endBaseline; i++)
                {
                    if (min > baseline[i]) min = baseline[i];
                }
            }

            if (UseOnWindow)
            {
                for (int i = startWindow; i <= endWindow; i++)
                {
                    values[i] = min;
                }
            }
            if (UseOnBaseline)
            {
                for (int i = startBaseline; i <= endBaseline; i++)
                {
                    baseline[i] = min;
                }
            }
        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new MinTreatment(UseOnWindow, Window, UseOnBaseline, Baseline, Order, ID);
        }
        #endregion
    }
}