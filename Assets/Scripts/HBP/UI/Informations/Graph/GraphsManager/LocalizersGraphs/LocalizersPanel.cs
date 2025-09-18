using HBP.Core.Tools;
using HBP.Core.Object3D;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System;

namespace HBP.UI.Informations
{
    #region Enums
    public enum LocalizersGraphsMode { Voxel, Region, Atlas }
    public enum LocalizersGraphsAtlas { MarsAtlas, Jubrain }
    #endregion

    public class LocalizersPanel : MonoBehaviour
    {
        #region Properties
        // General
        [SerializeField] private Dropdown m_LocalizersGraphsModeDropdown;
        [SerializeField] private Dropdown m_LocalizersGraphsAtlasDropdown;
        [SerializeField] private Slider m_LocalizersGraphsPrecisionSlider;
        [SerializeField] private GameObject m_LocalizersGraphsVoxelSettingsContainer;
        [SerializeField] private GameObject m_LocalizersGraphsRegionSettingsContainer;
        [SerializeField] private GameObject m_LocalizersGraphsAtlasSettingsContainer;
        // Protocols
        [SerializeField] private Dropdown m_DataTypeDropdown;
        [SerializeField] private GameObject m_ProtocolItemPrefab;
        [SerializeField] private Transform m_ProtocolsContainer;
        
        private List<ProtocolItem> m_ProtocolItems = new List<ProtocolItem>();
        private string m_SelectedDataType;
        
        public string SelectedDataType => m_SelectedDataType;
        public List<ProtocolItem> ProtocolItems => m_ProtocolItems;
        #endregion

        #region Public Methods
        public void Initialize()
        {
            m_LocalizersGraphsModeDropdown.Set(typeof(LocalizersGraphsMode), (int)LocalizersGraphsMode.Voxel);
            m_LocalizersGraphsModeDropdown.onValueChanged.AddListener(OnChangeLocalizersGraphsMode);
            m_LocalizersGraphsAtlasDropdown.Set(typeof(LocalizersGraphsAtlas), (int)LocalizersGraphsAtlas.MarsAtlas);
            m_LocalizersGraphsPrecisionSlider.minValue = 1;
            m_LocalizersGraphsPrecisionSlider.maxValue = 10;
            m_LocalizersGraphsPrecisionSlider.value = 1;

            m_DataTypeDropdown.options = Object3DManager.Localizers.AvailableDataNames.Select(name => new Dropdown.OptionData(name)).ToList();
            m_DataTypeDropdown.value = 0;
            InitializeProtocols();
        }
        public async void GenerateLocalizersGraphs()
        {
        }
        #endregion

        #region Private Methods
        private void OnChangeLocalizersGraphsMode(int value)
        {
            LocalizersGraphsMode mode = (LocalizersGraphsMode)value;
            m_LocalizersGraphsVoxelSettingsContainer.SetActive(mode == LocalizersGraphsMode.Voxel);
            m_LocalizersGraphsRegionSettingsContainer.SetActive(mode == LocalizersGraphsMode.Region);
            m_LocalizersGraphsAtlasSettingsContainer.SetActive(mode == LocalizersGraphsMode.Atlas);
        }
        private void InitializeProtocols()
        {
            foreach (var protocolItem in m_ProtocolItems)
            {
                Destroy(protocolItem.gameObject);
            }
            m_ProtocolItems.Clear();

            foreach (var protocolName in Object3DManager.Localizers.AvailableProtocolNames)
            {
                GameObject protocolItemObj = Instantiate(m_ProtocolItemPrefab, m_ProtocolsContainer);
                ProtocolItem protocolItem = protocolItemObj.GetComponent<ProtocolItem>();
                protocolItem.Initialize(protocolName);
                m_ProtocolItems.Add(protocolItem);
            }
        }
        #endregion
    }
}