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
        [SerializeField] ProtocolPreferencesModifier m_EventPreferencesModifier;
        [SerializeField] AnatomyPreferencesModifier m_AnatomyPreferencesModifier;
        [SerializeField] _3DPreferencesModifier m_3DPreferencesModifier;
        [SerializeField] TrialMatrixPreferencesModifier m_TrialMatrixPreferencesModifier;
        [SerializeField] GraphPreferencesModifier m_GraphPreferencesModifier;
        [SerializeField] CutPreferencesModifier m_CutPreferencesModifier;

        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }
            set
            {
                base.Interactable = value;

                m_ProjectPreferencesModifier.Interactable = value;
                m_ThemePreferencesModifier.Interactable = value;
                m_LocalizationPreferencesModifier.Interactable = value;
                m_SystemPreferencesModifier.Interactable = value;
                m_EEGPreferencesModifier.Interactable = value;
                m_EventPreferencesModifier.Interactable = value;
                m_AnatomyPreferencesModifier.Interactable = value;
                m_3DPreferencesModifier.Interactable = value;
                m_TrialMatrixPreferencesModifier.Interactable = value;
                m_GraphPreferencesModifier.Interactable = value;
                m_CutPreferencesModifier.Interactable = value;
            }
        }
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
            Theme.Theme.UpdateThemeElements(ApplicationState.UserPreferences.Theme);
            base.Close();
        }
        #endregion

        #region Private Methods
        protected override void SetFields()
        {
            m_ProjectPreferencesModifier.SetFields();
            m_ThemePreferencesModifier.SetFields();
            m_LocalizationPreferencesModifier.SetFields();
            m_SystemPreferencesModifier.SetFields();
            m_EEGPreferencesModifier.SetFields();
            m_EventPreferencesModifier.SetFields();
            m_AnatomyPreferencesModifier.SetFields();
            m_3DPreferencesModifier.SetFields();
            m_TrialMatrixPreferencesModifier.SetFields();
            m_GraphPreferencesModifier.SetFields();
            m_CutPreferencesModifier.SetFields();
        }
        #endregion
    }
}