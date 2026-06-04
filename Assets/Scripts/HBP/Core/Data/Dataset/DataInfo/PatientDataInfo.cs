using HBP.Core.Errors;
using HBP.Core.Tools;
using HBP.Data.Database;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting;

namespace HBP.Core.Data
{
    /// <summary>
    /// Class containing paths to functional data files related to a patient.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader>
    /// <term>Data</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term><b>Name</b></term>
    /// <description>Name of the data.</description>
    /// </item>
    /// <item>
    /// <term><b>Patient</b></term>
    /// <description>Patient who has passed the experiment.</description>
    /// </item>
    /// <item>
    /// <term><b>Data container</b></term>
    /// <description>Data container containing all the paths to functional data files.</description>
    /// </item>
    /// <item>
    /// <term><b>Dataset</b></term>
    /// <description>Dataset the dataInfo belongs to.</description>
    /// </item>
    /// <item>
    /// <term><b>IsOk</b></term>
    /// <description>True if the dataInfo is visualizable, False otherwise.</description>
    /// </item>
    /// <item>
    /// <term><b>Errors</b></term>
    /// <description>All dataInfo errors.</description>
    /// </item>
    /// <item>
    /// <term><b>OnRequestErrorCheck</b></term>
    /// <description>Callback executed when error checking is required.</description>
    /// </item>
    /// </list>
    /// </remarks>
    [JsonObject(MemberSerialization.OptIn), Preserve, Hide]
    public class PatientDataInfo : DataInfo
    {
        #region Properties
        [JsonProperty("Patient")] protected string m_PatientID;
        protected Patient m_Patient;
        /// <summary>
        /// Patient who has passed the experiment.
        /// </summary>
        ///
        public Patient Patient
        {
            get
            {
                // Utile si le patient ne fait pas parti de la base de données
                if (m_Patient == null)
                {
                    UpdatePatient();
                }
                return m_Patient; 
            }
            set
            {
                if (value != null) m_PatientID = value.ID;
                m_Patient = value;
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new PatientDataInfo instance.
        /// </summary>
        /// <param name="name">Name of the patient dataInfo.</param>
        /// <param name="dataContainer">Data container of the patient dataInfo.</param>
        /// <param name="patient">Patient related to the data.</param>
        /// <param name="ID">Unique identifier</param>
        public PatientDataInfo(string name, Protocol protocol, Container.DataContainer dataContainer, IEnumerable<Error> errors, IEnumerable<Warning> warnings, Patient patient, string correspondingDatabaseID, string ID) : base(name, protocol, dataContainer, errors, warnings, correspondingDatabaseID, ID)
        {
            Patient = patient;
        }
        /// <summary>
        /// Create a new PatientDataInfo instance.
        /// </summary>
        /// <param name="name">Name of the patient dataInfo.</param>
        /// <param name="dataContainer">Data container of the patient dataInfo.</param>
        /// <param name="patient">Patient related to the data.</param>
        public PatientDataInfo(string name, Protocol protocol, Container.DataContainer dataContainer, IEnumerable<Error> errors, IEnumerable<Warning> warnings, Patient patient, string correspondingDatabaseID) : base(name, protocol, dataContainer, errors, warnings, correspondingDatabaseID)
        {
            Patient = patient;
        }
        /// <summary>
        /// Create a new PatientDataInfo instance.
        /// </summary>
        public PatientDataInfo() : this("Data", DatabaseManager.Database.Protocols.FirstOrDefault(), new Container.Elan(), new Error[0], new Warning[0], ApplicationState.LoadedProject.Patients.FirstOrDefault(), "")
        {
        }
        #endregion

        #region Operators
        /// <summary>
        /// Clone this instance.
        /// </summary>
        /// <returns>Clone of this instance.</returns>
        public override object Clone()
        {
            return new PatientDataInfo(Name, Protocol, DataContainer.Clone() as Container.DataContainer, Errors, Warnings, Patient, CorrespondingDatabaseID, ID);
        }
        public override void Copy(object obj)
        {
            base.Copy(obj);
            if (obj is PatientDataInfo patientDataInfo)
            {
                Patient = patientDataInfo.Patient;
            }
        }
        #endregion

        #region Private Methods
        protected override IEnumerable<Error> GetErrors()
        {
            List<Error> errors = new(base.GetErrors());
            errors.AddRange(GetPatientErrors());
            return errors;
        }
        /// <summary>
        /// Get all dataInfo errors related to the patient.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<Error> GetPatientErrors()
        {
            List<Error> errors = new();
            if (m_Patient == null) errors.Add(new PatientEmptyError());
            return errors;
        }
        protected override IEnumerable<Warning> GetWarnings()
        {
            List<Warning> warnings = new(base.GetWarnings());
            warnings.AddRange(GetPatientWarnings());
            return warnings;
        }
        private IEnumerable<Warning> GetPatientWarnings()
        {
            List<Warning> warnings = new();
            return warnings;
        }
        #endregion

        #region Public Methods
        public void UpdatePatient()
        {
            if (ApplicationState.LoadedProject != null && ApplicationState.LoadedProject.Datasets.Any(ds => ds.Data.Contains(this)))
                m_Patient = ApplicationState.LoadedProject.Patients.FirstOrDefault(p => p.ID == m_PatientID);
            else
                m_Patient = DatabaseManager.Database.Patients.FirstOrDefault(p => p.ID == m_PatientID);
        }
        #endregion

        #region Serialization
        protected override void OnDeserialized()
        {
            base.OnDeserialized();
            UpdatePatient();
        }
        #endregion
    }
}