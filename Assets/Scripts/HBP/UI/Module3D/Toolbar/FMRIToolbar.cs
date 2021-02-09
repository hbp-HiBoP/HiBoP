using UnityEngine;

namespace HBP.UI.Module3D
{
    public class FMRIToolbar : Toolbar
    {
        #region Properties
        /// <summary>
        /// Allows the selection of the fMRI to display
        /// </summary>
        [SerializeField] private Tools.FMRISelector m_FMRISelector;
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
            m_Tools.Add(m_FMRISelector);
            m_Tools.Add(m_ThresholdFMRI);
        }
        /// <summary>
        /// Add the listeners to the elements of the toolbar
        /// </summary>
        protected override void AddListeners()
        {
            base.AddListeners();

            m_FMRISelector.OnChangeFMRI.AddListener(() =>
            {
                ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
            });
        }
        #endregion
    }
}