using UnityEngine;
using UnityEngine.Events;
using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using CielaSpike;
using HBP.Data.Experience.Dataset;

namespace HBP.Data.Visualization
{
    /**
    * \class Visualization
    * \author Adrien Gannerie
    * \version 2.0
    * \date 09 mai 2017
    * \brief 3D brain vizualisation.
    * 
    * \details Define a 3D brain visualization and contains:
    * 
    *   - \a ID.
    *   - \a Name.
    *   - \a Configuration.
    *   - \a Columns.   
    */
    [DataContract]
    public abstract class Visualization :  ICloneable , ICopiable
    {
        #region Properties
        /// <summary>
        /// Unique ID.
        /// </summary>
        [DataMember(Order = 1)]     
        public string ID { get; private set; }

        /// <summary>
        /// Name of the visualization.
        /// </summary>
        [DataMember(Order = 2)]
        public string Name { get; set; }

        /// <summary>
        /// Configuration of the visualization.
        /// </summary>
        [DataMember(Order = 4)]
        public VisualizationConfiguration Configuration { get; set; }

        /// <summary>
        /// Columns of the visualization.
        /// </summary>
        [DataMember(Order = 5)]
        public List<Column> Columns { get; set; }

        /// <summary>
        /// Test if the visualization is visualizable;
        /// </summary>
        public virtual bool IsVisualizable
        {
            get { return Columns.Count > 0; }
        }

        /// <summary>
        /// Event called when changing loading progress.
        /// <para>Argument 1 : Visualization progress.</para>
        /// <para>Argument 2 : Time to reach the progress.</para>
        /// <para>Argument 3 : Message.</para>
        /// </summary>
        public GenericEvent<float, float, string> OnChangeLoadingProgress { get; set; }

        /// <summary>
        /// Event called when an error occurs.
        /// <para> Argument 1 : Error message.</para>
        /// </summary>
        public GenericEvent<string> OnErrorOccur { get; set; }

        enum LoadingErrorType { None, CanNotFindFilesToRead, CanNotReadData, CanNotEpochingData, CanNotStandardizeColumns };
        const float FIND_FILES_TO_READ_PROGRESS = 0.025f;
        const float READ_FILES_PROGRESS = 0.8f;
        const float EPOCH_DATA_PROGRESS = 0.025f;
        const float STANDARDIZE_COLUMNS_PROGRESS = 0.15f;
        Coroutine m_LoadingCoroutine;
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new visualization instance.
        /// </summary>
        /// <param name="name">Name of the visualization.</param>
        /// <param name="columns">Columns of the visualization.</param>
        /// <param name="id">Unique ID.</param>
        protected Visualization(string name, IEnumerable<Column> columns, string id)
        {
            ID = id;
            Name = name;
            Columns = columns.ToList();
        }
        /// <summary>
        /// Create a new visualization instance.
        /// </summary>
        /// <param name="name">Name of the visualization.</param>
        /// <param name="columns">Columns of the visualization.</param>
        protected Visualization(string name, IEnumerable<Column> columns) : this(name,columns,Guid.NewGuid().ToString())
        {
        }
        /// <summary>
        /// Create a new visualization instance with default value.
        /// </summary>
        protected Visualization() : this("Unknown",new List<Column>())
        {
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Load the visualization.
        /// </summary>
        public virtual void Load()
        {
            ApplicationState.CoroutineManager.StartCoroutineAsync(c_Load());
        }
        /// <summary>
        /// Unload the visualization.
        /// </summary>
        public virtual void Unload()
        {
            foreach (Column column in Columns) column.Unload();
        }
        /// <summary>
        /// Swap two columns by index.
        /// </summary>
        /// <param name="index1">Index of the first column to swap.</param>
        /// <param name="index2">Index of the second column to swap.</param>
        public void SwapColumns(int index1,int index2)
        {
            Column tmp = Columns[index1];
            Columns[index1] = Columns[index2];
            Columns[index2] = tmp;
        }
        /// <summary>
        /// Get the DataInfo of the column.
        /// </summary>
        /// <param name="column">Column</param>
        /// <returns>DataInfo of the column.</returns>
        public abstract IEnumerable<DataInfo> GetDataInfo(Column column);
        public abstract IEnumerable<Patient> GetPatients();
        #endregion

        #region Operators
        /// <summary>
        /// Clone this instance.
        /// </summary>
        /// <returns>Clone of this instance.</returns>
        public abstract object Clone();
        /// <summary>
        /// Copy an instance in this instance.
        /// </summary>
        /// <param name="copy">Instance to copy.</param>
        public virtual void Copy(object copy)
        {
            Visualization visualization = copy as Visualization;
            Name = visualization.Name;
            Columns = visualization.Columns;
            ID = visualization.ID;
            Configuration = visualization.Configuration;
        }
        #endregion

        #region Private Methods
        IEnumerator c_Load()
        {
            // Initialisation.
            LoadingErrorType loadingError = LoadingErrorType.None;
            string additionalInformations = string.Empty;
            float progress = 0.0f;

            // Find files to read.
            Dictionary<Column, DataInfo[]> dataInfoByColumn = new Dictionary<Column, DataInfo[]>();
            float progressStep = FIND_FILES_TO_READ_PROGRESS / (Columns.Count);
            foreach (var column in Columns)
            {
                progress += progressStep;
                yield return Ninja.JumpToUnity;
                OnChangeLoadingProgress.Invoke(progress, 0.0f, "Finding files to read.");
                yield return Ninja.JumpBack;
                try
                {
                    dataInfoByColumn.Add(column, GetDataInfo(column).ToArray());
                }
                catch
                {
                    additionalInformations = (column.DataLabel).ToString();
                    loadingError = LoadingErrorType.CanNotFindFilesToRead;
                    break;
                }
                yield return Ninja.JumpToUnity;
            }
            Dictionary<DataInfo, Experience.Dataset.Data> dataByDataInfo = (from dataInfos in dataInfoByColumn.Values from dataInfo in dataInfos select dataInfo).Distinct().ToDictionary(t => t, t => new Data.Experience.Dataset.Data());
            yield return Ninja.JumpToUnity;
            HandleError(loadingError, additionalInformations);
            yield return Ninja.JumpBack;

            // Read files.
            Stopwatch timer = new Stopwatch();
            progressStep = READ_FILES_PROGRESS / (dataByDataInfo.Count);
            float readingSpeed = 18000000;
            List<DataInfo> dataInfoToRead = dataByDataInfo.Keys.ToList();
            foreach (var dataInfo in dataInfoToRead)
            {
                // Find file to read informations.
                FileInfo fileToRead = new FileInfo(dataInfo.EEG);
                float assumedReadingTime = fileToRead.Length / readingSpeed;

                // Update progressBar
                progress += progressStep;
                yield return Ninja.JumpToUnity;
                OnChangeLoadingProgress.Invoke(progress, assumedReadingTime, "Reading" + fileToRead.Name);
                yield return Ninja.JumpBack;

                // Read Data.
                try
                {
                    timer.Start();
                    dataByDataInfo[dataInfo] = new Experience.Dataset.Data(dataInfo);
                    timer.Stop();
                }
                catch
                {
                    loadingError = LoadingErrorType.CanNotReadData;
                    additionalInformations = fileToRead.Name;
                    break;
                }

                // Calculate real reading speed.
                float actualReadingTime = timer.ElapsedMilliseconds / 1000.0f;
                readingSpeed = Mathf.Lerp(readingSpeed, fileToRead.Length / actualReadingTime, 0.5f);
            }
            Dictionary<Column, Experience.Dataset.Data[]> dataByColumn = dataInfoByColumn.ToDictionary(t => t.Key, t => (from dataInfo in t.Value select dataByDataInfo[dataInfo]).ToArray());
            yield return Ninja.JumpToUnity;
            HandleError(loadingError, additionalInformations);

            /// Load Column.
            foreach (Column column in Columns)
            {
                column.Load(dataByColumn[column]);
            }
        }
        void HandleError(LoadingErrorType error, string additionalInformations)
        {
            if (error != LoadingErrorType.None)
            {
                ApplicationState.CoroutineManager.StopCoroutine(m_LoadingCoroutine);
                OnErrorOccur.Invoke(GetErrorMessage(error, additionalInformations));
            }
        }
        string GetErrorMessage(LoadingErrorType error, string additionalInformations)
        {
            string l_errorMessage = string.Empty;
            string l_firstPart = "The visualization could not be loaded.\n";
            switch (error)
            {
                case LoadingErrorType.None: l_errorMessage = "None error detected."; return l_errorMessage;
                case LoadingErrorType.CanNotFindFilesToRead: l_errorMessage = " Can not find the files of the column <color=red>n°  " + additionalInformations + "</color>."; break;
                case LoadingErrorType.CanNotReadData: l_errorMessage = " Can not read \" <color=red>" + additionalInformations + "</color>.\""; break;
                case LoadingErrorType.CanNotEpochingData: l_errorMessage = " Can not epoching data of the column \"<color=red>n°" + additionalInformations + "</color>.\""; break;
                case LoadingErrorType.CanNotStandardizeColumns: l_errorMessage = " Can not standardize columns."; break;
            }
            l_errorMessage = l_firstPart + l_errorMessage;
            return l_errorMessage;
        }
        protected void AddPatientConfiguration(Patient patient)
        {
            patient.Brain.LoadImplantation(true);
            foreach (Column column in Columns)
            {
                if (!column.Configuration.ConfigurationByPatient.ContainsKey(patient)) column.Configuration.ConfigurationByPatient.Add(patient, new PatientConfiguration(Configuration.Color));
                PatientConfiguration patientConfiguration = column.Configuration.ConfigurationByPatient[patient];
                foreach (Anatomy.Electrode electrode in patient.Brain.Implantation.Electrodes)
                {
                    if (!patientConfiguration.ConfigurationByElectrode.ContainsKey(electrode)) patientConfiguration.ConfigurationByElectrode.Add(electrode, new ElectrodeConfiguration(patientConfiguration.Color));
                    ElectrodeConfiguration electrodeConfiguration = patientConfiguration.ConfigurationByElectrode[electrode];
                    foreach (Anatomy.Site site in electrode.Sites)
                    {
                        if (!electrodeConfiguration.ConfigurationBySite.ContainsKey(site)) electrodeConfiguration.ConfigurationBySite.Add(site, new SiteConfiguration(electrodeConfiguration.Color));
                    }
                }
            }
            patient.Brain.UnloadImplantation();
        }
        protected void RemovePatientConfiguration(Patient patient)
        {
            foreach (Column column in Columns) column.Configuration.ConfigurationByPatient.Remove(patient);
        }
        #endregion
    }
}