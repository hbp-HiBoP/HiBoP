using HBP.Core.Data;
using HBP.Core.Tools;
using HBP.UI.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Cysharp.Threading.Tasks;

namespace HBP.Data.Database
{
    public class GlobalDatabase
    {
        #region Properties
        private GlobalDatabaseSettings m_Settings = new();
        public GlobalDatabaseSettings Settings => m_Settings;

        private List<Protocol> m_Protocols = new();
        public ReadOnlyCollection<Protocol> Protocols => new(m_Protocols);

        private List<DatabaseReference> m_DatabaseReferences = new();
        public ReadOnlyCollection<DatabaseReference> DatabaseReferences => new(m_DatabaseReferences);

        private List<Patient> m_Patients = new();
        public ReadOnlyCollection<Patient> Patients => new(m_Patients);

        private List<Dataset> m_Datasets = new();
        public ReadOnlyCollection<Dataset> Datasets => new(m_Datasets);

        public bool IsLoaded { get; private set; } = false;
        #endregion

        #region Getters/Setters
        public void SetProtocols(IEnumerable<Protocol> protocols)
        {
            m_Protocols = protocols.ToList();
        }
        public void SetDatabaseReferences(IEnumerable<DatabaseReference> databaseReferences)
        {
            m_DatabaseReferences = databaseReferences.ToList();
        }
        #endregion

        #region Public Methods
        public static GlobalDatabase Initialize()
        {
            GlobalDatabase database = new();
            if (!new DirectoryInfo(ApplicationState.DatabasePath).Exists) Directory.CreateDirectory(ApplicationState.DatabasePath);
            database.LoadSettings();
            if (!database.Settings.IsFirstUse)
            {
                CopyDefaultDatabase();
                database.SaveSettings();
            }
            database.InitializeDatabase();
            return database;
        }
        public async void SaveProtocols()
        {
            await SaveProtocolsAsync();
        } 
        public async void SaveDatabaseReferences()
        {
            await SaveDatabaseReferencesAsync();
        }

        public async void LoadDatabase()
        {
            await LoadDatabaseAsync();
        }
        public void UpdateDatabases(IEnumerable<DatabaseReference> databaseReferences, UnityAction onUpdated)
        {
            LoadingManager.Load((update) => UpdateDatabasesAsync(databaseReferences, update, onUpdated));
        }
        #endregion

        #region Private Methods
        private static void CopyDefaultDatabase()
        {
            DirectoryInfo defaultDatabaseDirectory = new DirectoryInfo(Path.Combine(ApplicationState.DataPath, "DefaultDatabase"));
            defaultDatabaseDirectory.CopyFilesRecursively(new DirectoryInfo(ApplicationState.DatabasePath));
        }

        private void LoadSettings()
        {
            if (new FileInfo(GlobalDatabaseSettings.PATH).Exists)
            {
                try
                {
                    m_Settings = ClassLoaderSaver.LoadFromJson<GlobalDatabaseSettings>(GlobalDatabaseSettings.PATH);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    throw e;
                }
            }
        }
        private void SaveSettings()
        {
            m_Settings.IsFirstUse = true;
            ClassLoaderSaver.SaveToJSon(m_Settings, GlobalDatabaseSettings.PATH, true);
        }

        private async void InitializeDatabase()
        {
            await LoadProtocolsAsync();
            await LoadDatabaseReferencesAsync();
            LoadDatabase();
        }
        private async UniTask LoadDatabaseAsync()
        {
            System.Diagnostics.Stopwatch stopwatch = new();
            stopwatch.Start();
            await LoadPatientsAsync();
            await LoadDatasetsAsync();
            IsLoaded = true;
            stopwatch.Stop();
            Debug.Log("Database loaded in " + stopwatch.ElapsedMilliseconds + " ms");
        }

        private async UniTask LoadProtocolsAsync()
        {
            List<Protocol> protocols = new List<Protocol>();
            DirectoryInfo protocolDirectory = new DirectoryInfo(Path.Combine(ApplicationState.DatabasePath, "Protocols"));
            if (!protocolDirectory.Exists) protocolDirectory.Create();
            FileInfo[] protocolFiles = protocolDirectory.GetFiles("*" + Protocol.EXTENSION, SearchOption.TopDirectoryOnly);
            m_Protocols = (await UniTask.WhenAll(protocolFiles.Select(pf => ClassLoaderSaver.LoadFromJsonAsync<Protocol>(pf.FullName)))).ToList();
        }
        private async UniTask SaveProtocolsAsync()
        {
            DirectoryInfo protocolDirectory = Directory.CreateDirectory(Path.Combine(ApplicationState.DatabasePath, "Protocols"));
            DirectoryInfo protocolTempDirectory = Directory.CreateDirectory(Path.Combine(ApplicationState.DatabasePath, "ProtocolsTemp"));
            await UniTask.WhenAll(m_Protocols.Select(p => ClassLoaderSaver.SaveToJSonAsync(p, Path.Combine(protocolTempDirectory.FullName, p.Name + Protocol.EXTENSION), true)));
            protocolDirectory.Delete(true);
            protocolTempDirectory.MoveTo(protocolDirectory.FullName);
        }

        private async UniTask LoadDatabaseReferencesAsync()
        {
            List<DatabaseReference> databaseReferences = new List<DatabaseReference>();
            DirectoryInfo referencesDirectory = Directory.CreateDirectory(Path.Combine(ApplicationState.DatabasePath, "References"));
            if (!referencesDirectory.Exists) referencesDirectory.Create();
            FileInfo[] referenceFiles = referencesDirectory.GetFiles("*" + DatabaseReference.EXTENSION, SearchOption.TopDirectoryOnly);
            m_DatabaseReferences = (await UniTask.WhenAll(referenceFiles.Select(rf => ClassLoaderSaver.LoadFromJsonAsync<DatabaseReference>(rf.FullName)))).ToList();
        }
        private async UniTask SaveDatabaseReferencesAsync()
        {
            DirectoryInfo referencesDirectory = Directory.CreateDirectory(Path.Combine(ApplicationState.DatabasePath, "References"));
            DirectoryInfo referencesTempDirectory = Directory.CreateDirectory(Path.Combine(ApplicationState.DatabasePath, "ReferencesTemp"));
            await UniTask.WhenAll(m_DatabaseReferences.Select(dr => ClassLoaderSaver.SaveToJSonAsync(dr, Path.Combine(referencesTempDirectory.FullName, dr.Name + DatabaseReference.EXTENSION), true)));
            referencesDirectory.Delete(true);
            referencesTempDirectory.MoveTo(referencesDirectory.FullName);
            // Remove patients and datasets that are not in the database references
            // TODO : warn the user that patients and datasets will be deleted
            m_Patients.RemoveAll(p => !m_DatabaseReferences.Any(r => r.ID == p.CorrespondingDatabaseID));
            foreach (var dataset in m_Datasets) dataset.RemoveData(dataset.Data.Where(d => m_DatabaseReferences.All(r => r.ID != d.CorrespondingDatabaseID) || (d is PatientDataInfo pd && !m_Patients.Contains(pd.Patient))).ToList());
            m_Datasets.RemoveAll(d => d.Data.Count == 0);
            await SavePatientsAsync();
            await SaveDatasetsAsync();
        }

        private async UniTask LoadPatientsAsync()
        {
            List<Patient> patients = new List<Patient>();
            DirectoryInfo patientsDirectory = new DirectoryInfo(Path.Combine(ApplicationState.DatabasePath, "Patients"));
            if (!patientsDirectory.Exists) patientsDirectory.Create();
            FileInfo[] patientFiles = patientsDirectory.GetFiles("*" + Patient.EXTENSION, SearchOption.TopDirectoryOnly);
            m_Patients = (await UniTask.WhenAll(patientFiles.Select(pf => ClassLoaderSaver.LoadFromJsonAsync<Patient>(pf.FullName)))).ToList();
        }
        private async UniTask SavePatientsAsync()
        {
            DirectoryInfo patientsDirectory = Directory.CreateDirectory(Path.Combine(ApplicationState.DatabasePath, "Patients"));
            DirectoryInfo patientsTempDirectory = Directory.CreateDirectory(Path.Combine(ApplicationState.DatabasePath, "PatientsTemp"));
            await UniTask.WhenAll(m_Patients.Select(p => ClassLoaderSaver.SaveToJSonAsync(p, Path.Combine(patientsTempDirectory.FullName, p.ID + Patient.EXTENSION), true)));
            patientsDirectory.Delete(true);
            patientsTempDirectory.MoveTo(patientsDirectory.FullName);
        }

        private async UniTask LoadDatasetsAsync()
        {
            List<Dataset> datasets = new List<Dataset>();
            DirectoryInfo datasetsDirectory = new DirectoryInfo(Path.Combine(ApplicationState.DatabasePath, "Datasets"));
            if (!datasetsDirectory.Exists) datasetsDirectory.Create();
            FileInfo[] datasetFiles = datasetsDirectory.GetFiles("*" + Dataset.EXTENSION, SearchOption.TopDirectoryOnly);
            m_Datasets = (await UniTask.WhenAll(datasetFiles.Select(df => ClassLoaderSaver.LoadFromJsonAsync<Dataset>(df.FullName)))).ToList();
        }
        private async UniTask SaveDatasetsAsync()
        {
            DirectoryInfo datasetsDirectory = Directory.CreateDirectory(Path.Combine(ApplicationState.DatabasePath, "Datasets"));
            DirectoryInfo datasetsTempDirectory = Directory.CreateDirectory(Path.Combine(ApplicationState.DatabasePath, "DatasetsTemp"));
            await UniTask.WhenAll(m_Datasets.Select(d => ClassLoaderSaver.SaveToJSonAsync(d, Path.Combine(datasetsTempDirectory.FullName, d.Name + Dataset.EXTENSION), true)));
            datasetsDirectory.Delete(true);
            datasetsTempDirectory.MoveTo(datasetsDirectory.FullName);
        }
        
        private async UniTask UpdateDatabasesAsync(IEnumerable<DatabaseReference> databaseReferences, Action<float, float, LoadingText> updateProgress, UnityAction onUpdated)
        {
            await UniTask.SwitchToThreadPool();
            var brainvisaDatabaseReferences = databaseReferences.Where(d => d.Type == DatabaseType.Brainvisa).ToArray();
            var localizerDatabaseReferences = databaseReferences.Where(d => d.Type == DatabaseType.Localizer).ToArray();
            var bidsDatabaseReferences = databaseReferences.Where(d => d.Type == DatabaseType.BIDS).ToArray();
            // Load patients first
            foreach (var brainvisaDatabaseReference in brainvisaDatabaseReferences)
            {
                Patient.LoadFromIntranatDatabase(brainvisaDatabaseReference.Path, out Patient[] patients, updateProgress);
                foreach (var patient in patients) patient.CorrespondingDatabaseID = brainvisaDatabaseReference.ID;
                // TODO: Warn that patients will be deleted / overwritten
                m_Patients.RemoveAll(p => patients.Contains(p) || p.CorrespondingDatabaseID == brainvisaDatabaseReference.ID);
                m_Patients.AddRange(patients);
            }
            foreach (var bidsDatabaseReference in bidsDatabaseReferences)
            {
                Patient.LoadFromBIDSDatabase(bidsDatabaseReference.Path, out Patient[] patients, updateProgress);
                foreach (var patient in patients) patient.CorrespondingDatabaseID = bidsDatabaseReference.ID;
                // TODO: Warn that patients will be deleted / overwritten
                m_Patients.RemoveAll(p => patients.Contains(p) || p.CorrespondingDatabaseID == bidsDatabaseReference.ID);
                m_Patients.AddRange(patients);
            }
            // Then load datasets
            List<Dataset> generatedDatasets = new();
            foreach (var localizerDatabaseReference in localizerDatabaseReferences)
            {
                Dataset.LoadFromLocalizersDatabase(localizerDatabaseReference.Path, out Dataset[] datasets, updateProgress);
                foreach (var dataset in datasets)
                    foreach (var data in dataset.Data)
                        data.CorrespondingDatabaseID = localizerDatabaseReference.ID;
                generatedDatasets.AddRange(datasets);
            }
            foreach (var bidsDatabaseReference in bidsDatabaseReferences)
            {
                Dataset.LoadFromBIDSDatabase(bidsDatabaseReference.Path, out Dataset[] datasets, updateProgress);
                foreach (var dataset in datasets)
                    foreach (var data in dataset.Data)
                        data.CorrespondingDatabaseID = bidsDatabaseReference.ID;
                generatedDatasets.AddRange(datasets);
            }
            // TODO: Warn that datasets will be deleted / overwritten
            foreach (var dataset in m_Datasets)
            {
                dataset.RemoveData(dataset.Data.Where(d => databaseReferences.Any(r => r.ID == d.CorrespondingDatabaseID)).ToList());
            }
            m_Datasets.RemoveAll(d => d.Data.Count == 0);
            foreach (var dataset in generatedDatasets)
            {
                Dataset protocolDataset = m_Datasets.FirstOrDefault(d => d.Protocol == dataset.Protocol);
                if (protocolDataset == null)
                {
                    protocolDataset = dataset;
                    m_Datasets.Add(protocolDataset);
                }
                else
                {
                    protocolDataset.AddData(dataset.Data);
                }
            }
            // Update last updated
            foreach (var databaseReference in databaseReferences)
            {
                databaseReference.LastUpdated = DateTime.Now;
            }
            await SaveDatabaseReferencesAsync();
            await UniTask.SwitchToMainThread();
            DialogBoxManager.Open(DialogBoxManager.AlertType.Informational, "Databases updated", "The databases have been updated successfully");
            onUpdated();
        }
        #endregion
    }
}