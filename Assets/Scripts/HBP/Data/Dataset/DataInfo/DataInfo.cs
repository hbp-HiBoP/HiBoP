using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using UnityEngine.Events;
using HBP.Errors;

namespace HBP.Data.Experience.Dataset
{
    /// <summary>
    /// A base class containing paths to functional data files.
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
        /// <summary>
        /// Data container containing all the paths to functional data files.
        /// </summary>
        public Container.DataContainer DataContainer
        {
            get { return m_DataContainer; }
            set { m_DataContainer = value; m_DataContainer.GetErrors(); m_DataContainer.OnRequestErrorCheck.AddListener(OnRequestErrorCheck.Invoke); }
        }

        /// <summary>
        /// Dataset the dataInfo belongs to.
        /// </summary>
        public Dataset Dataset
        {
            get
            {
                return ApplicationState.ProjectLoaded.Datasets.FirstOrDefault((d) => d.Data.Contains(this));
            }
        }

        /// <summary>
        /// Naming-related errors.
        /// </summary>
        protected Error[] m_NameErrors = new Error[0];

        /// <summary>
        /// True if the dataInfo is visualizable, False otherwise.
        /// </summary>
        public bool IsOk
        {
            get
            {
                return Errors.Length == 0;
            }
        }

        /// <summary>
        /// All dataInfo errors.
        /// </summary>
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

        /// <summary>
        /// Callback executed when error checking is required.
        /// </summary>
        public UnityEvent OnRequestErrorCheck { get; set; } = new UnityEvent();
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new DataInfo instance.
        /// </summary>
        /// <param name="name">Name of the dataInfo.</param>
        /// <param name="dataContainer">Data container of the dataInfo.</param>
        /// <param name="ID">Unique identifier of the dataInfo.</param>
        public DataInfo(string name, Container.DataContainer dataContainer, string ID) : base(ID)
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
        public DataInfo() : this("Data", new Container.Elan(), Guid.NewGuid().ToString())
        {
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Get all dataInfo errors.
        /// </summary>
        /// <param name="protocol">Protocol of the dataset the dataInfo belongs to.</param>
        /// <returns>All dataInfo errors.</returns>
        public virtual Error[] GetErrors(Protocol.Protocol protocol)
        {
            List<Error> errors = new List<Error>(GetNameErrors());
            errors.AddRange(m_DataContainer.GetErrors());
            return errors.Distinct().ToArray();
        }
        /// <summary>
        /// Get all naming-related errors.
        /// </summary>
        /// <returns>All naming-related errors.</returns>
        public virtual Error[] GetNameErrors()
        {
            List<Error> errors = new List<Error>();
            if (string.IsNullOrEmpty(Name)) errors.Add(new LabelEmptyError());
            m_NameErrors = errors.ToArray();
            return m_NameErrors;
        }
        /// <summary>
        /// Get all message errors in a readable form.
        /// </summary>
        /// <returns></returns>
        public virtual string GetErrorsMessage()
        {
            Error[] errors = Errors;
            StringBuilder stringBuilder = new StringBuilder();
            if (errors.Length == 0) stringBuilder.Append(string.Format("• {0}", "No error detected."));
            else
            {
                for (int i = 0; i < errors.Length - 1; i++)
                {
                    if (errors[i].Message != "")
                    {
                        stringBuilder.AppendLine(string.Format("• {0} ({1})", errors[i].Title, errors[i].Message));

                    }
                    else
                    {
                        stringBuilder.AppendLine(string.Format("• {0}", errors[i].Title));

                    }
                }
                if (errors.Last().Message != "")
                {
                    stringBuilder.Append(string.Format("• {0} ({1})", errors.Last().Title, errors.Last().Message));

                }
                else
                {
                    stringBuilder.Append(string.Format("• {0}", errors.Last().Title));

                }
            }
            return stringBuilder.ToString();
        }
        /// <summary>
        /// Generate a new unique identifier.
        /// </summary>
        public override void GenerateID()
        {
            base.GenerateID();
            DataContainer.GenerateID();
        }
        public override List<BaseData> GetAllIdentifiable()
        {
            List<BaseData> IDs = base.GetAllIdentifiable();
            IDs.AddRange(DataContainer.GetAllIdentifiable());
            return IDs;
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
        /// <summary>
        /// Copy an instance to this instance.
        /// </summary>
        /// <param name="copy"></param>
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is DataInfo dataInfo)
            {
                Name = dataInfo.Name;
                DataContainer = dataInfo.DataContainer;
            }
        }
        #endregion
    }
}