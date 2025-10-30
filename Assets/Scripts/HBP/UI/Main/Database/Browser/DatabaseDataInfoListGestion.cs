using UnityEngine;
using HBP.UI.Tools.Lists;
using HBP.UI.Tools;
using HBP.UI.Main;

namespace HBP.UI.Database
{
    public class DatabaseDataInfoListGestion : ListGestion<Core.Data.DataInfo>
    {
        #region Properties
        [SerializeField] protected DatabaseDataInfoList m_List;
        public override ActionableList<Core.Data.DataInfo> List => m_List;

        [SerializeField] protected DataInfoCreator m_ObjectCreator;
        public override ObjectCreator<Core.Data.DataInfo> ObjectCreator => m_ObjectCreator;
        #endregion
    }
}