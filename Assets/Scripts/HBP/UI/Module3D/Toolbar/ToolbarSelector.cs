using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public class ToolbarSelector : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// Toolbar menu
        /// </summary>
        [SerializeField]
        private ToolbarMenu m_ToolbarMenu;

        /// <summary>
        /// Scene configuration toggle
        /// </summary>
        [SerializeField]
        private Toggle m_ConfigurationToggle;

        /// <summary>
        /// Scene toggle
        /// </summary>
        [SerializeField]
        private Toggle m_SceneToggle;

        /// <summary>
        /// Display toggle
        /// </summary>
        [SerializeField]
        private Toggle m_DisplayToggle;

        /// <summary>
        /// IEEG toggle
        /// </summary>
        [SerializeField]
        private Toggle m_IEEGToggle;

        /// <summary>
        /// Timeline toggle
        /// </summary>
        [SerializeField]
        private Toggle m_TimelineToggle;

        /// <summary>
        /// Site toggle
        /// </summary>
        [SerializeField]
        private Toggle m_SiteToggle;

        /// <summary>
        /// CCEP toggle
        /// </summary>
        [SerializeField]
        private Toggle m_CCEPToggle;

        /// <summary>
        /// IBC toggle
        /// </summary>
        [SerializeField]
        private Toggle m_IBCToggle;

        /// <summary>
        /// ROI Toggle
        /// </summary>
        [SerializeField]
        private Toggle m_ROIToggle;

        /// <summary>
        /// Triangle erasing Toggle
        /// </summary>
        [SerializeField]
        private Toggle m_TriangleToggle;

        /// <summary>
        /// FMRI Toggle
        /// </summary>
        [SerializeField]
        private Toggle m_FMRIToggle;

        /// <summary>
        /// Toggle group associated to the left menu toggles
        /// </summary>
        private ToggleGroup m_ToggleGroup;

        /// <summary>
        /// Link toggle to its respective toolbar
        /// </summary>
        private Dictionary<Toggle, Toolbar> m_Toolbars = new Dictionary<Toggle, Toolbar>();
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_ToggleGroup = GetComponent<ToggleGroup>();

            m_Toolbars.Add(m_ConfigurationToggle, m_ToolbarMenu.ConfigurationToolbar);
            m_Toolbars.Add(m_SceneToggle, m_ToolbarMenu.SceneSettingsToolbar);
            m_Toolbars.Add(m_DisplayToggle, m_ToolbarMenu.DisplaySettingsToolbar);
            m_Toolbars.Add(m_IEEGToggle, m_ToolbarMenu.IEEGSettingsToolbar);
            m_Toolbars.Add(m_TimelineToggle, m_ToolbarMenu.TimelineToolbar);
            m_Toolbars.Add(m_SiteToggle, m_ToolbarMenu.SiteToolbar);
            m_Toolbars.Add(m_ROIToggle, m_ToolbarMenu.ROIToolbar);
            m_Toolbars.Add(m_TriangleToggle, m_ToolbarMenu.TriangleToolbar);
            m_Toolbars.Add(m_CCEPToggle, m_ToolbarMenu.CCEPToolbar);
            m_Toolbars.Add(m_IBCToggle, m_ToolbarMenu.IBCToolbar);
            m_Toolbars.Add(m_FMRIToggle, m_ToolbarMenu.FMRIToolbar);

            AddListeners();
        }
        /// <summary>
        /// Add the listeners to the toggles (to change the toolbar)
        /// </summary>
        private void AddListeners()
        {
            m_ConfigurationToggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    ChangeToolbar(m_ConfigurationToggle);
                }
            });
            m_SceneToggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    ChangeToolbar(m_SceneToggle);
                }
            });
            m_DisplayToggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    ChangeToolbar(m_DisplayToggle);
                }
            });
            m_IEEGToggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    ChangeToolbar(m_IEEGToggle);
                }
            });
            m_TimelineToggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    ChangeToolbar(m_TimelineToggle);
                }
            });
            m_SiteToggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    ChangeToolbar(m_SiteToggle);
                }
            });
            m_ROIToggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    ChangeToolbar(m_ROIToggle);
                }
            });
            m_TriangleToggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    ChangeToolbar(m_TriangleToggle);
                }
            });
            m_CCEPToggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    ChangeToolbar(m_CCEPToggle);
                }
            });
            m_IBCToggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    ChangeToolbar(m_IBCToggle);
                }
            });
            m_FMRIToggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    ChangeToolbar(m_FMRIToggle);
                }
            });
        }
        /// <summary>
        /// Method to be called when changing the state of a toggle
        /// </summary>
        /// <param name="triggeredToggle">Toggle which value changed</param>
        private void ChangeToolbar(Toggle triggeredToggle)
        {
            m_ToolbarMenu.CurrentToolbar.HideToolbarCallback();
            m_ToolbarMenu.CurrentToolbar.gameObject.SetActive(false);
            m_ToolbarMenu.CurrentToolbar = m_Toolbars[triggeredToggle];
            m_ToolbarMenu.CurrentToolbar.gameObject.SetActive(true);
            m_ToolbarMenu.CurrentToolbar.ShowToolbarCallback();
        }
        #endregion
    }
}