using Cysharp.Threading.Tasks;
using HBP.Core.Enums;
using HBP.Core.Errors;
using HBP.Core.Exceptions;
using HBP.Core.Interfaces;
using HBP.Core.Tools;
using HBP.Core.Database;
using HBP.Core.Preferences;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;
using UnityEngine.Scripting;

namespace HBP.Core.Data
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
    [JsonObject(MemberSerialization.OptIn), Preserve]
    public class Dataset : BaseData, ILoadable<Dataset>, INameable
    {
        #region Properties
        public const string EXTENSION = ".dataset";
        /// <summary>
        /// Name of the dataset.
        /// </summary>
        [JsonProperty] public string Name { get; set; }

        [JsonProperty("Protocol", Order = 3)] string m_ProtocolID;
        Protocol m_Protocol;
        /// <summary>
        /// Protocol used during the experiment.
        /// </summary>
        public Protocol Protocol
        {
            get
            {
                return m_Protocol;
            }
            set
            {
                m_Protocol = value;

                if (m_Data != null)
                    foreach (DataInfo dataInfo in m_Data)
                        dataInfo.Protocol = value;
            }
        }

        [JsonProperty("Data", Order = 4)] List<DataInfo> m_Data;
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
        public Dataset(string name, Protocol protocol, IEnumerable<DataInfo> data, string ID) : base(ID)
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
        public Dataset(string name, Protocol protocol, IEnumerable<DataInfo> data) : base()
        {
            Name = name;
            Protocol = protocol;
            SetData(data);
        }
        /// <summary>
        /// Create a new Dataset instance with default values.
        /// </summary>
        public Dataset() : this("New dataset", null, new DataInfo[0])
        {
        }
        #endregion

        #region Public Methods
        internal void ResolveReferences(LoadingContext context)
        {
            m_Protocol = context.ResolveRequired(
                context.ProtocolById,
                m_ProtocolID ?? m_Protocol?.ID,
                "protocol",
                $"Dataset '{ID}'");

            foreach (DataInfo dataInfo in m_Data ?? Enumerable.Empty<DataInfo>())
            {
                dataInfo.ResolveReferences(context);
            }
        }

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
        public FMRIDataInfo[] GetFMRIDataInfos()
        {
            return m_Data.OfType<FMRIDataInfo>().ToArray();
        }
        public SharedFMRIDataInfo[] GetSharedFMRIDataInfos()
        {
            return m_Data.OfType<SharedFMRIDataInfo>().ToArray();
        }
        public StaticDataInfo[] GetStaticDataInfos()
        {
            return m_Data.OfType<StaticDataInfo>().ToArray();
        }
        public PatientDataInfo[] GetMEGDataInfos()
        {
            return m_Data.Where(d => d is MEGcDataInfo || d is MEGvDataInfo).Select(d => d as PatientDataInfo).ToArray();
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
            bool result = true;
            foreach (var d in data)
            {
                result &= RemoveData(d);
            }
            return result;
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
            return data.All((d) => AddData(d));
        }
        public override void GenerateID()
        {
            base.GenerateID();
            foreach (var dataInfo in Data) dataInfo.GenerateID();
        }
        public override List<BaseData> GetAllIdentifiable()
        {
            List<BaseData> IDs = base.GetAllIdentifiable();
            foreach (var dataInfo in Data) IDs.AddRange(dataInfo.GetAllIdentifiable());
            return IDs;
        }
        #endregion

        #region Public static Methods
        /// <summary>
        /// Get all the extensions of dataset file.
        /// </summary>
        /// <returns></returns>
        public static string[] GetExtensions()
        {
            return new string[] { EXTENSION[0] == '.' ? EXTENSION[1..] : EXTENSION };
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
                if (result == null)
                {
                    return false;
                }
                IEnumerable<Patient> patients = ApplicationState.LoadedProject?.Patients
                    ?? DatabaseManager.Database.Patients;
                LoadingContext context = new(
                    Array.Empty<BaseTag>(),
                    DatabaseManager.Database.Protocols,
                    patients,
                    new[] { result });
                context.ResolveProject(
                    patients,
                    Array.Empty<Group>(),
                    new[] { result },
                    Array.Empty<Visualization>());
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw new CanNotReadDatasetFileException(Path.GetFileNameWithoutExtension(path));
            }
        }
        public static async UniTask<DataInfoValidationResult> ValidateDataInfosAsync(
            IEnumerable<DataInfo> dataInfos,
            bool force,
            Action<float, float, LoadingText> updateProgress,
            CancellationToken token = default,
            long generation = 0)
        {
            int concurrency = PersistentDataManager.UserPreferences.General.System.MultiThreading ? 20 : 1;
            return await new DataInfoValidator().ValidateAsync(
                dataInfos,
                force,
                concurrency,
                token,
                (completed, total) => updateProgress(
                    total == 0 ? 1 : (float)completed / total,
                    completed == 0 ? 0 : 0.2f,
                    total == 0
                        ? new LoadingText("Checking data")
                        : new LoadingText("Checking data", " ", completed + "/" + total)),
                generation);
        }

        public static async UniTask CheckDatasetsAsync(
            IEnumerable<DataInfo> dataInfos,
            bool force,
            Action<float, float, LoadingText> updateProgress,
            CancellationToken token = default)
        {
            DataInfoValidationResult result = await ValidateDataInfosAsync(
                dataInfos,
                force,
                updateProgress,
                token);
            token.ThrowIfCancellationRequested();
            await UniTask.SwitchToMainThread();
            result.TryApply(0);
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
            m_Data ??= new List<DataInfo>();
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
