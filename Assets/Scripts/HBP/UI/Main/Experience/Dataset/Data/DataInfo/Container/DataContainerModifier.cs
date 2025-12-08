using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using HBP.Core.Data.Container;
using HBP.Core.Data;
using HBP.Core.Tools;
using HBP.UI.Tools;
using HBP.Core.Errors;
using Cysharp.Threading.Tasks;

namespace HBP.UI.Main
{
    public class DataContainerModifier : SubModifier<DataContainer>
    {
        #region Properties
        [SerializeField] Dropdown m_ContainerTypeDropdown;
        [SerializeField] Button m_DisplayHeaderDataButton;
        [SerializeField] ElanDataContainerSubModifier m_ElanDataContainerSubModifier;
        [SerializeField] BrainVisionDataContainerSubModifier m_BrainVisionDataContainerSubModifier;
        [SerializeField] EDFDataContainerSubModifier m_EDFDataContainerSubModifier;
        [SerializeField] MicromedDataContainerSubModifier m_MicromedDataContainerSubModifier;
        [SerializeField] NiftiDataContainerSubModifier m_NiftiDataContainerSubModifier;
        [SerializeField] FIFDataContainerSubModifier m_FIFDataContainerSubModifier;
        [SerializeField] CSVDataContainerSubModifier m_CSVDataContainerSubModifier;

        Type[] m_Types;
        Elan m_ElanDataContainerTemp;
        EDF m_EDFDataContainerTemp;
        Micromed m_MicromedDataContainerTemp;
        BrainVision m_BrainVisionDataContainerTemp;
        Nifti m_NiftiDataContainerTemp;
        FIF m_FIFDataContainerTemp;
        CSV m_CSVDataContainerTemp;

        DataAttribute m_DataAttribute;
        public DataAttribute DataAttribute
        {
            get
            {
                return m_DataAttribute;
            }
            set
            {
                m_DataAttribute = value;
                m_Types = m_ContainerTypeDropdown.Set(typeof(DataContainer), m_DataAttribute);
            }
        }

        public UnityEvent OnChangeDataType { get; } = new UnityEvent();

        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }
            set
            {
                base.Interactable = value;

                m_ContainerTypeDropdown.interactable = value;
                m_DisplayHeaderDataButton.interactable = true; // Always interactable to allow viewing even if the container is not editable
                m_ElanDataContainerSubModifier.Interactable = value;
                m_EDFDataContainerSubModifier.Interactable = value;
                m_BrainVisionDataContainerSubModifier.Interactable = value;
                m_MicromedDataContainerSubModifier.Interactable = value;
                m_NiftiDataContainerSubModifier.Interactable = value;
                m_FIFDataContainerSubModifier.Interactable = value;
                m_CSVDataContainerSubModifier.Interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();
            m_ContainerTypeDropdown.onValueChanged.AddListener(ChangeDataInfoType);
            m_DisplayHeaderDataButton.onClick.AddListener(DisplayHeaderData);
        }
        #endregion

        #region Private Methods
        void ChangeDataInfoType(int i)
        {
            Type type = m_Types[i];
            if (type == typeof(Elan))
            {
                m_ElanDataContainerSubModifier.IsActive = true;
                m_BrainVisionDataContainerSubModifier.IsActive = false;
                m_EDFDataContainerSubModifier.IsActive = false;
                m_MicromedDataContainerSubModifier.IsActive = false;
                m_NiftiDataContainerSubModifier.IsActive = false;
                m_FIFDataContainerSubModifier.IsActive = false;
                m_CSVDataContainerSubModifier.IsActive = false;

                m_ElanDataContainerSubModifier.Object = m_ElanDataContainerTemp;
                m_Object = m_ElanDataContainerTemp;
            }
            else if (type == typeof(BrainVision))
            {
                m_ElanDataContainerSubModifier.IsActive = false;
                m_BrainVisionDataContainerSubModifier.IsActive = true;
                m_EDFDataContainerSubModifier.IsActive = false;
                m_MicromedDataContainerSubModifier.IsActive = false;
                m_NiftiDataContainerSubModifier.IsActive = false;
                m_FIFDataContainerSubModifier.IsActive = false;
                m_CSVDataContainerSubModifier.IsActive = false;

                m_BrainVisionDataContainerSubModifier.Object = m_BrainVisionDataContainerTemp;
                m_Object = m_BrainVisionDataContainerTemp;
            }
            else if (type == typeof(EDF))
            {
                m_ElanDataContainerSubModifier.IsActive = false;
                m_BrainVisionDataContainerSubModifier.IsActive = false;
                m_EDFDataContainerSubModifier.IsActive = true;
                m_MicromedDataContainerSubModifier.IsActive = false;
                m_NiftiDataContainerSubModifier.IsActive = false;
                m_FIFDataContainerSubModifier.IsActive = false;
                m_CSVDataContainerSubModifier.IsActive = false;

                m_EDFDataContainerSubModifier.Object = m_EDFDataContainerTemp;
                m_Object = m_EDFDataContainerTemp;
            }
            else if (type == typeof(Micromed))
            {
                m_ElanDataContainerSubModifier.IsActive = false;
                m_BrainVisionDataContainerSubModifier.IsActive = false;
                m_EDFDataContainerSubModifier.IsActive = false;
                m_MicromedDataContainerSubModifier.IsActive = true;
                m_NiftiDataContainerSubModifier.IsActive = false;
                m_FIFDataContainerSubModifier.IsActive = false;
                m_CSVDataContainerSubModifier.IsActive = false;

                m_MicromedDataContainerSubModifier.Object = m_MicromedDataContainerTemp;
                m_Object = m_MicromedDataContainerTemp;
            }
            else if (type == typeof(Nifti))
            {
                m_ElanDataContainerSubModifier.IsActive = false;
                m_BrainVisionDataContainerSubModifier.IsActive = false;
                m_EDFDataContainerSubModifier.IsActive = false;
                m_MicromedDataContainerSubModifier.IsActive = false;
                m_NiftiDataContainerSubModifier.IsActive = true;
                m_FIFDataContainerSubModifier.IsActive = false;
                m_CSVDataContainerSubModifier.IsActive = false;

                m_NiftiDataContainerSubModifier.Object = m_NiftiDataContainerTemp;
                m_Object = m_NiftiDataContainerTemp;
            }
            else if (type == typeof(FIF))
            {
                m_ElanDataContainerSubModifier.IsActive = false;
                m_BrainVisionDataContainerSubModifier.IsActive = false;
                m_EDFDataContainerSubModifier.IsActive = false;
                m_MicromedDataContainerSubModifier.IsActive = false;
                m_NiftiDataContainerSubModifier.IsActive = false;
                m_FIFDataContainerSubModifier.IsActive = true;
                m_CSVDataContainerSubModifier.IsActive = false;

                m_FIFDataContainerSubModifier.Object = m_FIFDataContainerTemp;
                m_Object = m_FIFDataContainerTemp;
            }
            else if (type == typeof(CSV))
            {
                m_ElanDataContainerSubModifier.IsActive = false;
                m_BrainVisionDataContainerSubModifier.IsActive = false;
                m_EDFDataContainerSubModifier.IsActive = false;
                m_MicromedDataContainerSubModifier.IsActive = false;
                m_NiftiDataContainerSubModifier.IsActive = false;
                m_FIFDataContainerSubModifier.IsActive = false;
                m_CSVDataContainerSubModifier.IsActive = true;

                m_CSVDataContainerSubModifier.Object = m_CSVDataContainerTemp;
                m_Object = m_CSVDataContainerTemp;
            }
            else
            {
                m_ElanDataContainerSubModifier.IsActive = false;
                m_BrainVisionDataContainerSubModifier.IsActive = false;
                m_EDFDataContainerSubModifier.IsActive = false;
                m_MicromedDataContainerSubModifier.IsActive = false;
                m_NiftiDataContainerSubModifier.IsActive = false;
                m_FIFDataContainerSubModifier.IsActive = false;
                m_CSVDataContainerSubModifier.IsActive = false;
            }
            UpdateDisplayHeaderDataButtonVisibility();
            OnChangeDataType.Invoke();
        }
        void UpdateDisplayHeaderDataButtonVisibility()
        {
            Type containerType = m_Object.GetType();
            bool hasIEEGAttribute = containerType != null && containerType.GetCustomAttributes(typeof(IEEG), false).Length > 0;
            m_DisplayHeaderDataButton.gameObject.SetActive(hasIEEGAttribute);
        }
        async void DisplayHeaderData()
        {
            try
            {
                var errors = Object.GetErrors();
                if (errors.Length > 0)
                {
                    await DialogBoxManager.OpenAsync(Core.Enums.DialogBoxType.Error, "Cannot Display Header", "The data container has errors and cannot be read. Please fix the errors before displaying the header information.", "OK");
                    return;
                }

                Core.DLL.EEG.File.FileType type;
                string[] files;

                if (m_Object is BrainVision brainVisionDataContainer)
                {
                    type = Core.DLL.EEG.File.FileType.BrainVision;
                    files = new string[] { brainVisionDataContainer.Header };
                }
                else if (m_Object is EDF edfDataContainer)
                {
                    type = Core.DLL.EEG.File.FileType.EDF;
                    files = new string[] { edfDataContainer.File };
                }
                else if (m_Object is Elan elanDataContainer)
                {
                    type = Core.DLL.EEG.File.FileType.ELAN;
                    files = new string[] { elanDataContainer.EEG, elanDataContainer.POS, elanDataContainer.Notes };
                }
                else if (m_Object is Micromed micromedDataContainer)
                {
                    type = Core.DLL.EEG.File.FileType.Micromed;
                    files = new string[] { micromedDataContainer.Path };
                }
                else if (m_Object is FIF fifDataContainer)
                {
                    type = Core.DLL.EEG.File.FileType.FIF;
                    files = new string[] { fifDataContainer.File };
                }
                else
                {
                    await DialogBoxManager.OpenAsync(Core.Enums.DialogBoxType.Error, "Invalid Container Type", "The selected data container type does not support header viewing.", "OK");
                    return;
                }

                // Try to load the file
                Core.DLL.EEG.File file = new Core.DLL.EEG.File(type, false, files);

                // Format header information
                System.Text.StringBuilder headerInfo = new System.Text.StringBuilder();
                headerInfo.AppendLine("<b>=== FILE HEADER INFORMATION ===</b>");
                headerInfo.AppendLine();

                // Sampling frequency
                headerInfo.AppendLine($"<b>Sampling Frequency</b>: {file.SamplingFrequency.Value} Hz");
                headerInfo.AppendLine();

                // Electrodes - grouped by electrode name
                if (file.ElectrodeCount > 0)
                {
                    headerInfo.AppendLine($"<b>Channels</b> ({file.ElectrodeCount}):");
                    var electrodes = file.Electrodes;
                    
                    // Group channels by electrode prefix
                    var groupedChannels = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>>();
                    var otherChannels = new System.Collections.Generic.List<string>();
                    
                    foreach (var electrode in electrodes)
                    {
                        string label = electrode.Label;
                        // Extract prefix (all characters except trailing digits)
                        var match = System.Text.RegularExpressions.Regex.Match(label, @"^(.*?)(\d+)$");
                        
                        if (match.Success)
                        {
                            string prefix = match.Groups[1].Value;
                            if (!groupedChannels.ContainsKey(prefix))
                            {
                                groupedChannels[prefix] = new System.Collections.Generic.List<string>();
                            }
                            groupedChannels[prefix].Add(label);
                        }
                        else
                        {
                            // No digits at the end, add to "Other"
                            otherChannels.Add(label);
                        }
                    }
                    
                    // Sort groups by prefix using SiteNameComparer
                    var sortedGroups = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, System.Collections.Generic.List<string>>>(groupedChannels);
                    sortedGroups.Sort((a, b) => new SiteNameComparer().Compare(a.Value[0], b.Value[0]));
                    
                    // Display each group
                    foreach (var group in sortedGroups)
                    {
                        // Sort channels within the group using SiteNameComparer
                        group.Value.Sort(new SiteNameComparer());
                        headerInfo.AppendLine($"  • {group.Key} - {string.Join(", ", group.Value)}");
                    }
                    
                    // Display "Other" channels if any
                    if (otherChannels.Count > 0)
                    {
                        otherChannels.Sort();
                        headerInfo.AppendLine($"  • Other - {string.Join(", ", otherChannels)}");
                    }
                    
                    headerInfo.AppendLine();
                }
                else
                {
                    headerInfo.AppendLine($"<b>No channel found in the header.</b>");
                    headerInfo.AppendLine();
                }

                // Triggers - display statistics
                if (file.TriggerCount > 0)
                {
                    headerInfo.AppendLine($"<b>Triggers</b> ({file.TriggerCount}):");
                    var triggers = file.Triggers;
                    
                    // Group triggers by code and count occurrences
                    var triggerStats = new System.Collections.Generic.Dictionary<int, int>();
                    foreach (var trigger in triggers)
                    {
                        if (!triggerStats.ContainsKey(trigger.Code))
                        {
                            triggerStats[trigger.Code] = 0;
                        }
                        triggerStats[trigger.Code]++;
                    }
                    
                    // Sort by code and display
                    var sortedStats = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<int, int>>(triggerStats);
                    sortedStats.Sort((a, b) => a.Key.CompareTo(b.Key));
                    
                    foreach (var stat in sortedStats)
                    {
                        headerInfo.AppendLine($"  • Code {stat.Key}: {stat.Value} occurrence{(stat.Value > 1 ? "s" : "")}");
                    }
                }
                else
                {
                    headerInfo.AppendLine($"<b>No trigger found in the header.</b>");
                    headerInfo.AppendLine();
                }

                // Display the formatted information
                DialogBoxManager.OpenScrollable(Core.Enums.DialogBoxType.Informational, "File Header Information", headerInfo.ToString(), "OK").Forget();
            }
            catch (Exception ex)
            {
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "Unable to Load File", $"The file could not be loaded. Please check that the file path is correct and the file is valid.\n\nError: {ex.Message}", "OK").Forget();
            }
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(DataContainer objectToDisplay)
        {
            m_ElanDataContainerTemp = new Elan("", "", "", new Error[0], new Warning[0], objectToDisplay.ID);
            m_EDFDataContainerTemp = new EDF("", new Error[0], new Warning[0], objectToDisplay.ID);
            m_BrainVisionDataContainerTemp = new BrainVision("", new Error[0], new Warning[0], objectToDisplay.ID);
            m_MicromedDataContainerTemp = new Micromed("", new Error[0], new Warning[0], objectToDisplay.ID);
            m_NiftiDataContainerTemp = new Nifti("", new Error[0], new Warning[0], objectToDisplay.ID);
            m_FIFDataContainerTemp = new FIF("", new Error[0], new Warning[0], objectToDisplay.ID);
            m_CSVDataContainerTemp = new CSV("", new Error[0], new Warning[0], objectToDisplay.ID);

            if (objectToDisplay is Elan)
            {
                m_ElanDataContainerTemp = objectToDisplay as Elan;
            }
            else if (objectToDisplay is EDF)
            {
                m_EDFDataContainerTemp = objectToDisplay as EDF;
            }
            else if (objectToDisplay is Micromed)
            {
                m_MicromedDataContainerTemp = objectToDisplay as Micromed;
            }
            else if (objectToDisplay is BrainVision)
            {
                m_BrainVisionDataContainerTemp = objectToDisplay as BrainVision;
            }
            else if (objectToDisplay is Nifti)
            {
                m_NiftiDataContainerTemp = objectToDisplay as Nifti;
            }
            else if (objectToDisplay is FIF)
            {
                m_FIFDataContainerTemp = objectToDisplay as FIF;
            }
            else if (objectToDisplay is CSV)
            {
                m_CSVDataContainerTemp = objectToDisplay as CSV;
            }
            m_ContainerTypeDropdown.SetValue(Array.IndexOf(m_Types, Object.GetType()));
            UpdateDisplayHeaderDataButtonVisibility();
        }
        #endregion
    }
}