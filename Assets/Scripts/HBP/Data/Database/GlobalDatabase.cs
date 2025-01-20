using HBP.Core.Data;
using HBP.Core.Exceptions;
using HBP.Core.Tools;
using HBP.UI.Main;
using HBP.UI.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using ThirdParty.CielaSpike;
using UnityEngine;
using UnityEngine.Events;

namespace HBP.Data.Database
{
    public class GlobalDatabase
    {
        #region Properties
        private GlobalDatabaseSettings m_Settings = new GlobalDatabaseSettings();
        public GlobalDatabaseSettings Settings => m_Settings;

        private List<Protocol> m_Protocols = new List<Protocol>();
        public ReadOnlyCollection<Protocol> Protocols => new ReadOnlyCollection<Protocol>(m_Protocols);

        private List<DatabaseReference> m_DatabaseReferences = new List<DatabaseReference>();
        public ReadOnlyCollection<DatabaseReference> DatabaseReferences => new ReadOnlyCollection<DatabaseReference>(m_DatabaseReferences);

        private List<Patient> m_Patients = new List<Patient>();
        public ReadOnlyCollection<Patient> Patients => new ReadOnlyCollection<Patient>(m_Patients);

        private List<Dataset> m_Datasets = new List<Dataset>();
        public ReadOnlyCollection<Dataset> Datasets => new ReadOnlyCollection<Dataset>(m_Datasets);

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
        public void SetPatients(IEnumerable<Patient> patients)
        {
            m_Patients = patients.ToList();
        }
        public void SetDatasets(IEnumerable<Dataset> datasets)
        {
            m_Datasets = datasets.ToList();
        }
        #endregion

        #region Public Methods
        public static GlobalDatabase Initialize()
        {
            GlobalDatabase database = new GlobalDatabase();
            if (!new DirectoryInfo(ApplicationState.DatabasePath).Exists) Directory.CreateDirectory(ApplicationState.DatabasePath);
            database.LoadSettings();
            if (!database.Settings.Initialized)
            {
                CopyDefaultDatabase();
                database.SaveSettings();
            }
            GenericEvent<float, float, LoadingText> onChangeProgress = new GenericEvent<float, float, LoadingText>();
            LoadingManager.Load(database.c_LoadDatabase(ApplicationState.DatabasePath, onChangeProgress), onChangeProgress);
            return database;
        }
        public void SaveProtocols()
        {
            GenericEvent<float, float, LoadingText> onChangeProgress = new GenericEvent<float, float, LoadingText>();
            LoadingManager.Load(c_SaveProtocols(new DirectoryInfo(ApplicationState.DatabasePath), onChangeProgress.Invoke), onChangeProgress);
        } 
        public void SaveDatabaseReferences()
        {
            // TODO: Remove patients that are not in the database references and warn the user before
            GenericEvent<float, float, LoadingText> onChangeProgress = new GenericEvent<float, float, LoadingText>();
            LoadingManager.Load(c_SaveDatabaseReferences(new DirectoryInfo(ApplicationState.DatabasePath), onChangeProgress.Invoke), onChangeProgress);
        }
        public void SavePatients()
        {
            GenericEvent<float, float, LoadingText> onChangeProgress = new GenericEvent<float, float, LoadingText>();
            LoadingManager.Load(c_SavePatients(new DirectoryInfo(ApplicationState.DatabasePath), onChangeProgress.Invoke), onChangeProgress);
        }
        public void SaveDatasets()
        {
            GenericEvent<float, float, LoadingText> onChangeProgress = new GenericEvent<float, float, LoadingText>();
            LoadingManager.Load(c_SaveDatasets(new DirectoryInfo(ApplicationState.DatabasePath), onChangeProgress.Invoke), onChangeProgress);
        }

        public void UpdateDatabases(IEnumerable<DatabaseReference> databaseReferences, UnityAction onUpdated)
        {
            GenericEvent<float, float, LoadingText> onChangeProgress = new GenericEvent<float, float, LoadingText>();
            LoadingManager.Load(c_UpdateDatabases(databaseReferences, onChangeProgress, onUpdated), onChangeProgress);
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
            m_Settings.Initialized = true;
            ClassLoaderSaver.SaveToJSon(m_Settings, GlobalDatabaseSettings.PATH, true);
        }

        private IEnumerator c_LoadDatabase(string rootDirectory, GenericEvent<float, float, LoadingText> onChangeProgress)
        {
            yield return Ninja.JumpToUnity;
            yield return CoroutineManager.StartAsync(c_LoadProtocols(new DirectoryInfo(rootDirectory), onChangeProgress));
            yield return CoroutineManager.StartAsync(c_LoadDatabaseReferences(new DirectoryInfo(rootDirectory), onChangeProgress));
            // TODO: Do not load patients/datasets if the user does not want to
            yield return CoroutineManager.StartAsync(c_LoadPatients(new DirectoryInfo(rootDirectory), onChangeProgress));
            yield return CoroutineManager.StartAsync(c_LoadDatasets(new DirectoryInfo(rootDirectory), onChangeProgress));
            yield return Ninja.JumpBack;
            IsLoaded = true;
        }

        IEnumerator c_LoadProtocols(DirectoryInfo rootDirectory, GenericEvent<float, float, LoadingText> onChangeProgress)
        {
            yield return Ninja.JumpBack;
            // Load Protocols
            List<Protocol> protocols = new List<Protocol>();
            DirectoryInfo protocolDirectory = new DirectoryInfo(Path.Combine(rootDirectory.FullName, "Protocols"));
            if (!protocolDirectory.Exists) protocolDirectory.Create();
            FileInfo[] protocolFiles = protocolDirectory.GetFiles("*" + Protocol.EXTENSION, SearchOption.TopDirectoryOnly);
            for (int i = 0; i < protocolFiles.Length; ++i)
            {
                FileInfo protocolFile = protocolFiles[i];
                onChangeProgress.Invoke((float)(i + 1) / protocolFiles.Length, 0, new LoadingText("Loading protocol ", Path.GetFileNameWithoutExtension(protocolFile.Name), " [" + (i + 1).ToString() + "/" + protocolFiles.Length + "]"));
                try
                {
                    protocols.Add(ClassLoaderSaver.LoadFromJson<Protocol>(protocolFile.FullName));
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    throw new CanNotReadProtocolFileException(Path.GetFileNameWithoutExtension(protocolFile.Name));
                }
            }
            SetProtocols(protocols.ToArray());
            onChangeProgress.Invoke(1.0f, 0, new LoadingText("Protocols loaded successfully"));
        }
        IEnumerator c_SaveProtocols(DirectoryInfo rootDirectory, Action<float, float, LoadingText> onChangeProgress)
        {
            yield return Ninja.JumpBack;
            // Save protocols
            DirectoryInfo protocolDirectory = Directory.CreateDirectory(Path.Combine(rootDirectory.FullName, "Protocols"));
            DirectoryInfo protocolTempDirectory = Directory.CreateDirectory(Path.Combine(rootDirectory.FullName, "ProtocolsTemp"));
            int count = 0;
            int length = m_Protocols.Count();
            foreach (Protocol protocol in m_Protocols)
            {
                onChangeProgress.Invoke((float)count / length, 0, new LoadingText("Saving protocol ", protocol.Name, " [" + (count + 1).ToString() + "/" + length + "]"));
                try
                {
                    ClassLoaderSaver.SaveToJSon(protocol, Path.Combine(protocolTempDirectory.FullName, protocol.Name + Protocol.EXTENSION), true);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    throw new CanNotSaveSettingsException();
                }
                count++;
            }
            // Move files
            protocolDirectory.Delete(true);
            protocolTempDirectory.MoveTo(protocolDirectory.FullName);
            onChangeProgress.Invoke(1.0f, 0, new LoadingText("Protocols saved successfully"));
        }

        IEnumerator c_LoadDatabaseReferences(DirectoryInfo rootDirectory, GenericEvent<float, float, LoadingText> onChangeProgress)
        {
            yield return Ninja.JumpBack;
            // Load References
            List<DatabaseReference> databaseReferences = new List<DatabaseReference>();
            DirectoryInfo referencesDirectory = Directory.CreateDirectory(Path.Combine(rootDirectory.FullName, "References"));
            if (!referencesDirectory.Exists) referencesDirectory.Create();
            FileInfo[] referenceFiles = referencesDirectory.GetFiles("*" + DatabaseReference.EXTENSION, SearchOption.TopDirectoryOnly);
            for (int i = 0; i < referenceFiles.Length; ++i)
            {
                FileInfo referenceFile = referenceFiles[i];
                onChangeProgress.Invoke((float)(i + 1) / referenceFiles.Length, 0, new LoadingText("Loading reference ", Path.GetFileNameWithoutExtension(referenceFile.Name), " [" + (i + 1).ToString() + "/" + referenceFiles.Length + "]"));
                try
                {
                    databaseReferences.Add(ClassLoaderSaver.LoadFromJson<DatabaseReference>(referenceFile.FullName));
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    throw e;
                }
            }
            m_DatabaseReferences = databaseReferences;
            onChangeProgress.Invoke(1.0f, 0, new LoadingText("References loaded successfully"));
        }
        IEnumerator c_SaveDatabaseReferences(DirectoryInfo rootDirectory, Action<float, float, LoadingText> onChangeProgress)
        {
            yield return Ninja.JumpBack;
            // Save references
            DirectoryInfo referencesDirectory = Directory.CreateDirectory(Path.Combine(rootDirectory.FullName, "References"));
            DirectoryInfo referencesTempDirectory = Directory.CreateDirectory(Path.Combine(rootDirectory.FullName, "ReferencesTemp"));
            int count = 0;
            int length = m_DatabaseReferences.Count();
            foreach (DatabaseReference databaseReference in m_DatabaseReferences)
            {
                onChangeProgress.Invoke((float)count / length, 0, new LoadingText("Saving reference ", databaseReference.Name, " [" + (count + 1).ToString() + "/" + length + "]"));
                try
                {
                    ClassLoaderSaver.SaveToJSon(databaseReference, Path.Combine(referencesTempDirectory.FullName, databaseReference.Name + DatabaseReference.EXTENSION), true);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    throw new CanNotSaveSettingsException();
                }
                count++;
            }
            // Move files
            referencesDirectory.Delete(true);
            referencesTempDirectory.MoveTo(referencesDirectory.FullName);
            onChangeProgress.Invoke(1.0f, 0, new LoadingText("References saved successfully"));
        }

        IEnumerator c_SavePatients(DirectoryInfo rootDirectory, Action<float, float, LoadingText> onChangeProgress)
        {
            yield return Ninja.JumpBack;
            // Save patients
            DirectoryInfo patientsDirectory = Directory.CreateDirectory(Path.Combine(rootDirectory.FullName, "Patients"));
            int count = 0;
            int length = m_Patients.Count();
            foreach (Patient patient in m_Patients)
            {
                onChangeProgress.Invoke((float)count / length, 0, new LoadingText("Saving patient ", patient.Name, " [" + (count + 1).ToString() + "/" + length + "]"));
                try
                {
                    ClassLoaderSaver.SaveToJSon(patient, Path.Combine(patientsDirectory.FullName, patient.ID + Patient.EXTENSION), true);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    throw new CanNotSaveSettingsException();
                }
                count++;
            }
            onChangeProgress.Invoke(1.0f, 0, new LoadingText("Patients saved successfully"));
        }
        IEnumerator c_LoadPatients(DirectoryInfo rootDirectory, GenericEvent<float, float, LoadingText> onChangeProgress)
        {
            yield return Ninja.JumpBack;
            // Load Patients
            List<Patient> patients = new List<Patient>();
            DirectoryInfo patientsDirectory = new DirectoryInfo(Path.Combine(rootDirectory.FullName, "Patients"));
            if (!patientsDirectory.Exists) patientsDirectory.Create();
            FileInfo[] patientFiles = patientsDirectory.GetFiles("*" + Patient.EXTENSION, SearchOption.TopDirectoryOnly);
            for (int i = 0; i < patientFiles.Length; ++i)
            {
                FileInfo patientFile = patientFiles[i];
                onChangeProgress.Invoke((float)(i + 1) / patientFiles.Length, 0, new LoadingText("Loading patient ", Path.GetFileNameWithoutExtension(patientFile.Name), " [" + (i + 1).ToString() + "/" + patientFiles.Length + "]"));
                try
                {
                    patients.Add(ClassLoaderSaver.LoadFromJson<Patient>(patientFile.FullName));
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    throw e;
                }
            }
            SetPatients(patients.ToArray());
            onChangeProgress.Invoke(1.0f, 0, new LoadingText("Patients loaded successfully"));
        }

        IEnumerator c_SaveDatasets(DirectoryInfo rootDirectory, Action<float, float, LoadingText> onChangeProgress)
        {
            yield return Ninja.JumpBack;
            // Save datasets
            DirectoryInfo datasetsDirectory = Directory.CreateDirectory(Path.Combine(rootDirectory.FullName, "Datasets"));
            int count = 0;
            int length = m_Datasets.Count();
            foreach (Dataset dataset in m_Datasets)
            {
                onChangeProgress.Invoke((float)count / length, 0, new LoadingText("Saving dataset ", dataset.Name, " [" + (count + 1).ToString() + "/" + length + "]"));
                try
                {
                    ClassLoaderSaver.SaveToJSon(dataset, Path.Combine(datasetsDirectory.FullName, dataset.ID + Dataset.EXTENSION), true);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    throw new CanNotSaveSettingsException();
                }
                count++;
            }
            onChangeProgress.Invoke(1.0f, 0, new LoadingText("Datasets saved successfully"));
        }
        IEnumerator c_LoadDatasets(DirectoryInfo rootDirectory, GenericEvent<float, float, LoadingText> onChangeProgress)
        {
            yield return Ninja.JumpBack;
            // Load Datasets
            List<Dataset> datasets = new List<Dataset>();
            DirectoryInfo datasetsDirectory = new DirectoryInfo(Path.Combine(rootDirectory.FullName, "Datasets"));
            if (!datasetsDirectory.Exists) datasetsDirectory.Create();
            FileInfo[] datasetFiles = datasetsDirectory.GetFiles("*" + Dataset.EXTENSION, SearchOption.TopDirectoryOnly);
            for (int i = 0; i < datasetFiles.Length; ++i)
            {
                FileInfo datasetFile = datasetFiles[i];
                onChangeProgress.Invoke((float)(i + 1) / datasetFiles.Length, 0, new LoadingText("Loading dataset ", Path.GetFileNameWithoutExtension(datasetFile.Name), " [" + (i + 1).ToString() + "/" + datasetFiles.Length + "]"));
                try
                {
                    datasets.Add(ClassLoaderSaver.LoadFromJson<Dataset>(datasetFile.FullName));
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    throw e;
                }
            }
            SetDatasets(datasets.ToArray());
            onChangeProgress.Invoke(1.0f, 0, new LoadingText("Datasets loaded successfully"));
        }
        
        IEnumerator c_UpdateDatabases(IEnumerable<DatabaseReference> databaseReferences, GenericEvent<float, float, LoadingText> onChangeProgress, UnityAction onUpdated)
        {
            yield return Ninja.JumpBack;
            var brainvisaDatabaseReferences = databaseReferences.Where(d => d.Type == DatabaseType.Brainvisa).ToArray();
            var localizerDatabaseReferences = databaseReferences.Where(d => d.Type == DatabaseType.Localizer).ToArray();
            var bidsDatabaseReferences = databaseReferences.Where(d => d.Type == DatabaseType.BIDS).ToArray();
            // Load patients first
            foreach (var brainvisaDatabaseReference in brainvisaDatabaseReferences)
            {
                Patient.LoadFromIntranatDatabase(brainvisaDatabaseReference.Path, out Patient[] patients, (progress, duration, text) => onChangeProgress.Invoke(progress, duration, text));
                foreach (var patient in patients) patient.CorrespondingDatabaseID = brainvisaDatabaseReference.ID;
                // TODO: Warn that patients will be deleted / overwritten
                m_Patients.RemoveAll(p => patients.Contains(p) || p.CorrespondingDatabaseID == brainvisaDatabaseReference.ID);
                m_Patients.AddRange(patients);
            }
            foreach (var bidsDatabaseReference in bidsDatabaseReferences)
            {
                Patient.LoadFromBIDSDatabase(bidsDatabaseReference.Path, out Patient[] patients, (progress, duration, text) => onChangeProgress.Invoke(progress, duration, text));
                foreach (var patient in patients) patient.CorrespondingDatabaseID = bidsDatabaseReference.ID;
                // TODO: Warn that patients will be deleted / overwritten
                m_Patients.RemoveAll(p => patients.Contains(p) || p.CorrespondingDatabaseID == bidsDatabaseReference.ID);
                m_Patients.AddRange(patients);
            }
            // Then load datasets
            List<Dataset> generatedDatasets = new();
            foreach (var localizerDatabaseReference in localizerDatabaseReferences)
            {
                Dataset.LoadFromLocalizersDatabase(localizerDatabaseReference.Path, out Dataset[] datasets, (progress, duration, text) => onChangeProgress.Invoke(progress, duration, text));
                foreach (var dataset in datasets)
                    foreach (var data in dataset.Data)
                        data.CorrespondingDatabaseID = localizerDatabaseReference.ID;
                generatedDatasets.AddRange(datasets);
            }
            foreach (var bidsDatabaseReference in bidsDatabaseReferences)
            {
                Dataset.LoadFromBIDSDatabase(bidsDatabaseReference.Path, out Dataset[] datasets, (progress, duration, text) => onChangeProgress.Invoke(progress, duration, text));
                foreach (var dataset in datasets)
                    foreach (var data in dataset.Data)
                        data.CorrespondingDatabaseID = bidsDatabaseReference.ID; generatedDatasets.AddRange(datasets);
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
            yield return Ninja.JumpToUnity;
            yield return CoroutineManager.StartAsync(c_SavePatients(new DirectoryInfo(ApplicationState.DatabasePath), onChangeProgress.Invoke));
            yield return CoroutineManager.StartAsync(c_SaveDatasets(new DirectoryInfo(ApplicationState.DatabasePath), onChangeProgress.Invoke));
            yield return CoroutineManager.StartAsync(c_SaveDatabaseReferences(new DirectoryInfo(ApplicationState.DatabasePath), onChangeProgress.Invoke));
            DialogBoxManager.Open(DialogBoxManager.AlertType.Informational, "Databases updated", "The databases have been updated successfully");
            onUpdated();
        }
        #endregion
    }
}