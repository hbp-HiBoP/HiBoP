using HBP.Core.Tools;
using System.ComponentModel;

namespace HBP.Core.Data
{
    /// <summary>
    /// Class which define a mean treatment to apply at a subBloc.
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
    [DisplayName("Mean")]
    public class MeanTreatment : Treatment
    {
        public override TreatmentExecutionKind ExecutionKind => TreatmentExecutionKind.Scalar;
        #region Constructors
        /// <summary>
        /// Create a new MeanTreatment instance with default values.
        /// </summary>
        public MeanTreatment() : base()
        {

        }
        /// <summary>
        /// Create a new MeanTreatment instance with default values with a specified unique identifier.
        /// </summary>
        public MeanTreatment(string ID) : base(ID)
        {

        }
        /// <summary>
        /// Create a new MeanTreatment instance.
        /// </summary>
        /// <param name="useOnWindow">True if we apply the treatment on the window, False otherwise</param>
        /// <param name="window">Temporal window to apply the treatment on the window of the subBloc</param>
        /// <param name="useOnBaseline">True if we apply the treatment on the baseline, False otherwise</param>
        /// <param name="baseline">Temporal window to apply the treatment on the baseline of the subBloc</param>
        /// <param name="order">Order of the treatment</param>
        /// <param name="ID">Unique identifier</param>
        public MeanTreatment(bool useOnWindow, TimeWindow window, bool useOnBaseline, TimeWindow baseline, int order, string ID) : base(useOnWindow, window, useOnBaseline, baseline, order, ID)
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
            double sum = 0;
            int count = 0;
            if (UseOnWindow)
            {
                for (int i = startWindow; i <= endWindow; ++i)
                {
                    sum += values[i];
                    count++;
                }
            }
            if (UseOnBaseline)
            {
                for (int i = startBaseline; i <= endBaseline; ++i)
                {
                    sum += baseline[i];
                    count++;
                }
            }
            if (count == 0)
                throw new System.Exception("Array is empty");

            float mean = (float)(sum / count);
            if(UseOnWindow)
            {
                for (int i = startWindow; i <= endWindow; i++) values[i] = mean;
            }
            if(UseOnBaseline)
            {
                for (int i = startBaseline; i <= endBaseline; i++) baseline[i] = mean;
            }
        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new MeanTreatment(UseOnWindow, Window, UseOnBaseline, Baseline, Order, ID);
        }
        #endregion
    }
}
