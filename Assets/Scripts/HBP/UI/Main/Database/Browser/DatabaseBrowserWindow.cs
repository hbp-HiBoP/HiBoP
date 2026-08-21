using HBP.Core.Database;
using HBP.Core.Preferences;
using HBP.Core.Tools;
using HBP.UI.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Database
{
    public class DatabaseBrowserWindow : DialogWindow
    {
        #region Properties

        [SerializeField] private DatabasePatientList m_PatientList;
        [SerializeField] private DatabasePatientExplorer m_PatientExplorer;

        [SerializeField] private Button m_OpenDatabaseReferencesWindowButton;
        [SerializeField] private Button m_OpenExportLocalizerAtlasWindowButton;
        [SerializeField] private Button m_OpenExportBIDSWindowButton;

        #endregion

        #region Private Methods

        protected override void Initialize()
        {
            base.Initialize();
            m_OpenDatabaseReferencesWindowButton.onClick.AddListener(OpenDatabaseReferenceGestionWindow);
            m_OpenExportLocalizerAtlasWindowButton.onClick.AddListener(OpenExportLocalizerAtlasWindow);
            m_OpenExportBIDSWindowButton.onClick.AddListener(OpenExportBIDSWindow);
            m_PatientExplorer.Initialize(m_WindowsReferencer);
            DatabaseManager.Database.OnUpdateDatabases.AddSafeListener(RefreshDatabaseGraph, gameObject);

            PersistentDataManager.UserPreferences.OnSavePreferences.AddListener(() =>
            {
                m_OpenExportLocalizerAtlasWindowButton.gameObject.SetActive(PersistentDataManager.UserPreferences.General.Misc.AdvancedFeatures);
                m_OpenExportBIDSWindowButton.gameObject.SetActive(PersistentDataManager.UserPreferences.General.Misc.AdvancedFeatures);
            });
        }

        protected override void SetFields()
        {
            base.SetFields();
            RefreshDatabaseGraph();
            m_PatientList.OnSelect.AddListener(m_PatientExplorer.Set);

            m_OpenExportLocalizerAtlasWindowButton.gameObject.SetActive(PersistentDataManager.UserPreferences.General.Misc.AdvancedFeatures);
            m_OpenExportBIDSWindowButton.gameObject.SetActive(PersistentDataManager.UserPreferences.General.Misc.AdvancedFeatures);
        }

        private void RefreshDatabaseGraph()
        {
            m_PatientExplorer.SetFields();
            m_PatientList.Set(DatabaseManager.Database.Patients);
        }

        private void OpenDatabaseReferenceGestionWindow()
        {
            var window = WindowsManager.Open("Database Reference gestion window", this) as DialogWindow;
            window.OnOk.AddListener(() =>
            {
                if (window is DatabaseReferenceGestion databaseReferenceGestion && databaseReferenceGestion.ListGestion.HasBeenModified)
                    SetFields();
            });
            WindowsReferencer.Add(window);
        }

        private void OpenExportLocalizerAtlasWindow()
        {
            var window = WindowsManager.Open("Export Localizer atlas window", this) as DialogWindow;
            WindowsReferencer.Add(window);
        }

        private void OpenExportBIDSWindow()
        {
            var window = WindowsManager.Open("Export BIDS window", this) as DialogWindow;
            WindowsReferencer.Add(window);
        }

        #endregion
    }
}
