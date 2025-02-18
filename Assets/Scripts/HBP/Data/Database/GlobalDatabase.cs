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
using System.Threading;
using System.Diagnostics;

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

        private List<DataInfo> m_DataInfos = new();
        public ReadOnlyCollection<DataInfo> DataInfos => new(m_DataInfos);

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
            database.InitializeDatabase().Forget();
            return database;
        }
        public async UniTaskVoid SaveProtocols()
        {
            await SaveProtocolsAsync();
        } 
        public async UniTaskVoid SaveDatabaseReferences()
        {
            await SaveDatabaseReferencesAsync();
        }

        public async UniTaskVoid LoadDatabase()
        {
            await UniTask.SwitchToThreadPool();
            await LoadDatabaseAsync();
        }
        public async UniTask UpdateDatabases(IEnumerable<DatabaseReference> databaseReferences)
        {
            await LoadingManager.LoadAsync((update, token) => UpdateDatabasesAsync(databaseReferences, update, token));
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
                    UnityEngine.Debug.LogException(e);
                    throw e;
                }
            }
        }
        private void SaveSettings()
        {
            m_Settings.IsFirstUse = true;
            ClassLoaderSaver.SaveToJSon(m_Settings, GlobalDatabaseSettings.PATH, true);
        }

        private async UniTaskVoid InitializeDatabase()
        {
            await LoadProtocolsAsync();
            await LoadDatabaseReferencesAsync();
            LoadDatabase().Forget();
        }
        private async UniTask LoadDatabaseAsync()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            await LoadPatientsAsync();
            await LoadDataInfosAsync();
            IsLoaded = true;
            stopwatch.Stop();
            UnityEngine.Debug.Log($"Database loaded in {stopwatch.ElapsedMilliseconds} ms");
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
            await UniTask.WhenAll(m_Protocols.Select(p => ClassLoaderSaver.SaveToJsonAsync(p, Path.Combine(protocolTempDirectory.FullName, p.Name + Protocol.EXTENSION), true)));
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
            await UniTask.WhenAll(m_DatabaseReferences.Select(dr => ClassLoaderSaver.SaveToJsonAsync(dr, Path.Combine(referencesTempDirectory.FullName, dr.Name + DatabaseReference.EXTENSION), true)));
            referencesDirectory.Delete(true);
            referencesTempDirectory.MoveTo(referencesDirectory.FullName);
            m_Patients.RemoveAll(p => !m_DatabaseReferences.Any(r => r.ID == p.CorrespondingDatabaseID));
            m_DataInfos.RemoveAll(d => m_DatabaseReferences.All(r => r.ID != d.CorrespondingDatabaseID) || (d is PatientDataInfo pd && !m_Patients.Contains(pd.Patient)));
            await SavePatientsAsync();
            await SaveDataInfosAsync();
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
            await UniTask.WhenAll(m_Patients.Select(p => ClassLoaderSaver.SaveToJsonAsync(p, Path.Combine(patientsTempDirectory.FullName, p.ID + Patient.EXTENSION), true)));
            patientsDirectory.Delete(true);
            patientsTempDirectory.MoveTo(patientsDirectory.FullName);
        }

        private async UniTask LoadDataInfosAsync()
        {
            List<DataInfo> dataInfos = new List<DataInfo>();
            DirectoryInfo dataInfosDirectory = new DirectoryInfo(Path.Combine(ApplicationState.DatabasePath, "DataInfos"));
            if (!dataInfosDirectory.Exists) dataInfosDirectory.Create();
            FileInfo[] dataInfoFiles = dataInfosDirectory.GetFiles("*" + DataInfo.EXTENSION, SearchOption.TopDirectoryOnly);
            m_DataInfos = (await UniTask.WhenAll(dataInfoFiles.Select(df => ClassLoaderSaver.LoadFromJsonAsync<List<DataInfo>>(df.FullName)))).SelectMany(d => d).ToList();
        }
        private async UniTask SaveDataInfosAsync()
        {
            DirectoryInfo dataInfosDirectory = Directory.CreateDirectory(Path.Combine(ApplicationState.DatabasePath, "DataInfos"));
            DirectoryInfo dataInfosTempDirectory = Directory.CreateDirectory(Path.Combine(ApplicationState.DatabasePath, "DataInfosTemp"));
            await ClassLoaderSaver.SaveToJsonAsync(m_DataInfos, Path.Combine(dataInfosTempDirectory.FullName, "DataInfos" + DataInfo.EXTENSION), true);
            dataInfosDirectory.Delete(true);
            dataInfosTempDirectory.MoveTo(dataInfosDirectory.FullName);
        }
        
        private async UniTask UpdateDatabasesAsync(IEnumerable<DatabaseReference> databaseReferences, Action<float, float, LoadingText> updateProgress, CancellationToken token)
        {
            updateProgress(0, 1, new LoadingText("Initialization"));
            await UniTask.SwitchToThreadPool();
            var brainvisaDatabaseReferences = databaseReferences.Where(d => d.Type == DatabaseType.Brainvisa).ToArray();
            var localizerDatabaseReferences = databaseReferences.Where(d => d.Type == DatabaseType.Localizer).ToArray();
            var bidsDatabaseReferences = databaseReferences.Where(d => d.Type == DatabaseType.BIDS).ToArray();
            int numberOfDatabases = brainvisaDatabaseReferences.Length + localizerDatabaseReferences.Length + 2 * bidsDatabaseReferences.Length;
            float progress = 0;
            // Backup patients and datasets
            List<Patient> patientsBackup = m_Patients.DeepClone().ToList();
            List<DataInfo> dataInfosBackup = m_DataInfos.DeepClone().ToList();
            try
            {
                // Load patients first
                foreach (var brainvisaDatabaseReference in brainvisaDatabaseReferences)
                {
                    token.ThrowIfCancellationRequested();
                    Patient.LoadFromIntranatDatabase(brainvisaDatabaseReference.Path, out Patient[] patients, (localProgress, duration, text) => updateProgress(progress + (float)localProgress / numberOfDatabases, duration, text), token);
                    foreach (var patient in patients) patient.CorrespondingDatabaseID = brainvisaDatabaseReference.ID;
                    // TODO: Warn that patients will be deleted / overwritten
                    m_Patients.RemoveAll(p => patients.Contains(p) || p.CorrespondingDatabaseID == brainvisaDatabaseReference.ID);
                    m_Patients.AddRange(patients);
                    progress += 1f / numberOfDatabases;
                }
                foreach (var bidsDatabaseReference in bidsDatabaseReferences)
                {
                    token.ThrowIfCancellationRequested();
                    Patient.LoadFromBIDSDatabase(bidsDatabaseReference.Path, out Patient[] patients, (localProgress, duration, text) => updateProgress(progress + (float)localProgress / numberOfDatabases, duration, text), token);
                    foreach (var patient in patients) patient.CorrespondingDatabaseID = bidsDatabaseReference.ID;
                    // TODO: Warn that patients will be deleted / overwritten
                    m_Patients.RemoveAll(p => patients.Contains(p) || p.CorrespondingDatabaseID == bidsDatabaseReference.ID);
                    m_Patients.AddRange(patients);
                    progress += 1f / numberOfDatabases;
                }
                // Then load dataInfos
                foreach (var localizerDatabaseReference in localizerDatabaseReferences)
                {
                    token.ThrowIfCancellationRequested();
                    DataInfo.LoadFromLocalizersDatabase(localizerDatabaseReference.Path, out DataInfo[] dataInfos, (localProgress, duration, text) => updateProgress(progress + (float)localProgress / numberOfDatabases, duration, text), token);
                    foreach (var dataInfo in dataInfos) dataInfo.CorrespondingDatabaseID = localizerDatabaseReference.ID;
                    // TODO: Warn that dataInfos will be deleted / overwritten
                    m_DataInfos.RemoveAll(d => dataInfos.Contains(d) || d.CorrespondingDatabaseID == localizerDatabaseReference.ID);
                    m_DataInfos.AddRange(dataInfos);
                    progress += 1f / numberOfDatabases;
                }
                foreach (var bidsDatabaseReference in bidsDatabaseReferences)
                {
                    token.ThrowIfCancellationRequested();
                    DataInfo.LoadFromBIDSDatabase(bidsDatabaseReference.Path, out DataInfo[] dataInfos, (localProgress, duration, text) => updateProgress(progress + (float)localProgress / numberOfDatabases, duration, text), token);
                    foreach (var dataInfo in dataInfos) dataInfo.CorrespondingDatabaseID = bidsDatabaseReference.ID;
                    // TODO: Warn that dataInfos will be deleted / overwritten
                    m_DataInfos.RemoveAll(d => dataInfos.Contains(d) || d.CorrespondingDatabaseID == bidsDatabaseReference.ID);
                    m_DataInfos.AddRange(dataInfos);
                    progress += 1f / numberOfDatabases;
                }
                // Update last updated
                foreach (var databaseReference in databaseReferences)
                {
                    token.ThrowIfCancellationRequested();
                    databaseReference.LastUpdated = DateTime.Now;
                }
                await SaveDatabaseReferencesAsync();
            }
            catch (Exception e)
            {
                m_Patients = patientsBackup;
                m_DataInfos = dataInfosBackup;
                FixDatasets();
                throw e;
            }
        }

        private void FixDatasets()
        {
            foreach (var dataInfo in m_DataInfos)
            {
                dataInfo.Protocol = m_Protocols.FirstOrDefault(p => p.ID == dataInfo.Protocol.ID);
                if (dataInfo is PatientDataInfo patientDataInfo)
                {
                    patientDataInfo.Patient = m_Patients.FirstOrDefault(p => p.ID == patientDataInfo.Patient.ID);
                }
            }
        }
        #endregion
    }
}