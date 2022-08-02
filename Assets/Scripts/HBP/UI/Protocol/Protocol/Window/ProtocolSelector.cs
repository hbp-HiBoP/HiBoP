using HBP.UI.Experience.Protocol;
using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI
{
    /// <summary>
    /// Window to select protocols.
    /// </summary>
    public class ProtocolSelector : ObjectSelector<Core.Data.Protocol>
    {
        #region Properties
        [SerializeField] ProtocolList m_List;
        /// <summary>
        /// UI protocols list.
        /// </summary>
        protected override SelectableList<Core.Data.Protocol> List => m_List;
        #endregion
    }
}

