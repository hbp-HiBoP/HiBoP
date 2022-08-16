using UnityEngine;
using HBP.UI.Lists;

namespace HBP.UI.Experience.Protocol
{
    public class BlocListGestion : ListGestion<Core.Data.Bloc>
    {
        #region Properties
        [SerializeField] protected BlocList m_List;
        public override ActionableList<Core.Data.Bloc> List => m_List;

        [SerializeField] protected BlocCreator m_ObjectCreator;
        public override ObjectCreator<Core.Data.Bloc> ObjectCreator => m_ObjectCreator;
        #endregion
    }
}