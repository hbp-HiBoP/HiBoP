using UnityEngine;
using HBP.UI.Tools;
using HBP.Core.Tools;
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
        #endregion

        #region Private Methods
        protected override void Awake()
        {
            base.Awake();
            m_OpenPreferencesButton.Initialize(this, OpenPreferences);
            m_OpenTagsManagerButton.Initialize(this, OpenTagsManager);
        }
        #endregion

        #region Public Methods
        public void OpenPreferences()
        {
            WindowsManager.OpenModifier(PersistentDataManager.UserPreferences);
        }
        public void OpenTagsManager()
        {
            WindowsManager.OpenModifier(PersistentDataManager.Tags);
        }
        #endregion
    }

}
