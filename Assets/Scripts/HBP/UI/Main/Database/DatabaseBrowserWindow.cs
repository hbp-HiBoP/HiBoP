using HBP.Data.Database;
using HBP.UI.Main;
using HBP.UI.Tools;
using UnityEngine;

namespace HBP.UI.Database
{
    public class DatabaseBrowserWindow : DialogWindow
    {
        #region Properties
        [SerializeField] PatientListGestion m_ListGestion;
        #endregion

        #region Private Methods
        protected override void SetFields()
        {
            base.SetFields();
            m_ListGestion.List.Set(DatabaseManager.Database.Patients);
        }
        #endregion
    }
}