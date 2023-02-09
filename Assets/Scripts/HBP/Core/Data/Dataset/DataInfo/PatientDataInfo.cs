using HBP.Core.Errors;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using HBP.Core.Tools;

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
    [DataContract, Hide]
    public class PatientDataInfo : DataInfo
    {
        #region Properties
        [DataMember(Name = "Patient")] protected string m_PatientID;
        protected Patient m_Patient;
        /// <summary>
        /// Patient who has passed the experiment.
        /// </summary>
        ///
        public Patient Patient
        {
            get { return m_Patient; }
            set { m_PatientID = value.ID; m_Patient = ApplicationState.ProjectLoaded.Patients.FirstOrDefault(p => p.ID == m_PatientID); m_PatientErrors = GetPatientErrors(); }
        }

        protected Error[] m_PatientErrors = new Error[0];
        public override Error[] Errors
        {
            get
            {
                List<Error> errors = new List<Error>(base.Errors);
                errors.AddRange(m_PatientErrors);
                return errors.Distinct().ToArray();
            }
        }

        protected Warning[] m_PatientWarnings = new Warning[0];
        public override Warning[] Warnings
        {
            get
            {
                List<Warning> warnings = new List<Warning>(base.Warnings);
                warnings.AddRange(m_PatientWarnings);
                return warnings.Distinct().ToArray();
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
        public PatientDataInfo(string name, Container.DataContainer dataContainer, Patient patient, string ID) : base(name, dataContainer, ID)
        {
            Patient = patient;
        }
        /// <summary>
        /// Create a new PatientDataInfo instance.
        /// </summary>
        /// <param name="name">Name of the patient dataInfo.</param>
        /// <param name="dataContainer">Data container of the patient dataInfo.</param>
        /// <param name="patient">Patient related to the data.</param>
        public PatientDataInfo(string name, Container.DataContainer dataContainer, Patient patient) : base(name, dataContainer)
        {
            Patient = patient;
        }
        /// <summary>
        /// Create a new PatientDataInfo instance.
        /// </summary>
        public PatientDataInfo() : this("Data", new Container.Elan(), ApplicationState.ProjectLoaded.Patients.FirstOrDefault())
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
            return new PatientDataInfo(Name, DataContainer.Clone() as Container.DataContainer, Patient, ID);
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

        #region Public Methods
        public override Error[] GetErrors(Protocol protocol)
        {
            List<Error> errors = new List<Error>(base.GetErrors(protocol));
            errors.AddRange(GetPatientErrors());
            return errors.Distinct().ToArray();
        }
        /// <summary>
        /// Get all dataInfo errors related to the patient.
        /// </summary>
        /// <returns></returns>
        public Error[] GetPatientErrors()
        {
            List<Error> errors = new List<Error>();
            if (Patient == null) errors.Add(new PatientEmptyError());
            m_PatientErrors = errors.ToArray();
            return m_PatientErrors;
        }
        public override Warning[] GetWarnings(Protocol protocol)
        {
            List<Warning> warnings = new List<Warning>(base.GetWarnings(protocol));
            warnings.AddRange(GetPatientWarnings());
            return warnings.Distinct().ToArray();
        }
        public Warning[] GetPatientWarnings()
        {
            List<Warning> warnings = new List<Warning>();
            m_PatientWarnings = warnings.ToArray();
            return m_PatientWarnings;
        }
        #endregion

        #region Serialization
        protected override void OnDeserialized()
        {
            base.OnDeserialized();
            m_Patient = ApplicationState.ProjectLoaded.Patients.FirstOrDefault(p => p.ID == m_PatientID);
        }
        #endregion
    }
}