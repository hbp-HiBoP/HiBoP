using HBP.Data.Database;
using HBP.Data.Module3D;
using HBP.UI.Tools;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Database
{
    public class DatabaseBrowserWindow : DialogWindow
    {
        #region Properties
        [SerializeField] DatabasePatientList m_PatientList;
        [SerializeField] DatabasePatientExplorer m_PatientExplorer;

        [SerializeField] Button m_OpenDatabaseReferencesWindowButton;
        #endregion

        #region Private Methods
        protected override void Initialize()
        {
            base.Initialize();
            m_OpenDatabaseReferencesWindowButton.onClick.AddListener(OpenDatabaseReferenceGestionWindow);
            m_PatientExplorer.Initialize(m_WindowsReferencer);
        }
        protected override void SetFields()
        {
            base.SetFields();
            m_PatientExplorer.SetFields();

            m_PatientList.Set(DatabaseManager.Database.Patients);
            m_PatientList.OnSelect.AddListener(m_PatientExplorer.Set);
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
        #endregion
    }
}