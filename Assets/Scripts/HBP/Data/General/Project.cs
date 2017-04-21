using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using HBP.Data.Settings;
using HBP.Data.Visualisation;
using HBP.Data.Experience.Dataset;
using HBP.Data.Experience.Protocol;

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
    *     - Visualisations.
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

        List<SinglePatientVisualisation> singlePatientVisualisations;
        /// <summary>
        /// Singe patient brain visualisations of the project.
        /// </summary>
        public ReadOnlyCollection<SinglePatientVisualisation> SinglePatientVisualisations
        {
            get { return new ReadOnlyCollection<SinglePatientVisualisation>(singlePatientVisualisations); }
        }

        List<MultiPatientsVisualisation> multiPatientsVisualisations;
        /// <summary>
        /// Multi patients brain visualisations of the project.
        /// </summary>
        public ReadOnlyCollection<MultiPatientsVisualisation> MultiPatientsVisualisations
        {
            get { return new ReadOnlyCollection<MultiPatientsVisualisation>(multiPatientsVisualisations); }
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
        /// <param name="singleVisualisations">Single patient visualisations of the project.</param>
        /// <param name="multiVisualisations">Multi patients visualisations of the project.</param>
        public Project(ProjectSettings settings, Patient[] patients, Group[] groups, Protocol[] protocols, Dataset[] datasets, SinglePatientVisualisation[] singleVisualisations, MultiPatientsVisualisation[] multiVisualisations)
        {
            Settings = settings;
            SetPatients(patients);
            SetGroups(groups);
            SetProtocols(protocols);
            SetDatasets(datasets);
            SetSinglePatientVisualisations(singleVisualisations);
            SetMultiPatientsVisualisations(multiVisualisations);
        }
        /// <summary>
        /// Create a new project with only the settings.
        /// </summary>
        /// <param name="settings">Settings of the project.</param>
        public Project(ProjectSettings settings) : this(settings, new Patient[0], new Group[0], new Protocol[0], new Dataset[0] , new SinglePatientVisualisation[0], new MultiPatientsVisualisation[0])
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
        public void SetPatients(Patient[] patients)
        {
            this.patients = new List<Patient>();
            AddPatient(patients);
        }
        public void AddPatient(Patient patient)
        {
            patients.Add(patient);
        }
        public void AddPatient(Patient[] patients)
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
            RemoveSinglePatientVisualisation((from singlePatientVisualisation in singlePatientVisualisations where singlePatientVisualisation.Patient == patient select singlePatientVisualisation).ToArray());
            foreach(MultiPatientsVisualisation multiPatientsVisualisation in multiPatientsVisualisations)
            {
                multiPatientsVisualisation.RemovePatient(patient);
            }
            patients.Remove(patient);
        }
        public void RemovePatient(Patient[] patients)
        {
            foreach (Patient patient in patients)
            {
                RemovePatient(patient);
            }
        }

        // Groups.
        public void SetGroups(Group[] groups)
        {
            this.groups = new List<Group>();
            AddGroup(groups);
        }
        public void AddGroup(Group group)
        {
            groups.Add(group);
        }
        public void AddGroup(Group[] groups)
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
        public void RemoveGroup(Group[] groups)
        {
            foreach (Group group in groups)
            {
                RemoveGroup(group);
            }
        }

        // Protocols.
        public void SetProtocols(Protocol[] protocols)
        {
            this.protocols = new List<Protocol>();
            AddProtocol(protocols);
        }
        public void AddProtocol(Protocol protocol)
        {
            protocols.Add(protocol);
        }
        public void AddProtocol(Protocol[] protocols)
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
            foreach (SinglePatientVisualisation singlePatientVisualisation in singlePatientVisualisations)
            {                
                singlePatientVisualisation.Columns.Remove((from column in singlePatientVisualisation.Columns where column.Protocol == protocol select column).ToArray());
            }
            foreach (MultiPatientsVisualisation multiPatientsVisualisation in multiPatientsVisualisations)
            {
                multiPatientsVisualisation.Columns.Remove((from column in multiPatientsVisualisation.Columns where column.Protocol == protocol select column).ToArray());
            }
            protocols.Remove(protocol);
        }
        public void RemoveProtocol(Protocol[] protocols)
        {
            foreach (Protocol protocol in protocols)
            {
                RemoveProtocol(protocol);
            }
        }

        // Datasets.
        public void SetDatasets(Dataset[] datasets)
        {
            this.datasets = new List<Dataset>();
            AddDataset(datasets);
        }
        public void AddDataset(Dataset dataset)
        {
            datasets.Add(dataset);
            dataset.UpdateDataStates();
        }
        public void AddDataset(Dataset[] datasets)
        {
            foreach (Dataset dataset in datasets)
            {
                AddDataset(dataset);
            }
        }
        public void RemoveDataset(Dataset dataset)
        {
            foreach (SinglePatientVisualisation singlePatientVisualisation in singlePatientVisualisations)
            {
                singlePatientVisualisation.Columns.Remove((from column in singlePatientVisualisation.Columns where column.Dataset == dataset select column).ToArray());
            }
            foreach (MultiPatientsVisualisation multiPatientsVisualisation in multiPatientsVisualisations)
            {
                multiPatientsVisualisation.Columns.Remove((from column in multiPatientsVisualisation.Columns where column.Dataset == dataset select column).ToArray());
            }
            datasets.Remove(dataset);
        }
        public void RemoveDataset(Dataset[] datasets)
        {
            foreach (Dataset dataset in datasets)
            {
                RemoveDataset(dataset);
            }
        }

        // SinglePatientVisualisations.
        public void SetSinglePatientVisualisations(SinglePatientVisualisation[] singlePatientVisualisations)
        {
            this.singlePatientVisualisations = new List<SinglePatientVisualisation>();
            AddSinglePatientVisualisation(singlePatientVisualisations);
        }
        public void AddSinglePatientVisualisation(SinglePatientVisualisation singlePatientVisualisation)
        {
            singlePatientVisualisations.Add(singlePatientVisualisation);
        }
        public void AddSinglePatientVisualisation(SinglePatientVisualisation[] singlePatientVisualisations)
        {
            foreach(SinglePatientVisualisation singlePatientVisualisation in singlePatientVisualisations)
            {
                AddSinglePatientVisualisation(singlePatientVisualisation);
            }
        }
        public void RemoveSinglePatientVisualisation(SinglePatientVisualisation singlePatientVisualisation)
        {
            singlePatientVisualisations.Remove(singlePatientVisualisation);
        }
        public void RemoveSinglePatientVisualisation(SinglePatientVisualisation[] singlePatientVisualisations)
        {
            foreach (SinglePatientVisualisation singlePatientVisualisation in singlePatientVisualisations)
            {
                RemoveSinglePatientVisualisation(singlePatientVisualisation);
            }
        }

        // MultiPatientsVisualisations.
        public void SetMultiPatientsVisualisations(MultiPatientsVisualisation[] multiPatientsVisualisations)
        {
            this.multiPatientsVisualisations = new List<MultiPatientsVisualisation>();
            AddMultiPatientsVisualisation(multiPatientsVisualisations);
        }
        public void AddMultiPatientsVisualisation(MultiPatientsVisualisation multiPatientsVisualisation)
        {
            multiPatientsVisualisations.Add(multiPatientsVisualisation);
        }
        public void AddMultiPatientsVisualisation(MultiPatientsVisualisation[] multiPatientsVisualisations)
        {
            foreach (MultiPatientsVisualisation multiPatientsVisualisation in multiPatientsVisualisations)
            {
                AddMultiPatientsVisualisation(multiPatientsVisualisation);
            }
        }
        public void RemoveMultiPatientsVisualisation(MultiPatientsVisualisation multiPatientsVisualisation)
        {
            multiPatientsVisualisations.Remove(multiPatientsVisualisation);
        }
        public void RemoveMultiPatientsVisualisation(MultiPatientsVisualisation[] multiPatientsVisualisations)
        {
            foreach (MultiPatientsVisualisation multiPatientsVisualisation in multiPatientsVisualisations)
            {
                RemoveMultiPatientsVisualisation(multiPatientsVisualisation);
            }
        }

        // Others.
        public static bool IsProject(string path)
        {
            bool l_isProject = false;
            string l_path = path;
            string l_name;
            if (Directory.Exists(l_path))
            {
                DirectoryInfo l_projectDirectory = new DirectoryInfo(l_path);
                l_name = l_projectDirectory.Name;
                if (Directory.Exists(l_path + Path.DirectorySeparatorChar + "Patients") && Directory.Exists(l_path + Path.DirectorySeparatorChar + "Groups") && Directory.Exists(l_path + Path.DirectorySeparatorChar + "Protocols") && Directory.Exists(l_path + Path.DirectorySeparatorChar + "ROI") && Directory.Exists(l_path + Path.DirectorySeparatorChar + "Datasets") && Directory.Exists(l_path + Path.DirectorySeparatorChar + "Visualisations") && Directory.Exists(l_path + Path.DirectorySeparatorChar + "Visualisations"+Path.DirectorySeparatorChar+"SinglePatient") && Directory.Exists(l_path + Path.DirectorySeparatorChar + "Visualisations" + Path.DirectorySeparatorChar + "MultiPatients") && File.Exists(l_path + Path.DirectorySeparatorChar + l_name + ".settings"))
                {
                    l_isProject = true;
                }
            }
            return l_isProject;
        }
        public static string[] GetProject(string path)
        {
            string[] projects = new string[0];
            if (!string.IsNullOrEmpty(path))
            {
                if (path.Last() != Path.DirectorySeparatorChar && path.Last() != Path.AltDirectorySeparatorChar) path += Path.DirectorySeparatorChar;
                DirectoryInfo directory = new DirectoryInfo(path);
                if (directory.Exists)
                {
                    projects = (from dir in directory.GetDirectories() where (dir.Attributes == FileAttributes.Directory && IsProject(dir.FullName)) select dir.FullName).ToArray();
                }
            }
            return projects;
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
        #endregion
    }
}
