using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using Tools.CSharp;
using HBP.Core.Tools;

namespace HBP.Core.Data
{
    /// <summary>
    /// Class which define a absolute treatment to apply at a subBloc.
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
    /// <description>Temporal window to apply the treatment on the baseline of the subBloc.</description>
    /// </item>
    /// </list>
    /// </remarks>
    [DataContract, DisplayName("Absolute")]
    public class AbsTreatment : Treatment
    {
        #region Public Methods
        public override void Apply(ref float[] values, ref float[] baseline, int windowMainEventIndex, int baselineMainEventIndex, Frequency frequency)
        {
            int start, end;
            if(UseOnWindow)
            {
                start = windowMainEventIndex + frequency.ConvertToCeiledNumberOfSamples(Window.Start);
                end = windowMainEventIndex + frequency.ConvertToFlooredNumberOfSamples(Window.End);
                for (int i = start; i <= end; i++)
                {
                    values[i] = Math.Abs(values[i]);
                }
            }
            if(UseOnBaseline)
            {
                start = baselineMainEventIndex + frequency.ConvertToCeiledNumberOfSamples(Baseline.Start);
                end = baselineMainEventIndex + frequency.ConvertToFlooredNumberOfSamples(Baseline.End);
                for (int i = start; i <= end; i++)
                {
                    baseline[i] = Math.Abs(baseline[i]);
                }
            }

        }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new AbsoluteTreatment instance.
        /// </summary>
        public AbsTreatment() : base()
        {

        }
        /// <summary>
        /// Create a new AbsoluteTreatment instance.
        /// </summary>
        /// <param name="ID">Unique identifier</param>
        public AbsTreatment(string ID) : base(ID)
        {

        }
        /// <summary>
        /// Create a new AbsoluteTreatment window.
        /// </summary>
        /// <param name="useOnWindow">True if we apply the treatment on the window, False otherwise</param>
        /// <param name="window">Temporal window to apply the treatment on the window of the subBloc</param>
        /// <param name="useOnBaseline">True if we apply the treatment on the baseline, False otherwise</param>
        /// <param name="baseline">Temporal window to apply the treatment on the baseline of the subBloc</param>
        /// <param name="order">Order of the treatmeants to apply to the subBloc</param>
        public AbsTreatment(bool useOnWindow, Window window, bool useOnBaseline, Window baseline, int order) : base(useOnWindow, window, useOnBaseline, baseline, order)
        {

        }
        /// <summary>
        /// Create a new AbsoluteTreatment window.
        /// </summary>
        /// <param name="useOnWindow">True if we apply the treatment on the window, False otherwise</param>
        /// <param name="window">Temporal window to apply the treatment on the window of the subBloc</param>
        /// <param name="useOnBaseline">True if we apply the treatment on the baseline, False otherwise</param>
        /// <param name="baseline">Temporal window to apply the treatment on the baseline of the subBloc</param>
        /// <param name="order">Order of the treatmeants to apply to the subBloc</param>
        /// <param name="ID">Unique identifier</param>
        public AbsTreatment(bool useOnWindow, Window window, bool useOnBaseline, Window baseline, int order, string ID) : base(useOnWindow, window, useOnBaseline, baseline, order, ID)
        {

        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new AbsTreatment(UseOnWindow, Window, UseOnBaseline, Baseline, Order, ID);
        }
        #endregion
    }
}