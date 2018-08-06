using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Preferences
{
    public class SystemPreferencesModifier : MonoBehaviour
    {
        #region Properties
        [SerializeField] Toggle m_MultiThreading;
        [SerializeField] Slider m_MemorySizeSlider;
        #endregion

        #region Public Methods
        public void Initialize()
        {
            Data.Preferences.SystemPreferences preferences = ApplicationState.UserPreferences.General.System;
            m_MultiThreading.isOn = preferences.MultiThreading;
            m_MemorySizeSlider.maxValue = SystemInfo.systemMemorySize;
            m_MemorySizeSlider.minValue = 0;
            m_MemorySizeSlider.value = preferences.MemoryCacheLimit;
        }
        public void Save()
        {
            Data.Preferences.SystemPreferences preferences = ApplicationState.UserPreferences.General.System;
            preferences.MultiThreading = m_MultiThreading.isOn;
            preferences.MemoryCacheLimit = Mathf.FloorToInt(m_MemorySizeSlider.value);
        }
        public void SetInteractable(bool interactable)
        {
            m_MultiThreading.interactable = interactable;
            m_MemorySizeSlider.interactable = interactable;
        }
        #endregion
    }
}