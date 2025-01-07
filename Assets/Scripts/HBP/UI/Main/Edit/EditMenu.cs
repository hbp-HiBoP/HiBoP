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
        [SerializeField] InteractableConditions m_TagsManagerInteractableConditions;
        public InteractableConditions TagsManagerInteractableConditions { get { return m_TagsManagerInteractableConditions; } }
        #endregion

        #region Public Methods
        public void OpenPreferences()
        {
            WindowsManager.OpenModifier(PersistentDataManager.UserPreferences);
        }
        public void OpenProjectPreferences()
        {
            WindowsManager.OpenModifier(ApplicationState.ProjectLoaded.Preferences);
        }
        public void OpenTagsManager()
        {
            WindowsManager.OpenModifier(PersistentDataManager.Tags);
        }
        #endregion
    }

}
