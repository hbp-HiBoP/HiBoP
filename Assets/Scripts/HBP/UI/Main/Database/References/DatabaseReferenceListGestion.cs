using HBP.Data.Database;
using HBP.UI.Tools;
using HBP.UI.Tools.Lists;
using UnityEngine;

namespace HBP.UI.Database
{
    public class DatabaseReferenceListGestion : ListGestion<DatabaseReference>
    {
        #region Properties
        [SerializeField] protected DatabaseReferenceList m_List;
        public override ActionableList<DatabaseReference> List => m_List;

        [SerializeField] protected DatabaseReferenceCreator m_ObjectCreator;
        public override ObjectCreator<DatabaseReference> ObjectCreator => m_ObjectCreator;
        #endregion
    }
}