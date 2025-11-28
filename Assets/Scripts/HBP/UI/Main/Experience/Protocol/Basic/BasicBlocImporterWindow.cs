using HBP.Core.Data;
using HBP.Core.Data.Container;
using HBP.UI.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Main
{
    public class BasicBlocImporterWindow : DialogWindow
    {
        #region Properties
        [SerializeField] private Button m_PreviousButton;
        [SerializeField] private Button m_NextButton;
        [SerializeField] private Button m_FinishButton;
        
        [SerializeField] private BasicBlocImporterPanel[] m_Panels;
        
        public BlocImporterData Data { get; private set; } = new BlocImporterData();

        private string m_FilePath;
        public string FilePath
        {
            get => m_FilePath;
            set
            {
                m_FilePath = value;
                SetFilePath(m_FilePath);
            }
        }

        private int m_CurrentPanelIndex = 0;
        #endregion

        #region Events
        public GenericEvent<Bloc[]> OnBlocsImported = new();
        #endregion

        #region Private Methods
        protected override void Initialize()
        {
            base.Initialize();
            
            m_PreviousButton.onClick.AddListener(GoToPreviousPanel);
            m_NextButton.onClick.AddListener(GoToNextPanel);
            m_FinishButton.onClick.AddListener(FinishImport);
            
            foreach (var panel in m_Panels)
            {
                panel.Initialize(Data);
                panel.OnUpdateNavigation.AddListener(UpdateButtonStates);
                panel.gameObject.SetActive(false);
            }
            
            UpdateButtonStates();
            ShowPanel(0);
        }
        private void SetFilePath(string filePath)
        {
            Data.Clear();
            LoadEvents(filePath);
            RefreshCurrentPanel();
        }
        private void LoadEvents(string filePath)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            Core.DLL.EEG.File.FileType type;
            string[] files;

            if (string.Equals(fileInfo.Extension, BrainVision.HEADER_EXTENSION, StringComparison.OrdinalIgnoreCase))
            {
                type = Core.DLL.EEG.File.FileType.BrainVision;
                files = new string[] { filePath };
            }
            else if (string.Equals(fileInfo.Extension, EDF.EDF_EXTENSION, StringComparison.OrdinalIgnoreCase))
            {
                type = Core.DLL.EEG.File.FileType.EDF;
                files = new string[] { filePath };
            }
            else if (string.Equals(fileInfo.Extension, Elan.POS_EXTENSION, StringComparison.OrdinalIgnoreCase))
            {
                type = Core.DLL.EEG.File.FileType.ELAN;
                files = new string[] { "", filePath, "" };
            }
            else if (string.Equals(fileInfo.Extension, Micromed.MICROMED_EXTENSION, StringComparison.OrdinalIgnoreCase))
            {
                type = Core.DLL.EEG.File.FileType.Micromed;
                files = new string[] { filePath };
            }
            else if (string.Equals(fileInfo.Extension, FIF.FIF_EXTENSION, StringComparison.OrdinalIgnoreCase))
            {
                type = Core.DLL.EEG.File.FileType.FIF;
                files = new string[] { filePath };
            }
            else
            {
                throw new Exception("Invalid data container type");
            }
            
            Core.DLL.EEG.File file = new Core.DLL.EEG.File(type, false, files);
            List<Core.DLL.EEG.Trigger> triggers = file.Triggers;

            if (triggers.Count == 0)
            {
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "No triggers found", "The selected file does not contain any triggers.").Forget();
                Close();
                return;
            }

            foreach (var uniqueCode in triggers.Select(t => t.Code).Distinct())
            {
                Data.OccurencesByCode[uniqueCode] = 0;
            }
            foreach (var trigger in triggers)
            {
                Data.OccurencesByCode[trigger.Code]++;
            }
        }
        private void ShowPanel(int index)
        {
            for (int i = 0; i < m_Panels.Length; i++)
            {
                m_Panels[i].gameObject.SetActive(i == index);
            }
            m_CurrentPanelIndex = index;
            RefreshCurrentPanel();
            UpdateButtonStates();
        }
        private void RefreshCurrentPanel()
        {
            if (m_CurrentPanelIndex < m_Panels.Length)
            {
                m_Panels[m_CurrentPanelIndex].Refresh();
            }
        }
        private void UpdateButtonStates()
        {
            m_PreviousButton.interactable = m_CurrentPanelIndex > 0;
            
            bool showNext = false;
            bool showFinish = false;
            bool secondButtonEnabled = false;
            
            switch (m_CurrentPanelIndex)
            {
                case 0: // Panel 1: Code selection
                    showNext = true;
                    secondButtonEnabled = m_Panels[m_CurrentPanelIndex].CanProceed();
                    break;
                    
                case 1: // Panel 2: Bloc naming
                    showNext = true;
                    secondButtonEnabled = m_Panels[m_CurrentPanelIndex].CanProceed();
                    break;
                    
                case 2: // Panel 3: Response code selection
                    // Show Finish if no response codes selected, otherwise show Next
                    if (Data.SelectedResponseCodes.Count == 0)
                    {
                        showFinish = true;
                        secondButtonEnabled = true; // Can always finish if no response codes
                    }
                    else
                    {
                        showNext = true;
                        secondButtonEnabled = true; // Can always proceed to next if response codes selected
                    }
                    break;
                    
                case 3: // Panel 4: Response assignment
                    showFinish = true;
                    secondButtonEnabled = m_Panels[m_CurrentPanelIndex].CanProceed();
                    break;
                    
                default:
                    // Fallback for any additional panels
                    if (m_CurrentPanelIndex < m_Panels.Length - 1)
                    {
                        showNext = true;
                        secondButtonEnabled = m_Panels[m_CurrentPanelIndex].CanProceed();
                    }
                    else
                    {
                        showFinish = true;
                        secondButtonEnabled = m_Panels[m_CurrentPanelIndex].CanProceed();
                    }
                    break;
            }
            
            // Apply button states
            m_NextButton.gameObject.SetActive(showNext);
            m_NextButton.interactable = secondButtonEnabled;
            
            m_FinishButton.gameObject.SetActive(showFinish);
            m_FinishButton.interactable = secondButtonEnabled;
        }
        private void GoToPreviousPanel()
        {
            if (m_CurrentPanelIndex > 0)
            {
                ShowPanel(m_CurrentPanelIndex - 1);
            }
        }
        private void GoToNextPanel()
        {
            if (m_CurrentPanelIndex < m_Panels.Length - 1 && m_Panels[m_CurrentPanelIndex].CanProceed())
            {
                m_Panels[m_CurrentPanelIndex].OnProceed();
                ShowPanel(m_CurrentPanelIndex + 1);
            }
        }
        private void FinishImport()
        {
            if (m_Panels[m_CurrentPanelIndex].CanProceed())
            {
                m_Panels[m_CurrentPanelIndex].OnProceed();

                // Process data and create blocs
                Data.ProcessBlocNames();
                List<Bloc> blocs = CreateBlocsFromData();
                OnBlocsImported.Invoke(blocs.ToArray());
                Close();
            }
        }
        private List<Bloc> CreateBlocsFromData()
        {
            List<Bloc> blocs = new List<Bloc>();
            
            for (int i = 0; i < Data.CreatedBlocs.Count; i++)
            {
                var blocData = Data.CreatedBlocs[i];
                
                // Create main event with all main codes
                var mainEvent = new Core.Data.Event(Core.Enums.MainSecondaryEnum.Main)
                {
                    Name = blocData.Name,
                    CodesString = string.Join(",", blocData.MainCodes)
                };

                // Create sub-bloc
                var subBloc = new SubBloc()
                {
                    Name = "Main",
                    Order = 0,
                    Type = Core.Enums.MainSecondaryEnum.Main,
                    Events = new List<Core.Data.Event> { mainEvent }
                };

                // Add response events if any
                if (blocData.ResponseCodes.Count > 0)
                {
                    var responseEvent = new Core.Data.Event(Core.Enums.MainSecondaryEnum.Secondary)
                    {
                        Name = "RESPONSE",
                        CodesString = string.Join(",", blocData.ResponseCodes)
                    };
                    subBloc.Events.Add(responseEvent);
                }

                // Create bloc
                var bloc = new Bloc()
                {
                    Name = blocData.Name,
                    Order = i,
                    SubBlocs = new List<SubBloc> { subBloc }
                };

                blocs.Add(bloc);
            }
            
            return blocs;
        }
        #endregion
    }
}