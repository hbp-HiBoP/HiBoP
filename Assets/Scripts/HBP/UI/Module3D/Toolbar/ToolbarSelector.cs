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
        /// Display toggle
        /// </summary>
        [SerializeField]
        private Toggle m_IEEGToggle;

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

            m_Toolbars.Add(m_SceneToggle, m_ToolbarMenu.SceneSettingsToolbar);
            m_Toolbars.Add(m_DisplayToggle, m_ToolbarMenu.DisplaySettingsToolbar);
            m_Toolbars.Add(m_IEEGToggle, m_ToolbarMenu.IEEGSettingsToolbar);

            AddListeners();
        }
        /// <summary>
        /// Add the listeners to the toggles (to change the toolbar)
        /// </summary>
        private void AddListeners()
        {
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
        }
        /// <summary>
        /// Method to be called when changing the state of a toggle
        /// </summary>
        /// <param name="triggeredToggle">Toggle which value changed</param>
        private void ChangeToolbar(Toggle triggeredToggle)
        {
            Toolbar newlyActivatedToolbar = m_Toolbars[triggeredToggle];
            m_ToolbarMenu.CurrentToolbar.gameObject.SetActive(false);
            newlyActivatedToolbar.gameObject.SetActive(true);
            m_ToolbarMenu.CurrentToolbar = newlyActivatedToolbar;
        }
        #endregion
    }
}