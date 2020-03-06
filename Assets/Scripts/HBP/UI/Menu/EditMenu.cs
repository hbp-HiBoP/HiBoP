using UnityEngine;

namespace HBP.UI
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
            ApplicationState.WindowsManager.OpenModifier(ApplicationState.UserPreferences, true);
        }
        public void OpenProjectPreferences()
        {
            var modifier = ApplicationState.WindowsManager.OpenModifier(ApplicationState.ProjectLoaded.Preferences, true);
        }
        #endregion
    }

}
