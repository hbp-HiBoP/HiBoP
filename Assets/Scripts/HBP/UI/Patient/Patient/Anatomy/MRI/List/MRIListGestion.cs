using HBP.Data.Anatomy;
using Tools.Unity.Components;
using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI.Anatomy
{
    public class MRIListGestion : ListGestion<MRI>
    {
        #region Properties
        [SerializeField] protected MRIList m_List;
        public override SelectableListWithItemAction<MRI> List => m_List;

        [SerializeField] protected MRICreator m_ObjectCreator;
        public override ObjectCreator<MRI> ObjectCreator => m_ObjectCreator;
        #endregion
    }
}