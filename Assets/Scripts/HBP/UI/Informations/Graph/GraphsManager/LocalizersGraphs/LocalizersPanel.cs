using HBP.Core.Tools;
using HBP.Core.Object3D;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System;
using HBP.UI.Tools;
using Cysharp.Threading.Tasks;
using UnityEngine.Events;
using HBP.Data.Informations;
using HBP.Data.Informations.Graphs;
using System.Globalization;

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

        // Rescaling
        [SerializeField] private Transform m_RescalingContainer;
        [SerializeField] private Toggle m_EnableRescalingToggle;
        [SerializeField] private InputField m_BaselineValueInputField;
        [SerializeField] private InputField m_GainFactorInputField;
        [SerializeField] private InputField m_OffsetInputField;
        [SerializeField] private Text m_RescalingFormulaText;
        
        // Protocols
        [SerializeField] private Dropdown m_DataTypeDropdown;
        [SerializeField] private GameObject m_ProtocolItemPrefab;
        [SerializeField] private Transform m_ProtocolsContainer;

        // Generate
        [SerializeField] private Button m_GenerateLocalizersGraphsButton;

        private List<ProtocolItem> m_ProtocolItems = new List<ProtocolItem>();
        private string m_SelectedDataType;
        
        // Rescaling parameters
        private bool m_EnableRescaling = false;
        private float m_BaselineValue = 0f;
        private float m_GainFactor = 1f;
        private float m_Offset = 0f;
        
        public string SelectedDataType => m_SelectedDataType;
        public List<ProtocolItem> ProtocolItems => m_ProtocolItems;
        
        // Rescaling properties
        public bool EnableRescaling => m_EnableRescaling;
        public float BaselineValue => m_BaselineValue;
        public float GainFactor => m_GainFactor;
        public float Offset => m_Offset;

        private LocalizersGraphsWorker m_Worker = new();
        #endregion

        #region Events
        public GenericEvent<Dictionary<ChannelStruct, List<LocalizerCurveData>>> OnGenerateLocalizersGraphs = new GenericEvent<Dictionary<ChannelStruct, List<LocalizerCurveData>>>();
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

            m_GenerateLocalizersGraphsButton.interactable = Object3DManager.Localizers.Loaded;

            InitializeRescaling();
            InitializeProtocols();
        } 
        public async void GenerateLocalizersGraphs()
        {
            var mode = (LocalizersGraphsMode)m_LocalizersGraphsModeDropdown.value;
            var atlas = (LocalizersGraphsAtlas)m_LocalizersGraphsAtlasDropdown.value;
            var precision = (int)m_LocalizersGraphsPrecisionSlider.value;
            var dataType = m_DataTypeDropdown.options[m_DataTypeDropdown.value].text;
            
            // Create rescaling parameters
            var rescalingParams = new RescalingParameters(m_EnableRescaling, m_BaselineValue, m_GainFactor, m_Offset);
            try
            {
                Dictionary<ChannelStruct, List<LocalizerCurveData>> result = mode switch
                {
                    LocalizersGraphsMode.Voxel => await LoadingManager.LoadAsync((progress, token) => m_Worker.GenerateLocalizersGraphsVoxelAsync(dataType, m_ProtocolItems, rescalingParams, progress, token)),
                    LocalizersGraphsMode.Region => await LoadingManager.LoadAsync((progress, token) => m_Worker.GenerateLocalizersGraphsRegionAsync(precision, dataType, m_ProtocolItems, rescalingParams, progress, token)),
                    LocalizersGraphsMode.Atlas => await LoadingManager.LoadAsync((progress, token) => m_Worker.GenerateLocalizersGraphsAtlasAsync(atlas, dataType, m_ProtocolItems, rescalingParams, progress, token)),
                    _ => new(),
                };
                await DialogBoxManager.OpenAsync(Core.Enums.DialogBoxType.Informational, "Localizers Graphs", $"Curves generated for all selected blocs.", "OK");
                OnGenerateLocalizersGraphs.Invoke(result);
            }
            catch (OperationCanceledException)
            {
                // Ignore cancellation
            }
            catch (Exception e)
            {
                throw e;
            }
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
        private void InitializeRescaling()
        {
            // Initialize rescaling toggle
            if (m_EnableRescalingToggle != null)
            {
                m_EnableRescalingToggle.isOn = m_EnableRescaling;
                m_EnableRescalingToggle.onValueChanged.AddListener(OnToggleRescaling);
            }
            
            // Initialize baseline value input field
            if (m_BaselineValueInputField != null)
            {
                m_BaselineValueInputField.text = m_BaselineValue.ToString(CultureInfo.InvariantCulture);
                m_BaselineValueInputField.onEndEdit.AddListener(OnChangeBaselineValue);
            }
            
            // Initialize gain factor input field
            if (m_GainFactorInputField != null)
            {
                m_GainFactorInputField.text = m_GainFactor.ToString(CultureInfo.InvariantCulture);
                m_GainFactorInputField.onEndEdit.AddListener(OnChangeGainFactor);
            }
            
            // Initialize offset input field
            if (m_OffsetInputField != null)
            {
                m_OffsetInputField.text = m_Offset.ToString(CultureInfo.InvariantCulture);
                m_OffsetInputField.onEndEdit.AddListener(OnChangeOffset);
            }
            
            // Set initial state of rescaling container
            UpdateRescalingContainerState();
            UpdateRescalingFormulaText();
        }
        private void OnToggleRescaling(bool enabled)
        {
            m_EnableRescaling = enabled;
            UpdateRescalingContainerState();
            UpdateRescalingFormulaText();
        }
        private void UpdateRescalingContainerState()
        {
            m_RescalingContainer.gameObject.SetActive(m_EnableRescaling);
        }
        private void UpdateRescalingFormulaText()
        {
            if (m_RescalingFormulaText != null)
            {
                if (m_EnableRescaling)
                {
                    // Format the formula with current values
                    string formula = string.Format(CultureInfo.InvariantCulture, 
                        "newValue = (oldValue - {0}) × {1} + {0} + {2}",
                        m_BaselineValue.ToString("0.##"),
                        m_GainFactor.ToString("0.##"),
                        m_Offset.ToString("0.##"));
                    
                    m_RescalingFormulaText.text = formula;
                }
                else
                {
                    m_RescalingFormulaText.text = "No rescaling applied";
                }
            }
        }
        private void OnChangeBaselineValue(string value)
        {
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedValue))
            {
                m_BaselineValue = parsedValue;
                UpdateRescalingFormulaText();
            }
            else
            {
                // Reset to previous valid value if parsing fails
                m_BaselineValueInputField.text = m_BaselineValue.ToString(CultureInfo.InvariantCulture);
            }
        }
        private void OnChangeGainFactor(string value)
        {
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedValue))
            {
                // Prevent division by zero or negative scaling
                if (parsedValue != 0f)
                {
                    m_GainFactor = parsedValue;
                    UpdateRescalingFormulaText();
                }
                else
                {
                    // Reset to previous valid value
                    m_GainFactorInputField.text = m_GainFactor.ToString(CultureInfo.InvariantCulture);
                }
            }
            else
            {
                // Reset to previous valid value if parsing fails
                m_GainFactorInputField.text = m_GainFactor.ToString(CultureInfo.InvariantCulture);
            }
        }
        private void OnChangeOffset(string value)
        {
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedValue))
            {
                m_Offset = parsedValue;
                UpdateRescalingFormulaText();
            }
            else
            {
                // Reset to previous valid value if parsing fails
                m_OffsetInputField.text = m_Offset.ToString(CultureInfo.InvariantCulture);
            }
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