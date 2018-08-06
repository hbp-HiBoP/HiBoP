﻿using UnityEngine;
using UnityEngine.UI;
using d = HBP.Data.Experience.Dataset;
using System.Linq;
using System.Collections.Generic;

namespace HBP.UI.Experience.Dataset
{
	/// <summary>
	/// Display/Modify dataset.
	/// </summary>
	public class DatasetModifier : ItemModifier<d.Dataset> 
	{
		#region Properties		
		[SerializeField] DataInfoList m_DataInfoList;
		[SerializeField] InputField m_NameInputField;
        [SerializeField] Dropdown m_ProtocolDropdown;
        [SerializeField] Button m_CreateButton, m_RemoveButton;
        [SerializeField] GameObject m_DataInfoModifierPrefab;
        [SerializeField] Text m_Counter;
        List<DataInfoModifier> m_Modifiers = new List<DataInfoModifier>();
        Data.Experience.Protocol.Protocol[] m_Protocols;

        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }

            set
            {
                base.Interactable = value;
                m_NameInputField.interactable = value;
                m_ProtocolDropdown.interactable = value;
                m_CreateButton.interactable = value;
                m_RemoveButton.interactable = value;
                m_DataInfoList.Interactable = value;
            }
        }
        #endregion

        #region Public Methods			
        public void Add()
		{
            OpenDataInfoModifier(new d.DataInfo());
		}
		public void Remove()
		{
            d.DataInfo[] dataInfoToRemove = m_DataInfoList.ObjectsSelected;
            ItemTemp.RemoveData(dataInfoToRemove);
            m_DataInfoList.Remove(dataInfoToRemove);
        }
        public override void Close()
        {
            foreach (var modifier in m_Modifiers.ToArray()) modifier.Close();
            m_Modifiers.Clear();
            base.Close();
        }
        #endregion

        #region Protected Methods
        protected void OpenDataInfoModifier(d.DataInfo dataInfo)
        {
            DataInfoModifier dataInfoModifier = ApplicationState.WindowsManager.Open("dataInfo modifier window", Interactable) as DataInfoModifier;
            dataInfoModifier.OnSave.AddListener(() => OnSaveDataInfoModifier(dataInfoModifier));
            dataInfoModifier.OnClose.AddListener(() => OnCloseDataInfoModifier(dataInfoModifier));
            dataInfoModifier.CanSaveEvent.AddListener(() => OnCanSave(dataInfoModifier));
            m_Modifiers.Add(dataInfoModifier);
        }
        protected void OnCanSave(DataInfoModifier modifier)
        {
            modifier.CanSave = !ItemTemp.Data.Any((d) => d.Name == modifier.ItemTemp.Name && d.Patient == modifier.ItemTemp.Patient && d != modifier.Item);
        }
        protected void OnSaveDataInfoModifier(DataInfoModifier modifier)
        {
            modifier.Item.GetErrors(ItemTemp.Protocol);
            // Save
            if (!ItemTemp.Data.Contains(modifier.Item))
            {
                ItemTemp.AddData(modifier.Item);
                m_DataInfoList.Add(modifier.Item);
            }
            else
            {
                m_DataInfoList.UpdateObject(modifier.Item);
            }
        }
        protected void OnCloseDataInfoModifier(DataInfoModifier modifier)
        {
            m_Modifiers.Remove(modifier);
        }
        protected override void SetFields(d.Dataset objectToDisplay)
        {
            m_NameInputField.text = ItemTemp.Name;
            m_NameInputField.onValueChanged.AddListener((value) => ItemTemp.Name = value);

            m_Protocols = ApplicationState.ProjectLoaded.Protocols.ToArray();
            m_ProtocolDropdown.options = (from protocol in m_Protocols select new Dropdown.OptionData(protocol.Name)).ToList();
            m_ProtocolDropdown.value = m_Protocols.ToList().IndexOf(ItemTemp.Protocol);
            m_ProtocolDropdown.onValueChanged.RemoveAllListeners();
            m_ProtocolDropdown.onValueChanged.AddListener((i) => ItemTemp.Protocol = m_Protocols[i]);

            m_DataInfoList.Objects = ItemTemp.Data.ToArray();
            m_DataInfoList.OnAction.RemoveAllListeners();
            m_DataInfoList.OnAction.AddListener((dataInfo, i) => OpenDataInfoModifier(dataInfo));
            m_DataInfoList.OnSelectionChanged.RemoveAllListeners();
            m_DataInfoList.OnSelectionChanged.AddListener(() => m_Counter.text = m_DataInfoList.ObjectsSelected.Length.ToString());
            m_DataInfoList.SortByName(DataInfoList.Sorting.Descending);
        }
        #endregion
    }
}