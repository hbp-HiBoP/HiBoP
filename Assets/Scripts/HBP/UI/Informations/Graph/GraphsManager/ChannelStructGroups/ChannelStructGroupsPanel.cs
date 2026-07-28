using HBP.Data.Informations;
using HBP.UI.Tools;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using HBP.Core.Tools;

namespace HBP.UI.Informations
{
    public class ChannelStructGroupsPanel : MonoBehaviour
    {
        #region Properties

        [SerializeField] private ChannelStructsGroupListGestion m_ChannelStructsGroupListGestion;

        public List<ChannelStructsGroup> ChannelStructsGroups
        {
            get => m_ChannelStructsGroupListGestion.List.Objects.ToList();
            set => m_ChannelStructsGroupListGestion.List.Set(value);
        }

        public WindowsReferencer WindowsReferencer => m_ChannelStructsGroupListGestion.WindowsReferencer;

        #endregion

        #region Events

        public GenericEvent<List<ChannelStructsGroup>> OnDisplayChannelStructsGroupsGraphs = new();

        #endregion

        #region Public Methods

        public void DisplayGraphs()
        {
            OnDisplayChannelStructsGroupsGraphs.Invoke(ChannelStructsGroups);
        }

        #endregion
    }
}
