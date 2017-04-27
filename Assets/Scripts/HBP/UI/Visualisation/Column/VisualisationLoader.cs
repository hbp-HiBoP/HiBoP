using UnityEngine;
using Tools.Unity;
using CielaSpike;
using System.Linq;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using HBP.Module3D;
using data = HBP.Data.Visualisation;
using HBP.Data.Experience.Dataset;

namespace HBP.UI.Visualisation
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
        public void Load(data.Visualisation visualisation)
        {
            this.StartCoroutineAsync(c_Load(visualisation, visualisation.GetType() == typeof(data.MultiPatientsVisualisation)));
        }
        #endregion
        #region Private Methods
        void Awake()
        {
            command = FindObjectOfType<HiBoP_3DModule_API>();
            UnityEngine.Debug.Log(command);
            command.LoadSPSceneFromMP.AddListener((i) => LoadSPSceneFromMP(i));
        }
        IEnumerator c_Load(data.Visualisation visualisation, bool MNI)
        {
            UnityEngine.Debug.Log("c_Load");

            // Initialize.
            LoadingErrorEnum loadingError = LoadingErrorEnum.None;
            string additionalInformations = string.Empty;

            // Open progress window.
            yield return Ninja.JumpToUnity;
            loadingCircle = (Instantiate(loadingCirclePrefab, Vector3.zero, Quaternion.identity, GameObject.Find("Windows").transform) as GameObject).GetComponent<LoadingCircle>();
            loadingCircle.transform.localPosition = new Vector3(0, 0, 0);
            loadingCircle.Set(0, "Finding files");
            yield return Ninja.JumpBack;

            // Find files to read.
            List<DataInfo> experienceDataToRead = new List<DataInfo>();
            Dictionary<int, int[]> dataByColumn = new Dictionary<int, int[]>();
            float progressStep = FIND_FILES_TO_READ / (visualisation.Columns.Count);
            for (int c = 0; c < visualisation.Columns.Count; c++)
            {
                yield return Ninja.JumpToUnity;
                loadingCircle.Progress = (c * progressStep);
                yield return Ninja.JumpBack;

                try
                {
                    DataInfo[] dataInfoForThisColumn = visualisation.GetDataInfo(visualisation.Columns[c]);
                    List<int> dataIndexForThisColumn = new List<int>();
                    for (int d = 0; d < dataInfoForThisColumn.Length; d++)
                    {
                        if (!experienceDataToRead.Contains(dataInfoForThisColumn[d]))
                        {
                            experienceDataToRead.Add(dataInfoForThisColumn[d]);
                            dataIndexForThisColumn.Add(experienceDataToRead.Count - 1);
                        }
                        else
                        {
                            dataIndexForThisColumn.Add(experienceDataToRead.FindIndex((x) => x == dataInfoForThisColumn[d]));
                        }
                    }
                    dataByColumn.Add(c, dataIndexForThisColumn.ToArray());
                }
                catch
                {
                    additionalInformations = c.ToString();
                    loadingError = LoadingErrorEnum.CanNotFindFilesToRead;
                    break;
                }
            }
            yield return Ninja.JumpToUnity;
            HandleError(loadingError, additionalInformations);
            yield return Ninja.JumpBack;

            // Read files.
            Data.Experience.Dataset.Data[] data = new Data.Experience.Dataset.Data[experienceDataToRead.Count];
            progressStep = READ_FILES / (experienceDataToRead.Count);
            long readingSpeed = 18000000;
            for (int i = 0; i < experienceDataToRead.Count; i++)
            {
                // Find file to read informations.
                System.IO.FileInfo fileToRead = new System.IO.FileInfo(experienceDataToRead[i].EEG);
                float assumedReadingTime = (float)fileToRead.Length / readingSpeed;

                // Update progressBar
                yield return Ninja.JumpToUnity;
                loadingCircle.Text = "Reading " + fileToRead.Name;
                loadingCircle.ChangePercentage(FIND_FILES_TO_READ + (i) * progressStep, FIND_FILES_TO_READ + (i + 1) * progressStep, assumedReadingTime);
                yield return Ninja.JumpBack;

                // Read Data.
                Stopwatch timer = new Stopwatch();
                try
                {
                    timer.Start();
                    data[i] = new Data.Experience.Dataset.Data(experienceDataToRead[i], MNI);
                    timer.Stop();
                }
                catch
                {
                    loadingError = LoadingErrorEnum.CanNotReadData;
                    additionalInformations = fileToRead.Name;
                    break;
                }

                // Calculate real reading speed.
                float actualReadingTime = timer.ElapsedMilliseconds / 1000.0f;
                if (i == 0)
                {
                    readingSpeed = (long)(fileToRead.Length / actualReadingTime);
                }
                else
                {
                    readingSpeed = (readingSpeed + (long)(fileToRead.Length / actualReadingTime)) / 2;
                }
            }
            yield return Ninja.JumpToUnity;
            HandleError(loadingError, additionalInformations);
            yield return Ninja.JumpBack;

            // Create ColumnData.
            data.ColumnData[] columnsData = new data.ColumnData[visualisation.Columns.Count];
            progressStep = EPOCH_DATA / (visualisation.Columns.Count);
            for (int c = 0; c < visualisation.Columns.Count; c++)
            {
                // Update progressBar.
                yield return Ninja.JumpToUnity;
                loadingCircle.Set(FIND_FILES_TO_READ + READ_FILES + c * progressStep, "Epoching column n°" + c);
                yield return Ninja.JumpBack;

                // Epoch data.
                Data.Experience.Dataset.Data[] dataForThisColumn = new Data.Experience.Dataset.Data[dataByColumn[c].Length];
                dataForThisColumn = data.Where((d, index) => dataByColumn[c].Contains(index)).ToArray();
                Data.Patient[] patients = new Data.Patient[0];
                if (MNI)
                {
                    data.MultiPatientsVisualisation MPvisu = visualisation as data.MultiPatientsVisualisation;
                    patients = MPvisu.Patients.ToArray();
                }
                else
                {
                    data.SinglePatientVisualisation SPvisu = visualisation as data.SinglePatientVisualisation;
                    patients = new Data.Patient[] { SPvisu.Patient };
                }
                try
                {
                    columnsData[c] = new data.ColumnData(patients, dataForThisColumn, visualisation.Columns[c]);
                }
                catch
                {
                    loadingError = LoadingErrorEnum.CanNotEpochingData;
                    additionalInformations = c.ToString();
                    break;
                }
            }
            yield return Ninja.JumpToUnity;
            HandleError(loadingError, additionalInformations);

            // Stadardize columns.
            loadingCircle.Set(FIND_FILES_TO_READ + READ_FILES + EPOCH_DATA, "Standardizing columns");
            yield return Ninja.JumpBack;

            if (MNI)
            {
                data.MultiPatientsVisualisation MPvisu = visualisation as data.MultiPatientsVisualisation;
                data.MultiPatientsVisualisationData visualisationData = new data.MultiPatientsVisualisationData(MPvisu.Patients.ToArray(), columnsData);
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
                VisualisationLoaded.MP_Visualisation = MPvisu;
                VisualisationLoaded.MP_VisualisationData = visualisationData;
                VisualisationLoaded.MP_Columns = new bool[visualisationData.Columns.Count];

                // Set scene.
                loadingCircle.Set(1, "Finish");
                yield return Ninja.JumpBack;
                yield return Ninja.JumpToUnity;
                command.LoadData(visualisationData);
            }
            else
            {
                data.SinglePatientVisualisation SPvisu = visualisation as data.SinglePatientVisualisation;
                data.SinglePatientVisualisationData visualisationData = new data.SinglePatientVisualisationData(SPvisu.Patient, columnsData);
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

                VisualisationLoaded.SP_Visualisation = SPvisu;
                VisualisationLoaded.SP_VisualisationData = visualisationData;
                VisualisationLoaded.SP_Columns = new bool[visualisationData.Columns.Count];

                // Set scene.
                loadingCircle.Set(1, "Finish");
                yield return Ninja.JumpBack;
                yield return Ninja.JumpToUnity;
                command.LoadData(visualisationData);
            }
            command.SetScenesVisibility(!MNI, MNI);
            command.SetModuleFocus(true);
            loadingCircle.Close();
        }
        void LoadSPSceneFromMP(int idPatient)
        {
            UnityEngine.Debug.Log("LoadSPSceneFromMP");
            data.SinglePatientVisualisationData spVisuData = data.SinglePatientVisualisationData.LoadFromMultiPatients(VisualisationLoaded.MP_VisualisationData, idPatient);
            data.SinglePatientVisualisation spVisu = data.SinglePatientVisualisation.LoadFromMultiPatients(VisualisationLoaded.MP_Visualisation, idPatient);
            VisualisationLoaded.SP_VisualisationData = spVisuData;
            VisualisationLoaded.SP_Visualisation = spVisu;
            VisualisationLoaded.SP_Columns = VisualisationLoaded.MP_Columns;
            // TODO : Create single patient visualisation and send it as a new visualisation
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