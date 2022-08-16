using HBP.UI.Lists;
using UnityEngine;

namespace HBP.UI.Experience.Dataset
{
    public class DatasetListGestion : ListGestion<Core.Data.Dataset>
    {
        #region Properties
        [SerializeField] protected DatasetList m_List;
        public override ActionableList<Core.Data.Dataset> List => m_List;

        [SerializeField] protected DatasetCreator m_ObjectCreator;
        public override ObjectCreator<Core.Data.Dataset> ObjectCreator => m_ObjectCreator;
        #endregion
    }
}
