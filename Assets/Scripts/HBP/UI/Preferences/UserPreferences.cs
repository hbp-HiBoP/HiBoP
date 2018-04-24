using System;
using UnityEngine;

namespace HBP.UI.Preferences
{
    public class UserPreferences : Window
    {
        #region Properties
        // General.
        [SerializeField] ProjectPreferencesModifier m_ProjectPreferencesModifier;
        [SerializeField] ThemePreferencesModifier m_ThemePreferencesModifier;
        [SerializeField] LocalizationPreferencesModifier m_LocalizationPreferencesModifier;
        [SerializeField] SystemPreferencesModifier m_SystemPreferencesModifier;

        // Data.
        [SerializeField] EEGPreferencesModifier m_EEGPreferencesModifier;
        [SerializeField] EventPreferencesModifier m_EventPreferencesModifier;
        [SerializeField] AnatomyPreferencesModifier m_AnatomyPreferencesModifier;

        [SerializeField] _3DPreferencesModifier m__3DPreferencesModifier;
        #endregion

        #region Public Methods
        public void Save()
        {
            Close();
        }
        public override void Close()
        {
            base.Close();
            Theme.Theme.UpdateThemeElements(ApplicationState.UserPreferences.Theme);
        }


        #endregion

        #region Private Methods
        protected override void SetWindow()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}