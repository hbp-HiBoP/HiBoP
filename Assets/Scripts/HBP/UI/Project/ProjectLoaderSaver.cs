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
using HBP.Data.Visualisation;

namespace HBP.UI
{
    public class ProjectLoaderSaver : MonoBehaviour
    {
        #region Properties
        [SerializeField]
        GameObject progressWindowPrefab;
        LoadingCircle loadingCircle;

        [SerializeField]
        GameObject popUpPrefab;

        Project oldProject;
        string oldProjectLocation;

        enum LoadErrorTypeEnum { None, DirectoryDoNoExist, IsNotAProject, CanNotReadSettings, CanNotReadPatient, CanNotReadGroup, CanNotReadProtocol,
            CanNotReadDataset, CanNotReadSingleVisualisation, CanNotReadMultiVisualisation };
        enum SaveErrorTypeEnum { None, DirectoryDoNoExist, CanNotDeleteOldDirectories, CanNotCreateNewDirectories, CanNotSaveSettings, CanNotSavePatient,
            CanNotSaveGroup, CanNotSaveProtocol, CanNotSaveDataset, CanNotSaveSinglePatientVisualisation, CanNotSaveMultiPatientsVisualisation, CanNotMoveDirectory
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
            int steps = 1 + info.Patients + info.Groups + info.Protocols + ratioDataset * info.Patients * info.Datasets + info.Visualisations;
            float settingsStep = (float)1 / steps;
            float patientStep = (float) info.Patients / steps;
            float groupStep = (float) info.Groups / steps;
            float protocolStep = (float) info.Protocols / steps;
            float datasetStep = (float) ratioDataset * info.Datasets * info.Patients / steps;
            float visualisationStep = (float) info.Visualisations / steps;
            float all = settingsStep + patientStep + groupStep + protocolStep + datasetStep + visualisationStep;
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
            oldProject = ApplicationState.ProjectLoaded;
            oldProjectLocation = ApplicationState.ProjectLoadedLocation;
            ApplicationState.ProjectLoaded = projectToLoad;
            ApplicationState.ProjectLoadedLocation = projectDirectory.Parent.FullName;

            //Load Settings
            GameObject progressWindowObj = Instantiate(progressWindowPrefab, GameObject.Find("Windows").transform) as GameObject;
            progressWindowObj.transform.localPosition = new Vector3(0, 0, 0);
            loadingCircle = progressWindowObj.GetComponent<LoadingCircle>();
            loadingCircle.Set(actualStep, "Loading settings");
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
            FileInfo[] patientFiles = patientDirectory.GetFiles("*" + Data.Patient.Extension, SearchOption.TopDirectoryOnly);
            foreach (FileInfo patientFile in patientFiles)
            {
                yield return Ninja.JumpToUnity;
                loadingCircle.Set(actualStep, "Loading patients : " + patientFile.Name.Remove(patientFile.Name.Length - patientFile.Extension.Length));
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
            FileInfo[] groupFiles = grousDirectory.GetFiles("*" + Data.Group.Extension  , SearchOption.TopDirectoryOnly);
            foreach (FileInfo groupFile in groupFiles)
            {
                yield return Ninja.JumpToUnity;
                loadingCircle.Set(actualStep, "Loading groups : " + groupFile.Name);
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
            FileInfo[] protocolFiles = protocolDirectory.GetFiles("*" + Protocol.Extension, SearchOption.TopDirectoryOnly);
            foreach (FileInfo protocolFile in protocolFiles)
            {
                yield return Ninja.JumpToUnity;
                loadingCircle.Set(actualStep, "Loading protocols : " + protocolFile.Name.Remove(protocolFile.Name.Length - protocolFile.Extension.Length));
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
            FileInfo[] datasetFiles = datasetDirectory.GetFiles("*" + Dataset.Extension, SearchOption.TopDirectoryOnly);
            foreach (FileInfo datasetFile in datasetFiles)
            {
                yield return Ninja.JumpToUnity;
                loadingCircle.Set(actualStep, "Loading datasets : " + datasetFile.Name.Remove(datasetFile.Name.Length - datasetFile.Extension.Length));
                loadingCircle.ChangePercentage(actualStep, actualStep + datasetStep / info.Datasets, info.Patients / 13.0f);
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

            //Load Visualisations
            DirectoryInfo visualisationsDirectory = projectDirectory.GetDirectories("Visualisations", SearchOption.TopDirectoryOnly)[0];
            DirectoryInfo SPvisualisationsDirectory = visualisationsDirectory.GetDirectories("SinglePatient", SearchOption.TopDirectoryOnly)[0];
            DirectoryInfo MPvisualisationsDirectory = visualisationsDirectory.GetDirectories("MultiPatients", SearchOption.TopDirectoryOnly)[0];

            List<SinglePatientVisualisation> singlePatientVisualisations = new List<SinglePatientVisualisation>();
            FileInfo[] singlePatientVisualisationFiles = SPvisualisationsDirectory.GetFiles("*" + SinglePatientVisualisation.Extension, SearchOption.TopDirectoryOnly);
            foreach (FileInfo singlePatientVisualisationFile in singlePatientVisualisationFiles)
            {
                yield return Ninja.JumpToUnity;
                loadingCircle.Set(actualStep, "Loading single patient visualisations : " + singlePatientVisualisationFile.Name.Remove(singlePatientVisualisationFile.Name.Length - singlePatientVisualisationFile.Extension.Length));
                yield return Ninja.JumpBack;
                try
                {
                    SinglePatientVisualisation singlePatientVisualisation = ClassLoaderSaver.LoadFromJson<SinglePatientVisualisation>(singlePatientVisualisationFile.FullName);
                    singlePatientVisualisations.Add(singlePatientVisualisation);
                }
                catch
                {
                    additionalInformations = singlePatientVisualisationFile.Name;
                    loadingError = LoadErrorTypeEnum.CanNotReadSingleVisualisation;
                    break;
                }
                actualStep += visualisationStep / info.Visualisations;

            }
            projectToLoad.SetSinglePatientVisualisations(singlePatientVisualisations.ToArray());
            yield return Ninja.JumpToUnity;
            HandleError(loadingError,additionalInformations);
            yield return Ninja.JumpBack;

            List<MultiPatientsVisualisation> multiPatientsVisualisations = new List<MultiPatientsVisualisation>();
            FileInfo[] multiPatientVisualisationFiles = MPvisualisationsDirectory.GetFiles("*" + MultiPatientsVisualisation.Extension, SearchOption.TopDirectoryOnly);
            foreach (FileInfo multiPatientVisualisationFile in multiPatientVisualisationFiles)
            {
                yield return Ninja.JumpToUnity;
                loadingCircle.Set(actualStep, "Loading multi-patients visualisations : " + multiPatientVisualisationFile.Name.Remove(multiPatientVisualisationFile.Name.Length - multiPatientVisualisationFile.Extension.Length));
                yield return Ninja.JumpBack;
                try
                {
                    MultiPatientsVisualisation multiPatientVisualisation = ClassLoaderSaver.LoadFromJson<MultiPatientsVisualisation>(multiPatientVisualisationFile.FullName);
                    multiPatientsVisualisations.Add(multiPatientVisualisation);
                }
                catch
                {
                    additionalInformations = multiPatientVisualisationFile.Name;
                    loadingError = LoadErrorTypeEnum.CanNotReadMultiVisualisation;
                    break;
                }
                actualStep += visualisationStep / info.Visualisations;

            }
            projectToLoad.SetMultiPatientsVisualisations(multiPatientsVisualisations.ToArray());
            yield return Ninja.JumpToUnity;
            HandleError(loadingError,additionalInformations);
            yield return Ninja.JumpBack;

            yield return Ninja.JumpToUnity;
            loadingCircle.Set(1,"Project loaded succesfully");
            yield return new WaitForSeconds(0.5f);
            loadingCircle.Close();
            GameObject.FindGameObjectWithTag("Gestion").GetComponent<MenuButtonState>().SetInteractableButtons();
        }
        IEnumerator c_Save(string path)
        {
            yield return Ninja.JumpBack;
            Project project = ApplicationState.ProjectLoaded;
            SaveErrorTypeEnum l_loadingState = SaveErrorTypeEnum.None;
            string additionalInformations = "";
            int l_maxStep = project.Patients.Count + project.Groups.Count + project.Protocols.Count + project.Datasets.Count + project.SinglePatientVisualisations.Count + project.MultiPatientsVisualisations.Count + 4;
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
            GameObject progressWindowObj = GameObject.Instantiate(progressWindowPrefab, GameObject.Find("Windows").transform) as GameObject;
            progressWindowObj.transform.localPosition = new Vector3(0, 0, 0);
            loadingCircle = progressWindowObj.GetComponent<LoadingCircle>();
            loadingCircle.Set((float)l_actualStep / l_maxStep, "Creating new directories");
            yield return Ninja.JumpBack;

            string l_projectPath = path + Path.DirectorySeparatorChar + project.Settings.Name;
            string l_projectTempPath = l_projectPath + "-temp";
            DirectoryInfo projectDirectory = new DirectoryInfo("f");
            DirectoryInfo patientDirectory = projectDirectory;
            DirectoryInfo groupDirectory = projectDirectory;
            DirectoryInfo protocolDirectory = projectDirectory;
            DirectoryInfo datasetDirectory = projectDirectory;
            DirectoryInfo RegionOfInterestDirectory = projectDirectory;
            DirectoryInfo visualisationDirectory = projectDirectory;
            DirectoryInfo singlePatientVisualisationDirectory = projectDirectory;
            DirectoryInfo multiPatientsVisualisationDirectory = projectDirectory;
            try
            {
                projectDirectory = Directory.CreateDirectory(l_projectTempPath);
                patientDirectory = Directory.CreateDirectory(projectDirectory.FullName + Path.DirectorySeparatorChar + "Patients");
                groupDirectory = Directory.CreateDirectory(projectDirectory.FullName + Path.DirectorySeparatorChar + "Groups");
                protocolDirectory = Directory.CreateDirectory(projectDirectory.FullName + Path.DirectorySeparatorChar + "Protocols");
                datasetDirectory = Directory.CreateDirectory(projectDirectory.FullName + Path.DirectorySeparatorChar + "Datasets");
                RegionOfInterestDirectory = Directory.CreateDirectory(projectDirectory.FullName + Path.DirectorySeparatorChar + "ROI");
                visualisationDirectory = Directory.CreateDirectory(projectDirectory.FullName + Path.DirectorySeparatorChar + "Visualisations");
                singlePatientVisualisationDirectory = Directory.CreateDirectory(visualisationDirectory.FullName + Path.DirectorySeparatorChar + "SinglePatient");
                multiPatientsVisualisationDirectory = Directory.CreateDirectory(visualisationDirectory.FullName + Path.DirectorySeparatorChar + "MultiPatients");
            }
            catch
            {
                l_loadingState = SaveErrorTypeEnum.CanNotCreateNewDirectories;
            }
            l_actualStep++;
            yield return Ninja.JumpToUnity;
            HandleError(l_loadingState, additionalInformations, projectDirectory);

            // Save settings
            loadingCircle.Set((float)l_actualStep / l_maxStep, "Saving settings");
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
            loadingCircle.Set((float)l_actualStep / l_maxStep, "Saving patients");
            yield return Ninja.JumpBack;
            foreach (Data.Patient patientToSave in project.Patients)
            {
                try
                {
                    ClassLoaderSaver.SaveToJSon(patientToSave, patientDirectory.FullName + Path.DirectorySeparatorChar + patientToSave.ID + Data.Patient.Extension);
                }
                catch
                {
                    additionalInformations = patientToSave.Name;
                    l_loadingState = SaveErrorTypeEnum.CanNotSavePatient;
                    break;
                }
                l_actualStep++;
                yield return Ninja.JumpToUnity;
                loadingCircle.Progress = ((float)l_actualStep / l_maxStep);
                yield return Ninja.JumpBack;
            }
            yield return Ninja.JumpToUnity;
            HandleError(l_loadingState, additionalInformations, projectDirectory);

            // Save groups
            loadingCircle.Set((float)l_actualStep / l_maxStep, "Saving groups");
            yield return Ninja.JumpBack;
            foreach (Data.Group groupToSave in project.Groups)
            {
                try
                {
                    ClassLoaderSaver.SaveToJSon(groupToSave, groupDirectory.FullName + Path.DirectorySeparatorChar + groupToSave.Name + Data.Group.Extension);
                }
                catch
                {
                    additionalInformations = groupToSave.Name;
                    l_loadingState = SaveErrorTypeEnum.CanNotSaveGroup;
                    break;
                }
                l_actualStep++;
                yield return Ninja.JumpToUnity;
                loadingCircle.Progress = ((float)l_actualStep / l_maxStep);
                yield return Ninja.JumpBack;
            }
            yield return Ninja.JumpToUnity;
            HandleError(l_loadingState, additionalInformations, projectDirectory);

            // Save protocols
            loadingCircle.Set((float)l_actualStep / l_maxStep, "Saving protocols");
            yield return Ninja.JumpBack;
            foreach (Protocol protocolToSave in project.Protocols)
            {
                try
                {
                    ClassLoaderSaver.SaveToJSon(protocolToSave, protocolDirectory.FullName + Path.DirectorySeparatorChar + protocolToSave.Name + Protocol.Extension);
                }
                catch
                {
                    additionalInformations = protocolToSave.Name;
                    l_loadingState = SaveErrorTypeEnum.CanNotSaveProtocol;
                    break;
                }
                l_actualStep++;
                yield return Ninja.JumpToUnity;
                loadingCircle.Progress = ((float)l_actualStep / l_maxStep);
                yield return Ninja.JumpBack;
            }
            yield return Ninja.JumpToUnity;
            HandleError(l_loadingState, additionalInformations, projectDirectory);

            //Save datasets
            loadingCircle.Set((float)l_actualStep / l_maxStep, "Saving datasets");
            yield return Ninja.JumpBack;
            foreach (Dataset datasetToSave in project.Datasets)
            {
                try
                {
                    ClassLoaderSaver.SaveToJSon(datasetToSave, datasetDirectory.FullName + Path.DirectorySeparatorChar + datasetToSave.Name + Dataset.Extension);
                }
                catch
                {
                    additionalInformations = datasetToSave.Name;
                    l_loadingState = SaveErrorTypeEnum.CanNotSaveDataset;
                    break;
                }
                l_actualStep++;
                yield return Ninja.JumpToUnity;
                loadingCircle.Progress = ((float)l_actualStep / l_maxStep);
                yield return Ninja.JumpBack;
            }
            yield return Ninja.JumpToUnity;
            HandleError(l_loadingState, additionalInformations, projectDirectory);

            //Save singleVisualisations
            loadingCircle.Set((float)l_actualStep / l_maxStep, "Saving single patient visualisations");
            yield return Ninja.JumpBack;
            foreach (SinglePatientVisualisation visualisation in project.SinglePatientVisualisations)
            {
                try
                {
                    ClassLoaderSaver.SaveToJSon(visualisation, singlePatientVisualisationDirectory.FullName + Path.DirectorySeparatorChar + visualisation.Name + SinglePatientVisualisation.Extension);
                }
                catch
                {
                    additionalInformations = visualisation.Name;
                    l_loadingState = SaveErrorTypeEnum.CanNotSaveSinglePatientVisualisation;
                    break;
                }
                l_actualStep++;
                yield return Ninja.JumpToUnity;
                loadingCircle.Progress = ((float)l_actualStep / l_maxStep);
                yield return Ninja.JumpBack;
            }
            yield return Ninja.JumpToUnity;
            HandleError(l_loadingState, additionalInformations, projectDirectory);

            //Save  multiVisualisations
            loadingCircle.Set((float)l_actualStep / l_maxStep, "Saving multi-patients visualisations");
            yield return Ninja.JumpBack;
            foreach (MultiPatientsVisualisation visualisation in project.MultiPatientsVisualisations)
            {
                try
                {
                    ClassLoaderSaver.SaveToJSon(visualisation, multiPatientsVisualisationDirectory.FullName + Path.DirectorySeparatorChar + visualisation.Name + MultiPatientsVisualisation.Extension);
                }
                catch
                {
                    additionalInformations = visualisation.Name;
                    l_loadingState = SaveErrorTypeEnum.CanNotSaveMultiPatientsVisualisation;
                    break;
                }
                l_actualStep++;
                yield return Ninja.JumpToUnity;
                loadingCircle.Progress = ((float)l_actualStep / l_maxStep);
                yield return Ninja.JumpBack;
            }
            yield return Ninja.JumpToUnity;
            HandleError(l_loadingState, additionalInformations, projectDirectory);

            // Deleting old directories.
            loadingCircle.Set((float)l_actualStep / l_maxStep, "Deleting old directories");
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

            loadingCircle.Set(1, "Project saved succesfully");
            yield return new WaitForSeconds(0.5f);
            loadingCircle.Close();
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
                case LoadErrorTypeEnum.CanNotReadSingleVisualisation: l_errorMessage = "The single patient visualisation file \" <color=red>" + additionalInformations + " </color>\" could not be loaded." + l_lastPart; break;
                case LoadErrorTypeEnum.CanNotReadMultiVisualisation: l_errorMessage = "The multi patients visualisation file \" <color=red>" + additionalInformations + " </color>\" could not be loaded." + l_lastPart; break;
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
                case SaveErrorTypeEnum.CanNotSaveSinglePatientVisualisation: l_errorMessage = "Could not save the single patient visualisation <color=red>" + additionalInformations + "</color>."; break;
                case SaveErrorTypeEnum.CanNotSaveMultiPatientsVisualisation: l_errorMessage = "Could not save the multi patients visualisation <color=red>" + additionalInformations + "</color>."; break;
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
                ApplicationState.ProjectLoaded = oldProject;
                ApplicationState.ProjectLoadedLocation = oldProjectLocation;
                loadingCircle.Close();
                StopAllCoroutines();
                GameObject popUpobj = GameObject.Instantiate(popUpPrefab, GetComponentInParent<VisualisationLoader>().transform) as GameObject;
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
                loadingCircle.Close();
                StopAllCoroutines();
                GameObject popUpobj = Instantiate(popUpPrefab, GetComponentInParent<VisualisationLoader>().transform) as GameObject;
                popUpobj.GetComponent<PopUp>().Show(GetErrorMessage(error, additionalInformations));
            }
        }
        #endregion
    }

}

