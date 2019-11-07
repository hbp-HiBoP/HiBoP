using Tools.Unity.Components;
using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI
{
    public class MRIListGestion : ListGestion<Data.MRI>
    {
        #region Properties
        [SerializeField] protected MRIList m_List;
        public override SelectableListWithItemAction<Data.MRI> List => m_List;

        [SerializeField] protected MRICreator m_ObjectCreator;
        public override ObjectCreator<Data.MRI> ObjectCreator => m_ObjectCreator;
        #endregion
    }
}