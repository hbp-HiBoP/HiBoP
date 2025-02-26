using HBP.Data.Database;
using HBP.Data.Module3D;
using HBP.UI.Tools;
using System.Linq;
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
            m_PatientList.Set(DatabaseManager.Database.Patients.OrderBy(p => p.Place).ThenBy(p => p.Date).ThenBy(p => p.Name));
            m_PatientList.OnSelect.AddListener(m_PatientExplorer.Set);
        }
        #endregion
    }
}