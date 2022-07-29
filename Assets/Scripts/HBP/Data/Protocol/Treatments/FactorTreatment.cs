using System.ComponentModel;
using System.Runtime.Serialization;
using Tools.CSharp;
using HBP.Core.Tools;

namespace HBP.Data.Experience.Protocol
{
    /// <summary>
    /// Class which define a factor treatment to apply at a subBloc.
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
    /// <item>
    /// <term><b>Factor</b></term> 
    /// <description>Factor to multiply all the values with.</description>
    /// </item>
    /// </list>
    /// </remarks>
    [DataContract, DisplayName("Factor")]
    public class FactorTreatment : Treatment
    {
        #region Properties
        /// <summary>
        /// Factor to multiply all the values with.
        /// </summary>
        [DataMember] public float Factor { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new FactorTreatment instance with default values.
        /// </summary>
        public FactorTreatment() : base()
        {
            Factor = 1;
        }
        /// <summary>
        /// Create a new FactorTreatment instance with defualt values and a specified unique identifier.
        /// </summary>
        /// <param name="ID">Unique identifier</param>
        public FactorTreatment(string ID) : base(ID)
        {
            Factor = 1;
        }
        /// <summary>
        /// Create a new FactorTreatment instance.
        /// </summary>
        /// <param name="useOnWindow">True if we apply the treatment on the window, False otherwise</param>
        /// <param name="window">Temporal window to apply the treatment on the window of the subBloc</param>
        /// <param name="useOnBaseline">True if we apply the treatment on the baseline, False otherwise</param>
        /// <param name="baseline">Temporal window to apply the treatment on the baseline of the subBloc</param>
        /// <param name="factor">Factor to multiply all the values with</param>
        /// <param name="order">Order of the treatmeants to apply to the subBloc</param>
        /// <param name="ID">Unique identifier</param>
        public FactorTreatment(bool useOnWindow, Window window, bool useOnBaseline, Window baseline, float factor, int order, string ID) : base(useOnWindow, window, useOnBaseline, baseline, order, ID)
        {
            Factor = factor;
        }
        #endregion

        #region Public Methods
        public override void Apply(ref float[] values, ref float[] baseline, int windowMainEventIndex, int baselineMainEventIndex, Frequency frequency)
        {
            if(UseOnWindow)
            {
                int startIndex = windowMainEventIndex + frequency.ConvertToCeiledNumberOfSamples(Window.Start);
                int endIndex = windowMainEventIndex + frequency.ConvertToFlooredNumberOfSamples(Window.End);
                for (int i = startIndex; i <= endIndex; i++)
                {
                    values[i] *= Factor;
                }
            }
            if (UseOnBaseline)
            {
                int startIndex = baselineMainEventIndex + frequency.ConvertToCeiledNumberOfSamples(Baseline.Start);
                int endIndex = baselineMainEventIndex + frequency.ConvertToFlooredNumberOfSamples(Baseline.End);
                for (int i = startIndex; i <= endIndex; i++)
                {
                    baseline[i] *= Factor;
                }
            }
        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new FactorTreatment(UseOnWindow, Window, UseOnBaseline, Baseline, Factor, Order, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if(copy is FactorTreatment treatment)
            {
                Factor = treatment.Factor;
            }
        }
        #endregion
    }
}