using HBP.Core.Errors;
using HBP.Core.Tools;
using HBP.Core.Database;
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
            get => m_Patient;
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
        public PatientDataInfo() : this("Data", null, new Container.Elan(), new Error[0], new Warning[0], null, "")
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

        internal override IEnumerable<ValidationState> GetValidationStates(ValidationAspect aspect, ValidationRequest request, DataInfoValidationContext context)
        {
            if (aspect != ValidationAspect.Structure)
            {
                return base.GetValidationStates(aspect, request, context);
            }

            List<Error> errors = base.GetValidationStates(aspect, request, context).SelectMany(state => state.Errors).ToList();
            if (m_Patient == null)
            {
                errors.Add(new PatientEmptyError());
            }

            return new[]
            {
                CreateValidationState(aspect, string.Empty, $"{Name}|{Protocol?.ID}|{Patient?.ID}", errors, System.Array.Empty<Warning>())
            };
        }

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

        /// <summary>
        /// Rebinds this instance against the currently published graph. Loading
        /// code should use <see cref="LoadingContext"/> directly.
        /// </summary>
        public void UpdatePatient()
        {
            IEnumerable<Patient> patients = ApplicationState.LoadedProject != null && ApplicationState.LoadedProject.Datasets.Any(dataset => dataset.Data.Contains(this)) ? ApplicationState.LoadedProject.Patients : DatabaseManager.Database.Patients;
            LoadingContext context = new(System.Array.Empty<BaseTag>(), DatabaseManager.Database.Protocols, patients);
            m_Patient = context.ResolveRequired(context.PatientById, m_PatientID ?? m_Patient?.ID, "patient", $"{GetType().Name} '{ID}'");
        }

        internal override void ResolveReferences(LoadingContext context)
        {
            base.ResolveReferences(context);
            ResolvePatientReference(context, true);
        }

        internal void ResolvePatientReference(LoadingContext context, bool required)
        {
            string patientID = m_PatientID ?? m_Patient?.ID;
            m_Patient = required ? context.ResolveRequired(context.PatientById, patientID, "patient", $"{GetType().Name} '{ID}'") : context.ResolveOptional(context.PatientById, patientID);
        }

        #endregion

        #region Serialization

        protected override void OnDeserialized()
        {
            base.OnDeserialized();
        }

        #endregion
    }
}
