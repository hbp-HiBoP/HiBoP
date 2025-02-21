using HBP.Data.Database;
using HBP.UI.Main;
using HBP.UI.Tools;
using UnityEngine;

namespace HBP.UI.Database
{
    public class DatabaseBrowserWindow : DialogWindow
    {
        #region Properties
        [SerializeField] PatientListGestion m_PatientListGestion;
        [SerializeField] DataInfoListGestion m_DataInfoListGestion;
        #endregion

        #region Private Methods
        protected override void SetFields()
        {
            base.SetFields();
            m_PatientListGestion.List.Set(DatabaseManager.Database.Patients);
            m_DataInfoListGestion.List.Set(DatabaseManager.Database.DataInfos);
        }
        #endregion
    }
}