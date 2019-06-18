using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using UnityEngine.Events;

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
    */
    [DataContract]
    public class DataInfo : ICloneable, ICopiable, IIdentifiable
    {
        #region Properties
        [DataMember(Name = "Name")] protected string m_Name;
        /// <summary>
        /// Name of the data.
        /// </summary>
        public string Name
        {
            get { return m_Name; }
            set { m_Name = value; m_NameErrors = GetNameErrors(); }
        }

        [DataMember] public string ID { get; set; }

        [DataMember] protected DataContainer m_DataContainer;
        public DataContainer DataContainer
        {
            get { return m_DataContainer; }
            set { m_DataContainer = value;  m_DataContainer.GetErrors(); m_DataContainer.OnRequestErrorCheck.AddListener(OnRequestErrorCheck.Invoke); }
        }
        /// <summary>
        /// Error type of the DataInfo.
        /// </summary>
        public enum ErrorType
        {
            LabelEmpty, PatientEmpty, RequiredFieldEmpty, FileDoesNotExist, WrongExtension, BlocsCantBeEpoched, NotEnoughInformation
        }



        public Dataset Dataset
        {
            get
            {
                return ApplicationState.ProjectLoaded.Datasets.FirstOrDefault((d) => d.Data.Contains(this));
            }
        }
        
        protected ErrorType[] m_NameErrors = new ErrorType[0];

        public bool IsOk
        {
            get
            {
                return Errors.Length == 0;
            }
        }
        public virtual ErrorType[] Errors
        {
            get
            {
                List<ErrorType> errors = new List<ErrorType>();
                errors.AddRange(m_NameErrors);
                errors.AddRange(m_DataContainer.Errors);
                return errors.Distinct().ToArray();
            }
        }

        public UnityEvent OnRequestErrorCheck { get; set; } = new UnityEvent();
        #endregion

        #region Public Methods
        public virtual ErrorType[] GetErrors(Protocol.Protocol protocol)
        {
            List<ErrorType> errors = new List<ErrorType>(GetNameErrors());
            errors.AddRange(m_DataContainer.GetErrors());
            return errors.Distinct().ToArray();
        }
        public ErrorType[] GetNameErrors()
        {
            List<ErrorType> errors = new List<ErrorType>();
            if(string.IsNullOrEmpty(Name)) errors.Add(ErrorType.LabelEmpty);
            m_NameErrors = errors.ToArray();
            return m_NameErrors;
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
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new DataInfo instance.
        /// </summary>
        /// <param name="name">Name of the dataInfo.</param>
        /// <param name="measure">Name of the measure in the EEG file.</param>
        /// <param name="eeg">EEG file path.</param>
        /// <param name="pos">POS file path.</param>
        public DataInfo(string name, DataContainer dataContainer, string id)
        {
            Name = name;
            ID = id;
            DataContainer = dataContainer;
        }
        /// <summary>
        /// Create a new DataInfo instance with default value.
        /// </summary>
        public DataInfo() : this("Data", new DataContainer(), Guid.NewGuid().ToString())
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
            return new DataInfo(Name, DataContainer, ID);
        }
        public virtual void Copy(object copy)
        {
            DataInfo dataInfo = copy as DataInfo;
            Name = dataInfo.Name;
            DataContainer = dataInfo.DataContainer;
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
        }
        #endregion
    }
}