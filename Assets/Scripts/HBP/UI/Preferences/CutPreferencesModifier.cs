using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Preferences
{
    public class CutPreferencesModifier : MonoBehaviour
    {
        #region Properties
        [SerializeField] Toggle m_ShowCutLinesToggle;
        [SerializeField] Toggle m_SimplifiedMeshesToggle;

        protected bool m_Interactable;
        public virtual bool Interactable
        {
            get
            {
                return m_Interactable;
            }
            set
            {
                m_Interactable = value;

                m_ShowCutLinesToggle.interactable = value;
                m_SimplifiedMeshesToggle.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public void SetFields()
        {
            Data.Preferences.CutPreferences cutPreferences = ApplicationState.UserPreferences.Visualization.Cut;

            m_ShowCutLinesToggle.isOn = cutPreferences.ShowCutLines;
            m_SimplifiedMeshesToggle.isOn = cutPreferences.SimplifiedMeshes;
        }
        public void Save()
        {
            Data.Preferences.CutPreferences cutPreferences = ApplicationState.UserPreferences.Visualization.Cut;

            cutPreferences.ShowCutLines = m_ShowCutLinesToggle.isOn;
            cutPreferences.SimplifiedMeshes = m_SimplifiedMeshesToggle.isOn;
        }
        #endregion
    }
}