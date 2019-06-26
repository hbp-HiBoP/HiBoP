using HBP.Data.Experience.Dataset;
using HBP.UI.Experience.Dataset;
using UnityEngine;

namespace HBP.UI
{
    public class DataInfoSelector : ObjectSelector<DataInfo>
    {
        #region Properties
        [SerializeField] DataInfoList m_DataInfoList;
        #endregion

        #region Public Methods
        protected override void Initialize()
        {
            m_List = m_DataInfoList;
            base.Initialize();
        }
        #endregion
    }
}