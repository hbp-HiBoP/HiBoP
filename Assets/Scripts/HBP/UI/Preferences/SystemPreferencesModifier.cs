using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Preferences
{
    public class SystemPreferencesModifier : MonoBehaviour
    {
        #region Properties
        [SerializeField] Toggle m_MultiThreading;
        [SerializeField] Slider m_MemorySizeSlider;

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

                m_MultiThreading.interactable = value;
                m_MemorySizeSlider.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public void SetFields()
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
        #endregion
    }
}