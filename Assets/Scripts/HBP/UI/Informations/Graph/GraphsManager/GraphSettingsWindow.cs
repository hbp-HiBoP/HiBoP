using HBP.Data.Informations;
using HBP.UI.Tools;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace HBP.UI.Informations
{
    public class GraphSettingsWindow : DialogWindow
    {
        #region Properties
        [SerializeField] private ChannelStructsGroupListGestion m_ChannelStructsGroupListGestion;

        public override bool Interactable
        {
            get => base.Interactable;
            set
            {
                base.Interactable = value;
                m_ChannelStructsGroupListGestion.Interactable = value;
            }
        }

        private List<ChannelStructsGroup> m_ChannelStructsGroups = new();
        public List<ChannelStructsGroup> ChannelStructsGroups
        {
            get => m_ChannelStructsGroups;
            set
            {
                m_ChannelStructsGroups = value;
                SetFields();
            }
        }
        #endregion

        #region Events
        public GenericEvent<List<ChannelStructsGroup>> OnChannelStructsGroupsChanged = new GenericEvent<List<ChannelStructsGroup>>();
        #endregion

        #region Public Methods
        public override void OK()
        {
            base.OK();

            OnChannelStructsGroupsChanged.Invoke(m_ChannelStructsGroupListGestion.List.Objects.ToList());
        }
        #endregion

        #region Private Methods
        protected override void SetFields()
        {
            base.SetFields();

            m_ChannelStructsGroupListGestion.List.Set(m_ChannelStructsGroups);
            m_ChannelStructsGroupListGestion.WindowsReferencer.OnOpenWindow.AddListener(WindowsReferencer.Add);
        }
        #endregion
    }
}