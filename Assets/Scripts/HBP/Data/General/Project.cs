using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using HBP.Data.Settings;
using HBP.Data.Visualization;
using HBP.Data.Experience.Dataset;
using HBP.Data.Experience.Protocol;
using CielaSpike;
using Tools.Unity;
using UnityEngine;
using UnityEngine.Events;

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
        ProjectSettings settings;
        /// <summary>
        /// Settings of the project.
        /// </summary>
        public ProjectSettings Settings
        {
            get { return settings; }
            set { settings = value; }
        }

        List<Patient> patients;
        /// <summary>
        /// Patients of the project.
        /// </summary>
        public ReadOnlyCollection<Patient> Patients
        {
            get { return new ReadOnlyCollection<Patient>(patients); }
        }

        List<Group> groups;
        /// <summary>
        /// Patient groups of the project.
        /// </summary>
        public ReadOnlyCollection<Group> Groups
        {
            get { return new ReadOnlyCollection<Group>(groups); }
        }

        List<Protocol> protocols;
        /// <summary>
        /// Protocols of the project.
        /// </summary>
        public ReadOnlyCollection<Protocol> Protocols
        {
            get { return new ReadOnlyCollection<Protocol>(protocols); }
        }

        List<Dataset> datasets;
        /// <summary>
        /// Datasets of the project.
        /// </summary>
        public ReadOnlyCollection<Dataset> Datasets
        {
            get { return new ReadOnlyCollection<Dataset>(datasets); }
        }

        List<SinglePatientVisualization> singlePatientVisualizations;
        /// <summary>
        /// Singe patient brain visualizations of the project.
        /// </summary>
        public ReadOnlyCollection<SinglePatientVisualization> SinglePatientVisualizations
        {
            get { return new ReadOnlyCollection<SinglePatientVisualization>(singlePatientVisualizations); }
        }

        List<MultiPatientsVisualization> multiPatientsVisualizations;
        /// <summary>
        /// Multi patients brain visualizations of the project.
        /// </summary>
        public ReadOnlyCollection<MultiPatientsVisualization> MultiPatientsVisualizations
        {
            get { return new ReadOnlyCollection<MultiPatientsVisualization>(multiPatientsVisualizations); }
        }

        /// <summary>
        /// Event called when changing loading progress.
        /// <para>Argument 1 : Loading progress.</para>
        /// <para>Argument 2 : Time to reach the progress.</para>
        /// <para>Argument 3 : Message.</para>
        /// </summary>
        public GenericEvent<float, float, string> OnChangeLoadingProgress { get; set; }
        /// <summary>
        /// Event called when changing saving progress.
        /// <para>Argument 1 : Saving progress.</para>
        /// <para>Argument 2 : Time to reach the progress.</para>
        /// <para>Argument 3 : Message.</para>
        /// </summary>
        public GenericEvent<float, float, string> OnChangeSavingProgress { get; set; }
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
        /// <param name="singleVisualizations">Single patient visualizations of the project.</param>
        /// <param name="multiVisualizations">Multi patients visualizations of the project.</param>
        public Project(ProjectSettings settings, Patient[] patients, Group[] groups, Protocol[] protocols, Dataset[] datasets, SinglePatientVisualization[] singleVisualizations, MultiPatientsVisualization[] multiVisualizations)
        {
            Settings = settings;
            SetPatients(patients);
            SetGroups(groups);
            SetProtocols(protocols);
            SetDatasets(datasets);
            SetSinglePatientVisualizations(singleVisualizations);
            SetMultiPatientsVisualizations(multiVisualizations);
            OnChangeLoadingProgress = new GenericEvent<float, float, string>();
            OnChangeSavingProgress = new GenericEvent<float, float, string>();
        }
        /// <summary>
        /// Create a new project with only the settings.
        /// </summary>
        /// <param name="settings">Settings of the project.</param>
        public Project(ProjectSettings settings) : this(settings, new Patient[0], new Group[0], new Protocol[0], new Dataset[0] , new SinglePatientVisualization[0], new MultiPatientsVisualization[0])
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

        #region Public Methods
        // Patients.
        /// <summary>
        /// Set the patients of the project.
        /// </summary>
        /// <param name="patients"></param>
        public void SetPatients(IEnumerable<Patient> patients)
        {
            this.patients = new List<Patient>();
            AddPatient(patients);
        }
        public void AddPatient(Patient patient)
        {
            patients.Add(patient);
            patient.Brain.LoadImplantations();
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
            foreach(Group group in groups)
            {
                group.RemovePatient(patient);
            }
            foreach (Dataset dataset in datasets)
            {
                dataset.Data.Remove(from info in dataset.Data where info.Patient == patient select info);
            }
            RemoveSinglePatientVisualization((from singlePatientVisualization in singlePatientVisualizations where singlePatientVisualization.Patient == patient select singlePatientVisualization).ToArray());
            foreach(MultiPatientsVisualization multiPatientsVisualization in multiPatientsVisualizations)
            {
                multiPatientsVisualization.RemovePatient(patient);
            }
            patients.Remove(patient);
            patient.Brain.UnloadImplantations();
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
            this.groups = new List<Group>();
            AddGroup(groups);
        }
        public void AddGroup(Group group)
        {
            groups.Add(group);
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
            groups.Remove(group);
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
            this.protocols = new List<Protocol>();
            AddProtocol(protocols);
        }
        public void AddProtocol(Protocol protocol)
        {
            protocols.Add(protocol);
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
            foreach (Dataset dataset in datasets)
            {
                dataset.Data.Remove(from info in dataset.Data where info.Protocol == protocol select info);
            }
            foreach (SinglePatientVisualization singlePatientVisualization in singlePatientVisualizations)
            {                
                singlePatientVisualization.Columns.Remove((from column in singlePatientVisualization.Columns where column.Protocol == protocol select column).ToArray());
            }
            foreach (MultiPatientsVisualization multiPatientsVisualization in multiPatientsVisualizations)
            {
                multiPatientsVisualization.Columns.Remove((from column in multiPatientsVisualization.Columns where column.Protocol == protocol select column).ToArray());
            }
            protocols.Remove(protocol);
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
            this.datasets = new List<Dataset>();
            AddDataset(datasets);
        }
        public void AddDataset(Dataset dataset)
        {
            datasets.Add(dataset);
            dataset.UpdateDataStates();
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
            foreach (SinglePatientVisualization singlePatientVisualization in singlePatientVisualizations)
            {
                singlePatientVisualization.Columns.Remove((from column in singlePatientVisualization.Columns where column.Dataset == dataset select column).ToArray());
            }
            foreach (MultiPatientsVisualization multiPatientsVisualization in multiPatientsVisualizations)
            {
                multiPatientsVisualization.Columns.Remove((from column in multiPatientsVisualization.Columns where column.Dataset == dataset select column).ToArray());
            }
            datasets.Remove(dataset);
        }
        public void RemoveDataset(IEnumerable<Dataset> datasets)
        {
            foreach (Dataset dataset in datasets)
            {
                RemoveDataset(dataset);
            }
        }

        // SinglePatientVisualizations.
        public void SetSinglePatientVisualizations(IEnumerable<SinglePatientVisualization> singlePatientVisualizations)
        {
            this.singlePatientVisualizations = new List<SinglePatientVisualization>();
            AddSinglePatientVisualization(singlePatientVisualizations);
        }
        public void AddSinglePatientVisualization(SinglePatientVisualization singlePatientVisualization)
        {
            singlePatientVisualizations.Add(singlePatientVisualization);
        }
        public void AddSinglePatientVisualization(IEnumerable<SinglePatientVisualization> singlePatientVisualizations)
        {
            foreach(SinglePatientVisualization singlePatientVisualization in singlePatientVisualizations)
            {
                AddSinglePatientVisualization(singlePatientVisualization);
            }
        }
        public void RemoveSinglePatientVisualization(SinglePatientVisualization singlePatientVisualization)
        {
            singlePatientVisualizations.Remove(singlePatientVisualization);
        }
        public void RemoveSinglePatientVisualization(IEnumerable<SinglePatientVisualization> singlePatientVisualizations)
        {
            foreach (SinglePatientVisualization singlePatientVisualization in singlePatientVisualizations)
            {
                RemoveSinglePatientVisualization(singlePatientVisualization);
            }
        }

        // MultiPatientsVisualizations.
        public void SetMultiPatientsVisualizations(IEnumerable<MultiPatientsVisualization> multiPatientsVisualizations)
        {
            this.multiPatientsVisualizations = new List<MultiPatientsVisualization>();
            AddMultiPatientsVisualization(multiPatientsVisualizations);
        }
        public void AddMultiPatientsVisualization(MultiPatientsVisualization multiPatientsVisualization)
        {
            multiPatientsVisualizations.Add(multiPatientsVisualization);
        }
        public void AddMultiPatientsVisualization(IEnumerable<MultiPatientsVisualization> multiPatientsVisualizations)
        {
            foreach (MultiPatientsVisualization multiPatientsVisualization in multiPatientsVisualizations)
            {
                AddMultiPatientsVisualization(multiPatientsVisualization);
            }
        }
        public void RemoveMultiPatientsVisualization(MultiPatientsVisualization multiPatientsVisualization)
        {
            multiPatientsVisualizations.Remove(multiPatientsVisualization);
        }
        public void RemoveMultiPatientsVisualization(IEnumerable<MultiPatientsVisualization> multiPatientsVisualizations)
        {
            foreach (MultiPatientsVisualization multiPatientsVisualization in multiPatientsVisualizations)
            {
                RemoveMultiPatientsVisualization(multiPatientsVisualization);
            }
        }

        // Others.
        public static bool IsProject(string path)
        {
            if(!string.IsNullOrEmpty(path))
            {
                DirectoryInfo directory = new DirectoryInfo(path);
                if(directory.Exists)
                {
                    DirectoryInfo[] directories = directory.GetDirectories();
                    if (directories.Any((d) => d.Name == "Patients")
                        && directories.Any((d) => d.Name == "Groups")
                        && directories.Any((d) => d.Name == "Protocols")
                        && directories.Any((d) => d.Name == "Datasets")
                        && directories.Any((d) => d.Name == "Visualizations")
                        && directory.GetFiles().Any((f) => f.Extension == ProjectSettings.EXTENSION))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public static IEnumerable<string> c_GetProject(string path)
        {
            if(!string.IsNullOrEmpty(path))
            {
                DirectoryInfo directory = new DirectoryInfo(path);
                if(directory.Exists)
                {
                    DirectoryInfo[] directories = directory.GetDirectories();
                    return from dir in directories where IsProject(dir.FullName) select dir.FullName;
                }
            }
            return new string[0];
        }
        public string GetProject(string path, string ID)
        {
            if (path != string.Empty && Directory.Exists(path))
            {
                DirectoryInfo l_directory = new DirectoryInfo(path);
                DirectoryInfo[] l_directories = l_directory.GetDirectories("*", SearchOption.TopDirectoryOnly);
                foreach (DirectoryInfo directory in l_directories)
                {
                    FileInfo[] l_files = directory.GetFiles("*" + ProjectSettings.EXTENSION, SearchOption.TopDirectoryOnly);
                    foreach (FileInfo file in l_files)
                    {
                        ProjectSettings l_setting = Tools.Unity.ClassLoaderSaver.LoadFromJson<ProjectSettings>(file.FullName);
                        if (l_setting.ID == ID)
                        {
                            return directory.FullName;
                        }
                    }
                }

            }
            return string.Empty;
        }

        public IEnumerator c_Load(ProjectInfo projectInfo)
        {
            // Initialize progress.
            float progress = 0.0f;
            float progressStep = 1.0f / ( 1 + projectInfo.Patients + projectInfo.Groups + projectInfo.Protocols + projectInfo.Datasets + projectInfo.Visualizations);

            yield return Ninja.JumpToUnity;
            OnChangeLoadingProgress.Invoke(progress, 0, "Loading project");
            yield return Ninja.JumpBack;

            if (!Directory.Exists(projectInfo.Path)) throw new DirectoryNotFoundException(projectInfo.Path); // Test if the directory exist.
            if (!IsProject(projectInfo.Path)) throw new DirectoryNotProjectException(projectInfo.Path); // Test if the directory is a project.
            DirectoryInfo projectDirectory = new DirectoryInfo(projectInfo.Path);

            Action<float> updateProgress = new Action<float>((value) => progress = value);
            yield return c_LoadSettings(projectDirectory, progress, progressStep, updateProgress);
            yield return c_LoadPatients(projectDirectory, progress, progressStep, updateProgress);
            yield return c_LoadGroups(projectDirectory, progress, progressStep, updateProgress);
            yield return c_LoadProtocols(projectDirectory, progress, progressStep, updateProgress);
            yield return c_LoadDatasets(projectDirectory, progress, progressStep, updateProgress);
            yield return c_LoadVisualizations(projectDirectory, progress, progressStep, updateProgress);
            
            yield return Ninja.JumpToUnity;
            OnChangeLoadingProgress.Invoke(1, 0, "Project succesfully loaded.");
        }
        public IEnumerator c_Save(string path)
        {
            float progress = 0.0f;
            float progressStep = 1.0f / ( 4 + Patients.Count + Groups.Count + Protocols.Count + Datasets.Count + SinglePatientVisualizations.Count + MultiPatientsVisualizations.Count);

            yield return Ninja.JumpToUnity;
            OnChangeSavingProgress.Invoke(progress, 0, "Saving project.");
            yield return Ninja.JumpBack;

            if (String.IsNullOrEmpty(path)) throw new ArgumentNullException(path);
            if (!Directory.Exists(path)) throw new DirectoryNotFoundException(path);
            DirectoryInfo projectDirectory = Directory.CreateDirectory(path + Path.DirectorySeparatorChar + Settings.Name + "-temp");
            string oldProjectDirectory = GetProject(path, Settings.ID);
            progress += progressStep;

            yield return Ninja.JumpToUnity;
            OnChangeSavingProgress.Invoke(progress, 0, "Saving project.");
            yield return Ninja.JumpBack;

            Action<float> updateProgress = new Action<float>((value) => progress = value);
            yield return c_SaveSettings(projectDirectory, progress, progressStep, updateProgress);
            yield return c_SavePatients(projectDirectory, progress, progressStep, updateProgress);
            yield return c_SaveGroups(projectDirectory, progress, progressStep, updateProgress);
            yield return c_SaveProtocols(projectDirectory, progress, progressStep, updateProgress);
            yield return c_SaveDatasets(projectDirectory, progress, progressStep, updateProgress);
            yield return c_SaveVisualizations(projectDirectory, progress, progressStep, updateProgress);      

            // Deleting old directories.
            yield return Ninja.JumpToUnity;
            OnChangeSavingProgress.Invoke(progress, 0, "Finalizing.");
            yield return Ninja.JumpBack;

            if (Directory.Exists(oldProjectDirectory))
            {
                try
                {
                    Directory.Delete(oldProjectDirectory,true);
                }
                catch
                {
                    throw new CanNotDeleteOldProjectDirectory(oldProjectDirectory);
                }
            }
            progress += progressStep;

            try
            {
                projectDirectory.MoveTo(path + Path.DirectorySeparatorChar + Settings.Name);
            }
            catch
            {
                throw new CanNotRenameProjectDirectory();
            }
            progress += progressStep;

            yield return Ninja.JumpToUnity;
            OnChangeSavingProgress.Invoke(1, 0, "Project succesfully saved.");
        }
        #endregion

        #region Private Methods
        IEnumerator c_LoadSettings(DirectoryInfo projectDirectory, float progress, float progressStep,Action<float> outPut)
        {
            // Loading settings file.
            yield return Ninja.JumpToUnity;
            OnChangeLoadingProgress.Invoke(progress, 0, "Loading settings");
            yield return Ninja.JumpBack;

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
        IEnumerator c_LoadPatients(DirectoryInfo projectDirectory, float progress, float progressStep, Action<float> outPut)
        {
            // Load patients.
            List<Patient> patients = new List<Patient>();
            DirectoryInfo patientDirectory = projectDirectory.GetDirectories("Patients", SearchOption.TopDirectoryOnly)[0];
            FileInfo[] patientFiles = patientDirectory.GetFiles("*" + Patient.EXTENSION, SearchOption.TopDirectoryOnly);
            foreach (FileInfo patientFile in patientFiles)
            {
                yield return Ninja.JumpToUnity;
                OnChangeLoadingProgress.Invoke(progress, 0, "Loading patient <color=blue>" + Path.GetFileNameWithoutExtension(patientFile.Name) + "</color>.");
                yield return Ninja.JumpBack;
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
        IEnumerator c_LoadGroups(DirectoryInfo projectDirectory, float progress, float progressStep, Action<float> outPut)
        {
            // Load groups.
            List<Group> groups = new List<Group>();
            DirectoryInfo groupDirectory = projectDirectory.GetDirectories("Groups", SearchOption.TopDirectoryOnly)[0];
            FileInfo[] groupFiles = groupDirectory.GetFiles("*" + Group.EXTENSION, SearchOption.TopDirectoryOnly);
            foreach (FileInfo groupFile in groupFiles)
            {
                yield return Ninja.JumpToUnity;
                OnChangeLoadingProgress.Invoke(progress, 0, "Loading group <color=blue>" + Path.GetFileNameWithoutExtension(groupFile.Name) + "</color>.");
                yield return Ninja.JumpBack;
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
        IEnumerator c_LoadProtocols(DirectoryInfo projectDirectory, float progress, float progressStep,Action<float> outPut)
        {
            //Load Protocols
            List<Protocol> protocols = new List<Protocol>();
            DirectoryInfo protocolDirectory = projectDirectory.GetDirectories("Protocols", SearchOption.TopDirectoryOnly)[0];
            FileInfo[] protocolFiles = protocolDirectory.GetFiles("*" + Protocol.EXTENSION, SearchOption.TopDirectoryOnly);
            foreach (FileInfo protocolFile in protocolFiles)
            {
                yield return Ninja.JumpToUnity;
                OnChangeLoadingProgress.Invoke(progress, 0, "Loading protocol <color=blue>" + Path.GetFileNameWithoutExtension(protocolFile.Name) + "</color>.");
                yield return Ninja.JumpBack;
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
        IEnumerator c_LoadDatasets(DirectoryInfo projectDirectory, float progress, float progressStep, Action<float> outPut)
        {
            //Load Datasets
            List<Dataset> datasets = new List<Dataset>();
            DirectoryInfo datasetDirectory = projectDirectory.GetDirectories("Datasets", SearchOption.TopDirectoryOnly)[0];
            FileInfo[] datasetFiles = datasetDirectory.GetFiles("*" + Dataset.EXTENSION, SearchOption.TopDirectoryOnly);
            foreach (FileInfo datasetFile in datasetFiles)
            {
                yield return Ninja.JumpToUnity;
                OnChangeLoadingProgress.Invoke(progress, 0, "Loading dataset <color=blue>" + Path.GetFileNameWithoutExtension(datasetFile.Name) + "</color>.");
                yield return Ninja.JumpBack;
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
        IEnumerator c_LoadVisualizations(DirectoryInfo projectDirectory, float progress, float progressStep, Action<float> outPut)
        {
            //Load Visualizations
            DirectoryInfo visualizationsDirectory = projectDirectory.GetDirectories("Visualizations", SearchOption.TopDirectoryOnly)[0];

            List<SinglePatientVisualization> singlePatientVisualizations = new List<SinglePatientVisualization>();
            DirectoryInfo SPvisualizationsDirectory = visualizationsDirectory.GetDirectories("SinglePatient", SearchOption.TopDirectoryOnly)[0];
            FileInfo[] singlePatientVisualizationFiles = SPvisualizationsDirectory.GetFiles("*" + SinglePatientVisualization.EXTENSION, SearchOption.TopDirectoryOnly);
            foreach (FileInfo singlePatientVisualizationFile in singlePatientVisualizationFiles)
            {
                yield return Ninja.JumpToUnity;
                OnChangeLoadingProgress.Invoke(progress, 0, "Loading visualization <color=blue>" + Path.GetFileNameWithoutExtension(singlePatientVisualizationFile.Name) + "</color>.");
                yield return Ninja.JumpBack;
                try
                {
                    singlePatientVisualizations.Add(ClassLoaderSaver.LoadFromJson<SinglePatientVisualization>(singlePatientVisualizationFile.FullName));
                }
                catch
                {
                    throw new CanNotReadVisualizationFileException(Path.GetFileNameWithoutExtension(singlePatientVisualizationFile.Name));
                }
                progress += progressStep;
            }
            SetSinglePatientVisualizations(singlePatientVisualizations.ToArray());

            List<MultiPatientsVisualization> multiPatientsVisualizations = new List<MultiPatientsVisualization>();
            DirectoryInfo MPvisualizationsDirectory = visualizationsDirectory.GetDirectories("MultiPatients", SearchOption.TopDirectoryOnly)[0];
            FileInfo[] multiPatientVisualizationFiles = MPvisualizationsDirectory.GetFiles("*" + MultiPatientsVisualization.EXTENSION, SearchOption.TopDirectoryOnly);
            foreach (FileInfo multiPatientVisualizationFile in multiPatientVisualizationFiles)
            {
                yield return Ninja.JumpToUnity;
                OnChangeLoadingProgress.Invoke(progress, 0, "Loading visualization <color=blue>" + Path.GetFileNameWithoutExtension(multiPatientVisualizationFile.Name) + "</color>.");
                yield return Ninja.JumpBack;
                try
                {
                    multiPatientsVisualizations.Add(ClassLoaderSaver.LoadFromJson<MultiPatientsVisualization>(multiPatientVisualizationFile.FullName));
                }
                catch
                {
                    throw new CanNotReadVisualizationFileException(Path.GetFileNameWithoutExtension(multiPatientVisualizationFile.Name));
                }
                progress += progressStep;

            }
            SetMultiPatientsVisualizations(multiPatientsVisualizations.ToArray());
            outPut(progress);
        }
        IEnumerator c_SaveSettings(DirectoryInfo projectDirectory, float progress, float progressStep, Action<float> outPut)
        {
            // Save settings
            yield return Ninja.JumpToUnity;
            OnChangeSavingProgress.Invoke(progress, 0, "Saving settings.");
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
        IEnumerator c_SavePatients(DirectoryInfo projectDirectory, float progress, float progressStep, Action<float> outPut)
        {
            DirectoryInfo patientDirectory = Directory.CreateDirectory(projectDirectory.FullName + Path.DirectorySeparatorChar + "Patients");
            // Save patients
            foreach (Patient patient in Patients)
            {
                yield return Ninja.JumpToUnity;
                OnChangeSavingProgress.Invoke(progress, 0, "Saving patient <color=blue>" + patient.ID +"</color>.");
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
        IEnumerator c_SaveGroups(DirectoryInfo projectDirectory, float progress, float progressStep, Action<float> outPut)
        {
            // Save groups
            DirectoryInfo groupDirectory = Directory.CreateDirectory(projectDirectory.FullName + Path.DirectorySeparatorChar + "Groups");
            foreach (Group group in Groups)
            {
                yield return Ninja.JumpToUnity;
                OnChangeSavingProgress.Invoke(progress, 0, "Saving group <color=blue>" + group.Name + "</color>.");
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
        IEnumerator c_SaveProtocols(DirectoryInfo projectDirectory, float progress, float progressStep, Action<float> outPut)
        {
            // Save protocols
            DirectoryInfo protocolDirectory = Directory.CreateDirectory(projectDirectory.FullName + Path.DirectorySeparatorChar + "Protocols");
            foreach (Protocol protocol in Protocols)
            {
                yield return Ninja.JumpToUnity;
                OnChangeSavingProgress.Invoke(progress, 0, "Saving protocol <color=blue>" + protocol.Name + "</color>.");
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
        IEnumerator c_SaveDatasets(DirectoryInfo projectDirectory, float progress, float progressStep, Action<float> outPut)
        {
            //Save datasets
            DirectoryInfo datasetDirectory = Directory.CreateDirectory(projectDirectory.FullName + Path.DirectorySeparatorChar + "Datasets");
            foreach (Dataset dataset in Datasets)
            {
                yield return Ninja.JumpToUnity;
                OnChangeSavingProgress.Invoke(progress, 0, "Saving dataset <color=blue>" + dataset.Name +"</color>.");
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
        IEnumerator c_SaveVisualizations(DirectoryInfo projectDirectory, float progress, float progressStep, Action<float> outPut)
        {
            DirectoryInfo visualizationDirectory = Directory.CreateDirectory(projectDirectory.FullName + Path.DirectorySeparatorChar + "Visualizations");
            DirectoryInfo singlePatientVisualizationDirectory = Directory.CreateDirectory(visualizationDirectory.FullName + Path.DirectorySeparatorChar + "SinglePatient");
            DirectoryInfo multiPatientsVisualizationDirectory = Directory.CreateDirectory(visualizationDirectory.FullName + Path.DirectorySeparatorChar + "MultiPatients");

            //Save singleVisualizations
            foreach (SinglePatientVisualization visualization in SinglePatientVisualizations)
            {
                yield return Ninja.JumpToUnity;
                OnChangeSavingProgress.Invoke(progress, 0, "Saving visualization <color=blue>" + visualization.Name + "</color>.");
                yield return Ninja.JumpBack;

                try
                {
                    ClassLoaderSaver.SaveToJSon(visualization, singlePatientVisualizationDirectory.FullName + Path.DirectorySeparatorChar + visualization.Name + SinglePatientVisualization.EXTENSION);
                }
                catch
                {
                    throw new CanNotSaveSettingsException();
                }
                progress += progressStep;
            }

            //Save  multiVisualizations
            foreach (MultiPatientsVisualization visualization in MultiPatientsVisualizations)
            {
                yield return Ninja.JumpToUnity;
                OnChangeSavingProgress.Invoke(progress, 0, "Saving visualization <color=blue>" + visualization.Name + "</color>.");
                yield return Ninja.JumpBack;

                try
                {
                    ClassLoaderSaver.SaveToJSon(visualization, multiPatientsVisualizationDirectory.FullName + Path.DirectorySeparatorChar + visualization.Name + MultiPatientsVisualization.EXTENSION);
                }
                catch
                {
                    throw new CanNotSaveSettingsException();
                }
                progress += progressStep;
            }
            outPut(progress);
        }
        #endregion
    }
}
