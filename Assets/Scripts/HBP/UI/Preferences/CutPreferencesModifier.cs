using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Preferences
{
    public class CutPreferencesModifier : MonoBehaviour
    {
        #region Properties
        [SerializeField] Toggle m_ShowCutLinesToggle;
        [SerializeField] Toggle m_SimplifiedMeshesToggle;
        #endregion

        #region Public Methods
        public void Initialize()
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
        public void SetInteractable(bool interactable)
        {
            m_ShowCutLinesToggle.interactable = interactable;
            m_SimplifiedMeshesToggle.interactable = interactable;
        }
        #endregion
    }
}