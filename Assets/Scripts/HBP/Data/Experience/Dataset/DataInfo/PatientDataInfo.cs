using HBP.Errors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace HBP.Data.Experience.Dataset
{
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
        #endregion

        #region Contructors
        public PatientDataInfo(string name, Container.DataContainer dataContainer, Patient patient, string id) : base(name, dataContainer, id)
        {
            Patient = patient;
        }
        public PatientDataInfo() : this("Data", new Container.DataContainer(), ApplicationState.ProjectLoaded.Patients.FirstOrDefault(), Guid.NewGuid().ToString())
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
            return new PatientDataInfo(Name, DataContainer, Patient, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            PatientDataInfo dataInfo = copy as PatientDataInfo;
            Patient = dataInfo.Patient;
        }
        #endregion

        #region Public Methods
        public override Error[] GetErrors(Protocol.Protocol protocol)
        {
            List<Error> errors = new List<Error>(base.GetErrors(protocol));
            errors.AddRange(GetPatientErrors());
            return errors.Distinct().ToArray();
        }
        public Error[] GetPatientErrors()
        {
            List<Error> errors = new List<Error>();
            if (Patient == null) errors.Add(new PatientEmptyError());
            m_PatientErrors = errors.ToArray();
            return m_PatientErrors;
        }
        #endregion

        #region Serialization
        public override void OnDeserializedOperation(StreamingContext context)
        {
            base.OnDeserializedOperation(context);
            m_Patient = ApplicationState.ProjectLoaded.Patients.FirstOrDefault(p => p.ID == m_PatientID);
        }
        #endregion

        #region Errors
        public class PatientEmptyError : Error
        {
            #region Properties
            public PatientEmptyError() : this("")
            {

            }
            public PatientEmptyError(string message) : base("The patient field is empty.", message)
            {

            }
            #endregion
        }
        #endregion
    }
}