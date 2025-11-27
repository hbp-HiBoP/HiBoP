using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Data.Processed;
using HBP.Core.DLL;
using HBP.Core.Enums;
using HBP.Core.Exceptions;
using HBP.Core.Object3D;
using HBP.Core.Tools;
using HBP.Data.Database;
using HBP.Data.Module3D;
using HBP.Data.Preferences;
using HBP.UI.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Main
{
    public class ExportLocalizerAtlasWindow : DialogWindow
    {
        #region Properties
        [SerializeField] private Button m_SelectPatientsButton;
        [SerializeField] private Text m_PatientsSelectedText;
        
        [SerializeField] private Transform m_ProtocolsContainer;
        [SerializeField] private GameObject m_ProtocolItemPrefab;
        
        [SerializeField] private Transform m_DataNamesContainer;
        [SerializeField] private GameObject m_DataNameItemPrefab;
        
        [SerializeField] private FolderSelector m_ExportFolderSelector;
        
        private List<Patient> m_AvailablePatients = new List<Patient>();
        private List<Patient> m_SelectedPatients = new List<Patient>();
        private List<ExportProtocolItem> m_ProtocolItems = new List<ExportProtocolItem>();
        private List<ExportDataNameItem> m_DataNameItems = new List<ExportDataNameItem>();
        #endregion
        
        #region Public Methods
        public override async void OK()
        {
            if (m_SelectedPatients.Count == 0)
            {
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "No patients selected", "Please select at least one patient.").Forget();
                return;
            }
            
            var selectedProtocols = m_ProtocolItems.Where(p => p.IsSelected).ToList();
            if (selectedProtocols.Count == 0)
            {
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "No protocols selected", "Please select at least one protocol block.").Forget();
                return;
            }
            
            var selectedDataNames = m_DataNameItems.Where(d => d.IsSelected).ToList();
            if (selectedDataNames.Count == 0)
            {
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "No data selected", "Please select at least one data type.").Forget();
                return;
            }
            
            if (!Directory.Exists(m_ExportFolderSelector.Folder))
            {
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "Invalid output folder", "The specified output folder does not exist.").Forget();
                return;
            }
            
            if (ApplicationState.LoadedProject != null)
            {
                if (ApplicationState.LoadedProject.Visualizations.Any(v => Module3DMain.Visualizations.Contains(v)))
                {
                    int result = await DialogBoxManager.OpenAsync(Core.Enums.DialogBoxType.Warning, "Opened visualizations", "Some visualizations are currently opened. It's recommended to close them before export to avoid conflicts.\n\nWould you like to close them and continue?", "Close and Continue", "Cancel");
                    if (result == 0)
                    {
                        Module3DMain.RemoveAllScenes();
                    }
                    else
                    {
                        return;
                    }
                }
            }
            
            base.OK();

            await LoadingManager.LoadAsync(ExportAtlasAsync);
            DialogBoxManager.Open(Core.Enums.DialogBoxType.Informational, "Export complete", "The export of localizer atlas is complete.").Forget();
        }
        #endregion
        
        #region Protected Methods
        protected override void Initialize()
        {
            base.Initialize();
            
            m_SelectPatientsButton.onClick.AddListener(OpenPatientSelector);
        }
        protected override void SetFields()
        {
            base.SetFields();

            m_ExportFolderSelector.Folder = PersistentDataManager.UserPreferences.General.Project.DefaultExportLocation;
            SetAvailablePatients();
            SetupProtocols();
            SetupDataNames();
            UpdateUI();
        }
        #endregion
        
        #region Private Methods
        private void SetAvailablePatients()
        {
            m_AvailablePatients = DatabaseManager.Database.Patients
                .Where(p => DatabaseManager.Database.DataInfos.OfType<IEEGDataInfo>()
                .Any(d => d.Patient == p))
                .OrderBy(p => p.Name)
                .ToList();
        }
        private void SetupProtocols()
        {
            // Clear existing protocol items
            foreach (var item in m_ProtocolItems)
            {
                if (item != null) Destroy(item.gameObject);
            }
            m_ProtocolItems.Clear();
            
            // Get all protocols from database
            var protocols = DatabaseManager.Database.Protocols.OrderBy(p => p.Name).ToList();
            foreach (var protocol in protocols)
            {
                GameObject itemObj = Instantiate(m_ProtocolItemPrefab, m_ProtocolsContainer);
                ExportProtocolItem item = itemObj.GetComponent<ExportProtocolItem>();
                if (item != null)
                {
                    item.Initialize(protocol);
                    item.OnToggleChanged.AddListener(UpdateUI);
                    m_ProtocolItems.Add(item);
                }
            }
        }
        private void SetupDataNames()
        {
            // Clear existing data name items
            foreach (var item in m_DataNameItems)
            {
                if (item != null) Destroy(item.gameObject);
            }
            m_DataNameItems.Clear();
            
            // Get all distinct data names from database
            var dataNames = DatabaseManager.Database.DataInfos
                .OfType<IEEGDataInfo>()
                .Select(d => d.Name)
                .Distinct()
                .OrderBy(name => name)
                .ToList();
            
            foreach (var dataName in dataNames)
            {
                GameObject itemObj = Instantiate(m_DataNameItemPrefab, m_DataNamesContainer);
                ExportDataNameItem item = itemObj.GetComponent<ExportDataNameItem>();
                if (item != null)
                {
                    item.Initialize(dataName);
                    item.OnToggleChanged.AddListener(UpdateUI);
                    m_DataNameItems.Add(item);
                }
            }
        }
        private void OpenPatientSelector()
        {
            ObjectSelector<Patient> selector = WindowsManager.OpenSelector(m_AvailablePatients, this);
            selector.ObjectsSelected = m_SelectedPatients.ToArray();
            selector.OnOk.AddListener(() => OnPatientsSelected(selector.ObjectsSelected));
            WindowsReferencer.Add(selector);
        }
        private void OnPatientsSelected(Patient[] selectedPatients)
        {
            m_SelectedPatients = selectedPatients.ToList();
            UpdateUI();
        }
        private void UpdateUI()
        {
            // Update patients text
            if (m_SelectedPatients.Count == 0)
            {
                m_PatientsSelectedText.text = "No patients selected";
            }
            else if (m_SelectedPatients.Count == 1)
            {
                m_PatientsSelectedText.text = "1 patient selected";
            }
            else
            {
                m_PatientsSelectedText.text = $"{m_SelectedPatients.Count} patients selected";
            }
            
            // Enable/disable export button
            bool canExport = m_SelectedPatients.Count > 0 &&
                           m_ProtocolItems.Any(p => p.IsSelected) &&
                           m_DataNameItems.Any(d => d.IsSelected) &&
                           !string.IsNullOrEmpty(m_ExportFolderSelector.Folder);
            
            m_OKButton.interactable = canExport;
        }

        private async UniTask ExportAtlasAsync(Action<float, float, LoadingText> updateProgress, CancellationToken token)
        {
            await UniTask.SwitchToThreadPool();

            var failedDataInfos = new List<(IEEGDataInfo dataInfo, string patientName, string error)>();

            // Initialize generator
            GeneratorSurface generatorSurface = new GeneratorSurface();
            generatorSurface.Initialize(Object3DManager.MNI.GreyMatter.Both, Object3DManager.MNI.MRI.Volume, 120);
            IEEGGenerator generator = new IEEGGenerator();
            generator.Initialize(generatorSurface);

            var selectedDataNames = m_DataNameItems.Where(d => d.IsSelected).Select(d => d.DataName).ToList();
            var selectedProtocols = m_ProtocolItems.Where(p => p.IsSelected).ToList();

            // Compute number of steps for progress reporting
            updateProgress.Invoke(0, 0, new LoadingText("Initialization"));

            int totalDataInfosToLoad = 0;
            int totalBlocsToProcess = 0;
            foreach (var dataName in selectedDataNames)
            {
                foreach (var protocolItem in selectedProtocols)
                {
                    foreach (var patient in m_SelectedPatients)
                    {
                        totalDataInfosToLoad += DatabaseManager.Database.DataInfos.OfType<IEEGDataInfo>().Count(d => d.Patient == patient && d.Protocol.Name == protocolItem.Name && d.Name == dataName);
                    }
                    totalBlocsToProcess += protocolItem.SelectedBlocs.Count;
                }
            }

            float dataInfoLoadProgress = (1f / totalDataInfosToLoad) * 0.5f;
            float blocProcessProgress = (1f / totalBlocsToProcess) * 0.4f;
            float writeProgress = (1 / totalBlocsToProcess) * 0.1f;
            float progress = 0;

            foreach (var dataName in selectedDataNames)
            {
                foreach (var protocolItem in selectedProtocols)
                {
                    token.ThrowIfCancellationRequested();

                    // Find dataInfos for all selected patients for this protocol and data name
                    var allDataInfos = new List<IEEGDataInfo>();
                    foreach (var patient in m_SelectedPatients)
                    {
                        var patientDataInfos = DatabaseManager.Database.DataInfos.OfType<IEEGDataInfo>().Where(d => d.Patient == patient && d.Protocol.Name == protocolItem.Name && d.Name == dataName).ToList();
                        allDataInfos.AddRange(patientDataInfos);
                    }

                    if (allDataInfos.Count == 0) continue;

                    // Load all data
                    var dataInfosToRemove = new List<IEEGDataInfo>();
                    foreach (var dataInfo in allDataInfos)
                    {
                        try
                        {
                            progress += dataInfoLoadProgress;
                            updateProgress.Invoke(progress, 0, new LoadingText($"Loading data: {dataInfo.Patient.Name} - {dataInfo.Protocol.Name} - {dataInfo.Name}"));
                            DataManager.Load(dataInfo);
                        }
                        catch (Exception ex)
                        {
                            failedDataInfos.Add((dataInfo, dataInfo.Patient.Name, ex.Message));
                            dataInfosToRemove.Add(dataInfo);
                        }
                    }

                    // Remove failed dataInfos
                    foreach (var dataInfoToRemove in dataInfosToRemove)
                    {
                        allDataInfos.Remove(dataInfoToRemove);
                    }

                    if (allDataInfos.Count == 0)
                    {
                        Debug.LogWarning($"No valid data loaded for protocol {protocolItem.Name}, dataName {dataName}");
                        continue;
                    }

                    var globalImplantation3D = GenerateImplantation3D(m_SelectedPatients);

                    foreach (var blocItem in protocolItem.SelectedBlocs)
                    {
                        token.ThrowIfCancellationRequested();

                        progress += blocProcessProgress;
                        updateProgress.Invoke(progress, 0, new LoadingText($"Processing bloc: {blocItem.Name} ({protocolItem.Name} - {dataName})"));

                        var bloc = allDataInfos.FirstOrDefault()?.Protocol.Blocs.FirstOrDefault(b => b.Name == blocItem.Name);
                        if (bloc == null) continue;

                        var processedIEEGData = GenerateProcessedIEEGData(allDataInfos, bloc);
                        if (processedIEEGData == null) continue;

                        var activityValues = ExtractActivityValues(processedIEEGData, globalImplantation3D);
                        if (activityValues == null) continue;

                        generator.ComputeActivity(globalImplantation3D.RawSiteList, 15.0f, activityValues, processedIEEGData.Timeline.Length, globalImplantation3D.RawSiteList.NumberOfSites, PersistentDataManager.UserPreferences.Visualization._3D.SiteInfluenceByDistance);

                        string outputPath = GenerateOutputPath(protocolItem.Name, dataName, blocItem.Name);

                        progress += writeProgress;
                        updateProgress?.Invoke(progress, 0, new LoadingText($"Writing file: {Path.Combine(protocolItem.Name, dataName, blocItem.Name)}.nii.gz"));

                        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

                        bool success = generator.SaveActivityAsNifti(outputPath, processedIEEGData.Timeline.CurrentSubtimeline, $"Localizer Atlas - {protocolItem.Name} - {dataName} - {blocItem.Name}");
                        success &= generator.SaveMaskAsNifti(outputPath.Replace(".nii.gz", "_mask.nii.gz"), $"Localizer Atlas Mask - {protocolItem.Name} - {dataName} - {blocItem.Name}");

                        if (!success)
                        {
                            throw new HBPException("Export failed", $"Failed to export atlas for {protocolItem.Name} - {dataName} - {blocItem.Name}");
                        }
                    }

                    // Clean DataManager between protocols
                    DataManager.Clear();
                }
            }

            updateProgress?.Invoke(1.0f, 0, new LoadingText("Export completed successfully"));

            if (failedDataInfos.Count > 0)
            {
                await UniTask.SwitchToMainThread();
                ShowFailedDataInfosDialog(failedDataInfos);
            }
        }
        private Implantation3D GenerateImplantation3D(List<Patient> patients)
        {
            if (patients.Count == 0) return null;
            
            var siteInfos = new List<Implantation3D.SiteInfo>();
            Regex regex = new(@"^([a-zA-Z']+)([0-9]+)$");
            int globalSiteIndex = 0;
            
            foreach (var patient in patients)
            {
                var patientIndex = patients.IndexOf(patient);
                var patientSites = patient.Sites;
                foreach (var site in patientSites)
                {
                    GroupCollection groups = regex.Match(site.Name).Groups;
                    var mniCoordinate = site.Coordinates.FirstOrDefault(c => c.ReferenceSystem == "MNI");
                    
                    if (mniCoordinate == null)
                    {
                        continue;
                    }
                    
                    var siteInfo = new Implantation3D.SiteInfo
                    {
                        Name = site.Name,
                        Position = mniCoordinate.Position.ToVector3(),
                        Index = globalSiteIndex++,
                        PatientIndex = patientIndex,
                        Patient = patient,
                        Electrode = groups.Count == 3 ? groups[1].ToString() : "Other",
                        SiteData = site
                    };
                    
                    siteInfos.Add(siteInfo);
                }
            }
            
            return new Implantation3D("MNI", siteInfos, patients);
        }
        private Core.Data.Processed.IEEGData GenerateProcessedIEEGData(List<IEEGDataInfo> allDataInfos, Bloc bloc)
        {
            if (allDataInfos == null || allDataInfos.Count == 0 || bloc == null) return null;
            var processedIEEGData = new Core.Data.Processed.IEEGData();
            processedIEEGData.Load(allDataInfos, bloc);
            var maxFrequency = new Frequency(allDataInfos.Max(dataInfo => (DataManager.GetData(dataInfo) as Core.Data.IEEGData)?.Frequency.RawValue ?? 0));
            var allBlocs = allDataInfos.Select(d => d.Protocol.Blocs).SelectMany(b => b).Distinct();
            processedIEEGData.SetTimeline(maxFrequency, bloc, allBlocs);
            return processedIEEGData;
        }
        private float[] ExtractActivityValues(Core.Data.Processed.IEEGData processedIEEGData, Implantation3D implantation)
        {
            int timelineLength = processedIEEGData.Timeline.Length;
            int sitesCount = implantation.SiteInfos.Count;
            var activityValuesBySiteID = new float[sitesCount][];
            foreach (var siteInfo in implantation.SiteInfos)
            {
                if (processedIEEGData.ProcessedValuesByChannel.TryGetValue($"{siteInfo.Patient.ID}_{siteInfo.Name}", out float[] values))
                {
                    if (values.Length > 0)
                    {
                        activityValuesBySiteID[siteInfo.Index] = values;
                        implantation.RawSiteList.UpdateMask(siteInfo.Index, false);
                    }
                    else
                    {
                        activityValuesBySiteID[siteInfo.Index] = new float[timelineLength];
                        implantation.RawSiteList.UpdateMask(siteInfo.Index, true);
                    }
                }
                else
                {
                    activityValuesBySiteID[siteInfo.Index] = new float[timelineLength];
                    implantation.RawSiteList.UpdateMask(siteInfo.Index, true);
                }
            }

            var allValues = new float[timelineLength * sitesCount];
            for (int s = 0; s < sitesCount; ++s)
            {
                for (int t = 0; t < timelineLength; ++t)
                {
                    float val = activityValuesBySiteID[s][t];
                    allValues[t * sitesCount + s] = val;
                }
            }
            return allValues;
        }
        private string GenerateOutputPath(string protocolName, string dataName, string blocName)
        {
            string exportFolder = m_ExportFolderSelector.Folder;
            string localizersFolder = Path.Combine(exportFolder, "Localizers");
            string protocolFolder = Path.Combine(localizersFolder, protocolName);
            string dataFolder = Path.Combine(protocolFolder, dataName);
            return Path.Combine(dataFolder, $"{blocName}.nii.gz");
        }
        private void ShowFailedDataInfosDialog(List<(IEEGDataInfo dataInfo, string patientName, string error)> failedDataInfos)
        {
            var message = $"{failedDataInfos.Count} data could not be loaded and have been skipped:\n\n";
            foreach (var (dataInfo, patientName, error) in failedDataInfos)
            {
                message += $"• Patient: {patientName}, Protocol: {dataInfo.Protocol.Name}, Data: {dataInfo.Name}\nError: {error}\n\n";
            }
            message += "The atlases have been exported only with valid data.";

            DialogBoxManager.OpenScrollable(DialogBoxType.Warning, "Loading Errors", message, "OK").Forget();
        }
        #endregion
    }
}