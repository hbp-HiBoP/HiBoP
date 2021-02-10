using UnityEngine;

namespace HBP.UI.Module3D
{
    public class FMRIToolbar : Toolbar
    {
        #region Properties
        /// <summary>
        /// Allows to change the threshold values of the fMRI currently displayed
        /// </summary>
        [SerializeField] private Tools.ThresholdFMRI m_ThresholdFMRI;
        #endregion

        #region Private Methods
        /// <summary>
        /// Link elements to the toolbar
        /// </summary>
        protected override void AddTools()
        {
            m_Tools.Add(m_ThresholdFMRI);
        }
        #endregion
    }
}