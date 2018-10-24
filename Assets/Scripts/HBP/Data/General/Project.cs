using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using HBP.Data.Preferences;
using HBP.Data.Visualization;
using HBP.Data.Experience.Dataset;
using HBP.Data.Experience.Protocol;
using CielaSpike;
using Tools.Unity;
using UnityEngine.Events;
using Ionic.Zip;

namespace HBP.Data.General
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
                return Settings.Name + EXTENSION;
            }
        }

        ProjectSettings m_Settings = new ProjectSettings();
        /// <summary>
        /// Settings of the project.
        /// </summary>
        public ProjectSettings Settings
        {
            get { return m_Settings; }
            set { m_Settings = value; }
        }

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

        List<Protocol> m_Protocols = new List<Protocol>();
        /// <summary>
        /// Protocols of the project.
        /// </summary>
        public ReadOnlyCollection<Protocol> Protocols
        {
            get { return new ReadOnlyCollection<Protocol>(m_Protocols); }
        }

        List<Dataset> m_Datasets = new List<Dataset>();
        /// <summary>
        /// Datasets of the project.
        /// </summary>
        public ReadOnlyCollection<Dataset> Datasets
        {
            get { return new ReadOnlyCollection<Dataset>(m_Datasets); }
        }

        List<Visualization.Visualization> m_Visualizations = new List<Visualization.Visualization>();
        /// <summary>
        /// Visualizations of the project.
        /// </summary>
        public ReadOnlyCollection<Visualization.Visualization> Visualizations
        {
            get { return new ReadOnlyCollection<Visualization.Visualization>(m_Visualizations); }
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
        public Project(ProjectSettings settings, IEnumerable<Patient> patients, IEnumerable<Group> groups, IEnumerable<Protocol> protocols, IEnumerable<Dataset> datasets, IEnumerable<Visualization.Visualization> visualizations)
        {
            Settings = settings;
            SetPatients(patients);
            SetGroups(groups);
            SetProtocols(protocols);
            SetDatasets(datasets);
            SetVisualizations(visualizations);
        }
        /// <summary>
        /// Create a new project with only the settings.
        /// </summary>
        /// <param name="settings">Settings of the project.</param>
        public Project(ProjectSettings settings) : this(settings, new Patient[0], new Group[0], new Protocol[0], new Dataset[0] , new Visualization.Visualization[0])
        {
        }
        /// <summary>
        /// Create a empty project with a name.
        /// </summary>
        /// <param name="name">Name of the project.</param>
        public Project(string name) : this(new ProjectSettings(name))
        {
        }
        /// <summary>
        /// Create a empty project with default values.
        /// </summary>
        public Project() : this(new ProjectSettings())
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
            this.m_Patients = new List<Patient>();
            AddPatient(patients);
            foreach (Dataset dataset in m_Datasets)
            {
                dataset.RemoveData(from data in dataset.Data where !m_Patients.Any(p => p == data.Patient) select data);
            }
            foreach (Visualization.Visualization visualization in m_Visualizations)
            {
                visualization.RemovePatient(from patient in visualization.Patients where !m_Patients.Contains(patient) select patient);
            }
            foreach (Group _group in m_Groups)
            {
                _group.RemovePatient((from patient in _group.Patients where !m_Patients.Contains(patient) select patient).ToArray());
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
            foreach(Group group in m_Groups)
            {
                group.RemovePatient(patient);
            }
            foreach (Dataset dataset in m_Datasets)
            {
                dataset.RemoveData(from data in dataset.Data where data.Patient == patient select data);
            }
            foreach(Visualization.Visualization visualization in m_Visualizations)
            {
                visualization.RemovePatient(patient);
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
        // Protocols.
        public void SetProtocols(IEnumerable<Protocol> protocols)
        {
            m_Protocols = new List<Protocol>();
            AddProtocol(protocols);
            RemoveDataset((from dataset in m_Datasets where !m_Protocols.Any(p => p == dataset.Protocol) select dataset).ToArray());
            foreach (Visualization.Visualization visualization in m_Visualizations)
            {
                IEEGColumn[] columnsToRemove = visualization.Columns.Where(c => c is IEEGColumn).Select(c => c as IEEGColumn).Where(c => !m_Protocols.Any(p => p == c.Dataset.Protocol)).ToArray();
                foreach (BaseColumn column in columnsToRemove)
                {
                    visualization.Columns.Remove(column);
                }
            }
        }
        public void AddProtocol(Protocol protocol)
        {
            m_Protocols.Add(protocol);
        }
        public void AddProtocol(IEnumerable<Protocol> protocols)
        {
            foreach (Protocol protocol in protocols)
            {
                AddProtocol(protocol);
            }
        }
        public void RemoveProtocol(Protocol protocol)
        {
            m_Datasets.RemoveAll((d) => d.Protocol == protocol);
            foreach (Visualization.Visualization visualization in m_Visualizations)
            {
                visualization.Columns.RemoveAll((column) => (column is IEEGColumn) && (column as IEEGColumn).Dataset.Protocol == protocol);
            }
            m_Protocols.Remove(protocol);
        }
        public void RemoveProtocol(IEnumerable<Protocol> protocols)
        {
            foreach (Protocol protocol in protocols)
            {
                RemoveProtocol(protocol);
            }
        }
        // Datasets.
        public void SetDatasets(IEnumerable<Dataset> datasets)
        {
            m_Datasets = new List<Dataset>();
            AddDataset(datasets);
            foreach (Visualization.Visualization visualization in m_Visualizations)
            {
                BaseColumn[] columnsToRemove = visualization.Columns.Where(column => column is IEEGColumn && !m_Datasets.Any(d => d == (column as IEEGColumn).Dataset)).ToArray();
                foreach (BaseColumn column in columnsToRemove)
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
            foreach (Visualization.Visualization visualization in m_Visualizations)
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
        public void SetVisualizations(IEnumerable<Visualization.Visualization> visualizations)
        {
            this.m_Visualizations = new List<Visualization.Visualization>();
            AddVisualization(visualizations);
        }
        public void AddVisualization(Visualization.Visualization visualization)
        {
            m_Visualizations.Add(visualization);
        }
        public void AddVisualization(IEnumerable<Visualization.Visualization> visualizations)
        {
            foreach(Visualization.Visualization visualization in visualizations)
            {
                AddVisualization(visualization);
            }
        }
        public void RemoveVisualization(Visualization.Visualization visualization)
        {
            m_Visualizations.Remove(visualization);
        }
        public void RemoveVisualization(IEnumerable<Visualization.Visualization> visualizations)
        {
            foreach (Visualization.Visualization visualization in visualizations)
            {
                RemoveVisualization(visualization);
            }
        }
        #endregion

        #region Public Methods
        public static bool IsProject(string path)
        {
            return new FileInfo(path).Extension == EXTENSION;
        }
        public static IEnumerable<string> GetProject(string path)
        {
            if(!string.IsNullOrEmpty(path))
            {
                DirectoryInfo directory = new DirectoryInfo(path);
                if(directory.Exists)
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
        public IEnumerator c_Load(ProjectInfo projectInfo, GenericEvent<float,float, LoadingText> OnChangeProgress = null)
        {
            // Initialize progress.
            float progress = 0.0f;
            float progressStep = 1.0f / ( 1 + projectInfo.Patients + projectInfo.Groups + projectInfo.Protocols + projectInfo.Datasets + projectInfo.Visualizations);
            if (OnChangeProgress == null) OnChangeProgress = new GenericEvent<float, float, LoadingText>();
            Action<float> outPut = (value) => progress = value;

            yield return Ninja.JumpToUnity;
            OnChangeProgress.Invoke(progress, 0, new LoadingText("Loading project"));
            yield return Ninja.JumpBack;

            // Unzipping
            if (Directory.Exists(ApplicationState.ProjectLoadedTMPFullPath)) Directory.Delete(ApplicationState.ProjectLoadedTMPFullPath, true);
            using (ZipFile zip = ZipFile.Read(projectInfo.Path))
            {
                zip.ExtractAll(ApplicationState.ProjectLoadedTMPFullPath, ExtractExistingFileAction.OverwriteSilently);
            }

            if (!File.Exists(projectInfo.Path)) throw new FileNotFoundException(projectInfo.Path); // Test if the file exists.
            if (!IsProject(projectInfo.Path)) throw new FileNotFoundException(projectInfo.Path); // Test if the file is a project.
            DirectoryInfo projectDirectory = new DirectoryInfo(ApplicationState.ProjectLoadedTMPFullPath);

            yield return Ninja.JumpToUnity;
            yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_LoadSettings(projectDirectory, progress, progressStep, OnChangeProgress, outPut));
            yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_LoadPatients(projectDirectory, progress, progressStep, OnChangeProgress, outPut));
            yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_LoadGroups(projectDirectory, progress, progressStep, OnChangeProgress, outPut));
            yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_LoadProtocols(projectDirectory, progress, progressStep, OnChangeProgress, outPut));
            yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_LoadDatasets(projectDirectory, progress, progressStep, OnChangeProgress, outPut));
            yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_LoadVisualizations(projectDirectory, progress, progressStep, OnChangeProgress, outPut));
            yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_CheckDatasets(OnChangeProgress));
            
            yield return Ninja.JumpToUnity;
            OnChangeProgress.Invoke(2, 0, new LoadingText("Project succesfully loaded."));
        }
        public IEnumerator c_Save(string path, GenericEvent<float, float, LoadingText> OnChangeProgress = null)
        {
            float progress = 0.0f;
            float progressStep = 1.0f / ( 4 + Patients.Count + Groups.Count + Protocols.Count + Datasets.Count + Visualizations.Count);
            if (OnChangeProgress == null) OnChangeProgress = new GenericEvent<float, float, LoadingText>();
            Action<float> outPut = (value) => progress = value;

            yield return Ninja.JumpToUnity;
            OnChangeProgress.Invoke(progress, 0, new LoadingText("Saving project."));
            yield return Ninja.JumpBack;

            if (String.IsNullOrEmpty(path)) throw new ArgumentNullException(path);
            if (!Directory.Exists(path)) throw new DirectoryNotFoundException(path);
            DirectoryInfo tmpProjectDirectory = Directory.CreateDirectory(ApplicationState.ProjectLoadedTMPFullPath + "-temp");
            string oldTMPProjectDirectory = ApplicationState.ProjectLoadedTMPFullPath;
            progress += progressStep;

            yield return Ninja.JumpToUnity;
            OnChangeProgress.Invoke(progress, 0, new LoadingText("Saving project."));
            yield return Ninja.JumpBack;

            //yield return c_EmbedDataIntoProjectFile(tmpProjectDirectory, oldTMPProjectDirectory, OnChangeProgress);
            yield return c_SaveSettings(tmpProjectDirectory, progress, progressStep, OnChangeProgress, outPut);
            yield return c_SavePatients(tmpProjectDirectory, progress, progressStep, OnChangeProgress, outPut);
            yield return c_SaveGroups(tmpProjectDirectory, progress, progressStep, OnChangeProgress, outPut);
            yield return c_SaveProtocols(tmpProjectDirectory, progress, progressStep, OnChangeProgress, outPut);
            yield return c_SaveDatasets(tmpProjectDirectory, progress, progressStep, OnChangeProgress, outPut);
            yield return c_SaveVisualizations(tmpProjectDirectory, progress, progressStep, OnChangeProgress, outPut);
            CopyIcons(oldTMPProjectDirectory + Path.DirectorySeparatorChar + "Protocols" + Path.DirectorySeparatorChar + "Icons", tmpProjectDirectory.FullName + Path.DirectorySeparatorChar + "Protocols" + Path.DirectorySeparatorChar + "Icons");

            // Deleting old directories.
            yield return Ninja.JumpToUnity;
            OnChangeProgress.Invoke(progress, 0, new LoadingText("Finalizing."));
            yield return Ninja.JumpBack;

            if (Directory.Exists(oldTMPProjectDirectory))
            {
                try
                {
                    Directory.Delete(oldTMPProjectDirectory,true);
                }
                catch
                {
                    throw new CanNotDeleteOldProjectDirectory(oldTMPProjectDirectory);
                }
            }
            progress += progressStep;

            try
            {
                tmpProjectDirectory.MoveTo(oldTMPProjectDirectory);
            }
            catch
            {
                throw new CanNotRenameProjectDirectory();
            }
            progress += progressStep;

            // Zipping
            string filePath = path + Path.DirectorySeparatorChar + FileName;
            if (File.Exists(filePath)) File.Delete(filePath);
            using (ZipFile zip = new ZipFile(filePath))
            {
                zip.AddDirectory(oldTMPProjectDirectory);
                zip.Save();
            }

            yield return Ninja.JumpToUnity;
            OnChangeProgress.Invoke(1, 0, new LoadingText("Project succesfully saved."));
        }
        #endregion

        #region Private Methods
        IEnumerator c_LoadSettings(DirectoryInfo projectDirectory, float progress, float progressStep, GenericEvent<float, float, LoadingText> OnChangeProgress, Action<float> outPut)
        {
            yield return Ninja.JumpBack;
            // Loading settings file.
            OnChangeProgress.Invoke(progress, 0, new LoadingText("Loading settings"));

            FileInfo[] settingsFiles = projectDirectory.GetFiles("*" + ProjectSettings.EXTENSION, SearchOption.TopDirectoryOnly);
            if (settingsFiles.Length == 0) throw new SettingsFileNotFoundException(); // Test if settings files found.
            else if (settingsFiles.Length > 1) throw new MultipleSettingsFilesFoundException(); // Test if multiple settings files found.
            try
            {
                Settings = ClassLoaderSaver.LoadFromJson<ProjectSettings>(settingsFiles[0].FullName);
            }
            catch
            {
                throw new CanNotReadSettingsFileException(settingsFiles[0].Name);
            }
            progress += progressStep;
            outPut(progress);
        }
        IEnumerator c_LoadPatients(DirectoryInfo projectDirectory, float progress, float progressStep, GenericEvent<float, float, LoadingText> OnChangeProgress, Action<float> outPut)
        {
            yield return Ninja.JumpBack;
            // Load patients.
            List<Patient> patients = new List<Patient>();
            DirectoryInfo patientDirectory = projectDirectory.GetDirectories("Patients", SearchOption.TopDirectoryOnly)[0];
            FileInfo[] patientFiles = patientDirectory.GetFiles("*" + Patient.EXTENSION, SearchOption.TopDirectoryOnly);
            for (int i = 0; i < patientFiles.Length; ++i)
            {
                FileInfo patientFile = patientFiles[i];
                OnChangeProgress.Invoke(progress, 0, new LoadingText("Loading patient ", Path.GetFileNameWithoutExtension(patientFile.Name), " [" + (i + 1).ToString() + "/" + patientFiles.Length + "]"));
                try
                {
                    patients.Add(ClassLoaderSaver.LoadFromJson<Patient>(patientFile.FullName));
                }
                catch
                {
                    throw new CanNotReadPatientFileException(Path.GetFileNameWithoutExtension(patientFile.Name));
                }
                progress += progressStep;
            }
            SetPatients(patients.ToArray());
            outPut(progress);
        }
        IEnumerator c_LoadGroups(DirectoryInfo projectDirectory, float progress, float progressStep, GenericEvent<float, float, LoadingText> OnChangeProgress, Action<float> outPut)
        {
            yield return Ninja.JumpBack;
            // Load groups.
            List<Group> groups = new List<Group>();
            DirectoryInfo groupDirectory = projectDirectory.GetDirectories("Groups", SearchOption.TopDirectoryOnly)[0];
            FileInfo[] groupFiles = groupDirectory.GetFiles("*" + Group.EXTENSION, SearchOption.TopDirectoryOnly);
            for (int i = 0; i < groupFiles.Length; ++i)
            {
                FileInfo groupFile = groupFiles[i];
                OnChangeProgress.Invoke(progress, 0, new LoadingText("Loading group ", Path.GetFileNameWithoutExtension(groupFile.Name), " [" + (i + 1).ToString() + "/" + groupFiles.Length + "]"));
                try
                {
                    groups.Add(ClassLoaderSaver.LoadFromJson<Group>(groupFile.FullName));
                }
                catch
                {
                    throw new CanNotReadGroupFileException(Path.GetFileNameWithoutExtension(groupFile.Name));
                }
                progress += progressStep;
            }
            SetGroups(groups.ToArray());
            outPut(progress);
        }
        IEnumerator c_LoadProtocols(DirectoryInfo projectDirectory, float progress, float progressStep, GenericEvent<float, float, LoadingText> OnChangeProgress, Action<float> outPut)
        {
            yield return Ninja.JumpBack;
            //Load Protocols
            List<Protocol> protocols = new List<Protocol>();
            DirectoryInfo protocolDirectory = projectDirectory.GetDirectories("Protocols", SearchOption.TopDirectoryOnly)[0];
            FileInfo[] protocolFiles = protocolDirectory.GetFiles("*" + Protocol.EXTENSION, SearchOption.TopDirectoryOnly);
            for (int i = 0; i < protocolFiles.Length; ++i)
            {
                FileInfo protocolFile = protocolFiles[i];
                OnChangeProgress.Invoke(progress, 0, new LoadingText("Loading protocol ", Path.GetFileNameWithoutExtension(protocolFile.Name), " [" + (i + 1).ToString() + "/" + protocolFiles.Length + "]"));
                try
                {
                    protocols.Add(ClassLoaderSaver.LoadFromJson<Protocol>(protocolFile.FullName));
                }
                catch
                {
                    throw new CanNotReadProtocolFileException(Path.GetFileNameWithoutExtension(protocolFile.Name));
                }
                progress += progressStep;
            }
            SetProtocols(protocols.ToArray());
            outPut(progress);
        }
        IEnumerator c_LoadDatasets(DirectoryInfo projectDirectory, float progress, float progressStep, GenericEvent<float, float, LoadingText> OnChangeProgress, Action<float> outPut)
        {
            yield return Ninja.JumpBack;
            //Load Datasets
            List<Dataset> datasets = new List<Dataset>();
            DirectoryInfo datasetDirectory = projectDirectory.GetDirectories("Datasets", SearchOption.TopDirectoryOnly)[0];
            FileInfo[] datasetFiles = datasetDirectory.GetFiles("*" + Dataset.EXTENSION, SearchOption.TopDirectoryOnly);
            for (int i = 0; i < datasetFiles.Length; ++i)
            {
                FileInfo datasetFile = datasetFiles[i];
                OnChangeProgress.Invoke(progress, 0, new LoadingText("Loading dataset ", Path.GetFileNameWithoutExtension(datasetFile.Name), " [" + (i + 1).ToString() + "/" + datasetFiles.Length + "]"));
                try
                {
                    datasets.Add(ClassLoaderSaver.LoadFromJson<Dataset>(datasetFile.FullName));
                }
                catch
                {
                    throw new CanNotReadDatasetFileException(Path.GetFileNameWithoutExtension(datasetFile.Name));
                }
                progress += progressStep;
            }
            SetDatasets(datasets.ToArray());
            outPut(progress);
        }
        IEnumerator c_LoadVisualizations(DirectoryInfo projectDirectory, float progress, float progressStep, GenericEvent<float, float, LoadingText> OnChangeProgress, Action<float> outPut)
        {
            yield return Ninja.JumpBack;
            //Load Visualizations
            DirectoryInfo visualizationsDirectory = projectDirectory.GetDirectories("Visualizations", SearchOption.TopDirectoryOnly)[0];

            List<Visualization.Visualization> visualizations = new List<Visualization.Visualization>();
            FileInfo[] visualizationFiles = visualizationsDirectory.GetFiles("*" + Visualization.Visualization.EXTENSION, SearchOption.TopDirectoryOnly);
            for (int i = 0; i < visualizationFiles.Length; ++i)
            {
                FileInfo visualizationFile = visualizationFiles[i];
                OnChangeProgress.Invoke(progress, 0, new LoadingText("Loading visualization ", Path.GetFileNameWithoutExtension(visualizationFile.Name), " [" + (i + 1).ToString() + "/" + visualizationFiles.Length + "]"));
                try
                {
                    visualizations.Add(ClassLoaderSaver.LoadFromJson<Visualization.Visualization>(visualizationFile.FullName));
                }
                catch
                {
                    throw new CanNotReadVisualizationFileException(Path.GetFileNameWithoutExtension(visualizationFile.Name));
                }
                progress += progressStep;
            }
            SetVisualizations(visualizations.ToArray());
            outPut(progress);
        }
        IEnumerator c_CheckDatasets(GenericEvent<float, float, LoadingText> OnChangeProgress)
        {
            yield return Ninja.JumpBack;
            int count = 1;
            int length = m_Datasets.SelectMany(d => d.Data).Count();
            float progress = 1.0f;
            float progressStep = 1.0f / length;
            for (int i = 0; i < m_Datasets.Count; ++i)
            {
                Dataset dataset = m_Datasets[i];
                for (int j = 0; j < dataset.Data.Length; ++j, ++count)
                {
                    DataInfo data = dataset.Data[j];
                    data.GetErrors(dataset.Protocol);
                    progress += progressStep;

                    // DEBUG
                    yield return Ninja.JumpToUnity;
                    OnChangeProgress.Invoke(progress, 0, new LoadingText("Checking ", data.Name + " | " + dataset.Protocol.Name + " | " + data.Patient.Name, " [" + count + "/" + length + "]"));
                    yield return Ninja.JumpBack;
                }
            }
        }
        IEnumerator c_SaveSettings(DirectoryInfo projectDirectory, float progress, float progressStep, GenericEvent<float, float, LoadingText> OnChangeProgress, Action<float> outPut)
        {
            // Save settings
            yield return Ninja.JumpToUnity;
            OnChangeProgress.Invoke(progress, 0, new LoadingText("Saving settings"));
            yield return Ninja.JumpBack;

            try
            {
                ClassLoaderSaver.SaveToJSon(Settings, projectDirectory.FullName + Path.DirectorySeparatorChar + Settings.Name + ProjectSettings.EXTENSION);
            }
            catch
            {
                throw new CanNotSaveSettingsException();
            }
            progress += progressStep;
            outPut(progress);
        }
        IEnumerator c_SavePatients(DirectoryInfo projectDirectory, float progress, float progressStep, GenericEvent<float, float, LoadingText> OnChangeProgress, Action<float> outPut)
        {
            DirectoryInfo patientDirectory = Directory.CreateDirectory(projectDirectory.FullName + Path.DirectorySeparatorChar + "Patients");
            // Save patients
            foreach (Patient patient in Patients)
            {
                yield return Ninja.JumpToUnity;
                OnChangeProgress.Invoke(progress, 0, new LoadingText("Saving patient ", patient.ID));
                yield return Ninja.JumpBack;

                try
                {
                    ClassLoaderSaver.SaveToJSon(patient, patientDirectory.FullName + Path.DirectorySeparatorChar + patient.ID + Patient.EXTENSION);
                }
                catch
                {
                    throw new CanNotSaveSettingsException();
                }
                progress += progressStep;
            }
            outPut(progress);
        }
        IEnumerator c_SaveGroups(DirectoryInfo projectDirectory, float progress, float progressStep, GenericEvent<float, float, LoadingText> OnChangeProgress, Action<float> outPut)
        {
            // Save groups
            DirectoryInfo groupDirectory = Directory.CreateDirectory(projectDirectory.FullName + Path.DirectorySeparatorChar + "Groups");
            foreach (Group group in Groups)
            {
                yield return Ninja.JumpToUnity;
                OnChangeProgress.Invoke(progress, 0, new LoadingText("Saving group ", group.Name));
                yield return Ninja.JumpBack;

                try
                {
                    ClassLoaderSaver.SaveToJSon(group, groupDirectory.FullName + Path.DirectorySeparatorChar + group.Name + Group.EXTENSION);
                }
                catch
                {
                    throw new CanNotSaveSettingsException();
                }
                progress += progressStep;
            }
            outPut(progress);
        }
        IEnumerator c_SaveProtocols(DirectoryInfo projectDirectory, float progress, float progressStep, GenericEvent<float, float, LoadingText> OnChangeProgress, Action<float> outPut)
        {
            // Save protocols
            DirectoryInfo protocolDirectory = Directory.CreateDirectory(projectDirectory.FullName + Path.DirectorySeparatorChar + "Protocols");
            foreach (Protocol protocol in Protocols)
            {
                yield return Ninja.JumpToUnity;
                OnChangeProgress.Invoke(progress, 0, new LoadingText("Saving protocol ", protocol.Name));
                yield return Ninja.JumpBack;

                try
                {
                    ClassLoaderSaver.SaveToJSon(protocol, protocolDirectory.FullName + Path.DirectorySeparatorChar + protocol.Name + Protocol.EXTENSION);
                }
                catch
                {
                    throw new CanNotSaveSettingsException();
                }
                progress += progressStep;
            }
            outPut(progress);
        }
        IEnumerator c_SaveDatasets(DirectoryInfo projectDirectory, float progress, float progressStep, GenericEvent<float, float, LoadingText> OnChangeProgress, Action<float> outPut)
        {
            //Save datasets
            DirectoryInfo datasetDirectory = Directory.CreateDirectory(projectDirectory.FullName + Path.DirectorySeparatorChar + "Datasets");
            foreach (Dataset dataset in Datasets)
            {
                yield return Ninja.JumpToUnity;
                OnChangeProgress.Invoke(progress, 0, new LoadingText("Saving dataset ", dataset.Name));
                yield return Ninja.JumpBack;

                try
                {
                    ClassLoaderSaver.SaveToJSon(dataset, datasetDirectory.FullName + Path.DirectorySeparatorChar + dataset.Name + Dataset.EXTENSION);
                }
                catch
                {
                    throw new CanNotSaveSettingsException();
                }
                progress += progressStep;
            }
            outPut(progress);
        }
        IEnumerator c_SaveVisualizations(DirectoryInfo projectDirectory, float progress, float progressStep, GenericEvent<float, float, LoadingText> OnChangeProgress, Action<float> outPut)
        {
            DirectoryInfo visualizationDirectory = Directory.CreateDirectory(projectDirectory.FullName + Path.DirectorySeparatorChar + "Visualizations");

            //Save singleVisualizations
            foreach (Visualization.Visualization visualization in Visualizations)
            {
                yield return Ninja.JumpToUnity;
                OnChangeProgress.Invoke(progress, 0, new LoadingText("Saving visualization ", visualization.Name));
                yield return Ninja.JumpBack;

                try
                {
                    ClassLoaderSaver.SaveToJSon(visualization, visualizationDirectory.FullName + Path.DirectorySeparatorChar + visualization.Name + Visualization.Visualization.EXTENSION);
                }
                catch
                {
                    throw new CanNotSaveSettingsException();
                }
                progress += progressStep;
            }
            outPut(progress);
        }
        void CopyIcons(string oldIconsDirectoryPath, string newIconsDirectoryPath)
        {
            new DirectoryInfo(oldIconsDirectoryPath).CopyFilesRecursively(new DirectoryInfo(newIconsDirectoryPath));
        }
        IEnumerator c_EmbedDataIntoProjectFile(DirectoryInfo projectDirectory, string oldProjectDirectory, GenericEvent<float, float, LoadingText> OnChangeProgress)
        {
            DirectoryInfo dataDirectory = Directory.CreateDirectory(Path.Combine(projectDirectory.FullName, "Data"));

            float progress = 0.0f;
            float progressStep = 1.0f / (Patients.Count + Datasets.Count);

            yield return Ninja.JumpToUnity;
            OnChangeProgress.Invoke(progress, 0, new LoadingText("Copying data"));
            yield return Ninja.JumpBack;

            // Save Patient Data
            if (Patients.Count > 0)
            {
                DirectoryInfo patientsDirectory = Directory.CreateDirectory(Path.Combine(dataDirectory.FullName, "Anatomy"));
                foreach (var patient in Patients)
                {
                    yield return Ninja.JumpToUnity;
                    progress += progressStep;
                    OnChangeProgress.Invoke(progress, 0, new LoadingText("Copying ", patient.Name, " anatomical data"));
                    yield return Ninja.JumpBack;

                    DirectoryInfo patientDirectory = Directory.CreateDirectory(Path.Combine(patientsDirectory.FullName, patient.ID));
                    if (patient.Brain.Meshes.Count > 0)
                    {
                        DirectoryInfo meshesDirectory = Directory.CreateDirectory(Path.Combine(patientDirectory.FullName, "Meshes"));
                        foreach (var mesh in patient.Brain.Meshes)
                        {
                            if (mesh is Anatomy.SingleMesh)
                            {
                                Anatomy.SingleMesh singleMesh = mesh as Anatomy.SingleMesh;
                                singleMesh.Path = singleMesh.Path.CopyToDirectory(meshesDirectory).Replace(projectDirectory.FullName, oldProjectDirectory);
                                singleMesh.MarsAtlasPath = singleMesh.MarsAtlasPath.CopyToDirectory(meshesDirectory).Replace(projectDirectory.FullName, oldProjectDirectory);
                            }
                            else if (mesh is Anatomy.LeftRightMesh)
                            {
                                Anatomy.LeftRightMesh singleMesh = mesh as Anatomy.LeftRightMesh;
                                singleMesh.LeftHemisphere = singleMesh.LeftHemisphere.CopyToDirectory(meshesDirectory).Replace(projectDirectory.FullName, oldProjectDirectory);
                                singleMesh.RightHemisphere = singleMesh.RightHemisphere.CopyToDirectory(meshesDirectory).Replace(projectDirectory.FullName, oldProjectDirectory);
                                singleMesh.LeftMarsAtlasHemisphere = singleMesh.LeftMarsAtlasHemisphere.CopyToDirectory(meshesDirectory).Replace(projectDirectory.FullName, oldProjectDirectory);
                                singleMesh.RightMarsAtlasHemisphere = singleMesh.RightMarsAtlasHemisphere.CopyToDirectory(meshesDirectory).Replace(projectDirectory.FullName, oldProjectDirectory);
                            }
                            mesh.Transformation = mesh.Transformation.CopyToDirectory(meshesDirectory).Replace(projectDirectory.FullName, oldProjectDirectory);
                        }
                    }
                    if (patient.Brain.MRIs.Count > 0)
                    {
                        DirectoryInfo mriDirectory = Directory.CreateDirectory(Path.Combine(patientDirectory.FullName, "MRIs"));
                        foreach (var mri in patient.Brain.MRIs)
                        {
                            mri.File = mri.File.CopyToDirectory(mriDirectory).Replace(projectDirectory.FullName, oldProjectDirectory);
                        }
                    }
                    if (patient.Brain.Implantations.Count > 0)
                    {
                        DirectoryInfo implantationsDirectory = Directory.CreateDirectory(Path.Combine(patientDirectory.FullName, "Implantations"));
                        foreach (var implantation in patient.Brain.Implantations)
                        {
                            implantation.File = implantation.File.CopyToDirectory(implantationsDirectory).Replace(projectDirectory.FullName, oldProjectDirectory);
                            implantation.MarsAtlas = implantation.MarsAtlas.CopyToDirectory(implantationsDirectory).Replace(projectDirectory.FullName, oldProjectDirectory);
                        }
                    }
                    if (patient.Brain.Connectivities.Count > 0)
                    {
                        DirectoryInfo connectivitiesDirectory = Directory.CreateDirectory(Path.Combine(patientDirectory.FullName, "Connectivities"));
                        foreach (var connectivity in patient.Brain.Connectivities)
                        {
                            connectivity.File = connectivity.File.CopyToDirectory(connectivitiesDirectory).Replace(projectDirectory.FullName, oldProjectDirectory);
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
                    if (dataset.Data.Length > 0)
                    {
                        yield return Ninja.JumpToUnity;
                        progress += progressStep;
                        OnChangeProgress.Invoke(progress, 0, new LoadingText("Copying ", dataset.Name));
                        yield return Ninja.JumpBack;

                        DirectoryInfo datasetDirectory = Directory.CreateDirectory(Path.Combine(localizersDirectory.FullName, dataset.Name));
                        foreach (var data in dataset.Data)
                        {
                            DirectoryInfo dataInfoDirectory = new DirectoryInfo(Path.Combine(datasetDirectory.FullName, data.Name));
                            if (!dataInfoDirectory.Exists) dataInfoDirectory = Directory.CreateDirectory(dataInfoDirectory.FullName);
                            data.EEGHeader.CopyToDirectory(dataInfoDirectory);
                            string eeg = data.EEG.CopyToDirectory(dataInfoDirectory).Replace(projectDirectory.FullName, oldProjectDirectory);
                            string pos = data.POS.CopyToDirectory(dataInfoDirectory).Replace(projectDirectory.FullName, oldProjectDirectory);
                            data.SetPathsWithoutCheckingErrors(eeg, pos);
                        }
                    }
                }
            }
        }
        #endregion
    }
}
