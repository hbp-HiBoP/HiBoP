using Tools.Unity.Components;
using UnityEngine;

namespace HBP.UI.Experience.Protocol
{
    public class BlocListGestion : ListGestion<Core.Data.Bloc>
    {
        #region Properties
        [SerializeField] protected BlocList m_List;
        public override Tools.Unity.Lists.ActionableList<Core.Data.Bloc> List => m_List;

        [SerializeField] protected BlocCreator m_ObjectCreator;
        public override ObjectCreator<Core.Data.Bloc> ObjectCreator => m_ObjectCreator;
        #endregion
    }
}