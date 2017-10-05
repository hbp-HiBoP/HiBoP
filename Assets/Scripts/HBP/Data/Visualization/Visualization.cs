﻿using UnityEngine;
using UnityEngine.Events;
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Runtime.Serialization;
using CielaSpike;
using HBP.Data.Experience.Dataset;
using System.Diagnostics;

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
    public class Visualization :  ICloneable , ICopiable
    {
        #region Properties
        public const string EXTENSION = ".visualization";

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

        [DataMember(Name = "Patients", Order = 3)]
        List<string> m_patientsID;
        /// <summary>
        /// Patients of the Visualization.
        /// </summary>
        public ReadOnlyCollection<Patient> Patients
        {
            get
            {
                IEnumerable<Patient> patients = from patient in ApplicationState.ProjectLoaded.Patients where m_patientsID.Contains(patient.ID) select patient;
                return new ReadOnlyCollection<Patient>(patients.ToArray());
            }
        }

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
            get { return Columns.Count > 0 && Patients.Count > 0 && Columns.All((column) => column.IsCompatible(Patients)); }
        }

        const float FIND_FILES_TO_READ_PROGRESS = 0.025f;
        const float READ_FILES_PROGRESS = 0.6f;
        const float LOAD_COLUMNS_PROGRESS = 0.3f;
        const float STANDARDIZE_COLUMNS_PROGRESS = 0.075f;
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new visualization instance.
        /// </summary>
        /// <param name="name">Name of the visualization.</param>
        /// <param name="columns">Columns of the visualization.</param>
        /// <param name="id">Unique ID.</param>
        public Visualization(string name, IEnumerable<Patient> patients, IEnumerable<Column> columns, string id)
        {
            Name = name;
            Columns = columns.ToList();
            SetPatients(patients);
            Configuration = new VisualizationConfiguration();
            ID = id;
        }
        /// <summary>
        /// Create a new visualization instance.
        /// </summary>
        /// <param name="name">Name of the visualization.</param>
        /// <param name="columns">Columns of the visualization.</param>
        public Visualization(string name, IEnumerable<Patient> patients, IEnumerable<Column> columns) : this(name, patients, columns, Guid.NewGuid().ToString())
        {
        }
        /// <summary>
        /// Create a new visualization instance with default value.
        /// </summary>
        public Visualization() : this("Unknown", new Patient[0], new Column[0])
        {

        }
        #endregion

        #region Getter/Setter
        /// <summary>
        /// Add a patient to the visualization.
        /// </summary>
        /// <param name="patient">Patient to add.</param>
        public void AddPatient(Patient patient)
        {
            if (!Patients.Contains(patient)) m_patientsID.Add(patient.ID);
        }
        /// <summary>
        /// Add patients to the visualization.
        /// </summary>
        /// <param name="patients">Patients to add.</param>
        public void AddPatient(IEnumerable<Patient> patients)
        {
            foreach (Patient patient in patients)
            {
                AddPatient(patient);
            }
        }
        /// <summary>
        /// Remove a patient to the visualization.
        /// </summary>
        /// <param name="patient">Patient to remove.</param>
        public void RemovePatient(Patient patient)
        {
            m_patientsID.Remove(patient.ID);
        }
        /// <summary>
        /// Remove patients to the visualization.
        /// </summary>
        /// <param name="patients">Patients to remove.</param>
        public void RemovePatient(IEnumerable<Patient> patients)
        {
            foreach (Patient patient in patients) RemovePatient(patient);
        }
        /// <summary>
        /// Set patients in the visualization.
        /// </summary>
        /// <param name="patients">Patients to set in the visualization.</param>
        public void SetPatients(IEnumerable<Patient> patients)
        {
            m_patientsID = new List<string>();
            AddPatient(from patient in patients where !m_patientsID.Contains(patient.ID) select patient);
        }
        /// <summary>
        /// Remove all patients in the visualization.
        /// </summary>
        public void RemoveAllPatients()
        {
            RemovePatient(Patients);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Load the visualization.
        /// </summary>
        /// <returns></returns>
        public IEnumerator c_Load(GenericEvent<float,float,string> onChangeProgress = null)
        {
            if (onChangeProgress == null) onChangeProgress = new GenericEvent<float, float, string>();

            float progress = 0.0f;

            yield return Ninja.JumpToUnity;
            Dictionary<Column, DataInfo[]> dataInfoByColumn = new Dictionary<Column, DataInfo[]>();
            yield return Ninja.JumpToUnity;
            yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_FindDataToRead(progress, onChangeProgress,(value, progressValue) => { dataInfoByColumn = value; progress = progressValue; }));
            yield return Ninja.JumpBack;
            Dictionary<DataInfo, Experience.Dataset.Data> dataByDataInfo = (from dataInfos in dataInfoByColumn.Values from dataInfo in dataInfos select dataInfo).Distinct().ToDictionary(t => t, t => new Experience.Dataset.Data());
            Dictionary<Column, Experience.Dataset.Data[]> dataByColumn = new Dictionary<Column, Experience.Dataset.Data[]>();
            yield return Ninja.JumpToUnity;
            yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_ReadData(dataInfoByColumn, dataByDataInfo, progress, onChangeProgress,(value, progressValue) => { dataByColumn = value; progress = progressValue; }));
            yield return Ninja.JumpBack;
            yield return Ninja.JumpToUnity;
            yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_LoadColumns(dataByColumn, progress, onChangeProgress,(value) => progress = value));
            yield return Ninja.JumpBack;
            yield return Ninja.JumpToUnity;
            yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_StandardizeColumns(progress, onChangeProgress,(value) => progress = value));
        }
        /// <summary>
        /// Unload the visualization.
        /// </summary>
        public void Unload()
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
        public IEnumerable<DataInfo> GetDataInfo(Column column)
        {
            return column.Dataset.Data.Where((data) => (column.Data == data.Name && Patients.Contains(data.Patient)));
        }
        /// <summary>
        /// Get the DataInfo used by the column for a specific Patient.
        /// </summary>
        /// <param name="patient">Patient concerned.</param>
        /// <param name="column">Column concerned.</param>
        /// <returns>DataInfo used by the column for the specific Patient.</returns>
        public DataInfo GetDataInfo(Patient patient, Column column)
        {
            return GetDataInfo(column).First((dataInfo) => dataInfo.Patient == patient);
        }
        #endregion

        #region Operators
        /// <summary>
        /// Clone this instance.
        /// </summary>
        /// <returns>Clone of this instance.</returns>
        public object Clone()
        {
            Column[] columns = (from column in Columns select column.Clone() as Column).ToArray();
            return new Visualization(Name, Patients, columns, ID);
        }
        /// <summary>
        /// Copy an instance in this instance.
        /// </summary>
        /// <param name="copy">Instance to copy.</param>
        public void Copy(object copy)
        {
            Visualization visualization = copy as Visualization;
            Name = visualization.Name;
            Columns = visualization.Columns;
            ID = visualization.ID;
            Configuration = visualization.Configuration;
            SetPatients(visualization.Patients);
        }
        #endregion

        #region Private Methods
        IEnumerator c_FindDataToRead(float progress, GenericEvent<float, float, string> onChangeProgress, Action<Dictionary<Column, DataInfo[]>, float> outPut)
        {
            // Find files to read.
            Dictionary<Column, DataInfo[]> dataInfoByColumn = new Dictionary<Column, DataInfo[]>();
            float progressStep = FIND_FILES_TO_READ_PROGRESS / (Columns.Count);
            foreach (var column in Columns)
            {
                // Update progress;
                yield return Ninja.JumpToUnity;
                progress += progressStep;
                onChangeProgress.Invoke(progress, 0.0f, "Finding files to read.");
                yield return Ninja.JumpBack;

                // Work.
                try
                {
                    List<DataInfo> dataInfoForThisColumn = GetDataInfo(column).ToList();
                    foreach (Patient patient in Patients)
                    {
                        if (!dataInfoForThisColumn.Exists((dataInfo) => dataInfo.Patient == patient))
                        {
                            throw new CannotFindDataInfoException(patient.ID, column.Data);
                        }
                    }
                    dataInfoByColumn.Add(column, GetDataInfo(column).ToArray());
                }
                catch(Exception exception)
                {
                }
            }
            outPut(dataInfoByColumn, progress);
        }
        IEnumerator c_ReadData(Dictionary<Column, DataInfo[]> dataInfoByColumn, Dictionary<DataInfo, Experience.Dataset.Data> dataByDataInfo, float progress, GenericEvent<float, float, string> onChangeProgress, Action<Dictionary<Column, Experience.Dataset.Data[]>, float> outPut)
        {
            Stopwatch timer = new Stopwatch();
            float progressStep = READ_FILES_PROGRESS / (dataByDataInfo.Count);
            float readingSpeed = 36000000;
            List<DataInfo> dataInfoToRead = dataByDataInfo.Keys.ToList();
            foreach (var dataInfo in dataInfoToRead)
            {
                // Find file to read informations.
                FileInfo fileToRead = new FileInfo(dataInfo.EEG);
                float assumedReadingTime = fileToRead.Length / readingSpeed;

                // Update progressBar
                progress += progressStep;
                yield return Ninja.JumpToUnity;
                onChangeProgress.Invoke(progress, assumedReadingTime, "Reading" + fileToRead.Name);
                yield return Ninja.JumpBack;

                // Read Data.
                try
                {
                    timer.Start();
                    dataByDataInfo[dataInfo] = new Experience.Dataset.Data(dataInfo);
                    timer.Stop();
                }
                catch (Exception exception)
                {
                    throw (exception);
                }

                // Calculate real reading speed.
                float actualReadingTime = timer.ElapsedMilliseconds / 1000.0f;
                readingSpeed = Mathf.Lerp(readingSpeed, fileToRead.Length / actualReadingTime, 0.5f);
                timer.Reset();
            }
            Dictionary<Column, Experience.Dataset.Data[]> dataByColumn = dataInfoByColumn.ToDictionary(t => t.Key, t => (from dataInfo in t.Value select dataByDataInfo[dataInfo]).ToArray());
            outPut(dataByColumn, progress);
        }
        IEnumerator c_LoadColumns(Dictionary<Column, Experience.Dataset.Data[]> dataByColumn, float progress, GenericEvent<float, float, string> onChangeProgress, Action<float> outPut)
        {
            float progressStep = LOAD_COLUMNS_PROGRESS / Columns.Count;
            foreach (Column column in Columns)
            {
                yield return Ninja.JumpToUnity;
                progress += progressStep;
                onChangeProgress.Invoke(progress, 0.2f, "Load column <color=blue>" + column.Data + "</color>.");
                yield return Ninja.JumpBack;
                column.Load(dataByColumn[column]);
                yield return Ninja.JumpToUnity;
                column.IconicScenario.LoadIcons();
            }
            outPut(progress);
        }
        IEnumerator c_StandardizeColumns(float progress, GenericEvent<float, float, string> onChangeProgress, Action<float> outPut)
        {
            float progressStep = STANDARDIZE_COLUMNS_PROGRESS / Columns.Count;
            int maxBefore = (from column in Columns select column.TimeLine.MainEvent.Position).Max();
            int maxAfter = (from column in Columns select column.TimeLine.Lenght - column.TimeLine.MainEvent.Position).Max();
            foreach (var column in Columns)
            {
                yield return Ninja.JumpToUnity;
                progress += progressStep;
                onChangeProgress.Invoke(progress, 0, "Standardize column <color=blue>" + column.Data + "</color>.");
                yield return Ninja.JumpBack;
                column.Standardize(maxBefore, maxAfter);
            }
        }
        #endregion
    }
}