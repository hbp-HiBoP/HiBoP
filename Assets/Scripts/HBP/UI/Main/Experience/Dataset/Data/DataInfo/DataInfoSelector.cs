using UnityEngine;
using HBP.UI.Tools.Lists;
using HBP.UI.Tools;

namespace HBP.UI.Main
{
    /// <summary>
    /// Window to select dataInfo.
    /// </summary>
    public class DataInfoSelector : ObjectSelector<Core.Data.DataInfo>
    {
        #region Properties
        [SerializeField] DataInfoList m_List;
        /// <summary>
        /// UI dataInfos list.
        /// </summary>
        protected override SelectableList<Core.Data.DataInfo> List => m_List;
        #endregion
    }
}