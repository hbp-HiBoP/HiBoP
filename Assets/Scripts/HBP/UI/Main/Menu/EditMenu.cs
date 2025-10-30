using UnityEngine;
using HBP.UI.Tools;
using HBP.Data.Preferences;

namespace HBP.UI.Main
{
    public class EditMenu : Menu
    {
        #region Properties
        [SerializeField] private MenuButton m_OpenPreferencesButton;
        public MenuButton OpenPreferencesButton { get { return m_OpenPreferencesButton; } }

        [SerializeField] private MenuButton m_OpenTagsManagerButton;
        public MenuButton OpenTagsManagerButton { get { return m_OpenTagsManagerButton; } }

        [SerializeField] private MenuButton m_OpenAliasesManagerButton;
        public MenuButton OpenAliasesManagerButton { get { return m_OpenAliasesManagerButton; } }
        #endregion

        #region Private Methods
        protected override void Awake()
        {
            base.Awake();
            m_OpenPreferencesButton.Initialize(this, OpenPreferences);
            m_OpenTagsManagerButton.Initialize(this, OpenTagsManager);
            m_OpenAliasesManagerButton.Initialize(this, OpenAliasesManager);
        }
        #endregion

        #region Public Methods
        public void OpenPreferences()
        {
            WindowsManager.OpenModifier(PersistentDataManager.UserPreferences, null);
        }
        public void OpenTagsManager()
        {
            WindowsManager.OpenModifier(PersistentDataManager.Tags, null);
        }
        public void OpenAliasesManager()
        {
            WindowsManager.OpenModifier(PersistentDataManager.Aliases, null);
        }
        #endregion
    }

}
