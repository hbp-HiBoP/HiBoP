using HBP.Data.Database;
using HBP.UI.Main;
using HBP.UI.Tools;
using UnityEngine;

namespace HBP.UI.Database
{
    public class DatabaseReferenceGestion : GestionWindow<DatabaseReference>
    {
        #region Properties
        [SerializeField] DatabaseReferenceListGestion m_ListGestion;
        public override ListGestion<DatabaseReference> ListGestion => m_ListGestion;
        #endregion

        #region Public Methods
        public override void OK()
        {
            base.OK();
            DatabaseManager.Database.SetDatabaseReferences(m_ListGestion.List.Objects);
            DatabaseManager.Database.SaveDatabaseReferences();
            InteractableStateManager.SetInteractables();
        }
        #endregion

        #region Private Methods
        protected override void SetFields()
        {
            base.SetFields();
            ListGestion.List.Set(DatabaseManager.Database.DatabaseReferences);
        }
        #endregion
    }
}