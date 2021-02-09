using Tools.CSharp;
using UnityEngine;
using UnityEngine.Events;

namespace HBP.Module3D
{
    /// <summary>
    /// Class containing the parameters for the activity of a FMRI column
    /// </summary>
    public class FMRIDataParameters
    {
        #region Properties
        /// <summary>
        /// Calibration min factor of the FMRI (between 0 and 1)
        /// </summary>
        public float FMRINegativeCalMinFactor { get; private set; } = 0.05f;
        /// <summary>
        /// Calibration min factor of the FMRI (between 0 and 1)
        /// </summary>
        public float FMRINegativeCalMaxFactor { get; private set; } = 0.5f;
        /// <summary>
        /// Calibration max factor of the FMRI (between 0 and 1)
        /// </summary>
        public float FMRIPositiveCalMinFactor { get; private set; } = 0.05f;
        /// <summary>
        /// Calibration max factor of the FMRI (between 0 and 1)
        /// </summary>
        public float FMRIPositiveCalMaxFactor { get; private set; } = 0.5f;
        #endregion

        #region Events
        /// <summary>
        /// Event called when updating the span values (min, mid or max)
        /// </summary>
        public UnityEvent OnUpdateCalValues = new UnityEvent();
        #endregion

        #region Public Methods
        /// <summary>
        /// Set the span values all together
        /// </summary>
        /// <param name="min">Span min value</param>
        /// <param name="mid">Middle value</param>
        /// <param name="max">Span max value</param>
        public void SetSpanValues(float negativeMin, float negativeMax, float positiveMin, float positiveMax)
        {
            if (Mathf.Approximately(negativeMin, 0f) && Mathf.Approximately(negativeMax, 0f) && Mathf.Approximately(positiveMin, 0f) && Mathf.Approximately(positiveMax, 0f)) return;

            if (negativeMin > negativeMax) negativeMin = negativeMax;
            if (positiveMin > positiveMax) positiveMin = positiveMax;
            FMRINegativeCalMinFactor = negativeMin;
            FMRINegativeCalMaxFactor = negativeMax;
            FMRIPositiveCalMinFactor = positiveMin;
            FMRIPositiveCalMaxFactor = positiveMax;
            OnUpdateCalValues.Invoke();
        }
        /// <summary>
        /// Reset span values to their default values
        /// </summary>
        /// <param name="column">Column associated with this class</param>
        public void ResetSpanValues()
        {
            FMRINegativeCalMinFactor = 0.05f;
            FMRINegativeCalMaxFactor = 0.5f;
            FMRIPositiveCalMinFactor = 0.05f;
            FMRIPositiveCalMaxFactor = 0.5f;
            OnUpdateCalValues.Invoke();
        }
        #endregion
    }
}