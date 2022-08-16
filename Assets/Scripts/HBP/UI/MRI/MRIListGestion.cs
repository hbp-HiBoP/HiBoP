using HBP.UI.Lists;
using UnityEngine;

namespace HBP.UI
{
    public class MRIListGestion : ListGestion<Core.Data.MRI>
    {
        #region Properties
        [SerializeField] protected MRIList m_List;
        public override ActionableList<Core.Data.MRI> List => m_List;

        [SerializeField] protected MRICreator m_ObjectCreator;
        public override ObjectCreator<Core.Data.MRI> ObjectCreator => m_ObjectCreator;
        #endregion
    }
}