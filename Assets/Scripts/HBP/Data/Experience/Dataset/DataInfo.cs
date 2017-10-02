using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

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
    public class DataInfo : ICloneable, ICopiable
    {
        #region Properties
        [DataMember(Name = "Name")]
        string m_Name;
        /// <summary>
        /// Name of the data.
        /// </summary>
        public string Name
        {
            get { return m_Name; }
            set { m_Name = value; m_NameErrors = GetNameErrors(); }
        }

        [DataMember(Name = "Patient")]
        string m_Patient;
        /// <summary>
        /// Patient who has passed the experiment.
        /// </summary>
        ///
        public Patient Patient
        {
            get { return ApplicationState.ProjectLoaded.Patients.FirstOrDefault(p => p.ID == m_Patient); }
            set { m_Patient = value.ID; m_PatientErrors = GetPatientErrors(); }
        }

        [DataMember(Name = "Measure")]
        string m_Measure;
        /// <summary>
        /// Name of the measure in the EEG file : "EGG data" by default.
        /// </summary>
        public string Measure
        {
            get { return m_Measure; }
            set { m_Measure = value; m_MeasureErrors = GetMeasureErrors(); }
        }

        [DataMember(Name = "EEG")]
        string m_EEG;
        /// <summary>
        /// Path of the EEG file.
        /// </summary>
        public string EEG
        {
            get { return m_EEG; }
            set { m_EEG = value; m_EEGErrors = GetEEGErrors(); }
        }

        [DataMember(Name = "POS")]
        string m_POS;
        /// <summary>
        /// Path of the POS file.
        /// </summary>
        public string POS
        {
            get { return m_POS; }
            set { m_POS = value; m_POSErrors = GetPOSErrors(); }
        }

        [DataMember(Name = "Protocol")]
        string m_Protocol;
        /// <summary>
        /// Protocol used during the experiment.
        /// </summary>
        public Protocol.Protocol Protocol
        {
            get { return ApplicationState.ProjectLoaded.Protocols.FirstOrDefault(p => p.ID == m_Protocol); ; }
            set { m_Protocol = value.ID; m_ProtocolErrors = GetProtocolErrors(); }
        }
         
        /// <summary>
        /// Error type of the DataInfo.
        /// </summary>
        public enum ErrorType
        {
            LabelEmpty, PatientEmpty, MeasureEmpty, EEGEmpty, POSEmpty, ProtocolEmpty, EEGFileNotExist, POSFileNotExist,
            EEGFileNotAGoodFile, EEGDoNotContainsMeasure, POSFileNotAGoodFile
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
                return errors.Distinct().ToArray();
            }
        }
        #endregion

        #region Public Methods
        public ErrorType[] GetErrors()
        {
            GetNameErrors();
            GetPatientErrors();
            GetMeasureErrors();
            GetEEGErrors();
            GetPOSErrors();
            GetProtocolErrors();
            return Errors;
        }
        public ErrorType[] GetNameErrors()
        {
            List<ErrorType> errors = new List<ErrorType>();
            if(string.IsNullOrEmpty(Name)) errors.Add(ErrorType.LabelEmpty);
            m_NameErrors = errors.ToArray();
            return errors.ToArray();
        }
        public ErrorType[] GetMeasureErrors()
        {
            List<ErrorType> errors = new List<ErrorType>();
            if(string.IsNullOrEmpty(Measure)) errors.Add(ErrorType.MeasureEmpty);
            if(m_EEGErrors != null && m_EEGErrors.Length == 0)
            {
                Elan.ElanFile elanFile = new Elan.ElanFile(EEG, false);
                if (!elanFile.MeasureLabels.Contains(Measure))
                {
                    errors.Add(ErrorType.EEGDoNotContainsMeasure);
                }
            }
            m_MeasureErrors = errors.ToArray();
            return errors.ToArray();
        }
        public ErrorType[] GetPatientErrors()
        {
            List<ErrorType> errors = new List<ErrorType>();
            if (Patient == null) errors.Add(ErrorType.PatientEmpty);
            m_PatientErrors = errors.ToArray();
            return errors.ToArray();
        }
        public ErrorType[] GetEEGErrors()
        {
            List<ErrorType> errors = new List<ErrorType>();
            if (string.IsNullOrEmpty(EEG)) errors.Add(ErrorType.EEGEmpty);
            else
            {
                FileInfo EEGFile = new FileInfo(EEG);
                if (!EEGFile.Exists) errors.Add(ErrorType.EEGFileNotExist);
                else
                {
                    if (EEGFile.Extension != Elan.EEG.EXTENSION) errors.Add(ErrorType.EEGFileNotAGoodFile);
                    else
                    {
                        Elan.ElanFile elanFile = new Elan.ElanFile(EEGFile.FullName, false);
                        if (!elanFile.MeasureLabels.Contains(Measure)) errors.Add(ErrorType.EEGDoNotContainsMeasure);
                    }
                }
            }
            m_EEGErrors = errors.ToArray();
            return errors.ToArray();
        }
        public ErrorType[] GetPOSErrors()
        {
            List<ErrorType> errors = new List<ErrorType>();
            if (string.IsNullOrEmpty(POS)) errors.Add(ErrorType.POSEmpty);
            else
            {
                FileInfo POSFile = new FileInfo(POS);
                if (!POSFile.Exists) errors.Add(ErrorType.POSFileNotExist);
                else
                {
                    if (POSFile.Extension != Localizer.POS.EXTENSION) errors.Add(ErrorType.POSFileNotAGoodFile);
                }
            }
            m_POSErrors = errors.ToArray();
            return errors.ToArray();
        }
        public ErrorType[] GetProtocolErrors()
        {
            List<ErrorType> errors = new List<ErrorType>();
            if (Protocol == null) errors.Add(ErrorType.ProtocolEmpty);
            m_ProtocolErrors = errors.ToArray();
            return errors.ToArray();
        }
        public string GetErrorsMessage()
        {
            ErrorType[] errors = Errors;
            StringBuilder stringBuilder = new StringBuilder();
            if (errors.Length == 0) stringBuilder.Append("• No error detected.");
            else
            {
                for (int i = 0; i < errors.Length - 1; i++)
                {
                    stringBuilder.AppendLine(GetErrorMessage(errors[i]));
                }
                stringBuilder.Append(GetErrorMessage(errors.Last()));
            }
            return stringBuilder.ToString();
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
        public void Copy(object copy)
        {
            DataInfo dataInfo = copy as DataInfo;
            Name = dataInfo.Name;
            Patient = dataInfo.Patient;
            Protocol = dataInfo.Protocol;
            Measure = dataInfo.Measure;
            EEG = dataInfo.EEG;
            POS = dataInfo.POS;
        }
        #endregion

        #region Private Methods
        string GetErrorMessage(ErrorType error)
        {
            string message = string.Empty;
            switch (error)
            {
                case ErrorType.LabelEmpty:
                    message = "• The label field is empty.";
                    break;
                case ErrorType.PatientEmpty:
                    message = "• The patient field is empty.";
                    break;
                case ErrorType.MeasureEmpty:
                    message = "• The measure field is empty.";
                    break;
                case ErrorType.EEGEmpty:
                    message = "• The EEG field is empty.";
                    break;
                case ErrorType.POSEmpty:
                    message = "• The POS field is empty.";
                    break;
                case ErrorType.ProtocolEmpty:
                    message = "• The protocol field is empty.";
                    break;
                case ErrorType.EEGFileNotExist:
                    message = "• The EEG file does not exist.";
                    break;
                case ErrorType.POSFileNotExist:
                    message = "• The POS file does not exist.";
                    break;
                case ErrorType.EEGFileNotAGoodFile:
                    message = "• The EEG file is incorrect.";
                    break;
                case ErrorType.EEGDoNotContainsMeasure:
                    message = "• The EEG file does not contains the measure.";
                    break;
                case ErrorType.POSFileNotAGoodFile:
                    message = "• The EEG file is incorrect.";
                    break;
                default:
                    break;
            }
            return message;
        }
        #endregion
    }
}