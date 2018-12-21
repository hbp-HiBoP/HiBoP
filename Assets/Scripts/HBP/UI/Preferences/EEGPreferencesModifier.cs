using Tools.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Preferences
{
    public class EEGPreferencesModifier : MonoBehaviour
    {
        #region Properties
        [SerializeField] Dropdown m_EEGAveragingDropdown;
        [SerializeField] Dropdown m_EEGNormalizationDropdown;

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

                m_EEGAveragingDropdown.interactable = value;
                m_EEGNormalizationDropdown.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public void SetFields()
        {
            Data.Preferences.EEGPrefrences preferences = ApplicationState.UserPreferences.Data.EEG;

            m_EEGNormalizationDropdown.Set(typeof(Data.Enums.NormalizationType), (int)preferences.Normalization);
            m_EEGAveragingDropdown.Set(typeof(Data.Enums.AveragingType), (int)preferences.Averaging); 
        }
        public void Save()
        {
            Data.Preferences.EEGPrefrences preferences = ApplicationState.UserPreferences.Data.EEG;

            preferences.Normalization = (Data.Enums.NormalizationType) m_EEGNormalizationDropdown.value;
            preferences.Averaging = (Data.Enums.AveragingType) m_EEGAveragingDropdown.value;
        }
        #endregion
    }
}