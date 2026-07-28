using HBP.Core.Data;
using HBP.Core.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Cysharp.Threading.Tasks;
using System.Threading;
using HBP.Core.Preferences;
using HBP.Core.Errors;
using LoadingOperation = HBP.Core.Tools.LoadingDiagnostics.Operation;
using LoadingPhase = HBP.Core.Tools.LoadingDiagnostics.Phase;

namespace HBP.Core.Database
{
    public class DatabaseUpdateReport
    {
        public ReadOnlyCollection<Patient> RemovedPatients { get; }
        public ReadOnlyCollection<Patient> AddedPatients { get; }
        public ReadOnlyCollection<Patient> UpdatedPatients { get; }
        public bool HasChanges => RemovedPatients.Count > 0 || AddedPatients.Count > 0 || UpdatedPatients.Count > 0;

        public DatabaseUpdateReport(IEnumerable<Patient> removedPatients, IEnumerable<Patient> addedPatients, IEnumerable<Patient> updatedPatients)
        {
            RemovedPatients = new ReadOnlyCollection<Patient>(removedPatients.ToList());
            AddedPatients = new ReadOnlyCollection<Patient>(addedPatients.ToList());
            UpdatedPatients = new ReadOnlyCollection<Patient>(updatedPatients.ToList());
        }
    }

    public class GlobalDatabase
    {
        #region Properties
        private const float READY_PROGRESS_WEIGHT = 0.7f;
        private readonly object m_LoadingOperationLock = new();
        private SharedLoadingOperation<GlobalDatabase> m_LoadingOperation;
        private string m_LoadingWorkspaceID;
        private string m_PublishedWorkspaceID;
        private long m_LoadingGeneration;
        private bool m_ValidationPublished;
        private Guid? m_PresentedFailureOperationID;
        private ValidationRequest m_ValidationRequest =
            ValidationRequest.Startup;

        public SharedLoadingOperation<GlobalDatabase> CurrentLoadingOperation
        {
            get
            {
                lock (m_LoadingOperationLock)
                {
                    return m_LoadingOperation;
                }
            }
        }

        private GlobalDatabaseSettings m_Settings = new();
        public GlobalDatabaseSettings Settings => m_Settings;

        private List<Protocol> m_Protocols = new();
        public ReadOnlyCollection<Protocol> Protocols => new(m_Protocols);

        private List<DatabaseReference> m_DatabaseReferences = new();
        public ReadOnlyCollection<DatabaseReference> DatabaseReferences => new(m_DatabaseReferences);

        private List<Patient> m_Patients = new();
        public ReadOnlyCollection<Patient> Patients => new(m_Patients);

        private List<DataInfo> m_DataInfos = new();
        public ReadOnlyCollection<DataInfo> DataInfos => new(m_DataInfos);

        public bool IsLoaded
        {
            get
            {
                lock (m_LoadingOperationLock)
                {
                    return m_PublishedWorkspaceID != null &&
                        m_PublishedWorkspaceID == Settings.SelectedWorkspace?.ID;
                }
            }
        }

        public bool NeedsReadyWait => !IsLoaded;

        public bool NeedsValidationWait
        {
            get
            {
                lock (m_LoadingOperationLock)
                {
                    return !m_ValidationPublished || !IsLoaded;
                }
            }
        }

        public bool RequiresValidation(ValidationRequest request)
        {
            if (request == null || request.Aspects == ValidationAspect.None)
            {
                return false;
            }

            lock (m_LoadingOperationLock)
            {
                if (!m_ValidationPublished || !IsLoaded)
                {
                    return true;
                }
            }

            ValidationAspect dataInfoAspects =
                request.Aspects & ValidationAspect.DataInfoAll;
            return request.Force ||
                (request.Includes(ValidationAspect.PatientAssets) &&
                    m_Patients
                        .Where(request.Matches)
                        .Any(patient =>
                            !patient.IsAssetValidationCurrent)) ||
                (dataInfoAspects != ValidationAspect.None &&
                    m_DataInfos
                        .Where(request.Matches)
                        .Any(dataInfo =>
                            !dataInfo.IsValidationCurrent(
                                dataInfoAspects,
                                request)));
        }
        #endregion

        #region Events
        public UnityEvent OnUpdateDatabases { get; } = new UnityEvent();
        public event Action OnValidationStateChanged;
        #endregion

        #region Getters/Setters
        public void SetProtocols(
            IEnumerable<Protocol> protocols,
            ValidationRequest validationRequest = null)
        {
            m_Protocols = protocols.ToList();
            if (validationRequest?.Aspects != ValidationAspect.None)
            {
                RefreshAfterProtocolChangeAsync(
                    validationRequest).Forget();
            }
        }
        public void SetDatabaseReferences(IEnumerable<DatabaseReference> databaseReferences)
        {
            m_DatabaseReferences = databaseReferences.ToList();
        }
        #endregion

        #region Public Methods
        public async UniTask InitializeAsync()
        {
            // TEMP-LOADING-PROFILING
            using LoadingDiagnostics.SessionScope session = LoadingDiagnostics.BeginSession(LoadingOperation.Database);
            try
            {
                await UniTask.SwitchToThreadPool();
                if (!new DirectoryInfo(ApplicationState.DatabasePath).Exists) Directory.CreateDirectory(ApplicationState.DatabasePath);
                LoadSettings();
                session.MarkSucceeded();
            }
            catch (OperationCanceledException)
            {
                session.MarkCanceled();
                throw;
            }
            catch (Exception exception)
            {
                session.MarkFailed(exception);
                throw;
            }
        }
        public void SaveSettings()
        {
            ClassLoaderSaver.SaveToJSon(m_Settings, GlobalDatabaseSettings.PATH, true);
        }
        public async UniTask CheckIntegrityAsync(string path, Action<float, float, LoadingText> updateProgress, CancellationToken token)
        {
            await EnsureDatabaseValidatedAsync(
                ValidationRequest.Full,
                (progress, duration, text) => updateProgress(
                    progress * 0.9f,
                    duration,
                    text),
                token);

            updateProgress(0.9f, 0, new LoadingText("Writing report"));

            // Gather useful information

            // Anatomical
            List<Patient> missingMeshesPatients = m_Patients.Where(p => p.Meshes.Count == 0).OrderBy(p => p.Name).ToList();
            List<Patient> missingMRIsPatients = m_Patients.Where(p => p.MRIs.Count == 0).OrderBy(p => p.Name).ToList();
            List<Patient> missingSitesPatients = m_Patients.Where(p => p.Sites.Count == 0).OrderBy(p => p.Name).ToList();

            // Functional
            Dictionary<string, Dictionary<Type, List<IEEGDataInfo>>> dataInfosByErrorTypeByDataName = new();
            var ieegDataInfos = m_DataInfos.OfType<IEEGDataInfo>();
            List<string> dataNames = ieegDataInfos.Select(d => d.Name).Distinct().ToList();
            foreach (var name in dataNames)
            {
                Dictionary<Type, List<IEEGDataInfo>> dataInfosByErrorType = new();
                foreach (var dataInfo in ieegDataInfos.Where(d => d.Name == name))
                {
                    foreach (var error in dataInfo.Errors)
                    {
                        if (!dataInfosByErrorType.ContainsKey(error.GetType()))
                            dataInfosByErrorType[error.GetType()] = new List<IEEGDataInfo>();

                        dataInfosByErrorType[error.GetType()].Add(dataInfo);
                    }
                    foreach (var warning in dataInfo.Warnings)
                    {
                        if (!dataInfosByErrorType.ContainsKey(warning.GetType()))
                            dataInfosByErrorType[warning.GetType()] = new List<IEEGDataInfo>();

                        dataInfosByErrorType[warning.GetType()].Add(dataInfo);
                    }
                }
                dataInfosByErrorTypeByDataName[name] = dataInfosByErrorType;
            }

            // Write report
            using StreamWriter writer = new(path);
            await writer.WriteLineAsync("=== Anatomical Data Check ===\n");
            if (missingMeshesPatients.Any())
            {
                await writer.WriteLineAsync("Missing Meshes:");
                foreach (var patient in missingMeshesPatients)
                    await writer.WriteLineAsync($"    - {patient.ID}");
                await writer.WriteLineAsync();
            }

            if (missingMRIsPatients.Any())
            {
                await writer.WriteLineAsync("Missing MRIs:");
                foreach (var patient in missingMRIsPatients)
                    await writer.WriteLineAsync($"    - {patient.ID}");
                await writer.WriteLineAsync();
            }

            if (missingSitesPatients.Any())
            {
                await writer.WriteLineAsync("Missing Sites:");
                foreach (var patient in missingSitesPatients)
                    await writer.WriteLineAsync($"    - {patient.ID}");
                await writer.WriteLineAsync();
            }

            await writer.WriteLineAsync("=== Functional Data Check ===\n");

            foreach (var (dataName, dataInfosByErrorType) in dataInfosByErrorTypeByDataName)
            {
                await writer.WriteLineAsync($"== {dataName} ==\n");

                foreach (var kv in dataInfosByErrorType)
                {
                    await writer.WriteLineAsync($"{kv.Key}:");

                    var groupedByPatient = kv.Value.GroupBy(d => d.Patient).Where(g => g.Key != null).OrderBy(g => g.Key.Name);

                    foreach (var patientGroup in groupedByPatient)
                    {
                        string patientId = patientGroup.Key.ID;
                        var protocolNames = patientGroup.Select(d => d.Protocol?.Name).Where(name => !string.IsNullOrEmpty(name)).Distinct().OrderBy(name => name);

                        string protocolsJoined = string.Join(", ", protocolNames);

                        await writer.WriteLineAsync($"    - {patientId}: {protocolsJoined}");
                    }
                    await writer.WriteLineAsync();
                }
            }

            writer.Close();

            await SaveDatabaseAsync(updateProgress);
        }
        #endregion

        #region Private Methods
        public void ConfigureDefault()
        {
            DirectoryInfo defaultDatabaseDirectory = new(Path.Combine(ApplicationState.DataPath, "DefaultDatabase"));
            defaultDatabaseDirectory.CopyFilesRecursively(new DirectoryInfo(ApplicationState.DatabasePath));
        }

        private void LoadSettings()
        {
            if (new FileInfo(GlobalDatabaseSettings.PATH).Exists)
            {
                try
                {
                    m_Settings = ClassLoaderSaver.LoadFromJson<GlobalDatabaseSettings>(
                        GlobalDatabaseSettings.PATH,
                        LoadingPhase.DatabaseSettings,
                        LoadingPhase.DatabaseSettings);
                    // Remove unused workspaces
                    var workspaceDirectories = new DirectoryInfo(Path.Combine(ApplicationState.DatabasePath, "Workspaces")).GetDirectories();
                    foreach (var workspaceDirectory in workspaceDirectories)
                    {
                        if (!m_Settings.Workspaces.Any(w => w.ID == workspaceDirectory.Name))
                        {
                            workspaceDirectory.Delete(true);
                        }
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                    throw e;
                }
            }
            else
            {
                m_Settings.SetDefaultWorkspace();
            }
        }

        public async UniTask StartLoadingSilentlyAsync()
        {
            await UniTask.SwitchToMainThread();
            SharedLoadingOperation<GlobalDatabase> operation =
                GetOrCreateLoadingOperation(false, out SharedLoadingOperation<GlobalDatabase> obsolete, out bool created);
            obsolete?.Cancel();
            if (created)
            {
                ObserveBackgroundOperationAsync(operation).Forget();
            }
        }

        public async UniTask ReloadSelectedWorkspaceAsync(
            Action<float, float, LoadingText> updateProgress,
            CancellationToken token = default)
        {
            await UniTask.SwitchToMainThread();
            SharedLoadingOperation<GlobalDatabase> operation =
                GetOrCreateLoadingOperation(true, out SharedLoadingOperation<GlobalDatabase> obsolete, out _);
            obsolete?.Cancel();
            ObserveBackgroundOperationAsync(operation).Forget();
            try
            {
                await WaitForReadyAsync(operation, updateProgress, token);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                MarkFailurePresented(operation);
                throw;
            }
        }

        public async UniTask ReloadSelectedWorkspaceSilentlyAsync()
        {
            await UniTask.SwitchToMainThread();
            SharedLoadingOperation<GlobalDatabase> operation =
                GetOrCreateLoadingOperation(
                    true,
                    out SharedLoadingOperation<GlobalDatabase> obsolete,
                    out _);
            obsolete?.Cancel();
            ObserveBackgroundOperationAsync(operation).Forget();
        }

        public async UniTask EnsureDatabaseReadyAsync(
            Action<float, float, LoadingText> updateProgress,
            CancellationToken token = default)
        {
            await UniTask.SwitchToMainThread();
            SharedLoadingOperation<GlobalDatabase> operation =
                GetOrCreateLoadingOperation(false, out SharedLoadingOperation<GlobalDatabase> obsolete, out bool created);
            obsolete?.Cancel();
            if (created)
            {
                ObserveBackgroundOperationAsync(operation).Forget();
            }
            try
            {
                await WaitForReadyAsync(operation, updateProgress, token);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                MarkFailurePresented(operation);
                throw;
            }
        }

        public async UniTask EnsureDatabaseValidatedAsync(
            Action<float, float, LoadingText> updateProgress,
            CancellationToken token = default)
        {
            await UniTask.SwitchToMainThread();
            SharedLoadingOperation<GlobalDatabase> operation =
                GetOrCreateLoadingOperation(false, out SharedLoadingOperation<GlobalDatabase> obsolete, out bool created);
            obsolete?.Cancel();
            if (created)
            {
                ObserveBackgroundOperationAsync(operation).Forget();
            }

            if (operation.State == LoadingOperationState.Validated ||
                operation.State == LoadingOperationState.ValidatedWithIssues)
            {
                await operation.EnsureValidatedAsync(token);
                return;
            }

            using IDisposable progressSubscription = operation.SubscribeProgress(
                progress => updateProgress?.Invoke(
                    progress.Value,
                    progress.Duration,
                    progress.Text));
            try
            {
                await operation.EnsureValidatedAsync(token);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                MarkFailurePresented(operation);
                throw;
            }
        }

        public async UniTask EnsureDatabaseValidatedAsync(
            ValidationRequest request,
            Action<float, float, LoadingText> updateProgress,
            CancellationToken token = default)
        {
            if (!IsLoaded)
            {
                await EnsureDatabaseReadyAsync(updateProgress, token);
            }
            if (RequiresValidation(request))
            {
                InvalidateValidation(request);
            }
            await EnsureDatabaseValidatedAsync(updateProgress, token);
        }

        public async UniTask LoadDatabaseAsync(
            Action<float, float, LoadingText> updateProgress)
        {
            await EnsureDatabaseValidatedAsync(updateProgress);
        }

        public void InvalidateValidation(
            ValidationRequest request = null)
        {
            request ??= ValidationRequest.Full;
            MarkRequestedValidationStale(request);
            SharedLoadingOperation<GlobalDatabase> operation;
            SharedLoadingOperation<GlobalDatabase> replacement;
            lock (m_LoadingOperationLock)
            {
                string workspaceID = Settings.SelectedWorkspace?.ID;
                if (m_LoadingOperation == null ||
                    workspaceID == null ||
                    workspaceID != m_PublishedWorkspaceID)
                {
                    return;
                }

                operation = m_LoadingOperation;
                long generation = ++m_LoadingGeneration;
                m_LoadingWorkspaceID = workspaceID;
                m_ValidationRequest = m_ValidationPublished
                    ? request
                    : m_ValidationRequest.Merge(request);
                m_ValidationPublished = false;
                replacement = CreateValidationOperation(
                    generation,
                    workspaceID,
                    m_ValidationRequest);
                m_LoadingOperation = replacement;
            }

            operation.Cancel();
            ObserveBackgroundOperationAsync(replacement).Forget();
            OnValidationStateChanged?.Invoke();
        }

        private void MarkRequestedValidationStale(
            ValidationRequest request)
        {
            ValidationAspect[] aspects =
            {
                ValidationAspect.Structure,
                ValidationAspect.SourceAvailability,
                ValidationAspect.SourceReadability,
                ValidationAspect.StaticContent,
                ValidationAspect.Epoching,
                ValidationAspect.ChannelMapping
            };
            foreach (DataInfo dataInfo in m_DataInfos)
            {
                foreach (ValidationAspect aspect in aspects)
                {
                    if (request.Matches(dataInfo, aspect))
                    {
                        dataInfo.MarkValidationStale(aspect);
                    }
                }
            }
            foreach (Patient patient in m_Patients)
            {
                if (request.Matches(patient))
                {
                    patient.MarkAssetValidationStale();
                }
            }
        }

        private async UniTaskVoid RefreshAfterProtocolChangeAsync(
            ValidationRequest request)
        {
            await UniTask.SwitchToMainThread();
            if (CurrentLoadingOperation == null)
            {
                return;
            }
            if (IsLoaded)
            {
                InvalidateValidation(request);
                return;
            }

            SharedLoadingOperation<GlobalDatabase> operation =
                GetOrCreateLoadingOperation(true, out SharedLoadingOperation<GlobalDatabase> obsolete, out _);
            obsolete?.Cancel();
            ObserveBackgroundOperationAsync(operation).Forget();
        }

        private SharedLoadingOperation<GlobalDatabase> CreateValidationOperation(
            long generation,
            string workspaceID,
            ValidationRequest request)
        {
            return new SharedLoadingOperation<GlobalDatabase>(
                generation,
                (progress, operationToken) => UniTask.FromResult(this),
                (database, progress, operationToken) =>
                    ValidateDatabaseCoreAsync(
                        workspaceID,
                        (value, duration, text) => progress(
                            READY_PROGRESS_WEIGHT +
                                value * (1 - READY_PROGRESS_WEIGHT),
                            duration,
                            text),
                        operationToken,
                        generation,
                        request));
        }

        private async UniTask WaitForReadyAsync(
            SharedLoadingOperation<GlobalDatabase> operation,
            Action<float, float, LoadingText> updateProgress,
            CancellationToken token)
        {
            if (operation.Ready.IsCompleted)
            {
                await operation.EnsureReadyAsync(token);
                return;
            }

            using IDisposable progressSubscription = operation.SubscribeProgress(progress =>
            {
                if (progress.State != LoadingOperationState.Loading ||
                    progress.Value > READY_PROGRESS_WEIGHT)
                {
                    return;
                }

                updateProgress?.Invoke(
                    Math.Min(1, progress.Value / READY_PROGRESS_WEIGHT),
                    progress.Duration,
                    progress.Text);
            });
            await operation.EnsureReadyAsync(token);
        }

        private SharedLoadingOperation<GlobalDatabase> GetOrCreateLoadingOperation(
            bool forceReload,
            out SharedLoadingOperation<GlobalDatabase> obsolete,
            out bool created)
        {
            lock (m_LoadingOperationLock)
            {
                Workspace workspace = Settings.SelectedWorkspace ??
                    throw new InvalidOperationException("No database workspace is selected.");
                bool retryFailedOperation =
                    m_LoadingOperation?.State == LoadingOperationState.Cancelled ||
                    (m_LoadingOperation?.State == LoadingOperationState.ValidationFailed &&
                        m_PresentedFailureOperationID == m_LoadingOperation.ID);
                if (!forceReload &&
                    !retryFailedOperation &&
                    m_LoadingOperation != null &&
                    m_LoadingWorkspaceID == workspace.ID)
                {
                    obsolete = null;
                    created = false;
                    return m_LoadingOperation;
                }

                obsolete = m_LoadingOperation;
                long generation = ++m_LoadingGeneration;
                string workspaceID = workspace.ID;
                string workspacePath = workspace.Path;
                Protocol[] protocols = m_Protocols.ToArray();
                m_LoadingWorkspaceID = workspaceID;
                m_ValidationPublished = false;
                m_ValidationRequest = ValidationRequest.Startup;
                m_PresentedFailureOperationID = null;
                m_LoadingOperation = new SharedLoadingOperation<GlobalDatabase>(
                    generation,
                    async (progress, operationToken) =>
                    {
                        await LoadDatabaseCoreAsync(
                            workspaceID,
                            workspacePath,
                            protocols,
                            (value, duration, text) => progress(
                                value * READY_PROGRESS_WEIGHT,
                                duration,
                                text),
                            operationToken,
                            generation);
                        return this;
                    },
                    (database, progress, operationToken) =>
                        ValidateDatabaseCoreAsync(
                            workspaceID,
                            (value, duration, text) => progress(
                                READY_PROGRESS_WEIGHT +
                                    value * (1 - READY_PROGRESS_WEIGHT),
                                duration,
                                text),
                            operationToken,
                            generation,
                            ValidationRequest.Startup));
                created = true;
                return m_LoadingOperation;
            }
        }

        private void MarkFailurePresented(
            SharedLoadingOperation<GlobalDatabase> operation)
        {
            lock (m_LoadingOperationLock)
            {
                if (m_LoadingOperation == operation &&
                    operation.State == LoadingOperationState.ValidationFailed)
                {
                    m_PresentedFailureOperationID = operation.ID;
                }
            }
        }

        private async UniTaskVoid ObserveBackgroundOperationAsync(
            SharedLoadingOperation<GlobalDatabase> operation)
        {
            try
            {
                await operation.EnsureValidatedAsync();
            }
            catch (OperationCanceledException)
            {
            }
            catch
            {
                // Load and validation cores log technical exceptions. The shared
                // operation retains the exception for the next visible consumer.
            }
        }

        private async UniTask LoadDatabaseCoreAsync(
            string workspaceID,
            string workspacePath,
            IReadOnlyCollection<Protocol> protocols,
            Action<float, float, LoadingText> updateProgress,
            CancellationToken token,
            long generation)
        {
            using LoadingDiagnostics.SessionScope session =
                LoadingDiagnostics.BeginSession(LoadingOperation.Database);
            try
            {
                FileInfo[] referenceFiles = GetDatabaseReferenceFiles(workspacePath);
                FileInfo[] patientFiles = GetPatientFiles(workspacePath);
                FileInfo[] dataInfoFiles = GetDataInfoFiles(workspacePath);
                float referenceProgress = referenceFiles.Length;
                float patientProgress = patientFiles.Length;
                float dataInfoProgress = dataInfoFiles.Length;
                float linkingProgress = 1;
                float steps =
                    referenceProgress + patientProgress + dataInfoProgress + linkingProgress;
                referenceProgress /= steps;
                patientProgress /= steps;
                dataInfoProgress /= steps;
                linkingProgress /= steps;
                float progress = 0;

                token.ThrowIfCancellationRequested();
                List<DatabaseReference> references = await LoadDatabaseReferencesAsync(
                    referenceFiles,
                    (localProgress, duration, text) => updateProgress(
                        progress + localProgress * referenceProgress,
                        duration,
                        text));
                progress += referenceProgress;

                token.ThrowIfCancellationRequested();
                List<Patient> patients = await LoadPatientsAsync(
                    patientFiles,
                    (localProgress, duration, text) => updateProgress(
                        progress + localProgress * patientProgress,
                        duration,
                        text));
                progress += patientProgress;

                token.ThrowIfCancellationRequested();
                List<DataInfo> dataInfos = await LoadDataInfosAsync(
                    dataInfoFiles,
                    (localProgress, duration, text) => updateProgress(
                        progress + localProgress * dataInfoProgress,
                        duration,
                        text));
                progress += dataInfoProgress;

                token.ThrowIfCancellationRequested();
                updateProgress(progress, 0, new LoadingText("Linking database references"));
                using (LoadingDiagnostics.BeginPhase(
                    LoadingPhase.DatabaseLinkReferences,
                    objectCount: patients.Count + dataInfos.Count))
                {
                    LoadingContext context = new(
                        PersistentDataManager.Tags.AllTags,
                        protocols,
                        patients);
                    context.ResolveDatabase(patients, dataInfos);
                    ISet<string> tagIds = new HashSet<string>(
                        context.TagById.Keys,
                        StringComparer.Ordinal);
                    await UniTask.WhenAll(
                        patients.Select(patient => patient.CheckTagsAsync(tagIds)));
                }
                progress += linkingProgress;

                token.ThrowIfCancellationRequested();
                await UniTask.SwitchToMainThread();
                lock (m_LoadingOperationLock)
                {
                    if (generation != m_LoadingGeneration ||
                        workspaceID != Settings.SelectedWorkspace?.ID)
                    {
                        throw new OperationCanceledException(
                            "The database loading generation is obsolete.",
                            token);
                    }

                    m_DatabaseReferences = references;
                    m_Patients = patients;
                    m_DataInfos = dataInfos;
                    m_PublishedWorkspaceID = workspaceID;
                    m_ValidationPublished = false;
                }

                updateProgress(1, 0, new LoadingText("Database ready"));
                OnUpdateDatabases.Invoke();
                session.MarkSucceeded();
            }
            catch (OperationCanceledException)
            {
                session.MarkCanceled();
                throw;
            }
            catch (ThreadAbortException)
            {
                session.MarkCanceled();
                throw;
            }
            catch (Exception exception)
            {
                session.MarkFailed(exception);
                Debug.LogException(exception);
                throw;
            }
        }

        private async UniTask<bool> ValidateDatabaseCoreAsync(
            string workspaceID,
            Action<float, float, LoadingText> updateProgress,
            CancellationToken token,
            long generation,
            ValidationRequest request)
        {
            try
            {
                await UniTask.SwitchToMainThread();
                Patient[] patients;
                DataInfo[] dataInfos;
                lock (m_LoadingOperationLock)
                {
                    if (generation != m_LoadingGeneration ||
                        workspaceID != m_PublishedWorkspaceID ||
                        workspaceID != Settings.SelectedWorkspace?.ID)
                    {
                        throw new OperationCanceledException(
                            "The database validation generation is obsolete.",
                            token);
                    }
                    patients = request.Includes(
                            ValidationAspect.PatientAssets)
                        ? m_Patients.Where(request.Matches).ToArray()
                        : Array.Empty<Patient>();
                    dataInfos = m_DataInfos
                        .Where(request.Matches)
                        .ToArray();
                }

                float pathWeight = patients.Length == 0
                    ? 0
                    : dataInfos.Length == 0
                        ? 1
                        : (float)patients.Length / (patients.Length + dataInfos.Length);
                int pathConcurrency =
                    PersistentDataManager.UserPreferences.General.System.MultiThreading
                        ? 20
                        : 1;
                int dataInfoConcurrency =
                    PersistentDataManager.UserPreferences.General.System.MultiThreading
                        ? 2
                        : 1;

                PatientAssetValidationResult assetResult;
                using (LoadingDiagnostics.BeginPhase(
                    LoadingPhase.DatabasePatientsValidateFiles,
                    objectCount: patients.Length,
                    concurrency: pathConcurrency))
                {
                    assetResult = await new AssetReferenceValidator().ValidatePatientsAsync(
                        patients,
                        pathConcurrency,
                        token,
                        (completed, total) => updateProgress(
                            (total == 0 ? 1 : (float)completed / total) * pathWeight,
                            completed == 0 ? 0 : 0.2f,
                            total == 0
                                ? new LoadingText("Validating database file references")
                                : new LoadingText(
                                    "Validating database file references",
                                    " ",
                                    completed + "/" + total)),
                        generation);
                }

                DataInfoValidationResult dataInfoResult =
                    await new DataInfoValidator().ValidateAsync(
                        dataInfos,
                        request,
                        dataInfoConcurrency,
                        token,
                        (completed, total) => updateProgress(
                            pathWeight +
                                (total == 0 ? 1 : (float)completed / total) *
                                (1 - pathWeight),
                            completed == 0 ? 0 : 0.2f,
                            total == 0
                                ? new LoadingText("Validating database data")
                                : new LoadingText(
                                    "Validating database data",
                                    " ",
                                    completed + "/" + total)),
                        generation);

                token.ThrowIfCancellationRequested();
                await UniTask.SwitchToMainThread();
                lock (m_LoadingOperationLock)
                {
                    if (generation != m_LoadingGeneration ||
                        workspaceID != m_PublishedWorkspaceID ||
                        workspaceID != Settings.SelectedWorkspace?.ID ||
                        !assetResult.TryApply(m_LoadingGeneration) ||
                        !dataInfoResult.TryApply(m_LoadingGeneration))
                    {
                        throw new OperationCanceledException(
                            "The database validation generation is obsolete.",
                            token);
                    }
                    m_ValidationPublished = true;
                }

                OnValidationStateChanged?.Invoke();
                OnUpdateDatabases.Invoke();
                return assetResult.HasIssues || dataInfoResult.HasIssues;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (ThreadAbortException)
            {
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                throw;
            }
        }
        public async UniTask SaveDatabaseAsync(Action<float, float, LoadingText> updateProgress)
        {
            float validationWeight = NeedsValidationWait ? 0.2f : 0;
            if (validationWeight > 0)
            {
                await EnsureDatabaseValidatedAsync(
                    (progress, duration, text) => updateProgress(
                        progress * validationWeight,
                        duration,
                        text));
            }

            await SaveDatabaseCoreAsync(
                (progress, duration, text) => updateProgress(
                    validationWeight + progress * (1 - validationWeight),
                    duration,
                    text));
        }

        private async UniTask SaveDatabaseCoreAsync(
            Action<float, float, LoadingText> updateProgress)
        {
            if (m_Patients.Count == 0 && m_DataInfos.Count == 0)
            {
                await SavePatientsAsync(updateProgress);
                await SaveDataInfosAsync(updateProgress);
                updateProgress(1, 0, new LoadingText("Finalizing"));
                return;
            }

            float patientProgress = m_Patients.Count;
            float dataInfoProgress = m_Patients.Count > 0 ? (float)m_DataInfos.Count / m_Patients.Count : m_DataInfos.Count;
            float steps = patientProgress + dataInfoProgress;
            patientProgress /= steps;
            dataInfoProgress /= steps;
            float progress = 0;
            await SavePatientsAsync((localProgress, duration, text) => updateProgress(progress + localProgress * patientProgress, duration, text));
            progress += patientProgress;
            await SaveDataInfosAsync((localProgress, duration, text) => updateProgress(progress + localProgress * dataInfoProgress, duration, text));
            progress += dataInfoProgress;
            updateProgress(1, 0, new LoadingText("Finalizing"));
        }

        public async UniTask LoadProtocolsAsync()
        {
            // TEMP-LOADING-PROFILING
            using LoadingDiagnostics.SessionScope session = LoadingDiagnostics.BeginSession(LoadingOperation.Database);
            try
            {
                DirectoryInfo protocolDirectory = new(Path.Combine(ApplicationState.DatabasePath, "Protocols"));
                if (!protocolDirectory.Exists) protocolDirectory.Create();
                FileInfo[] protocolFiles = protocolDirectory.GetFiles("*" + Protocol.EXTENSION, SearchOption.TopDirectoryOnly);
                using (LoadingDiagnostics.BeginPhase(LoadingPhase.DatabaseProtocols, concurrency: protocolFiles.Length))
                {
                    m_Protocols = (await UniTask.WhenAll(protocolFiles.Select(pf => ClassLoaderSaver.LoadFromJsonAsync<Protocol>(
                        pf.FullName,
                        LoadingPhase.DatabaseProtocols,
                        LoadingPhase.DatabaseProtocols,
                        protocolFiles.Length)))).ToList();
                }
                LoadingDiagnostics.RecordObjects("Protocol", m_Protocols.Count);
                session.MarkSucceeded();
            }
            catch (OperationCanceledException)
            {
                session.MarkCanceled();
                throw;
            }
            catch (Exception exception)
            {
                session.MarkFailed(exception);
                throw;
            }
        }
        public async UniTask SaveProtocolsAsync()
        {
            CopyProtocolsImages();
            DirectoryInfo protocolDirectory = Directory.CreateDirectory(Path.Combine(ApplicationState.DatabasePath, "Protocols"));
            DirectoryInfo protocolTempDirectory = Directory.CreateDirectory(Path.Combine(ApplicationState.DatabasePath, "ProtocolsTemp"));
            await UniTask.WhenAll(m_Protocols.Select(p => ClassLoaderSaver.SaveToJsonAsync(p, Path.Combine(protocolTempDirectory.FullName, p.Name + Protocol.EXTENSION), true)));
            protocolDirectory.Delete(true);
            protocolTempDirectory.MoveTo(protocolDirectory.FullName);
        }
        private void CopyProtocolsImages()
        {
            DirectoryInfo imagesDirectory = Directory.CreateDirectory(Path.Combine(ApplicationState.DatabasePath, "Images"));
            DirectoryInfo imagesTempDirectory = Directory.CreateDirectory(Path.Combine(ApplicationState.DatabasePath, "ImagesTemp"));
            foreach (var protocol in m_Protocols)
            {
                foreach (var bloc in protocol.Blocs)
                {
                    if (!string.IsNullOrEmpty(bloc.IllustrationPath))
                    {
                        var blocImage = new FileInfo(bloc.IllustrationPath);
                        if (blocImage.Exists)
                        {
                            blocImage.CopyTo(Path.Join(imagesTempDirectory.FullName, blocImage.Name));
                            bloc.IllustrationPath = Path.Join(imagesDirectory.FullName, blocImage.Name);
                        }
                    }

                    foreach (var subBloc in bloc.SubBlocs)
                    {
                        foreach (var icon in subBloc.Icons)
                        {
                            if (!string.IsNullOrEmpty(icon.ImagePath))
                            {
                                var iconImage = new FileInfo(icon.ImagePath);
                                if (iconImage.Exists)
                                {
                                    iconImage.CopyTo(Path.Join(imagesTempDirectory.FullName, iconImage.Name));
                                    icon.ImagePath = Path.Join(imagesDirectory.FullName, iconImage.Name);
                                }
                            }
                        }
                    }
                }
            }
            imagesDirectory.Delete(true);
            imagesTempDirectory.MoveTo(imagesDirectory.FullName);
        }

        public async UniTask LoadDatabaseReferencesAsync()
        {
            // TEMP-LOADING-PROFILING
            using LoadingDiagnostics.SessionScope session = LoadingDiagnostics.BeginSession(LoadingOperation.Database);
            try
            {
                FileInfo[] referenceFiles =
                    GetDatabaseReferenceFiles(Settings.SelectedWorkspace.Path);
                List<DatabaseReference> references =
                    await LoadDatabaseReferencesAsync(referenceFiles, null);
                await UniTask.SwitchToMainThread();
                m_DatabaseReferences = references;
                session.MarkSucceeded();
            }
            catch (OperationCanceledException)
            {
                session.MarkCanceled();
                throw;
            }
            catch (Exception exception)
            {
                session.MarkFailed(exception);
                throw;
            }
        }

        private FileInfo[] GetDatabaseReferenceFiles(string workspacePath)
        {
            DirectoryInfo referencesDirectory = Directory.CreateDirectory(
                Path.Combine(workspacePath, "References"));
            return referencesDirectory.GetFiles(
                "*" + DatabaseReference.EXTENSION,
                SearchOption.TopDirectoryOnly);
        }

        private async UniTask<List<DatabaseReference>> LoadDatabaseReferencesAsync(
            FileInfo[] referenceFiles,
            Action<float, float, LoadingText> updateProgress)
        {
            updateProgress?.Invoke(
                0,
                0,
                new LoadingText("Loading database references"));
            using (LoadingDiagnostics.BeginPhase(
                LoadingPhase.DatabaseReferences,
                concurrency: referenceFiles.Length))
            {
                List<DatabaseReference> references =
                    (await UniTask.WhenAll(referenceFiles.Select(referenceFile =>
                        ClassLoaderSaver.LoadFromJsonAsync<DatabaseReference>(
                            referenceFile.FullName,
                            LoadingPhase.DatabaseReferences,
                            LoadingPhase.DatabaseReferences,
                            referenceFiles.Length))))
                    .ToList();
                LoadingDiagnostics.RecordObjects(
                    "DatabaseReference",
                    references.Count);
                updateProgress?.Invoke(
                    1,
                    0,
                    new LoadingText("Database references loaded"));
                return references;
            }
        }
        public async UniTask SaveDatabaseReferencesAsync()
        {
            await SaveDatabaseReferencesAsync(true);
        }

        private async UniTask SaveDatabaseReferencesAsync(
            bool invalidateValidation)
        {
            await UniTask.SwitchToMainThread();
            string workspaceID = Settings.SelectedWorkspace?.ID;
            string workspacePath = Settings.SelectedWorkspace?.Path ??
                throw new InvalidOperationException("No database workspace is selected.");
            DatabaseReference[] references = m_DatabaseReferences.ToArray();

            DirectoryInfo referencesDirectory = Directory.CreateDirectory(Path.Combine(workspacePath, "References"));
            DirectoryInfo referencesTempDirectory = Directory.CreateDirectory(Path.Combine(workspacePath, "ReferencesTemp"));
            await UniTask.WhenAll(references.Select(dr => ClassLoaderSaver.SaveToJsonAsync(dr, Path.Combine(referencesTempDirectory.FullName, dr.ID + DatabaseReference.EXTENSION), true)));
            referencesDirectory.Delete(true);
            referencesTempDirectory.MoveTo(referencesDirectory.FullName);

            await UniTask.SwitchToMainThread();
            if (workspaceID != Settings.SelectedWorkspace?.ID)
            {
                return;
            }
            m_Patients.RemoveAll(p => !references.Any(r => r.ID == p.CorrespondingDatabaseID));
            m_DataInfos.RemoveAll(d => references.All(r => r.ID != d.CorrespondingDatabaseID) || (d is PatientDataInfo pd && !m_Patients.Contains(pd.Patient)));
            if (invalidateValidation)
            {
                InvalidateValidation(
                    new ValidationRequest(ValidationAspect.None));
            }
            OnUpdateDatabases.Invoke();
        }

        private FileInfo[] GetPatientFiles(string workspacePath)
        {
            DirectoryInfo patientsDirectory = new(Path.Combine(workspacePath, "Patients"));
            if (!patientsDirectory.Exists) patientsDirectory.Create();
            return patientsDirectory.GetFiles("*" + Patient.EXTENSION, SearchOption.TopDirectoryOnly);
        }
        private async UniTask<List<Patient>> LoadPatientsAsync(FileInfo[] patientFiles, Action<float, float, LoadingText> updateProgress)
        {
            int concurrency = PersistentDataManager.UserPreferences.General.System.MultiThreading ? 20 : 1;
            var tasks = patientFiles.Select(file => (Func<UniTask<Patient>>)(async () =>
            {
                var patient = await ClassLoaderSaver.LoadFromJsonAsync<Patient>(
                    file.FullName,
                    LoadingPhase.DatabasePatientsRead,
                    LoadingPhase.DatabasePatientsDeserialize,
                    concurrency);
                LoadingDiagnostics.RecordPatientGraph(patient);
                return patient;
            }));
            List<Patient> patients = (await Core.Tools.UniTaskExtensions.PerformMultipleTasksAsync(
                tasks,
                0,
                1,
                "Loading database patients",
                updateProgress,
                20,
                PersistentDataManager.UserPreferences.General.System.MultiThreading))
                .OrderBy(patient => patient.Name)
                .ToList();
            return patients;
        }
        private async UniTask SavePatientsAsync(Action<float, float, LoadingText> updateProgress)
        {
            DirectoryInfo patientsDirectory = Directory.CreateDirectory(Path.Combine(Settings.SelectedWorkspace.Path, "Patients"));
            DirectoryInfo patientsTempDirectory = Directory.CreateDirectory(Path.Combine(Settings.SelectedWorkspace.Path, "PatientsTemp"));
            var tasks = m_Patients.Select(p => (Func<UniTask>)(async () =>
            {
                await ClassLoaderSaver.SaveToJsonAsync(p, Path.Combine(patientsTempDirectory.FullName, p.ID + Patient.EXTENSION), true);
            }));
            await Core.Tools.UniTaskExtensions.PerformMultipleTasksAsync(tasks, 0, 1, "Saving database patients", updateProgress, 20, PersistentDataManager.UserPreferences.General.System.MultiThreading);
            patientsDirectory.Delete(true);
            patientsTempDirectory.MoveTo(patientsDirectory.FullName);
        }

        private FileInfo[] GetDataInfoFiles(string workspacePath)
        {
            DirectoryInfo dataInfosDirectory = new(Path.Combine(workspacePath, "DataInfos"));
            if (!dataInfosDirectory.Exists) dataInfosDirectory.Create();
            return dataInfosDirectory.GetFiles("*" + DataInfo.EXTENSION, SearchOption.TopDirectoryOnly);
        }
        private async UniTask<List<DataInfo>> LoadDataInfosAsync(FileInfo[] dataInfoFiles, Action<float, float, LoadingText> updateProgress)
        {
            int concurrency = PersistentDataManager.UserPreferences.General.System.MultiThreading ? 20 : 1;
            var tasks = dataInfoFiles.Select(file => (Func<UniTask<List<DataInfo>>>)(async () =>
            {
                List<DataInfo> dataInfos = await ClassLoaderSaver.LoadFromJsonAsync<List<DataInfo>>(
                    file.FullName,
                    LoadingPhase.DatabaseDataInfosRead,
                    LoadingPhase.DatabaseDataInfosDeserialize,
                    concurrency);
                LoadingDiagnostics.RecordObjects("DataInfo", dataInfos.Count);
                return dataInfos;
            }));
            return (await Core.Tools.UniTaskExtensions.PerformMultipleTasksAsync(tasks, 0, 1, "Loading database functional data", updateProgress, 20, PersistentDataManager.UserPreferences.General.System.MultiThreading)).SelectMany(d => d).ToList();
        }
        private async UniTask SaveDataInfosAsync(Action<float, float, LoadingText> updateProgress)
        {
            DirectoryInfo dataInfosDirectory = Directory.CreateDirectory(Path.Combine(Settings.SelectedWorkspace.Path, "DataInfos"));
            DirectoryInfo dataInfosTempDirectory = Directory.CreateDirectory(Path.Combine(Settings.SelectedWorkspace.Path, "DataInfosTemp"));
            Dictionary<Patient, List<PatientDataInfo>> patientDataInfos = new();
            foreach (var patient in m_DataInfos.OfType<PatientDataInfo>().Select(d => d.Patient).Distinct())
            {
                patientDataInfos.Add(patient, new List<PatientDataInfo>());
            }
            List<DataInfo> otherDataInfos = new();
            foreach (var dataInfo in m_DataInfos)
            {
                if (dataInfo is PatientDataInfo patientDataInfo) patientDataInfos[patientDataInfo.Patient].Add(patientDataInfo);
                else otherDataInfos.Add(dataInfo);
            }
            var tasks = patientDataInfos.Select(kvp => (Func<UniTask>)(async () =>
            {
                await ClassLoaderSaver.SaveToJsonAsync(kvp.Value, Path.Combine(dataInfosTempDirectory.FullName, kvp.Key.ID + DataInfo.EXTENSION), true);
            }));
            await Core.Tools.UniTaskExtensions.PerformMultipleTasksAsync(tasks, 0, 1, "Saving database functional data", updateProgress, 20, PersistentDataManager.UserPreferences.General.System.MultiThreading);
            await ClassLoaderSaver.SaveToJsonAsync(otherDataInfos, Path.Combine(dataInfosTempDirectory.FullName, "None" + DataInfo.EXTENSION), true);
            dataInfosDirectory.Delete(true);
            dataInfosTempDirectory.MoveTo(dataInfosDirectory.FullName);
        }
        
        public async UniTask<DatabaseUpdateReport> UpdateDatabasesAsync(IEnumerable<DatabaseReference> databaseReferences, Action<float, float, LoadingText> updateProgress, CancellationToken token)
        {
            updateProgress(0, 1, new LoadingText("Initialization"));
            await UniTask.SwitchToMainThread();
            string workspaceID = Settings.SelectedWorkspace?.ID;
            List<Patient> oldPatients = m_Patients
                .Select(patient => (Patient)patient.Clone())
                .ToList();
            List<DataInfo> oldDataInfos = m_DataInfos
                .Select(dataInfo => (DataInfo)dataInfo.Clone())
                .ToList();
            List<Patient> patientsTemp = new(m_Patients);
            List<DataInfo> dataInfosTemp = new(m_DataInfos);
            DatabaseReference[] referenceSnapshot = databaseReferences.ToArray();
            await UniTask.SwitchToThreadPool();
            var brainvisaDatabaseReferences = referenceSnapshot.Where(d => d.Type == DatabaseType.Brainvisa).ToArray();
            var localizerDatabaseReferences = referenceSnapshot.Where(d => d.Type == DatabaseType.Localizer).ToArray();
            var bidsDatabaseReferences = referenceSnapshot.Where(d => d.Type == DatabaseType.BIDS).ToArray();
            var tagsDatabaseReferences = referenceSnapshot.Where(d => d.Type == DatabaseType.Tags).ToArray();
            int numberOfDatabases = brainvisaDatabaseReferences.Length + localizerDatabaseReferences.Length + 2 * bidsDatabaseReferences.Length + tagsDatabaseReferences.Length;
            float progress = 0;
            // Load databases
            List<Patient> updatedPatients = new();
            // Load patients first
            foreach (var brainvisaDatabaseReference in brainvisaDatabaseReferences)
            {
                token.ThrowIfCancellationRequested();
                Patient.LoadFromIntranatDatabase(brainvisaDatabaseReference, out Patient[] patients, (localProgress, duration, text) => updateProgress(progress + (float)localProgress / numberOfDatabases, duration, text), token);
                patientsTemp.RemoveAll(p => patients.Contains(p) || p.CorrespondingDatabaseID == brainvisaDatabaseReference.ID);
                patientsTemp.AddRange(patients);
                updatedPatients.AddRange(patients);
                progress += 1f / numberOfDatabases;
            }
            foreach (var bidsDatabaseReference in bidsDatabaseReferences)
            {
                token.ThrowIfCancellationRequested();
                Patient.LoadFromBIDSDatabase(bidsDatabaseReference, out Patient[] patients, (localProgress, duration, text) => updateProgress(progress + (float)localProgress / numberOfDatabases, duration, text), token);
                foreach (var patient in patients) patient.CorrespondingDatabaseID = bidsDatabaseReference.ID;
                patientsTemp.RemoveAll(p => patients.Contains(p) || p.CorrespondingDatabaseID == bidsDatabaseReference.ID);
                patientsTemp.AddRange(patients);
                updatedPatients.AddRange(patients);
                progress += 1f / numberOfDatabases;
            }
            // Then load additional tags
            foreach (var tagsDatabaseReference in tagsDatabaseReferences)
            {
                token.ThrowIfCancellationRequested();
                Patient.LoadAdditionalTagsFromTagsDatabase(tagsDatabaseReference, patientsTemp, out Patient[] patients, (localProgress, duration, text) => updateProgress(progress + (float)localProgress / numberOfDatabases, duration, text), token);
                foreach (var patient in patients)
                {
                    patientsTemp.Remove(patient);
                    patientsTemp.Add(patient);
                }
                updatedPatients.AddRange(patients);
                progress += 1f / numberOfDatabases;
            }
            // Then load dataInfos
            foreach (var localizerDatabaseReference in localizerDatabaseReferences)
            {
                token.ThrowIfCancellationRequested();
                DataInfo.LoadFromLocalizersDatabase(localizerDatabaseReference, patientsTemp, out DataInfo[] dataInfos, (localProgress, duration, text) => updateProgress(progress + (float)localProgress / numberOfDatabases, duration, text), token);
                dataInfosTemp.RemoveAll(d => dataInfos.Contains(d) || d.CorrespondingDatabaseID == localizerDatabaseReference.ID);
                dataInfosTemp.AddRange(dataInfos);
                progress += 1f / numberOfDatabases;
            }
            foreach (var bidsDatabaseReference in bidsDatabaseReferences)
            {
                token.ThrowIfCancellationRequested();
                DataInfo.LoadFromBIDSDatabase(bidsDatabaseReference, patientsTemp, out DataInfo[] dataInfos, (localProgress, duration, text) => updateProgress(progress + (float)localProgress / numberOfDatabases, duration, text), token);
                dataInfosTemp.RemoveAll(d => dataInfos.Contains(d) || d.CorrespondingDatabaseID == bidsDatabaseReference.ID);
                dataInfosTemp.AddRange(dataInfos);
                progress += 1f / numberOfDatabases;
            }
            // Update last updated
            foreach (var databaseReference in referenceSnapshot)
            {
                token.ThrowIfCancellationRequested();
                databaseReference.LastUpdated = DateTime.Now;
            }
            updateProgress(1, 0, new LoadingText("Finalizing"));
            var report = FindChanges(oldPatients, patientsTemp, updatedPatients);

            token.ThrowIfCancellationRequested();
            await UniTask.SwitchToMainThread();
            if (workspaceID != Settings.SelectedWorkspace?.ID)
            {
                throw new OperationCanceledException(
                    "The database workspace changed while its references were updating.",
                    token);
            }
            m_Patients = patientsTemp;
            m_DataInfos = dataInfosTemp;
            ValidationRequest validationRequest =
                ValidationImpactAnalyzer.ForPatients(
                    oldPatients,
                    patientsTemp)
                .Merge(ValidationImpactAnalyzer.ForDataInfos(
                    oldDataInfos,
                    dataInfosTemp));
            if (validationRequest.Aspects != ValidationAspect.None)
            {
                InvalidateValidation(validationRequest);
            }
            OnUpdateDatabases.Invoke();
            await SaveDatabaseReferencesAsync(false);
            return report;
        }

        private DatabaseUpdateReport FindChanges(List<Patient> oldPatients, List<Patient> newPatients, List<Patient> updatedPatients)
        {
            var removedPatients = oldPatients.Except(newPatients).ToList();
            var addedPatients = newPatients.Except(oldPatients).ToList();
            updatedPatients = updatedPatients.Distinct().Except(addedPatients).Except(removedPatients).ToList();
            return new DatabaseUpdateReport(removedPatients, addedPatients, updatedPatients);
        }
        #endregion
    }
}
