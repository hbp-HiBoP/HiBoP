using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine.Events;
using System.IO;
using System.Collections.ObjectModel;
using System.Collections;
using ThirdParty.CielaSpike;
using System.Text.RegularExpressions;
using HBP.Core.Enums;
using HBP.Core.Exceptions;
using HBP.Core.Interfaces;
using HBP.Core.Tools;

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
    [DataContract]
    public class Dataset : BaseData, ILoadable<Dataset>, ILoadableFromDatabase<Dataset>, INameable
    {
        #region Properties
        public const string EXTENSION = ".dataset";
        /// <summary>
        /// Name of the dataset.
        /// </summary>
        [DataMember] public string Name { get; set; }

        [DataMember(Name = "Protocol", Order = 3)] string m_ProtocolID;
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
        public Dataset() : this("New dataset", ApplicationState.ProjectLoaded.Protocols.FirstOrDefault(), new DataInfo[0], Guid.NewGuid().ToString())
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
                UnityAction action = new UnityAction(() => { data.GetErrorsAndWarnings(Protocol); });
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
                foreach (DataInfo dataInfo in m_Data) dataInfo.GetErrorsAndWarnings(Protocol);
            }
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
        /// <summary>
        /// Loads datasets from localizers database.
        /// </summary>
        /// <param name="path">The specified path of the localizers database.</param>
        /// <param name="datasets">Datasets loaded in the database.</param>
        /// <returns></returns>
        public static void LoadFromLocalizersDatabase(string path, out Dataset[] datasets, Action<float, float, LoadingText> OnChangeProgress = null)
        {
            OnChangeProgress?.Invoke(0, 0, new LoadingText("Finding datasets to load"));
            datasets = new Dataset[0];
            if (string.IsNullOrEmpty(path)) return;
            DirectoryInfo directory = new DirectoryInfo(path);
            if (!directory.Exists) return;

            string GetDownsamplingString(DirectoryInfo dir)
            {
                Regex posRegex = new Regex(dir.Name + @"_(ds[0-9]+)?\.pos$");
                FileInfo[] posFiles = dir.GetFiles("*.pos", SearchOption.AllDirectories);
                string ds = "";
                foreach (var file in posFiles)
                {
                    Match match = posRegex.Match(file.FullName);
                    if (match.Success)
                    {
                        ds = match.Groups[1].Value;
                    }
                }
                return ds;
            }

            IEnumerable<DirectoryInfo> directories = directory.GetDirectories().SelectMany(d => d.GetDirectories());
            int length = directories.Count();
            int progress = 0;
            Dictionary<Protocol, Dataset> datasetByProtocol = new Dictionary<Protocol, Dataset>(ApplicationState.ProjectLoaded.Protocols.Count);
            foreach (var protocol in ApplicationState.ProjectLoaded.Protocols)
            {
                datasetByProtocol.Add(protocol, new Dataset(protocol.Name, protocol, new DataInfo[0]));
            }
            foreach (var dir in directories)
            {
                OnChangeProgress?.Invoke((float)progress++ / length, 0, new LoadingText("Loading localizer ", dir.Name, " [" + (progress + 1) + "/" + length + "]"));
                Patient patient = ApplicationState.ProjectLoaded.Patients.FirstOrDefault(p => p.ID.ToUpper().CompareTo(dir.Name.ToUpper()) == 0);
                if (patient != null)
                {
                    DirectoryInfo[] subDirectories = dir.GetDirectories();
                    foreach (var subdir in subDirectories)
                    {
                        string[] splits = subdir.Name.Split('_');
                        if (splits.Length == 4)
                        {
                            Protocol protocol = ApplicationState.ProjectLoaded.Protocols.FirstOrDefault(p => p.Name == splits[3]);
                            if (protocol != null)
                            {
                                FileInfo rawEEG = new FileInfo(Path.Combine(subdir.FullName, subdir.Name + ".eeg"));
                                FileInfo rawPos = new FileInfo(Path.Combine(subdir.FullName, subdir.Name + ".pos"));
                                if (rawEEG.Exists && rawPos.Exists)
                                    datasetByProtocol[protocol].AddData(new IEEGDataInfo("raw", new Container.Elan(rawEEG.FullName, rawPos.FullName, ""), patient, NormalizationType.Auto));

                                string ds = GetDownsamplingString(subdir);
                                if (!string.IsNullOrEmpty(ds))
                                {
                                    FileInfo posDS = new FileInfo(Path.Combine(subdir.FullName, string.Format("{0}_{1}.pos", subdir.Name, ds)));
                                    if (posDS.Exists)
                                    {
                                        // Maybe TODO : parameters (specific UI or user preferences)
                                        string[] frequencies = new string[] { "f8f24", "f50f150" };
                                        string[] temporalSmoothings = new string[] { "sm0", "sm250", "sm500", "sm1000", "sm2500", "sm5000" };
                                        foreach (var freq in frequencies)
                                        {
                                            foreach (var ts in temporalSmoothings)
                                            {
                                                FileInfo eeg = new FileInfo(Path.Combine(subdir.FullName, string.Format("{0}_{1}", subdir.Name, freq), string.Format("{0}_{1}_{2}_{3}.eeg", subdir.Name, freq, ds, ts)));
                                                if (eeg.Exists)
                                                {
                                                    datasetByProtocol[protocol].AddData(new IEEGDataInfo(string.Format("{0}{1}", freq, ts), new Container.Elan(eeg.FullName, posDS.FullName, ""), patient, NormalizationType.Auto));
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            datasets = datasetByProtocol.Values.OrderBy(d => d.Name).ToArray();
            OnChangeProgress?.Invoke(1.0f, 0, new LoadingText("Datasets loaded successfully"));
        }
        /// <summary>
        /// Loads datasets from BIDS database.
        /// </summary>
        /// <param name="path">The specified path of the BIDS database.</param>
        /// <param name="datasets"></param>
        /// <returns></returns>
        public static void LoadFromBIDSDatabase(string path, out Dataset[] datasets, Action<float, float, LoadingText> OnChangeProgress = null)
        {
            datasets = new Dataset[0];
            if (string.IsNullOrEmpty(path)) return;
            DirectoryInfo databaseDirectoryInfo = new DirectoryInfo(path);
            if (!databaseDirectoryInfo.Exists) return;

            Dictionary<Protocol, Dataset> datasetByProtocol = new Dictionary<Protocol, Dataset>(ApplicationState.ProjectLoaded.Protocols.Count);
            foreach (var protocol in ApplicationState.ProjectLoaded.Protocols)
            {
                datasetByProtocol.Add(protocol, new Dataset(protocol.Name, protocol, new DataInfo[0]));
            }

            // Brainvision
            Regex brainvisionHeaderRegex = new Regex(@"sub-([a-zA-Z0-9.]+)(_ses-([a-zA-Z0-9.]+))?(_task-([a-zA-Z0-9.]+))(_acq-([a-zA-Z0-9.]+))?(_run-([a-zA-Z0-9.]+))?_ieeg\.vhdr$");
            FileInfo[] brainvisionHeaderFiles = databaseDirectoryInfo.GetFiles("*.vhdr", SearchOption.AllDirectories);
            foreach (var file in brainvisionHeaderFiles)
            {
                Match match = brainvisionHeaderRegex.Match(file.FullName);
                if (match.Success)
                {
                    Patient patient = ApplicationState.ProjectLoaded.Patients.FirstOrDefault(p => p.Name.CompareTo(match.Groups[1].Value) == 0);
                    if (patient != null)
                    {
                        Protocol protocol = ApplicationState.ProjectLoaded.Protocols.FirstOrDefault(p => p.Name == match.Groups[5].Value);
                        if (protocol != null)
                        {
                            string acq = string.IsNullOrEmpty(match.Groups[7].Value) ? "raw" : match.Groups[7].Value;
                            string run = string.IsNullOrEmpty(match.Groups[9].Value) ? "" : "-" + match.Groups[9].Value;
                            datasetByProtocol[protocol].AddData(new IEEGDataInfo(string.Format("{0}{1}", acq, run), new Container.BrainVision(file.FullName), patient, NormalizationType.Auto));
                        }
                    }
                }
            }

            // EDF
            Regex edfRegex = new Regex(@"sub-([a-zA-Z0-9.]+)(_ses-([a-zA-Z0-9.]+))?(_task-([a-zA-Z0-9.]+))(_acq-([a-zA-Z0-9.]+))?(_run-([a-zA-Z0-9.]+))?_ieeg\.edf$");
            FileInfo[] edfFiles = databaseDirectoryInfo.GetFiles("*.edf", SearchOption.AllDirectories);
            foreach (var file in edfFiles)
            {
                Match match = edfRegex.Match(file.FullName);
                if (match.Success)
                {
                    Patient patient = ApplicationState.ProjectLoaded.Patients.FirstOrDefault(p => p.ID.ToUpper().CompareTo(match.Groups[1].Value.ToUpper()) == 0);
                    if (patient != null)
                    {
                        Protocol protocol = ApplicationState.ProjectLoaded.Protocols.FirstOrDefault(p => p.Name == match.Groups[3].Value);
                        if (protocol != null)
                        {
                            string acq = string.IsNullOrEmpty(match.Groups[4].Value) ? "raw" : match.Groups[4].Value;
                            string run = string.IsNullOrEmpty(match.Groups[5].Value) ? "" : "-" + match.Groups[5].Value;
                            datasetByProtocol[protocol].AddData(new IEEGDataInfo(string.Format("{0}{1}", acq, run), new Container.EDF(file.FullName), patient, NormalizationType.Auto));
                        }
                    }
                }
            }

            datasets = datasetByProtocol.Values.OrderBy(d => d.Name).ToArray();
            OnChangeProgress?.Invoke(1.0f, 0, new LoadingText("Datasets loaded successfully"));
        }
        /// <summary>
        /// Coroutine to load datasets from database. Implementation of ILoadableFromDatabase.
        /// </summary>
        /// <param name="path">The specified path of the dataset file.</param>
        /// <param name="OnChangeProgress">Action called on change progress.</param>
        /// <param name="result">The datasets loaded.</param>
        /// <returns></returns>
        public static IEnumerator c_LoadFromDatabase(string path, Action<float, float, LoadingText> OnChangeProgress, Action<IEnumerable<Dataset>> result)
        {
            yield return Ninja.JumpBack;
            Dataset[] datasets;
            if (IsBIDSDirectory(path)) LoadFromBIDSDatabase(path, out datasets, OnChangeProgress);
            else LoadFromLocalizersDatabase(path, out datasets, OnChangeProgress);
            yield return Ninja.JumpToUnity;
            result(datasets);
        }
        #endregion

        #region Private Static Methods
        /// <summary>
        /// Checks if the input directory is a BIDS database
        /// </summary>
        /// <param name="path">Path to the input database</param>
        /// <returns>True if the input database is a BIDS database</returns>
        private static bool IsBIDSDirectory(string path)
        {
            FileInfo participantsFileInfo = new FileInfo(Path.Combine(path, "participants.tsv"));
            return participantsFileInfo.Exists;
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
                UnityAction action = new UnityAction(() => data.GetErrorsAndWarnings(Protocol));
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
        IEnumerator ILoadableFromDatabase<Dataset>.LoadFromDatabase(string path, Action<float, float, LoadingText> OnChangeProgress, Action<IEnumerable<Dataset>> result)
        {
            yield return Ninja.JumpToUnity;
            yield return CoroutineManager.StartAsync(c_LoadFromDatabase(path, OnChangeProgress, result));
            yield return Ninja.JumpBack;
        }
        #endregion
    }
}