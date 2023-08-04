﻿using HBP.Data.Module3D;
using UnityEngine;

namespace HBP.UI.Toolbar
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
        [SerializeField] private ActivityGlobal m_ActivityGlobal;
        /// <summary>
        /// IEEG Transparency parameters
        /// </summary>
        [SerializeField] private ActivityTransparency m_ActivityTransparency;
        /// <summary>
        /// Dynamic Parameters
        /// </summary>
        [SerializeField] private DynamicParameters m_DynamicParameters;
        /// <summary>
        /// FMRI Parameters
        /// </summary>
        [SerializeField] private FMRIParameters m_FMRIParameters;
        [SerializeField] private CCEPModeSelector m_CCEPModeSelector;
        [SerializeField] private CCEPSiteSourceSelector m_CCEPSiteSourceSelector;
        [SerializeField] private CCEPAreaSourceSelector m_CCEPAreaSourceSelector;
        /// <summary>
        /// Compute IEEG values
        /// </summary>
        [SerializeField] private ComputeActivity m_ComputeActivity;
        /// <summary>
        /// Compute and display site correlations
        /// </summary>
        [SerializeField] private SiteCorrelations m_SiteCorrelations;
        /// <summary>
        /// Select the MRI once the activity is computed
        /// </summary>
        [SerializeField] private FMRISelector m_FMRISelector;
        /// <summary>
        /// Select the MEG once the activity is computed
        /// </summary>
        [SerializeField] private MEGSelector m_MEGSelector;
        /// <summary>
        /// Select the Static label once the activity is computed
        /// </summary>
        [SerializeField] private StaticLabelSelector m_StaticLabelSelector;
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
            m_Tools.Add(m_CCEPModeSelector);
            m_Tools.Add(m_CCEPSiteSourceSelector);
            m_Tools.Add(m_CCEPAreaSourceSelector);
            m_Tools.Add(m_ComputeActivity);
            m_Tools.Add(m_SiteCorrelations);
            m_Tools.Add(m_FMRISelector);
            m_Tools.Add(m_MEGSelector);
            m_Tools.Add(m_StaticLabelSelector);
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
                m_ToolbarMenu.TimelineToolbar.IsGlobal = global;
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
            });
        }
        #endregion
    }
}