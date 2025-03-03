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

        private float m_KeyHoldDelay = 0.5f;
        private float m_KeyHoldRepeatRate = 0.1f;

        private float m_DownKeyHoldTimer = 0f;
        private float m_UpKeyHoldTimer = 0f;
        #endregion

        #region Private Methods
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                m_PatientList.SelectNext();
                m_DownKeyHoldTimer = 0f;
            }
            else if (Input.GetKey(KeyCode.DownArrow))
            {
                m_DownKeyHoldTimer += Time.deltaTime;
                if (m_DownKeyHoldTimer >= m_KeyHoldDelay)
                {
                    if (m_DownKeyHoldTimer - m_KeyHoldDelay >= m_KeyHoldRepeatRate)
                    {
                        m_PatientList.SelectNext();
                        m_DownKeyHoldTimer -= m_KeyHoldRepeatRate;
                    }
                }
            }
            else
            {
                m_DownKeyHoldTimer = 0f;
            }

            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                m_PatientList.SelectPrevious();
                m_UpKeyHoldTimer = 0f;
            }
            else if (Input.GetKey(KeyCode.UpArrow))
            {
                m_UpKeyHoldTimer += Time.deltaTime;
                if (m_UpKeyHoldTimer >= m_KeyHoldDelay)
                {
                    if (m_UpKeyHoldTimer - m_KeyHoldDelay >= m_KeyHoldRepeatRate)
                    {
                        m_PatientList.SelectPrevious();
                        m_UpKeyHoldTimer -= m_KeyHoldRepeatRate;
                    }
                }
            }
            else
            {
                m_UpKeyHoldTimer = 0f;
            }
        }
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

            m_PatientList.Set(DatabaseManager.Database.Patients.OrderBy(p => p.Place).ThenBy(p => p.Date).ThenBy(p => p.Name));
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