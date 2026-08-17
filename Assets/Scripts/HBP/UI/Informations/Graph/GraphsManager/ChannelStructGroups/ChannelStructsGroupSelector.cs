using HBP.Data.Informations;
using HBP.UI.Tools;
using HBP.UI.Tools.Lists;
using UnityEngine;

namespace HBP.UI.Informations
{
    public class ChannelStructsGroupSelector : ObjectSelector<ChannelStructsGroup>
    {
        #region Properties

        [SerializeField] ChannelStructsGroupList m_List;
        protected override SelectableList<ChannelStructsGroup> List => m_List;

        #endregion
    }
}
