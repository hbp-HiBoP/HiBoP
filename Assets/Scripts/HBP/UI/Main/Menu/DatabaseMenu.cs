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

        [SerializeField] private MenuButton m_OpenDatabaseBrowserButton;
        public MenuButton OpenDatabaseBrowserButton { get { return m_OpenDatabaseBrowserButton; } }

        [SerializeField] private MenuButton m_OpenTrialMatrixExplorerButton;
        public MenuButton OpenTrialMatrixExplorerButton { get { return m_OpenTrialMatrixExplorerButton; } }
        #endregion

        #region Private Methods
        protected override void Awake()
        {
            base.Awake();
            m_OpenSettingsModifierButton.Initialize(this, OpenSettingsModifier);
            m_OpenProtocolGestionButton.Initialize(this, OpenProtocolGestion);
            m_OpenDatabaseBrowserButton.Initialize(this, OpenDatabaseBrowser);
            m_OpenTrialMatrixExplorerButton.Initialize(this, OpenTrialMatrixExplorer);
        }
        #endregion

        #region Public Methods
        public void OpenSettingsModifier()
        {
            WindowsManager.OpenModifier(DatabaseManager.Database.Settings, null);
        }
        public void OpenProtocolGestion()
        {
            WindowsManager.Open("Protocol gestion window", null);
        }
        public void OpenDatabaseBrowser()
        {
            WindowsManager.Open("Database browser window", null);
        }
        public void OpenTrialMatrixExplorer()
        {
            WindowsManager.Open("Trial matrix explorer window", null);
        }
        #endregion
    }
}