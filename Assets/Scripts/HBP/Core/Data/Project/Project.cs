using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ThirdParty.CielaSpike;
using Ionic.Zip;
using UnityEngine;
using HBP.Core.Exceptions;
using HBP.Core.Tools;
using HBP.Core.Interfaces;

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
        public Project(ProjectPreferences settings, IEnumerable<Patient> patients, IEnumerable<Group> groups, IEnumerable<Protocol> protocols, IEnumerable<Dataset> datasets, IEnumerable<Visualization> visualizations)
        {
            Preferences = settings;
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
        public Project(ProjectPreferences settings) : this(settings, new Patient[0], new Group[0], new Protocol[0], new Dataset[0], new Visualization[0])
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
        // Protocols.
        public void SetProtocols(IEnumerable<Protocol> protocols)
        {
            m_Protocols = new List<Protocol>();
            AddProtocol(protocols);
            RemoveDataset((from dataset in m_Datasets where !m_Protocols.Any(p => p == dataset.Protocol) select dataset).ToArray());
            foreach (Visualization visualization in m_Visualizations)
            {
                IEEGColumn[] columnsToRemove = visualization.Columns.Where(c => c is IEEGColumn).Select(c => c as IEEGColumn).Where(c => !m_Protocols.Any(p => p == c.Dataset.Protocol)).ToArray();
                foreach (Column column in columnsToRemove)
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
            foreach (Visualization visualization in m_Visualizations)
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
                using (ZipFile zip = ZipFile.Read(path))
                {
                    bool hasPatientsDirectory = false;
                    bool hasGroupsDirectory = false;
                    bool hasProtocolsDirectory = false;
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
                        else if (entryFileName == "Protocols/")
                        {
                            hasProtocolsDirectory = true;
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
                    isProject = hasPatientsDirectory && hasGroupsDirectory && hasProtocolsDirectory && hasDatasetsDirectory && hasVisualizationsDirectory && hasSettingsFile;
                }
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
        public Dictionary<string, List<Tuple<BaseData, string>>> CheckProjectIDs()
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
            foreach (var alias in Preferences.Aliases) addToDict(alias, string.Format("{0} ({1})", alias.Key, getType(alias)));
            foreach (var tag in Preferences.Tags) addToDict(tag, getName(tag));
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
            // Protocols
            foreach (var protocol in m_Protocols)
            {
                addToDict(protocol, getName(protocol));
                foreach (var bloc in protocol.Blocs)
                {
                    addToDict(bloc, string.Format("{0} / {1}", getName(protocol), getName(bloc)));
                    foreach (var subBloc in bloc.SubBlocs)
                    {
                        addToDict(subBloc, string.Format("{0} / {1} / {2}", getName(protocol), getName(bloc), getName(subBloc)));
                        foreach (var ev in subBloc.Events) addToDict(ev, string.Format("{0} / {1} / {2} / {3}", getName(protocol), getName(bloc), getName(subBloc), getName(ev)));
                        foreach (var icon in subBloc.Icons) addToDict(icon, string.Format("{0} / {1} / {2} / {3}", getName(protocol), getName(bloc), getName(subBloc), getName(icon)));
                        foreach (var treatment in subBloc.Treatments) addToDict(treatment, string.Format("{0} / {1} / {2} / {3}", getName(protocol), getName(bloc), getName(subBloc), getType(treatment)));
                    }
                }
            }
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
        }

        public IEnumerator c_Load(ProjectInfo projectInfo, Action<float, float, LoadingText> onChangeProgress)
        {
            yield return Ninja.JumpBack;

            // Initialize progress.
            float steps = 1 + projectInfo.Patients + projectInfo.Groups + projectInfo.Protocols + 5 * projectInfo.Patients * projectInfo.Datasets + projectInfo.Visualizations;
            float progress = 0.0f;

            float settingsProgress = 1 / steps;
            float patientsProgress = projectInfo.Patients / steps;
            float groupsProgress = projectInfo.Groups / steps;
            float protocolsProgress = projectInfo.Protocols / steps;
            float datasetsProgress = 5 * projectInfo.Patients * projectInfo.Datasets / steps;
            float visualizationsProgress = projectInfo.Visualizations / steps;

            onChangeProgress.Invoke(progress, 0, new LoadingText("Loading project"));

            // Unzipping
            if (Directory.Exists(ApplicationState.ExtractProjectFolder)) Directory.Delete(ApplicationState.ExtractProjectFolder, true);
            using (ZipFile zip = ZipFile.Read(projectInfo.Path))
            {
                zip.ExtractAll(ApplicationState.ExtractProjectFolder, ExtractExistingFileAction.OverwriteSilently);
            }

            if (!File.Exists(projectInfo.Path)) throw new FileNotFoundException(projectInfo.Path); // Test if the file exists.
            if (!IsProject(projectInfo.Path)) throw new FileNotFoundException(projectInfo.Path); // Test if the file is a project.
            DirectoryInfo projectDirectory = new DirectoryInfo(ApplicationState.ExtractProjectFolder);

            yield return Ninja.JumpToUnity;

            // Load Settings.
            yield return CoroutineManager.StartAsync(c_LoadSettings(projectDirectory, (localProgress, duration, text) => onChangeProgress.Invoke(progress + localProgress * settingsProgress, duration, text)));
            progress += settingsProgress;

            // Load Patients.
            yield return CoroutineManager.StartAsync(c_LoadPatients(projectDirectory, (localProgress, duration, text) => onChangeProgress.Invoke(progress + localProgress * patientsProgress, duration, text)));
            progress += patientsProgress;

            // Load Groups.
            yield return CoroutineManager.StartAsync(c_LoadGroups(projectDirectory, (localProgress, duration, text) => onChangeProgress.Invoke(progress + localProgress * groupsProgress, duration, text)));
            progress += groupsProgress;

            // Load Protocols.
            yield return CoroutineManager.StartAsync(c_LoadProtocols(projectDirectory, (localProgress, duration, text) => onChangeProgress.Invoke(progress + localProgress * protocolsProgress, duration, text)));
            progress += protocolsProgress;

            // Load Datasets.
            yield return CoroutineManager.StartAsync(c_LoadDatasets(projectDirectory, (localProgress, duration, text) => onChangeProgress.Invoke(progress + localProgress * datasetsProgress, duration, text)));
            progress += datasetsProgress;

            // Load Visualizations.
            yield return CoroutineManager.StartAsync(c_LoadVisualizations(projectDirectory, (localProgress, duration, text) => onChangeProgress.Invoke(progress + localProgress * visualizationsProgress, duration, text)));
            progress += visualizationsProgress;

            yield return Ninja.JumpBack;

            Directory.Delete(ApplicationState.ExtractProjectFolder, true);

            onChangeProgress.Invoke(1.0f, 0, new LoadingText("Project loaded successfully."));
        }
        public IEnumerator c_Save(string path, Action<float, float, LoadingText> onChangeProgress)
        {
            yield return Ninja.JumpBack;
            // Initialize progress.
            float steps = 12 + m_Patients.Count + m_Groups.Count + m_Protocols.Count + m_Datasets.Count + m_Visualizations.Count;
            float progress = 0.0f;

            float initializationProgress = 1 / steps;
            float settingsProgress = 1 / steps;
            float patientsProgress = m_Patients.Count / steps;
            float groupsProgress = m_Groups.Count / steps;
            float protocolsProgress = m_Protocols.Count / steps;
            float datasetsProgress = m_Datasets.Count / steps;
            float visualizationsProgress = m_Visualizations.Count / steps;
            float finalizationProgress = 10 / steps;

            // Initialization.
            onChangeProgress.Invoke(progress, 0, new LoadingText("Initialization"));

            if (string.IsNullOrEmpty(path)) throw new Exceptions.DirectoryNotFoundException("");
            if (!Directory.Exists(path)) throw new Exceptions.DirectoryNotFoundException(path);
            DirectoryInfo projectDirectory = Directory.Exists(ApplicationState.ExtractProjectFolder) ? new DirectoryInfo(ApplicationState.ExtractProjectFolder) : Directory.CreateDirectory(ApplicationState.ExtractProjectFolder);
            progress += initializationProgress;

            onChangeProgress.Invoke(progress, 0, new LoadingText("Saving project"));

            yield return Ninja.JumpToUnity;

            // Save Settings.
            yield return CoroutineManager.StartAsync(c_SaveSettings(projectDirectory, (localProgress, duration, text) => onChangeProgress.Invoke(progress + localProgress * settingsProgress, duration, text)));
            progress += settingsProgress;

            // Save Patients
            yield return CoroutineManager.StartAsync(c_SavePatients(projectDirectory, (localProgress, duration, text) => onChangeProgress.Invoke(progress + localProgress * patientsProgress, duration, text)));
            progress += patientsProgress;

            // Save Groups.
            yield return CoroutineManager.StartAsync(c_SaveGroups(projectDirectory, (localProgress, duration, text) => onChangeProgress.Invoke(progress + localProgress * groupsProgress, duration, text)));
            progress += groupsProgress;

            // Save Protocols.
            yield return CoroutineManager.StartAsync(c_SaveProtocols(projectDirectory, (localProgress, duration, text) => onChangeProgress.Invoke(progress + localProgress * protocolsProgress, duration, text)));
            progress += protocolsProgress;

            // Save Datasets
            yield return CoroutineManager.StartAsync(c_SaveDatasets(projectDirectory, (localProgress, duration, text) => onChangeProgress.Invoke(progress + localProgress * datasetsProgress, duration, text)));
            progress += datasetsProgress;

            // Save Visualizations.
            yield return CoroutineManager.StartAsync(c_SaveVisualizations(projectDirectory, (localProgress, duration, text) => onChangeProgress.Invoke(progress + localProgress * visualizationsProgress, duration, text)));
            progress += visualizationsProgress;

            yield return Ninja.JumpBack;

            // Deleting old directories.
            onChangeProgress.Invoke(progress + finalizationProgress, 0.75f, new LoadingText("Finalizing."));
            progress += finalizationProgress;

            // Zipping
            string filePath = Path.Combine(path, FileName);
            if (File.Exists(filePath)) File.Delete(filePath);
            using (ZipFile zip = new ZipFile(filePath))
            {
                zip.AddDirectory(ApplicationState.ExtractProjectFolder);
                zip.Save();
            }
            Directory.Delete(ApplicationState.ExtractProjectFolder, true);

            onChangeProgress.Invoke(1, 0, new LoadingText("Project saved successfully."));
        }

        public IEnumerator c_CheckDatasets(IEnumerable<Protocol> protocols, Action<float, float, LoadingText> onChangeProgress)
        {
            yield return Ninja.JumpBack;

            IEnumerable<Dataset> datasets = m_Datasets.Where(d => protocols.Contains(d.Protocol));
            int count = 0;
            int length = datasets.SelectMany(d => d.Data).Count();
            foreach (var dataset in datasets)
            {
                for (int j = 0; j < dataset.Data.Count; ++j, ++count)
                {
                    DataInfo data = dataset.Data[j];
                    data.GetErrorsAndWarnings(dataset.Protocol);

                    string message;
                    if (data is PatientDataInfo patientDataInfo) message = patientDataInfo.Name + " | " + dataset.Protocol.Name + " | " + patientDataInfo.Patient.Name;
                    else message = data.Name + " | " + dataset.Protocol.Name;

                    onChangeProgress.Invoke((float)count / length, 0, new LoadingText("Checking ", message, " [" + (count + 1) + "/" + length + "]"));
                }
            }
        }
        public IEnumerator c_CheckPatientTagValues(IEnumerable<BaseTag> tags, Action<float, float, LoadingText> onChangeProgress)
        {
            yield return Ninja.JumpBack;

            // Test patient TagValues;
            int length = m_Patients.Count;
            for (int p = 0; p < length; p++)
            {
                var patient = m_Patients[p];
                onChangeProgress.Invoke((float)(p + 1) / length, 0.1f, new LoadingText("Checking ", patient.Name, " [" + (p + 1) + "/" + length + "]"));
                patient.Tags.RemoveAll(t => t.Tag == null || !Preferences.Tags.Contains(t.Tag));
                BaseTagValue[] tagsToUpdate = patient.Tags.Where(t => tags.Contains(t.Tag)).ToArray();
                foreach (var tagValue in tagsToUpdate)
                {
                    if (tagValue.Tag is IntTag && !(tagValue is IntTagValue))
                    {
                        patient.Tags.Remove(tagValue);
                        var newTagValue = new IntTagValue();
                        newTagValue.Copy(tagValue);
                        patient.Tags.Add(newTagValue);
                        newTagValue.UpdateValue();
                    }
                    else if (tagValue.Tag is FloatTag && !(tagValue is FloatTagValue))
                    {
                        patient.Tags.Remove(tagValue);
                        var newTagValue = new FloatTagValue();
                        newTagValue.Copy(tagValue);
                        patient.Tags.Add(newTagValue);
                        newTagValue.UpdateValue();
                    }
                    else if (tagValue.Tag is BoolTag && !(tagValue is BoolTagValue))
                    {
                        patient.Tags.Remove(tagValue);
                        var newTagValue = new BoolTagValue();
                        newTagValue.Copy(tagValue);
                        patient.Tags.Add(newTagValue);
                        newTagValue.UpdateValue();
                    }
                    else if (tagValue.Tag is EmptyTag && !(tagValue is EmptyTagValue))
                    {
                        patient.Tags.Remove(tagValue);
                        var newTagValue = new EmptyTagValue();
                        newTagValue.Copy(tagValue);
                        patient.Tags.Add(newTagValue);
                        newTagValue.UpdateValue();
                    }
                    else if (tagValue.Tag is EnumTag && !(tagValue is EnumTagValue))
                    {
                        patient.Tags.Remove(tagValue);
                        var newTagValue = new EnumTagValue();
                        newTagValue.Copy(tagValue);
                        patient.Tags.Add(newTagValue);
                        newTagValue.UpdateValue();
                    }
                    else if (tagValue.Tag is StringTag && !(tagValue is StringTagValue))
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
            }
        }
        #endregion

        #region Private Methods
        IEnumerator c_LoadSettings(DirectoryInfo projectDirectory, Action<float, float, LoadingText> onChangeProgress)
        {
            yield return Ninja.JumpBack;
            onChangeProgress.Invoke(0, 0, new LoadingText("Loading settings"));

            FileInfo[] settingsFiles = projectDirectory.GetFiles("*" + ProjectPreferences.EXTENSION, SearchOption.TopDirectoryOnly);
            if (settingsFiles.Length == 0) throw new SettingsFileNotFoundException(); // Test if settings files found.
            else if (settingsFiles.Length > 1) throw new MultipleSettingsFilesFoundException(); // Test if multiple settings files found.
            try
            {
                Preferences = ClassLoaderSaver.LoadFromJson<ProjectPreferences>(settingsFiles[0].FullName);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw new CanNotReadSettingsFileException(settingsFiles[0].Name);
            }
            onChangeProgress.Invoke(1.0f, 0, new LoadingText("Settings loaded successfully"));
        }
        IEnumerator c_LoadPatients(DirectoryInfo projectDirectory, Action<float, float, LoadingText> onChangeProgress)
        {
            const float LOADING_PROGRESS = 0.99f;
            const float CHECKING_PROGRESS = 0.01f;

            yield return Ninja.JumpBack;

            // Load patients.
            List<Patient> patients = new List<Patient>();
            DirectoryInfo patientDirectory = projectDirectory.GetDirectories("Patients", SearchOption.TopDirectoryOnly)[0];
            FileInfo[] patientFiles = patientDirectory.GetFiles("*" + Patient.EXTENSION, SearchOption.TopDirectoryOnly);
            for (int i = 0; i < patientFiles.Length; ++i)
            {
                FileInfo patientFile = patientFiles[i];
                onChangeProgress.Invoke((float)(i + 1) / patientFiles.Length * LOADING_PROGRESS, 0, new LoadingText("Loading patient ", Path.GetFileNameWithoutExtension(patientFile.Name), " [" + (i + 1).ToString() + "/" + patientFiles.Length + "]"));
                try
                {
                    patients.Add(ClassLoaderSaver.LoadFromJson<Patient>(patientFile.FullName));
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    throw new CanNotReadPatientFileException(Path.GetFileNameWithoutExtension(patientFile.Name));
                }
            }
            SetPatients(patients.ToArray());
            yield return Ninja.JumpToUnity;
            yield return CoroutineManager.StartAsync(c_CheckPatientTagValues(Preferences.Tags, (localProgress, duration, text) => onChangeProgress.Invoke(LOADING_PROGRESS + localProgress * CHECKING_PROGRESS, duration, text)));
            yield return Ninja.JumpBack;
            onChangeProgress.Invoke(1.0f, 0, new LoadingText("Patients loaded successfully"));
        }
        IEnumerator c_LoadGroups(DirectoryInfo projectDirectory, Action<float, float, LoadingText> onChangeProgress)
        {
            yield return Ninja.JumpBack;

            // Load groups.
            List<Group> groups = new List<Group>();
            DirectoryInfo groupDirectory = projectDirectory.GetDirectories("Groups", SearchOption.TopDirectoryOnly)[0];
            FileInfo[] groupFiles = groupDirectory.GetFiles("*" + Group.EXTENSION, SearchOption.TopDirectoryOnly);
            for (int i = 0; i < groupFiles.Length; ++i)
            {
                FileInfo groupFile = groupFiles[i];
                onChangeProgress.Invoke((float)(i + 1) / groupFiles.Length, 0, new LoadingText("Loading group ", Path.GetFileNameWithoutExtension(groupFile.Name), " [" + (i + 1).ToString() + "/" + groupFiles.Length + "]"));
                try
                {
                    groups.Add(ClassLoaderSaver.LoadFromJson<Group>(groupFile.FullName));
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    throw new CanNotReadGroupFileException(Path.GetFileNameWithoutExtension(groupFile.Name));
                }
            }
            SetGroups(groups.ToArray());
            onChangeProgress.Invoke(1.0f, 0, new LoadingText("Groups loaded successfully"));
        }
        IEnumerator c_LoadProtocols(DirectoryInfo projectDirectory, Action<float, float, LoadingText> onChangeProgress)
        {
            yield return Ninja.JumpBack;
            //Load Protocols
            List<Protocol> protocols = new List<Protocol>();
            DirectoryInfo protocolDirectory = projectDirectory.GetDirectories("Protocols", SearchOption.TopDirectoryOnly)[0];
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
        IEnumerator c_LoadDatasets(DirectoryInfo projectDirectory, Action<float, float, LoadingText> onChangeProgress)
        {
            const float LOADING_TIME = 0.01f;
            const float CHECKING_TIME = 0.99f;
            yield return Ninja.JumpBack;
            //Load Datasets
            List<Dataset> datasets = new List<Dataset>();
            DirectoryInfo datasetDirectory = projectDirectory.GetDirectories("Datasets", SearchOption.TopDirectoryOnly)[0];
            FileInfo[] datasetFiles = datasetDirectory.GetFiles("*" + Dataset.EXTENSION, SearchOption.TopDirectoryOnly);
            for (int i = 0; i < datasetFiles.Length; ++i)
            {
                FileInfo datasetFile = datasetFiles[i];
                onChangeProgress.Invoke((float)(i + 1) / datasetFiles.Length * LOADING_TIME, 0, new LoadingText("Loading dataset ", Path.GetFileNameWithoutExtension(datasetFile.Name), " [" + (i + 1).ToString() + "/" + datasetFiles.Length + "]"));
                try
                {
                    datasets.Add(ClassLoaderSaver.LoadFromJson<Dataset>(datasetFile.FullName));
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    throw new CanNotReadDatasetFileException(Path.GetFileNameWithoutExtension(datasetFile.Name));
                }
            }
            SetDatasets(datasets.ToArray());
            yield return Ninja.JumpToUnity;
            yield return CoroutineManager.StartAsync(c_CheckDatasets(m_Protocols, (localProgress, duration, text) => onChangeProgress.Invoke(LOADING_TIME + localProgress * CHECKING_TIME, duration, text)));
            yield return Ninja.JumpBack;
            onChangeProgress.Invoke(1.0f, 0, new LoadingText("Datasets loaded successfully"));
        }
        IEnumerator c_LoadVisualizations(DirectoryInfo projectDirectory, Action<float, float, LoadingText> onChangeProgress)
        {
            yield return Ninja.JumpBack;
            //Load Visualizations
            DirectoryInfo visualizationsDirectory = projectDirectory.GetDirectories("Visualizations", SearchOption.TopDirectoryOnly)[0];

            List<Visualization> visualizations = new List<Visualization>();
            FileInfo[] visualizationFiles = visualizationsDirectory.GetFiles("*" + Visualization.EXTENSION, SearchOption.TopDirectoryOnly);
            for (int i = 0; i < visualizationFiles.Length; ++i)
            {
                FileInfo visualizationFile = visualizationFiles[i];
                onChangeProgress.Invoke((float)(i + 1) / visualizationFiles.Length, 0, new LoadingText("Loading visualization ", Path.GetFileNameWithoutExtension(visualizationFile.Name), " [" + (i + 1).ToString() + "/" + visualizationFiles.Length + "]"));
                try
                {
                    visualizations.Add(ClassLoaderSaver.LoadFromJson<Visualization>(visualizationFile.FullName));
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    throw new CanNotReadVisualizationFileException(Path.GetFileNameWithoutExtension(visualizationFile.Name));
                }
            }
            SetVisualizations(visualizations.ToArray());
            onChangeProgress.Invoke(1.0f, 0, new LoadingText("Visualizations loaded successfully"));
        }

        IEnumerator c_SaveSettings(DirectoryInfo projectDirectory, Action<float, float, LoadingText> onChangeProgress)
        {
            // Save settings
            yield return Ninja.JumpBack;
            onChangeProgress.Invoke(0, 0, new LoadingText("Saving settings"));

            try
            {
                ClassLoaderSaver.SaveToJSon(Preferences, Path.Combine(projectDirectory.FullName, Preferences.Name + ProjectPreferences.EXTENSION));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw new CanNotSaveSettingsException();
            }
            onChangeProgress.Invoke(1.0f, 0, new LoadingText("Settings saved successfully"));
        }
        IEnumerator c_SavePatients(DirectoryInfo projectDirectory, Action<float, float, LoadingText> onChangeProgress)
        {
            yield return Ninja.JumpBack;
            DirectoryInfo patientDirectory = Directory.CreateDirectory(Path.Combine(projectDirectory.FullName, "Patients"));
            // Save patients

            int count = 0;
            int length = m_Patients.Count();
            foreach (Patient patient in m_Patients)
            {
                onChangeProgress.Invoke((float)count / length, 0, new LoadingText("Saving patient ", patient.ID, " [" + (count + 1) + "/" + length + "]"));
                try
                {
                    ClassLoaderSaver.SaveToJSon(patient, Path.Combine(patientDirectory.FullName, patient.ID + Patient.EXTENSION));
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
        IEnumerator c_SaveGroups(DirectoryInfo projectDirectory, Action<float, float, LoadingText> onChangeProgress)
        {
            yield return Ninja.JumpBack;
            // Save groups
            DirectoryInfo groupDirectory = Directory.CreateDirectory(Path.Combine(projectDirectory.FullName, "Groups"));

            int count = 0;
            int length = m_Patients.Count();
            foreach (Group group in m_Groups)
            {
                onChangeProgress.Invoke((float)count / length, 0, new LoadingText("Saving group ", group.Name, " [" + (count + 1) + "/" + length + "]"));
                try
                {
                    ClassLoaderSaver.SaveToJSon(group, Path.Combine(groupDirectory.FullName, group.Name + Group.EXTENSION));
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    throw new CanNotSaveSettingsException();
                }
                count++;
            }
            onChangeProgress.Invoke(1.0f, 0, new LoadingText("Groups saved successfully"));
        }
        IEnumerator c_SaveProtocols(DirectoryInfo projectDirectory, Action<float, float, LoadingText> onChangeProgress)
        {
            yield return Ninja.JumpBack;
            // Save protocols
            DirectoryInfo protocolDirectory = Directory.CreateDirectory(Path.Combine(projectDirectory.FullName, "Protocols"));
            int count = 0;
            int length = m_Protocols.Count();
            foreach (Protocol protocol in m_Protocols)
            {
                onChangeProgress.Invoke((float)count / length, 0, new LoadingText("Saving protocol ", protocol.Name, " [" + (count + 1).ToString() + "/" + length + "]"));
                try
                {
                    ClassLoaderSaver.SaveToJSon(protocol, Path.Combine(protocolDirectory.FullName, protocol.Name + Protocol.EXTENSION));
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    throw new CanNotSaveSettingsException();
                }
                count++;
            }
            onChangeProgress.Invoke(1.0f, 0, new LoadingText("Protocols saved successfully"));
        }
        IEnumerator c_SaveDatasets(DirectoryInfo projectDirectory, Action<float, float, LoadingText> onChangeProgress)
        {
            yield return Ninja.JumpBack;
            //Save datasets
            DirectoryInfo datasetDirectory = Directory.CreateDirectory(Path.Combine(projectDirectory.FullName, "Datasets"));

            int count = 0;
            int length = m_Datasets.Count();
            foreach (Dataset dataset in m_Datasets)
            {
                onChangeProgress.Invoke((float)count / length, 0, new LoadingText("Saving dataset ", dataset.Name, " [" + (count + 1).ToString() + "/" + length + "]"));
                try
                {
                    ClassLoaderSaver.SaveToJSon(dataset, Path.Combine(datasetDirectory.FullName, dataset.Name + Dataset.EXTENSION));
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
        IEnumerator c_SaveVisualizations(DirectoryInfo projectDirectory, Action<float, float, LoadingText> onChangeProgress)
        {
            DirectoryInfo visualizationDirectory = Directory.CreateDirectory(Path.Combine(projectDirectory.FullName, "Visualizations"));

            //Save singleVisualizations
            int count = 0;
            int length = m_Visualizations.Count();
            foreach (Visualization visualization in m_Visualizations)
            {
                yield return Ninja.JumpToUnity;
                onChangeProgress.Invoke((float)count / length, 0, new LoadingText("Saving visualization ", visualization.Name, " [" + (count + 1) + "/" + length + "]"));
                yield return Ninja.JumpBack;

                try
                {
                    ClassLoaderSaver.SaveToJSon(visualization, Path.Combine(visualizationDirectory.FullName, visualization.Name + Visualization.EXTENSION));
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    throw new CanNotSaveSettingsException();
                }
                count++;
            }
            onChangeProgress.Invoke(1.0f, 0, new LoadingText("Visualizations saved successfully"));
        }

        void CopyIcons(string oldIconsDirectoryPath, string newIconsDirectoryPath)
        {
            new DirectoryInfo(oldIconsDirectoryPath).CopyFilesRecursively(new DirectoryInfo(newIconsDirectoryPath));
        }
        IEnumerator c_EmbedDataIntoProjectFile(DirectoryInfo projectDirectory, string oldProjectDirectory, Action<float, float, LoadingText> onChangeProgress)
        {
            DirectoryInfo dataDirectory = Directory.CreateDirectory(Path.Combine(projectDirectory.FullName, "Data"));

            float progress = 0.0f;
            float progressStep = 1.0f / (Patients.Count + Datasets.Count);

            yield return Ninja.JumpToUnity;
            onChangeProgress.Invoke(progress, 0, new LoadingText("Copying data"));
            yield return Ninja.JumpBack;

            // Save Patient Data
            if (Patients.Count > 0)
            {
                DirectoryInfo patientsDirectory = Directory.CreateDirectory(Path.Combine(dataDirectory.FullName, "Anatomy"));
                foreach (var patient in Patients)
                {
                    yield return Ninja.JumpToUnity;
                    progress += progressStep;
                    onChangeProgress.Invoke(progress, 0, new LoadingText("Copying ", patient.Name, " anatomical data"));
                    yield return Ninja.JumpBack;

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
                        yield return Ninja.JumpToUnity;
                        progress += progressStep;
                        onChangeProgress.Invoke(progress, 0, new LoadingText("Copying ", dataset.Name));
                        yield return Ninja.JumpBack;

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
