using Tools.CSharp;
using Tools.CSharp.EEG;
using System;
using System.Runtime.Serialization;
using System.ComponentModel;

namespace HBP.Data.Experience.Protocol
{
    /// <summary>
    /// Class which define a median treatment to apply at a subBloc.
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
    [DataContract, DisplayName("Median")]
    public class MedianTreatment : Treatment
    {
        #region Constructors
        /// <summary>
        /// Create a new MedianTreatment instance with default values.
        /// </summary>
        public MedianTreatment() : base()
        {

        }
        /// <summary>
        /// Create a new MedianTreatment instance with default values and a specified unique identifier.
        /// </summary>
        /// <param name="ID">Unique identifier</param>
        public MedianTreatment(string ID) : base(ID)
        {

        }
        /// <summary>
        /// Create a new MedianTreatment instance.
        /// </summary>
        /// <param name="useOnWindow">True if we apply the treatment on the window, False otherwise</param>
        /// <param name="window">Temporal window to apply the treatment on the window of the subBloc</param>
        /// <param name="useOnBaseline">True if we apply the treatment on the baseline, False otherwise</param>
        /// <param name="baseline">Temporal window to apply the treatment on the baseline of the subBloc</param>
        /// <param name="order">Order of the treatment</param>
        /// <param name="ID">Unique identifier</param>
        public MedianTreatment(bool useOnWindow, Window window, bool useOnBaseline, Window baseline, int order, string ID) : base(useOnWindow, window, useOnBaseline, baseline, order, ID)
        {
        }
        #endregion

        #region Public Methods
        public override void Apply(ref float[] values, ref float[] baseline, int windowMainEventIndex, int baselineMainEventIndex, Frequency frequency)
        {
            float[] windowSubArray = new float[0];
            float[] baselineSubArray = new float[0];
            int startWindow = windowMainEventIndex + frequency.ConvertToCeiledNumberOfSamples(Window.Start);
            int endWindow = windowMainEventIndex + frequency.ConvertToFlooredNumberOfSamples(Window.End);
            int startBaseline = baselineMainEventIndex + frequency.ConvertToCeiledNumberOfSamples(Baseline.Start);
            int endBaseline = baselineMainEventIndex + frequency.ConvertToFlooredNumberOfSamples(Baseline.End);
            if (UseOnWindow)
            {
                windowSubArray = new float[endWindow - startWindow + 1];
                Array.Copy(values, startWindow, windowSubArray, 0, windowSubArray.Length);
            }
            if (UseOnBaseline)
            {
                baselineSubArray = new float[endBaseline - startBaseline + 1];
                Array.Copy(baseline, startBaseline, baselineSubArray, 0, baselineSubArray.Length);
            }
            float[] subArray = new float[windowSubArray.Length + baselineSubArray.Length];
            windowSubArray.CopyTo(subArray, 0);
            baselineSubArray.CopyTo(subArray, windowSubArray.Length);
            float median = subArray.Median();
            if (UseOnWindow)
            {
                for (int i = startWindow; i <= endWindow; i++) values[i] = median;
            }
            if (UseOnBaseline)
            {
                for (int i = startBaseline; i <= endBaseline; i++) baseline[i] = median;
            }
        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new MedianTreatment(UseOnWindow, Window, UseOnBaseline, Baseline, Order, ID);
        }
        #endregion
    }
}