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
using HBP.Data.Preferences;
using HBP.Data.Database;
using Cysharp.Threading.Tasks;

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
        /// <summary>
        /// Project file
        /// </summary>
        public string FileName
        {
            get
            {
                return Preferences.Name + EXTENSION;
            }
        }

        /// <summary>
        /// Settings of the project.
        /// </summary>
        public ProjectPreferences Preferences { get; set; }

        List<Patient> m_Patients = new List<Patient>();
        /// <summary>
        /// Patients of the project.
        /// </summary>
        public ReadOnlyCollection<Patient> Patients
        {
            get { return new ReadOnlyCollection<Patient>(m_Patients); }
        }

        List<Group> m_Groups = new List<Group>();
        /// <summary>
        /// Patient groups of the project.
        /// </summary>
        public ReadOnlyCollection<Group> Groups
        {
            get { return new ReadOnlyCollection<Group>(m_Groups); }
        }

        List<Dataset> m_Datasets = new List<Dataset>();
        /// <summary>
        /// Datasets of the project.
        /// </summary>
        public ReadOnlyCollection<Dataset> Datasets
        {
            get { return new ReadOnlyCollection<Dataset>(m_Datasets); }
        }

        List<Visualization> m_Visualizations = new List<Visualization>();
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
        public Project(ProjectPreferences settings, IEnumerable<Patient> patients, IEnumerable<Group> groups, IEnumerable<Dataset> datasets, IEnumerable<Visualization> visualizations)
        {
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
        public Project(ProjectPreferences settings) : this(settings, new Patient[0], new Group[0], new Dataset[0], new Visualization[0])
        {
        }
        /// <summary>
        /// Create a empty project with a name.
        /// </summary>
        /// <param name="name">Name of the project.</param>
        public Project(string name) : this(new ProjectPreferences(name))
        {
        }
        /// <summary>
        /// Create a empty project with default values.
        /// </summary>
        public Project() : this(new ProjectPreferences())
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
            }
            foreach (Visualization visualization in m_Visualizations)
            {
                visualization.Patients.RemoveAll(patient => !m_Patients.Contains(patient));
            }
            foreach (Group _group in m_Groups)
            {
                _group.Patients.RemoveAll(patient => !m_Patients.Contains(patient));
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
                Column[] columnsToRemove = visualization.Columns.Where(column => column is IEEGColumn && !m_Datasets.Any(d => d == (column as IEEGColumn).Dataset)).ToArray();
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
                visualization.Columns.RemoveAll((column) => (column is IEEGColumn) && (column as IEEGColumn).Dataset == dataset);
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
            return isProject;
        }
        public static IEnumerable<string> GetProject(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                DirectoryInfo directory = new DirectoryInfo(path);
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
                ProjectInfo projectInfo = new ProjectInfo(directoryPaths);
            }
            return projectsDirectories.FirstOrDefault((project) => new ProjectInfo(project).Settings.ID == ID);
        }
        public async UniTask<Dictionary<string, List<Tuple<BaseData, string>>>> CheckProjectIDsAsync()
        {
            return await UniTask.RunOnThreadPool(() =>
            {
                Dictionary<string, List<Tuple<BaseData, string>>> dataByID = new Dictionary<string, List<Tuple<BaseData, string>>>();
                void addToDict(BaseData data, string name)
                {
                    if (dataByID.ContainsKey(data.ID)) dataByID[data.ID].Add(new Tuple<BaseData, string>(data, name));
                    else dataByID.Add(data.ID, new List<Tuple<BaseData, string>>(new Tuple<BaseData, string>[] { new Tuple<BaseData, string>(data, name) }));
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
                Dictionary<string, List<Tuple<BaseData, string>>> problematicData = new Dictionary<string, List<Tuple<BaseData, string>>>();
                foreach (var kv in dataByID) if (kv.Value.Count > 1) problematicData.Add(kv.Key, kv.Value);
                return problematicData;
            });
        }

        public async UniTask LoadAsync(ProjectInfo projectInfo, Action<float, float, LoadingText> updateProgress)
        {
            // Initialize progress.
            float progress = 0.0f;
            float settingsProgress = 1;
            float patientsProgress = 2 * projectInfo.Patients;
            float groupsProgress = projectInfo.Groups;
            float protocolsProgress = projectInfo.Protocols;
            float datasetsProgress = projectInfo.Patients * projectInfo.Datasets;
            float visualizationsProgress = projectInfo.Visualizations;
            float steps = settingsProgress + groupsProgress + patientsProgress + protocolsProgress + datasetsProgress + visualizationsProgress;
            settingsProgress /= steps;
            patientsProgress /= steps;
            groupsProgress /= steps;
            protocolsProgress /= steps;
            datasetsProgress /= steps;
            visualizationsProgress /= steps;

            updateProgress.Invoke(progress, 0, new LoadingText("Loading project"));

            // Unzipping
            await UniTask.SwitchToThreadPool();
            if (Directory.Exists(ApplicationState.ExtractProjectFolder)) Directory.Delete(ApplicationState.ExtractProjectFolder, true);
            using ZipFile zip = ZipFile.Read(projectInfo.Path);
            zip.ExtractAll(ApplicationState.ExtractProjectFolder, ExtractExistingFileAction.OverwriteSilently);
            if (!File.Exists(projectInfo.Path)) throw new FileNotFoundException(projectInfo.Path); // Test if the file exists.
            if (!IsProject(projectInfo.Path)) throw new FileNotFoundException(projectInfo.Path); // Test if the file is a project.
            DirectoryInfo projectDirectory = new DirectoryInfo(ApplicationState.ExtractProjectFolder);

            // Load Settings.
            await LoadSettingsAsync(projectDirectory, (localProgress, duration, text) => updateProgress.Invoke(progress + localProgress * settingsProgress, duration, text));
            progress += settingsProgress;

            // Load Patients.
            await LoadPatientsAsync(projectDirectory, (localProgress, duration, text) => updateProgress.Invoke(progress + localProgress * patientsProgress, duration, text));
            progress += patientsProgress;

            // Load Groups.
            await LoadGroupsAsync(projectDirectory, (localProgress, duration, text) => updateProgress.Invoke(progress + localProgress * groupsProgress, duration, text));
            progress += groupsProgress;

            // Load Datasets.
            await LoadDatasetsAsync(projectDirectory, (localProgress, duration, text) => updateProgress.Invoke(progress + localProgress * datasetsProgress, duration, text));
            progress += datasetsProgress;

            // Load Visualizations.
            await LoadVisualizationsAsync(projectDirectory, (localProgress, duration, text) => updateProgress.Invoke(progress + localProgress * visualizationsProgress, duration, text));
            progress += visualizationsProgress;

            Directory.Delete(ApplicationState.ExtractProjectFolder, true);
            updateProgress.Invoke(1.0f, 0, new LoadingText("Project loaded successfully."));
        }
        public async UniTask SaveAsync(string path, Action<float, float, LoadingText> updateProgress)
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

            // Save Settings.
            await SaveSettingsAsync(projectDirectory, (localProgress, duration, text) => updateProgress.Invoke(progress + localProgress * settingsProgress, duration, text));
            progress += settingsProgress;

            // Save Patients
            await SavePatientsAsync(projectDirectory, (localProgress, duration, text) => updateProgress.Invoke(progress + localProgress * patientsProgress, duration, text));
            progress += patientsProgress;

            // Save Groups.
            await SaveGroupsAsync(projectDirectory, (localProgress, duration, text) => updateProgress.Invoke(progress + localProgress * groupsProgress, duration, text));
            progress += groupsProgress;

            // Save Datasets
            await SaveDatasetsAsync(projectDirectory, (localProgress, duration, text) => updateProgress.Invoke(progress + localProgress * datasetsProgress, duration, text));
            progress += datasetsProgress;

            // Save Visualizations.
            await SaveVisualizationsAsync(projectDirectory, (localProgress, duration, text) => updateProgress.Invoke(progress + localProgress * visualizationsProgress, duration, text));
            progress += visualizationsProgress;

            // Deleting old directories.
            updateProgress.Invoke(progress + finalizationProgress, 0.75f, new LoadingText("Finalizing"));
            progress += finalizationProgress;

            // Zipping
            await UniTask.SwitchToThreadPool();
            string filePath = Path.Combine(path, FileName);
            if (File.Exists(filePath)) File.Delete(filePath);
            using (ZipFile zip = new(filePath))
            {
                zip.AddDirectory(ApplicationState.ExtractProjectFolder);
                zip.Save();
            }
            Directory.Delete(ApplicationState.ExtractProjectFolder, true);
            await UniTask.SwitchToMainThread();

            updateProgress.Invoke(1, 0, new LoadingText("Project saved successfully"));
        }

        public async UniTask CheckPatientTagValuesAsync(IEnumerable<BaseTag> tags, Action<float, float, LoadingText> updateProgress)
        {
            var tasks = m_Patients.Select(patient => (Func<UniTask>)(async () =>
            {
                await UniTask.SwitchToThreadPool();
                patient.Tags.RemoveAll(t => t.Tag == null || !PersistentDataManager.Tags.AllTags.Contains(t.Tag));
                foreach (var site in patient.Sites) site.Tags.RemoveAll(t => t.Tag == null || !PersistentDataManager.Tags.AllTags.Contains(t.Tag));
                List<BaseTagValue> tagsToUpdate = patient.Tags.Where(t => tags.Contains(t.Tag)).ToList();
                tagsToUpdate.AddRange(patient.Sites.SelectMany(s => s.Tags).Where(t => tags.Contains(t.Tag)));
                foreach (var tagValue in tagsToUpdate)
                {
                    if (tagValue.Tag is IntTag && tagValue is not IntTagValue)
                    {
                        patient.Tags.Remove(tagValue);
                        var newTagValue = new IntTagValue();
                        newTagValue.Copy(tagValue);
                        patient.Tags.Add(newTagValue);
                        newTagValue.UpdateValue();
                    }
                    else if (tagValue.Tag is FloatTag && tagValue is not FloatTagValue)
                    {
                        patient.Tags.Remove(tagValue);
                        var newTagValue = new FloatTagValue();
                        newTagValue.Copy(tagValue);
                        patient.Tags.Add(newTagValue);
                        newTagValue.UpdateValue();
                    }
                    else if (tagValue.Tag is BoolTag && tagValue is not BoolTagValue)
                    {
                        patient.Tags.Remove(tagValue);
                        var newTagValue = new BoolTagValue();
                        newTagValue.Copy(tagValue);
                        patient.Tags.Add(newTagValue);
                        newTagValue.UpdateValue();
                    }
                    else if (tagValue.Tag is EmptyTag && tagValue is not EmptyTagValue)
                    {
                        patient.Tags.Remove(tagValue);
                        var newTagValue = new EmptyTagValue();
                        newTagValue.Copy(tagValue);
                        patient.Tags.Add(newTagValue);
                        newTagValue.UpdateValue();
                    }
                    else if (tagValue.Tag is EnumTag && tagValue is not EnumTagValue)
                    {
                        patient.Tags.Remove(tagValue);
                        var newTagValue = new EnumTagValue();
                        newTagValue.Copy(tagValue);
                        patient.Tags.Add(newTagValue);
                        newTagValue.UpdateValue();
                    }
                    else if (tagValue.Tag is StringTag && tagValue is not StringTagValue)
                    {
                        patient.Tags.Remove(tagValue);
                        var newTagValue = new StringTagValue();
                        newTagValue.Copy(tagValue);
                        patient.Tags.Add(newTagValue);
                        newTagValue.UpdateValue();
                    }
                    else
                    {
                        tagValue.UpdateValue();
                    }
                }
            }));
            await Tools.UniTaskExtensions.PerformMultipleTasksAsync(tasks, 0, 1, "Checking patients", updateProgress, 20, PersistentDataManager.UserPreferences.General.System.MultiThreading);
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
                Preferences = await ClassLoaderSaver.LoadFromJsonAsync<ProjectPreferences>(settingsFiles[0].FullName);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw new CanNotReadSettingsFileException(settingsFiles[0].Name);
            }
            updateProgress.Invoke(1.0f, 0, new LoadingText("Settings loaded successfully"));
        }
        private async UniTask LoadPatientsAsync(DirectoryInfo projectDirectory, Action<float, float, LoadingText> updateProgress)
        {
            const float LOADING_PROGRESS = 0.95f;
            const float CHECKING_PROGRESS = 0.05f;
            List<Patient> patients = new List<Patient>();
            DirectoryInfo patientDirectory = projectDirectory.GetDirectories("Patients", SearchOption.TopDirectoryOnly)[0];
            FileInfo[] patientFiles = patientDirectory.GetFiles("*" + Patient.EXTENSION, SearchOption.TopDirectoryOnly);
            var tasks = patientFiles.Select(file => (Func<UniTask<Patient>>)(async () =>
            {
                try
                {
                    return await ClassLoaderSaver.LoadFromJsonAsync<Patient>(file.FullName);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    throw new CanNotReadPatientFileException(Path.GetFileNameWithoutExtension(file.Name));
                }
            }));
            patients.AddRange(await Tools.UniTaskExtensions.PerformMultipleTasksAsync(tasks, 0, LOADING_PROGRESS, "Loading patients", updateProgress, 20, PersistentDataManager.UserPreferences.General.System.MultiThreading));
            SetPatients(patients.ToArray());
            await CheckPatientTagValuesAsync(PersistentDataManager.Tags.AllTags, (localProgress, duration, text) => updateProgress.Invoke(LOADING_PROGRESS + localProgress * CHECKING_PROGRESS, duration, text));
            updateProgress.Invoke(1.0f, 0, new LoadingText("Patients loaded successfully"));
        }
        private async UniTask LoadGroupsAsync(DirectoryInfo projectDirectory, Action<float, float, LoadingText> updateProgress)
        {
            List<Group> groups = new List<Group>();
            DirectoryInfo groupDirectory = projectDirectory.GetDirectories("Groups", SearchOption.TopDirectoryOnly)[0];
            FileInfo[] groupFiles = groupDirectory.GetFiles("*" + Group.EXTENSION, SearchOption.TopDirectoryOnly);
            var tasks = groupFiles.Select(file => (Func<UniTask<Group>>)(async () =>
            {
                try
                {
                    return await ClassLoaderSaver.LoadFromJsonAsync<Group>(file.FullName);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    throw new CanNotReadGroupFileException(Path.GetFileNameWithoutExtension(file.Name));
                }
            }));
            groups.AddRange(await Tools.UniTaskExtensions.PerformMultipleTasksAsync(tasks, 0, 1, "Loading groups", updateProgress, 20, PersistentDataManager.UserPreferences.General.System.MultiThreading));
            SetGroups(groups.ToArray());
            updateProgress.Invoke(1.0f, 0, new LoadingText("Groups loaded successfully"));
        }
        private async UniTask LoadDatasetsAsync(DirectoryInfo projectDirectory, Action<float, float, LoadingText> updateProgress)
        {
            List<Dataset> datasets = new List<Dataset>();
            DirectoryInfo datasetDirectory = projectDirectory.GetDirectories("Datasets", SearchOption.TopDirectoryOnly)[0];
            FileInfo[] datasetFiles = datasetDirectory.GetFiles("*" + Dataset.EXTENSION, SearchOption.TopDirectoryOnly);
            var tasks = datasetFiles.Select(file => (Func<UniTask<Dataset>>)(async () =>
            {
                try
                {
                    return await ClassLoaderSaver.LoadFromJsonAsync<Dataset>(file.FullName);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    throw new CanNotReadDatasetFileException(Path.GetFileNameWithoutExtension(file.Name));
                }
            }));
            datasets.AddRange(await Tools.UniTaskExtensions.PerformMultipleTasksAsync(tasks, 0, 1, "Loading datasets", updateProgress, 20, PersistentDataManager.UserPreferences.General.System.MultiThreading));
            SetDatasets(datasets.ToArray());
            updateProgress.Invoke(1.0f, 0, new LoadingText("Datasets loaded successfully"));
        }
        private async UniTask LoadVisualizationsAsync(DirectoryInfo projectDirectory, Action<float, float, LoadingText> updateProgress)
        {
            DirectoryInfo visualizationsDirectory = projectDirectory.GetDirectories("Visualizations", SearchOption.TopDirectoryOnly)[0];
            List<Visualization> visualizations = new List<Visualization>();
            FileInfo[] visualizationFiles = visualizationsDirectory.GetFiles("*" + Visualization.EXTENSION, SearchOption.TopDirectoryOnly);
            var tasks = visualizationFiles.Select(file => (Func<UniTask<Visualization>>)(async () =>
            {
                try
                {
                    return await ClassLoaderSaver.LoadFromJsonAsync<Visualization>(file.FullName);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    throw new CanNotReadVisualizationFileException(Path.GetFileNameWithoutExtension(file.Name));
                }
            }));
            visualizations.AddRange(await Tools.UniTaskExtensions.PerformMultipleTasksAsync(tasks, 0, 1, "Loading visualizations", updateProgress, 20, PersistentDataManager.UserPreferences.General.System.MultiThreading));
            SetVisualizations(visualizations.ToArray());
            updateProgress.Invoke(1.0f, 0, new LoadingText("Visualizations loaded successfully"));
        }

        private async UniTask SaveSettingsAsync(DirectoryInfo projectDirectory, Action<float, float, LoadingText> updateProgress)
        {
            updateProgress.Invoke(0, 0, new LoadingText("Saving settings"));
            try
            {
                await ClassLoaderSaver.SaveToJSonAsync(Preferences, Path.Combine(projectDirectory.FullName, Preferences.Name + ProjectPreferences.EXTENSION));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw new CanNotSaveSettingsException();
            }
            updateProgress.Invoke(1.0f, 0, new LoadingText("Settings saved successfully"));
        }
        private async UniTask SavePatientsAsync(DirectoryInfo projectDirectory, Action<float, float, LoadingText> updateProgress)
        {
            DirectoryInfo patientDirectory = Directory.CreateDirectory(Path.Combine(projectDirectory.FullName, "Patients"));
            var tasks = m_Patients.Select(patient => (Func<UniTask>)(async () =>
            {
                try
                {
                    await ClassLoaderSaver.SaveToJSonAsync(patient, Path.Combine(patientDirectory.FullName, patient.ID + Patient.EXTENSION));
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    throw new CanNotSaveSettingsException();
                }
            }));
            await Tools.UniTaskExtensions.PerformMultipleTasksAsync(tasks, 0, 1, "Saving patients", updateProgress, 20, PersistentDataManager.UserPreferences.General.System.MultiThreading);
            updateProgress.Invoke(1.0f, 0, new LoadingText("Patients saved successfully"));
        }
        private async UniTask SaveGroupsAsync(DirectoryInfo projectDirectory, Action<float, float, LoadingText> updateProgress)
        {
            DirectoryInfo groupDirectory = Directory.CreateDirectory(Path.Combine(projectDirectory.FullName, "Groups"));
            var tasks = m_Groups.Select(group => (Func<UniTask>)(async () =>
            {
                try
                {
                    await ClassLoaderSaver.SaveToJSonAsync(group, Path.Combine(groupDirectory.FullName, group.Name + Group.EXTENSION));
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    throw new CanNotSaveSettingsException();
                }
            }));
            await Tools.UniTaskExtensions.PerformMultipleTasksAsync(tasks, 0, 1, "Saving groups", updateProgress, 20, PersistentDataManager.UserPreferences.General.System.MultiThreading);
            updateProgress.Invoke(1.0f, 0, new LoadingText("Groups saved successfully"));
        }
        private async UniTask SaveDatasetsAsync(DirectoryInfo projectDirectory, Action<float, float, LoadingText> updateProgress)
        {
            DirectoryInfo datasetDirectory = Directory.CreateDirectory(Path.Combine(projectDirectory.FullName, "Datasets"));
            var tasks = m_Datasets.Select(dataset => (Func<UniTask>)(async () =>
            {
                try
                {
                    await ClassLoaderSaver.SaveToJSonAsync(dataset, Path.Combine(datasetDirectory.FullName, dataset.Name + Dataset.EXTENSION));
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    throw new CanNotSaveSettingsException();
                }
            }));
            await Tools.UniTaskExtensions.PerformMultipleTasksAsync(tasks, 0, 1, "Saving datasets", updateProgress, 20, PersistentDataManager.UserPreferences.General.System.MultiThreading);
            updateProgress.Invoke(1.0f, 0, new LoadingText("Datasets saved successfully"));
        }
        private async UniTask SaveVisualizationsAsync(DirectoryInfo projectDirectory, Action<float, float, LoadingText> updateProgress)
        {
            DirectoryInfo visualizationDirectory = Directory.CreateDirectory(Path.Combine(projectDirectory.FullName, "Visualizations"));
            var tasks = m_Visualizations.Select(visualization => (Func<UniTask>)(async () =>
            {
                try
                {
                    await ClassLoaderSaver.SaveToJSonAsync(visualization, Path.Combine(visualizationDirectory.FullName, visualization.Name + Visualization.EXTENSION));
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    throw new CanNotSaveSettingsException();
                }
            }));
            await Tools.UniTaskExtensions.PerformMultipleTasksAsync(tasks, 0, 1, "Saving visualizations", updateProgress, 20, PersistentDataManager.UserPreferences.General.System.MultiThreading);
            updateProgress.Invoke(1.0f, 0, new LoadingText("Visualizations saved successfully"));
        }

        private void CopyIcons(string oldIconsDirectoryPath, string newIconsDirectoryPath)
        {
            new DirectoryInfo(oldIconsDirectoryPath).CopyFilesRecursively(new DirectoryInfo(newIconsDirectoryPath));
        }
        private async UniTask EmbedDataIntoProjectFileAsync(DirectoryInfo projectDirectory, string oldProjectDirectory, Action<float, float, LoadingText> updateProgress)
        {
            await UniTask.SwitchToThreadPool();
            DirectoryInfo dataDirectory = Directory.CreateDirectory(Path.Combine(projectDirectory.FullName, "Data"));

            float progress = 0.0f;
            float progressStep = 1.0f / (Patients.Count + Datasets.Count);

            updateProgress.Invoke(progress, 0, new LoadingText("Copying data"));

            // Save Patient Data
            if (Patients.Count > 0)
            {
                DirectoryInfo patientsDirectory = Directory.CreateDirectory(Path.Combine(dataDirectory.FullName, "Anatomy"));
                foreach (var patient in Patients)
                {
                    progress += progressStep;
                    updateProgress.Invoke(progress, 0, new LoadingText("Copying ", patient.Name, " anatomical data"));

                    DirectoryInfo patientDirectory = Directory.CreateDirectory(Path.Combine(patientsDirectory.FullName, patient.ID));
                    if (patient.Meshes.Count > 0)
                    {
                        DirectoryInfo meshesDirectory = Directory.CreateDirectory(Path.Combine(patientDirectory.FullName, "Meshes"));
                        foreach (var mesh in patient.Meshes)
                        {
                            if (mesh is SingleMesh)
                            {
                                SingleMesh singleMesh = mesh as SingleMesh;
                                singleMesh.Path = singleMesh.Path.CopyToDirectory(meshesDirectory).Replace(projectDirectory.FullName, oldProjectDirectory);
                                singleMesh.MarsAtlasPath = singleMesh.MarsAtlasPath.CopyToDirectory(meshesDirectory).Replace(projectDirectory.FullName, oldProjectDirectory);
                            }
                            else if (mesh is LeftRightMesh)
                            {
                                LeftRightMesh singleMesh = mesh as LeftRightMesh;
                                singleMesh.LeftHemisphere = singleMesh.LeftHemisphere.CopyToDirectory(meshesDirectory).Replace(projectDirectory.FullName, oldProjectDirectory);
                                singleMesh.RightHemisphere = singleMesh.RightHemisphere.CopyToDirectory(meshesDirectory).Replace(projectDirectory.FullName, oldProjectDirectory);
                                singleMesh.LeftMarsAtlasHemisphere = singleMesh.LeftMarsAtlasHemisphere.CopyToDirectory(meshesDirectory).Replace(projectDirectory.FullName, oldProjectDirectory);
                                singleMesh.RightMarsAtlasHemisphere = singleMesh.RightMarsAtlasHemisphere.CopyToDirectory(meshesDirectory).Replace(projectDirectory.FullName, oldProjectDirectory);
                            }
                            mesh.Transformation = mesh.Transformation.CopyToDirectory(meshesDirectory).Replace(projectDirectory.FullName, oldProjectDirectory);
                        }
                    }
                    if (patient.MRIs.Count > 0)
                    {
                        DirectoryInfo mriDirectory = Directory.CreateDirectory(Path.Combine(patientDirectory.FullName, "MRIs"));
                        foreach (var mri in patient.MRIs)
                        {
                            mri.File = mri.File.CopyToDirectory(mriDirectory).Replace(projectDirectory.FullName, oldProjectDirectory);
                        }
                    }
                }
            }
            // Save Localizer Data
            if (Datasets.Count > 0)
            {
                DirectoryInfo localizersDirectory = Directory.CreateDirectory(Path.Combine(dataDirectory.FullName, "Functional"));
                foreach (var dataset in Datasets)
                {
                    if (dataset.Data.Count > 0)
                    {
                        progress += progressStep;
                        updateProgress.Invoke(progress, 0, new LoadingText("Copying ", dataset.Name));

                        DirectoryInfo datasetDirectory = Directory.CreateDirectory(Path.Combine(localizersDirectory.FullName, dataset.Name));
                        foreach (var data in dataset.Data)
                        {
                            DirectoryInfo dataInfoDirectory = new DirectoryInfo(Path.Combine(datasetDirectory.FullName, data.Name));
                            if (!dataInfoDirectory.Exists) dataInfoDirectory = Directory.CreateDirectory(dataInfoDirectory.FullName);
                            data.DataContainer.CopyDataToDirectory(dataInfoDirectory, projectDirectory.FullName, oldProjectDirectory);
                        }
                    }
                }
            }
        }
        #endregion
    }
}
