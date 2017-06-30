using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

namespace HBP.Data.Experience.Dataset
{
    /**
    * \class DataInfo
    * \author Adrien Gannerie
    * \version 1.0
    * \date 09 janvier 2017
    * \brief DataInfo.
    * 
    * \details Class which define a DataInfo which contains : 
    *     - Name.
    *     - Patient.
    *     - Measure.
    *     - EEG file.
    *     - POS file.
    *     - Protocol.
    */
    [DataContract]
    public class DataInfo : ICloneable
    {
        #region Properties
        [DataMember] string m_Name;
        /// <summary>
        /// Name of the data.
        /// </summary>
        public string Name
        {
            get { return m_Name; }
            set { m_Name = value; m_NameErrors = GetNameErrors(); }
        }

        [DataMember(Name = "Patient")] string m_PatientID;
        Patient m_Patient;
        /// <summary>
        /// Patient who has passed the experiment.
        /// </summary>
        ///
        public Patient Patient
        {
            get { return m_Patient; }
            set { m_Patient = value; m_PatientErrors = GetPatientErrors(); }
        }

        [DataMember] string m_Measure;
        /// <summary>
        /// Name of the measure in the EEG file : "EGG data" by default.
        /// </summary>
        public string Measure
        {
            get { return m_Measure; }
            set { m_Measure = value; m_MeasureErrors = GetMeasureErrors(); }
        }

        [DataMember] string m_EEG;
        /// <summary>
        /// Path of the EEG file.
        /// </summary>
        public string EEG
        {
            get { return m_EEG; }
            set { m_EEG = value; m_EEGErrors = GetEEGErrors(); }
        }

        [DataMember] string m_POS;
        /// <summary>
        /// Path of the POS file.
        /// </summary>
         public string POS
        {
            get { return m_POS; }
            set { m_POS = value; m_POSErrors = GetPOSErrors(); }
        }

        [DataMember(Name = "Protocol")] string m_ProtocolID;
        Protocol.Protocol m_Protocol;
        /// <summary>
        /// Protocol used during the experiment.
        /// </summary>
        public Protocol.Protocol Protocol
        {
            get { return m_Protocol; }
            set { m_Protocol = value; m_ProtocolErrors = GetProtocolErrors(); }
        }
         
        /// <summary>
        /// Error type of the DataInfo.
        /// </summary>
        public enum ErrorType
        {
            LabelEmpty, PatientEmpty, MeasureEmpty, EEGEmpty, POSEmpty, ProtocolEmpty, EEGFileNotExist, POSFileNotExist,
            EEGFileNotAGoodFile, POSFileNotAGoodFile
        }

        ErrorType[] m_NameErrors;
        ErrorType[] m_MeasureErrors;
        ErrorType[] m_EEGErrors;
        ErrorType[] m_POSErrors;
        ErrorType[] m_PatientErrors;
        ErrorType[] m_ProtocolErrors;
        public bool isOk
        {
            get
            {
                return m_NameErrors.Length == 0 && m_MeasureErrors.Length == 0 && m_EEGErrors.Length == 0 && m_POSErrors.Length == 0 && m_PatientErrors.Length == 0 && m_ProtocolErrors.Length == 0;
            }
        }
        public ErrorType[] Errors
        {
            get
            {
                List<ErrorType> errors = new List<ErrorType>();
                errors.AddRange(m_NameErrors);
                errors.AddRange(m_MeasureErrors);
                errors.AddRange(m_EEGErrors);
                errors.AddRange(m_POSErrors);
                errors.Add(m_PatientErrors);
                errors.Add(m_ProtocolErrors);
                return errors.ToArray();
            }
        }
        #endregion

        #region Public Methods
        public ErrorType[] GetErrors()
        {
            List<ErrorType> errors = new List<ErrorType>();

            m_NameErrors = GetNameErrors();
            errors.AddRange(m_NameErrors);

            m_MeasureErrors = GetMeasureErrors();
            errors.AddRange(m_MeasureErrors);

            m_PatientErrors = GetPatientErrors();
            errors.AddRange(m_PatientErrors);

            m_EEGErrors = GetEEGErrors();
            errors.AddRange(m_EEGErrors);

            m_POSErrors = GetPOSErrors();
            errors.AddRange(m_POSErrors);

            m_ProtocolErrors = GetProtocolErrors();
            errors.AddRange(m_ProtocolErrors);
            return errors.ToArray();
        }
        public ErrorType[] GetNameErrors()
        {
            List<ErrorType> errors = new List<ErrorType>();
            if (string.IsNullOrEmpty(Name)) errors.Add(ErrorType.LabelEmpty);
            return errors.ToArray();
        }
        public ErrorType[] GetMeasureErrors()
        {
            List<ErrorType> errors = new List<ErrorType>();
            if (string.IsNullOrEmpty(Measure)) errors.Add(ErrorType.MeasureEmpty);
            return errors.ToArray();
        }
        public ErrorType[] GetPatientErrors()
        {
            List<ErrorType> errors = new List<ErrorType>();
            if (Patient == null) errors.Add(ErrorType.PatientEmpty);
            return errors.ToArray();
        }
        public ErrorType[] GetEEGErrors()
        {
            List<ErrorType> errors = new List<ErrorType>();
            if (string.IsNullOrEmpty(EEG)) errors.Add(ErrorType.EEGEmpty);
            return errors.ToArray();
        }
        public ErrorType[] GetPOSErrors()
        {
            List<ErrorType> errors = new List<ErrorType>();
            if (string.IsNullOrEmpty(POS)) errors.Add(ErrorType.POSEmpty);
            return errors.ToArray();
        }
        public ErrorType[] GetProtocolErrors()
        {
            List<ErrorType> errors = new List<ErrorType>();
            if (Protocol == null) errors.Add(ErrorType.ProtocolEmpty);
            return errors.ToArray();
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new DataInfo instance.
        /// </summary>
        /// <param name="name">Name of the dataInfo.</param>
        /// <param name="patient">Patient who passed the experiment.</param>
        /// <param name="measure">Name of the measure in the EEG file.</param>
        /// <param name="eeg">EEG file path.</param>
        /// <param name="pos">POS file path.</param>
        /// <param name="protocol">Protocol used for the experiment.</param>
        public DataInfo(string name, Patient patient, string measure, string eeg, string pos, Protocol.Protocol protocol)
        {
            Name = name;
            Patient = patient;
            Measure = measure;
            EEG = eeg;
            POS = pos;
            Protocol = protocol;
        }
        /// <summary>
        /// Create a new DataInfo instance with default value.
        /// </summary>
        public DataInfo() : this("Data", ApplicationState.ProjectLoaded.Patients.FirstOrDefault(),"EEG data", string.Empty, string.Empty,ApplicationState.ProjectLoaded.Protocols.FirstOrDefault())
        {
        }
        #endregion

        #region Operators
        /// <summary>
        /// Clone this instance.
        /// </summary>
        /// <returns>Clone of this instance.</returns>
        public object Clone()
        {
            return new DataInfo(Name.Clone() as string, Patient.Clone() as Patient, Measure.Clone() as string, EEG.Clone() as string, POS.Clone() as string, Protocol.Clone() as Protocol.Protocol);
        }
        #endregion

        #region Serialization
        [OnSerializing]
        void OnSerializing()
        {
            m_PatientID = Patient.ID;
            m_ProtocolID = Protocol.ID;
        }
        [OnDeserialized]
        void OnDeserialized()
        {
            Patient = ApplicationState.ProjectLoaded.Patients.FirstOrDefault(p => p.ID == m_PatientID);
            Protocol = ApplicationState.ProjectLoaded.Protocols.FirstOrDefault(p => p.ID == m_ProtocolID);
        }
        #endregion
    }
}