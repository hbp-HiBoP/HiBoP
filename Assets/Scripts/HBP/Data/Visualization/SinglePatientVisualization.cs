using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using HBP.Data.Experience.Dataset;

namespace HBP.Data.Visualization
{
    /**
    * \class SinglePatientVisualization
    * \author Adrien Gannerie
    * \version 2.0
    * \date 09 mai 2017
    * \brief Class which define a visualization with the single patient.
    * 
    * \details Single patient visualization is a class which contains the informations of a single patient visualization and herite from the Visualization class.
    */
    [DataContract]
    public class SinglePatientVisualization : Visualization
    {
        #region Properties
        public const string EXTENSION = ".singleVisualization";
        [DataMember(Name = "Patient",Order = 3)]
        private string m_patientID;
        /// <summary>
        /// Patient of the single patient visualization.
        /// </summary>
        public Patient Patient { get; set; }
        /// <summary>
        /// Test if the visualization is visualizable.
        /// </summary>
        public override bool IsVisualizable
        {
            get
            {
                return base.IsVisualizable && Patient != null && Columns.All((c) => c.IsCompatible(Patient));
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new single patient visualization instance.
        /// </summary>
        /// <param name="name">Name of single patient visualization.</param>
        /// <param name="columns">Columns of single patient visualization.</param>
        /// <param name="patient">Patient of the single patient visualization.</param>
        /// <param name="id">Unique ID of the single patient visualization.</param>
        public SinglePatientVisualization(string name,IEnumerable<Column> columns, Patient patient, string id) : base(name,columns,id)
        {
            Patient = patient;
        }
        /// <summary>
        /// Create a new single patient visualization instance.
        /// </summary>
        /// <param name="name">Name of single patient visualization.</param>
        /// <param name="columns">Columns of single patient visualization.</param>
        /// <param name="patient">Patient of the single patient visualization.</param>
        public SinglePatientVisualization(string name,IEnumerable<Column> columns,Patient patient) : base (name,columns)
        {
            Patient = patient;
        }
        /// <summary>
        /// Create a new single patient visualization instance with default value.
        /// </summary>
        public SinglePatientVisualization() : base()
        {
            Patient = new Patient();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Get the DataInfo used by the column.
        /// </summary>
        /// <param name="column">Column to get the dataInfo.</param>
        /// <returns>DataInfo used by the column.</returns>
        public override IEnumerable<DataInfo> GetDataInfo(Column column)
        {
            return column.Dataset.Data.FindAll((data) => (column.DataLabel == data.Name && data.Patient.ID == m_patientID && column.Protocol == data.Protocol && data.Protocol.Blocs.Contains(column.Bloc)));
        }
        /// <summary>
        /// Load a single patient visualization from a multi-patients visualization.
        /// </summary>
        /// <param name="multiPatientsVisualization">Multi patients visualization.</param>
        /// <param name="patient">Patient of the single patient visualization.</param>
        /// <returns>Single patient visualization loaded from the multi-patients visualization.</returns>
        public static SinglePatientVisualization LoadFromMultiPatients(MultiPatientsVisualization multiPatientsVisualization,Patient patient)
        {
            return new SinglePatientVisualization(multiPatientsVisualization.Name, multiPatientsVisualization.Columns, patient);
        }
        /// <summary>
        /// Load the visualization data.
        /// </summary>
        public override void Load()
        {
            AddPatientConfiguration(Patient);
            base.Load();
        }
        public override IEnumerable<Patient> GetPatients()
        {
            return new Patient[] { Patient };
        }
        #endregion

        #region Operators
        /// <summary>
        /// Clone this single patient visualization instance.
        /// </summary>
        /// <returns>Single patient visualization clone.</returns>
        public override object Clone()
        {
            return new SinglePatientVisualization(Name.Clone() as string, from column in Columns select column.Clone() as Column, Patient);
        }
        /// <summary>
        /// Copy a single patient viusalization instance to this instance.
        /// </summary>
        /// <param name="copy">Single patient visualization instance to copy.</param>
        public override void Copy(object copy)
        {
            base.Copy(copy);
            Patient = (copy as SinglePatientVisualization).Patient;
        }
        #endregion

        #region Serialization
        [OnSerializing]
        void OnSerializing(StreamingContext streamingContext)
        {
            m_patientID = Patient.ID;
        }
        [OnDeserialized]
        void OnDeserialized(StreamingContext streamingContext)
        {
            Patient = ApplicationState.ProjectLoaded.Patients.FirstOrDefault((patient) => patient.ID == m_patientID);
        }
        #endregion
    }
}