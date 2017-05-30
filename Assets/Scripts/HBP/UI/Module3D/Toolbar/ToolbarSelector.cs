using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public class ToolbarSelector : MonoBehaviour
    {

        #region Properties
        [SerializeField, Candlelight.PropertyBackingField]
        private ToolbarMenu m_ToolbarMenu;
        /// <summary>
        /// Toolbar menu
        /// </summary>
        public ToolbarMenu ToolbarMenu
        {
            get
            {
                return m_ToolbarMenu;
            }
            set
            {
                m_ToolbarMenu = value;
            }
        }

        [SerializeField, Candlelight.PropertyBackingField]
        private Toggle m_SceneToggle;
        /// <summary>
        /// Scene toggle
        /// </summary>
        public Toggle SceneToggle
        {
            get
            {
                return m_SceneToggle;
            }
            set
            {
                m_SceneToggle = value;
            }
        }

        [SerializeField, Candlelight.PropertyBackingField]
        private Toggle m_DisplayToggle;
        /// <summary>
        /// Display toggle
        /// </summary>
        public Toggle DisplayToggle
        {
            get
            {
                return m_DisplayToggle;
            }
            set
            {
                m_DisplayToggle = value;
            }
        }

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
                else
                {
                    // todo
                }
            });
            m_DisplayToggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    ChangeToolbar(m_DisplayToggle);
                }
                else
                {
                    // todo
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

        #region Public Methods

        #endregion
    }
}