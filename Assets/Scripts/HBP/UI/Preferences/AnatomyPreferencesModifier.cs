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
        #endregion

        #region Public Methods
        public void Save()
        {
            ApplicationState.UserPreferences.Data.Anatomy.SiteNameCorrection = m_SiteNameCorrectionToggle.isOn;
            ApplicationState.UserPreferences.Data.Anatomy.MeshPreloading = m_PreloadMeshesToggle.isOn;
            ApplicationState.UserPreferences.Data.Anatomy.MRIPreloading = m_PreloadMRIsToggle.isOn;
            ApplicationState.UserPreferences.Data.Anatomy.ImplantationPreloading = m_PreloadImplantationsToggle.isOn;
        }
        public void Initialize()
        {
            m_SiteNameCorrectionToggle.isOn = ApplicationState.UserPreferences.Data.Anatomy.SiteNameCorrection;
            m_PreloadMeshesToggle.isOn = ApplicationState.UserPreferences.Data.Anatomy.MeshPreloading;
            m_PreloadMRIsToggle.isOn = ApplicationState.UserPreferences.Data.Anatomy.MRIPreloading;
            m_PreloadImplantationsToggle.isOn = ApplicationState.UserPreferences.Data.Anatomy.ImplantationPreloading;
        }
        public void SetInteractable(bool interactable)
        {
            m_SiteNameCorrectionToggle.interactable = interactable;
            m_PreloadMeshesToggle.interactable = interactable;
            m_PreloadMRIsToggle.interactable = interactable;
            m_PreloadImplantationsToggle.interactable = interactable;
        }
        #endregion
    }
}

