using UnityEngine;
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

        [DataMember(Order =3)]
        public Anatomy.ReferenceFrameType ReferenceFrame { get; set; }

        [DataMember(Name = "Patients", Order = 3)]
        IEnumerable<string> m_patientsID;
        List<Patient> m_patients;
        /// <summary>
        /// Patients of the Visualization.
        /// </summary>
        public ReadOnlyCollection<Patient> Patients
        {
            get { return new ReadOnlyCollection<Patient>(m_patients); }
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
        const float READ_FILES_PROGRESS = 0.8f;
        const float EPOCH_DATA_PROGRESS = 0.025f;
        const float STANDARDIZE_COLUMNS_PROGRESS = 0.15f;
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new visualization instance.
        /// </summary>
        /// <param name="name">Name of the visualization.</param>
        /// <param name="columns">Columns of the visualization.</param>
        /// <param name="id">Unique ID.</param>
        public Visualization(string name, Anatomy.ReferenceFrameType referenceFrame, IEnumerable<Patient> patients, IEnumerable<Column> columns, string id)
        {
            Name = name;
            ReferenceFrame = referenceFrame;
            SetPatients(patients);
            Columns = columns.ToList();
            Configuration = new VisualizationConfiguration();
            ID = id;
        }
        /// <summary>
        /// Create a new visualization instance.
        /// </summary>
        /// <param name="name">Name of the visualization.</param>
        /// <param name="columns">Columns of the visualization.</param>
        public Visualization(string name, Anatomy.ReferenceFrameType referenceFrame, IEnumerable<Patient> patients, IEnumerable<Column> columns) : this(name, referenceFrame, patients, columns, Guid.NewGuid().ToString())
        {
        }
        /// <summary>
        /// Create a new visualization instance with default value.
        /// </summary>
        public Visualization() : this("Unknown", Anatomy.ReferenceFrameType.Patient, new Patient[0], new Column[0])
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
            if (!Patients.Contains(patient))
            {
                m_patients.Add(patient);
                AddPatientConfiguration(patient);
                if(m_patients.Count > 1)
                {
                    ReferenceFrame = Anatomy.ReferenceFrameType.MNI;
                }
            }
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
            if (m_patients.Remove(patient))
            {
                RemovePatientConfiguration(patient);
            }
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
            m_patients = new List<Patient>();
            AddPatient(from patient in patients where !this.m_patients.Contains(patient) select patient);
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

            Dictionary<Column, DataInfo[]> dataInfoByColumn = new Dictionary<Column, DataInfo[]>();
            yield return c_FindDataToRead(progress, onChangeProgress,(value, progressValue) => { dataInfoByColumn = value; progress = progressValue; });
            Dictionary<DataInfo, Experience.Dataset.Data> dataByDataInfo = (from dataInfos in dataInfoByColumn.Values from dataInfo in dataInfos select dataInfo).Distinct().ToDictionary(t => t, t => new Experience.Dataset.Data());
            Dictionary<Column, Experience.Dataset.Data[]> dataByColumn = new Dictionary<Column, Experience.Dataset.Data[]>();
            yield return c_ReadData(dataInfoByColumn, dataByDataInfo, progress, onChangeProgress,(value, progressValue) => { dataByColumn = value; progress = progressValue; });
            yield return c_LoadColumns(dataByColumn, progress, onChangeProgress,(value) => progress = value);
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
            return column.Dataset.Data.FindAll((data) => (column.DataLabel == data.Name && Patients.Contains(data.Patient) && column.Protocol == data.Protocol && data.Protocol.Blocs.Contains(column.Bloc)));
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
            return new Visualization(Name, ReferenceFrame, Patients, from column in Columns select column.Clone() as Column, ID);

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
        void AddPatientConfiguration(Patient patient)
        {
            foreach (Column column in Columns)
            {
                if (!column.Configuration.ConfigurationByPatient.ContainsKey(patient)) column.Configuration.ConfigurationByPatient.Add(patient, new PatientConfiguration(Configuration.Color, patient));
                PatientConfiguration patientConfiguration = column.Configuration.ConfigurationByPatient[patient];
                foreach (Anatomy.Electrode electrode in patient.Brain.Implantation.Electrodes)
                {
                    if (!patientConfiguration.ConfigurationByElectrode.ContainsKey(electrode)) patientConfiguration.ConfigurationByElectrode.Add(electrode, new ElectrodeConfiguration(patientConfiguration.Color, patient));
                    ElectrodeConfiguration electrodeConfiguration = patientConfiguration.ConfigurationByElectrode[electrode];
                    foreach (Anatomy.Site site in electrode.Sites)
                    {
                        if (!electrodeConfiguration.ConfigurationBySite.ContainsKey(site)) electrodeConfiguration.ConfigurationBySite.Add(site, new SiteConfiguration(electrodeConfiguration.Color));
                    }
                }
            }
        }
        void RemovePatientConfiguration(Patient patient)
        {
            foreach (Column column in Columns)
            {
                if(column.Configuration.ConfigurationByPatient.ContainsKey(patient))
                {
                    column.Configuration.ConfigurationByPatient.Remove(patient);
                }
            }
        }
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
                            throw new CannotFindDataInfoException(patient.ID, column.DataLabel);
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
            }
            Dictionary<Column, Experience.Dataset.Data[]> dataByColumn = dataInfoByColumn.ToDictionary(t => t.Key, t => (from dataInfo in t.Value select dataByDataInfo[dataInfo]).ToArray());
            outPut(dataByColumn, progress);
        }
        IEnumerator c_LoadColumns(Dictionary<Column, Experience.Dataset.Data[]> dataByColumn, float progress, GenericEvent<float, float, string> onChangeProgress, Action<float> outPut)
        {
            float progressStep = EPOCH_DATA_PROGRESS / Columns.Count;
            foreach (Column column in Columns)
            {
                yield return Ninja.JumpToUnity;
                progress += progressStep;
                onChangeProgress.Invoke(progress, 0, "Load column <color=blue>" + column.DataLabel + "</color>.");
                yield return Ninja.JumpBack;
                column.Load(dataByColumn[column]);
            }
        }
        #endregion

        #region Serialization
        [OnSerializing]
        void OnSerializing(StreamingContext streamingContext)
        {
            m_patientsID = from patient in m_patients select patient.ID;
        }
        [OnDeserialized]
        void OnDeserialized(StreamingContext streamingContext)
        {
            m_patients = ApplicationState.ProjectLoaded.Patients.Where((patient) => m_patientsID.Contains(patient.ID)).ToList();
        }
        #endregion
    }
}