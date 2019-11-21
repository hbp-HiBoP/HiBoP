using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using UnityEngine.Events;
using HBP.Errors;

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
    public class DataInfo : BaseData, INameable
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

        [DataMember(Name = "DataContainer")] protected Container.DataContainer m_DataContainer;
        public Container.DataContainer DataContainer
        {
            get { return m_DataContainer; }
            set { m_DataContainer = value; m_DataContainer.GetErrors(); m_DataContainer.OnRequestErrorCheck.AddListener(OnRequestErrorCheck.Invoke); }
        }

        public Dataset Dataset
        {
            get
            {
                return ApplicationState.ProjectLoaded.Datasets.FirstOrDefault((d) => d.Data.Contains(this));
            }
        }

        protected Error[] m_NameErrors = new Error[0];

        public bool IsOk
        {
            get
            {
                return Errors.Length == 0;
            }
        }
        public virtual Error[] Errors
        {
            get
            {
                List<Error> errors = new List<Error>();
                errors.AddRange(m_NameErrors);
                errors.AddRange(m_DataContainer.Errors);
                return errors.Distinct().ToArray();
            }
        }

        public UnityEvent OnRequestErrorCheck { get; set; } = new UnityEvent();
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new DataInfo instance.
        /// </summary>
        /// <param name="name">Name of the dataInfo.</param>
        /// <param name="dataContainer">Data container of the dataInfo.</param>
        /// <param name="id">Unique ID of the subBloc.</param>
        public DataInfo(string name, Container.DataContainer dataContainer, string id): base(id)
        {
            Name = name;
            DataContainer = dataContainer;
        }
        /// <summary>
        /// Create a new DataInfo instance.
        /// </summary>
        /// <param name="name">Name of the dataInfo.</param>
        /// <param name="dataContainer">Data container of the dataInfo.</param>
        public DataInfo(string name, Container.DataContainer dataContainer) : base()
        {
            Name = name;
            DataContainer = dataContainer;
        }
        /// <summary>
        /// Create a new DataInfo instance with default value.
        /// </summary>
        public DataInfo() : this("Data", new Container.DataContainer(), Guid.NewGuid().ToString())
        {
        }
        #endregion

        #region Public Methods
        public virtual Error[] GetErrors(Protocol.Protocol protocol)
        {
            List<Error> errors = new List<Error>(GetNameErrors());
            errors.AddRange(m_DataContainer.GetErrors());
            return errors.Distinct().ToArray();
        }
        public virtual Error[] GetNameErrors()
        {
            List<Error> errors = new List<Error>();
            if (string.IsNullOrEmpty(Name)) errors.Add(new LabelEmptyError());
            m_NameErrors = errors.ToArray();
            return m_NameErrors;
        }
        public virtual string GetErrorsMessage()
        {
            Error[] errors = Errors;
            StringBuilder stringBuilder = new StringBuilder();
            if (errors.Length == 0) stringBuilder.Append(string.Format("• {0}", "No error detected."));
            else
            {
                for (int i = 0; i < errors.Length - 1; i++)
                {
                    stringBuilder.AppendLine(string.Format("• {0}", errors[i].Message));
                }
                stringBuilder.Append(string.Format("• {0}", errors.Last().Message));
            }
            return stringBuilder.ToString();
        }
        public override void GenerateID()
        {
            base.GenerateID();
            DataContainer.GenerateID();
        }
        #endregion

        #region Operators
        /// <summary>
        /// Clone this instance.
        /// </summary>
        /// <returns>Clone of this instance.</returns>
        public override object Clone()
        {
            return new DataInfo(Name, DataContainer.Clone() as Container.DataContainer, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if(copy is DataInfo dataInfo)
            {
                Name = dataInfo.Name;
                DataContainer = dataInfo.DataContainer;
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

        #region Errors
        public class LabelEmptyError : Error
        {
            #region Properties
            public LabelEmptyError() : this("")
            {
            }
            public LabelEmptyError(string message) : base("The label field is empty", message)
            {
            }
            #endregion
        }
        #endregion
    }
}