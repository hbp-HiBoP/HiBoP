using UnityEngine;
using HBP.UI.Tools;
using HBP.Core.Tools;
using HBP.Data.Preferences;

namespace HBP.UI.Main
{
    public class EditMenu : Menu
    {
        #region Properties
        [SerializeField] InteractableConditions m_PreferencesInteractableConditions;
        public InteractableConditions PreferencesInteractableConditions { get { return m_PreferencesInteractableConditions; } }
        [SerializeField] InteractableConditions m_ProjectPreferencesInteractableConditions;
        public InteractableConditions ProjectPreferencesInteractableConditions { get { return m_ProjectPreferencesInteractableConditions; } }
        #endregion

        #region Public Methods
        public void OpenPreferences()
        {
            WindowsManager.OpenModifier(PreferencesManager.UserPreferences);
        }
        public void OpenProjectPreferences()
        {
            var modifier = WindowsManager.OpenModifier(ApplicationState.ProjectLoaded.Preferences);
        }
        #endregion
    }

}
