using System;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Preferences
{
    public class EEGPreferencesModifier : MonoBehaviour
    {
        #region Properties
        [SerializeField] Dropdown m_EEGAveragingDropdown;
        [SerializeField] Dropdown m_EEGNormalizationDropdown;
        #endregion

        #region Public Methods
        public void Set()
        {
            Data.Preferences.EEGPreferences preferences = ApplicationState.UserPreferences.Data.EEG;

            string[] normalizationType = Enum.GetNames(typeof(NormalizationType));
            m_EEGNormalizationDropdown.ClearOptions();
            foreach (string type in normalizationType)
            {
                m_EEGNormalizationDropdown.options.Add(new Dropdown.OptionData(type));
            }
            m_EEGNormalizationDropdown.value = (int)preferences.Normalization;
            m_EEGNormalizationDropdown.RefreshShownValue();

            string[] averagingType = Enum.GetNames(typeof(AveragingType));
            m_EEGAveragingDropdown.ClearOptions();
            foreach (string type in averagingType)
            {
                m_EEGAveragingDropdown.options.Add(new Dropdown.OptionData(type));
            }
            m_EEGAveragingDropdown.value = (int)preferences.Averaging;
            m_EEGAveragingDropdown.RefreshShownValue();
        }
        public void Save()
        {
            ApplicationState.UserPreferences.Data.EEG.Normalization = (NormalizationType)m_EEGNormalizationDropdown.value;
            ApplicationState.UserPreferences.Data.EEG.Averaging = (AveragingType)m_EEGAveragingDropdown.value;
        }
        #endregion
    }
}