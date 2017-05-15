using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using CielaSpike;
using Tools.Unity;
using HBP.Data.General;
using HBP.Data.Settings;
using HBP.Data.Experience.Dataset;
using HBP.Data.Experience.Protocol;
using HBP.Data.Visualization;

namespace HBP.UI
{
    public class ProjectLoaderSaver : MonoBehaviour
    {
        #region Properties
        [SerializeField]
        GameObject m_ProgressWindowPrefab;
        LoadingCircle m_LoadingCircle;

        [SerializeField]
        GameObject m_PopUpPrefab;

        Project m_OldProject;
        string m_OldProjectLocation;

        enum LoadErrorTypeEnum { None, DirectoryDoNoExist, IsNotAProject, CanNotReadSettings, CanNotReadPatient, CanNotReadGroup, CanNotReadProtocol,
            CanNotReadDataset, CanNotReadSingleVisualization, CanNotReadMultiVisualization };
        enum SaveErrorTypeEnum { None, DirectoryDoNoExist, CanNotDeleteOldDirectories, CanNotCreateNewDirectories, CanNotSaveSettings, CanNotSavePatient,
            CanNotSaveGroup, CanNotSaveProtocol, CanNotSaveDataset, CanNotSaveSinglePatientVisualization, CanNotSaveMultiPatientsVisualization, CanNotMoveDirectory
        };
        #endregion

        #region Public Methods
        public void Load(ProjectInfo info)
        {
            this.StartCoroutineAsync(c_Load(info));
        }
        public void Save(string path)
        {
            this.StartCoroutineAsync(c_Save(path));
        }
        public void Save()
        {
            Save(ApplicationState.ProjectLoadedLocation);
        }
        public void SaveAndReload()
        {
            this.StartCoroutineAsync(c_SaveAndReload(ApplicationState.ProjectLoadedLocation));
        }
        #endregion

        #region Coroutine
        IEnumerator c_Load(ProjectInfo info)
        {
            yield return Ninja.JumpBack;

            // Initialize loadingState 
            LoadErrorTypeEnum loadingError = LoadErrorTypeEnum.None;
            string additionalInformations = "";

            // Calculate number of step.
            int ratioDataset = 3;
            int steps = 1 + info.Patients + info.Groups + info.Protocols + ratioDataset * info.Patients * info.Datasets + info.Visualizations;
            float settingsStep = (float)1 / steps;
            float patientStep = (float) info.Patients / steps;
            float groupStep = (float) info.Groups / steps;
            float protocolStep = (float) info.Protocols / steps;
            float datasetStep = (float) ratioDataset * info.Datasets * info.Patients / steps;
            float visualizationStep = (float) info.Visualizations / steps;
            float all = settingsStep + patientStep + groupStep + protocolStep + datasetStep + visualizationStep;
            float actualStep = 0;

            // Test if the directory exist.
            if(!Directory.Exists(info.Path))
            {
                loadingError = LoadErrorTypeEnum.DirectoryDoNoExist;
            }
            yield return Ninja.JumpToUnity;
            HandleError(loadingError,additionalInformations);
            yield return Ninja.JumpBack;

            // Test if the directory is a project.
            DirectoryInfo projectDirectory = new DirectoryInfo(info.Path);
            if(!Project.IsProject(info.Path))
            {
                loadingError = LoadErrorTypeEnum.IsNotAProject;
            }
            yield return Ninja.JumpToUnity;
            HandleError(loadingError,additionalInformations);

            //Settings project
            Project projectToLoad = new Project();
            m_OldProject = ApplicationState.ProjectLoaded;
            m_OldProjectLocation = ApplicationState.ProjectLoadedLocation;
            ApplicationState.ProjectLoaded = projectToLoad;
            ApplicationState.ProjectLoadedLocation = projectDirectory.Parent.FullName;

            //Load Settings
            GameObject progressWindowObj = Instantiate(m_ProgressWindowPrefab, GameObject.Find("Windows").transform) as GameObject;
            progressWindowObj.transform.localPosition = new Vector3(0, 0, 0);
            m_LoadingCircle = progressWindowObj.GetComponent<LoadingCircle>();
            m_LoadingCircle.Set(actualStep, "Loading settings");
            yield return Ninja.JumpBack;

            try
            {
                FileInfo fileInfo = projectDirectory.GetFiles("*" + ProjectSettings.EXTENSION, SearchOption.TopDirectoryOnly)[0];
                projectToLoad.Settings = ClassLoaderSaver.LoadFromJson<ProjectSettings>(fileInfo.FullName);
                actualStep += settingsStep;
            }
            catch
            {
                loadingError = LoadErrorTypeEnum.CanNotReadSettings;
            }
            yield return Ninja.JumpToUnity;
            HandleError(loadingError,additionalInformations);
            yield return Ninja.JumpBack;

            // Load patients.
            List<Data.Patient> patients = new List<Data.Patient>();
            DirectoryInfo patientDirectory = projectDirectory.GetDirectories("Patients", SearchOption.TopDirectoryOnly)[0];
            FileInfo[] patientFiles = patientDirectory.GetFiles("*" + Data.Patient.EXTENSION, SearchOption.TopDirectoryOnly);
            foreach (FileInfo patientFile in patientFiles)
            {
                yield return Ninja.JumpToUnity;
                m_LoadingCircle.Set(actualStep, "Loading patients : " + patientFile.Name.Remove(patientFile.Name.Length - patientFile.Extension.Length));
                yield return Ninja.JumpBack;
                try
                {
                    Data.Patient patient = ClassLoaderSaver.LoadFromJson<Data.Patient>(patientFile.FullName);
                    patients.Add(patient);
                }
                catch
                {
                    additionalInformations = patientFile.Name;
                    loadingError = LoadErrorTypeEnum.CanNotReadPatient;
                    break;
                }
                actualStep += patientStep / info.Patients;
            }
            projectToLoad.SetPatients(patients.ToArray());
            yield return Ninja.JumpToUnity;
            HandleError(loadingError,additionalInformations);
            yield return Ninja.JumpBack;

            // Load groups.
            List<Data.Group> groups = new List<Data.Group>();
            DirectoryInfo grousDirectory = projectDirectory.GetDirectories("Groups", SearchOption.TopDirectoryOnly)[0];
            FileInfo[] groupFiles = grousDirectory.GetFiles("*" + Data.Group.EXTENSION  , SearchOption.TopDirectoryOnly);
            foreach (FileInfo groupFile in groupFiles)
            {
                yield return Ninja.JumpToUnity;
                m_LoadingCircle.Set(actualStep, "Loading groups : " + groupFile.Name);
                yield return Ninja.JumpBack;
                try
                {
                    Data.Group group = ClassLoaderSaver.LoadFromJson<Data.Group>(groupFile.FullName);
                    groups.Add(group);
                }
                catch
                {
                    additionalInformations = groupFile.Name;
                    loadingError = LoadErrorTypeEnum.CanNotReadGroup;
                    break;
                }
                actualStep += groupStep / info.Groups;
            }
            projectToLoad.SetGroups(groups.ToArray());
            yield return Ninja.JumpToUnity;
            HandleError(loadingError,additionalInformations);
            yield return Ninja.JumpBack;

            //Load Protocols
            List<Protocol> protocols = new List<Protocol>();
            DirectoryInfo protocolDirectory = projectDirectory.GetDirectories("Protocols", SearchOption.TopDirectoryOnly)[0];
            FileInfo[] protocolFiles = protocolDirectory.GetFiles("*" + Protocol.EXTENSION, SearchOption.TopDirectoryOnly);
            foreach (FileInfo protocolFile in protocolFiles)
            {
                yield return Ninja.JumpToUnity;
                m_LoadingCircle.Set(actualStep, "Loading protocols : " + protocolFile.Name.Remove(protocolFile.Name.Length - protocolFile.Extension.Length));
                yield return Ninja.JumpBack;
                try
                {
                    Protocol protocol = ClassLoaderSaver.LoadFromJson<Protocol>(protocolFile.FullName);
                    protocols.Add(protocol);
                }
                catch
                {
                    additionalInformations = protocolFile.Name;
                    loadingError = LoadErrorTypeEnum.CanNotReadProtocol;
                    break;
                }
                actualStep += protocolStep / info.Protocols;
            }
            projectToLoad.SetProtocols(protocols.ToArray());
            yield return Ninja.JumpToUnity;
            HandleError(loadingError,additionalInformations);
            yield return Ninja.JumpBack;

            //Load Datasets
            List<Dataset> datasets = new List<Dataset>();
            DirectoryInfo datasetDirectory = projectDirectory.GetDirectories("Datasets", SearchOption.TopDirectoryOnly)[0];
            FileInfo[] datasetFiles = datasetDirectory.GetFiles("*" + Dataset.EXTENSION, SearchOption.TopDirectoryOnly);
            foreach (FileInfo datasetFile in datasetFiles)
            {
                yield return Ninja.JumpToUnity;
                m_LoadingCircle.Set(actualStep, "Loading datasets : " + datasetFile.Name.Remove(datasetFile.Name.Length - datasetFile.Extension.Length));
                m_LoadingCircle.ChangePercentage(actualStep, actualStep + datasetStep / info.Datasets, info.Patients / 13.0f);
                yield return Ninja.JumpBack;
                try
                {
                    Dataset dataset = ClassLoaderSaver.LoadFromJson<Dataset>(datasetFile.FullName);
                    datasets.Add(dataset);
                }
                catch
                {
                    additionalInformations = datasetFile.Name;
                    loadingError = LoadErrorTypeEnum.CanNotReadDataset;
                    break;
                }
                actualStep += datasetStep / info.Datasets;
            }
            projectToLoad.SetDatasets(datasets.ToArray());
            yield return Ninja.JumpToUnity;
            HandleError(loadingError,additionalInformations);
            yield return Ninja.JumpBack;

            //Load Visualizations
            DirectoryInfo visualizationsDirectory = projectDirectory.GetDirectories("Visualizations", SearchOption.TopDirectoryOnly)[0];
            DirectoryInfo SPvisualizationsDirectory = visualizationsDirectory.GetDirectories("SinglePatient", SearchOption.TopDirectoryOnly)[0];
            DirectoryInfo MPvisualizationsDirectory = visualizationsDirectory.GetDirectories("MultiPatients", SearchOption.TopDirectoryOnly)[0];

            List<SinglePatientVisualization> singlePatientVisualizations = new List<SinglePatientVisualization>();
            FileInfo[] singlePatientVisualizationFiles = SPvisualizationsDirectory.GetFiles("*" + SinglePatientVisualization.EXTENSION, SearchOption.TopDirectoryOnly);
            foreach (FileInfo singlePatientVisualizationFile in singlePatientVisualizationFiles)
            {
                yield return Ninja.JumpToUnity;
                m_LoadingCircle.Set(actualStep, "Loading single patient visualizations : " + singlePatientVisualizationFile.Name.Remove(singlePatientVisualizationFile.Name.Length - singlePatientVisualizationFile.Extension.Length));
                yield return Ninja.JumpBack;
                try
                {
                    SinglePatientVisualization singlePatientVisualization = ClassLoaderSaver.LoadFromJson<SinglePatientVisualization>(singlePatientVisualizationFile.FullName);
                    singlePatientVisualizations.Add(singlePatientVisualization);
                }
                catch
                {
                    additionalInformations = singlePatientVisualizationFile.Name;
                    loadingError = LoadErrorTypeEnum.CanNotReadSingleVisualization;
                    break;
                }
                actualStep += visualizationStep / info.Visualizations;

            }
            projectToLoad.SetSinglePatientVisualizations(singlePatientVisualizations.ToArray());
            yield return Ninja.JumpToUnity;
            HandleError(loadingError,additionalInformations);
            yield return Ninja.JumpBack;

            List<MultiPatientsVisualization> multiPatientsVisualizations = new List<MultiPatientsVisualization>();
            FileInfo[] multiPatientVisualizationFiles = MPvisualizationsDirectory.GetFiles("*" + MultiPatientsVisualization.EXTENSION, SearchOption.TopDirectoryOnly);
            foreach (FileInfo multiPatientVisualizationFile in multiPatientVisualizationFiles)
            {
                yield return Ninja.JumpToUnity;
                m_LoadingCircle.Set(actualStep, "Loading multi-patients visualizations : " + multiPatientVisualizationFile.Name.Remove(multiPatientVisualizationFile.Name.Length - multiPatientVisualizationFile.Extension.Length));
                yield return Ninja.JumpBack;
                try
                {
                    MultiPatientsVisualization multiPatientVisualization = ClassLoaderSaver.LoadFromJson<MultiPatientsVisualization>(multiPatientVisualizationFile.FullName);
                    multiPatientsVisualizations.Add(multiPatientVisualization);
                }
                catch
                {
                    additionalInformations = multiPatientVisualizationFile.Name;
                    loadingError = LoadErrorTypeEnum.CanNotReadMultiVisualization;
                    break;
                }
                actualStep += visualizationStep / info.Visualizations;

            }
            projectToLoad.SetMultiPatientsVisualizations(multiPatientsVisualizations.ToArray());
            yield return Ninja.JumpToUnity;
            HandleError(loadingError,additionalInformations);
            yield return Ninja.JumpBack;

            yield return Ninja.JumpToUnity;
            m_LoadingCircle.Set(1,"Project loaded succesfully");
            yield return new WaitForSeconds(0.5f);
            m_LoadingCircle.Close();
            GameObject.FindGameObjectWithTag("Gestion").GetComponent<MenuButtonState>().SetInteractableButtons();
        }
        IEnumerator c_Save(string path)
        {
            yield return Ninja.JumpBack;
            Project project = ApplicationState.ProjectLoaded;
            SaveErrorTypeEnum l_loadingState = SaveErrorTypeEnum.None;
            string additionalInformations = "";
            int l_maxStep = project.Patients.Count + project.Groups.Count + project.Protocols.Count + project.Datasets.Count + project.SinglePatientVisualizations.Count + project.MultiPatientsVisualizations.Count + 4;
            int l_actualStep = 0;

            if (!Directory.Exists(path))
            {
                l_loadingState = SaveErrorTypeEnum.DirectoryDoNoExist;
            }
            yield return Ninja.JumpToUnity;
            HandleError(l_loadingState, additionalInformations, new DirectoryInfo("f"));

            //Find if old directory
            string l_folderToDelete = project.GetProject(path, project.Settings.ID);

            // Create folders
            GameObject progressWindowObj = GameObject.Instantiate(m_ProgressWindowPrefab, GameObject.Find("Windows").transform) as GameObject;
            progressWindowObj.transform.localPosition = new Vector3(0, 0, 0);
            m_LoadingCircle = progressWindowObj.GetComponent<LoadingCircle>();
            m_LoadingCircle.Set((float)l_actualStep / l_maxStep, "Creating new directories");
            yield return Ninja.JumpBack;

            string l_projectPath = path + Path.DirectorySeparatorChar + project.Settings.Name;
            string l_projectTempPath = l_projectPath + "-temp";
            DirectoryInfo projectDirectory = new DirectoryInfo("f");
            DirectoryInfo patientDirectory = projectDirectory;
            DirectoryInfo groupDirectory = projectDirectory;
            DirectoryInfo protocolDirectory = projectDirectory;
            DirectoryInfo datasetDirectory = projectDirectory;
            DirectoryInfo RegionOfInterestDirectory = projectDirectory;
            DirectoryInfo visualizationDirectory = projectDirectory;
            DirectoryInfo singlePatientVisualizationDirectory = projectDirectory;
            DirectoryInfo multiPatientsVisualizationDirectory = projectDirectory;
            try
            {
                projectDirectory = Directory.CreateDirectory(l_projectTempPath);
                patientDirectory = Directory.CreateDirectory(projectDirectory.FullName + Path.DirectorySeparatorChar + "Patients");
                groupDirectory = Directory.CreateDirectory(projectDirectory.FullName + Path.DirectorySeparatorChar + "Groups");
                protocolDirectory = Directory.CreateDirectory(projectDirectory.FullName + Path.DirectorySeparatorChar + "Protocols");
                datasetDirectory = Directory.CreateDirectory(projectDirectory.FullName + Path.DirectorySeparatorChar + "Datasets");
                RegionOfInterestDirectory = Directory.CreateDirectory(projectDirectory.FullName + Path.DirectorySeparatorChar + "ROI");
                visualizationDirectory = Directory.CreateDirectory(projectDirectory.FullName + Path.DirectorySeparatorChar + "Visualizations");
                singlePatientVisualizationDirectory = Directory.CreateDirectory(visualizationDirectory.FullName + Path.DirectorySeparatorChar + "SinglePatient");
                multiPatientsVisualizationDirectory = Directory.CreateDirectory(visualizationDirectory.FullName + Path.DirectorySeparatorChar + "MultiPatients");
            }
            catch
            {
                l_loadingState = SaveErrorTypeEnum.CanNotCreateNewDirectories;
            }
            l_actualStep++;
            yield return Ninja.JumpToUnity;
            HandleError(l_loadingState, additionalInformations, projectDirectory);

            // Save settings
            m_LoadingCircle.Set((float)l_actualStep / l_maxStep, "Saving settings");
            yield return Ninja.JumpBack;

            try
            {
                ClassLoaderSaver.SaveToJSon(project.Settings, projectDirectory.FullName + Path.DirectorySeparatorChar + project.Settings.Name + ProjectSettings.EXTENSION);
            }
            catch
            {
                l_loadingState = SaveErrorTypeEnum.CanNotSaveSettings;
            }
            l_actualStep++;
            yield return Ninja.JumpToUnity;
            HandleError(l_loadingState, additionalInformations, projectDirectory);

            // Save patients
            m_LoadingCircle.Set((float)l_actualStep / l_maxStep, "Saving patients");
            yield return Ninja.JumpBack;
            foreach (Data.Patient patientToSave in project.Patients)
            {
                try
                {
                    ClassLoaderSaver.SaveToJSon(patientToSave, patientDirectory.FullName + Path.DirectorySeparatorChar + patientToSave.ID + Data.Patient.EXTENSION);
                }
                catch
                {
                    additionalInformations = patientToSave.Name;
                    l_loadingState = SaveErrorTypeEnum.CanNotSavePatient;
                    break;
                }
                l_actualStep++;
                yield return Ninja.JumpToUnity;
                m_LoadingCircle.Progress = ((float)l_actualStep / l_maxStep);
                yield return Ninja.JumpBack;
            }
            yield return Ninja.JumpToUnity;
            HandleError(l_loadingState, additionalInformations, projectDirectory);

            // Save groups
            m_LoadingCircle.Set((float)l_actualStep / l_maxStep, "Saving groups");
            yield return Ninja.JumpBack;
            foreach (Data.Group groupToSave in project.Groups)
            {
                try
                {
                    ClassLoaderSaver.SaveToJSon(groupToSave, groupDirectory.FullName + Path.DirectorySeparatorChar + groupToSave.Name + Data.Group.EXTENSION);
                }
                catch
                {
                    additionalInformations = groupToSave.Name;
                    l_loadingState = SaveErrorTypeEnum.CanNotSaveGroup;
                    break;
                }
                l_actualStep++;
                yield return Ninja.JumpToUnity;
                m_LoadingCircle.Progress = ((float)l_actualStep / l_maxStep);
                yield return Ninja.JumpBack;
            }
            yield return Ninja.JumpToUnity;
            HandleError(l_loadingState, additionalInformations, projectDirectory);

            // Save protocols
            m_LoadingCircle.Set((float)l_actualStep / l_maxStep, "Saving protocols");
            yield return Ninja.JumpBack;
            foreach (Protocol protocolToSave in project.Protocols)
            {
                try
                {
                    ClassLoaderSaver.SaveToJSon(protocolToSave, protocolDirectory.FullName + Path.DirectorySeparatorChar + protocolToSave.Name + Protocol.EXTENSION);
                }
                catch
                {
                    additionalInformations = protocolToSave.Name;
                    l_loadingState = SaveErrorTypeEnum.CanNotSaveProtocol;
                    break;
                }
                l_actualStep++;
                yield return Ninja.JumpToUnity;
                m_LoadingCircle.Progress = ((float)l_actualStep / l_maxStep);
                yield return Ninja.JumpBack;
            }
            yield return Ninja.JumpToUnity;
            HandleError(l_loadingState, additionalInformations, projectDirectory);

            //Save datasets
            m_LoadingCircle.Set((float)l_actualStep / l_maxStep, "Saving datasets");
            yield return Ninja.JumpBack;
            foreach (Dataset datasetToSave in project.Datasets)
            {
                try
                {
                    ClassLoaderSaver.SaveToJSon(datasetToSave, datasetDirectory.FullName + Path.DirectorySeparatorChar + datasetToSave.Name + Dataset.EXTENSION);
                }
                catch
                {
                    additionalInformations = datasetToSave.Name;
                    l_loadingState = SaveErrorTypeEnum.CanNotSaveDataset;
                    break;
                }
                l_actualStep++;
                yield return Ninja.JumpToUnity;
                m_LoadingCircle.Progress = ((float)l_actualStep / l_maxStep);
                yield return Ninja.JumpBack;
            }
            yield return Ninja.JumpToUnity;
            HandleError(l_loadingState, additionalInformations, projectDirectory);

            //Save singleVisualizations
            m_LoadingCircle.Set((float)l_actualStep / l_maxStep, "Saving single patient visualizations");
            yield return Ninja.JumpBack;
            foreach (SinglePatientVisualization visualization in project.SinglePatientVisualizations)
            {
                try
                {
                    ClassLoaderSaver.SaveToJSon(visualization, singlePatientVisualizationDirectory.FullName + Path.DirectorySeparatorChar + visualization.Name + SinglePatientVisualization.EXTENSION);
                }
                catch
                {
                    additionalInformations = visualization.Name;
                    l_loadingState = SaveErrorTypeEnum.CanNotSaveSinglePatientVisualization;
                    break;
                }
                l_actualStep++;
                yield return Ninja.JumpToUnity;
                m_LoadingCircle.Progress = ((float)l_actualStep / l_maxStep);
                yield return Ninja.JumpBack;
            }
            yield return Ninja.JumpToUnity;
            HandleError(l_loadingState, additionalInformations, projectDirectory);

            //Save  multiVisualizations
            m_LoadingCircle.Set((float)l_actualStep / l_maxStep, "Saving multi-patients visualizations");
            yield return Ninja.JumpBack;
            foreach (MultiPatientsVisualization visualization in project.MultiPatientsVisualizations)
            {
                try
                {
                    ClassLoaderSaver.SaveToJSon(visualization, multiPatientsVisualizationDirectory.FullName + Path.DirectorySeparatorChar + visualization.Name + MultiPatientsVisualization.EXTENSION);
                }
                catch
                {
                    additionalInformations = visualization.Name;
                    l_loadingState = SaveErrorTypeEnum.CanNotSaveMultiPatientsVisualization;
                    break;
                }
                l_actualStep++;
                yield return Ninja.JumpToUnity;
                m_LoadingCircle.Progress = ((float)l_actualStep / l_maxStep);
                yield return Ninja.JumpBack;
            }
            yield return Ninja.JumpToUnity;
            HandleError(l_loadingState, additionalInformations, projectDirectory);

            // Deleting old directories.
            m_LoadingCircle.Set((float)l_actualStep / l_maxStep, "Deleting old directories");
            yield return Ninja.JumpBack;

            if (l_folderToDelete != string.Empty)
            {
                try
                {
                    Directory.Delete(l_folderToDelete, true);
                }
                catch
                {
                    l_loadingState = SaveErrorTypeEnum.CanNotDeleteOldDirectories;
                }
            }
            l_actualStep++;
            yield return Ninja.JumpToUnity;
            HandleError(l_loadingState, additionalInformations, projectDirectory);

            //Rename directory
            try
            {
                projectDirectory.MoveTo(l_projectPath);
            }
            catch
            {
                l_loadingState = SaveErrorTypeEnum.CanNotMoveDirectory;
            }

            l_actualStep++;
            yield return Ninja.JumpToUnity;
            HandleError(l_loadingState, additionalInformations, projectDirectory);

            m_LoadingCircle.Set(1, "Project saved succesfully");
            yield return new WaitForSeconds(0.5f);
            m_LoadingCircle.Close();
            yield return Ninja.JumpBack;
        }
        IEnumerator c_SaveAndReload(string path)
        {
            yield return c_Save(path);
            yield return Ninja.JumpToUnity;
            GameObject.FindGameObjectWithTag("Gestion").GetComponent<MenuButtonState>().SetInteractableButtons();
            //UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }
        #endregion

        #region Private Methods
        string GetErrorMessage(LoadErrorTypeEnum error, string additionalInformations)
        {
            string l_errorMessage = string.Empty;
            string l_firstPart = "The project could not be loaded.\n";
            string l_lastPart = " Please verify the file.";
            switch (error)
            {
                case LoadErrorTypeEnum.None: l_errorMessage = "None error detected."; return l_errorMessage;
                case LoadErrorTypeEnum.DirectoryDoNoExist: l_errorMessage = "The project directory doesn't exist. Please verify the directory."; break;
                case LoadErrorTypeEnum.IsNotAProject: l_errorMessage = "The project selected isn't a correct project. Please verify the project."; break;
                case LoadErrorTypeEnum.CanNotReadSettings: l_errorMessage = "The settings file could not be loaded .Please verify the settings file of the project selected."; break;
                case LoadErrorTypeEnum.CanNotReadPatient: l_errorMessage = "The patient file \" <color=red>"+ additionalInformations + " </color>\" could not be loaded." + l_lastPart; break;
                case LoadErrorTypeEnum.CanNotReadGroup: l_errorMessage = " The group file \" <color=red>" + additionalInformations + " </color>\" could not be loaded." + l_lastPart; break;
                case LoadErrorTypeEnum.CanNotReadProtocol: l_errorMessage = "The protocol file \" <color=red>" + additionalInformations + " </color>\" could not be loaded." + l_lastPart; break;
                case LoadErrorTypeEnum.CanNotReadDataset: l_errorMessage = "The dataset file \" <color=red>" + additionalInformations + " </color>\" could not be loaded." + l_lastPart; break;
                case LoadErrorTypeEnum.CanNotReadSingleVisualization: l_errorMessage = "The single patient visualization file \" <color=red>" + additionalInformations + " </color>\" could not be loaded." + l_lastPart; break;
                case LoadErrorTypeEnum.CanNotReadMultiVisualization: l_errorMessage = "The multi patients visualization file \" <color=red>" + additionalInformations + " </color>\" could not be loaded." + l_lastPart; break;
            }
            l_errorMessage = l_firstPart + l_errorMessage;
            return l_errorMessage;
        }
        string GetErrorMessage(SaveErrorTypeEnum error, string additionalInformations)
        {
            string l_errorMessage = string.Empty;
            string l_firstPart = "The project could not be saved.\n";
            switch (error)
            {
                case SaveErrorTypeEnum.None: l_errorMessage = "None error detected."; return l_errorMessage;
                case SaveErrorTypeEnum.DirectoryDoNoExist: l_errorMessage = "The location could not be found. Verifiy your location"; break;
                case SaveErrorTypeEnum.CanNotCreateNewDirectories: l_errorMessage = "Could not create a new directory. Verify your right on this directory."; break;
                case SaveErrorTypeEnum.CanNotSaveSettings: l_errorMessage = "Could not save the settings file."; break;
                case SaveErrorTypeEnum.CanNotSavePatient: l_errorMessage = "Could not save the patient <color=red>"+additionalInformations+"</color>."; break;
                case SaveErrorTypeEnum.CanNotSaveGroup: l_errorMessage = "Could not save the group <color=red>" + additionalInformations + "</color>."; break;
                case SaveErrorTypeEnum.CanNotSaveProtocol: l_errorMessage = "Could not save the protocol <color=red>" + additionalInformations + "</color>."; break;
                case SaveErrorTypeEnum.CanNotSaveDataset: l_errorMessage = "Could not save the dataset <color=red>" + additionalInformations + "</color>."; break;
                case SaveErrorTypeEnum.CanNotSaveSinglePatientVisualization: l_errorMessage = "Could not save the single patient visualization <color=red>" + additionalInformations + "</color>."; break;
                case SaveErrorTypeEnum.CanNotSaveMultiPatientsVisualization: l_errorMessage = "Could not save the multi patients visualization <color=red>" + additionalInformations + "</color>."; break;
                case SaveErrorTypeEnum.CanNotDeleteOldDirectories: l_errorMessage = "Could not delete the old directory."; break;
                case SaveErrorTypeEnum.CanNotMoveDirectory: l_errorMessage = "Could not move the directory"; break;
            }
            l_errorMessage = l_firstPart + l_errorMessage;
            return l_errorMessage;
        }
        void HandleError(LoadErrorTypeEnum error, string additionalInformations)
        {
            if(error != LoadErrorTypeEnum.None)
            {
                ApplicationState.ProjectLoaded = m_OldProject;
                ApplicationState.ProjectLoadedLocation = m_OldProjectLocation;
                m_LoadingCircle.Close();
                StopAllCoroutines();
                GameObject popUpobj = GameObject.Instantiate(m_PopUpPrefab, GameObject.Find("Windows").transform) as GameObject;
                popUpobj.GetComponent<PopUp>().Show(GetErrorMessage(error, additionalInformations));
            }
        }
        void HandleError(SaveErrorTypeEnum error, string additionalInformations, DirectoryInfo directory)
        {
            if(error != SaveErrorTypeEnum.None)
            {
                if(directory.Exists)
                {
                    directory.Delete(true);
                }
                m_LoadingCircle.Close();
                StopAllCoroutines();
                GameObject popUpobj = Instantiate(m_PopUpPrefab, GetComponentInParent<Visualization.VisualizationLoader>().transform) as GameObject;
                popUpobj.GetComponent<PopUp>().Show(GetErrorMessage(error, additionalInformations));
            }
        }
        #endregion
    }

}

