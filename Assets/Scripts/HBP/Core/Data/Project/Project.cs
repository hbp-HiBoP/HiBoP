using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Ionic.Zip;
using UnityEngine;
using HBP.Core.Exceptions;
using HBP.Core.Tools;
using HBP.Core.Interfaces;
using HBP.Core.Preferences;
using HBP.Core.Database;
using Cysharp.Threading.Tasks;
using System.Threading;
using LoadingOperation = HBP.Core.Tools.LoadingDiagnostics.Operation;
using LoadingPhase = HBP.Core.Tools.LoadingDiagnostics.Phase;

namespace HBP.Core.Data
{
    /**
    * \class Project
    * \author Adrien Gannerie
    * \version 1.0
    * \date 12 janvier 2017
    * \brief Class which define a HiBoP project.
    * 
    * \details Class which define a HiBoP project, it's contains :
    *     - Settings.
    *     - Patients.
    *     - Groups.
    *     - Regions of interest.(To Add)
    *     - Protocols.
    *     - Datasets.
    *     - Visualizations.
    */
    public class Project
    {
        #region Properties
        /// <summary>
        /// Project extension.
        /// </summary>
        public const string EXTENSION = ".hibop";
        public string Name { get; set; }
        /// <summary>
        /// Project file
        /// </summary>
        public string FileName
        {
            get
            {
                return Name + EXTENSION;
            }
        }

        /// <summary>
        /// Settings of the project.
        /// </summary>
        public ProjectPreferences Preferences { get; set; }

        List<Patient> m_Patients = new();
        /// <summary>
        /// Patients of the project.
        /// </summary>
        public ReadOnlyCollection<Patient> Patients
        {
            get { return new ReadOnlyCollection<Patient>(m_Patients); }
        }

        List<Group> m_Groups = new();
        /// <summary>
        /// Patient groups of the project.
        /// </summary>
        public ReadOnlyCollection<Group> Groups
        {
            get { return new ReadOnlyCollection<Group>(m_Groups); }
        }

        List<Dataset> m_Datasets = new();
        /// <summary>
        /// Datasets of the project.
        /// </summary>
        public ReadOnlyCollection<Dataset> Datasets
        {
            get { return new ReadOnlyCollection<Dataset>(m_Datasets); }
        }

        List<Visualization> m_Visualizations = new();
        /// <summary>
        /// Visualizations of the project.
        /// </summary>
        public ReadOnlyCollection<Visualization> Visualizations
        {
            get { return new ReadOnlyCollection<Visualization>(m_Visualizations); }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new project instance.
        /// </summary>
        /// <param name="settings">Settings of the project.</param>
        /// <param name="patients">Patients of the project.</param>
        /// <param name="groups">Groups of the project.</param>
        /// <param name="protocols">Protocols of the project.</param>
        /// <param name="datasets">Datasets of the project.</param>
        /// <param name="visualizations">Single patient visualizations of the project.</param>
        /// <param name="multiVisualizations">Multi patients visualizations of the project.</param>
        public Project(string name, ProjectPreferences settings, IEnumerable<Patient> patients, IEnumerable<Group> groups, IEnumerable<Dataset> datasets, IEnumerable<Visualization> visualizations)
        {
            Name = name;
            Preferences = settings;
            SetPatients(patients);
            SetGroups(groups);
            SetDatasets(datasets);
            SetVisualizations(visualizations);
        }
        /// <summary>
        /// Create a new project with only the settings.
        /// </summary>
        /// <param name="settings">Settings of the project.</param>
        public Project(string name, ProjectPreferences settings) : this(name, settings, new Patient[0], new Group[0], new Dataset[0], new Visualization[0])
        {
        }
        /// <summary>
        /// Create a empty project with a name.
        /// </summary>
        /// <param name="name">Name of the project.</param>
        public Project(string name) : this(name, new ProjectPreferences())
        {
        }
        /// <summary>
        /// Create a empty project with default values.
        /// </summary>
        public Project() : this(PersistentDataManager.UserPreferences.General.Project.DefaultName, new ProjectPreferences())
        {
        }
        #endregion

        #region Getter/Setter
        // Patients.
        /// <summary>
        /// Set the patients of the project.
        /// </summary>
        /// <param name="patients"></param>
        public void SetPatients(IEnumerable<Patient> patients)
        {
            m_Patients = new List<Patient>();
            AddPatient(patients);
            foreach (Dataset dataset in m_Datasets)
            {
                dataset.RemoveData(from data in dataset.GetPatientDataInfos() where !m_Patients.Any(p => p == data.Patient) select data);
                foreach (var patientDataInfo in dataset.Data.OfType<PatientDataInfo>())
                {
                    patientDataInfo.UpdatePatient();
                }
            }
            foreach (Visualization visualization in m_Visualizations)
            {
                visualization.UpdatePatients();
            }
            foreach (Group group in m_Groups)
            {
                group.UpdatePatients();
            }
        }
        public void AddPatient(Patient patient)
        {
            m_Patients.Add(patient);
        }
        public void AddPatient(IEnumerable<Patient> patients)
        {
            foreach (Patient patient in patients)
            {
                AddPatient(patient);
            }
        }
        public void RemovePatient(Patient patient)
        {
            foreach (Group group in m_Groups)
            {
                group.Patients.Remove(patient);
            }
            foreach (Dataset dataset in m_Datasets)
            {
                dataset.RemoveData(from data in dataset.GetPatientDataInfos() where data.Patient == patient select data);
            }
            foreach (Visualization visualization in m_Visualizations)
            {
                visualization.Patients.Remove(patient);
            }
            m_Patients.Remove(patient);
        }
        public void RemovePatient(IEnumerable<Patient> patients)
        {
            foreach (Patient patient in patients)
            {
                RemovePatient(patient);
            }
        }
        // Groups.
        public void SetGroups(IEnumerable<Group> groups)
        {
            this.m_Groups = new List<Group>();
            AddGroup(groups);
        }
        public void AddGroup(Group group)
        {
            m_Groups.Add(group);
        }
        public void AddGroup(IEnumerable<Group> groups)
        {
            foreach (Group group in groups)
            {
                AddGroup(group);
            }
        }
        public void RemoveGroup(Group group)
        {
            m_Groups.Remove(group);
        }
        public void RemoveGroup(IEnumerable<Group> groups)
        {
            foreach (Group group in groups)
            {
                RemoveGroup(group);
            }
        }
        // Datasets.
        public void SetDatasets(IEnumerable<Dataset> datasets)
        {
            m_Datasets = new List<Dataset>();
            AddDataset(datasets);
            foreach (Visualization visualization in m_Visualizations)
            {
                Column[] columnsToRemove = visualization.Columns.Where(ReferencesMissingDataset).ToArray();
                foreach (Column column in columnsToRemove)
                {
                    visualization.Columns.Remove(column);
                }
            }
        }
        public void AddDataset(Dataset dataset)
        {
            m_Datasets.Add(dataset);
        }
        public void AddDataset(IEnumerable<Dataset> datasets)
        {
            foreach (Dataset dataset in datasets)
            {
                AddDataset(dataset);
            }
        }
        public void RemoveDataset(Dataset dataset)
        {
            foreach (Visualization visualization in m_Visualizations)
            {
                visualization.Columns.RemoveAll(column => ReferencesDataset(column, dataset));
            }
            m_Datasets.Remove(dataset);
        }
        public void RemoveDataset(IEnumerable<Dataset> datasets)
        {
            foreach (Dataset dataset in datasets)
            {
                RemoveDataset(dataset);
            }
        }
        // Visualizations.
        public void SetVisualizations(IEnumerable<Visualization> visualizations)
        {
            this.m_Visualizations = new List<Visualization>();
            AddVisualization(visualizations);
        }
        public void AddVisualization(Visualization visualization)
        {
            m_Visualizations.Add(visualization);
        }
        public void AddVisualization(IEnumerable<Visualization> visualizations)
        {
            foreach (Visualization visualization in visualizations)
            {
                AddVisualization(visualization);
            }
        }
        public void RemoveVisualization(Visualization visualization)
        {
            m_Visualizations.Remove(visualization);
        }
        public void RemoveVisualization(IEnumerable<Visualization> visualizations)
        {
            foreach (Visualization visualization in visualizations)
            {
                RemoveVisualization(visualization);
            }
        }
        #endregion

        #region Public Methods
        public static bool IsProject(string path)
        {
            bool isProject = false;
            if (new FileInfo(path).Extension == EXTENSION)
            {
                try
                {
                    using ZipFile zip = ZipFile.Read(path);
                    bool hasPatientsDirectory = false;
                    bool hasGroupsDirectory = false;
                    bool hasDatasetsDirectory = false;
                    bool hasVisualizationsDirectory = false;
                    bool hasSettingsFile = false;
                    foreach (var entryFileName in zip.EntryFileNames)
                    {
                        if (entryFileName == "Patients/")
                        {
                            hasPatientsDirectory = true;
                        }
                        else if (entryFileName == "Groups/")
                        {
                            hasGroupsDirectory = true;
                        }
                        else if (entryFileName == "Datasets/")
                        {
                            hasDatasetsDirectory = true;
                        }
                        else if (entryFileName == "Visualizations/")
                        {
                            hasVisualizationsDirectory = true;
                        }
                        else if (entryFileName.EndsWith(ProjectPreferences.EXTENSION))
                        {
                            hasSettingsFile = true;
                        }
                    }
                    isProject = hasPatientsDirectory && hasGroupsDirectory && hasDatasetsDirectory && hasVisualizationsDirectory && hasSettingsFile;
                }
                catch
                {
                    isProject = false;
                }
            }
            return isProject;
        }
        public static IEnumerable<string> GetProject(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                DirectoryInfo directory = new(path);
                if (directory.Exists)
                {
                    FileInfo[] files = directory.GetFiles("*" + EXTENSION);
                    return from file in files where IsProject(file.FullName) select file.FullName;
                }
            }
            return new string[0];
        }
        public string GetProject(string path, string ID)
        {
            IEnumerable<string> projectsDirectories = GetProject(path);
            foreach (var directoryPaths in projectsDirectories)
            {
                ProjectInfo projectInfo = new(directoryPaths);
            }
            return projectsDirectories.FirstOrDefault((project) => new ProjectInfo(project).Settings.ID == ID);
        }
        public async UniTask<Dictionary<string, List<Tuple<BaseData, string>>>> CheckProjectIDsAsync()
        {
            return await UniTask.RunOnThreadPool(() =>
            {
                Dictionary<string, List<Tuple<BaseData, string>>> dataByID = new();
                void addToDict(BaseData data, string name)
                {
                    string id = data.ID ?? string.Empty;
                    if (dataByID.ContainsKey(id)) dataByID[id].Add(new Tuple<BaseData, string>(data, name));
                    else dataByID.Add(id, new List<Tuple<BaseData, string>>(new Tuple<BaseData, string>[] { new(data, name) }));
                }
                string getName(INameable data)
                {
                    return string.Format("{0} ({1})", data.Name, getType(data as BaseData));
                }
                string getType(BaseData data)
                {
                    return data.GetType().ToString().Split('.').Last();
                }
                // Settings
                addToDict(Preferences, getType(Preferences));
                // Patients
                foreach (var patient in m_Patients)
                {
                    addToDict(patient, getName(patient));
                    foreach (var mesh in patient.Meshes) addToDict(mesh, string.Format("{0} / {1}", getName(patient), getName(mesh)));
                    foreach (var mri in patient.MRIs) addToDict(mri, string.Format("{0} / {1}", getName(patient), getName(mri)));
                    foreach (var site in patient.Sites)
                    {
                        addToDict(site, string.Format("{0} / {1}", getName(patient), getName(site)));
                        foreach (var coordinate in site.Coordinates) addToDict(coordinate, string.Format("{0} / {1} / {2}", getName(patient), getName(site), string.Format("{0} ({1})", coordinate.ReferenceSystem, getType(coordinate))));
                        foreach (var tagValue in site.Tags) addToDict(tagValue, string.Format("{0} / {1} / {2}", getName(patient), getName(site), string.Format("{0} ({1})", tagValue.Tag.Name, getType(tagValue))));
                    }
                    foreach (var tagValue in patient.Tags) addToDict(tagValue, string.Format("{0} / {1}", getName(patient), string.Format("{0} ({1})", tagValue.Tag.Name, getType(tagValue))));
                }
                // Groups
                foreach (var group in m_Groups) addToDict(group, getName(group));
                // Datasets
                foreach (var dataset in m_Datasets)
                {
                    addToDict(dataset, getName(dataset));
                    foreach (var data in dataset.Data)
                    {
                        addToDict(data, string.Format("{0} / {1}", getName(dataset), getName(data)));
                        addToDict(data.DataContainer, string.Format("{0} / {1} / {2}", getName(dataset), getName(data), getType(data.DataContainer)));
                    }
                }
                // Visualizations
                foreach (var visualization in m_Visualizations)
                {
                    addToDict(visualization, getName(visualization));
                    foreach (var column in visualization.Columns)
                    {
                        addToDict(column, string.Format("{0} / {1}", getName(visualization), getName(column)));
                        addToDict(column.BaseConfiguration, string.Format("{0} / {1} / {2}", getName(visualization), getName(column), getType(column.BaseConfiguration)));
                        foreach (var siteConfig in column.BaseConfiguration.ConfigurationBySite) addToDict(siteConfig.Value, string.Format("{0} / {1} / {2})", getName(visualization), getName(column), string.Format("{0} ({1})", siteConfig.Key, getType(siteConfig.Value))));
                    }
                    foreach (var anatomicColumn in visualization.AnatomicColumns) addToDict(anatomicColumn.AnatomicConfiguration, string.Format("{0} / {1} / {2}", getName(visualization), getName(anatomicColumn), getType(anatomicColumn.AnatomicConfiguration)));
                    foreach (var ieegColumn in visualization.IEEGColumns) addToDict(ieegColumn.DynamicConfiguration, string.Format("{0} / {1} / {2}", getName(visualization), getName(ieegColumn), getType(ieegColumn.DynamicConfiguration)));
                    foreach (var ccepColumn in visualization.CCEPColumns) addToDict(ccepColumn.DynamicConfiguration, string.Format("{0} / {1} / {2}", getName(visualization), getName(ccepColumn), getType(ccepColumn.DynamicConfiguration)));
                    foreach (var fmriColumn in visualization.FMRIColumns) addToDict(fmriColumn.FMRIConfiguration, string.Format("{0} / {1} / {2}", getName(visualization), getName(fmriColumn), getType(fmriColumn.FMRIConfiguration)));
                    foreach (var megColumn in visualization.MEGColumns) addToDict(megColumn.MEGConfiguration, string.Format("{0} / {1} / {2}", getName(visualization), getName(megColumn), getType(megColumn.MEGConfiguration)));
                }
                // Check unicity and return error string
                Dictionary<string, List<Tuple<BaseData, string>>> problematicData = new();
                foreach (var kv in dataByID) if (kv.Value.Count > 1 || string.IsNullOrEmpty(kv.Key)) problematicData.Add(kv.Key, kv.Value);
                return problematicData;
            });
        }

        public async UniTask LoadAsync(ProjectInfo projectInfo, Action<float, float, LoadingText> updateProgress, CancellationToken token)
        {
            // TEMP-LOADING-PROFILING
            using LoadingDiagnostics.SessionScope session = LoadingDiagnostics.BeginSession(LoadingOperation.Project);
            try
            {
                // Initialize progress.
                float progress = 0.0f;
                float settingsProgress = 1;
                float patientsProgress = 2 * projectInfo.Patients;
                float groupsProgress = projectInfo.Groups;
                float datasetsProgress = projectInfo.Datasets;
                float visualizationsProgress = projectInfo.Visualizations;
                float steps = settingsProgress + groupsProgress + patientsProgress + datasetsProgress + visualizationsProgress;
                settingsProgress /= steps;
                patientsProgress /= steps;
                groupsProgress /= steps;
                datasetsProgress /= steps;
                visualizationsProgress /= steps;

                updateProgress.Invoke(progress, 0, new LoadingText("Loading project"));

                // Unzipping
                await UniTask.SwitchToThreadPool();
                // TEMP-LOADING-PROFILING
                using (LoadingDiagnostics.BeginPhase(
                    LoadingPhase.ProjectArchiveRead,
                    fileCount: 1,
                    byteCount: LoadingDiagnostics.GetFileLength(projectInfo.Path)))
                {
                    if (Directory.Exists(ApplicationState.ExtractProjectFolder)) Directory.Delete(ApplicationState.ExtractProjectFolder, true);
                    using ZipFile zip = ZipFile.Read(projectInfo.Path);
                    zip.ExtractAll(ApplicationState.ExtractProjectFolder, ExtractExistingFileAction.OverwriteSilently);
                    if (!File.Exists(projectInfo.Path)) throw new FileNotFoundException(projectInfo.Path); // Test if the file exists.
                    if (!IsProject(projectInfo.Path)) throw new FileNotFoundException(projectInfo.Path); // Test if the file is a project.
                }
                DirectoryInfo projectDirectory = new(ApplicationState.ExtractProjectFolder);

                Name = projectInfo.Name;

                // Load Settings.
                token.ThrowIfCancellationRequested();
                await LoadSettingsAsync(projectDirectory, (localProgress, duration, text) => updateProgress.Invoke(progress + localProgress * settingsProgress, duration, text));
                token.ThrowIfCancellationRequested();
                progress += settingsProgress;

                // Load Patients.
                token.ThrowIfCancellationRequested();
                await LoadPatientsAsync(projectDirectory, (localProgress, duration, text) => updateProgress.Invoke(progress + localProgress * patientsProgress, duration, text), token);
                token.ThrowIfCancellationRequested();
                progress += patientsProgress;

                // Load Groups.
                token.ThrowIfCancellationRequested();
                await LoadGroupsAsync(projectDirectory, (localProgress, duration, text) => updateProgress.Invoke(progress + localProgress * groupsProgress, duration, text), token);
                token.ThrowIfCancellationRequested();
                progress += groupsProgress;

                // Load Datasets.
                token.ThrowIfCancellationRequested();
                await LoadDatasetsAsync(projectDirectory, (localProgress, duration, text) => updateProgress.Invoke(progress + localProgress * datasetsProgress, duration, text), token);
                token.ThrowIfCancellationRequested();
                progress += datasetsProgress;

                // Load Visualizations.
                token.ThrowIfCancellationRequested();
                await LoadVisualizationsAsync(projectDirectory, (localProgress, duration, text) => updateProgress.Invoke(progress + localProgress * visualizationsProgress, duration, text), token);
                token.ThrowIfCancellationRequested();
                progress += visualizationsProgress;

                token.ThrowIfCancellationRequested();
                updateProgress.Invoke(1.0f, 0, new LoadingText("Project loaded successfully"));
                session.MarkSucceeded();
            }
            catch (OperationCanceledException)
            {
                session.MarkCanceled();
                throw;
            }
            catch (Exception exception)
            {
                session.MarkFailed(exception);
                throw;
            }
            finally
            {
                if (Directory.Exists(ApplicationState.ExtractProjectFolder)) Directory.Delete(ApplicationState.ExtractProjectFolder, true);
                await UniTask.SwitchToMainThread();
            }
        }
        public async UniTask SaveAsync(string path, Action<float, float, LoadingText> updateProgress, CancellationToken token)
        {
            try
            {
                // Initialize progress.
                float steps = 12 + m_Patients.Count + m_Groups.Count + m_Datasets.Count + m_Visualizations.Count;
                float progress = 0.0f;

                float initializationProgress = 1 / steps;
                float settingsProgress = 1 / steps;
                float patientsProgress = m_Patients.Count / steps;
                float groupsProgress = m_Groups.Count / steps;
                float datasetsProgress = m_Datasets.Count / steps;
                float visualizationsProgress = m_Visualizations.Count / steps;
                float finalizationProgress = 10 / steps;

                // Initialization.
                updateProgress.Invoke(progress, 0, new LoadingText("Initialization"));

                if (string.IsNullOrEmpty(path)) throw new Exceptions.DirectoryNotFoundException("");
                if (!Directory.Exists(path)) throw new Exceptions.DirectoryNotFoundException(path);
                DirectoryInfo projectDirectory = Directory.Exists(ApplicationState.ExtractProjectFolder) ? new DirectoryInfo(ApplicationState.ExtractProjectFolder) : Directory.CreateDirectory(ApplicationState.ExtractProjectFolder);
                progress += initializationProgress;

                updateProgress.Invoke(progress, 0, new LoadingText("Saving project"));

                if (HasInvalidFileNameChars(FileName))
                {
                    throw new CanNotSaveSettingsException();
                }

                // Save Settings.
                token.ThrowIfCancellationRequested();
                await SaveSettingsAsync(projectDirectory, (localProgress, duration, text) => updateProgress.Invoke(progress + localProgress * settingsProgress, duration, text));
                progress += settingsProgress;

                // Save Patients
                token.ThrowIfCancellationRequested();
                await SavePatientsAsync(projectDirectory, (localProgress, duration, text) => updateProgress.Invoke(progress + localProgress * patientsProgress, duration, text), token);
                progress += patientsProgress;

                // Save Groups.
                token.ThrowIfCancellationRequested();
                await SaveGroupsAsync(projectDirectory, (localProgress, duration, text) => updateProgress.Invoke(progress + localProgress * groupsProgress, duration, text), token);
                progress += groupsProgress;

                // Save Datasets
                token.ThrowIfCancellationRequested();
                await SaveDatasetsAsync(projectDirectory, (localProgress, duration, text) => updateProgress.Invoke(progress + localProgress * datasetsProgress, duration, text), token);
                progress += datasetsProgress;

                // Save Visualizations.
                token.ThrowIfCancellationRequested();
                await SaveVisualizationsAsync(projectDirectory, (localProgress, duration, text) => updateProgress.Invoke(progress + localProgress * visualizationsProgress, duration, text), token);
                progress += visualizationsProgress;

                // Zipping
                token.ThrowIfCancellationRequested();
                updateProgress.Invoke(progress + finalizationProgress, 0.75f, new LoadingText("Finalizing"));
                progress += finalizationProgress;
                await UniTask.SwitchToThreadPool();
                string filePath = Path.Combine(path, FileName);
                if (File.Exists(filePath)) File.Delete(filePath);
                using (ZipFile zip = new(filePath))
                {
                    zip.AddDirectory(ApplicationState.ExtractProjectFolder);
                    zip.Save();
                }
                await UniTask.SwitchToMainThread();

                updateProgress.Invoke(1, 0, new LoadingText("Project saved successfully"));
            }
            catch
            {
                throw;
            }
            finally
            {
                if (Directory.Exists(ApplicationState.ExtractProjectFolder)) Directory.Delete(ApplicationState.ExtractProjectFolder, true);
                await UniTask.SwitchToMainThread();
            }
        }
        #endregion

        #region Private Methods
        private async UniTask LoadSettingsAsync(DirectoryInfo projectDirectory, Action<float, float, LoadingText> updateProgress)
        {
            updateProgress.Invoke(0, 0, new LoadingText("Loading settings"));
            FileInfo[] settingsFiles = projectDirectory.GetFiles("*" + ProjectPreferences.EXTENSION, SearchOption.TopDirectoryOnly);
            if (settingsFiles.Length == 0) throw new SettingsFileNotFoundException(); // Test if settings files found.
            else if (settingsFiles.Length > 1) throw new MultipleSettingsFilesFoundException(); // Test if multiple settings files found.
            try
            {
                Preferences = await ClassLoaderSaver.LoadFromJsonAsync<ProjectPreferences>(
                    settingsFiles[0].FullName,
                    LoadingPhase.ProjectSettings,
                    LoadingPhase.ProjectSettings,
                    1);
                LoadingDiagnostics.RecordObjects("ProjectPreferences", 1);
            }
            catch (Exception e)
            {
                throw new CanNotReadSettingsFileException(settingsFiles[0].Name, e);
            }
            updateProgress.Invoke(1.0f, 0, new LoadingText("Settings loaded successfully"));
        }

        private bool ReferencesMissingDataset(Column column)
        {
            Dataset dataset = GetColumnDataset(column);
            return IsDatasetBackedColumn(column) && (dataset == null || !m_Datasets.Contains(dataset));
        }

        private static bool ReferencesDataset(Column column, Dataset dataset)
        {
            return GetColumnDataset(column) == dataset;
        }

        private static bool IsDatasetBackedColumn(Column column)
        {
            return column is IEEGColumn or CCEPColumn or FMRIColumn or MEGColumn or StaticColumn;
        }

        private static Dataset GetColumnDataset(Column column)
        {
            return column switch
            {
                IEEGColumn ieegColumn => ieegColumn.Dataset,
                CCEPColumn ccepColumn => ccepColumn.Dataset,
                FMRIColumn fmriColumn => fmriColumn.Dataset,
                MEGColumn megColumn => megColumn.Dataset,
                StaticColumn staticColumn => staticColumn.Dataset,
                _ => null
            };
        }
        private async UniTask LoadPatientsAsync(DirectoryInfo projectDirectory, Action<float, float, LoadingText> updateProgress, CancellationToken token)
        {
            List<Patient> patients = new();
            DirectoryInfo patientDirectory = projectDirectory.GetDirectories("Patients", SearchOption.TopDirectoryOnly)[0];
            FileInfo[] patientFiles = patientDirectory.GetFiles("*" + Patient.EXTENSION, SearchOption.TopDirectoryOnly);
            int concurrency = PersistentDataManager.UserPreferences.General.System.MultiThreading ? 20 : 1;
            ISet<string> tagIds = new HashSet<string>(
                PersistentDataManager.Tags.AllTags
                    .Where(tag => tag != null && !string.IsNullOrEmpty(tag.ID))
                    .Select(tag => tag.ID),
                StringComparer.Ordinal);
            var tasks = patientFiles.Select(file => (Func<UniTask<Patient>>)(async () =>
            {
                try
                {
                    var patient = await ClassLoaderSaver.LoadFromJsonAsync<Patient>(
                        file.FullName,
                        LoadingPhase.ProjectPatientsRead,
                        LoadingPhase.ProjectPatientsDeserialize,
                        concurrency);
                    LoadingDiagnostics.RecordPatientGraph(patient);
                    using (LoadingDiagnostics.BeginPhase(LoadingPhase.ProjectPatientsBindTags, concurrency: concurrency))
                    {
                        await patient.CheckTagsAsync(tagIds);
                    }
                    return patient;
                }
                catch (Exception e)
                {
                    throw new CanNotReadPatientFileException(Path.GetFileNameWithoutExtension(file.Name), e);
                }
            }));
            patients.AddRange(await Tools.UniTaskExtensions.PerformMultipleTasksAsync(tasks, 0, 1, "Loading patients", updateProgress, 20, PersistentDataManager.UserPreferences.General.System.MultiThreading, token));
            // TEMP-LOADING-PROFILING
            using (LoadingDiagnostics.BeginPhase(
                LoadingPhase.ProjectValidateFiles,
                objectCount: patients.Count,
                concurrency: concurrency))
            {
                await new AssetReferenceValidator().ValidatePatientsAsync(
                    patients,
                    concurrency,
                    token);
            }
            // TEMP-LOADING-PROFILING
            using (LoadingDiagnostics.BeginPhase(LoadingPhase.ProjectLinkReferences, objectCount: patients.Count))
            {
                SetPatients(patients.ToArray());
            }
            updateProgress.Invoke(1.0f, 0, new LoadingText("Patients loaded successfully"));
        }
        private async UniTask LoadGroupsAsync(DirectoryInfo projectDirectory, Action<float, float, LoadingText> updateProgress, CancellationToken token)
        {
            List<Group> groups = new();
            DirectoryInfo groupDirectory = projectDirectory.GetDirectories("Groups", SearchOption.TopDirectoryOnly)[0];
            FileInfo[] groupFiles = groupDirectory.GetFiles("*" + Group.EXTENSION, SearchOption.TopDirectoryOnly);
            int concurrency = PersistentDataManager.UserPreferences.General.System.MultiThreading ? 20 : 1;
            var tasks = groupFiles.Select(file => (Func<UniTask<Group>>)(async () =>
            {
                try
                {
                    return await ClassLoaderSaver.LoadFromJsonAsync<Group>(
                        file.FullName,
                        LoadingPhase.ProjectGroups,
                        LoadingPhase.ProjectGroups,
                        concurrency);
                }
                catch (Exception e)
                {
                    throw new CanNotReadGroupFileException(Path.GetFileNameWithoutExtension(file.Name), e);
                }
            }));
            groups.AddRange(await Tools.UniTaskExtensions.PerformMultipleTasksAsync(tasks, 0, 1, "Loading groups", updateProgress, 20, PersistentDataManager.UserPreferences.General.System.MultiThreading, token));
            LoadingDiagnostics.RecordObjects("Group", groups.Count);
            // TEMP-LOADING-PROFILING
            using (LoadingDiagnostics.BeginPhase(LoadingPhase.ProjectLinkReferences, objectCount: groups.Count))
            {
                SetGroups(groups.ToArray());
            }
            updateProgress.Invoke(1.0f, 0, new LoadingText("Groups loaded successfully"));
        }
        private async UniTask LoadDatasetsAsync(DirectoryInfo projectDirectory, Action<float, float, LoadingText> updateProgress, CancellationToken token)
        {
            List<Dataset> datasets = new();
            DirectoryInfo datasetDirectory = projectDirectory.GetDirectories("Datasets", SearchOption.TopDirectoryOnly)[0];
            FileInfo[] datasetFiles = datasetDirectory.GetFiles("*" + Dataset.EXTENSION, SearchOption.TopDirectoryOnly);
            int concurrency = PersistentDataManager.UserPreferences.General.System.MultiThreading ? 20 : 1;
            var tasks = datasetFiles.Select(file => (Func<UniTask<Dataset>>)(async () =>
            {
                try
                {
                    return await ClassLoaderSaver.LoadFromJsonAsync<Dataset>(
                        file.FullName,
                        LoadingPhase.ProjectDatasets,
                        LoadingPhase.ProjectDatasets,
                        concurrency);
                }
                catch (Exception e)
                {
                    throw new CanNotReadDatasetFileException(Path.GetFileNameWithoutExtension(file.Name), e);
                }
            }));
            datasets.AddRange(await Tools.UniTaskExtensions.PerformMultipleTasksAsync(tasks, 0, 1, "Loading datasets", updateProgress, 20, PersistentDataManager.UserPreferences.General.System.MultiThreading, token));
            LoadingDiagnostics.RecordObjects("Dataset", datasets.Count);
            // TEMP-LOADING-PROFILING
            using (LoadingDiagnostics.BeginPhase(LoadingPhase.ProjectLinkReferences, objectCount: datasets.Count))
            {
                SetDatasets(datasets.ToArray());
            }
            updateProgress.Invoke(1.0f, 0, new LoadingText("Datasets loaded successfully"));
        }
        private async UniTask LoadVisualizationsAsync(DirectoryInfo projectDirectory, Action<float, float, LoadingText> updateProgress, CancellationToken token)
        {
            DirectoryInfo visualizationsDirectory = projectDirectory.GetDirectories("Visualizations", SearchOption.TopDirectoryOnly)[0];
            List<Visualization> visualizations = new();
            FileInfo[] visualizationFiles = visualizationsDirectory.GetFiles("*" + Visualization.EXTENSION, SearchOption.TopDirectoryOnly);
            int concurrency = PersistentDataManager.UserPreferences.General.System.MultiThreading ? 20 : 1;
            var tasks = visualizationFiles.Select(file => (Func<UniTask<Visualization>>)(async () =>
            {
                try
                {
                    return await ClassLoaderSaver.LoadFromJsonAsync<Visualization>(
                        file.FullName,
                        LoadingPhase.ProjectVisualizations,
                        LoadingPhase.ProjectVisualizations,
                        concurrency);
                }
                catch (Exception e)
                {
                    throw new CanNotReadVisualizationFileException(Path.GetFileNameWithoutExtension(file.Name), e);
                }
            }));
            visualizations.AddRange(await Tools.UniTaskExtensions.PerformMultipleTasksAsync(tasks, 0, 1, "Loading visualizations", updateProgress, 20, PersistentDataManager.UserPreferences.General.System.MultiThreading, token));
            LoadingDiagnostics.RecordObjects("Visualization", visualizations.Count);
            // TEMP-LOADING-PROFILING
            using (LoadingDiagnostics.BeginPhase(LoadingPhase.ProjectLinkReferences, objectCount: visualizations.Count))
            {
                SetVisualizations(visualizations.ToArray());
            }
            updateProgress.Invoke(1.0f, 0, new LoadingText("Visualizations loaded successfully"));
        }

        private async UniTask SaveSettingsAsync(DirectoryInfo projectDirectory, Action<float, float, LoadingText> updateProgress)
        {
            updateProgress.Invoke(0, 0, new LoadingText("Saving settings"));
            try
            {
                Preferences.Version = ApplicationState.Version;
                await ClassLoaderSaver.SaveToJsonAsync(Preferences, Path.Combine(projectDirectory.FullName, Name + ProjectPreferences.EXTENSION));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw new CanNotSaveSettingsException();
            }
            updateProgress.Invoke(1.0f, 0, new LoadingText("Settings saved successfully"));
        }
        private async UniTask SavePatientsAsync(DirectoryInfo projectDirectory, Action<float, float, LoadingText> updateProgress, CancellationToken token)
        {
            DirectoryInfo patientDirectory = Directory.CreateDirectory(Path.Combine(projectDirectory.FullName, "Patients"));
            HashSet<string> reservedPaths = new(StringComparer.OrdinalIgnoreCase);
            var tasks = m_Patients.Select(patient =>
            {
                string patientPath = ReserveUniqueFilePath(Path.Combine(patientDirectory.FullName, BuildSafeFileName(patient.ID, Patient.EXTENSION, "patient")), reservedPaths);
                return (Func<UniTask>)(async () =>
                {
                    try
                    {
                        await ClassLoaderSaver.SaveToJsonAsync(patient, patientPath, true);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        throw new CanNotSaveSettingsException();
                    }
                });
            });
            await Tools.UniTaskExtensions.PerformMultipleTasksAsync(tasks, 0, 1, "Saving patients", updateProgress, 20, PersistentDataManager.UserPreferences.General.System.MultiThreading, token);
            updateProgress.Invoke(1.0f, 0, new LoadingText("Patients saved successfully"));
        }
        private async UniTask SaveGroupsAsync(DirectoryInfo projectDirectory, Action<float, float, LoadingText> updateProgress, CancellationToken token)
        {
            DirectoryInfo groupDirectory = Directory.CreateDirectory(Path.Combine(projectDirectory.FullName, "Groups"));
            HashSet<string> reservedPaths = new(StringComparer.OrdinalIgnoreCase);
            var tasks = m_Groups.Select(group =>
            {
                string groupPath = ReserveUniqueFilePath(Path.Combine(groupDirectory.FullName, BuildSafeFileName(group.Name, Group.EXTENSION, "group")), reservedPaths);
                return (Func<UniTask>)(async () =>
                {
                    try
                    {
                        await ClassLoaderSaver.SaveToJsonAsync(group, groupPath, true);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        throw new CanNotSaveSettingsException();
                    }
                });
            });
            await Tools.UniTaskExtensions.PerformMultipleTasksAsync(tasks, 0, 1, "Saving groups", updateProgress, 20, PersistentDataManager.UserPreferences.General.System.MultiThreading, token);
            updateProgress.Invoke(1.0f, 0, new LoadingText("Groups saved successfully"));
        }
        private async UniTask SaveDatasetsAsync(DirectoryInfo projectDirectory, Action<float, float, LoadingText> updateProgress, CancellationToken token)
        {
            DirectoryInfo datasetDirectory = Directory.CreateDirectory(Path.Combine(projectDirectory.FullName, "Datasets"));
            HashSet<string> reservedPaths = new(StringComparer.OrdinalIgnoreCase);
            var tasks = m_Datasets.Select(dataset =>
            {
                string datasetPath = ReserveUniqueFilePath(Path.Combine(datasetDirectory.FullName, BuildSafeFileName(dataset.Name, Dataset.EXTENSION, "dataset")), reservedPaths);
                return (Func<UniTask>)(async () =>
                {
                    try
                    {
                        await ClassLoaderSaver.SaveToJsonAsync(dataset, datasetPath, true);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        throw new CanNotSaveSettingsException();
                    }
                });
            });
            await Tools.UniTaskExtensions.PerformMultipleTasksAsync(tasks, 0, 1, "Saving datasets", updateProgress, 20, PersistentDataManager.UserPreferences.General.System.MultiThreading, token);
            updateProgress.Invoke(1.0f, 0, new LoadingText("Datasets saved successfully"));
        }
        private async UniTask SaveVisualizationsAsync(DirectoryInfo projectDirectory, Action<float, float, LoadingText> updateProgress, CancellationToken token)
        {
            DirectoryInfo visualizationDirectory = Directory.CreateDirectory(Path.Combine(projectDirectory.FullName, "Visualizations"));
            HashSet<string> reservedPaths = new(StringComparer.OrdinalIgnoreCase);
            var tasks = m_Visualizations.Select(visualization =>
            {
                string visualizationPath = ReserveUniqueFilePath(Path.Combine(visualizationDirectory.FullName, BuildSafeFileName(visualization.Name, Visualization.EXTENSION, "visualization")), reservedPaths);
                return (Func<UniTask>)(async () =>
                {
                    try
                    {
                        await ClassLoaderSaver.SaveToJsonAsync(visualization, visualizationPath, true);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        throw new CanNotSaveSettingsException();
                    }
                });
            });
            await Tools.UniTaskExtensions.PerformMultipleTasksAsync(tasks, 0, 1, "Saving visualizations", updateProgress, 20, PersistentDataManager.UserPreferences.General.System.MultiThreading, token);
            updateProgress.Invoke(1.0f, 0, new LoadingText("Visualizations saved successfully"));
        }

        private static string ReserveUniqueFilePath(string path, ISet<string> reservedPaths)
        {
            string result = path;
            string extension = Path.GetExtension(path);
            string pathWithoutExtension = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));
            int count = 0;
            while (File.Exists(result) || reservedPaths.Contains(result))
            {
                result = string.Format("{0}({1}){2}", pathWithoutExtension, ++count, extension);
            }
            reservedPaths.Add(result);
            return result;
        }

        private static string BuildSafeFileName(string fileNameWithoutExtension, string extension, string fallbackName)
        {
            string safeName = string.IsNullOrWhiteSpace(fileNameWithoutExtension) ? fallbackName : fileNameWithoutExtension;
            foreach (char invalidChar in Path.GetInvalidFileNameChars())
            {
                safeName = safeName.Replace(invalidChar, '_');
            }
            if (string.IsNullOrWhiteSpace(safeName))
            {
                safeName = fallbackName;
            }
            return safeName + extension;
        }

        private static bool HasInvalidFileNameChars(string fileName)
        {
            return fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0;
        }
        #endregion
    }
}
