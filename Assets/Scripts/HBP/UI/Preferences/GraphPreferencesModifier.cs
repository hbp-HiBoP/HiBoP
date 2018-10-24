using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Preferences
{
    public class GraphPreferencesModifier : MonoBehaviour
    {
        #region Properties
        [SerializeField] Toggle m_ShowCurvesOfMinimizedColumns;

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

                m_ShowCurvesOfMinimizedColumns.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public void SetFields()
        {
            Data.Preferences.GraphPreferences preferences = ApplicationState.UserPreferences.Visualization.Graph;
            m_ShowCurvesOfMinimizedColumns.isOn = preferences.ShowCurvesOfMinimizedColumns;
        }
        public void Save()
        {
            Data.Preferences.GraphPreferences preferences = ApplicationState.UserPreferences.Visualization.Graph;
            preferences.ShowCurvesOfMinimizedColumns = m_ShowCurvesOfMinimizedColumns.isOn;
        }
        #endregion
    }
}