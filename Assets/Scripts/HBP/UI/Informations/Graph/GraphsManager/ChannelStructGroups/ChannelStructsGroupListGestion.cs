using HBP.Data.Informations;
using HBP.UI.Tools;
using HBP.UI.Tools.Lists;
using UnityEngine;

namespace HBP.UI.Informations
{
    public class ChannelStructsGroupListGestion : ListGestion<ChannelStructsGroup>
    {
        #region Properties

        [SerializeField] private ChannelStructsGroupList m_List;
        public override ActionableList<ChannelStructsGroup> List => m_List;

        [SerializeField] private ChannelStructsGroupCreator m_ObjectCreator;
        public override ObjectCreator<ChannelStructsGroup> ObjectCreator => m_ObjectCreator;

        #endregion
    }
}
