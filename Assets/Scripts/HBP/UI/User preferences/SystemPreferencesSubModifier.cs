﻿using HBP.Data.Preferences;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.UserPreferences
{
    public class SystemPreferencesSubModifier : SubModifier<Core.Data.Preferences.SystemPreferences>
    {
        #region Properties
        [SerializeField] Toggle m_MultiThreading;
        [SerializeField] Slider m_MemorySizeSlider;
        [SerializeField] Slider m_SleepModeSlider;

        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }
            set
            {
                base.Interactable = value;

                m_MultiThreading.interactable = value;
                m_MemorySizeSlider.interactable = value;
                m_SleepModeSlider.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();

            m_MultiThreading.onValueChanged.AddListener(value => Object.MultiThreading = value);
            m_MemorySizeSlider.onValueChanged.AddListener(value => Object.MemoryCacheLimit = Mathf.FloorToInt(value));
            m_SleepModeSlider.onValueChanged.AddListener(value => Object.SleepModeAfter = Mathf.FloorToInt(value));
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(Core.Data.Preferences.SystemPreferences objectToDisplay)
        {
            base.SetFields(objectToDisplay);

            m_MemorySizeSlider.maxValue = SystemInfo.systemMemorySize;
            m_MemorySizeSlider.minValue = 0;

            m_MultiThreading.isOn = objectToDisplay.MultiThreading;
            m_MemorySizeSlider.value = objectToDisplay.MemoryCacheLimit;
            m_SleepModeSlider.value = objectToDisplay.SleepModeAfter;
        }
        #endregion
    }
}