using UnityEngine;

namespace HBP.UI.Module3D
{
    public class ActivitySettingsToolbar : Toolbar
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
                m_ActivityTransparency.IsGlobal = value;
                m_DynamicParameters.IsGlobal = value;
                m_FMRIParameters.IsGlobal = value;
            }
        }
        /// <summary>
        /// Activity Global setter
        /// </summary>
        [SerializeField] private Tools.ActivityGlobal m_ActivityGlobal;
        /// <summary>
        /// IEEG Transparency parameters
        /// </summary>
        [SerializeField] private Tools.ActivityTransparency m_ActivityTransparency;
        /// <summary>
        /// Dynamic Parameters
        /// </summary>
        [SerializeField] private Tools.DynamicParameters m_DynamicParameters;
        /// <summary>
        /// FMRI Parameters
        /// </summary>
        [SerializeField] private Tools.FMRIParameters m_FMRIParameters;
        /// <summary>
        /// Compute IEEG values
        /// </summary>
        [SerializeField] private Tools.ComputeActivity m_ComputeActivity;
        /// <summary>
        /// Compute and display site correlations
        /// </summary>
        [SerializeField] private Tools.SiteCorrelations m_SiteCorrelations;
        #endregion

        #region Private Methods
        /// <summary>
        /// Link elements to the toolbar
        /// </summary>
        protected override void AddTools()
        {
            m_Tools.Add(m_ActivityGlobal);
            m_Tools.Add(m_ActivityTransparency);
            m_Tools.Add(m_DynamicParameters);
            m_Tools.Add(m_FMRIParameters);
            m_Tools.Add(m_ComputeActivity);
            m_Tools.Add(m_SiteCorrelations);
        }
        /// <summary>
        /// Add the listeners to the elements of the toolbar
        /// </summary>
        protected override void AddListeners()
        {
            base.AddListeners();

            m_ActivityGlobal.OnChangeValue.AddListener((global) =>
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
            m_ActivityGlobal.Set(m_ToolbarMenu.TimelineToolbar.IsGlobal);
        }
        #endregion
    }
}