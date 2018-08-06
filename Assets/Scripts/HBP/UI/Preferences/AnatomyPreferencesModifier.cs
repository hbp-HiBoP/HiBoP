using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Preferences
{
    public class AnatomyPreferencesModifier : MonoBehaviour
    {
        #region Properties
        [SerializeField] Toggle m_SiteNameCorrectionToggle;
        [SerializeField] Toggle m_PreloadMeshesToggle;
        [SerializeField] Toggle m_PreloadMRIsToggle;
        [SerializeField] Toggle m_PreloadImplantationsToggle;

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

                m_SiteNameCorrectionToggle.interactable = value;
                m_PreloadMeshesToggle.interactable = value;
                m_PreloadMRIsToggle.interactable = value;
                m_PreloadImplantationsToggle.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public void Save()
        {
            Data.Preferences.AnatomyPreferences preferences = ApplicationState.UserPreferences.Data.Anatomy;
            preferences.SiteNameCorrection = m_SiteNameCorrectionToggle.isOn;
            preferences.MeshPreloading = m_PreloadMeshesToggle.isOn;
            preferences.MRIPreloading = m_PreloadMRIsToggle.isOn;
            preferences.ImplantationPreloading = m_PreloadImplantationsToggle.isOn;
        }
        public void SetFields()
        {
            Data.Preferences.AnatomyPreferences preferences = ApplicationState.UserPreferences.Data.Anatomy;
            m_SiteNameCorrectionToggle.isOn = preferences.SiteNameCorrection;
            m_PreloadMeshesToggle.isOn = preferences.MeshPreloading;
            m_PreloadMRIsToggle.isOn = preferences.MRIPreloading;
            m_PreloadImplantationsToggle.isOn = preferences.ImplantationPreloading;
        }
        #endregion
    }
}

