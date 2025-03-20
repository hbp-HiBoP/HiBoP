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
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using HBP.Core.Enums;
using UnityEditor;
using UnityEngine.UIElements;
using System.Diagnostics;

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
        public const string EXTENSION = ".data";

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
        public ReadOnlyCollection<Error> Errors => new(m_Errors.Concat(m_DataContainer.Errors).ToList());

        [JsonProperty] protected Warning[] m_Warnings = new Warning[0];
        public ReadOnlyCollection<Warning> Warnings => new(m_Warnings.Concat(m_DataContainer.Warnings).ToList());

        /// <summary>
        /// True if the dataInfo is visualizable, False otherwise.
        /// </summary>
        public bool IsOk
        {
            get
            {
                return m_Errors.Length == 0;
            }
        }
        public enum DataState { Error, Warning, Ok }
        public DataState State => m_Errors.Length > 0 ? DataState.Error : m_Warnings.Length > 0 ? DataState.Warning : DataState.Ok;

        public bool RequireErrorCheck { get; set; } = false;
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
        public virtual void CheckErrorsAndWarnings(bool force = false)
        {
            if (RequireErrorCheck || force)
            {
                m_Errors = GetErrors().Distinct().ToArray();
                m_Warnings = GetWarnings().Distinct().ToArray();
                RequireErrorCheck = false;
            }
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

        #region Public Static Methods
        public static async UniTask<IEnumerable<DataInfo>> LoadFromDatabaseAsync(Action<float, float, LoadingText> updateProgress, Func<DataInfo, bool> filter)
        {
            updateProgress(0, 0, new LoadingText("Loading database"));
            await UniTask.WaitUntil(() => DatabaseManager.Database.IsLoaded);
            await UniTask.SwitchToThreadPool();
            var result = new List<DataInfo>();
            int length = DatabaseManager.Database.DataInfos.Count;
            int progress = 0;
            List<DataInfo> dataToDelete = new();
            foreach (var dataInfo in DatabaseManager.Database.DataInfos)
            {
                updateProgress((float)progress++ / length, 0, new LoadingText("Loading data"));
                if (filter(dataInfo))
                {
                    if (dataInfo is PatientDataInfo patientDataInfo)
                    {
                        Patient projectPatient = ApplicationState.LoadedProject.Patients.FirstOrDefault(p => p.ID == patientDataInfo.Patient.ID);
                        if (projectPatient != null)
                        {
                            patientDataInfo.Patient = projectPatient;
                            result.Add(patientDataInfo);
                        }
                    }
                    else
                        result.Add(dataInfo);
                }
            }
            return result;
        }
        public static void LoadFromLocalizersDatabase(DatabaseReference databaseReference, out DataInfo[] dataInfos, Action<float, float, LoadingText> updateProgress, CancellationToken token)
        {
            updateProgress?.Invoke(0, 0, new LoadingText("Finding data to load"));
            dataInfos = new DataInfo[0];
            if (string.IsNullOrEmpty(databaseReference.Path)) return;
            DirectoryInfo directory = new DirectoryInfo(databaseReference.Path);
            if (!directory.Exists) return;
            LocalizerDatabaseParameters parameters = databaseReference.Parameters as LocalizerDatabaseParameters;
            if (parameters == null) return;

            static string GetDownsamplingString(DirectoryInfo dir)
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
            token.ThrowIfCancellationRequested();
            List<DataInfo> dataInfoList = new();
            foreach (var dir in directories)
            {
                token.ThrowIfCancellationRequested();
                updateProgress?.Invoke((float)progress++ / length, 0, new LoadingText("Loading localizer ", dir.Name, " [" + progress + "/" + length + "]"));
                Patient patient = DatabaseManager.Database.Patients.FirstOrDefault(p => p.ID.ToUpper().CompareTo(dir.Name.ToUpper()) == 0);
                if (patient != null)
                {
                    DirectoryInfo[] subDirectories = dir.GetDirectories();
                    foreach (var subdir in subDirectories)
                    {
                        string[] splits = subdir.Name.Split('_');
                        if (splits.Length == 4)
                        {
                            Protocol protocol = DatabaseManager.Database.Protocols.FirstOrDefault(p => p.Name == splits[3]);
                            if (protocol != null)
                            {
                                FileInfo rawEEG = new FileInfo(Path.Combine(subdir.FullName, subdir.Name + ".eeg"));
                                FileInfo rawPos = new FileInfo(Path.Combine(subdir.FullName, subdir.Name + ".pos"));
                                if (rawEEG.Exists && rawPos.Exists)
                                {
                                    var dataInfo = new IEEGDataInfo("raw", protocol, new Container.Elan(rawEEG.FullName, rawPos.FullName, "", new Error[0], new Warning[0]), new Error[0], new Warning[0], patient, NormalizationType.Auto, databaseReference.ID);
                                    dataInfo.CheckErrorsAndWarnings(true);
                                    dataInfoList.Add(dataInfo);
                                }

                                string ds = GetDownsamplingString(subdir);
                                if (!string.IsNullOrEmpty(ds))
                                {
                                    FileInfo posDS = new FileInfo(Path.Combine(subdir.FullName, string.Format("{0}_{1}.pos", subdir.Name, ds)));
                                    if (posDS.Exists)
                                    {
                                        foreach (var freq in parameters.Frequencies)
                                        {
                                            foreach (var ts in parameters.TemporalSmoothings)
                                            {
                                                FileInfo eeg = new FileInfo(Path.Combine(subdir.FullName, string.Format("{0}_{1}", subdir.Name, freq), string.Format("{0}_{1}_{2}_{3}.eeg", subdir.Name, freq, ds, ts)));
                                                if (eeg.Exists)
                                                {
                                                    var dataInfo = new IEEGDataInfo(string.Format("{0}{1}", freq, ts), protocol, new Container.Elan(eeg.FullName, posDS.FullName, "", new Error[0], new Warning[0]), new Error[0], new Warning[0], patient, NormalizationType.Auto, databaseReference.ID);
                                                    dataInfo.CheckErrorsAndWarnings(true);
                                                    dataInfoList.Add(dataInfo);
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
            dataInfos = dataInfoList.ToArray();
            updateProgress?.Invoke(1.0f, 0, new LoadingText("Data loaded successfully"));
        }
        public static void LoadFromBIDSDatabase(DatabaseReference databaseReference, out DataInfo[] dataInfos, Action<float, float, LoadingText> updateProgress, CancellationToken token)
        {
            updateProgress?.Invoke(0, 0, new LoadingText("Finding data to load"));

            dataInfos = new DataInfo[0];
            if (string.IsNullOrEmpty(databaseReference.Path)) return;
            DirectoryInfo databaseDirectoryInfo = new DirectoryInfo(databaseReference.Path);
            if (!databaseDirectoryInfo.Exists) return;

            List<DataInfo> dataInfoList = new();

            // Find all dataInfo files
            Regex brainvisionHeaderRegex = new Regex(@"sub-([a-zA-Z0-9.]+)(_ses-([a-zA-Z0-9.]+))?(_task-([a-zA-Z0-9.]+))(_acq-([a-zA-Z0-9.]+))?(_run-([a-zA-Z0-9.]+))?_ieeg\.vhdr$");
            FileInfo[] brainvisionHeaderFiles = databaseDirectoryInfo.GetFiles("*.vhdr", SearchOption.AllDirectories);
            Regex edfRegex = new Regex(@"sub-([a-zA-Z0-9.]+)(_ses-([a-zA-Z0-9.]+))?(_task-([a-zA-Z0-9.]+))(_acq-([a-zA-Z0-9.]+))?(_run-([a-zA-Z0-9.]+))?_ieeg\.edf$");
            FileInfo[] edfFiles = databaseDirectoryInfo.GetFiles("*.edf", SearchOption.AllDirectories);
            int progress = 0;
            int length = brainvisionHeaderFiles.Length + edfFiles.Length;

            // Brainvision
            foreach (var file in brainvisionHeaderFiles)
            {
                updateProgress?.Invoke((float)progress++ / length, 0, new LoadingText("Loading file ", file.Name, " [" + progress + "/" + length + "]"));
                token.ThrowIfCancellationRequested();
                Match match = brainvisionHeaderRegex.Match(file.FullName);
                if (match.Success)
                {
                    Patient patient = DatabaseManager.Database.Patients.FirstOrDefault(p => p.Name.CompareTo(match.Groups[1].Value) == 0);
                    if (patient != null)
                    {
                        Protocol protocol = DatabaseManager.Database.Protocols.FirstOrDefault(p => p.Name == match.Groups[5].Value);
                        if (protocol != null)
                        {
                            string acq = string.IsNullOrEmpty(match.Groups[7].Value) ? "raw" : match.Groups[7].Value;
                            string run = string.IsNullOrEmpty(match.Groups[9].Value) ? "" : "-" + match.Groups[9].Value;
                            var dataInfo = new IEEGDataInfo(string.Format("{0}{1}", acq, run), protocol, new Container.BrainVision(file.FullName, new Error[0], new Warning[0]), new Error[0], new Warning[0], patient, NormalizationType.Auto, databaseReference.ID);
                            dataInfo.CheckErrorsAndWarnings(true);
                            dataInfoList.Add(dataInfo);
                        }
                    }
                }
            }

            // EDF
            foreach (var file in edfFiles)
            {
                updateProgress?.Invoke((float)progress++ / length, 0, new LoadingText("Loading file ", file.Name, " [" + progress + "/" + length + "]"));
                token.ThrowIfCancellationRequested();
                Match match = edfRegex.Match(file.FullName);
                if (match.Success)
                {
                    Patient patient = ApplicationState.LoadedProject.Patients.FirstOrDefault(p => p.ID.ToUpper().CompareTo(match.Groups[1].Value.ToUpper()) == 0);
                    if (patient != null)
                    {
                        Protocol protocol = DatabaseManager.Database.Protocols.FirstOrDefault(p => p.Name == match.Groups[5].Value);
                        if (protocol != null)
                        {
                            string acq = string.IsNullOrEmpty(match.Groups[4].Value) ? "raw" : match.Groups[4].Value;
                            string run = string.IsNullOrEmpty(match.Groups[5].Value) ? "" : "-" + match.Groups[5].Value;
                            var dataInfo = new IEEGDataInfo(string.Format("{0}{1}", acq, run), protocol, new Container.EDF(file.FullName, new Error[0], new Warning[0]), new Error[0], new Warning[0], patient, NormalizationType.Auto, databaseReference.ID);
                            dataInfo.CheckErrorsAndWarnings(true);
                            dataInfoList.Add(dataInfo);
                        }
                    }
                }
            }

            dataInfos = dataInfoList.ToArray();
            updateProgress?.Invoke(1.0f, 0, new LoadingText("Data loaded successfully"));
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
        protected override void OnDeserialized()
        {
            base.OnDeserialized();
            var protocol = DatabaseManager.Database.Protocols.FirstOrDefault(p => p.ID == m_ProtocolID) ?? DatabaseManager.Database.Protocols.First();
            Protocol = protocol;
        }
        #endregion

        #region Interfaces
        async UniTask<IEnumerable<DataInfo>> ILoadableFromDatabase<DataInfo>.LoadFromDatabaseAsync(Action<float, float, LoadingText> updateProgress, Func<DataInfo, bool> filter)
        {
            return await LoadFromDatabaseAsync(updateProgress, filter);
        }
        #endregion
    }
}