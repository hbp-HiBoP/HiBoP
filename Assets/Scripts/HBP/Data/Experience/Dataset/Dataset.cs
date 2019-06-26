using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine.Events;
using Tools.Unity;
using System.IO;

namespace HBP.Data.Experience.Dataset
{
    /**
    * \class Dataset
    * \author Adrien Gannerie
    * \version 1.0
    * \date 09 janvier 2017
    * \brief Dataset which contains data of the experiment.
    * 
    * \details Class which contains data of the experiment :
    *       - \a Name.
    *       - \a DataInfo of the experiment.
    *       - \a Unique ID.
    */
    [DataContract]
	public class Dataset : ICloneable, ICopiable, ILoadable, IIdentifiable
	{
        #region Properties
        public const string EXTENSION = ".dataset";

        /// <summary>
        /// Unique ID of the dataset.
        /// </summary>
        [DataMember] public string ID { get; set; }

        /// <summary>
        /// Name of the dataset.
        /// </summary>
        [DataMember] public string Name { get; set; }
        
        [DataMember(Name = "Protocol", Order = 3)] string m_ProtocolID;
        /// <summary>
        /// Protocol used during the experiment.
        /// </summary>
        public Protocol.Protocol Protocol
        {
            get
            {
                return ApplicationState.ProjectLoaded.Protocols.FirstOrDefault(p => p.ID == m_ProtocolID);
            }
            set
            {
                m_ProtocolID = value.ID;
                UpdateDataStates();
            }
        }

        Dictionary<DataInfo, UnityAction> m_ActionByDataInfo;
        [DataMember(Order = 4,Name = "Data")] List<DataInfo> m_Data;
        /// <summary>
        /// DataInfo of the dataset.
        /// </summary>
        public DataInfo[] Data
        {
            get
            {
                return m_Data.ToArray();
            }
        }
        #endregion

        #region Constructor
        public Dataset(string name,Protocol.Protocol protocol, DataInfo[] data,string id)
        {
            Name = name;
            Protocol = protocol;
            SetData(data);
            ID = id;
        }
        public Dataset() : this("New dataset", ApplicationState.ProjectLoaded.Protocols.First(), new DataInfo[0], Guid.NewGuid().ToString())
		{
        }
        #endregion

        #region Public Methods
        public PatientDataInfo[] GetPatientDataInfos()
        {
            return m_Data.OfType<PatientDataInfo>().ToArray();
        }
        public PatientDataInfo[] GetPatientDataInfos(Patient patient)
        {
            return m_Data.OfType<PatientDataInfo>().Where(d => d.Patient == patient).ToArray();
        }
        public iEEGDataInfo[] GetIEEGDataInfos()
        {
            return m_Data.OfType<iEEGDataInfo>().ToArray();
        }
        public CCEPDataInfo[] GetCCEPDataInfos()
        {
            return m_Data.OfType<CCEPDataInfo>().ToArray();
        }

        public void Load(string path)
        {
            Dataset result;
            try
            {
                result = ClassLoaderSaver.LoadFromJson<Dataset>(path);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
                throw new CanNotReadDatasetFileException(Path.GetFileNameWithoutExtension(path));
            }
            Copy(result);
        }
        public string GetExtension()
        {
            return EXTENSION[0] == '.' ? EXTENSION.Substring(1) : EXTENSION;
        }
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
        public bool AddData(IEnumerable<DataInfo> data)
        {
            return data.All((d) => AddData(d));
        }
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
        public bool RemoveData(IEnumerable<DataInfo> data)
        {
            return data.All((d) => RemoveData(d));
        }
        public bool SetData(IEnumerable<DataInfo> data)
        {
            m_Data = new List<DataInfo>();
            m_ActionByDataInfo = new Dictionary<DataInfo, UnityAction>();
            return data.All((d) => AddData(d));
        }
        /// <summary>
        /// UpdateDataStates.
        /// </summary>
        public void UpdateDataStates()
        {
            if (m_Data != null)
            {
                foreach (DataInfo dataInfo in m_Data) dataInfo.GetErrors(Protocol);
            }
        }
        #endregion

        #region Operators
        /// <summary>
        /// Clone this instance.
        /// </summary>
        /// <returns>Clone of this instance</returns>
        public object Clone()
        {
            return new Dataset(Name ,Protocol ,Data.Clone() as DataInfo[], ID);
        }
        /// <summary>
        /// Copy this a instance to this instance.
        /// </summary>
        /// <param name="copy">Instance to copy.</param>
        public void Copy(object copy)
        {
            Dataset dataset = copy as Dataset;
            Name = dataset.Name;
            Protocol = dataset.Protocol;
            ID = dataset.ID;
            SetData(dataset.Data);
        }
        /// <summary>
        /// Operator Equals.
        /// </summary>
        /// <param name="obj">Object to test.</param>
        /// <returns>\a True if equals and \a false otherwise.</returns>
        public override bool Equals(object obj)
        {
            Dataset dataset = obj as Dataset;
            if (dataset != null && dataset.ID == ID)
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
        /// <param name="a">Dataset to compare.</param>
        /// <param name="b">Dataset to compare.</param>
        /// <returns>\a True if equals and \a false otherwise.</returns>
        public static bool operator ==(Dataset a, Dataset b)
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
        /// <param name="a">Dataset to compare.</param>
        /// <param name="b">Dataset to compare.</param>
        /// <returns>\a True if not equals and \a false otherwise.</returns>
        public static bool operator !=(Dataset a, Dataset b)
        {
            return !(a == b);
        }
        #endregion

        #region Serialization
        [OnDeserialized()]
        void SetListeners(StreamingContext context)
        {
            if (Protocol == null) Protocol = ApplicationState.ProjectLoaded.Protocols.First();
            foreach (var data in m_Data)
            {
                UnityAction action = new UnityAction(() => data.GetErrors(Protocol));
                m_ActionByDataInfo.Add(data, action);
                data.OnRequestErrorCheck.AddListener(action);
            }
        }
        #endregion

        #region Struct
        public struct Resume
        {
            public enum StateEnum { OK, Warning, Error }
            public string Label;
            public int Number;
            public StateEnum State;
        }
        #endregion
    }
}