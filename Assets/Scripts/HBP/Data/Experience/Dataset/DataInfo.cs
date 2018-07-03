using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using UnityEngine.Events;
using Tools.Unity;

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
        [DataMember(Name = "Name")] string m_Name;
        /// <summary>
        /// Name of the data.
        /// </summary>
        public string Name
        {
            get { return m_Name; }
            set { m_Name = value; m_NameErrors = GetNameErrors(); }
        }

        [DataMember(Name = "Patient")] string m_Patient;
        /// <summary>
        /// Patient who has passed the experiment.
        /// </summary>
        ///
        public Patient Patient
        {
            get { return ApplicationState.ProjectLoaded.Patients.FirstOrDefault(p => p.ID == m_Patient); }
            set { m_Patient = value.ID; m_PatientErrors = GetPatientErrors(); }
        }

        [DataMember(Name = "Measure")] string m_Measure;
        /// <summary>
        /// Name of the measure in the EEG file : "EGG data" by default.
        /// </summary>
        public string Measure
        {
            get { return m_Measure; }
            set { m_Measure = value; m_MeasureErrors = GetMeasureErrors(); }
        }

        [DataMember(Name = "EEG")] string m_EEG;
        /// <summary>
        /// Path of the EEG file.
        /// </summary>
        public string EEG
        {
            get { return m_EEG.ConvertToFullPath(); }
            set { m_EEG = value.ConvertToShortPath(); m_EEGErrors = GetEEGErrors(); }
        }
        public string SavedEEG { get { return m_EEG; } }

        [DataMember(Name = "POS")] string m_POS;
        /// <summary>
        /// Path of the POS file.
        /// </summary>
        public string POS
        {
            get { return m_POS.ConvertToFullPath(); }
            set { m_POS = value.ConvertToShortPath(); m_POSErrors = new ErrorType[0]; OnPOSChanged.Invoke(); }
        }
        public string SavedPOS { get { return m_POS; } }
        public UnityEvent OnPOSChanged { get; set; }
        
        [DataMember(Name = "Normalization")]
        /// <summary>
        /// Normalization of the Data.
        /// </summary>
        public NormalizationType Normalization { get; set; }

        /// <summary>
        /// Error type of the DataInfo.
        /// </summary>
        public enum ErrorType
        {
            LabelEmpty, PatientEmpty, MeasureEmpty, EEGEmpty, POSEmpty, EEGFileNotExist, POSFileNotExist,
            EEGFileNotAGoodFile, EEGDoNotContainsMeasure, POSFileNotAGoodFile, POSNotCompatible, EEGHeaderNotExist, EEGHeaderEmpty
        }
        /// <summary>
        /// Normalization Type.
        /// </summary>
        public enum NormalizationType
        {
            None, Trial, Bloc, Protocol, Auto
        }

        ErrorType[] m_NameErrors;
        ErrorType[] m_PatientErrors;
        ErrorType[] m_MeasureErrors;
        ErrorType[] m_EEGErrors;
        ErrorType[] m_POSErrors;

        public bool isOk
        {
            get
            {
                return Errors.Length == 0;
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
                errors.AddRange(m_PatientErrors);
                return errors.Distinct().ToArray();
            }
        }
        #endregion

        #region Public Methods
        public ErrorType[] GetErrors(Protocol.Protocol protocol)
        {
            GetNameErrors();
            GetPatientErrors();
            GetMeasureErrors();
            GetEEGErrors();
            GetPOSErrors(protocol);
            return Errors;
        }
        public ErrorType[] GetNameErrors()
        {
            List<ErrorType> errors = new List<ErrorType>();
            if(string.IsNullOrEmpty(Name)) errors.Add(ErrorType.LabelEmpty);
            m_NameErrors = errors.ToArray();
            return m_NameErrors;
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
                elanFile.Dispose();
            }
            m_MeasureErrors = errors.ToArray();
            return m_MeasureErrors;
        }
        public ErrorType[] GetPatientErrors()
        {
            List<ErrorType> errors = new List<ErrorType>();
            if (Patient == null) errors.Add(ErrorType.PatientEmpty);
            m_PatientErrors = errors.ToArray();
            return m_PatientErrors;
        }
        public ErrorType[] GetEEGErrors()
        {
            List<ErrorType> errors = new List<ErrorType>();
            if (string.IsNullOrEmpty(EEG))
            {
                errors.Add(ErrorType.EEGEmpty);
            }
            else
            {
                FileInfo EEGFile = new FileInfo(EEG);
                if (!EEGFile.Exists)
                {
                    errors.Add(ErrorType.EEGFileNotExist);
                }
                else
                {
                    if (EEGFile.Extension != Elan.EEG.EXTENSION)
                    {
                        errors.Add(ErrorType.EEGFileNotAGoodFile);
                    }
                    else
                    {
                        if (!File.Exists(EEGFile.FullName + Elan.EEG.HEADER_EXTENSION))
                        {
                            errors.Add(ErrorType.EEGHeaderNotExist);
                        }
                        else
                        {
                            if (!(new FileInfo(EEGFile.FullName + Elan.EEG.HEADER_EXTENSION).Length > 0))
                            {
                                errors.Add(ErrorType.EEGHeaderEmpty);
                            }
                            else
                            {
                                Elan.ElanFile elanFile = new Elan.ElanFile(EEGFile.FullName, false);
                                if (!elanFile.MeasureLabels.Contains(Measure)) errors.Add(ErrorType.EEGDoNotContainsMeasure);
                                elanFile.Dispose();
                            }
                        }
                    }
                }
            }
            m_EEGErrors = errors.ToArray();
            return m_EEGErrors;
        }
        public ErrorType[] GetPOSErrors(Protocol.Protocol protocol)
        {
            List<ErrorType> errors = new List<ErrorType>();
            if (string.IsNullOrEmpty(POS)) errors.Add(ErrorType.POSEmpty);
            else
            {
                FileInfo POSFile = new FileInfo(POS);
                if (!POSFile.Exists) errors.Add(ErrorType.POSFileNotExist);
                else
                {
                    if (POSFile.Extension != Localizer.POS.EXTENSION && POSFile.Extension != Localizer.POS.BIDS_EXTENSION) errors.Add(ErrorType.POSFileNotAGoodFile);
                    else
                    {
                        Localizer.POS pos = new Localizer.POS(POS);
                        if (!pos.IsCompatible(protocol)) errors.Add(ErrorType.POSNotCompatible);
                    }
                }
            }
            m_POSErrors = errors.ToArray();
            return m_POSErrors;
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
        public DataInfo(string name, Patient patient, string measure, string eeg, string pos, NormalizationType normalization)
        {
            OnPOSChanged = new UnityEvent();
            Name = name;
            Patient = patient;
            Measure = measure;
            EEG = eeg;
            POS = pos;
            Normalization = normalization;
        }
        /// <summary>
        /// Create a new DataInfo instance with default value.
        /// </summary>
        public DataInfo() : this("Data", ApplicationState.ProjectLoaded.Patients.FirstOrDefault(),"EEG data", string.Empty, string.Empty, NormalizationType.Auto)
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
            DataInfo dataInfo =  new DataInfo(Name.Clone() as string, Patient.Clone() as Patient, Measure.Clone() as string, m_EEG.Clone() as string, m_EEG.Clone() as string, Normalization);
            dataInfo.OnPOSChanged = OnPOSChanged;
            return dataInfo;
        }
        public void Copy(object copy)
        {
            DataInfo dataInfo = copy as DataInfo;
            Name = dataInfo.Name;
            Patient = dataInfo.Patient;
            Measure = dataInfo.Measure;
            EEG = dataInfo.EEG;
            POS = dataInfo.POS;
            Normalization = dataInfo.Normalization;
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
                    message = "• The POS file is incorrect.";
                    break;
                case ErrorType.POSNotCompatible:
                    message = "• The POS file is not compatible with the protocol.";
                    break;
                case ErrorType.EEGHeaderNotExist:
                    message = "• The header of the EEG file is missing.";
                    break;
                case ErrorType.EEGHeaderEmpty:
                    message = "• The header of the EEG file is empty.";
                    break;
                default:
                    break;
            }
            return message;
        }
        #endregion
    }
}