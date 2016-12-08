using UnityEngine;
using Tools.Unity;
using CielaSpike;
using dia = System.Diagnostics;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using HBP.Data.Experience.Dataset;
using HBP.Data.Visualisation;

namespace HBP.UI
{
    public class VisualisationLoader : MonoBehaviour
    {
        #region Properties
        VISU3D.HBP_3DModule_Command command;

        [SerializeField]
        GameObject progressWindowPrefab;
        ProgressWindow progressWindow;

        [SerializeField]
        GameObject popUpPrefab;

        const float FIND_FILES_TO_READ = 0.025f;
        const float READ_FILES = 0.8f;
        const float EPOCH_DATA = 0.025f;
        const float STANDARDIZE_COLUMNS = 0.15f;    
        enum LoadErrorTypeEnum { None, CanNotFindFilesToRead, CanNotReadData, CanNotEpochingData, CanNotStandardizeColumns };
        #endregion

        #region Public Methods
        public void Load(SinglePatientVisualisation visualisation)
        {
            this.StartCoroutineAsync(c_Load(visualisation));
        }
        public void Load(MultiPatientsVisualisation visualisation)
        {
            this.StartCoroutineAsync(c_Load(visualisation));
        }
        #endregion

        #region Coroutine
        IEnumerator c_Load(SinglePatientVisualisation visualisation)
        {
            LoadErrorTypeEnum loadingError = LoadErrorTypeEnum.None;
            string additionalInformations = string.Empty;

            // Start.
            yield return Ninja.JumpToUnity;
            GameObject progressWindowObj = GameObject.Instantiate(progressWindowPrefab, GameObject.Find("Windows").transform) as GameObject;
            progressWindowObj.transform.localPosition = new Vector3(0, 0, 0);
            progressWindow = progressWindowObj.GetComponent<ProgressWindow>();
            progressWindow.Set(0, "Finding files");
            yield return Ninja.JumpBack;

            // Finding files to read.
            Dictionary<Column, DataInfo> l_columnToExperienceData = new Dictionary<Column, DataInfo>();
            float l_ratio = FIND_FILES_TO_READ / (visualisation.Columns.Count);
            for (int i = 0; i < visualisation.Columns.Count; i++)
            {
                try
                {
                    l_columnToExperienceData.Add(visualisation.Columns[i], visualisation.GetDataInfo(visualisation.Columns[i])[0]);
                }
                catch
                {
                    additionalInformations = i.ToString();
                    loadingError = LoadErrorTypeEnum.CanNotFindFilesToRead;
                    break;
                }
                yield return Ninja.JumpToUnity;
                progressWindow.Set(i * l_ratio);
                yield return Ninja.JumpBack;
            }
            yield return Ninja.JumpToUnity;
            HandleError(loadingError, additionalInformations);
            yield return Ninja.JumpBack;

            // Reading files.
            DataInfo[] l_filesToRead = new List<DataInfo>(l_columnToExperienceData.Values).Distinct().ToArray();
            l_ratio = READ_FILES / (l_filesToRead.Length);
            long l_sizePerSecond = 18000000;
            Dictionary<DataInfo, Data.Experience.Dataset.Data> l_experienceDataToExperienceDataData = new Dictionary<DataInfo, Data.Experience.Dataset.Data>();
            for (int i = 0; i < l_filesToRead.Length; i++)
            {
                // Find file to read informations.
                System.IO.FileInfo l_file = new System.IO.FileInfo(l_filesToRead[i].EEG);
                float l_readingTime = (float)l_file.Length / l_sizePerSecond;
                string l_name = new System.IO.FileInfo(l_filesToRead[i].EEG).Name;

                // Update progressBar
                yield return Ninja.JumpToUnity;
                progressWindow.Set("Reading " + l_name);
                progressWindow.ChangePercentageInSeconds(FIND_FILES_TO_READ + (i) * l_ratio, FIND_FILES_TO_READ + (i + 1) * l_ratio, l_readingTime);
                yield return Ninja.JumpBack;

                // Read Data.
                dia.Stopwatch l_timer = new dia.Stopwatch();
                l_timer.Start();
                try
                {
                    l_experienceDataToExperienceDataData.Add(l_filesToRead[i], new Data.Experience.Dataset.Data(l_filesToRead[i], false));
                }
                catch
                {
                    loadingError = LoadErrorTypeEnum.CanNotReadData;
                    additionalInformations = l_name;
                    break;
                }
                l_timer.Stop();

                // Calculate real reading speed.
                float l_realReadingTime = l_timer.ElapsedMilliseconds / 1000.0f;
                if (i == 0)
                {
                    l_sizePerSecond = (long)(l_file.Length / l_realReadingTime);
                }
                else
                {
                    l_sizePerSecond = (l_sizePerSecond + (long)(l_file.Length / l_realReadingTime)) / 2;
                }
            }
            yield return Ninja.JumpToUnity;
            HandleError(loadingError, additionalInformations);
            yield return Ninja.JumpBack;

            // Create a dictionary to find data for each column.
            Dictionary<Column, Data.Experience.Dataset.Data> l_columnToExperienceDataData = new Dictionary<Column, Data.Experience.Dataset.Data>();
            foreach(Column col in l_columnToExperienceData.Keys)
            {
                l_columnToExperienceDataData.Add(col, l_experienceDataToExperienceDataData[l_columnToExperienceData[col]]);
            }

            // Epoching data.
            List<ColumnData> l_columns = new List<ColumnData>();
            l_ratio = EPOCH_DATA / (visualisation.Columns.Count);
            for (int i = 0; i < visualisation.Columns.Count; i++)
            {
                yield return Ninja.JumpToUnity;
                progressWindow.Set(FIND_FILES_TO_READ + READ_FILES + i * l_ratio, "Epoching column n°" + i);
                yield return Ninja.JumpBack;

                Data.Experience.Dataset.Data l_expData = l_columnToExperienceDataData[visualisation.Columns[i]];
                try
                {
                    l_columns.Add(new ColumnData(l_expData, visualisation.Columns[i]));
                }
                catch
                {
                    loadingError = LoadErrorTypeEnum.CanNotEpochingData;
                    additionalInformations = i.ToString();
                    break;
                }
            }
            yield return Ninja.JumpToUnity;
            HandleError(loadingError, additionalInformations);

            // Stadardize columns.
            progressWindow.Set(FIND_FILES_TO_READ + READ_FILES + EPOCH_DATA, "Standardizing columns");
            yield return Ninja.JumpBack;

            SinglePatientVisualisationData l_visuData = new SinglePatientVisualisationData(visualisation.Patient, l_columns);
            try
            {
                l_visuData.StandardizeColumns();
            }
            catch
            {
                loadingError = LoadErrorTypeEnum.CanNotStandardizeColumns;
            }
            yield return Ninja.JumpToUnity;
            HandleError(loadingError, additionalInformations);
      
            VisualisationLoaded.SP_Visualisation = visualisation;
            VisualisationLoaded.SP_VisualisationData = l_visuData;
            VisualisationLoaded.SP_Columns = new bool[l_visuData.Columns.Count];

            // Set scene.
            progressWindow.Set(1, "Finish");
            yield return Ninja.JumpBack;
            yield return Ninja.JumpToUnity;
            command.setSceneData(l_visuData);
            command.setScenesVisibility(true, false);
            command.setModuleFocusState(true);
            progressWindow.Close();
        }
        IEnumerator c_Load(MultiPatientsVisualisation visualisation)
        {
            LoadErrorTypeEnum l_loadingState = LoadErrorTypeEnum.None;
            string additionalInformations = string.Empty;

            // Start.
            yield return Ninja.JumpToUnity;
            GameObject progressWindowObj = GameObject.Instantiate(progressWindowPrefab, GameObject.Find("Windows").transform) as GameObject;
            progressWindowObj.transform.localPosition = new Vector3(0, 0, 0);
            progressWindow = progressWindowObj.GetComponent<ProgressWindow>(); progressWindow.Set(0, "Finding files");
            yield return Ninja.JumpBack;

            // Finding files to read.
            List<DataInfo> l_expDataToRead = new List<DataInfo>();
            List<Data.Experience.Dataset.Data> l_expDataData = new List<Data.Experience.Dataset.Data>();
            Dictionary<int, int[]> l_columnToData = new Dictionary<int, int[]>();
            float l_ratio = FIND_FILES_TO_READ / (visualisation.Columns.Count);
            for (int i = 0; i < visualisation.Columns.Count; i++)
            {
                yield return Ninja.JumpToUnity;
                progressWindow.Set(i * l_ratio);
                yield return Ninja.JumpBack;

                try
                {
                    DataInfo[] l_expData = visualisation.GetDataInfo(visualisation.Columns[i]);
                    List<int> expDataInColumn = new List<int>();
                    for (int n = 0; n < l_expData.Length; n++)
                    {
                        if (!l_expDataToRead.Contains(l_expData[n]))
                        {
                            l_expDataToRead.Add(l_expData[n]);
                            expDataInColumn.Add(l_expDataToRead.Count - 1);
                        }
                        else
                        {
                            expDataInColumn.Add(l_expDataToRead.FindIndex((x) => x == l_expData[n]));
                        }
                    }
                    l_columnToData.Add(i, expDataInColumn.ToArray());
                }
                catch
                {
                    additionalInformations = i.ToString();
                    l_loadingState = LoadErrorTypeEnum.CanNotFindFilesToRead;
                    break;
                }
            }
            yield return Ninja.JumpToUnity;
            HandleError(l_loadingState, additionalInformations);
            yield return Ninja.JumpBack;

            // Reading files.
            l_ratio = READ_FILES / (l_expDataToRead.Count);
            long l_sizePerSecond = 18000000;
            for (int i = 0; i < l_expDataToRead.Count; i++)
            {
                // Find file to read informations.
                System.IO.FileInfo l_file = new System.IO.FileInfo(l_expDataToRead[i].EEG);
                float l_readingTime = (float)l_file.Length / l_sizePerSecond;
                string l_name = new System.IO.FileInfo(l_expDataToRead[i].EEG).Name;

                // Update progressBar
                yield return Ninja.JumpToUnity;
                progressWindow.Set("Reading " + l_name);
                progressWindow.ChangePercentageInSeconds(FIND_FILES_TO_READ + (i) * l_ratio, FIND_FILES_TO_READ + (i + 1) * l_ratio, l_readingTime);
                yield return Ninja.JumpBack;

                // Read Data.
                dia.Stopwatch l_timer = new dia.Stopwatch();
                l_timer.Start();
                try
                {
                    l_expDataData.Add(new Data.Experience.Dataset.Data(l_expDataToRead[i], true));
                }
                catch
                {
                    l_loadingState = LoadErrorTypeEnum.CanNotReadData;
                    additionalInformations = l_name;
                    break;
                }
                 l_timer.Stop();

                // Calculate real reading speed.
                float l_realReadingTime = l_timer.ElapsedMilliseconds / 1000.0f;
                if (i == 0)
                {
                    l_sizePerSecond = (long)(l_file.Length / l_realReadingTime);
                }
                else
                {
                    l_sizePerSecond = (l_sizePerSecond + (long)(l_file.Length / l_realReadingTime)) / 2;
                }
            }
            yield return Ninja.JumpToUnity;
            HandleError(l_loadingState, additionalInformations);
            yield return Ninja.JumpBack;

            // Epoching data.
            List<ColumnData> l_columns = new List<ColumnData>();
            l_ratio = EPOCH_DATA / (visualisation.Columns.Count);
            for (int i = 0; i < visualisation.Columns.Count; i++)
            {
                yield return Ninja.JumpToUnity;
                progressWindow.Set(FIND_FILES_TO_READ + READ_FILES + i * l_ratio, "Epoching column n°" + i);
                yield return Ninja.JumpBack;

                List<Data.Experience.Dataset.Data> l_expData = new List<Data.Experience.Dataset.Data>();
                foreach (int index in l_columnToData[i])
                {
                    l_expData.Add(l_expDataData[index]);
                }
                try
                {
                    l_columns.Add(new ColumnData(visualisation.Patients.ToArray(), l_expData.ToArray(), visualisation.Columns[i]));
                }
                catch
                {
                    l_loadingState = LoadErrorTypeEnum.CanNotEpochingData;
                    additionalInformations = i.ToString();
                    break;
                }
            }
            yield return Ninja.JumpToUnity;
            HandleError(l_loadingState, additionalInformations);

            // Stadardize columns.
            progressWindow.Set(FIND_FILES_TO_READ + READ_FILES + EPOCH_DATA, "Standardizing columns");
            yield return Ninja.JumpBack;

            MultiPatientsVisualisationData l_visuData = new MultiPatientsVisualisationData(visualisation.Patients.ToList(), l_columns);
            try
            {
                l_visuData.StandardizeColumns();
            }
            catch
            {
                l_loadingState = LoadErrorTypeEnum.CanNotStandardizeColumns;
            }
            yield return Ninja.JumpToUnity;
            HandleError(l_loadingState, additionalInformations);
            VisualisationLoaded.MP_Visualisation = visualisation;
            VisualisationLoaded.MP_VisualisationData = l_visuData;
            VisualisationLoaded.MP_Columns = new bool[l_visuData.Columns.Count];

            // Set scene.
            progressWindow.Set(1, "Finish");
            yield return Ninja.JumpBack;
            yield return Ninja.JumpToUnity;
            command.setSceneData(l_visuData);
            command.setScenesVisibility(false, true);
            command.setModuleFocusState(true);
            progressWindow.Close();
        }
        #endregion

        #region Private Methods
        void Awake()
        {
            command = FindObjectOfType<VISU3D.HBP_3DModule_Command>();
            command.LoadSpSceneFromMP.AddListener((i) => LoadSPSceneFromMP(i));
        }        
        void LoadSPSceneFromMP(int idPatient)
        {
            SinglePatientVisualisationData spVisuData = SinglePatientVisualisationData.LoadFromMultiPatients(VisualisationLoaded.MP_VisualisationData, idPatient);
            SinglePatientVisualisation spVisu = SinglePatientVisualisation.LoadFromMultiPatients(VisualisationLoaded.MP_Visualisation, idPatient);
            VisualisationLoaded.SP_VisualisationData = spVisuData;
            VisualisationLoaded.SP_Visualisation = spVisu;
            VisualisationLoaded.SP_Columns = VisualisationLoaded.MP_Columns;
        }
        void HandleError(LoadErrorTypeEnum error, string additionalInformations)
        {
            if (error != LoadErrorTypeEnum.None)
            {
                progressWindow.Close();
                StopAllCoroutines();
                GameObject popUpobj = GameObject.Instantiate(popUpPrefab, GameObject.Find("UI").transform) as GameObject;
                popUpobj.transform.localPosition = new Vector3(0, 0, 0);
                popUpobj.GetComponent<PopUp>().Show(GetErrorMessage(error, additionalInformations));
            }
        }
        string GetErrorMessage(LoadErrorTypeEnum error, string additionalInformations)
        {
            string l_errorMessage = string.Empty;
            string l_firstPart = "The visualisation could not be loaded.\n";
            switch (error)
            {
                case LoadErrorTypeEnum.None: l_errorMessage = "None error detected."; return l_errorMessage;
                case LoadErrorTypeEnum.CanNotFindFilesToRead: l_errorMessage = " Can not find the files of the column <color=red>n°  "+additionalInformations+"</color>."; break;
                case LoadErrorTypeEnum.CanNotReadData: l_errorMessage = " Can not read \" <color=red>"+additionalInformations+"</color>.\""; break;
                case LoadErrorTypeEnum.CanNotEpochingData: l_errorMessage =  " Can not epoching data of the column \"<color=red>n°"+additionalInformations+"</color>.\""; break;
                case LoadErrorTypeEnum.CanNotStandardizeColumns: l_errorMessage = " Can not standardize columns."; break;
            }
            l_errorMessage = l_firstPart + l_errorMessage;
            return l_errorMessage;
        }
        #endregion
    }
}

