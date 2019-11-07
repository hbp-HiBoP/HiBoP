using Tools.Unity.Components;
using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI.Experience.Dataset
{
    public class DatasetListGestion : ListGestion<Data.Experience.Dataset.Dataset>
    {
        #region Properties
        [SerializeField] protected DatasetList m_List;
        public override SelectableListWithItemAction<Data.Experience.Dataset.Dataset> List => m_List;

        [SerializeField] protected DatasetCreator m_ObjectCreator;
        public override ObjectCreator<Data.Experience.Dataset.Dataset> ObjectCreator => m_ObjectCreator;
        #endregion
    }
}
