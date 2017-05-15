using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using HBP.Data.Settings;
using HBP.Data.Visualization;
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
        public void SetPatients(Patient[] patients)
        {
            this.patients = new List<Patient>();
            AddPatient(patients);
        }
        public void AddPatient(Patient patient)
        {
            patients.Add(patient);
            patient.Brain.LoadImplantations();
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
            RemoveSinglePatientVisualization((from singlePatientVisualization in singlePatientVisualizations where singlePatientVisualization.Patient == patient select singlePatientVisualization).ToArray());
            foreach(MultiPatientsVisualization multiPatientsVisualization in multiPatientsVisualizations)
            {
                multiPatientsVisualization.RemovePatient(patient);
            }
            patients.Remove(patient);
            patient.Brain.UnloadImplantations();
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
        public void RemoveDataset(Dataset[] datasets)
        {
            foreach (Dataset dataset in datasets)
            {
                RemoveDataset(dataset);
            }
        }

        // SinglePatientVisualizations.
        public void SetSinglePatientVisualizations(SinglePatientVisualization[] singlePatientVisualizations)
        {
            this.singlePatientVisualizations = new List<SinglePatientVisualization>();
            AddSinglePatientVisualization(singlePatientVisualizations);
        }
        public void AddSinglePatientVisualization(SinglePatientVisualization singlePatientVisualization)
        {
            singlePatientVisualizations.Add(singlePatientVisualization);
        }
        public void AddSinglePatientVisualization(SinglePatientVisualization[] singlePatientVisualizations)
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
        public void RemoveSinglePatientVisualization(SinglePatientVisualization[] singlePatientVisualizations)
        {
            foreach (SinglePatientVisualization singlePatientVisualization in singlePatientVisualizations)
            {
                RemoveSinglePatientVisualization(singlePatientVisualization);
            }
        }

        // MultiPatientsVisualizations.
        public void SetMultiPatientsVisualizations(MultiPatientsVisualization[] multiPatientsVisualizations)
        {
            this.multiPatientsVisualizations = new List<MultiPatientsVisualization>();
            AddMultiPatientsVisualization(multiPatientsVisualizations);
        }
        public void AddMultiPatientsVisualization(MultiPatientsVisualization multiPatientsVisualization)
        {
            multiPatientsVisualizations.Add(multiPatientsVisualization);
        }
        public void AddMultiPatientsVisualization(MultiPatientsVisualization[] multiPatientsVisualizations)
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
        public void RemoveMultiPatientsVisualization(MultiPatientsVisualization[] multiPatientsVisualizations)
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
        public static IEnumerable<string> GetProject(string path)
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
        #endregion
    }
}
