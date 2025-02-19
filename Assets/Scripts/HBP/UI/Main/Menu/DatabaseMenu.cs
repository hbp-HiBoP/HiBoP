using HBP.Data.Database;
using HBP.UI.Tools;
using UnityEngine;

namespace HBP.UI.Main
{
    public class DatabaseMenu : Menu
    {
        #region Properties
        [SerializeField] private MenuButton m_OpenSettingsModifierButton;
        public MenuButton OpenSettingsModifierButton { get { return m_OpenSettingsModifierButton; } }

        [SerializeField] private MenuButton m_OpenProtocolGestionButton;
        public MenuButton OpenProtocolGestionButton { get { return m_OpenProtocolGestionButton; } }

        [SerializeField] private MenuButton m_OpenDatabaseGestionButton;
        public MenuButton OpenDatabaseGestionButton { get { return m_OpenDatabaseGestionButton; } }

        [SerializeField] private MenuButton m_OpenDatabaseBrowserButton;
        public MenuButton OpenDatabaseBrowserButton { get { return m_OpenDatabaseBrowserButton; } }
        #endregion

        #region Private Methods
        protected override void Awake()
        {
            base.Awake();
            m_OpenSettingsModifierButton.Initialize(this, OpenSettingsModifier);
            m_OpenProtocolGestionButton.Initialize(this, OpenProtocolGestion);
            m_OpenDatabaseGestionButton.Initialize(this, OpenDatabaseGestion);
            m_OpenDatabaseBrowserButton.Initialize(this, OpenDatabaseBrowser);
        }
        #endregion

        #region Public Methods
        public void OpenSettingsModifier()
        {
            WindowsManager.OpenModifier(DatabaseManager.Database.Settings);
        }
        public void OpenProtocolGestion()
        {
            WindowsManager.Open("Protocol gestion window");
        }
        public void OpenDatabaseGestion()
        {
            WindowsManager.Open("Database Reference gestion window");
        }
        public void OpenDatabaseBrowser()
        {
            WindowsManager.Open("Database browser window");
        }
        #endregion
    }
}