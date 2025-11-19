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
        
        private IEEGGenerator m_Generator;
        #endregion
        
        #region Public Methods
        public override async void OK()
        {
            // Verify prerequisites
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
            
            // Check if project is opened and if visualization is opened
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
                .OrderBy(p => p.Place)
                .ThenBy(p => p.Date)
                .ThenBy(p => p.Name)
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

            // Liste pour tracker les dataInfos qui échouent au chargement
            var failedDataInfos = new List<(IEEGDataInfo dataInfo, string patientName, string error)>();

            try
            {
                // Initialize the generator
                GeneratorSurface generatorSurface = new GeneratorSurface();
                generatorSurface.Initialize(
                    Object3DManager.MNI.GreyMatter.Both,
                    Object3DManager.MNI.MRI.Volume,
                    120
                );
                
                m_Generator = new IEEGGenerator();
                m_Generator.Initialize(generatorSurface);
                
                var selectedDataNames = m_DataNameItems.Where(d => d.IsSelected).Select(d => d.DataName).ToList();
                var selectedProtocols = m_ProtocolItems.Where(p => p.IsSelected).ToList();
                
                // Calcul du nombre total d'opérations pour le progress
                int totalDataInfosToLoad = 0;
                int totalProtocolsToProcess = 0;
                int totalBlocsToProcess = 0;
                
                foreach (var dataName in selectedDataNames)
                {
                    foreach (var protocolItem in selectedProtocols)
                    {
                        // Compter les dataInfos à charger pour ce protocole/dataName
                        foreach (var patient in m_SelectedPatients)
                        {
                            var patientDataInfos = DatabaseManager.Database.DataInfos.OfType<IEEGDataInfo>()
                                .Where(d => d.Patient == patient && 
                                           d.Protocol.Name == protocolItem.Name && 
                                           d.Name == dataName)
                                .ToList();
                            totalDataInfosToLoad += patientDataInfos.Count;
                        }
                        totalProtocolsToProcess++;
                        totalBlocsToProcess += protocolItem.SelectedBlocs.Count;
                    }
                }
                
                int currentDataInfoLoaded = 0;
                int currentProtocolProcessed = 0;
                int currentBlocProcessed = 0;
                
                // Loop through data names
                foreach (var dataName in selectedDataNames)
                {
                    token.ThrowIfCancellationRequested();
                    
                    // Loop through protocols
                    foreach (var protocolItem in selectedProtocols)
                    {
                        token.ThrowIfCancellationRequested();
                        
                        var selectedBlocNames = protocolItem.SelectedBlocs.Select(b => b.Name).ToList();
                        if (selectedBlocNames.Count == 0) continue;
                        
                        // 1. CHARGER TOUTES LES DONNÉES de tous les patients sélectionnés pour ce protocole/dataname
                        var allDataInfos = new List<IEEGDataInfo>();
                        var dataInfosToRemove = new List<IEEGDataInfo>();
                        
                        foreach (var patient in m_SelectedPatients)
                        {
                            
                            var patientDataInfos = DatabaseManager.Database.DataInfos.OfType<IEEGDataInfo>()
                                .Where(d => d.Patient == patient && 
                                           d.Protocol.Name == protocolItem.Name && 
                                           d.Name == dataName)
                                .ToList();
                            
                            allDataInfos.AddRange(patientDataInfos);
                        }
                        
                        if (allDataInfos.Count == 0) continue;
                        
                        // Load all data for all patients avec gestion des erreurs
                        foreach (var dataInfo in allDataInfos)
                        {
                            try
                            {
                                // Phase 1: Chargement (0% à 50%)
                                float loadingProgress = (float)currentDataInfoLoaded / totalDataInfosToLoad * 0.5f;
                                updateProgress?.Invoke(loadingProgress, 0, 
                                    new LoadingText($"Loading data: {dataInfo.Patient.Name} - {dataInfo.Name}"));
                                
                                DataManager.Load(dataInfo);
                                currentDataInfoLoaded++;
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError($"Failed to load dataInfo for patient {dataInfo.Patient.Name}: {ex.Message}");
                                failedDataInfos.Add((dataInfo, dataInfo.Patient.Name, ex.Message));
                                dataInfosToRemove.Add(dataInfo);
                                currentDataInfoLoaded++; // Compter quand même pour le progress
                            }
                        }
                        
                        // Supprimer les dataInfos qui ont échoué au chargement
                        foreach (var dataInfoToRemove in dataInfosToRemove)
                        {
                            allDataInfos.Remove(dataInfoToRemove);
                        }
                        
                        if (allDataInfos.Count == 0)
                        {
                            Debug.LogWarning($"No valid data loaded for protocol {protocolItem.Name}, dataName {dataName}");
                            continue;
                        }
                        
                        // 2. CRÉER UNE IMPLANTATION GLOBALE pour tous les patients
                        // Phase 2 début: Génération implantation (50% + progression dans les 40%)
                        float implantationProgress = 0.5f + ((float)currentProtocolProcessed / totalProtocolsToProcess * 0.4f * 0.2f); // 20% des 40% pour implantation
                        updateProgress?.Invoke(implantationProgress, 0, 
                            new LoadingText($"Generating global implantation for {protocolItem.Name}"));
                            
                        var globalImplantation3D = GenerateImplantation3D(m_SelectedPatients);
                        if (globalImplantation3D == null || !globalImplantation3D.IsLoaded)
                        {
                            Debug.LogWarning($"Failed to generate global implantation for protocol {protocolItem.Name}");
                            // Clean loaded data before continuing
                            foreach (var dataInfo in allDataInfos)
                            {
                                DataManager.UnLoad(dataInfo);
                            }
                            continue;
                        }
                                                
                        // 3. BOUCLER SUR LES BLOCS pour générer un NIFTI par bloc
                        foreach (var blocName in selectedBlocNames)
                        {
                            token.ThrowIfCancellationRequested();
                            
                            // Phase 2: Traitement blocs (50% à 90% - répartition dans les 40%)
                            float blocBaseProgress = 0.5f + ((float)currentProtocolProcessed / totalProtocolsToProcess * 0.4f * 0.2f); // Base implantation
                            float blocCalculationProgress = blocBaseProgress + ((float)currentBlocProcessed / totalBlocsToProcess * 0.4f * 0.8f); // 80% des 40% pour calculs blocs
                            updateProgress?.Invoke(blocCalculationProgress, 0, 
                                new LoadingText($"Processing bloc: {blocName} ({protocolItem.Name} - {dataName})"));
                            
                            // Find the bloc in the loaded data
                            var bloc = allDataInfos.FirstOrDefault()?.Protocol.Blocs.FirstOrDefault(b => b.Name == blocName);
                            if (bloc == null) 
                            {
                                currentBlocProcessed++;
                                continue;
                            }
                            
                            // 4. GÉNÉRER LES DONNÉES IEEG PROCESSÉES pour ce bloc (tous patients)
                            var processedIEEGData = GenerateProcessedIEEGData(allDataInfos, bloc);
                            if (processedIEEGData == null) 
                            {
                                currentBlocProcessed++;
                                continue;
                            }
                            
                            // Extraire les valeurs d'activité
                            var activityValues = ExtractActivityValues(processedIEEGData, globalImplantation3D);
                            if (activityValues == null) 
                            {
                                currentBlocProcessed++;
                                continue;
                            }
                            
                            // Utiliser la timeline de Processed.IEEGData
                            var timeline = processedIEEGData.Timeline;
                            if (timeline == null) continue;
                            
                            // Compute activity for all patients together
                            m_Generator.ComputeActivity(
                                globalImplantation3D.RawSiteList, 
                                15.0f, // Default influence distance
                                activityValues, 
                                timeline.Length,
                                globalImplantation3D.RawSiteList.NumberOfSites,
                                PersistentDataManager.UserPreferences.Visualization._3D.SiteInfluenceByDistance
                            );
                            
                            // Generate output path (one file per bloc, not per patient)
                            string outputPath = GenerateOutputPath(protocolItem.Name, dataName, blocName);
                            
                            // Phase 3: Écriture (90% à 100% - répartition dans les 10%)
                            float writingProgress = 0.9f + ((float)currentBlocProcessed / totalBlocsToProcess * 0.1f);
                            updateProgress?.Invoke(writingProgress, 0, 
                                new LoadingText($"Writing file: {blocName}.nii.gz"));
                            
                            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                            
                            // Save as NIFTI
                            bool success = m_Generator.SaveActivityAsNifti(
                                outputPath, 
                                timeline.CurrentSubtimeline, 
                                $"Localizer Atlas - {protocolItem.Name} - {dataName} - {blocName}"
                            );

                            success &= m_Generator.SaveMaskAsNifti(
                                outputPath.Replace(".nii.gz", "_mask.nii.gz"),
                                $"Localizer Atlas Mask - {protocolItem.Name} - {dataName} - {blocName}"
                            );

                            if (!success)
                            {
                                throw new HBPException("Export failed", 
                                    $"Failed to export atlas for {protocolItem.Name} - {dataName} - {blocName}");
                            }
                            
                            currentBlocProcessed++;
                        }
                        
                        // Clean implantation3D after protocol is complete
                        globalImplantation3D?.Clean();
                        
                        // Clean DataManager between protocols
                        DataManager.Clear();
                        
                        // Incrémenter le compteur de protocoles traités
                        currentProtocolProcessed++;
                    }
                }
            }
            finally
            {
                m_Generator?.Dispose();
                m_Generator = null;
            }
            
            updateProgress?.Invoke(1.0f, 0, new LoadingText("Export completed successfully!"));
            
            // Afficher les erreurs de chargement s'il y en a
            if (failedDataInfos.Count > 0)
            {
                await UniTask.SwitchToMainThread();
                ShowFailedDataInfosDialog(failedDataInfos);
            }
        }
        
        /// <summary>
        /// Affiche une dialog box avec les dataInfos qui ont échoué au chargement
        /// </summary>
        /// <param name="failedDataInfos">Liste des dataInfos qui ont échoué</param>
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
                
                Debug.Log($"Traitement patient {patient.CompleteName}: {patientSites.Count} sites (index patient: {patientIndex})");
                
                foreach (var site in patientSites)
                {
                    GroupCollection groups = regex.Match(site.Name).Groups;
                    var mniCoordinate = site.Coordinates.FirstOrDefault(c => c.ReferenceSystem == "MNI");
                    
                    if (mniCoordinate == null)
                    {
                        Debug.LogWarning($"Site {site.Name} du patient {patient.CompleteName} n'a pas de coordonnées MNI");
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
            
            // Création de l'Implantation3D avec le constructeur correct
            var implantation3D = new Implantation3D("MNI", siteInfos, patients);
            
            Debug.Log($"Implantation3D globale créée: {siteInfos.Count} sites de {patients.Count} patients");
            return implantation3D.IsLoaded ? implantation3D : null;
        }
        
        private Core.Data.Processed.IEEGData GenerateProcessedIEEGData(List<IEEGDataInfo> allDataInfos, Bloc bloc)
        {
            if (allDataInfos == null || allDataInfos.Count == 0 || bloc == null) return null;
            
            try
            {
                // Création d'une instance de Processed.IEEGData
                var processedIEEGData = new Core.Data.Processed.IEEGData();
                
                // 1. Load des données - similaire à IEEGColumn.Data.Load dans Visualization
                processedIEEGData.Load(allDataInfos, bloc);
                
                // 2. Calcul de la fréquence maximale - similaire à Visualization.LoadColumnsAsync
                var maxFrequency = new Frequency(allDataInfos.Max(dataInfo => 
                {
                    var data = DataManager.GetData(dataInfo) as Core.Data.IEEGData;
                    return data?.Frequency.RawValue ?? 0;
                }));
                
                // 3. SetTimeline - similaire à IEEGColumn.Data.SetTimeline dans Visualization  
                var allBlocs = allDataInfos.Select(d => d.Protocol.Blocs).SelectMany(b => b).Distinct();
                processedIEEGData.SetTimeline(maxFrequency, bloc, allBlocs);
                
                Debug.Log($"Processed.IEEGData généré: {processedIEEGData.DataByChannelID.Count} canaux de {allDataInfos.Count} patients/dataInfos");
                return processedIEEGData;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Erreur lors de la génération des données IEEG processées: {ex.Message}");
                return null;
            }
        }
        
        private float[] ExtractActivityValues(Core.Data.Processed.IEEGData processedIEEGData, Implantation3D implantation)
        {
            if (processedIEEGData?.ProcessedValuesByChannel == null) return null;
            
            try
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
            catch (Exception ex)
            {
                Debug.LogError($"Erreur lors de l'extraction des valeurs d'activité: {ex.Message}");
                return null;
            }
        }
        
        private string GenerateOutputPath(string protocolName, string dataName, string blocName)
        {
            string exportFolder = m_ExportFolderSelector.Folder;
            string localizersFolder = Path.Combine(exportFolder, "Localizers");
            string protocolFolder = Path.Combine(localizersFolder, protocolName);
            string dataFolder = Path.Combine(protocolFolder, dataName);
            
            return Path.Combine(dataFolder, $"{blocName}.nii.gz");
        }
        #endregion
    }
}