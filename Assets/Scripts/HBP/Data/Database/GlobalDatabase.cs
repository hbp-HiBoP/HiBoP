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
using HBP.Core.Exceptions;
using HBP.Data.Preferences;
using UnityEngine.PlayerLoop;
using System.Text;
using HBP.Core.Enums;

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

        #region Events
        public UnityEvent OnUpdateDatabases { get; } = new UnityEvent();
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
        public async UniTaskVoid Initialize()
        {
            await UniTask.SwitchToThreadPool();
            if (!new DirectoryInfo(ApplicationState.DatabasePath).Exists) Directory.CreateDirectory(ApplicationState.DatabasePath);
            LoadSettings();
            if (m_Settings.IsFirstUse)
            {
                ConfigureDefault();
                SaveSettings();
            }
            await LoadProtocolsAsync();
            LoadDatabase().Forget();
        }
        public void SaveSettings()
        {
            ClassLoaderSaver.SaveToJSon(m_Settings, GlobalDatabaseSettings.PATH, true);
        }
        public async UniTaskVoid SaveProtocols()
        {
            await SaveProtocolsAsync();
        } 
        public async UniTaskVoid SaveDatabaseReferences()
        {
            await SaveDatabaseReferencesAsync();
            SaveDatabase().Forget();
        }

        public async UniTaskVoid LoadDatabase()
        {
            await UniTask.SwitchToThreadPool();
            await LoadDatabaseReferencesAsync();
            await LoadingManager.LoadAsync(update => LoadDatabaseAsync(update));
        }
        public async UniTaskVoid SaveDatabase()
        {
            await UniTask.SwitchToThreadPool();
            await LoadingManager.LoadAsync(update => SaveDatabaseAsync(update));
        }
        public async UniTask UpdateDatabases(IEnumerable<DatabaseReference> databaseReferences)
        {
            await LoadingManager.LoadAsync((update, token) => UpdateDatabasesAsync(databaseReferences, update, token));
            await LoadingManager.LoadAsync(update => SaveDatabaseAsync(update));
            await UniTask.SwitchToMainThread();
            OnUpdateDatabases.Invoke();
        }
        #endregion

        #region Private Methods
        private void ConfigureDefault()
        {
            DirectoryInfo defaultDatabaseDirectory = new DirectoryInfo(Path.Combine(ApplicationState.DataPath, "DefaultDatabase"));
            defaultDatabaseDirectory.CopyFilesRecursively(new DirectoryInfo(ApplicationState.DatabasePath));
            m_Settings.SetDefaultWorkspace();
            m_Settings.IsFirstUse = false;
        }

        private void LoadSettings()
        {
            if (new FileInfo(GlobalDatabaseSettings.PATH).Exists)
            {
                try
                {
                    m_Settings = ClassLoaderSaver.LoadFromJson<GlobalDatabaseSettings>(GlobalDatabaseSettings.PATH);
                    // Remove unused workspaces
                    var workspaceDirectories = new DirectoryInfo(Path.Combine(ApplicationState.DatabasePath, "Workspaces")).GetDirectories();
                    foreach (var workspaceDirectory in workspaceDirectories)
                    {
                        if (!m_Settings.Workspaces.Any(w => w.ID == workspaceDirectory.Name))
                        {
                            workspaceDirectory.Delete(true);
                        }
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                    throw e;
                }
            }
        }

        private async UniTask LoadDatabaseAsync(Action<float, float, LoadingText> updateProgress)
        {
            var patientFiles = GetPatientFiles();
            var dataInfoFiles = GetDataInfoFiles();
            float patientProgress = patientFiles.Length;
            float dataInfoProgress = dataInfoFiles.Length;
            float steps = patientProgress + dataInfoProgress;
            patientProgress /= steps;
            dataInfoProgress /= steps;
            float progress = 0;
            await LoadPatientsAsync(patientFiles, (localProgress, duration, text) => updateProgress(progress + localProgress * patientProgress, duration, text));
            progress += patientProgress;
            await LoadDataInfosAsync(dataInfoFiles, (localProgress, duration, text) => updateProgress(progress + localProgress * dataInfoProgress, duration, text));
            progress += dataInfoProgress;
            updateProgress(1, 0, new LoadingText("Finalizing"));
            IsLoaded = true;
        }
        private async UniTask SaveDatabaseAsync(Action<float, float, LoadingText> updateProgress)
        {
            float patientProgress = m_Patients.Count;
            float dataInfoProgress = (float)m_DataInfos.Count / m_Patients.Count;
            float steps = patientProgress + dataInfoProgress;
            patientProgress /= steps;
            dataInfoProgress /= steps;
            float progress = 0;
            await SavePatientsAsync((localProgress, duration, text) => updateProgress(progress + localProgress * patientProgress, duration, text));
            progress += patientProgress;
            await SaveDataInfosAsync((localProgress, duration, text) => updateProgress(progress + localProgress * dataInfoProgress, duration, text));
            progress += dataInfoProgress;
            updateProgress(1, 0, new LoadingText("Finalizing"));
        }

        private async UniTask LoadProtocolsAsync()
        {
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
            DirectoryInfo referencesDirectory = Directory.CreateDirectory(Path.Combine(Settings.SelectedWorkspace.Path, "References"));
            if (!referencesDirectory.Exists) referencesDirectory.Create();
            FileInfo[] referenceFiles = referencesDirectory.GetFiles("*" + DatabaseReference.EXTENSION, SearchOption.TopDirectoryOnly);
            m_DatabaseReferences = (await UniTask.WhenAll(referenceFiles.Select(rf => ClassLoaderSaver.LoadFromJsonAsync<DatabaseReference>(rf.FullName)))).ToList();
        }
        private async UniTask SaveDatabaseReferencesAsync()
        {
            DirectoryInfo referencesDirectory = Directory.CreateDirectory(Path.Combine(Settings.SelectedWorkspace.Path, "References"));
            DirectoryInfo referencesTempDirectory = Directory.CreateDirectory(Path.Combine(Settings.SelectedWorkspace.Path, "ReferencesTemp"));
            await UniTask.WhenAll(m_DatabaseReferences.Select(dr => ClassLoaderSaver.SaveToJsonAsync(dr, Path.Combine(referencesTempDirectory.FullName, dr.Name + DatabaseReference.EXTENSION), true)));
            referencesDirectory.Delete(true);
            referencesTempDirectory.MoveTo(referencesDirectory.FullName);
            m_Patients.RemoveAll(p => !m_DatabaseReferences.Any(r => r.ID == p.CorrespondingDatabaseID));
            m_DataInfos.RemoveAll(d => m_DatabaseReferences.All(r => r.ID != d.CorrespondingDatabaseID) || (d is PatientDataInfo pd && !m_Patients.Contains(pd.Patient)));
        }

        private FileInfo[] GetPatientFiles()
        {
            DirectoryInfo patientsDirectory = new DirectoryInfo(Path.Combine(Settings.SelectedWorkspace.Path, "Patients"));
            if (!patientsDirectory.Exists) patientsDirectory.Create();
            return patientsDirectory.GetFiles("*" + Patient.EXTENSION, SearchOption.TopDirectoryOnly);
        }
        private async UniTask LoadPatientsAsync(FileInfo[] patientFiles, Action<float, float, LoadingText> updateProgress)
        {
            var tasks = patientFiles.Select(file => (Func<UniTask<Patient>>)(async () =>
            {
                var patient = await ClassLoaderSaver.LoadFromJsonAsync<Patient>(file.FullName);
                await patient.CheckTagsAsync(PersistentDataManager.Tags.AllTags);
                return patient;
            }));
            m_Patients = (await Core.Tools.UniTaskExtensions.PerformMultipleTasksAsync(tasks, 0, 1, "Loading database patients", updateProgress, 20, PersistentDataManager.UserPreferences.General.System.MultiThreading)).ToList();
        }
        private async UniTask SavePatientsAsync(Action<float, float, LoadingText> updateProgress)
        {
            DirectoryInfo patientsDirectory = Directory.CreateDirectory(Path.Combine(Settings.SelectedWorkspace.Path, "Patients"));
            DirectoryInfo patientsTempDirectory = Directory.CreateDirectory(Path.Combine(Settings.SelectedWorkspace.Path, "PatientsTemp"));
            var tasks = m_Patients.Select(p => (Func<UniTask>)(async () =>
            {
                await ClassLoaderSaver.SaveToJsonAsync(p, Path.Combine(patientsTempDirectory.FullName, p.ID + Patient.EXTENSION), true);
            }));
            await Core.Tools.UniTaskExtensions.PerformMultipleTasksAsync(tasks, 0, 1, "Saving database patients", updateProgress, 20, PersistentDataManager.UserPreferences.General.System.MultiThreading);
            patientsDirectory.Delete(true);
            patientsTempDirectory.MoveTo(patientsDirectory.FullName);
        }

        private FileInfo[] GetDataInfoFiles()
        {
            DirectoryInfo dataInfosDirectory = new DirectoryInfo(Path.Combine(Settings.SelectedWorkspace.Path, "DataInfos"));
            if (!dataInfosDirectory.Exists) dataInfosDirectory.Create();
            return dataInfosDirectory.GetFiles("*" + DataInfo.EXTENSION, SearchOption.TopDirectoryOnly);
        }
        private async UniTask LoadDataInfosAsync(FileInfo[] dataInfoFiles, Action<float, float, LoadingText> updateProgress)
        {
            var tasks = dataInfoFiles.Select(file => (Func<UniTask<List<DataInfo>>>)(async () =>
            {
                return await ClassLoaderSaver.LoadFromJsonAsync<List<DataInfo>>(file.FullName);
            }));
            m_DataInfos = (await Core.Tools.UniTaskExtensions.PerformMultipleTasksAsync(tasks, 0, 1, "Loading database functional data", updateProgress, 20, PersistentDataManager.UserPreferences.General.System.MultiThreading)).SelectMany(d => d).ToList();
        }
        private async UniTask SaveDataInfosAsync(Action<float, float, LoadingText> updateProgress)
        {
            DirectoryInfo dataInfosDirectory = Directory.CreateDirectory(Path.Combine(Settings.SelectedWorkspace.Path, "DataInfos"));
            DirectoryInfo dataInfosTempDirectory = Directory.CreateDirectory(Path.Combine(Settings.SelectedWorkspace.Path, "DataInfosTemp"));
            Dictionary<Patient, List<PatientDataInfo>> patientDataInfos = new();
            foreach (var patient in m_DataInfos.OfType<PatientDataInfo>().Select(d => d.Patient).Distinct())
            {
                patientDataInfos.Add(patient, new List<PatientDataInfo>());
            }
            List<DataInfo> otherDataInfos = new List<DataInfo>();
            foreach (var dataInfo in m_DataInfos)
            {
                if (dataInfo is PatientDataInfo patientDataInfo) patientDataInfos[patientDataInfo.Patient].Add(patientDataInfo);
                else otherDataInfos.Add(dataInfo);
            }
            var tasks = patientDataInfos.Select(kvp => (Func<UniTask>)(async () =>
            {
                await ClassLoaderSaver.SaveToJsonAsync(kvp.Value, Path.Combine(dataInfosTempDirectory.FullName, kvp.Key.ID + DataInfo.EXTENSION), true);
            }));
            await Core.Tools.UniTaskExtensions.PerformMultipleTasksAsync(tasks, 0, 1, "Saving database functional data", updateProgress, 20, PersistentDataManager.UserPreferences.General.System.MultiThreading);
            await ClassLoaderSaver.SaveToJsonAsync(otherDataInfos, Path.Combine(dataInfosTempDirectory.FullName, "None" + DataInfo.EXTENSION), true);
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
            var tagsDatabaseReferences = databaseReferences.Where(d => d.Type == DatabaseType.Tags).ToArray();
            int numberOfDatabases = brainvisaDatabaseReferences.Length + localizerDatabaseReferences.Length + 2 * bidsDatabaseReferences.Length + tagsDatabaseReferences.Length;
            float progress = 0;
            // Load databases
            List<Patient> patientsTemp = new(m_Patients);
            List<DataInfo> dataInfosTemp = new(m_DataInfos);
            List<Patient> updatedPatients = new();
            // Load patients first
            foreach (var brainvisaDatabaseReference in brainvisaDatabaseReferences)
            {
                token.ThrowIfCancellationRequested();
                Patient.LoadFromIntranatDatabase(brainvisaDatabaseReference, out Patient[] patients, (localProgress, duration, text) => updateProgress(progress + (float)localProgress / numberOfDatabases, duration, text), token);
                patientsTemp.RemoveAll(p => patients.Contains(p) || p.CorrespondingDatabaseID == brainvisaDatabaseReference.ID);
                patientsTemp.AddRange(patients);
                updatedPatients.AddRange(patients);
                progress += 1f / numberOfDatabases;
            }
            foreach (var bidsDatabaseReference in bidsDatabaseReferences)
            {
                token.ThrowIfCancellationRequested();
                Patient.LoadFromBIDSDatabase(bidsDatabaseReference, out Patient[] patients, (localProgress, duration, text) => updateProgress(progress + (float)localProgress / numberOfDatabases, duration, text), token);
                foreach (var patient in patients) patient.CorrespondingDatabaseID = bidsDatabaseReference.ID;
                patientsTemp.RemoveAll(p => patients.Contains(p) || p.CorrespondingDatabaseID == bidsDatabaseReference.ID);
                patientsTemp.AddRange(patients);
                updatedPatients.AddRange(patients);
                progress += 1f / numberOfDatabases;
            }
            // Then load additional tags
            foreach (var tagsDatabaseReference in tagsDatabaseReferences)
            {
                token.ThrowIfCancellationRequested();
                Patient.LoadAdditionalTagsFromTagsDatabase(tagsDatabaseReference, patientsTemp, out Patient[] patients, (localProgress, duration, text) => updateProgress(progress + (float)localProgress / numberOfDatabases, duration, text), token);
                foreach (var patient in patients)
                {
                    patientsTemp.Remove(patient);
                    patientsTemp.Add(patient);
                }
                updatedPatients.AddRange(patients);
                progress += 1f / numberOfDatabases;
            }
            // Then load dataInfos
            foreach (var localizerDatabaseReference in localizerDatabaseReferences)
            {
                token.ThrowIfCancellationRequested();
                DataInfo.LoadFromLocalizersDatabase(localizerDatabaseReference, patientsTemp, out DataInfo[] dataInfos, (localProgress, duration, text) => updateProgress(progress + (float)localProgress / numberOfDatabases, duration, text), token);
                dataInfosTemp.RemoveAll(d => dataInfos.Contains(d) || d.CorrespondingDatabaseID == localizerDatabaseReference.ID);
                dataInfosTemp.AddRange(dataInfos);
                progress += 1f / numberOfDatabases;
            }
            foreach (var bidsDatabaseReference in bidsDatabaseReferences)
            {
                token.ThrowIfCancellationRequested();
                DataInfo.LoadFromBIDSDatabase(bidsDatabaseReference, patientsTemp, out DataInfo[] dataInfos, (localProgress, duration, text) => updateProgress(progress + (float)localProgress / numberOfDatabases, duration, text), token);
                dataInfosTemp.RemoveAll(d => dataInfos.Contains(d) || d.CorrespondingDatabaseID == bidsDatabaseReference.ID);
                dataInfosTemp.AddRange(dataInfos);
                progress += 1f / numberOfDatabases;
            }
            // Update last updated
            foreach (var databaseReference in databaseReferences)
            {
                token.ThrowIfCancellationRequested();
                databaseReference.LastUpdated = DateTime.Now;
            }
            updateProgress(1, 0, new LoadingText("Finalizing"));
            await FindAndDisplayChanges(m_Patients, patientsTemp, updatedPatients);
            // Set new lists
            m_Patients = patientsTemp;
            m_DataInfos = dataInfosTemp;
            await SaveDatabaseReferencesAsync();
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
        private async UniTask FindAndDisplayChanges(List<Patient> oldPatients, List<Patient> newPatients, List<Patient> updatedPatients)
        {
            var removedPatients = oldPatients.Except(newPatients).ToList();
            var addedPatients = newPatients.Except(oldPatients).ToList();
            updatedPatients = updatedPatients.Distinct().Except(addedPatients).Except(removedPatients).ToList();

            StringBuilder stringBuilder = new StringBuilder();
            if (removedPatients.Count > 0)
            {
                stringBuilder.AppendLine("<b>Removed patients:</b>");
                foreach (var patient in removedPatients)
                {
                    stringBuilder.AppendLine(patient.ID);
                }
                stringBuilder.AppendLine();
            }
            if (addedPatients.Count > 0)
            {
                stringBuilder.AppendLine("<b>Added patients:</b>");
                foreach (var patient in addedPatients)
                {
                    stringBuilder.AppendLine(patient.ID);
                }
                stringBuilder.AppendLine();
            }
            if (updatedPatients.Count > 0)
            {
                stringBuilder.AppendLine("<b>Updated patients:</b>");
                foreach (var patient in updatedPatients)
                {
                    stringBuilder.AppendLine(patient.ID);
                }
                stringBuilder.AppendLine();
            }
            if (stringBuilder.Length != 0)
            {
                await DialogBoxManager.OpenScrollableAsync(DialogBoxType.Informational, "Databases updated", stringBuilder.ToString(), "OK");
            }
        }
        #endregion
    }
}