using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Ionic.Zip;
using UnityEngine;
using HBP.Core.Exceptions;
using HBP.Core.Tools;
using HBP.Core.Interfaces;
using HBP.Core.Preferences;
using HBP.Core.Database;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace HBP.Core.Data
{
    /**
    * \class Project
    * \author Adrien Gannerie
    * \version 1.0
    * \date 12 janvier 2017
    * \brief Class which define a HiBoP project.
    *
    * \details Class which define a HiBoP project, it's contains :
    *     - Settings.
    *     - Patients.
    *     - Groups.
    *     - Regions of interest.(To Add)
    *     - Protocols.
    *     - Datasets.
    *     - Visualizations.
    */
    public class Project
    {
        #region Properties

        private const float READY_PROGRESS_WEIGHT = 0.7f;
        private readonly object m_LoadingOperationLock = new();
        private SharedLoadingOperation<Project> m_LoadingOperation;
        private string m_LoadingProjectPath;
        private long m_LoadingGeneration;
        private bool m_ValidationPublished;
        private ValidationRequest m_ValidationRequest = ValidationRequest.Startup;

        public SharedLoadingOperation<Project> CurrentLoadingOperation
        {
            get
            {
                lock (m_LoadingOperationLock)
                {
                    return m_LoadingOperation;
                }
            }
        }

        public bool NeedsValidationWait
        {
            get
            {
                lock (m_LoadingOperationLock)
                {
                    return !m_ValidationPublished;
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
                if (!m_ValidationPublished)
                {
                    return true;
                }
            }

            ValidationAspect dataInfoAspects = request.Aspects & ValidationAspect.DataInfoAll;
            return request.Force || (request.Includes(ValidationAspect.PatientAssets) && m_Patients.Where(request.Matches).Any(patient => !patient.IsAssetValidationCurrent)) || (dataInfoAspects != ValidationAspect.None && m_Datasets.SelectMany(dataset => dataset.Data).Where(request.Matches).Any(dataInfo => !dataInfo.IsValidationCurrent(dataInfoAspects, request)));
        }

        public event Action OnValidationStateChanged;

        /// <summary>
        /// Project extension.
        /// </summary>
        public const string EXTENSION = ".hibop";

        public string Name { get; set; }

        /// <summary>
        /// Project file
        /// </summary>
        public string FileName
        {
            get { return Name + EXTENSION; }
        }

        /// <summary>
        /// Settings of the project.
        /// </summary>
        public ProjectPreferences Preferences { get; set; }

        List<Patient> m_Patients = new();

        /// <summary>
        /// Patients of the project.
        /// </summary>
        public ReadOnlyCollection<Patient> Patients
        {
            get { return new ReadOnlyCollection<Patient>(m_Patients); }
        }

        List<Group> m_Groups = new();

        /// <summary>
        /// Patient groups of the project.
        /// </summary>
        public ReadOnlyCollection<Group> Groups
        {
            get { return new ReadOnlyCollection<Group>(m_Groups); }
        }

        List<Dataset> m_Datasets = new();

        /// <summary>
        /// Datasets of the project.
        /// </summary>
        public ReadOnlyCollection<Dataset> Datasets
        {
            get { return new ReadOnlyCollection<Dataset>(m_Datasets); }
        }

        List<Visualization> m_Visualizations = new();

        /// <summary>
        /// Visualizations of the project.
        /// </summary>
        public ReadOnlyCollection<Visualization> Visualizations
        {
            get { return new ReadOnlyCollection<Visualization>(m_Visualizations); }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Create a new project instance.
        /// </summary>
        /// <param name="settings">Settings of the project.</param>
        /// <param name="patients">Patients of the project.</param>
        /// <param name="groups">Groups of the project.</param>
        /// <param name="protocols">Protocols of the project.</param>
        /// <param name="datasets">Datasets of the project.</param>
        /// <param name="visualizations">Single patient visualizations of the project.</param>
        /// <param name="multiVisualizations">Multi patients visualizations of the project.</param>
        public Project(string name, ProjectPreferences settings, IEnumerable<Patient> patients, IEnumerable<Group> groups, IEnumerable<Dataset> datasets, IEnumerable<Visualization> visualizations)
        {
            Name = name;
            Preferences = settings;
            SetPatients(patients);
            SetGroups(groups);
            SetDatasets(datasets);
            SetVisualizations(visualizations);
        }

        /// <summary>
        /// Create a new project with only the settings.
        /// </summary>
        /// <param name="settings">Settings of the project.</param>
        public Project(string name, ProjectPreferences settings) : this(name, settings, new Patient[0], new Group[0], new Dataset[0], new Visualization[0])
        {
        }

        /// <summary>
        /// Create a empty project with a name.
        /// </summary>
        /// <param name="name">Name of the project.</param>
        public Project(string name) : this(name, new ProjectPreferences())
        {
        }

        /// <summary>
        /// Create a empty project with default values.
        /// </summary>
        public Project() : this(PersistentDataManager.UserPreferences.General.Project.DefaultName, new ProjectPreferences())
        {
        }

        #endregion

        #region Getter/Setter

        // Patients.
        /// <summary>
        /// Set the patients of the project.
        /// </summary>
        /// <param name="patients"></param>
        public void SetPatients(IEnumerable<Patient> patients, ValidationRequest validationRequest = null)
        {
            List<Patient> oldPatients = m_Patients;
            List<Patient> newPatients = patients?.ToList() ?? new List<Patient>();
            validationRequest ??= ValidationImpactAnalyzer.ForPatients(oldPatients, newPatients);
            m_Patients = newPatients;
            LoadingContext context = new(Array.Empty<BaseTag>(), Array.Empty<Protocol>(), m_Patients);
            foreach (Dataset dataset in m_Datasets)
            {
                foreach (PatientDataInfo dataInfo in dataset.GetPatientDataInfos())
                {
                    dataInfo.ResolvePatientReference(context, false);
                }

                dataset.RemoveData(dataset.GetPatientDataInfos().Where(dataInfo => dataInfo.Patient == null));
            }

            foreach (Visualization visualization in m_Visualizations)
            {
                visualization.ResolvePatientReferences(context, false);
            }

            foreach (Group group in m_Groups)
            {
                group.ResolvePatientReferences(context, false);
            }

            InvalidateValidation(validationRequest);
        }

        public void AddPatient(Patient patient)
        {
            m_Patients.Add(patient);
            InvalidateValidation(new ValidationRequest(ValidationAspect.PatientAssets | ValidationAspect.ChannelMapping, patientIDs: new[] { patient.ID }, force: true));
        }

        public void AddPatient(IEnumerable<Patient> patients)
        {
            Patient[] added = patients.ToArray();
            m_Patients.AddRange(added);
            InvalidateValidation(new ValidationRequest(ValidationAspect.PatientAssets | ValidationAspect.ChannelMapping, patientIDs: added.Select(patient => patient.ID), force: true));
        }

        public void RemovePatient(Patient patient)
        {
            foreach (Group group in m_Groups)
            {
                group.Patients.Remove(patient);
            }

            foreach (Dataset dataset in m_Datasets)
            {
                dataset.RemoveData(from data in dataset.GetPatientDataInfos() where data.Patient == patient select data);
            }

            foreach (Visualization visualization in m_Visualizations)
            {
                visualization.Patients.Remove(patient);
            }

            m_Patients.Remove(patient);
            InvalidateValidation(new ValidationRequest(ValidationAspect.None));
        }

        public void RemovePatient(IEnumerable<Patient> patients)
        {
            foreach (Patient patient in patients.ToArray())
            {
                foreach (Group group in m_Groups)
                {
                    group.Patients.Remove(patient);
                }

                foreach (Dataset dataset in m_Datasets)
                {
                    dataset.RemoveData(dataset.GetPatientDataInfos().Where(data => data.Patient == patient));
                }

                foreach (Visualization visualization in m_Visualizations)
                {
                    visualization.Patients.Remove(patient);
                }

                m_Patients.Remove(patient);
            }

            InvalidateValidation(new ValidationRequest(ValidationAspect.None));
        }

        // Groups.
        public void SetGroups(IEnumerable<Group> groups)
        {
            this.m_Groups = new List<Group>();
            AddGroup(groups);
        }

        public void AddGroup(Group group)
        {
            m_Groups.Add(group);
        }

        public void AddGroup(IEnumerable<Group> groups)
        {
            foreach (Group group in groups)
            {
                AddGroup(group);
            }
        }

        public void RemoveGroup(Group group)
        {
            m_Groups.Remove(group);
        }

        public void RemoveGroup(IEnumerable<Group> groups)
        {
            foreach (Group group in groups)
            {
                RemoveGroup(group);
            }
        }

        // Datasets.
        public void SetDatasets(IEnumerable<Dataset> datasets, ValidationRequest validationRequest = null)
        {
            List<Dataset> oldDatasets = m_Datasets;
            List<Dataset> newDatasets = datasets?.ToList() ?? new List<Dataset>();
            validationRequest ??= ValidationImpactAnalyzer.ForDatasets(oldDatasets, newDatasets);
            m_Datasets = newDatasets;
            foreach (Visualization visualization in m_Visualizations)
            {
                Column[] columnsToRemove = visualization.Columns.Where(ReferencesMissingDataset).ToArray();
                foreach (Column column in columnsToRemove)
                {
                    visualization.Columns.Remove(column);
                }
            }

            InvalidateValidation(validationRequest);
        }

        public void AddDataset(Dataset dataset)
        {
            m_Datasets.Add(dataset);
            InvalidateValidation(new ValidationRequest(ValidationAspect.DataInfoAll, dataInfoIDs: dataset.Data.Select(dataInfo => dataInfo.ID), force: true));
        }

        public void AddDataset(IEnumerable<Dataset> datasets)
        {
            Dataset[] added = datasets.ToArray();
            m_Datasets.AddRange(added);
            InvalidateValidation(new ValidationRequest(ValidationAspect.DataInfoAll, dataInfoIDs: added.SelectMany(dataset => dataset.Data).Select(dataInfo => dataInfo.ID), force: true));
        }

        public void RemoveDataset(Dataset dataset)
        {
            foreach (Visualization visualization in m_Visualizations)
            {
                visualization.Columns.RemoveAll(column => ReferencesDataset(column, dataset));
            }

            m_Datasets.Remove(dataset);
            InvalidateValidation(new ValidationRequest(ValidationAspect.None));
        }

        public void RemoveDataset(IEnumerable<Dataset> datasets)
        {
            foreach (Dataset dataset in datasets.ToArray())
            {
                foreach (Visualization visualization in m_Visualizations)
                {
                    visualization.Columns.RemoveAll(column => ReferencesDataset(column, dataset));
                }

                m_Datasets.Remove(dataset);
            }

            InvalidateValidation(new ValidationRequest(ValidationAspect.None));
        }

        // Visualizations.
        public void SetVisualizations(IEnumerable<Visualization> visualizations)
        {
            this.m_Visualizations = new List<Visualization>();
            AddVisualization(visualizations);
        }

        public void AddVisualization(Visualization visualization)
        {
            m_Visualizations.Add(visualization);
        }

        public void AddVisualization(IEnumerable<Visualization> visualizations)
        {
            foreach (Visualization visualization in visualizations)
            {
                AddVisualization(visualization);
            }
        }

        public void RemoveVisualization(Visualization visualization)
        {
            m_Visualizations.Remove(visualization);
        }

        public void RemoveVisualization(IEnumerable<Visualization> visualizations)
        {
            foreach (Visualization visualization in visualizations)
            {
                RemoveVisualization(visualization);
            }
        }

        #endregion

        #region Public Methods

        public static bool IsProject(string path)
        {
            try
            {
                _ = ProjectManifest.Read(path, false);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static IEnumerable<string> GetProject(string path)
        {
            return GetProjectInfos(path).Select(project => project.Path);
        }

        public static IEnumerable<ProjectInfo> GetProjectInfos(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                DirectoryInfo directory = new(path);
                if (directory.Exists)
                {
                    FileInfo[] files = directory.GetFiles("*" + EXTENSION);
                    List<ProjectInfo> projects = new();
                    foreach (FileInfo file in files)
                    {
                        try
                        {
                            projects.Add(new ProjectInfo(file.FullName));
                        }
                        catch (DirectoryNotProjectException)
                        {
                        }
                    }

                    return projects;
                }
            }

            return Array.Empty<ProjectInfo>();
        }

        public string GetProject(string path, string ID)
        {
            return GetProjectInfos(path).FirstOrDefault(project => project.SettingsLoadException == null && project.Settings.ID == ID)?.Path;
        }

        public async UniTask<Dictionary<string, List<Tuple<BaseData, string>>>> CheckProjectIDsAsync()
        {
            return await UniTask.RunOnThreadPool(() =>
            {
                Dictionary<string, List<Tuple<BaseData, string>>> dataByID = new();

                void addToDict(BaseData data, string name)
                {
                    string id = data.ID ?? string.Empty;
                    if (dataByID.ContainsKey(id)) dataByID[id].Add(new Tuple<BaseData, string>(data, name));
                    else dataByID.Add(id, new List<Tuple<BaseData, string>>(new Tuple<BaseData, string>[] { new(data, name) }));
                }

                string getName(INameable data)
                {
                    return string.Format("{0} ({1})", data.Name, getType(data as BaseData));
                }

                string getType(BaseData data)
                {
                    return data.GetType().ToString().Split('.').Last();
                }

                // Settings
                addToDict(Preferences, getType(Preferences));
                // Patients
                foreach (var patient in m_Patients)
                {
                    addToDict(patient, getName(patient));
                    foreach (var mesh in patient.Meshes) addToDict(mesh, string.Format("{0} / {1}", getName(patient), getName(mesh)));
                    foreach (var mri in patient.MRIs) addToDict(mri, string.Format("{0} / {1}", getName(patient), getName(mri)));
                    foreach (var site in patient.Sites)
                    {
                        addToDict(site, string.Format("{0} / {1}", getName(patient), getName(site)));
                        foreach (var coordinate in site.Coordinates) addToDict(coordinate, string.Format("{0} / {1} / {2}", getName(patient), getName(site), string.Format("{0} ({1})", coordinate.ReferenceSystem, getType(coordinate))));
                        foreach (var tagValue in site.Tags) addToDict(tagValue, string.Format("{0} / {1} / {2}", getName(patient), getName(site), string.Format("{0} ({1})", tagValue.Tag.Name, getType(tagValue))));
                    }

                    foreach (var tagValue in patient.Tags) addToDict(tagValue, string.Format("{0} / {1}", getName(patient), string.Format("{0} ({1})", tagValue.Tag.Name, getType(tagValue))));
                }

                // Groups
                foreach (var group in m_Groups) addToDict(group, getName(group));
                // Datasets
                foreach (var dataset in m_Datasets)
                {
                    addToDict(dataset, getName(dataset));
                    foreach (var data in dataset.Data)
                    {
                        addToDict(data, string.Format("{0} / {1}", getName(dataset), getName(data)));
                        addToDict(data.DataContainer, string.Format("{0} / {1} / {2}", getName(dataset), getName(data), getType(data.DataContainer)));
                    }
                }

                // Visualizations
                foreach (var visualization in m_Visualizations)
                {
                    addToDict(visualization, getName(visualization));
                    foreach (var column in visualization.Columns)
                    {
                        addToDict(column, string.Format("{0} / {1}", getName(visualization), getName(column)));
                        addToDict(column.BaseConfiguration, string.Format("{0} / {1} / {2}", getName(visualization), getName(column), getType(column.BaseConfiguration)));
                        foreach (var siteConfig in column.BaseConfiguration.ConfigurationBySite) addToDict(siteConfig.Value, string.Format("{0} / {1} / {2})", getName(visualization), getName(column), string.Format("{0} ({1})", siteConfig.Key, getType(siteConfig.Value))));
                    }

                    foreach (var anatomicColumn in visualization.AnatomicColumns) addToDict(anatomicColumn.AnatomicConfiguration, string.Format("{0} / {1} / {2}", getName(visualization), getName(anatomicColumn), getType(anatomicColumn.AnatomicConfiguration)));
                    foreach (var ieegColumn in visualization.IEEGColumns) addToDict(ieegColumn.DynamicConfiguration, string.Format("{0} / {1} / {2}", getName(visualization), getName(ieegColumn), getType(ieegColumn.DynamicConfiguration)));
                    foreach (var ccepColumn in visualization.CCEPColumns) addToDict(ccepColumn.DynamicConfiguration, string.Format("{0} / {1} / {2}", getName(visualization), getName(ccepColumn), getType(ccepColumn.DynamicConfiguration)));
                    foreach (var fmriColumn in visualization.FMRIColumns) addToDict(fmriColumn.FMRIConfiguration, string.Format("{0} / {1} / {2}", getName(visualization), getName(fmriColumn), getType(fmriColumn.FMRIConfiguration)));
                    foreach (var megColumn in visualization.MEGColumns) addToDict(megColumn.MEGConfiguration, string.Format("{0} / {1} / {2}", getName(visualization), getName(megColumn), getType(megColumn.MEGConfiguration)));
                }

                // Check unicity and return error string
                Dictionary<string, List<Tuple<BaseData, string>>> problematicData = new();
                foreach (var kv in dataByID)
                    if (kv.Value.Count > 1 || string.IsNullOrEmpty(kv.Key))
                        problematicData.Add(kv.Key, kv.Value);
                return problematicData;
            });
        }

        public async UniTask LoadAsync(ProjectInfo projectInfo, Action<float, float, LoadingText> updateProgress, CancellationToken token)
        {
            await UniTask.SwitchToMainThread();
            SharedLoadingOperation<Project> operation;
            lock (m_LoadingOperationLock)
            {
                if (m_LoadingOperation == null || m_LoadingOperation.IsTerminal)
                {
                    m_LoadingProjectPath = projectInfo.Path;
                    long generation = ++m_LoadingGeneration;
                    m_ValidationRequest = ValidationRequest.Startup;
                    SharedLoadingOperation<Project> createdOperation = null;
                    createdOperation = new SharedLoadingOperation<Project>(generation, async (progress, operationToken) =>
                    {
                        await LoadCoreAsync(projectInfo, (value, duration, text) => progress(value * READY_PROGRESS_WEIGHT, duration, text), operationToken, () => createdOperation.Priority);
                        return this;
                    }, (project, progress, operationToken) => ValidateProjectCoreAsync((value, duration, text) => progress(READY_PROGRESS_WEIGHT + value * (1 - READY_PROGRESS_WEIGHT), duration, text), operationToken, generation, ValidationRequest.Startup, () => createdOperation.Priority));
                    m_LoadingOperation = createdOperation;
                }
                else if (!string.Equals(m_LoadingProjectPath, projectInfo.Path, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("A different project is already loading into this instance.");
                }

                operation = m_LoadingOperation;
            }

            bool backgroundValidation = LoadingConcurrencyPolicy.BackgroundValidationEnabled;
            using IDisposable foreground = operation.AttachForeground();
            using IDisposable progressSubscription = operation.SubscribeProgress(progress =>
            {
                if (!backgroundValidation)
                {
                    updateProgress.Invoke(progress.Value, progress.Duration, progress.Text);
                    return;
                }

                if (progress.State == LoadingOperationState.Validating || progress.Value > READY_PROGRESS_WEIGHT)
                {
                    return;
                }

                updateProgress.Invoke(Math.Min(1, progress.Value / READY_PROGRESS_WEIGHT), progress.Duration, progress.Text);
            });
            using CancellationTokenRegistration cancellationRegistration = token.Register(operation.Cancel);
            try
            {
                if (backgroundValidation)
                {
                    await operation.EnsureReadyAsync();
                }
                else
                {
                    await operation.EnsureValidatedAsync();
                }
            }
            catch (OperationCanceledException)
            {
                throw operation.Exception ?? new OperationCanceledException(token);
            }
        }

        private async UniTask LoadCoreAsync(ProjectInfo projectInfo, Action<float, float, LoadingText> updateProgress, CancellationToken token, Func<LoadingWorkPriority> priorityProvider)
        {
            try
            {
                updateProgress.Invoke(0.0f, 0, new LoadingText("Loading project"));
                token.ThrowIfCancellationRequested();
                await UniTask.SwitchToThreadPool();

                ProjectManifest manifest;
                try
                {
                    manifest = projectInfo.GetCurrentManifest();
                }
                catch (Exception exception)
                {
                    throw new FileNotFoundException(projectInfo.Path, exception);
                }

                // Initialize progress.
                float progress = 0.0f;
                float settingsProgress = 1;
                float patientsProgress = 2 * manifest.Patients;
                float groupsProgress = manifest.Groups;
                float datasetsProgress = manifest.Datasets;
                float visualizationsProgress = manifest.Visualizations;
                float linkingProgress = 1;
                float steps = settingsProgress + groupsProgress + patientsProgress + datasetsProgress + visualizationsProgress + linkingProgress;
                settingsProgress /= steps;
                patientsProgress /= steps;
                groupsProgress /= steps;
                datasetsProgress /= steps;
                visualizationsProgress /= steps;
                linkingProgress /= steps;

                Name = manifest.Name;

                // Load Settings.
                token.ThrowIfCancellationRequested();
                ProjectPreferences preferences = LoadSettings(manifest, (localProgress, duration, text) => updateProgress.Invoke(progress + localProgress * settingsProgress, duration, text));
                token.ThrowIfCancellationRequested();
                progress += settingsProgress;

                int concurrency = LoadingConcurrencyPolicy.Current.GetLimit(LoadingWorkCategory.JsonAndZip);
                int readerCount = Math.Min(concurrency, Math.Max(1, Math.Max(Math.Max(manifest.Patients, manifest.Groups), Math.Max(manifest.Datasets, manifest.Visualizations))));
                List<Patient> patients;
                List<Group> groups;
                List<Dataset> datasets;
                List<Visualization> visualizations;
                ProjectArchiveReader archiveReader = new(manifest.Path, readerCount);
                using (archiveReader)
                {
                    // Load Patients.
                    token.ThrowIfCancellationRequested();
                    patients = await LoadPatientsAsync(manifest, archiveReader, (localProgress, duration, text) => updateProgress.Invoke(progress + localProgress * patientsProgress, duration, text), token, concurrency, priorityProvider);
                    token.ThrowIfCancellationRequested();
                    progress += patientsProgress;

                    // Load Groups.
                    token.ThrowIfCancellationRequested();
                    groups = await LoadGroupsAsync(manifest, archiveReader, (localProgress, duration, text) => updateProgress.Invoke(progress + localProgress * groupsProgress, duration, text), token, concurrency, priorityProvider);
                    token.ThrowIfCancellationRequested();
                    progress += groupsProgress;

                    // Load Datasets.
                    token.ThrowIfCancellationRequested();
                    datasets = await LoadDatasetsAsync(manifest, archiveReader, (localProgress, duration, text) => updateProgress.Invoke(progress + localProgress * datasetsProgress, duration, text), token, concurrency, priorityProvider);
                    token.ThrowIfCancellationRequested();
                    progress += datasetsProgress;

                    // Load Visualizations.
                    token.ThrowIfCancellationRequested();
                    visualizations = await LoadVisualizationsAsync(manifest, archiveReader, (localProgress, duration, text) => updateProgress.Invoke(progress + localProgress * visualizationsProgress, duration, text), token, concurrency, priorityProvider);
                    token.ThrowIfCancellationRequested();
                    progress += visualizationsProgress;
                }

                // Link every serialized ID against the canonical instances before
                // the new graph becomes visible through this Project.
                updateProgress.Invoke(progress, 0, new LoadingText("Linking project references"));
                LoadingContext context = new(PersistentDataManager.Tags.AllTags, DatabaseManager.Database.Protocols, patients, datasets);
                context.ResolveProject(patients, groups, datasets, visualizations);

                ISet<string> tagIds = new HashSet<string>(context.TagById.Keys, StringComparer.Ordinal);
                int tagConcurrency = LoadingConcurrencyPolicy.Current.GetLimit(LoadingWorkCategory.Metadata);
                await LoadingWorkScheduler.Shared.RunAsync(patients.Select(patient => (Func<UniTask>)(() => patient.CheckTagsAsync(tagIds))), LoadingWorkCategory.Metadata, priorityProvider, token, null, tagConcurrency);
                token.ThrowIfCancellationRequested();
                progress += linkingProgress;

                await UniTask.SwitchToMainThread();
                Preferences = preferences;
                m_Patients = patients;
                m_Groups = groups;
                m_Datasets = datasets;
                m_Visualizations = visualizations;
                lock (m_LoadingOperationLock)
                {
                    m_ValidationPublished = false;
                }

                token.ThrowIfCancellationRequested();
                updateProgress.Invoke(1.0f, 0, new LoadingText("Project loaded successfully"));
            }
            finally
            {
                await UniTask.SwitchToMainThread();
            }
        }

        public virtual async UniTask EnsureProjectValidatedAsync(Action<float, float, LoadingText> updateProgress, CancellationToken token = default)
        {
            await UniTask.SwitchToMainThread();
            SharedLoadingOperation<Project> operation;
            lock (m_LoadingOperationLock)
            {
                if (m_LoadingOperation == null)
                {
                    long generation = ++m_LoadingGeneration;
                    m_LoadingOperation = CreateValidationOperation(generation, m_ValidationRequest);
                }

                operation = m_LoadingOperation;
            }

            if (operation.State == LoadingOperationState.Validated || operation.State == LoadingOperationState.ValidatedWithIssues)
            {
                using IDisposable foreground = operation.AttachForeground();
                await operation.EnsureValidatedAsync(token);
                return;
            }

            using IDisposable foregroundLease = operation.AttachForeground();
            updateProgress?.Invoke(0, 0, new LoadingText("Validating project"));
            using IDisposable progressSubscription = operation.SubscribeProgress(progress =>
            {
                if (progress.Value < READY_PROGRESS_WEIGHT)
                {
                    return;
                }

                float validationProgress = Math.Min(1, (progress.Value - READY_PROGRESS_WEIGHT) / (1 - READY_PROGRESS_WEIGHT));
                updateProgress?.Invoke(validationProgress, progress.Duration, progress.Text);
            });
            await operation.EnsureValidatedAsync(token);
        }

        public virtual async UniTask EnsureProjectValidatedAsync(ValidationRequest request, Action<float, float, LoadingText> updateProgress, CancellationToken token = default)
        {
            if (RequiresValidation(request))
            {
                InvalidateValidation(request);
            }

            await EnsureProjectValidatedAsync(updateProgress, token);
        }

        public virtual async UniTask EnsureProjectValidatedForImmediateLoadAsync(ValidationRequest request, Action<float, float, LoadingText> updateProgress, CancellationToken token = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request.Aspects == ValidationAspect.None)
            {
                return;
            }

            await UniTask.SwitchToMainThread();
            long generation;
            lock (m_LoadingOperationLock)
            {
                generation = m_LoadingGeneration;
            }

            await ValidateProjectCoreAsync(updateProgress, token, generation, request, () => LoadingWorkPriority.Foreground, DataManager.CreatePreloadingValidationMetadataReader(), false);
        }

        public void InvalidateValidation(ValidationRequest request = null)
        {
            request ??= ValidationRequest.Full;
            MarkRequestedValidationStale(request);
            SharedLoadingOperation<Project> operation;
            SharedLoadingOperation<Project> replacement;
            lock (m_LoadingOperationLock)
            {
                if (m_LoadingOperation == null)
                {
                    return;
                }

                operation = m_LoadingOperation;
                m_LoadingProjectPath = null;
                long generation = ++m_LoadingGeneration;
                m_ValidationRequest = m_ValidationPublished ? request : m_ValidationRequest.Merge(request);
                m_ValidationPublished = false;
                replacement = CreateValidationOperation(generation, m_ValidationRequest);
                m_LoadingOperation = replacement;
            }

            operation.Cancel();
            _ = replacement.EnsureReadyAsync();
            OnValidationStateChanged?.Invoke();
        }

        private void MarkRequestedValidationStale(ValidationRequest request)
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
            foreach (DataInfo dataInfo in m_Datasets.SelectMany(dataset => dataset.Data))
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

        private SharedLoadingOperation<Project> CreateValidationOperation(long generation, ValidationRequest request)
        {
            SharedLoadingOperation<Project> operation = null;
            operation = new SharedLoadingOperation<Project>(generation, (progress, operationToken) => UniTask.FromResult(this), (project, progress, operationToken) => ValidateProjectCoreAsync((value, duration, text) => progress(READY_PROGRESS_WEIGHT + value * (1 - READY_PROGRESS_WEIGHT), duration, text), operationToken, generation, request, () => operation.Priority));
            return operation;
        }

        private async UniTask<bool> ValidateProjectCoreAsync(Action<float, float, LoadingText> updateProgress, CancellationToken token, long generation, ValidationRequest request, Func<LoadingWorkPriority> priorityProvider, IEEGValidationMetadataReader metadataReader = null, bool publishValidation = true)
        {
            try
            {
                await UniTask.SwitchToMainThread();
                Patient[] patients = request.Includes(ValidationAspect.PatientAssets) ? m_Patients.Where(request.Matches).ToArray() : Array.Empty<Patient>();
                DataInfo[] dataInfos = m_Datasets.SelectMany(dataset => dataset.Data).Where(request.Matches).ToArray();
                float pathWeight = patients.Length == 0 ? 0 : dataInfos.Length == 0 ? 1 : (float)patients.Length / (patients.Length + dataInfos.Length);
                LoadingConcurrencyPolicy concurrencyPolicy = LoadingConcurrencyPolicy.Current;
                int pathConcurrency = concurrencyPolicy.GetLimit(LoadingWorkCategory.FileSystem);
                int dataInfoConcurrency = concurrencyPolicy.GetLimit(DataInfoValidator.GetWorkCategory(request));

                PatientAssetValidationResult assetResult = await new AssetReferenceValidator().ValidatePatientsAsync(patients, pathConcurrency, token, (completed, total) => updateProgress((total == 0 ? 1 : (float)completed / total) * pathWeight, completed == 0 ? 0 : 0.2f, total == 0 ? new LoadingText("Validating patient file references") : new LoadingText("Validating patient file references", " ", completed + "/" + total)), generation, priorityProvider);

                DataInfoValidationResult dataInfoResult = await new DataInfoValidator(metadataReader).ValidateAsync(dataInfos, request, dataInfoConcurrency, token, (completed, total) => updateProgress(pathWeight + (total == 0 ? 1 : (float)completed / total) * (1 - pathWeight), completed == 0 ? 0 : 0.2f, total == 0 ? new LoadingText("Validating project data") : new LoadingText("Validating project data", " ", completed + "/" + total)), generation, priorityProvider);

                token.ThrowIfCancellationRequested();
                await UniTask.SwitchToMainThread();
                lock (m_LoadingOperationLock)
                {
                    if (generation != m_LoadingGeneration || !assetResult.TryApply(m_LoadingGeneration) || !dataInfoResult.TryApply(m_LoadingGeneration))
                    {
                        throw new OperationCanceledException("The project validation generation is obsolete.", token);
                    }

                    if (publishValidation)
                    {
                        m_ValidationPublished = true;
                    }
                }

                OnValidationStateChanged?.Invoke();
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

        public async UniTask SaveAsync(string path, Action<float, float, LoadingText> updateProgress, CancellationToken token)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new Exceptions.DirectoryNotFoundException("");
            }

            if (!Directory.Exists(path))
            {
                throw new Exceptions.DirectoryNotFoundException(path);
            }

            float validationWeight = NeedsValidationWait ? 0.2f : 0;
            if (validationWeight > 0)
            {
                await EnsureProjectValidatedAsync((progress, duration, text) => updateProgress(progress * validationWeight, duration, text), token);
            }

            await SaveCoreAsync(path, (progress, duration, text) => updateProgress(validationWeight + progress * (1 - validationWeight), duration, text), token);
        }

        private async UniTask SaveCoreAsync(string path, Action<float, float, LoadingText> updateProgress, CancellationToken token)
        {
            try
            {
                // Initialize progress.
                float steps = 12 + m_Patients.Count + m_Groups.Count + m_Datasets.Count + m_Visualizations.Count;
                float progress = 0.0f;

                float initializationProgress = 1 / steps;
                float settingsProgress = 1 / steps;
                float patientsProgress = m_Patients.Count / steps;
                float groupsProgress = m_Groups.Count / steps;
                float datasetsProgress = m_Datasets.Count / steps;
                float visualizationsProgress = m_Visualizations.Count / steps;
                float finalizationProgress = 10 / steps;

                // Initialization.
                updateProgress.Invoke(progress, 0, new LoadingText("Initialization"));

                DirectoryInfo projectDirectory = Directory.Exists(ApplicationState.ExtractProjectFolder) ? new DirectoryInfo(ApplicationState.ExtractProjectFolder) : Directory.CreateDirectory(ApplicationState.ExtractProjectFolder);
                progress += initializationProgress;

                updateProgress.Invoke(progress, 0, new LoadingText("Saving project"));

                if (HasInvalidFileNameChars(FileName))
                {
                    throw new CanNotSaveSettingsException();
                }

                // Save Settings.
                token.ThrowIfCancellationRequested();
                await SaveSettingsAsync(projectDirectory, (localProgress, duration, text) => updateProgress.Invoke(progress + localProgress * settingsProgress, duration, text));
                progress += settingsProgress;

                // Save Patients
                token.ThrowIfCancellationRequested();
                await SavePatientsAsync(projectDirectory, (localProgress, duration, text) => updateProgress.Invoke(progress + localProgress * patientsProgress, duration, text), token);
                progress += patientsProgress;

                // Save Groups.
                token.ThrowIfCancellationRequested();
                await SaveGroupsAsync(projectDirectory, (localProgress, duration, text) => updateProgress.Invoke(progress + localProgress * groupsProgress, duration, text), token);
                progress += groupsProgress;

                // Save Datasets
                token.ThrowIfCancellationRequested();
                await SaveDatasetsAsync(projectDirectory, (localProgress, duration, text) => updateProgress.Invoke(progress + localProgress * datasetsProgress, duration, text), token);
                progress += datasetsProgress;

                // Save Visualizations.
                token.ThrowIfCancellationRequested();
                await SaveVisualizationsAsync(projectDirectory, (localProgress, duration, text) => updateProgress.Invoke(progress + localProgress * visualizationsProgress, duration, text), token);
                progress += visualizationsProgress;

                // Zipping
                token.ThrowIfCancellationRequested();
                updateProgress.Invoke(progress + finalizationProgress, 0.75f, new LoadingText("Finalizing"));
                progress += finalizationProgress;
                await UniTask.SwitchToThreadPool();
                string filePath = Path.Combine(path, FileName);
                if (File.Exists(filePath)) File.Delete(filePath);
                using (ZipFile zip = new(filePath))
                {
                    zip.AddDirectory(ApplicationState.ExtractProjectFolder);
                    zip.Save();
                }

                await UniTask.SwitchToMainThread();

                updateProgress.Invoke(1, 0, new LoadingText("Project saved successfully"));
            }
            catch
            {
                throw;
            }
            finally
            {
                if (Directory.Exists(ApplicationState.ExtractProjectFolder)) Directory.Delete(ApplicationState.ExtractProjectFolder, true);
                await UniTask.SwitchToMainThread();
            }
        }

        #endregion

        #region Private Methods

        private ProjectPreferences LoadSettings(ProjectManifest manifest, Action<float, float, LoadingText> updateProgress)
        {
            updateProgress.Invoke(0, 0, new LoadingText("Loading settings"));
            if (manifest.SettingsEntries.Count == 0)
            {
                throw new SettingsFileNotFoundException();
            }

            if (manifest.SettingsEntries.Count > 1)
            {
                throw new MultipleSettingsFilesFoundException();
            }

            if (manifest.PreferencesLoadException != null)
            {
                throw new CanNotReadSettingsFileException(Path.GetFileName(manifest.SettingsEntries[0]), manifest.PreferencesLoadException);
            }

            updateProgress.Invoke(1.0f, 0, new LoadingText("Settings loaded successfully"));
            return manifest.Preferences;
        }

        private bool ReferencesMissingDataset(Column column)
        {
            Dataset dataset = GetColumnDataset(column);
            return IsDatasetBackedColumn(column) && (dataset == null || !m_Datasets.Contains(dataset));
        }

        private static bool ReferencesDataset(Column column, Dataset dataset)
        {
            return GetColumnDataset(column) == dataset;
        }

        private static bool IsDatasetBackedColumn(Column column)
        {
            return column is IEEGColumn or CCEPColumn or FMRIColumn or MEGColumn or StaticColumn;
        }

        private static Dataset GetColumnDataset(Column column)
        {
            return column switch
            {
                IEEGColumn ieegColumn => ieegColumn.Dataset,
                CCEPColumn ccepColumn => ccepColumn.Dataset,
                FMRIColumn fmriColumn => fmriColumn.Dataset,
                MEGColumn megColumn => megColumn.Dataset,
                StaticColumn staticColumn => staticColumn.Dataset,
                _ => null
            };
        }

        private async UniTask<List<Patient>> LoadPatientsAsync(ProjectManifest manifest, ProjectArchiveReader archiveReader, Action<float, float, LoadingText> updateProgress, CancellationToken token, int concurrency, Func<LoadingWorkPriority> priorityProvider)
        {
            var tasks = manifest.PatientEntries.Select(entryName => (Func<UniTask<Patient>>)(async () =>
            {
                try
                {
                    Patient patient = await archiveReader.ReadAsync<Patient>(manifest, entryName, token);
                    return patient;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    throw new CanNotReadPatientFileException(Path.GetFileNameWithoutExtension(entryName), e);
                }
            }));
            List<Patient> patients = (await RunLoadingTasksAsync(tasks, "Loading patients", updateProgress, token, concurrency, priorityProvider)).ToList();
            updateProgress.Invoke(1.0f, 0, new LoadingText("Patients loaded successfully"));
            return patients;
        }

        private async UniTask<List<Group>> LoadGroupsAsync(ProjectManifest manifest, ProjectArchiveReader archiveReader, Action<float, float, LoadingText> updateProgress, CancellationToken token, int concurrency, Func<LoadingWorkPriority> priorityProvider)
        {
            var tasks = manifest.GroupEntries.Select(entryName => (Func<UniTask<Group>>)(async () =>
            {
                try
                {
                    return await archiveReader.ReadAsync<Group>(manifest, entryName, token);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    throw new CanNotReadGroupFileException(Path.GetFileNameWithoutExtension(entryName), e);
                }
            }));
            List<Group> groups = (await RunLoadingTasksAsync(tasks, "Loading groups", updateProgress, token, concurrency, priorityProvider)).ToList();
            updateProgress.Invoke(1.0f, 0, new LoadingText("Groups loaded successfully"));
            return groups;
        }

        private async UniTask<List<Dataset>> LoadDatasetsAsync(ProjectManifest manifest, ProjectArchiveReader archiveReader, Action<float, float, LoadingText> updateProgress, CancellationToken token, int concurrency, Func<LoadingWorkPriority> priorityProvider)
        {
            var tasks = manifest.DatasetEntries.Select(entryName => (Func<UniTask<Dataset>>)(async () =>
            {
                try
                {
                    return await archiveReader.ReadAsync<Dataset>(manifest, entryName, token);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    throw new CanNotReadDatasetFileException(Path.GetFileNameWithoutExtension(entryName), e);
                }
            }));
            List<Dataset> datasets = (await RunLoadingTasksAsync(tasks, "Loading datasets", updateProgress, token, concurrency, priorityProvider)).ToList();
            updateProgress.Invoke(1.0f, 0, new LoadingText("Datasets loaded successfully"));
            return datasets;
        }

        private async UniTask<List<Visualization>> LoadVisualizationsAsync(ProjectManifest manifest, ProjectArchiveReader archiveReader, Action<float, float, LoadingText> updateProgress, CancellationToken token, int concurrency, Func<LoadingWorkPriority> priorityProvider)
        {
            var tasks = manifest.VisualizationEntries.Select(entryName => (Func<UniTask<Visualization>>)(async () =>
            {
                try
                {
                    return await archiveReader.ReadAsync<Visualization>(manifest, entryName, token);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    throw new CanNotReadVisualizationFileException(Path.GetFileNameWithoutExtension(entryName), e);
                }
            }));
            List<Visualization> visualizations = (await RunLoadingTasksAsync(tasks, "Loading visualizations", updateProgress, token, concurrency, priorityProvider)).ToList();
            updateProgress.Invoke(1.0f, 0, new LoadingText("Visualizations loaded successfully"));
            return visualizations;
        }

        private static async UniTask<T[]> RunLoadingTasksAsync<T>(IEnumerable<Func<UniTask<T>>> tasks, string loadingText, Action<float, float, LoadingText> updateProgress, CancellationToken token, int concurrency, Func<LoadingWorkPriority> priorityProvider)
        {
            return await LoadingWorkScheduler.Shared.RunAsync(tasks, LoadingWorkCategory.JsonAndZip, priorityProvider, token, (completed, total) => updateProgress(total == 0 ? 1 : (float)completed / total, completed == 0 ? 0 : 0.2f, total == 0 ? new LoadingText(loadingText) : new LoadingText(loadingText, " ", completed + "/" + total)), concurrency);
        }

        private static async UniTask RunLoadingTasksAsync(IEnumerable<Func<UniTask>> tasks, string loadingText, Action<float, float, LoadingText> updateProgress, CancellationToken token)
        {
            int concurrency = LoadingConcurrencyPolicy.Current.GetLimit(LoadingWorkCategory.JsonAndZip);
            await LoadingWorkScheduler.Shared.RunAsync(tasks, LoadingWorkCategory.JsonAndZip, () => LoadingWorkPriority.Foreground, token, (completed, total) => updateProgress(total == 0 ? 1 : (float)completed / total, completed == 0 ? 0 : 0.2f, total == 0 ? new LoadingText(loadingText) : new LoadingText(loadingText, " ", completed + "/" + total)), concurrency);
        }

        private async UniTask SaveSettingsAsync(DirectoryInfo projectDirectory, Action<float, float, LoadingText> updateProgress)
        {
            updateProgress.Invoke(0, 0, new LoadingText("Saving settings"));
            try
            {
                Preferences.Version = ApplicationState.Version;
                await ClassLoaderSaver.SaveToJsonAsync(Preferences, Path.Combine(projectDirectory.FullName, Name + ProjectPreferences.EXTENSION));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw new CanNotSaveSettingsException();
            }

            updateProgress.Invoke(1.0f, 0, new LoadingText("Settings saved successfully"));
        }

        private async UniTask SavePatientsAsync(DirectoryInfo projectDirectory, Action<float, float, LoadingText> updateProgress, CancellationToken token)
        {
            DirectoryInfo patientDirectory = Directory.CreateDirectory(Path.Combine(projectDirectory.FullName, "Patients"));
            HashSet<string> reservedPaths = new(StringComparer.OrdinalIgnoreCase);
            var tasks = m_Patients.Select(patient =>
            {
                string patientPath = ReserveUniqueFilePath(Path.Combine(patientDirectory.FullName, BuildSafeFileName(patient.ID, Patient.EXTENSION, "patient")), reservedPaths);
                return (Func<UniTask>)(async () =>
                {
                    try
                    {
                        await ClassLoaderSaver.SaveToJsonAsync(patient, patientPath, true);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        throw new CanNotSaveSettingsException();
                    }
                });
            });
            await RunLoadingTasksAsync(tasks, "Saving patients", updateProgress, token);
            updateProgress.Invoke(1.0f, 0, new LoadingText("Patients saved successfully"));
        }

        private async UniTask SaveGroupsAsync(DirectoryInfo projectDirectory, Action<float, float, LoadingText> updateProgress, CancellationToken token)
        {
            DirectoryInfo groupDirectory = Directory.CreateDirectory(Path.Combine(projectDirectory.FullName, "Groups"));
            HashSet<string> reservedPaths = new(StringComparer.OrdinalIgnoreCase);
            var tasks = m_Groups.Select(group =>
            {
                string groupPath = ReserveUniqueFilePath(Path.Combine(groupDirectory.FullName, BuildSafeFileName(group.Name, Group.EXTENSION, "group")), reservedPaths);
                return (Func<UniTask>)(async () =>
                {
                    try
                    {
                        await ClassLoaderSaver.SaveToJsonAsync(group, groupPath, true);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        throw new CanNotSaveSettingsException();
                    }
                });
            });
            await RunLoadingTasksAsync(tasks, "Saving groups", updateProgress, token);
            updateProgress.Invoke(1.0f, 0, new LoadingText("Groups saved successfully"));
        }

        private async UniTask SaveDatasetsAsync(DirectoryInfo projectDirectory, Action<float, float, LoadingText> updateProgress, CancellationToken token)
        {
            DirectoryInfo datasetDirectory = Directory.CreateDirectory(Path.Combine(projectDirectory.FullName, "Datasets"));
            HashSet<string> reservedPaths = new(StringComparer.OrdinalIgnoreCase);
            var tasks = m_Datasets.Select(dataset =>
            {
                string datasetPath = ReserveUniqueFilePath(Path.Combine(datasetDirectory.FullName, BuildSafeFileName(dataset.Name, Dataset.EXTENSION, "dataset")), reservedPaths);
                return (Func<UniTask>)(async () =>
                {
                    try
                    {
                        await ClassLoaderSaver.SaveToJsonAsync(dataset, datasetPath, true);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        throw new CanNotSaveSettingsException();
                    }
                });
            });
            await RunLoadingTasksAsync(tasks, "Saving datasets", updateProgress, token);
            updateProgress.Invoke(1.0f, 0, new LoadingText("Datasets saved successfully"));
        }

        private async UniTask SaveVisualizationsAsync(DirectoryInfo projectDirectory, Action<float, float, LoadingText> updateProgress, CancellationToken token)
        {
            DirectoryInfo visualizationDirectory = Directory.CreateDirectory(Path.Combine(projectDirectory.FullName, "Visualizations"));
            HashSet<string> reservedPaths = new(StringComparer.OrdinalIgnoreCase);
            var tasks = m_Visualizations.Select(visualization =>
            {
                string visualizationPath = ReserveUniqueFilePath(Path.Combine(visualizationDirectory.FullName, BuildSafeFileName(visualization.Name, Visualization.EXTENSION, "visualization")), reservedPaths);
                return (Func<UniTask>)(async () =>
                {
                    try
                    {
                        await ClassLoaderSaver.SaveToJsonAsync(visualization, visualizationPath, true);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        throw new CanNotSaveSettingsException();
                    }
                });
            });
            await RunLoadingTasksAsync(tasks, "Saving visualizations", updateProgress, token);
            updateProgress.Invoke(1.0f, 0, new LoadingText("Visualizations saved successfully"));
        }

        private static string ReserveUniqueFilePath(string path, ISet<string> reservedPaths)
        {
            string result = path;
            string extension = Path.GetExtension(path);
            string pathWithoutExtension = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));
            int count = 0;
            while (File.Exists(result) || reservedPaths.Contains(result))
            {
                result = string.Format("{0}({1}){2}", pathWithoutExtension, ++count, extension);
            }

            reservedPaths.Add(result);
            return result;
        }

        private static string BuildSafeFileName(string fileNameWithoutExtension, string extension, string fallbackName)
        {
            string safeName = string.IsNullOrWhiteSpace(fileNameWithoutExtension) ? fallbackName : fileNameWithoutExtension;
            foreach (char invalidChar in Path.GetInvalidFileNameChars())
            {
                safeName = safeName.Replace(invalidChar, '_');
            }

            if (string.IsNullOrWhiteSpace(safeName))
            {
                safeName = fallbackName;
            }

            return safeName + extension;
        }

        private static bool HasInvalidFileNameChars(string fileName)
        {
            return fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0;
        }

        #endregion
    }
}
