using Tools.Unity;
using UnityEngine;

namespace HBP.UI.Preferences
{
    public class UserPreferences : SavableWindow
    {
        #region Properties
        [SerializeField] ProjectPreferencesModifier m_ProjectPreferencesModifier;
        [SerializeField] ThemePreferencesModifier m_ThemePreferencesModifier;
        [SerializeField] LocalizationPreferencesModifier m_LocalizationPreferencesModifier;
        [SerializeField] SystemPreferencesModifier m_SystemPreferencesModifier;
        [SerializeField] EEGPreferencesModifier m_EEGPreferencesModifier;
        [SerializeField] EventPreferencesModifier m_EventPreferencesModifier;
        [SerializeField] AnatomyPreferencesModifier m_AnatomyPreferencesModifier;
        [SerializeField] _3DPreferencesModifier m_3DPreferencesModifier;
        [SerializeField] TrialMatrixPreferencesModifier m_TrialMatrixPreferencesModifier;
        [SerializeField] GraphPreferencesModifier m_GraphPreferencesModifier;
        [SerializeField] CutPreferencesModifier m_CutPreferencesModifier;
        #endregion

        #region Public Methods
        public override  void Save()
        {
            m_ProjectPreferencesModifier.Save();
            m_ThemePreferencesModifier.Save();
            m_LocalizationPreferencesModifier.Save();
            m_SystemPreferencesModifier.Save();
            m_EEGPreferencesModifier.Save();
            m_EventPreferencesModifier.Save();
            m_AnatomyPreferencesModifier.Save();
            m_3DPreferencesModifier.Save();
            m_TrialMatrixPreferencesModifier.Save();
            m_GraphPreferencesModifier.Save();
            m_CutPreferencesModifier.Save();
            ClassLoaderSaver.SaveToJSon(ApplicationState.UserPreferences, Data.Preferences.UserPreferences.PATH, true);
            base.Save();
        }
        public override void Close()
        {
            base.Close();
            Theme.Theme.UpdateThemeElements(ApplicationState.UserPreferences.Theme);
        }
        #endregion

        #region Private Methods
        protected override void Initialize()
        {
            m_ProjectPreferencesModifier.Initialize();
            m_ThemePreferencesModifier.Initialize();
            m_LocalizationPreferencesModifier.Initialize();
            m_SystemPreferencesModifier.Initialize();
            m_EEGPreferencesModifier.Initialize();
            m_EventPreferencesModifier.Initialize();
            m_AnatomyPreferencesModifier.Initialize();
            m_3DPreferencesModifier.Initialize();
            m_TrialMatrixPreferencesModifier.Initialize();
            m_GraphPreferencesModifier.Initialize();
            m_CutPreferencesModifier.Initialize();
        }
        protected override void SetInteractable(bool interactable)
        {
            m_ProjectPreferencesModifier.SetInteractable(interactable);
            m_ThemePreferencesModifier.SetInteractable(interactable);
            m_LocalizationPreferencesModifier.SetInteractable(interactable);
            m_SystemPreferencesModifier.SetInteractable(interactable);
            m_EEGPreferencesModifier.SetInteractable(interactable);
            m_EventPreferencesModifier.SetInteractable(interactable);
            m_AnatomyPreferencesModifier.SetInteractable(interactable);
            m_3DPreferencesModifier.SetInteractable(interactable);
            m_TrialMatrixPreferencesModifier.SetInteractable(interactable);
            m_GraphPreferencesModifier.SetInteractable(interactable);
            m_CutPreferencesModifier.SetInteractable(interactable);
        }
        #endregion
    }
}