using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Events;
using HBP.Core.Errors;
using HBP.Core.Interfaces;
using Newtonsoft.Json;
using HBP.Data.Database;
using System.Collections.ObjectModel;
using Cysharp.Threading.Tasks;
using HBP.Core.Tools;

namespace HBP.Core.Data
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
    [JsonObject(MemberSerialization.OptIn)]
    public class DataInfo : BaseData, ILoadableFromDatabase<DataInfo>, INameable
    {
        #region Properties
        public const string EXTENSION = ".datainfo";

        [JsonProperty("Name")] protected string m_Name;
        /// <summary>
        /// Name of the data.
        /// </summary>
        public string Name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        [JsonProperty("DataContainer")] protected Container.DataContainer m_DataContainer;
        /// <summary>
        /// Data container containing all the paths to functional data files.
        /// </summary>
        public Container.DataContainer DataContainer
        {
            get { return m_DataContainer; }
            set { m_DataContainer = value; }
        }

        [JsonProperty] public string CorrespondingDatabaseID { get; set; }

        [JsonProperty] private string m_ProtocolID;
        private Protocol m_Protocol;
        public Protocol Protocol
        {
            get => m_Protocol;
            set
            {
                m_Protocol = value;
            }
        }

        [JsonProperty] protected Error[] m_Errors = new Error[0];
        public ReadOnlyCollection<Error> Errors => new(m_Errors);

        [JsonProperty] protected Warning[] m_Warnings = new Warning[0];
        public ReadOnlyCollection<Warning> Warnings => new(m_Warnings);

        /// <summary>
        /// True if the dataInfo is visualizable, False otherwise.
        /// </summary>
        public bool IsOk
        {
            get
            {
                return Errors.Count == 0;
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
        public DataInfo(string name, Protocol protocol, Container.DataContainer dataContainer, IEnumerable<Error> errors, IEnumerable<Warning> warnings, string correspondingDatabaseID, string ID) : base(ID)
        {
            m_Name = name;
            m_Protocol = protocol;
            m_DataContainer = dataContainer;
            m_Errors = errors.ToArray();
            m_Warnings = warnings.ToArray();
            CorrespondingDatabaseID = correspondingDatabaseID;
        }
        /// <summary>
        /// Create a new DataInfo instance.
        /// </summary>
        /// <param name="name">Name of the dataInfo.</param>
        /// <param name="dataContainer">Data container of the dataInfo.</param>
        public DataInfo(string name, Protocol protocol, Container.DataContainer dataContainer, IEnumerable<Error> errors, IEnumerable<Warning> warnings, string correspondingDatabaseID) : base()
        {
            m_Name = name;
            m_Protocol = protocol;
            m_DataContainer = dataContainer;
            m_Errors = errors.ToArray();
            m_Warnings = warnings.ToArray();
            CorrespondingDatabaseID = correspondingDatabaseID;
        }
        /// <summary>
        /// Create a new DataInfo instance with default value.
        /// </summary>
        public DataInfo() : this("Data", DatabaseManager.Database.Protocols.FirstOrDefault(), new Container.Elan(), new Error[0], new Warning[0], "", Guid.NewGuid().ToString())
        {
        }
        #endregion

        #region Public Methods
        public virtual void CheckErrorsAndWarnings()
        {
            m_Errors = GetErrors().Distinct().ToArray();
            m_Warnings = GetWarnings().Distinct().ToArray();
        }
        /// <summary>
        /// Get all message errors in a readable form.
        /// </summary>
        /// <returns></returns>
        public virtual string GetErrorsMessage()
        {
            var errors = Errors;
            StringBuilder stringBuilder = new();
            if (errors.Count == 0)
                stringBuilder.Append(string.Format("• {0}", "No error detected."));
            else
            {
                stringBuilder.AppendLine("Errors:");
                for (int i = 0; i < errors.Count - 1; i++)
                    stringBuilder.AppendLine(errors[i].FormatedMessage);
                stringBuilder.Append(errors.Last().FormatedMessage);
            }
            return stringBuilder.ToString();
        }
        /// <summary>
        /// Get all message warnings in a readable form.
        /// </summary>
        /// <returns></returns>
        public virtual string GetWarningsMessage()
        {
            var warnings = Warnings;
            StringBuilder stringBuilder = new();
            if (warnings.Count == 0)
                stringBuilder.Append(string.Format("• {0}", "No error detected."));
            else
            {
                stringBuilder.AppendLine("Warnings:");
                for (int i = 0; i < warnings.Count - 1; i++)
                    stringBuilder.AppendLine(warnings[i].FormatedMessage);
                stringBuilder.Append(warnings.Last().FormatedMessage);
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

        #region Private Methods
        /// <summary>
        /// Get all dataInfo errors.
        /// </summary>
        /// <param name="protocol">Protocol of the dataset the dataInfo belongs to.</param>
        /// <returns>All dataInfo errors.</returns>
        protected virtual IEnumerable<Error> GetErrors()
        {
            List<Error> errors = new();
            errors.AddRange(GetNameErrors());
            errors.AddRange(m_DataContainer.GetErrors());
            return errors;
        }
        /// <summary>
        /// Get all naming-related errors.
        /// </summary>
        /// <returns>All naming-related errors.</returns>
        private IEnumerable<Error> GetNameErrors()
        {
            List<Error> errors = new();
            if (string.IsNullOrEmpty(Name)) errors.Add(new LabelEmptyError());
            return errors;
        }
        /// <summary>
        /// Get all dataInfo warnings.
        /// </summary>
        /// <param name="protocol">Protocol of the dataset the dataInfo belongs to.</param>
        /// <returns>All dataInfo errors.</returns>
        protected virtual IEnumerable<Warning> GetWarnings()
        {
            List<Warning> warnings = new();
            warnings.AddRange(GetNameWarnings());
            warnings.AddRange(m_DataContainer.GetWarnings());
            return warnings;
        }
        /// <summary>
        /// Get all naming-related errors.
        /// </summary>
        /// <returns>All naming-related errors.</returns>
        private IEnumerable<Warning> GetNameWarnings()
        {
            List<Warning> warnings = new();
            return warnings;
        }
        #endregion

        #region Operators
        /// <summary>
        /// Clone this instance.
        /// </summary>
        /// <returns>Clone of this instance.</returns>
        public override object Clone()
        {
            return new DataInfo(Name, Protocol, DataContainer.Clone() as Container.DataContainer, Errors, Warnings, CorrespondingDatabaseID, ID);
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
                Protocol = dataInfo.Protocol;
                DataContainer = dataInfo.DataContainer;
                m_Errors = dataInfo.Errors.ToArray();
                m_Warnings = dataInfo.Warnings.ToArray();
                CorrespondingDatabaseID = dataInfo.CorrespondingDatabaseID;
            }
        }
        #endregion

        #region Serialization
        protected override void OnSerializing()
        {
            base.OnSerializing();
            m_ProtocolID = m_Protocol?.ID;
        }
        #endregion

        #region Interfaces
        public UniTask<IEnumerable<DataInfo>> LoadFromDatabaseAsync(Action<float, float, LoadingText> updateProgress)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}