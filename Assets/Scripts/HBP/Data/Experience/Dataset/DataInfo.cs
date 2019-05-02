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
    public class DataInfo : ICloneable, ICopiable, IIdentifiable
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

        [DataMember] public string ID { get; set; }

        [DataMember(Name = "Patient")] string m_PatientID;
        Patient m_Patient;
        /// <summary>
        /// Patient who has passed the experiment.
        /// </summary>
        ///
        public Patient Patient
        {
            get { return m_Patient; }
            set { m_PatientID = value.ID; m_Patient = ApplicationState.ProjectLoaded.Patients.FirstOrDefault(p => p.ID == m_PatientID); m_PatientErrors = GetPatientErrors(); }
        }
        
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
            LabelEmpty, PatientEmpty, RequiredFieldEmpty, FileDoesNotExist, WrongExtension, BlocsCantBeEpoched, NotEnoughInformation
        }
        /// <summary>
        /// Normalization Type.
        /// </summary>
        public enum NormalizationType
        {
            None, SubTrial, Trial, SubBloc, Bloc, Protocol, Auto
        }

        public Dataset Dataset
        {
            get
            {
                return ApplicationState.ProjectLoaded.Datasets.FirstOrDefault((d) => d.Data.Contains(this));
            }
        }
        
        protected ErrorType[] m_NameErrors = new ErrorType[0];
        protected ErrorType[] m_PatientErrors = new ErrorType[0];
        protected ErrorType[] m_DataErrors = new ErrorType[0];

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
                errors.AddRange(m_PatientErrors);
                errors.AddRange(m_DataErrors);
                return errors.Distinct().ToArray();
            }
        }

        public UnityEvent OnRequestErrorCheck = new UnityEvent();

        public virtual string DataTypeString
        {
            get
            {
                return "None";
            }
        }
        public virtual string DataFilesString
        {
            get
            {
                return "";
            }
        }
        public virtual Tools.CSharp.EEG.File.FileType Type
        {
            get
            {
                throw new Exception("Invalid file type");
            }
        }
        #endregion

        #region Public Methods
        public ErrorType[] GetErrors(Protocol.Protocol protocol)
        {
            GetNameErrors();
            GetPatientErrors();
            GetDataErrors(protocol);
            return Errors;
        }
        public ErrorType[] GetNameErrors()
        {
            List<ErrorType> errors = new List<ErrorType>();
            if(string.IsNullOrEmpty(Name)) errors.Add(ErrorType.LabelEmpty);
            m_NameErrors = errors.ToArray();
            return m_NameErrors;
        }
        public ErrorType[] GetPatientErrors()
        {
            List<ErrorType> errors = new List<ErrorType>();
            if (Patient == null) errors.Add(ErrorType.PatientEmpty);
            m_PatientErrors = errors.ToArray();
            return m_PatientErrors;
        }
        public virtual ErrorType[] GetDataErrors(Protocol.Protocol protocol)
        {
            List<ErrorType> errors = new List<ErrorType>();
            m_DataErrors = errors.ToArray();
            return m_DataErrors;
        }
        public string GetErrorsMessage()
        {
            ErrorType[] errors = Errors;
            StringBuilder stringBuilder = new StringBuilder();
            if (errors.Length == 0) stringBuilder.Append(string.Format("• {0}", "No error detected."));
            else
            {
                for (int i = 0; i < errors.Length - 1; i++)
                {
                    stringBuilder.AppendLine(string.Format("• {0}", GetErrorMessage(errors[i])));
                }
                stringBuilder.Append(string.Format("• {0}", GetErrorMessage(errors.Last())));
            }
            return stringBuilder.ToString();
        }
        public virtual void CopyDataToDirectory(DirectoryInfo dataInfoDirectory, string projectDirectory, string oldProjectDirectory)
        {
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
        public DataInfo(string name, Patient patient, NormalizationType normalization, string id)
        {
            Name = name;
            Patient = patient;
            Normalization = normalization;
            ID = id;
        }
        /// <summary>
        /// Create a new DataInfo instance with default value.
        /// </summary>
        public DataInfo() : this("Data", ApplicationState.ProjectLoaded.Patients.FirstOrDefault(), NormalizationType.Auto, Guid.NewGuid().ToString())
        {
        }
        #endregion

        #region Operators
        /// <summary>
        /// Operator Equals.
        /// </summary>
        /// <param name="obj">Object to test.</param>
        /// <returns>\a True if equals and \a false otherwise.</returns>
        public override bool Equals(object obj)
        {
            DataInfo dataInfo = obj as DataInfo;
            if (dataInfo != null && dataInfo.ID == ID)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// Get hash code.
        /// </summary>
        /// <returns>HashCode.</returns>
        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }
        /// <summary>
        /// Operator equals.
        /// </summary>
        /// <param name="a">First mesh to compare.</param>
        /// <param name="b">Second mesh to compare.</param>
        /// <returns>\a True if equals and \a false otherwise.</returns>
        public static bool operator ==(DataInfo a, DataInfo b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }

            return a.Equals(b);
        }
        /// <summary>
        /// Operator not equals.
        /// </summary>
        /// <param name="a">First mesh to compare.</param>
        /// <param name="b">Second mesh to compare.</param>
        /// <returns>\a True if not equals and \a false otherwise.</returns>
        public static bool operator !=(DataInfo a, DataInfo b)
        {
            return !(a == b);
        }
        /// <summary>
        /// Clone this instance.
        /// </summary>
        /// <returns>Clone of this instance.</returns>
        public virtual object Clone()
        {
            return new DataInfo(Name, Patient, Normalization, ID);
        }
        public virtual void Copy(object copy)
        {
            DataInfo dataInfo = copy as DataInfo;
            Name = dataInfo.Name;
            Patient = dataInfo.Patient;
            Normalization = dataInfo.Normalization;
            ID = dataInfo.ID;
        }
        #endregion

        #region Private Methods
        string GetErrorMessage(ErrorType error)
        {
            switch (error)
            {
                case ErrorType.LabelEmpty:
                    return "The label field is empty.";
                case ErrorType.PatientEmpty:
                    return "The patient field is empty.";
                case ErrorType.RequiredFieldEmpty:
                    return "One of the required fields is empty.";
                case ErrorType.FileDoesNotExist:
                    return "One of the files does not exist.";
                case ErrorType.WrongExtension:
                    return "One of the files has a wrong extension.";
                case ErrorType.BlocsCantBeEpoched:
                    return "One of the blocs of the protocol can't be epoched.";
                case ErrorType.NotEnoughInformation:
                    return "One of the files does not contain enough information.";
                default:
                    return "Unknown error.";
            }
        }
        #endregion

        #region Serialization
        [OnDeserialized()]
        public void OnDeserialized(StreamingContext context)
        {
            OnDeserializedOperation(context);
        }
        public virtual void OnDeserializedOperation(StreamingContext context)
        {
            m_Patient = ApplicationState.ProjectLoaded.Patients.FirstOrDefault(p => p.ID == m_PatientID);
        }
        #endregion
    }
}