using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using CielaSpike;
using Tools.Unity;
using d = HBP.Data.Patient;
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
        ProgressWindow progressWindow;

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
            // Calculate number of step.
            LoadErrorTypeEnum l_loadingState = LoadErrorTypeEnum.None;
            string additionalInformations = "";
            int l_maxStep = info.Patients + info.Groups + info.Protocols + info.Datasets + info.Visualisations + 1;
            int l_actualStep = 0;
            // Test if the directory exist.
            if(!Directory.Exists(info.Path))
            {
                l_loadingState = LoadErrorTypeEnum.DirectoryDoNoExist;
            }
            yield return Ninja.JumpToUnity;
            HandleError(l_loadingState,additionalInformations);
            yield return Ninja.JumpBack;

            // Test if the directory is a project.
            DirectoryInfo l_projectDirectory = new DirectoryInfo(info.Path);
            if(!Project.IsProject(info.Path))
            {
                l_loadingState = LoadErrorTypeEnum.IsNotAProject;
            }
            yield return Ninja.JumpToUnity;
            HandleError(l_loadingState,additionalInformations);

            //Load Settings
            GameObject progressWindowObj = GameObject.Instantiate(progressWindowPrefab, GameObject.Find("Windows").transform) as GameObject;
            progressWindowObj.transform.localPosition = new Vector3(0, 0, 0);
            progressWindow = progressWindowObj.GetComponent<ProgressWindow>();
            progressWindow.Set(0, "Loading settings");
            yield return Ninja.JumpBack;

            //Setting project
            Project l_project = new Project();
            oldProject = ApplicationState.ProjectLoaded;
            oldProjectLocation = ApplicationState.ProjectLoadedLocation;
            ApplicationState.ProjectLoaded = l_project;
            ApplicationState.ProjectLoadedLocation = l_projectDirectory.Parent.FullName;

            try
            {
                FileInfo fileInfo = l_projectDirectory.GetFiles("*" + FileExtension.Settings, SearchOption.TopDirectoryOnly)[0];
                l_project.Settings = ProjectSettings.LoadJson(fileInfo.FullName);
                l_actualStep++;
            }
            catch
            {
                l_loadingState = LoadErrorTypeEnum.CanNotReadSettings;
            }
            yield return Ninja.JumpToUnity;
            HandleError(l_loadingState,additionalInformations);

            //Load Patients
            progressWindow.Set((float)l_actualStep / l_maxStep, "Loading patients");
            yield return Ninja.JumpBack;

            // Read patients files.
            List<d.Patient> Patients = new List<d.Patient>();
            DirectoryInfo patientsDirectory = l_projectDirectory.GetDirectories("Patients", SearchOption.TopDirectoryOnly)[0];
            FileInfo[] l_patientFiles = patientsDirectory.GetFiles("*" + FileExtension.Patient, SearchOption.TopDirectoryOnly);
            foreach (FileInfo patientFile in l_patientFiles)
            {
                try
                {
                    Patients.Add(d.Patient.LoadJSon(patientFile.FullName));
                }
                catch
                {
                    l_loadingState = LoadErrorTypeEnum.CanNotReadPatient;
                    additionalInformations = patientFile.Name;
                    break;
                }
                l_actualStep++;
                yield return Ninja.JumpToUnity;
                progressWindow.Set((float)l_actualStep / l_maxStep);
                yield return Ninja.JumpBack;
            }
            l_project.SetPatients(Patients.ToArray());
            yield return Ninja.JumpToUnity;
            HandleError(l_loadingState,additionalInformations);

            //Load Groups
            progressWindow.Set("Loading groups");
            yield return Ninja.JumpBack;

            List<d.Group> Groups = new List<d.Group>();
            DirectoryInfo groupsDirectory = l_projectDirectory.GetDirectories("Groups", SearchOption.TopDirectoryOnly)[0];
            FileInfo[] l_groupFiles = groupsDirectory.GetFiles("*" + FileExtension.Group, SearchOption.TopDirectoryOnly);
            foreach (FileInfo groupFile in l_groupFiles)
            {
                try
                {
                    Groups.Add(d.Group.LoadJSon(groupFile.FullName));
                }
                catch
                {
                    additionalInformations = groupFile.Name;
                    l_loadingState = LoadErrorTypeEnum.CanNotReadGroup;
                    break;
                }
                l_actualStep++;
                yield return Ninja.JumpToUnity;
                progressWindow.Set((float)l_actualStep / l_maxStep);
                yield return Ninja.JumpBack;
            }
            l_project.SetGroups(Groups.ToArray());
            yield return Ninja.JumpToUnity;
            HandleError(l_loadingState,additionalInformations);

            //Load Protocols
            progressWindow.Set("Loading protocols");
            yield return Ninja.JumpBack;

            List<Protocol> Protocols = new List<Protocol>();
            DirectoryInfo protocolsDirectory = l_projectDirectory.GetDirectories("Protocols", SearchOption.TopDirectoryOnly)[0];
            FileInfo[] l_protocolsFiles = protocolsDirectory.GetFiles("*" + FileExtension.Protocol, SearchOption.TopDirectoryOnly);
            foreach (FileInfo protocolFile in l_protocolsFiles)
            {
                try
                {
                    Protocols.Add(Protocol.LoadJSon(protocolFile.FullName));
                }
                catch
                {
                    additionalInformations = protocolFile.Name;
                    l_loadingState = LoadErrorTypeEnum.CanNotReadProtocol;
                    break;
                }
                l_actualStep++;
                yield return Ninja.JumpToUnity;
                progressWindow.Set((float)l_actualStep / l_maxStep);
                yield return Ninja.JumpBack;
            }
            l_project.SetProtocols(Protocols.ToArray());
            yield return Ninja.JumpToUnity;
            HandleError(l_loadingState,additionalInformations);

            //Load Datasets
            progressWindow.Set("Loading datasets");
            yield return Ninja.JumpBack;

            List<Dataset> Datasets = new List<Dataset>();
            DirectoryInfo datasetsDirectory = l_projectDirectory.GetDirectories("Datasets", SearchOption.TopDirectoryOnly)[0];
            FileInfo[] l_datasetsFiles = datasetsDirectory.GetFiles("*" + FileExtension.Dataset, SearchOption.TopDirectoryOnly);
            foreach (FileInfo datasetInfo in l_datasetsFiles)
            {
                try
                {
                    Datasets.Add(Dataset.LoadJSon(datasetInfo.FullName));
                }   
                catch
                {
                    additionalInformations = datasetInfo.Name;
                    l_loadingState = LoadErrorTypeEnum.CanNotReadDataset;
                    break;
                }
                l_actualStep++;
                yield return Ninja.JumpToUnity;
                progressWindow.Set((float)l_actualStep / l_maxStep);
                yield return Ninja.JumpBack;
            }
            l_project.SetDatasets(Datasets.ToArray());
            yield return Ninja.JumpToUnity;
            HandleError(l_loadingState,additionalInformations);

            //Load Visualisations
            progressWindow.Set("Loading visualisations");
            yield return Ninja.JumpBack;

            DirectoryInfo visualisationsDirectory = l_projectDirectory.GetDirectories("Visualisations", SearchOption.TopDirectoryOnly)[0];
            DirectoryInfo SPvisualisationsDirectory = visualisationsDirectory.GetDirectories("SinglePatient", SearchOption.TopDirectoryOnly)[0];
            DirectoryInfo MPvisualisationsDirectory = visualisationsDirectory.GetDirectories("MultiPatients", SearchOption.TopDirectoryOnly)[0];

            List<SinglePatientVisualisation> SingleVisualisations = new List<SinglePatientVisualisation>();
            FileInfo[] l_singleVisualisationFiles = SPvisualisationsDirectory.GetFiles("*" + FileExtension.SingleVisualisation, SearchOption.TopDirectoryOnly);
            foreach (FileInfo singlePatientInfo in l_singleVisualisationFiles)
            {
                try
                {
                    SingleVisualisations.Add(SinglePatientVisualisation.LoadJSon(singlePatientInfo.FullName));
                }
                catch
                {
                    additionalInformations = singlePatientInfo.Name;
                    l_loadingState = LoadErrorTypeEnum.CanNotReadSingleVisualisation;
                    break;
                }
                l_actualStep++;
                yield return Ninja.JumpToUnity;
                progressWindow.Set((float)l_actualStep / l_maxStep);
                yield return Ninja.JumpBack;
            }
            l_project.SetSinglePatientVisualisations(SingleVisualisations.ToArray());
            yield return Ninja.JumpToUnity;
            HandleError(l_loadingState,additionalInformations);
            yield return Ninja.JumpBack;

            List<MultiPatientsVisualisation> MultiVisualisations = new List<MultiPatientsVisualisation>();
            FileInfo[] l_multiVisualisationsFiles = MPvisualisationsDirectory.GetFiles("*" + FileExtension.MultiVisualisation, SearchOption.TopDirectoryOnly);
            foreach (FileInfo multiPatientsInfo in l_multiVisualisationsFiles)
            {
                try
                {
                    MultiVisualisations.Add(MultiPatientsVisualisation.LoadJSon(multiPatientsInfo.FullName));
                }
                catch
                {
                    additionalInformations = multiPatientsInfo.Name;
                    l_loadingState = LoadErrorTypeEnum.CanNotReadMultiVisualisation;
                    break;
                }
                l_actualStep++;
                yield return Ninja.JumpToUnity;
                progressWindow.Set((float)l_actualStep / l_maxStep);
                yield return Ninja.JumpBack;
            }
            l_project.SetMultiPatientsVisualisations(MultiVisualisations.ToArray());
            yield return Ninja.JumpToUnity;
            HandleError(l_loadingState,additionalInformations);
            yield return Ninja.JumpBack;

            ApplicationState.ProjectLoadedLocation = l_projectDirectory.Parent.FullName;
            ApplicationState.ProjectLoaded = l_project;

            yield return Ninja.JumpToUnity;
            progressWindow.Set("Project loaded succesfully");
            yield return new WaitForSeconds(0.5f);
            progressWindow.Close();
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
            progressWindow = progressWindowObj.GetComponent<ProgressWindow>();
            progressWindow.Set((float)l_actualStep / l_maxStep, "Creating new directories");
            yield return Ninja.JumpBack;

            string l_projectPath = path + Path.DirectorySeparatorChar + project.Settings.Name;
            string l_projectTempPath = l_projectPath + "-temp";
            DirectoryInfo l_ProjectFolder = new DirectoryInfo("f");
            DirectoryInfo l_PatientsFolder = l_ProjectFolder;
            DirectoryInfo l_GroupsFolder = l_ProjectFolder;
            DirectoryInfo l_ProtocolsFolder = l_ProjectFolder;
            DirectoryInfo l_ExperiencesFolder = l_ProjectFolder;
            DirectoryInfo l_ROIFolder = l_ProjectFolder;
            DirectoryInfo l_Visualisation = l_ProjectFolder;
            DirectoryInfo l_SingleVisualisation = l_ProjectFolder;
            DirectoryInfo l_MultiVisualisation = l_ProjectFolder;
            try
            {
                l_ProjectFolder = Directory.CreateDirectory(l_projectTempPath);
                l_PatientsFolder = Directory.CreateDirectory(l_ProjectFolder.FullName + Path.DirectorySeparatorChar + "Patients");
                l_GroupsFolder = Directory.CreateDirectory(l_ProjectFolder.FullName + Path.DirectorySeparatorChar + "Groups");
                l_ProtocolsFolder = Directory.CreateDirectory(l_ProjectFolder.FullName + Path.DirectorySeparatorChar + "Protocols");
                l_ExperiencesFolder = Directory.CreateDirectory(l_ProjectFolder.FullName + Path.DirectorySeparatorChar + "Datasets");
                l_ROIFolder = Directory.CreateDirectory(l_ProjectFolder.FullName + Path.DirectorySeparatorChar + "ROI");
                l_Visualisation = Directory.CreateDirectory(l_ProjectFolder.FullName + Path.DirectorySeparatorChar + "Visualisations");
                l_SingleVisualisation = Directory.CreateDirectory(l_Visualisation.FullName + Path.DirectorySeparatorChar + "SinglePatient");
                l_MultiVisualisation = Directory.CreateDirectory(l_Visualisation.FullName + Path.DirectorySeparatorChar + "MultiPatients");
            }
            catch
            {
                l_loadingState = SaveErrorTypeEnum.CanNotCreateNewDirectories;
            }
            l_actualStep++;
            yield return Ninja.JumpToUnity;
            HandleError(l_loadingState, additionalInformations, l_ProjectFolder);

            // Save settings
            progressWindow.Set((float)l_actualStep / l_maxStep, "Saving settings");
            yield return Ninja.JumpBack;

            try
            {
                project.Settings.SaveJSon(l_ProjectFolder.FullName);
            }
            catch
            {
                l_loadingState = SaveErrorTypeEnum.CanNotSaveSettings;
            }
            l_actualStep++;
            yield return Ninja.JumpToUnity;
            HandleError(l_loadingState, additionalInformations, l_ProjectFolder);

            // Save patients
            progressWindow.Set((float)l_actualStep / l_maxStep, "Saving patients");
            yield return Ninja.JumpBack;
            foreach (d.Patient patientToSave in project.Patients)
            {
                try
                {
                    patientToSave.SaveJSon(l_PatientsFolder.FullName);
                }
                catch
                {
                    additionalInformations = patientToSave.Name;
                    l_loadingState = SaveErrorTypeEnum.CanNotSavePatient;
                    break;
                }
                l_actualStep++;
                yield return Ninja.JumpToUnity;
                progressWindow.Set((float)l_actualStep / l_maxStep);
                yield return Ninja.JumpBack;
            }
            yield return Ninja.JumpToUnity;
            HandleError(l_loadingState, additionalInformations, l_ProjectFolder);

            // Save groups
            progressWindow.Set((float)l_actualStep / l_maxStep, "Saving groups");
            yield return Ninja.JumpBack;
            foreach (d.Group groupToSave in project.Groups)
            {
                try
                {
                    groupToSave.SaveJSon(l_GroupsFolder.FullName);
                }
                catch
                {
                    additionalInformations = groupToSave.Name;
                    l_loadingState = SaveErrorTypeEnum.CanNotSaveGroup;
                    break;
                }
                l_actualStep++;
                yield return Ninja.JumpToUnity;
                progressWindow.Set((float)l_actualStep / l_maxStep);
                yield return Ninja.JumpBack;
            }
            yield return Ninja.JumpToUnity;
            HandleError(l_loadingState, additionalInformations, l_ProjectFolder);

            // Save protocols
            progressWindow.Set((float)l_actualStep / l_maxStep, "Saving protocols");
            yield return Ninja.JumpBack;
            foreach (Protocol protocolToSave in project.Protocols)
            {
                protocolToSave.SaveJSon(l_ProtocolsFolder.FullName);
                try
                {
                }
                catch
                {
                    additionalInformations = protocolToSave.Name;
                    l_loadingState = SaveErrorTypeEnum.CanNotSaveProtocol;
                    break;
                }
                l_actualStep++;
                yield return Ninja.JumpToUnity;
                progressWindow.Set((float)l_actualStep / l_maxStep);
                yield return Ninja.JumpBack;
            }
            yield return Ninja.JumpToUnity;
            HandleError(l_loadingState, additionalInformations, l_ProjectFolder);

            //Save experiences
            progressWindow.Set((float)l_actualStep / l_maxStep, "Saving datasets");
            yield return Ninja.JumpBack;
            foreach (Dataset experienceToSave in project.Datasets)
            {
                try
                {
                    experienceToSave.SaveJSon(l_ExperiencesFolder.FullName);
                }
                catch
                {
                    additionalInformations = experienceToSave.Name;
                    l_loadingState = SaveErrorTypeEnum.CanNotSaveDataset;
                    break;
                }
                l_actualStep++;
                yield return Ninja.JumpToUnity;
                progressWindow.Set((float)l_actualStep / l_maxStep);
                yield return Ninja.JumpBack;
            }
            yield return Ninja.JumpToUnity;
            HandleError(l_loadingState, additionalInformations, l_ProjectFolder);

            //Save singleVisualisations
            progressWindow.Set((float)l_actualStep / l_maxStep, "Saving single patient visualisations");
            yield return Ninja.JumpBack;
            foreach (SinglePatientVisualisation visualisation in project.SinglePatientVisualisations)
            {
                try
                {
                    visualisation.SaveJSon(l_SingleVisualisation.FullName);
                }
                catch
                {
                    additionalInformations = visualisation.Name;
                    l_loadingState = SaveErrorTypeEnum.CanNotSaveSinglePatientVisualisation;
                    break;
                }
                l_actualStep++;
                yield return Ninja.JumpToUnity;
                progressWindow.Set((float)l_actualStep / l_maxStep);
                yield return Ninja.JumpBack;
            }
            yield return Ninja.JumpToUnity;
            HandleError(l_loadingState, additionalInformations, l_ProjectFolder);

            //Save  multiVisualisations
            progressWindow.Set((float)l_actualStep / l_maxStep, "Saving multi-patients visualisations");
            yield return Ninja.JumpBack;
            foreach (MultiPatientsVisualisation visualisation in project.MultiPatientsVisualisations)
            {
                try
                {
                    visualisation.SaveJSon(l_MultiVisualisation.FullName);
                }
                catch
                {
                    additionalInformations = visualisation.Name;
                    l_loadingState = SaveErrorTypeEnum.CanNotSaveMultiPatientsVisualisation;
                    break;
                }
                l_actualStep++;
                yield return Ninja.JumpToUnity;
                progressWindow.Set((float)l_actualStep / l_maxStep);
                yield return Ninja.JumpBack;
            }
            yield return Ninja.JumpToUnity;
            HandleError(l_loadingState, additionalInformations, l_ProjectFolder);

            // Deleting old directories.
            progressWindow.Set((float)l_actualStep / l_maxStep, "Deleting old directories");
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
            HandleError(l_loadingState, additionalInformations, l_ProjectFolder);

            //Rename directory
            try
            {
                l_ProjectFolder.MoveTo(l_projectPath);
            }
            catch
            {
                l_loadingState = SaveErrorTypeEnum.CanNotMoveDirectory;
            }

            l_actualStep++;
            yield return Ninja.JumpToUnity;
            HandleError(l_loadingState, additionalInformations, l_ProjectFolder);

            progressWindow.Set(1, "Project saved succesfully");
            yield return new WaitForSeconds(0.5f);
            progressWindow.Close();
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
                progressWindow.Close();
                StopAllCoroutines();
                GameObject popUpobj = GameObject.Instantiate(popUpPrefab, GameObject.Find("UI").transform) as GameObject;
                popUpobj.transform.localPosition = new Vector3(0, 0, 0);
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
                progressWindow.Close();
                StopAllCoroutines();
                GameObject popUpobj = GameObject.Instantiate(popUpPrefab, GameObject.Find("UI").transform) as GameObject;
                popUpobj.transform.localPosition = new Vector3(0, 0, 0);
                popUpobj.GetComponent<PopUp>().Show(GetErrorMessage(error, additionalInformations));
            }
        }
        #endregion
    }

}

