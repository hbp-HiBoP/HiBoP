using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Collections.ObjectModel;
using HBP.Data.Anatomy;
using HBP.Data.Experience.Dataset;
using System;

namespace HBP.Data.Visualization
{
    /**
    * \class MultiPatientsVisualization
    * \author Adrien Gannerie
    * \version 1.0
    * \date 09 mai 2017
    * \brief 3D brain multi-patients visualization.
    * 
    * \details Define a 3D brain multi-patients3D visualization and contains:
    * 
    *   - \a ID.
    *   - \a Name.
    *   - \a Patients.
    *   - \a Configuration.
    *   - \a Columns.
    */
    [DataContract]
    public class MultiPatientsVisualization : Visualization
    {
        #region Properties
        public const string EXTENSION = ".mpv";
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
        public override bool IsVisualizable
        {
            get
            {
                return base.IsVisualizable && Patients.Count > 0 && Columns.All((column) => column.IsCompatible(Patients));
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new multi-patients visualization.
        /// </summary>
        /// <param name="name">Name of the visualization.</param>
        /// <param name="columns">Columns of the visualization.</param>
        /// <param name="patients">Patients of the visualization.</param>
        /// <param name="id">Unique ID.</param>
        public MultiPatientsVisualization(string name,IEnumerable<Column> columns, IEnumerable<Patient> patients, string id) : base(name,columns,id)
        {
            SetPatients(patients.ToArray());
        }
        /// <summary>
        /// Create a new multi-patients visualization.
        /// </summary>
        /// <param name="name">Name of the visualization.</param>
        /// <param name="columns">Columns of the visualization.</param>
        /// <param name="patients">Patients of the visualization.</param>
        public MultiPatientsVisualization(string name, IEnumerable<Column> columns, IEnumerable<Patient> patients) : base (name,columns)
        {
            SetPatients(patients.ToArray());
        }
        /// <summary>
        /// Create a new multi-patients visualization with default values.
        /// </summary>
        public MultiPatientsVisualization() : base()
        {
            RemoveAllPatients();
        }
        #endregion

        #region Public Methods
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
            } 
        }
        /// <summary>
        /// Add patients to the visualization.
        /// </summary>
        /// <param name="patients">Patients to add.</param>
        public void AddPatient(IEnumerable<Patient> patients)
        {
            foreach(Patient patient in patients)
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
            if(m_patients.Remove(patient))
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
        public void SetPatients(Patient[] patients)
        {
            RemovePatient(from patient in this.m_patients where !patients.Contains(patient) select patient);
            AddPatient(from patient in patients where !this.m_patients.Contains(patient) select patient);
        }
        /// <summary>
        /// Remove all patients in the visualization.
        /// </summary>
        public void RemoveAllPatients()
        {
            RemovePatient(Patients);
        }
        /// <summary>
        /// Get the DataInfo used by the column.
        /// </summary>
        /// <param name="column">Column to get the dataInfo.</param>
        /// <returns>DataInfo used by the column.</returns>
        public override IEnumerable<DataInfo> GetDataInfo(Column column)
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
            return column.Dataset.Data.Find((data) => (column.DataLabel == data.Name && data.Patient == patient && column.Protocol == data.Protocol && data.Protocol.Blocs.Contains(column.Bloc)));
        }
        public override IEnumerable<Patient> GetPatients()
        {
            return m_patients;
        }
        public override void Load()
        {
            foreach (Patient patient in m_patients) AddPatientConfiguration(patient);
            base.Load();
        }
        #endregion  

        #region Operators
        /// <summary>
        /// Clone this multi-patients visualization instance.
        /// </summary>
        /// <returns>Multi-patients visualization clone.</returns>
        public override object Clone()
        {
            return new MultiPatientsVisualization(Name, from column in Columns select column.Clone() as Column, from patient in Patients select patient.Clone() as Patient, ID);
        }
        /// <summary>
        /// Copy a multi-patients viusalization instance to this instance.
        /// </summary>
        /// <param name="copy">Multi-patients visualization instance to copy.</param>
        public override void Copy(object copy)
        {
            base.Copy(copy);
            MultiPatientsVisualization visu = copy as MultiPatientsVisualization;
            SetPatients(visu.Patients.ToArray());
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