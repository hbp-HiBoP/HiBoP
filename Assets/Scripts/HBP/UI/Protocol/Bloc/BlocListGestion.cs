using HBP.Data.Experience.Protocol;
using Tools.Unity.Components;
using UnityEngine;

namespace HBP.UI.Experience.Protocol
{
    public class BlocListGestion : ListGestion<Bloc>
    {
        #region Properties
        [SerializeField] protected BlocList m_List;
        public override Tools.Unity.Lists.ActionableList<Bloc> List => m_List;

        [SerializeField] protected BlocCreator m_ObjectCreator;
        public override ObjectCreator<Bloc> ObjectCreator => m_ObjectCreator;
        #endregion
    }
}