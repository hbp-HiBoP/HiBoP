using HBP.Data.Informations;
using HBP.Data.Module3D;
using HBP.UI.Tools;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Informations
{
    public class ChannelStructsGroupModifier : ObjectModifier<ChannelStructsGroup>
    {
        #region Properties
        [SerializeField] InputField m_NameInputField;
        [SerializeField] ChannelStructList m_ChannelStructsList;
        [SerializeField] ChannelStructList m_SelectedChannelStructsList;

        [SerializeField] Button m_AddChannelButton;
        [SerializeField] Button m_RemoveChannelButton;

        private List<ChannelStruct> m_ChannelStructs = new List<ChannelStruct>();

        public override bool Interactable
        {
            get => base.Interactable;
            set
            {
                base.Interactable = value;
                m_NameInputField.interactable = value;
                m_ChannelStructsList.Interactable = value;
                m_SelectedChannelStructsList.Interactable = value;
            }
        }
        #endregion

        #region Private Methods
        protected override void Initialize()
        {
            base.Initialize();

            m_NameInputField.onValueChanged.AddListener(name => ObjectTemp.Name = name);

            m_SelectedChannelStructsList.OnAddObject.AddListener(AddChannel);
            m_SelectedChannelStructsList.OnRemoveObject.AddListener(RemoveChannel);

            m_SelectedChannelStructsList.OnSelect.AddListener((c) => UpdateButtonsState());
            m_SelectedChannelStructsList.OnDeselect.AddListener((c) => UpdateButtonsState());
            m_ChannelStructsList.OnSelect.AddListener((c) => UpdateButtonsState());
            m_ChannelStructsList.OnDeselect.AddListener((c) => UpdateButtonsState());

            m_AddChannelButton.onClick.AddListener(OnAddChannels);
            m_RemoveChannelButton.onClick.AddListener(OnRemoveChannels);
        }
        protected override void SetFields()
        {
            base.SetFields();

            m_ChannelStructs = Module3DMain.SelectedScene.Columns.SelectMany(c => c.Sites).Where(s => !s.State.IsMasked).Select(s => new ChannelStruct(s)).GroupBy(c => (c.Channel, c.Patient)).Select(g => g.First()).ToList();
            m_ChannelStructsList.Set(m_ChannelStructs);
        }
        protected override void SetFields(ChannelStructsGroup group)
        {
            m_NameInputField.text = group.Name;
            m_SelectedChannelStructsList.Set(group.Channels);
            UpdateList();
        }

        private void OnAddChannels()
        {
            m_SelectedChannelStructsList.Add(m_ChannelStructsList.ObjectsSelected);
            UpdateList();
        }
        private void OnRemoveChannels()
        {
            m_SelectedChannelStructsList.Remove(m_SelectedChannelStructsList.ObjectsSelected);
            UpdateList();
        }
        private void UpdateList()
        {
            var selectedSet = new HashSet<ChannelStruct>(ObjectTemp.Channels);
            bool[] mask = m_ChannelStructs.Select(cs => !selectedSet.Contains(cs)).ToArray();
            m_ChannelStructsList.MaskList(mask, false);
            UpdateButtonsState();
        }
        private void UpdateButtonsState()
        {
            m_AddChannelButton.interactable = m_ChannelStructsList.ObjectsSelected.Length > 0;
            m_RemoveChannelButton.interactable = m_SelectedChannelStructsList.ObjectsSelected.Length > 0;
        }

        private void AddChannel(ChannelStruct channelStruct)
        {
            if (!ObjectTemp.Channels.Contains(channelStruct))
            {
                ObjectTemp.Channels.Add(channelStruct);
            }
        }
        private void RemoveChannel(ChannelStruct channelStruct)
        {
            if (ObjectTemp.Channels.Contains(channelStruct))
            {
                ObjectTemp.Channels.Remove(channelStruct);
            }
        }
        #endregion
    }
}