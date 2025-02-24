using HBP.Data.Database;
using HBP.UI.Tools;
using UnityEngine;

namespace HBP.UI.Database
{
    public class DatabaseBrowserWindow : DialogWindow
    {
        #region Properties
        [SerializeField] DatabasePatientList m_PatientList;
        [SerializeField] DatabasePatientExplorer m_PatientExplorer;
        #endregion

        #region Private Methods
        protected override void SetFields()
        {
            base.SetFields();
            m_PatientList.Set(DatabaseManager.Database.Patients);
            m_PatientList.OnSelect.AddListener(m_PatientExplorer.Set);
        }
        #endregion
    }
}