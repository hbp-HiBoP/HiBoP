using HBP.Core.Database;
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

        [SerializeField] private MenuButton m_CheckDatabaseIntegrityButton;
        public MenuButton CheckDatabaseIntegrityButton { get { return m_CheckDatabaseIntegrityButton; } }
        #endregion

        #region Private Methods
        protected override void Awake()
        {
            base.Awake();
            m_OpenSettingsModifierButton.Initialize(this, OpenSettingsModifier);
            m_OpenProtocolGestionButton.Initialize(this, OpenProtocolGestion);
            m_OpenDatabaseBrowserButton.Initialize(this, OpenDatabaseBrowser);
            m_OpenTrialMatrixExplorerButton.Initialize(this, OpenTrialMatrixExplorer);
            m_CheckDatabaseIntegrityButton.Initialize(this, CheckDatabaseIntegrity);
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
        public async void CheckDatabaseIntegrity()
        {
            int result = await DialogBoxManager.OpenAsync(Core.Enums.DialogBoxType.Informational, "Database integrity check", "This operation will check the integrity of the database and generate a report of any errors or warnings found. This may take a while.\n\nWould you like to proceed?", "Check", "Cancel");
            if (result == 1)
                return;

            string path = await FileBrowser.GetSavedFileNameAsync(new string[] { "txt" }, "Save report to", "", "database_integrity_report");
            if (string.IsNullOrEmpty(path))
                return;

            await LoadingManager.LoadAsync((update, token) => DatabaseManager.Database.CheckIntegrityAsync(path, update, token));

            await DialogBoxManager.OpenAsync(Core.Enums.DialogBoxType.Informational, "Database integrity check", "The database integrity check has been completed. The report has been saved to the specified location.", "OK");
        }
        #endregion
    }
}