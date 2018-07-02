using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Preferences
{
    public class GraphPreferencesModifier : MonoBehaviour
    {
        #region Properties
        [SerializeField] Toggle m_ShowCurvesOfMinimizedColumns;
        #endregion

        #region Public Methods
        public void Initialize()
        {
            Data.Preferences.GraphPreferences graphPreferences = ApplicationState.UserPreferences.Visualization.Graph;
            m_ShowCurvesOfMinimizedColumns.isOn = graphPreferences.ShowCurvesOfMinimizedColumns;
        }
        public void Save()
        {
            Data.Preferences.GraphPreferences graphPreferences = ApplicationState.UserPreferences.Visualization.Graph;
            graphPreferences.ShowCurvesOfMinimizedColumns = m_ShowCurvesOfMinimizedColumns.isOn;
        }
        public void SetInteractable(bool interactable)
        {
            m_ShowCurvesOfMinimizedColumns.interactable = interactable;
        }
        #endregion
    }
}