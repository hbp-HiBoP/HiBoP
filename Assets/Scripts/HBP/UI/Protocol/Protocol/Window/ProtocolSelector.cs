using UnityEngine;
using HBP.UI.Tools.Lists;
using HBP.UI.Tools;

namespace HBP.UI.Main
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

