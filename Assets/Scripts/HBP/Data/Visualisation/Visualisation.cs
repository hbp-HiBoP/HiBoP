using System;
using System.Diagnostics;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using CielaSpike;
using UnityEngine.Events;
using HBP.Data.Experience.Dataset;

namespace HBP.Data.Visualisation
{
    public class OnChangeProgressionEvent : UnityEvent<float,float> { };
    public class OnChangeMessageEvent : UnityEvent<string> { };

    /**
    * \class Visualisation
    * \author Adrien Gannerie
    * \version 1.0
    * \date 12 janvier 2017
    * \brief Visualisation is a class which define a brain visualisation.
    * 
    * \details Visualisation is a ckass which define a brain visualiation and contains:
    *   - \a Name of the visualisation.
    *   - \a Unique ID.
    *   - \a Columns of the visualisation.   
    */
    [DataContract]
    public abstract class Visualisation :  ICloneable , ICopiable
    {
        #region Properties
        [DataMember(Order = 1)]     
        /// <summary>
        /// Unique ID.
        /// </summary>
        public string ID { get; private set; }
        [DataMember(Order = 2)]
        /// <summary>
        /// Name of the visualisation.
        /// </summary>
        public string Name { get; set; }
        [DataMember(Order = 4)]
        /// <summary>
        /// Configuration of the visualisation;
        /// </summary>
        public VisualisationConfiguration Configuration { get; set; }
        [DataMember(Order = 5)]
        /// <summary>
        /// Columns of the visualisation.
        /// </summary>
        public List<Column> Columns { get; set; }
        /// <summary>
        /// Test if the visualisation est visualisable;
        /// </summary>
        public abstract bool IsVisualisable { get; }

        // Loading
        enum LoadingErrorEnum { None, CanNotFindFilesToRead, CanNotReadData, CanNotEpochingData, CanNotStandardizeColumns };
        const float FIND_FILES_TO_READ = 0.025f;
        const float READ_FILES = 0.8f;
        const float EPOCH_DATA = 0.025f;
        const float STANDARDIZE_COLUMNS = 0.15f;
        public OnChangeProgressionEvent OnChangeLoadingProgress { get; set; }
        public OnChangeMessageEvent OnChangeLoadingMessage { get; set; }
        public OnChangeMessageEvent OnError { get; set; }
        #endregion

        #region Constructor
        /// <summary>
        /// Create a new visualisation instance.
        /// </summary>
        /// <param name="name">Name of the visualisation.</param>
        /// <param name="columns">Columns of the visualisation.</param>
        /// <param name="id">Unique ID.</param>
        protected Visualisation(string name, IEnumerable<Column> columns, string id)
        {
            ID = id;
            Name = name;
            Columns = columns.ToList();
        }
        /// <summary>
        /// Create a new visualisation instance.
        /// </summary>
        /// <param name="name">Name of the visualisation.</param>
        /// <param name="columns">Columns of the visualisation.</param>
        protected Visualisation(string name, IEnumerable<Column> columns) : this(name,columns,Guid.NewGuid().ToString())
        {
        }
        /// <summary>
        /// Create a new visualisation instance with default value.
        /// </summary>
        protected Visualisation() : this("New visualisation",new List<Column>())
        {
        }
        #endregion

        #region Public Methods
        public void Load()
        {
            this.StartCoroutineAsyn(c_Load());
        }
        public void Dispose()
        {

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
        public abstract DataInfo[] GetDataInfo(Column column);
        /// <summary>
        /// Copy a Visualisation instance in this visualisation.
        /// </summary>
        /// <param name="copy">Instance to copy.</param>
        public virtual void Copy(object copy)
        {
            Visualisation visualisation = copy as Visualisation;
            Name = visualisation.Name;
            Columns = visualisation.Columns;
            ID = visualisation.ID;
            Configuration = visualisation.Configuration;
        }
        #endregion

        #region Operators
        /// <summary>
        /// Clone this instance.
        /// </summary>
        /// <returns>Clone of this instance.</returns>
        public abstract object Clone();
        #endregion

        #region Private Methods
        IEnumerator c_Load()
        {
            // Initialisation.
            LoadingErrorEnum loadingError = LoadingErrorEnum.None;
            string additionalInformations = string.Empty;
            float progress = 0;

            // Find files to read.
            yield return Ninja.JumpToUnity;
            OnChangeLoadingMessage.Invoke("Finding files to read.");
            yield return Ninja.JumpBack;
            List<DataInfo> experienceDataToRead = new List<DataInfo>();
            Dictionary<int, int[]> dataByColumn = new Dictionary<int, int[]>();
            float progressStep = FIND_FILES_TO_READ / (Columns.Count);
            for (int c = 0; c < Columns.Count; c++)
            {
                progress += progressStep;
                yield return Ninja.JumpToUnity;
                OnChangeLoadingProgress.Invoke(progress,0.0f);
                yield return Ninja.JumpBack;

                try
                {
                    DataInfo[] dataInfoForThisColumn = GetDataInfo(Columns[c]);
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
            Experience.Dataset.Data[] data = new Experience.Dataset.Data[experienceDataToRead.Count];
            progressStep = READ_FILES / (experienceDataToRead.Count);
            progress += progressStep;
            long readingSpeed = 18000000;
            for (int i = 0; i < experienceDataToRead.Count; i++)
            {
                // Find file to read informations.
                System.IO.FileInfo fileToRead = new System.IO.FileInfo(experienceDataToRead[i].EEG);
                float assumedReadingTime = (float)fileToRead.Length / readingSpeed;

                // Update progressBar
                yield return Ninja.JumpToUnity;
                OnChangeLoadingMessage.Invoke("Reading" + fileToRead.Name);
                progress += progressStep;
                OnChangeLoadingProgress.Invoke(progress, assumedReadingTime);
                yield return Ninja.JumpBack;

                // Read Data.
                Stopwatch timer = new Stopwatch();
                try
                {
                    timer.Start();
                    data[i] = new Experience.Dataset.Data(experienceDataToRead[i], MNI);
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
        }
        void HandleError(LoadingErrorEnum error, string additionalInformations)
        {
            if (error != LoadingErrorEnum.None)
            {
                StopAllCoroutines();
                OnError.Invoke(GetErrorMessage(error, additionalInformations));
            }
        }
        string GetErrorMessage(LoadingErrorEnum error, string additionalInformations)
        {
            string l_errorMessage = string.Empty;
            string l_firstPart = "The visualisation could not be loaded.\n";
            switch (error)
            {
                case LoadingErrorEnum.None: l_errorMessage = "None error detected."; return l_errorMessage;
                case LoadingErrorEnum.CanNotFindFilesToRead: l_errorMessage = " Can not find the files of the column <color=red>n°  " + additionalInformations + "</color>."; break;
                case LoadingErrorEnum.CanNotReadData: l_errorMessage = " Can not read \" <color=red>" + additionalInformations + "</color>.\""; break;
                case LoadingErrorEnum.CanNotEpochingData: l_errorMessage = " Can not epoching data of the column \"<color=red>n°" + additionalInformations + "</color>.\""; break;
                case LoadingErrorEnum.CanNotStandardizeColumns: l_errorMessage = " Can not standardize columns."; break;
            }
            l_errorMessage = l_firstPart + l_errorMessage;
            return l_errorMessage;
        }
        #endregion
    }
}