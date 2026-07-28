using Cysharp.Threading.Tasks;
using HBP.Core.Enums;
using HBP.Core.Errors;
using HBP.Core.Interfaces;
using HBP.Core.Tools;
using HBP.Core.Database;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine.Events;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

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
    [JsonObject(MemberSerialization.OptIn), Preserve]
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
        public ReadOnlyCollection<Error> Errors => new(
            m_Errors
                .Concat(m_DataContainer.Errors)
                .GroupBy(error => (
                    error.GetType(),
                    error.Title,
                    error.Message))
                .Select(group => group.First())
                .ToList());

        [JsonProperty] protected Warning[] m_Warnings = new Warning[0];
        public ReadOnlyCollection<Warning> Warnings => new(
            m_Warnings
                .Concat(m_DataContainer.Warnings)
                .GroupBy(warning => (
                    warning.GetType(),
                    warning.Title,
                    warning.Message))
                .Select(group => group.First())
                .ToList());

        [JsonProperty("ValidationStates")]
        protected List<ValidationState> m_ValidationStates = new();
        [JsonIgnore]
        public ReadOnlyCollection<ValidationState> ValidationStates =>
            new((m_ValidationStates ?? new List<ValidationState>())
                .Select(state => state.Clone())
                .ToList());

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
        public enum DataState { Error, Warning, Ok }
        public DataState State => Errors.Count > 0 ? DataState.Error : Warnings.Count > 0 ? DataState.Warning : DataState.Ok;

        public bool RequireErrorCheck { get; set; } = false;
        [JsonIgnore]
        public ValidationRequest PendingValidationRequest { get; set; }
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
        public DataInfo() : this("Data", null, new Container.Elan(), new Error[0], new Warning[0], "")
        {
        }
        #endregion

        #region Public Methods
        internal virtual void ResolveReferences(LoadingContext context)
        {
            m_Protocol = context.ResolveRequired(
                context.ProtocolById,
                m_ProtocolID ?? m_Protocol?.ID,
                "protocol",
                $"{GetType().Name} '{ID}'");
        }

        public virtual void CheckErrorsAndWarnings(bool force = false)
        {
            CheckErrorsAndWarnings(
                new ValidationRequest(
                    ValidationAspect.DataInfoAll,
                    force: force),
                force);
        }

        public void CheckErrorsAndWarnings(
            ValidationRequest request,
            bool force = false)
        {
            CheckErrorsAndWarnings(request, force, null);
        }

        internal void CheckErrorsAndWarnings(
            ValidationRequest request,
            bool force,
            IEEGValidationMetadataReader metadataReader)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (!request.Matches(this))
            {
                return;
            }

            EnsureValidationStates();
            DataInfoValidationContext context =
                new(this, metadataReader);
            bool validateRequested =
                RequireErrorCheck ||
                force ||
                request.Force;

            foreach (ValidationAspect aspect in AtomicDataInfoAspects)
            {
                if (!request.Matches(this, aspect))
                {
                    continue;
                }
                if (!validateRequested && IsValidationCurrent(aspect, request))
                {
                    continue;
                }

                if (aspect == ValidationAspect.SourceAvailability)
                {
                    string previousSignature = GetValidationState(
                        ValidationAspect.SourceAvailability,
                        string.Empty)?.Signature;
                    m_DataContainer.GetErrors();
                    m_DataContainer.GetWarnings();
                    ReplaceValidationStates(
                        aspect,
                        request,
                        GetValidationStates(aspect, request, context));
                    if (!string.IsNullOrEmpty(previousSignature) &&
                        !string.Equals(
                            previousSignature,
                            context.SourceSignature,
                            StringComparison.Ordinal))
                    {
                        MarkValidationStale(
                            ValidationAspect.SourceReadability |
                            ValidationAspect.StaticContent |
                            ValidationAspect.Epoching |
                            ValidationAspect.ChannelMapping);
                    }
                    continue;
                }

                ReplaceValidationStates(
                    aspect,
                    request,
                    GetValidationStates(aspect, request, context));
            }

            RefreshFlattenedIssues();
            RequireErrorCheck = false;
        }

        internal DataInfo CreateValidationSnapshot(
            ValidationRequest request,
            bool force,
            IEEGValidationMetadataReader metadataReader = null)
        {
            if (!request.Matches(this) ||
                (!RequireErrorCheck &&
                    !force &&
                    !request.Force &&
                    !HasStaleValidation(request)))
            {
                return null;
            }

            DataInfo snapshot = Clone() as DataInfo;
            if (snapshot == null)
            {
                throw new InvalidOperationException(
                    $"{GetType().Name}.Clone() did not return a DataInfo.");
            }
            CopyValidationStateTo(snapshot);
            snapshot.CheckErrorsAndWarnings(
                request,
                force,
                metadataReader);
            return snapshot;
        }

        internal DataInfo CreateValidationSnapshot(bool force)
        {
            return CreateValidationSnapshot(
                new ValidationRequest(
                    ValidationAspect.DataInfoAll,
                    force: force),
                force);
        }

        internal virtual void ApplyValidationState(DataInfo validatedSnapshot)
        {
            m_Errors = validatedSnapshot.m_Errors.ToArray();
            m_Warnings = validatedSnapshot.m_Warnings.ToArray();
            m_ValidationStates = validatedSnapshot.m_ValidationStates
                .Select(state => state.Clone())
                .ToList();
            m_DataContainer.ApplyValidationState(validatedSnapshot.m_DataContainer);
            RequireErrorCheck = false;
        }

        public bool IsValidationCurrent(
            ValidationAspect aspects,
            ValidationRequest request = null)
        {
            EnsureValidationStates();
            request ??= new ValidationRequest(aspects);
            foreach (ValidationAspect aspect in AtomicDataInfoAspects)
            {
                if ((aspects & aspect) == 0)
                {
                    continue;
                }
                if (!request.Matches(this, aspect))
                {
                    continue;
                }
                IReadOnlyCollection<string> targetedSubBlocIDs =
                    request.GetTargetedSubBlocIDs(this);
                IEnumerable<ValidationState> states = m_ValidationStates
                    .Where(state =>
                        state.Aspect == aspect &&
                        (aspect != ValidationAspect.Epoching ||
                            targetedSubBlocIDs.Count == 0 ||
                            targetedSubBlocIDs.Contains(state.ScopeID)));
                if (!states.Any() ||
                    states.Any(state =>
                        state.Status != ValidationStatus.Current &&
                        state.Status != ValidationStatus.NotApplicable))
                {
                    return false;
                }
            }
            return true;
        }

        public void MarkValidationStale(ValidationAspect aspects)
        {
            EnsureValidationStates();
            for (int i = 0; i < m_ValidationStates.Count; i++)
            {
                ValidationState state = m_ValidationStates[i];
                if ((state.Aspect & aspects) != 0 &&
                    state.Status != ValidationStatus.NotApplicable)
                {
                    m_ValidationStates[i] =
                        state.WithStatus(ValidationStatus.Stale);
                }
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
            GlobalDatabase database = DatabaseManager.Database;
            float databaseWeight = database.NeedsReadyWait ? 0.8f : 0;
            if (databaseWeight > 0)
            {
                await database.EnsureDatabaseReadyAsync(
                    (progress, duration, text) => updateProgress(
                        progress * databaseWeight,
                        duration,
                        text));
            }
            await UniTask.SwitchToThreadPool();
            var result = new List<DataInfo>();
            int length = database.DataInfos.Count;
            int progress = 0;
            List<DataInfo> dataToDelete = new();
            foreach (var dataInfo in database.DataInfos)
            {
                updateProgress(
                    databaseWeight +
                        (length == 0 ? 1 : (float)progress++ / length) *
                        (1 - databaseWeight),
                    0,
                    new LoadingText("Loading data"));
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
        public static void LoadFromLocalizersDatabase(DatabaseReference databaseReference, List<Patient> patients, out DataInfo[] dataInfos, Action<float, float, LoadingText> updateProgress, CancellationToken token)
        {
            updateProgress?.Invoke(0, 0, new LoadingText("Finding data to load"));
            dataInfos = new DataInfo[0];
            if (string.IsNullOrEmpty(databaseReference.Path)) return;
            DirectoryInfo directory = new(databaseReference.Path);
            if (!directory.Exists) return;
            LocalizerDatabaseParameters parameters = databaseReference.Parameters as LocalizerDatabaseParameters;
            if (parameters == null) return;

            static string GetDownsamplingString(DirectoryInfo dir)
            {
                Regex posRegex = new(dir.Name + @"_(ds[0-9]+)?\.pos$");
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
                Patient patient = patients.FirstOrDefault(p => p.ID.ToUpper().CompareTo(dir.Name.ToUpper()) == 0);
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
                                if (parameters.IncludeRaw)
                                {
                                    FileInfo rawEEG = new(Path.Combine(subdir.FullName, subdir.Name + ".eeg"));
                                    FileInfo rawPos = new(Path.Combine(subdir.FullName, subdir.Name + ".pos"));
                                    if (rawEEG.Exists && rawPos.Exists)
                                    {
                                        var dataInfo = new IEEGDataInfo("raw", protocol, new Container.Elan(rawEEG.FullName, rawPos.FullName, "", new Error[0], new Warning[0]), new Error[0], new Warning[0], patient, NormalizationType.Auto, databaseReference.ID);
                                        dataInfo.MarkValidationStale(
                                            ValidationAspect.DataInfoAll);
                                        dataInfoList.Add(dataInfo);
                                    }
                                }

                                string ds = GetDownsamplingString(subdir);
                                if (!string.IsNullOrEmpty(ds))
                                {
                                    FileInfo posDS = new(Path.Combine(subdir.FullName, string.Format("{0}_{1}.pos", subdir.Name, ds)));
                                    if (posDS.Exists)
                                    {
                                        foreach (var freq in parameters.Frequencies)
                                        {
                                            foreach (var ts in parameters.TemporalSmoothings)
                                            {
                                                FileInfo eeg = new(Path.Combine(subdir.FullName, string.Format("{0}_{1}", subdir.Name, freq), string.Format("{0}_{1}_{2}_{3}.eeg", subdir.Name, freq, ds, ts)));
                                                if (eeg.Exists)
                                                {
                                                    var dataInfo = new IEEGDataInfo(string.Format("{0}{1}", freq, ts), protocol, new Container.Elan(eeg.FullName, posDS.FullName, "", new Error[0], new Warning[0]), new Error[0], new Warning[0], patient, NormalizationType.Auto, databaseReference.ID);
                                                    dataInfo.MarkValidationStale(
                                                        ValidationAspect.DataInfoAll);
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
        public static void LoadFromBIDSDatabase(DatabaseReference databaseReference, List<Patient> patients, out DataInfo[] dataInfos, Action<float, float, LoadingText> updateProgress, CancellationToken token)
        {
            updateProgress?.Invoke(0, 0, new LoadingText("Finding data to load"));

            dataInfos = new DataInfo[0];
            if (string.IsNullOrEmpty(databaseReference.Path)) return;
            DirectoryInfo databaseDirectoryInfo = new(databaseReference.Path);
            if (!databaseDirectoryInfo.Exists) return;

            List<DataInfo> dataInfoList = new();

            // Find all dataInfo files
            var allIeegFiles = BIDSParser.FindFiles(databaseDirectoryInfo.FullName, new[] { "ieeg" }, new[] { ".vhdr", ".edf" });
            var brainvisionFiles = allIeegFiles.Where(f => f.Extension.EndsWith(".vhdr", StringComparison.OrdinalIgnoreCase)).ToList();
            var edfFiles = allIeegFiles.Where(f => f.Extension.EndsWith(".edf", StringComparison.OrdinalIgnoreCase)).ToList();
            int progress = 0;
            int length = brainvisionFiles.Count + edfFiles.Count;

            // Brainvision
            foreach (var bidsFile in brainvisionFiles)
            {
                updateProgress?.Invoke((float)progress++ / length, 0, new LoadingText("Loading file ", System.IO.Path.GetFileName(bidsFile.Path), " [" + progress + "/" + length + "]"));
                token.ThrowIfCancellationRequested();
                Patient patient = patients.FirstOrDefault(p => p.Name.CompareTo("sub-" + bidsFile.Entities["sub"]) == 0);
                if (patient != null)
                {
                    bidsFile.Entities.TryGetValue("task", out string task);
                    Protocol protocol = DatabaseManager.Database.Protocols.FirstOrDefault(p => p.Name == task);
                    if (protocol != null)
                    {
                        bidsFile.Entities.TryGetValue("acq", out string acq);
                        bidsFile.Entities.TryGetValue("run", out string run);
                        bidsFile.Entities.TryGetValue("desc", out string desc);
                        string dataName = string.Format("{0}{1}{2}", string.IsNullOrEmpty(acq) ? "raw" : acq, string.IsNullOrEmpty(run) ? "" : "-" + run, string.IsNullOrEmpty(desc) ? "" : "-" + desc);
                        var dataInfo = new IEEGDataInfo(dataName, protocol, new Container.BrainVision(bidsFile.Path, new Error[0], new Warning[0]), new Error[0], new Warning[0], patient, NormalizationType.Auto, databaseReference.ID);
                        dataInfo.MarkValidationStale(
                            ValidationAspect.DataInfoAll);
                        dataInfoList.Add(dataInfo);
                    }
                }
            }

            // EDF
            foreach (var bidsFile in edfFiles)
            {
                updateProgress?.Invoke((float)progress++ / length, 0, new LoadingText("Loading file ", System.IO.Path.GetFileName(bidsFile.Path), " [" + progress + "/" + length + "]"));
                token.ThrowIfCancellationRequested();
                Patient patient = patients.FirstOrDefault(p => p.Name.ToUpper().CompareTo(("sub-" + bidsFile.Entities["sub"]).ToUpper()) == 0);
                if (patient != null)
                {
                    bidsFile.Entities.TryGetValue("task", out string task);
                    Protocol protocol = DatabaseManager.Database.Protocols.FirstOrDefault(p => p.Name == task);
                    if (protocol != null)
                    {
                        bidsFile.Entities.TryGetValue("acq", out string acq);
                        bidsFile.Entities.TryGetValue("run", out string run);
                        bidsFile.Entities.TryGetValue("desc", out string desc);
                        string dataName = string.Format("{0}{1}{2}", string.IsNullOrEmpty(acq) ? "raw" : acq, string.IsNullOrEmpty(run) ? "" : "-" + run, string.IsNullOrEmpty(desc) ? "" : "-" + desc);
                        var dataInfo = new IEEGDataInfo(dataName, protocol, new Container.EDF(bidsFile.Path, new Error[0], new Warning[0]), new Error[0], new Warning[0], patient, NormalizationType.Auto, databaseReference.ID);
                        dataInfo.MarkValidationStale(
                            ValidationAspect.DataInfoAll);
                        dataInfoList.Add(dataInfo);
                    }
                }
            }

            dataInfos = dataInfoList.ToArray();
            updateProgress?.Invoke(1.0f, 0, new LoadingText("Data loaded successfully"));
        }
        #endregion

        #region Private Methods
        private static readonly ValidationAspect[] AtomicDataInfoAspects =
        {
            ValidationAspect.Structure,
            ValidationAspect.SourceAvailability,
            ValidationAspect.SourceReadability,
            ValidationAspect.StaticContent,
            ValidationAspect.Epoching,
            ValidationAspect.ChannelMapping
        };

        internal virtual IEnumerable<ValidationState> GetValidationStates(
            ValidationAspect aspect,
            ValidationRequest request,
            DataInfoValidationContext context)
        {
            switch (aspect)
            {
                case ValidationAspect.Structure:
                    return new[]
                    {
                        CreateValidationState(
                            aspect,
                            string.Empty,
                            $"{Name}|{Protocol?.ID}",
                            string.IsNullOrEmpty(Name)
                                ? new Error[] { new LabelEmptyError() }
                                : Array.Empty<Error>(),
                            Array.Empty<Warning>())
                    };
                case ValidationAspect.SourceAvailability:
                    return new[]
                    {
                        CreateValidationState(
                            aspect,
                            string.Empty,
                            context.SourceSignature,
                            m_DataContainer.Errors,
                            m_DataContainer.Warnings)
                    };
                case ValidationAspect.SourceReadability:
                    if (!IsEEGDataContainer())
                    {
                        return new[]
                        {
                            CreateNotApplicableState(aspect)
                        };
                    }
                    return context.TryGetEEGMetadata(out _, out Error error)
                        ? new[]
                        {
                            CreateValidationState(
                                aspect,
                                string.Empty,
                                context.SourceSignature,
                                Array.Empty<Error>(),
                                Array.Empty<Warning>())
                        }
                        : new[]
                        {
                            CreateValidationState(
                                aspect,
                                string.Empty,
                                context.SourceSignature,
                                error == null
                                    ? Array.Empty<Error>()
                                    : new[] { error },
                                Array.Empty<Warning>())
                        };
                default:
                    return new[]
                    {
                        CreateNotApplicableState(aspect)
                    };
            }
        }

        protected ValidationState CreateValidationState(
            ValidationAspect aspect,
            string scopeID,
            string signature,
            IEnumerable<Error> errors,
            IEnumerable<Warning> warnings)
        {
            return new ValidationState(
                aspect,
                scopeID,
                ValidationStatus.Current,
                signature,
                errors,
                warnings);
        }

        protected ValidationState CreateNotApplicableState(
            ValidationAspect aspect,
            string scopeID = "")
        {
            return new ValidationState(
                aspect,
                scopeID,
                ValidationStatus.NotApplicable,
                string.Empty,
                Array.Empty<Error>(),
                Array.Empty<Warning>());
        }

        private bool IsEEGDataContainer()
        {
            return m_DataContainer is Container.BrainVision ||
                m_DataContainer is Container.EDF ||
                m_DataContainer is Container.Elan ||
                m_DataContainer is Container.Micromed ||
                m_DataContainer is Container.FIF;
        }

        private bool HasStaleValidation(ValidationRequest request)
        {
            return AtomicDataInfoAspects.Any(aspect =>
                request.Matches(this, aspect) &&
                !IsValidationCurrent(aspect, request));
        }

        private ValidationState GetValidationState(
            ValidationAspect aspect,
            string scopeID)
        {
            return m_ValidationStates.FirstOrDefault(state =>
                state.Aspect == aspect &&
                string.Equals(
                    state.ScopeID,
                    scopeID ?? string.Empty,
                    StringComparison.Ordinal));
        }

        private void ReplaceValidationStates(
            ValidationAspect aspect,
            ValidationRequest request,
            IEnumerable<ValidationState> states)
        {
            ValidationState[] replacements =
                states?.ToArray() ?? Array.Empty<ValidationState>();
            IReadOnlyCollection<string> targetedSubBlocIDs =
                request.GetTargetedSubBlocIDs(this);
            if (aspect == ValidationAspect.Epoching &&
                targetedSubBlocIDs.Count > 0)
            {
                m_ValidationStates.RemoveAll(state =>
                    state.Aspect == aspect &&
                    (string.IsNullOrEmpty(state.ScopeID) ||
                        targetedSubBlocIDs.Contains(state.ScopeID)));
            }
            else
            {
                m_ValidationStates.RemoveAll(state => state.Aspect == aspect);
            }
            m_ValidationStates.AddRange(replacements);
        }

        private void EnsureValidationStates()
        {
            m_ValidationStates ??= new List<ValidationState>();
            if (m_ValidationStates.Count > 0)
            {
                return;
            }
            if ((m_Errors?.Length ?? 0) == 0 &&
                (m_Warnings?.Length ?? 0) == 0 &&
                m_DataContainer.Errors.Count == 0 &&
                m_DataContainer.Warnings.Count == 0)
            {
                return;
            }

            AddLegacyState(
                ValidationAspect.Structure,
                m_Errors.Where(error =>
                    error is LabelEmptyError ||
                    error is PatientEmptyError),
                Array.Empty<Warning>());
            AddLegacyState(
                ValidationAspect.SourceAvailability,
                m_Errors.Where(error =>
                        error is RequiredFieldEmptyError ||
                        error is FileDoesNotExistError ||
                        error is WrongExtensionError)
                    .Concat(m_DataContainer.Errors),
                m_DataContainer.Warnings);
            AddLegacyState(
                ValidationAspect.SourceReadability,
                m_Errors.Where(error =>
                    error is SourceUnreadableError),
                Array.Empty<Warning>());
            AddLegacyState(
                ValidationAspect.StaticContent,
                m_Errors.Where(error => error is InvalidDataFileError),
                Array.Empty<Warning>());
            AddLegacyState(
                ValidationAspect.Epoching,
                m_Errors.Where(error => error is BlocsCantBeEpochedError),
                m_Warnings.Where(warning =>
                    warning is BlocsCantBeEpochedWarning));
            AddLegacyState(
                ValidationAspect.ChannelMapping,
                m_Errors.Where(error => error is ChannelNotFoundError),
                m_Warnings.Where(warning => warning is NoMatchingSiteWarning));

            Error[] knownErrors = m_ValidationStates
                .SelectMany(state => state.Errors)
                .ToArray();
            Warning[] knownWarnings = m_ValidationStates
                .SelectMany(state => state.Warnings)
                .ToArray();
            AddLegacyState(
                ValidationAspect.None,
                m_Errors.Except(knownErrors),
                m_Warnings.Except(knownWarnings));
            RefreshFlattenedIssues();
        }

        private void AddLegacyState(
            ValidationAspect aspect,
            IEnumerable<Error> errors,
            IEnumerable<Warning> warnings)
        {
            Error[] errorArray = errors.ToArray();
            Warning[] warningArray = warnings.ToArray();
            if (errorArray.Length == 0 && warningArray.Length == 0)
            {
                return;
            }
            m_ValidationStates.Add(new ValidationState(
                aspect,
                string.Empty,
                ValidationStatus.Stale,
                string.Empty,
                errorArray,
                warningArray));
        }

        private void RefreshFlattenedIssues()
        {
            m_Errors = m_ValidationStates
                .SelectMany(state => state.Errors)
                .Distinct()
                .ToArray();
            m_Warnings = m_ValidationStates
                .SelectMany(state => state.Warnings)
                .Distinct()
                .ToArray();
        }

        protected void CopyValidationStateTo(DataInfo target)
        {
            EnsureValidationStates();
            target.m_ValidationStates = m_ValidationStates
                .Select(state => state.Clone())
                .ToList();
            target.m_Errors = m_Errors.ToArray();
            target.m_Warnings = m_Warnings.ToArray();
        }

        /// <summary>
        /// Get all dataInfo errors.
        /// </summary>
        /// <param name="protocol">Protocol of the dataset the dataInfo belongs to.</param>
        /// <returns>All dataInfo errors.</returns>
        protected virtual IEnumerable<Error> GetErrors()
        {
            List<Error> errors = new();
            errors.AddRange(GetNameErrors());
            m_DataContainer.GetErrors();
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
            m_DataContainer.GetWarnings();
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
                m_ValidationStates = dataInfo.m_ValidationStates?
                    .Select(state => state.Clone())
                    .ToList() ?? new List<ValidationState>();
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
            EnsureValidationStates();
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
