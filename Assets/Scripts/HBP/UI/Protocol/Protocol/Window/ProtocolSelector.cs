using HBP.Data.Experience.Protocol;
using HBP.UI.Experience.Protocol;
using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI
{
    public class ProtocolSelector : ObjectSelector<Protocol>
    {
        #region Properties
        [SerializeField] ProtocolList m_List;
        protected override SelectableList<Protocol> List => m_List;
        #endregion
    }
}

