using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Preferences
{
    public class SystemPreferencesModifier : MonoBehaviour
    {
        #region Properties
        [SerializeField] Toggle m_MultiThreading;
        #endregion

        #region Public Methods
        public void Initialize()
        {
            Data.Preferences.SystemPreferences preferences = ApplicationState.UserPreferences.General.System;
            m_MultiThreading.isOn = preferences.MultiThreading;
        }
        public void Save()
        {
            Data.Preferences.SystemPreferences preferences = ApplicationState.UserPreferences.General.System;
            preferences.MultiThreading = m_MultiThreading.isOn;
        }
        public void SetInteractable(bool interactable)
        {
            m_MultiThreading.interactable = interactable;
        }
        #endregion
    }
}