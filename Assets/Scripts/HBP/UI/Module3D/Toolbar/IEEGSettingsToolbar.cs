using UnityEngine;

namespace HBP.UI.Module3D
{
    public class IEEGSettingsToolbar : Toolbar
    {
        #region Properties
        private bool m_IsGlobal = false;
        /// <summary>
        /// Are the changes applied with this toolbar applied to all columns at once?
        /// </summary>
        public bool IsGlobal
        {
            get
            {
                return m_IsGlobal;
            }
            set
            {
                m_IsGlobal = value;
                m_ThresholdIEEG.IsGlobal = value;
                m_IEEGTransparency.IsGlobal = value;
                m_IEEGSitesParameters.IsGlobal = value;
            }
        }
        /// <summary>
        /// IEEG Global setter
        /// </summary>
        [SerializeField] private Tools.IEEGGlobal m_IEEGGlobal;
        /// <summary>
        /// Threshold IEEG parameters
        /// </summary>
        [SerializeField] private Tools.ThresholdIEEG m_ThresholdIEEG;
        /// <summary>
        /// IEEG Transparency parameters
        /// </summary>
        [SerializeField] private Tools.IEEGTransparency m_IEEGTransparency;
        /// <summary>
        /// IEEG Sites Parameters
        /// </summary>
        [SerializeField] private Tools.IEEGSitesParameters m_IEEGSitesParameters;
        /// <summary>
        /// Compute IEEG values
        /// </summary>
        [SerializeField] private Tools.ComputeIEEG m_ComputeIEEG;
        #endregion

        #region Private Methods
        /// <summary>
        /// Link elements to the toolbar
        /// </summary>
        protected override void AddTools()
        {
            m_Tools.Add(m_IEEGGlobal);
            m_Tools.Add(m_ThresholdIEEG);
            m_Tools.Add(m_IEEGTransparency);
            m_Tools.Add(m_IEEGSitesParameters);
            m_Tools.Add(m_ComputeIEEG);
        }
        /// <summary>
        /// Add the listeners to the elements of the toolbar
        /// </summary>
        protected override void AddListeners()
        {
            base.AddListeners();

            m_IEEGGlobal.OnChangeValue.AddListener((global) =>
            {
                IsGlobal = global;
            });
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Called when showing this toolbar
        /// </summary>
        public override void ShowToolbarCallback()
        {
            m_IEEGGlobal.Set(m_ToolbarMenu.TimelineToolbar.IsGlobal);
        }
        #endregion
    }
}