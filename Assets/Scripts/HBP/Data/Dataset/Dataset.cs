using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine.Events;
using Tools.Unity;
using System.IO;
using Tools.CSharp;
using System.Collections.ObjectModel;

namespace HBP.Data.Experience.Dataset
{
    /// <summary>
    /// Class which contains data of the experiment.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader>
    /// <term>Data</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term><b>ID</b></term>
    /// <description>Unique identifier.</description>
    /// </item>
    /// <item>
    /// <term><b>Name</b></term>
    /// <description>Name of the dataset.</description>
    /// </item>
    /// <item>
    /// <term><b>Protocol</b></term>
    /// <description>Protocol used during the experiment.</description>
    /// </item>
    /// <item>
    /// <term><b>Data</b></term>
    /// <description>DataInfo of the dataset.</description>
    /// </item>
    /// </list>
    /// </remarks>
    [DataContract]
    public class Dataset : BaseData, ILoadable<Dataset>, INameable
    {
        #region Properties
        public const string EXTENSION = ".dataset";
        /// <summary>
        /// Name of the dataset.
        /// </summary>
        [DataMember] public string Name { get; set; }

        [DataMember(Name = "Protocol", Order = 3)] string m_ProtocolID;
        Protocol.Protocol m_Protocol;
        /// <summary>
        /// Protocol used during the experiment.
        /// </summary>
        public Protocol.Protocol Protocol
        {
            get
            {
                return m_Protocol;
            }
            set
            {
                m_Protocol = value;
                UpdateDataStates();
            }
        }

        Dictionary<DataInfo, UnityAction> m_ActionByDataInfo;
        [DataMember(Order = 4, Name = "Data")] List<DataInfo> m_Data;
        /// <summary>
        /// DataInfo of the dataset.
        /// </summary>
        public ReadOnlyCollection<DataInfo> Data
        {
            get
            {
                return new ReadOnlyCollection<DataInfo>(m_Data);
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new Dataset instance.
        /// </summary>
        /// <param name="name">Name of the dataset</param>
        /// <param name="protocol">Protocol used during the experiment</param>
        /// <param name="data">DataInfo of the dataset</param>
        /// <param name="ID">Unique identifier</param>
        public Dataset(string name, Protocol.Protocol protocol, IEnumerable<DataInfo> data, string ID) : base(ID)
        {
            Name = name;
            Protocol = protocol;
            SetData(data);
        }
        /// <summary>
        /// Create a new Dataset instance.
        /// </summary>
        /// <param name="name">Name of the dataset</param>
        /// <param name="protocol">Protocol used during the experiment</param>
        /// <param name="data">DataInfo of the dataset</param>
        public Dataset(string name, Protocol.Protocol protocol, IEnumerable<DataInfo> data) : base()
        {
            Name = name;
            Protocol = protocol;
            SetData(data);
        }
        /// <summary>
        /// Create a new Dataset instance with default values.
        /// </summary>
        public Dataset() : this("New dataset", ApplicationState.ProjectLoaded.Protocols.First(), new DataInfo[0], Guid.NewGuid().ToString())
        {
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Get all the patient dataInfo.
        /// </summary>
        /// <returns>Patient dataInfo</returns>
        public PatientDataInfo[] GetPatientDataInfos()
        {
            return m_Data.OfType<PatientDataInfo>().ToArray();
        }
        /// <summary>
        /// Get all the patient dataInfo for a specified patient.
        /// </summary>
        /// <param name="patient">Patient</param>
        /// <returns>Patient dataInfo</returns>
        public PatientDataInfo[] GetPatientDataInfos(Patient patient)
        {
            return m_Data.OfType<PatientDataInfo>().Where(d => d.Patient == patient).ToArray();
        }
        /// <summary>
        /// Get all the IEEG dataInfo.
        /// </summary>
        /// <returns>IEEG dataInfo</returns>
        public IEEGDataInfo[] GetIEEGDataInfos()
        {
            return m_Data.OfType<IEEGDataInfo>().ToArray();
        }
        /// <summary>
        /// Get all the CCEP dataInfo.
        /// </summary>
        /// <returns>CCEP dataInfo</returns>
        public CCEPDataInfo[] GetCCEPDataInfos()
        {
            return m_Data.OfType<CCEPDataInfo>().ToArray();
        }
        /// <summary>
        /// Add a dataInfo to the dataset.
        /// </summary>
        /// <param name="data">DataInfo to add</param>
        /// <returns>True if its worked, False otherwise</returns>
        public bool AddData(DataInfo data)
        {
            if (!m_Data.Contains(data))
            {
                m_Data.Add(data);
                UnityAction action = new UnityAction(() => { data.GetErrors(Protocol); });
                m_ActionByDataInfo.Add(data, action);
                data.OnRequestErrorCheck.AddListener(action);
                return true;
            }
            else return false;
        }
        /// <summary>
        /// Add dataInfo to the dataset.
        /// </summary>
        /// <param name="data">DataInfo to add</param>
        /// <returns>True if its worked, False otherwise</returns>
        public bool AddData(IEnumerable<DataInfo> data)
        {
            return data.All((d) => AddData(d));
        }
        /// <summary>
        /// Remove a dataInfo from the dataset.
        /// </summary>
        /// <param name="data">DataInfo to remove</param>
        /// <returns>True if its worked, False otherwise</returns>
        public bool RemoveData(DataInfo data)
        {
            if (m_Data.Contains(data))
            {
                m_Data.Remove(data);
                data.OnRequestErrorCheck.RemoveListener(m_ActionByDataInfo[data]);
                m_ActionByDataInfo.Remove(data);
                return true;
            }
            else return false;
        }
        /// <summary>
        /// remove dataInfo from the dataset.
        /// </summary>
        /// <param name="data">DataInfo to remove.</param>
        /// <returns>True if its worked, False otherwise</returns>
        public bool RemoveData(IEnumerable<DataInfo> data)
        {
            return data.All((d) => RemoveData(d));
        }
        /// <summary>
        /// Update a specified dataInfo.
        /// </summary>
        /// <param name="data">DataInfo to update</param>
        /// <returns>True if its worked, False otherwise</returns>
        public bool UpdateData(DataInfo data)
        {
            int index = m_Data.FindIndex(d => d.Equals(data));
            if (index != -1)
            {
                m_Data[index] = data;
                return true;
            }
            return false;
        }
        /// <summary>
        /// Set the data of the dataset.
        /// </summary>
        /// <param name="data">DataInfo of the dataset.</param>
        /// <returns>True if its worked, False otherwise</returns>
        public bool SetData(IEnumerable<DataInfo> data)
        {
            m_Data = new List<DataInfo>();
            m_ActionByDataInfo = new Dictionary<DataInfo, UnityAction>();
            return data.All((d) => AddData(d));
        }
        /// <summary>
        /// Update all the dataInfo states.
        /// </summary>
        public void UpdateDataStates()
        {
            if (m_Data != null)
            {
                foreach (DataInfo dataInfo in m_Data) dataInfo.GetErrors(Protocol);
            }
        }
        public override void GenerateID()
        {
            base.GenerateID();
            foreach (var dataInfo in Data) dataInfo.GenerateID();
        }
        #endregion

        #region Public static Methods
        /// <summary>
        /// Get all the extensions of dataset file.
        /// </summary>
        /// <returns></returns>
        public static string[] GetExtensions()
        {
            return new string[] { EXTENSION[0] == '.' ? EXTENSION.Substring(1) : EXTENSION };
        }
        /// <summary>
        /// Load a dataset from a specified file.
        /// </summary>
        /// <param name="path">Path of the file</param>
        /// <param name="result">Dataset loaded from the file</param>
        /// <returns>True if its worked, False otherwise</returns>
        public static bool LoadFromFile(string path, out Dataset result)
        {
            result = null;
            try
            {
                result = ClassLoaderSaver.LoadFromJson<Dataset>(path);
                return result != null;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
                throw new CanNotReadDatasetFileException(Path.GetFileNameWithoutExtension(path));
            }
        }
        #endregion

        #region Operators
        /// <summary>
        /// Clone this instance.
        /// </summary>
        /// <returns>Clone of this instance</returns>
        public override object Clone()
        {
            return new Dataset(Name, Protocol, Data.DeepClone(), ID);
        }
        /// <summary>
        /// Copy this a instance to this instance.
        /// </summary>
        /// <param name="copy">Instance to copy.</param>
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is Dataset dataset)
            {
                Name = dataset.Name;
                Protocol = dataset.Protocol;
                SetData(dataset.Data);
            }
        }
        #endregion

        #region Serialization
        protected override void OnSerializing()
        {
            base.OnSerializing();
            m_ProtocolID = m_Protocol?.ID;
        }
        protected override void OnDeserialized()
        {
            base.OnDeserialized();
            var protocol = ApplicationState.ProjectLoaded.Protocols.FirstOrDefault(p => p.ID == m_ProtocolID);
            if (protocol == null) protocol = ApplicationState.ProjectLoaded.Protocols.First();
            Protocol = protocol;
            foreach (var data in m_Data)
            {
                UnityAction action = new UnityAction(() => data.GetErrors(Protocol));
                m_ActionByDataInfo.Add(data, action);
                data.OnRequestErrorCheck.AddListener(action);
            }
        }
        #endregion

        #region Interfaces
        /// <summary>
        /// Get all the extensions of dataset file.
        /// </summary>
        /// <returns></returns>
        string[] ILoadable<Dataset>.GetExtensions()
        {
            return GetExtensions();
        }
        /// <summary>
        /// Load a dataset from a specified file.
        /// </summary>
        /// <param name="path">Path of the file</param>
        /// <param name="result">Dataset loaded from the file</param>
        /// <returns>True if its worked, False otherwise</returns>
        bool ILoadable<Dataset>.LoadFromFile(string path, out Dataset[] result)
        {
            bool success = LoadFromFile(path, out Dataset dataset);
            result = new Dataset[] { dataset };
            return success;
        }
        #endregion
    }
}