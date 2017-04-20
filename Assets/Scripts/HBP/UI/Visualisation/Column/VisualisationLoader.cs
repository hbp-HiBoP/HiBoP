using UnityEngine;
using Tools.Unity;
using CielaSpike;
using System.Linq;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using HBP.VISU3D;
using HBP.Data.Visualisation;
using HBP.Data.Experience.Dataset;

namespace HBP.UI
{
    public class VisualisationLoader : MonoBehaviour
    {
        #region Properties
        HiBoP_3DModule_API command;

        [SerializeField]
        GameObject loadingCirclePrefab;
        LoadingCircle loadingCircle;

        [SerializeField]
        GameObject popUpPrefab;

        // Loading time ratio.
        const float FIND_FILES_TO_READ = 0.025f;
        const float READ_FILES = 0.8f;
        const float EPOCH_DATA = 0.025f;
        const float STANDARDIZE_COLUMNS = 0.15f;

        // Loading error type.
        enum LoadingErrorEnum { None, CanNotFindFilesToRead, CanNotReadData, CanNotEpochingData, CanNotStandardizeColumns };
        #endregion
        #region Public Methods
        public void Load(Visualisation visualisation)
        {
            this.StartCoroutineAsync(c_Load(visualisation));
        }
        #endregion
        #region Private Methods
        void Awake()
        {
            command = FindObjectOfType<HiBoP_3DModule_API>();
            command.LoadSPSceneFromMP.AddListener((i) => LoadSPSceneFromMP(i));
        }
        IEnumerator c_Load(Visualisation visualisation)
        {
            // Initialize.
            LoadingErrorEnum loadingError = LoadingErrorEnum.None;
            string additionalInformations = string.Empty;
            Stopwatch timer = new Stopwatch();

            // Open progress window.
            yield return Ninja.JumpToUnity;
            loadingCircle = (Instantiate(loadingCirclePrefab, Vector3.zero, Quaternion.identity, GameObject.Find("Windows").transform) as GameObject).GetComponent<LoadingCircle>();
            loadingCircle.transform.localPosition = new Vector3(0, 0, 0);
            loadingCircle.Set(0, "Finding files");

            // Find files to read.
            Dictionary<Column, DataInfo[]> dataInfoByColumn = new Dictionary<Column, DataInfo[]>();
            float progressStep = FIND_FILES_TO_READ / (visualisation.Columns.Count);
            foreach (var column in visualisation.Columns)
            {
                loadingCircle.Progress += progressStep;
                yield return Ninja.JumpBack;
                try
                {
                    dataInfoByColumn.Add(column, visualisation.GetDataInfo(column));
                }
                catch
                {
                    additionalInformations = (column.DataLabel).ToString();
                    loadingError = LoadingErrorEnum.CanNotFindFilesToRead;
                    break;
                }
                yield return Ninja.JumpToUnity;
            }
            Dictionary<DataInfo, Data.Experience.Dataset.Data> dataByDataInfo = (from dataInfos in dataInfoByColumn.Values from dataInfo in dataInfos select dataInfo).Distinct().ToDictionary(t => t, t=> new Data.Experience.Dataset.Data());
            HandleError(loadingError, additionalInformations);
            yield return Ninja.JumpBack;

            // Read files.
            progressStep = READ_FILES / (dataByDataInfo.Count);
            float readingSpeed = 18000000;
            List<DataInfo> dataInfoToRead = dataByDataInfo.Keys.ToList();
            foreach (var dataInfo in dataInfoToRead)
            {
                // Find file to read informations.
                FileInfo fileToRead = new FileInfo(dataInfo.EEG);
                float assumedReadingTime = (float)fileToRead.Length / readingSpeed;

                // Update progressBar
                yield return Ninja.JumpToUnity;
                loadingCircle.Text = "Reading " + fileToRead.Name;
                loadingCircle.ChangePercentage(loadingCircle.Progress, loadingCircle.Progress + progressStep, assumedReadingTime);
                yield return Ninja.JumpBack;

                // Read Data.
                //try
                //{
                    timer.Start();
                    dataByDataInfo[dataInfo] = new Data.Experience.Dataset.Data(dataInfo, visualisation is MultiPatientsVisualisation);
                    timer.Stop();
                //}
                //catch
                //{
                //    loadingError = LoadingErrorEnum.CanNotReadData;
                //    additionalInformations = fileToRead.Name;
                //    break;
                //}

                // Calculate real reading speed.
                float actualReadingTime = timer.ElapsedMilliseconds / 1000.0f;
                readingSpeed = Mathf.Lerp(readingSpeed, fileToRead.Length / actualReadingTime,0.5f);
            }
            Dictionary<Column, Data.Experience.Dataset.Data[]> dataByColumn = dataInfoByColumn.ToDictionary(t => t.Key, t => (from dataInfo in t.Value select dataByDataInfo[dataInfo]).ToArray());
            yield return Ninja.JumpToUnity;
            HandleError(loadingError, additionalInformations);

            // Create ColumnData.
            List<ColumnData> columnsData = new List<ColumnData>(visualisation.Columns.Count);
            progressStep = EPOCH_DATA / (visualisation.Columns.Count);
            Data.Patient[] patients = new Data.Patient[0];
            if (visualisation is MultiPatientsVisualisation) patients = (visualisation as MultiPatientsVisualisation).Patients.ToArray();
            else if (visualisation is SinglePatientVisualisation) patients = new Data.Patient[] { (visualisation as SinglePatientVisualisation).Patient };

            foreach (var column in visualisation.Columns)
            {
                // Update progressBar.
                loadingCircle.Set(loadingCircle.Progress + progressStep, "Epoching " + column.DataLabel);
                yield return Ninja.JumpBack;

                // Epoch data.
                try
                {
                    columnsData.Add(new ColumnData(patients, dataByColumn[column], column));
                }
                catch
                {
                    loadingError = LoadingErrorEnum.CanNotEpochingData;
                    additionalInformations = column.DataLabel.ToString();
                    break;
                }
            yield return Ninja.JumpToUnity;
            }
            yield return Ninja.JumpToUnity;
            HandleError(loadingError, additionalInformations);

            // Stadardize columns.
            loadingCircle.Text = "Standardizing columns";
            yield return Ninja.JumpBack;

            VisualisationData visualisationData = null;
            if (visualisation is MultiPatientsVisualisation) visualisationData = new MultiPatientsVisualisationData(patients, columnsData.ToArray());
            else if(visualisation is SinglePatientVisualisation) visualisationData = new SinglePatientVisualisationData(patients[0], columnsData.ToArray());
            try
            {
                visualisationData.StandardizeColumns();
            }
            catch
            {
                loadingError = LoadingErrorEnum.CanNotStandardizeColumns;
            }
            yield return Ninja.JumpToUnity;
            HandleError(loadingError, additionalInformations);
            
            if(visualisation is MultiPatientsVisualisation)
            {
                VisualisationLoaded.MP_Visualisation = visualisation as MultiPatientsVisualisation;
                VisualisationLoaded.MP_VisualisationData = visualisationData as MultiPatientsVisualisationData;
                VisualisationLoaded.MP_Columns = new bool[visualisationData.Columns.Count];
                loadingCircle.Set(1, "Finish");
                yield return Ninja.JumpBack;
                yield return Ninja.JumpToUnity;
                command.set_scene_data(visualisationData as MultiPatientsVisualisationData);
                // ROI
                ROIMenuController roiMenuController = GameObject.FindObjectOfType<ROIMenuController>();
                string projectsDirectory = ApplicationState.ProjectLoadedLocation;
                string projectName = ApplicationState.ProjectLoaded.Settings.Name;
                string currentVisualisation = visualisation.Name;
                roiMenuController.load_all_ROI(projectsDirectory + Path.DirectorySeparatorChar + projectName + Path.DirectorySeparatorChar + "ROI" + Path.DirectorySeparatorChar + currentVisualisation + Path.DirectorySeparatorChar);
            }
            else if(visualisation is SinglePatientVisualisation)
            {
                VisualisationLoaded.SP_Visualisation = visualisation as SinglePatientVisualisation;
                VisualisationLoaded.SP_VisualisationData = visualisationData as SinglePatientVisualisationData;
                VisualisationLoaded.SP_Columns = new bool[visualisationData.Columns.Count];
                loadingCircle.Set(1, "Finish");
                yield return Ninja.JumpBack;
                yield return Ninja.JumpToUnity;
                command.set_scene_data(visualisationData as SinglePatientVisualisationData);
            }
            command.set_scenes_visibility(visualisationData is SinglePatientVisualisationData, visualisationData is MultiPatientsVisualisationData);
            command.set_module_focus(true);
            loadingCircle.Close();
        }
        void LoadSPSceneFromMP(int idPatient)
        {
            UnityEngine.Debug.Log("LoadSPSceneFromMP");
            SinglePatientVisualisationData spVisuData = SinglePatientVisualisationData.LoadFromMultiPatients(VisualisationLoaded.MP_VisualisationData, idPatient);
            SinglePatientVisualisation spVisu = SinglePatientVisualisation.LoadFromMultiPatients(VisualisationLoaded.MP_Visualisation, idPatient);
            VisualisationLoaded.SP_VisualisationData = spVisuData;
            VisualisationLoaded.SP_Visualisation = spVisu;
            VisualisationLoaded.SP_Columns = VisualisationLoaded.MP_Columns;
        }
        void HandleError(LoadingErrorEnum error, string additionalInformations)
        {
            if (error != LoadingErrorEnum.None)
            {
                loadingCircle.Close();
                StopAllCoroutines();
                (Instantiate(popUpPrefab, Vector3.zero, Quaternion.identity, GameObject.Find("Windows").transform) as GameObject).GetComponent<PopUp>().Show(GetErrorMessage(error, additionalInformations));
            }
        }
        string GetErrorMessage(LoadingErrorEnum error, string additionalInformations)
        {
            string l_errorMessage = string.Empty;
            string l_firstPart = "The visualisation could not be loaded.\n";
            switch (error)
            {
                case LoadingErrorEnum.None: l_errorMessage = "None error detected."; return l_errorMessage;
                case LoadingErrorEnum.CanNotFindFilesToRead: l_errorMessage = " Can not find the files of the column <color=red>n°  "+additionalInformations+"</color>."; break;
                case LoadingErrorEnum.CanNotReadData: l_errorMessage = " Can not read \" <color=red>"+additionalInformations+"</color>.\""; break;
                case LoadingErrorEnum.CanNotEpochingData: l_errorMessage =  " Can not epoching data of the column \"<color=red>n°"+additionalInformations+"</color>.\""; break;
                case LoadingErrorEnum.CanNotStandardizeColumns: l_errorMessage = " Can not standardize columns."; break;
            }
            l_errorMessage = l_firstPart + l_errorMessage;
            return l_errorMessage;
        }
        #endregion
    }
}