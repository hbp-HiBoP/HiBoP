using HBP.Data.Experience.Dataset;
using HBP.UI.Experience.Dataset;
using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI
{
    /// <summary>
    /// Window to select dataInfo.
    /// </summary>
    public class DataInfoSelector : ObjectSelector<DataInfo>
    {
        #region Properties
        [SerializeField] DataInfoList m_List;
        /// <summary>
        /// UI dataInfos list.
        /// </summary>
        protected override SelectableList<DataInfo> List => m_List;
        #endregion
    }
}