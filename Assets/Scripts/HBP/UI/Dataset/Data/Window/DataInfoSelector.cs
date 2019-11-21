using HBP.Data.Experience.Dataset;
using HBP.UI.Experience.Dataset;
using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI
{
    public class DataInfoSelector : ObjectSelector<DataInfo>
    {
        #region Properties
        [SerializeField] DataInfoList m_List;
        protected override SelectableList<DataInfo> List => m_List;
        #endregion
    }
}