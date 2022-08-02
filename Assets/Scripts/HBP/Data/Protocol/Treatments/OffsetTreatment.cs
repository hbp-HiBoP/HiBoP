using System.ComponentModel;
using System.Runtime.Serialization;
using Tools.CSharp;
using HBP.Core.Tools;

namespace HBP.Core.Data
{
    /// <summary>
    /// Class which define a offset treatment to apply at a subBloc.
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
    /// <term><b>Offset</b></term> 
    /// <description>Offset to add to the values.</description>
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
    [DataContract, DisplayName("Offset")]
    public class OffsetTreatment : Treatment
    {
        #region Properties
        /// <summary>
        /// Offset to add to the values.
        /// </summary>
        [DataMember] public float Offset { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new OffsetTreatment instance with default values.
        /// </summary>
        public OffsetTreatment() : base()
        {
            Offset = 0;
        }
        /// <summary>
        /// Create a new OffsetTreatment instance with default values and a specified unique identifier.
        /// </summary>
        /// <param name="ID">Unique identifier</param>
        public OffsetTreatment(string ID) : base(ID)
        {
            Offset = 0;
        }
        /// <summary>
        /// Create a new MedianTreatment instance.
        /// </summary>
        /// <param name="useOnWindow">True if we apply the treatment on the window, False otherwise</param>
        /// <param name="window">Temporal window to apply the treatment on the window of the subBloc</param>
        /// <param name="useOnBaseline">True if we apply the treatment on the baseline, False otherwise</param>
        /// <param name="baseline">Temporal window to apply the treatment on the baseline of the subBloc</param>
        /// <param name="offset">Offset to add to the values</param>
        /// <param name="order">Order of the treatment</param>
        /// <param name="ID">Unique identifier</param>
        public OffsetTreatment(bool useOnWindow, Window window, bool useOnBaseline, Window baseline, float offset, int order, string ID) : base(useOnWindow, window, useOnBaseline, baseline, order, ID)
        {
            Offset = offset;
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
                    values[i] += Offset;
                }
            }
            if (UseOnBaseline)
            {
                int startIndex = baselineMainEventIndex + frequency.ConvertToCeiledNumberOfSamples(Baseline.Start);
                int endIndex = baselineMainEventIndex + frequency.ConvertToFlooredNumberOfSamples(Baseline.End);
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