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
        [SerializeField] DatasetListGestion m_DatasetListGestion;
        #endregion

        #region Private Methods
        protected override void SetFields()
        {
            base.SetFields();
            m_PatientListGestion.List.Set(DatabaseManager.Database.Patients);
            //m_DatasetListGestion.List.Set(DatabaseManager.Database.Datasets);
        }
        #endregion
    }
}