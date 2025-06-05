using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;
using HBP.Core.Exceptions;
using HBP.Core.Interfaces;
using HBP.Core.Tools;
using HBP.Data.Preferences;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using System.Threading;

namespace HBP.Core.Data
{
    /**
    * \class Visualization
    * \author Adrien Gannerie
    * \version 2.0
    * \date 09 mai 2017
    * \brief 3D brain visualization.
    * 
    * \details Define a 3D brain visualization and contains:
    * 
    *   - \a ID.
    *   - \a Name.
    *   - \a Configuration.
    *   - \a Columns.   
    */
    [JsonObject(MemberSerialization.OptIn)]
    public class Visualization : BaseData, ILoadable<Visualization>, INameable
    {
        #region Properties
        public const string EXTENSION = ".visualization";
        /// <summary>
        /// Name of the visualization.
        /// </summary>
        [JsonProperty(Order = 2)] public string Name { get; set; }

        [JsonProperty("Patients", Order = 3)] List<string> m_PatientsID;
        /// <summary>
        /// Patients of the Visualization.
        /// </summary>
        public List<Patient> Patients { get; set; }

        /// <summary>
        /// Configuration of the visualization.
        /// </summary>
        [JsonProperty(Order = 4)] public VisualizationConfiguration Configuration { get; set; }

        /// <summary>
        /// Columns of the visualization.
        /// </summary>
        [JsonProperty(Order = 5)] public List<Column> Columns { get; set; }
        /// <summary>
        /// EEG Columns of the visualization.
        /// </summary>
        public ReadOnlyCollection<IEEGColumn> IEEGColumns
        {
            get { return new ReadOnlyCollection<IEEGColumn>(Columns.OfType<IEEGColumn>().ToArray()); }
        }
        /// <summary>
        /// Anatomic columns of the visualization.
        /// </summary>
        public ReadOnlyCollection<AnatomicColumn> AnatomicColumns
        {
            get { return new ReadOnlyCollection<AnatomicColumn>(Columns.OfType<AnatomicColumn>().ToArray()); }
        }

        public ReadOnlyCollection<CCEPColumn> CCEPColumns
        {
            get
            {
                return new ReadOnlyCollection<CCEPColumn>(Columns.OfType<CCEPColumn>().ToArray());
            }
        }

        public ReadOnlyCollection<FMRIColumn> FMRIColumns
        {
            get
            {
                return new ReadOnlyCollection<FMRIColumn>(Columns.OfType<FMRIColumn>().ToArray());
            }
        }

        public ReadOnlyCollection<MEGColumn> MEGColumns
        {
            get
            {
                return new ReadOnlyCollection<MEGColumn>(Columns.OfType<MEGColumn>().ToArray());
            }
        }

        public ReadOnlyCollection<StaticColumn> StaticColumns
        {
            get
            {
                return new ReadOnlyCollection<StaticColumn>(Columns.OfType<StaticColumn>().ToArray());
            }
        }

        /// <summary>
        /// Test if the visualization is visualizable.
        /// </summary>
        public virtual bool IsVisualizable
        {
            get { return Columns.Count > 0 && Columns.All((column) => column.IsCompatible(Patients)); }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new visualization instance.
        /// </summary>
        /// <param name="name">Name of the visualization.</param>
        /// <param name="columns">Columns of the visualization.</param>
        /// <param name="ID">Unique ID.</param>
        public Visualization(string name, IEnumerable<Patient> patients, IEnumerable<Column> columns, VisualizationConfiguration configuration, string ID) : base(ID)
        {
            Name = name;
            Patients = patients.ToList();
            Columns = columns.ToList();
            Configuration = configuration;
        }
        /// <summary>
        /// Create a new visualization instance.
        /// </summary>
        /// <param name="name">Name of the visualization.</param>
        /// <param name="columns">Columns of the visualization.</param>
        /// <param name="id">Unique ID.</param>
        public Visualization(string name, IEnumerable<Patient> patients, IEnumerable<Column> columns, VisualizationConfiguration configuration) : base()
        {
            Name = name;
            Patients = patients.ToList();
            Columns = columns.ToList();
            Configuration = configuration;
        }
        /// <summary>
        /// Create a new visualization instance.
        /// </summary>
        /// <param name="name">Name of the visualization.</param>
        /// <param name="columns">Columns of the visualization.</param>
        /// <param name="ID">Unique ID.</param>
        public Visualization(string name, IEnumerable<Patient> patients, IEnumerable<Column> columns, string ID) : this(name, patients, columns, new VisualizationConfiguration(), ID)
        {
        }
        /// <summary>
        /// Create a new visualization instance.
        /// </summary>
        /// <param name="name">Name of the visualization.</param>
        /// <param name="columns">Columns of the visualization.</param>
        public Visualization(string name, IEnumerable<Patient> patients, IEnumerable<Column> columns) : this(name, patients, columns, new VisualizationConfiguration())
        {
        }
        /// <summary>
        /// Create a new visualization instance with default value.
        /// </summary>
        public Visualization() : this("Unknown", new Patient[0], new Column[0])
        {

        }
        #endregion

        #region Public Static Methods
        public static bool LoadFromFile(string path, out Visualization result)
        {
            result = null;
            try
            {
                result = ClassLoaderSaver.LoadFromJson<Visualization>(path);
                return result != null;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
                throw new CanNotReadVisualizationFileException(Path.GetFileNameWithoutExtension(path));
            }
        }
        public static string[] GetExtensions()
        {
            return new string[] { EXTENSION[0] == '.' ? EXTENSION.Substring(1) : EXTENSION };
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Load the visualization.
        /// </summary>
        /// <returns></returns>
        public async UniTask LoadAsync(Action<float, float, LoadingText> onChangeProgress, CancellationToken token)
        {
            await UniTask.SwitchToThreadPool();

            int nbDynamicColumns = CCEPColumns.Count + IEEGColumns.Count;
            int nbFMRIColumns = FMRIColumns.Count;
            int nbMEGColumns = MEGColumns.Count;
            int nbStaticColumns = StaticColumns.Count;

            onChangeProgress(0, 0, new LoadingText("Loading Visualization"));

            int nbPatients = Patients.Count;

            float steps = 1 + 2 * nbPatients * nbDynamicColumns + nbFMRIColumns + nbMEGColumns + nbStaticColumns;
            float progress = 0.0f;

            float findDataInfoToReadProgress = 1 / steps;
            float loadDataProgress = nbPatients * nbDynamicColumns / steps;
            float loadColumnsProgress = nbPatients * nbDynamicColumns / steps;
            float loadFMRIColumnsProgress = nbFMRIColumns / steps;
            float loadMEGColumnsProgress = nbMEGColumns / steps;
            float loadStaticColumnsProgress = nbStaticColumns / steps;

            if (nbDynamicColumns > 0)
            {
                var dataInfoByColumn = await FindDataInfoToReadAsync((localProgress, duration, text) => onChangeProgress(progress + localProgress * findDataInfoToReadProgress, duration, text), token);
                progress += findDataInfoToReadProgress;

                await LoadDataAsync(dataInfoByColumn, (localProgress, duration, text) => onChangeProgress(progress + localProgress * loadDataProgress, duration, text), token);
                progress += loadDataProgress;

                await LoadColumnsAsync(dataInfoByColumn, (localProgress, duration, text) => onChangeProgress(progress + localProgress * loadColumnsProgress, duration, text), token);
                progress += loadColumnsProgress;
            }
            if (nbFMRIColumns > 0)
            {
                await LoadFMRIColumnsAsync((localProgress, duration, text) => onChangeProgress(progress + localProgress * loadFMRIColumnsProgress, duration, text), token);
                progress += loadFMRIColumnsProgress;
            }
            if (nbMEGColumns > 0)
            {
                await LoadMEGColumnsAsync((localProgress, duration, text) => onChangeProgress(progress + localProgress * loadMEGColumnsProgress, duration, text), token);
                progress += loadMEGColumnsProgress;
            }
            if (nbStaticColumns > 0)
            {
                await LoadStaticColumnsAsync((localProgress, duration, text) => onChangeProgress(progress + localProgress * loadStaticColumnsProgress, duration, text), token);
                progress += loadFMRIColumnsProgress;
            }

            onChangeProgress(1.0f, 0, new LoadingText("Visualization loaded successfully"));
        }
        /// <summary>
        /// Swap two columns by index.
        /// </summary>
        /// <param name="index1">Index of the first column to swap.</param>
        /// <param name="index2">Index of the second column to swap.</param>
        public void SwapColumns(int index1,int index2)
        {
            Column tmp = Columns[index1];
            Columns[index1] = Columns[index2];
            Columns[index2] = tmp;
        }
        /// <summary>
        /// Get the DataInfo of the column.
        /// </summary>
        /// <param name="column">Column</param>
        /// <returns>DataInfo of the column.</returns>
        public IEnumerable<DataInfo> GetDataInfo(Column column)
        {
            if (column is IEEGColumn iEEGColumn)
            {
                return iEEGColumn.Dataset.GetIEEGDataInfos().Where((data) => (iEEGColumn.DataName == data.Name && Patients.Contains(data.Patient)));
            }
            else if (column is CCEPColumn ccepColumn)
            {
                return ccepColumn.Dataset.GetCCEPDataInfos().Where((data) => (ccepColumn.DataName == data.Name && Patients.Contains(data.Patient)));
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// Get the DataInfo used by the column for a specific Patient.
        /// </summary>
        /// <param name="patient">Patient concerned.</param>
        /// <param name="column">Column concerned.</param>
        /// <returns>DataInfo used by the column for the specific Patient.</returns>
        public DataInfo GetDataInfo(Patient patient, Column column)
        {
            if (column is IEEGColumn iEEGColumn)
            {
                return GetDataInfo(column).OfType<IEEGDataInfo>().First((dataInfo) => dataInfo.Patient == patient);
            }
            else if (column is CCEPColumn ccepColumn)
            {
                return GetDataInfo(column).OfType<CCEPDataInfo>().First((dataInfo) => dataInfo.Patient == patient);
            }
            else if (column is StaticColumn staticColumn)
            {
                return GetDataInfo(column).OfType<StaticDataInfo>().First((dataInfo) => dataInfo.Patient == patient);
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// Get the dataInfo of all columns for a specific patient
        /// </summary>
        /// <param name="patient"></param>
        /// <returns></returns>
        public IEnumerable<DataInfo> GetDataInfo(Patient patient)
        {
            return IEEGColumns.Select(c => GetDataInfo(patient, c)).Distinct();
        }
        public override void GenerateID()
        {
            base.GenerateID();
            foreach (var column in Columns) column.GenerateID();
        }
        public override List<BaseData> GetAllIdentifiable()
        {
            List<BaseData> IDs = base.GetAllIdentifiable();
            foreach (var column in Columns) IDs.AddRange(column.GetAllIdentifiable());
            return IDs;
        }
        public void Unload()
        {
            foreach (var column in Columns)
            {
                column.Unload();
            }
        }
        #endregion

        #region Operators
        /// <summary>
        /// Clone this instance.
        /// </summary>
        /// <returns>Clone of this instance.</returns>
        public override object Clone()
        {
            return new Visualization(Name, Patients.ToList(), Columns.DeepClone(), Configuration.Clone() as VisualizationConfiguration, ID);
        }
        /// <summary>
        /// Copy an instance in this instance.
        /// </summary>
        /// <param name="copy">Instance to copy.</param>
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if(copy is Visualization visualization)
            {
                Name = visualization.Name;
                Patients = visualization.Patients;
                Columns = visualization.Columns;
                ID = visualization.ID;
                Configuration = visualization.Configuration;
            }
        }
        #endregion

        #region Private Methods
        private async UniTask<Dictionary<Column, IEnumerable<DataInfo>>> FindDataInfoToReadAsync(Action<float, float, LoadingText> onChangeProgress, CancellationToken token)
        {
            await UniTask.SwitchToThreadPool();
            Dictionary<Column, IEnumerable<DataInfo>> dataInfoByColumn = new Dictionary<Column, IEnumerable<DataInfo>>();
            int count = 0;
            int length = Columns.Count;
            foreach (var column in Columns)
            {
                token.ThrowIfCancellationRequested();
                onChangeProgress((float)count / length, 0.0f, new LoadingText("Finding dataInfo for ", column.Name, " [" + (count + 1) + "/" + length + "]"));
                if (column is IEEGColumn iEEGColumn)
                {
                    IEnumerable<IEEGDataInfo> dataInfoForThisColumn = GetDataInfo(iEEGColumn).OfType<IEEGDataInfo>();
                    if (dataInfoForThisColumn.Select(d => d.Patient).Distinct().Count() != Patients.Count)
                    {
                        foreach (Patient patient in Patients)
                        {
                            if (!dataInfoForThisColumn.Any((dataInfo) => dataInfo.Patient == patient))
                            {
                                throw new CannotFindDataInfoException(patient.ID, iEEGColumn.DataName);
                            }
                        }
                    }
                    dataInfoByColumn.Add(column, dataInfoForThisColumn);
                }
                else if (column is CCEPColumn ccepColumn)
                {
                    IEnumerable<CCEPDataInfo> dataInfoForThisColumn = GetDataInfo(ccepColumn).OfType<CCEPDataInfo>();
                    if (dataInfoForThisColumn.Select(d => d.Patient).Distinct().Count() != Patients.Count)
                    {
                        foreach (Patient patient in Patients)
                        {
                            if (!dataInfoForThisColumn.Any((dataInfo) => dataInfo.Patient == patient))
                            {
                                throw new CannotFindDataInfoException(patient.ID, ccepColumn.DataName);
                            }
                        }
                    }
                    dataInfoByColumn.Add(column, dataInfoForThisColumn);
                }
                count++;
            }
            return dataInfoByColumn;
        }
        private async UniTask LoadDataAsync(Dictionary<Column, IEnumerable<DataInfo>> dataInfoByColumn, Action<float, float, LoadingText> updateProgress, CancellationToken token)
        {
            const float LOADING_DATA_PROGRESS = 0.95f;
            const float NORMALIZING_DATA_PROGRESS = 0.05f;
            IEnumerable<DataInfo> dataInfoCollection = dataInfoByColumn.SelectMany(d => d.Value).Distinct();
            var tasks = dataInfoCollection.Select(dataInfo => (Func<UniTask>)(async () =>
            {
                await UniTask.SwitchToThreadPool();
                try
                {
                    // PROBABLY FIXME
                    Data data = DataManager.GetData(dataInfo);
                    if (data is EpochedData epochedData)
                    {
                        foreach (var column in dataInfoByColumn.Keys)
                        {
                            if (column is CCEPColumn ccepColumn)
                            {
                                if (epochedData.DataByBloc.TryGetValue(ccepColumn.Bloc, out BlocData blocData) && !blocData.IsValid)
                                {
                                    throw new Exception("No bloc " + ccepColumn.Bloc.Name + " could be epoched.");
                                }
                            }
                        }
                    }
                }
                catch (CannotEpochAllTrialsException e)
                {
                    UnityEngine.Debug.LogException(e);
                    throw new CannotLoadDataInfoException(string.Format("{0} ({1})", dataInfo.Name, dataInfo.Protocol.Name), (dataInfo is PatientDataInfo pDataInfo ? pDataInfo.Patient.Name : "Unkwown patient"), e.Message);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                    throw new CannotLoadDataInfoException(string.Format("{0} ({1})", dataInfo.Name, dataInfo.Protocol.Name), (dataInfo is PatientDataInfo pDataInfo ? pDataInfo.Patient.Name : "Unkwown patient"), e.Message);
                }
            }));
            await Tools.UniTaskExtensions.PerformMultipleTasksAsync(tasks, 0, LOADING_DATA_PROGRESS, "Loading data", updateProgress, 5, PersistentDataManager.UserPreferences.General.System.MultiThreading, token);
            updateProgress.Invoke(LOADING_DATA_PROGRESS + NORMALIZING_DATA_PROGRESS, 1.0f, new LoadingText("Normalizing data"));
            DataManager.NormalizeiEEGData();
        }
        private async UniTask LoadColumnsAsync(Dictionary<Column, IEnumerable<DataInfo>> dataInfoByColumn, Action<float, float, LoadingText> onChangeProgress, CancellationToken token)
        {
            await UniTask.SwitchToThreadPool();

            ReadOnlyCollection<IEEGColumn> iEEGColumns = IEEGColumns;
            int nbIEEGColumns = iEEGColumns.Count;
            ReadOnlyCollection<CCEPColumn> ccepColumns = CCEPColumns;
            int nbCCEPColumns = ccepColumns.Count;
            int nbDynamicColumns = nbIEEGColumns + nbCCEPColumns;

            float progress = 0;
            const float LOADING_DATA_PROGRESS = 0.95f;
            const float LOADING_TIMELINE_PROGRESS = 0.05f;
            const float TIME_BY_DATAINFO = 0.15f;
            float loadingDataStep = LOADING_DATA_PROGRESS / nbDynamicColumns;
            float loadingTimelineStep = LOADING_TIMELINE_PROGRESS / nbDynamicColumns;

            // iEEG Columns
            if (nbIEEGColumns > 0)
            {
                for (int i = 0; i < nbIEEGColumns; ++i)
                {
                    token.ThrowIfCancellationRequested();
                    IEEGColumn iEEGColumn = iEEGColumns[i];
                    progress += loadingDataStep;
                    onChangeProgress(progress, TIME_BY_DATAINFO * dataInfoByColumn[iEEGColumn].Count() , new LoadingText("Loading iEEG column ", iEEGColumn.Name, " [" + (i + 1) + "/" + nbIEEGColumns + "]"));
                    iEEGColumn.Data.Load(dataInfoByColumn[iEEGColumn].OfType<IEEGDataInfo>(), iEEGColumn.Bloc);
                }
                Frequency maxiEEGFrequency = new Frequency(iEEGColumns.Max(column => column.Data.MaxFrequency));
                for (int i = 0; i < nbIEEGColumns; ++i)
                {
                    token.ThrowIfCancellationRequested();
                    IEEGColumn column = iEEGColumns[i];
                    progress += loadingTimelineStep;
                    onChangeProgress(progress, 0, new LoadingText("Loading timeline of iEEG column ", column.Name, " [" + (i + 1) + "/" + nbIEEGColumns + "]"));
                    column.Data.SetTimeline(maxiEEGFrequency, column.Bloc, iEEGColumns.Select(c => c.Bloc).Distinct());
                    await UniTask.SwitchToMainThread();
                    column.Data.IconicScenario.LoadIcons();
                    await UniTask.SwitchToThreadPool();
                }
            }

            // CCEP Columns
            if (nbCCEPColumns > 0)
            {
                for (int i = 0; i < nbCCEPColumns; ++i)
                {
                    token.ThrowIfCancellationRequested();
                    CCEPColumn ccepColumn = ccepColumns[i];
                    progress += loadingDataStep;
                    onChangeProgress(progress, TIME_BY_DATAINFO * dataInfoByColumn[ccepColumn].Count(), new LoadingText("Loading CCEP column ", ccepColumn.Name, " [" + (i + 1) + "/" + nbCCEPColumns + "]"));
                    ccepColumn.Data.Load(dataInfoByColumn[ccepColumn].OfType<CCEPDataInfo>(), ccepColumn.Bloc);
                }
                Frequency maxCCEPFrequency = new Frequency(ccepColumns.Max(column => column.Data.Frequencies.Max(f => f.RawValue)));
                for (int i = 0; i < nbCCEPColumns; ++i)
                {
                    token.ThrowIfCancellationRequested();
                    CCEPColumn column = ccepColumns[i];
                    progress += loadingTimelineStep;
                    onChangeProgress.Invoke(progress, 0, new LoadingText("Loading timeline of CCEP column ", column.Name, " [" + (i + 1) + "/" + nbCCEPColumns + "]"));
                    column.Data.SetTimeline(maxCCEPFrequency, column.Bloc, ccepColumns.Select(c => c.Bloc).Distinct());
                    await UniTask.SwitchToMainThread();
                    column.Data.IconicScenario.LoadIcons();
                    await UniTask.SwitchToThreadPool();
                }
            }

            await UniTask.SwitchToMainThread();
        }
        private async UniTask LoadFMRIColumnsAsync(Action<float, float, LoadingText> onChangeProgress, CancellationToken token)
        {
            await UniTask.SwitchToThreadPool();
            ReadOnlyCollection<FMRIColumn> fmriColumns = FMRIColumns;
            int nbFMRIColumns = fmriColumns.Count;

            float progress = 0;
            const float TIME_BY_DATAINFO = 1f;
            float loadingDataStep = 1f / nbFMRIColumns;

            if (nbFMRIColumns > 0)
            {
                for (int i = 0; i < nbFMRIColumns; ++i)
                {
                    token.ThrowIfCancellationRequested();
                    FMRIColumn fmriColumn = fmriColumns[i];
                    FMRIDataInfo[] dataInfos = fmriColumn.Dataset.GetFMRIDataInfos().Where(data => Patients.Contains(data.Patient)).ToArray();
                    SharedFMRIDataInfo[] sharedFMRIDataInfos = fmriColumn.Dataset.GetSharedFMRIDataInfos();
                    progress += loadingDataStep;
                    onChangeProgress(progress, TIME_BY_DATAINFO * (dataInfos.Length + sharedFMRIDataInfos.Length), new LoadingText("Loading FMRI column ", fmriColumn.Name, " [" + (i + 1) + "/" + nbFMRIColumns + "]"));
                    fmriColumn.Data.Load(dataInfos, sharedFMRIDataInfos);
                }
            }

        }
        private async UniTask LoadMEGColumnsAsync(Action<float, float, LoadingText> onChangeProgress, CancellationToken token)
        {
            await UniTask.SwitchToThreadPool();
            ReadOnlyCollection<MEGColumn> megColumns = MEGColumns;
            int nbMegColumns = megColumns.Count;

            float progress = 0;
            const float TIME_BY_DATAINFO = 1f;
            float loadingDataStep = 1f / nbMegColumns;

            if (nbMegColumns > 0)
            {
                for (int i = 0; i < nbMegColumns; ++i)
                {
                    token.ThrowIfCancellationRequested();
                    MEGColumn megColumn = megColumns[i];
                    PatientDataInfo[] dataInfos = megColumn.Dataset.GetMEGDataInfos().Where(data => Patients.Contains(data.Patient)).ToArray();
                    progress += loadingDataStep;
                    onChangeProgress(progress, TIME_BY_DATAINFO * dataInfos.Length, new LoadingText("Loading MEG column ", megColumn.Name, " [" + (i + 1) + "/" + nbMegColumns + "]"));
                    megColumn.Data.Load(dataInfos);
                }
            }
        }
        private async UniTask LoadStaticColumnsAsync(Action<float, float, LoadingText> onChangeProgress, CancellationToken token)
        {
            await UniTask.SwitchToThreadPool();
            ReadOnlyCollection<StaticColumn> staticColumns = StaticColumns;
            int nbStaticColumns = staticColumns.Count;

            float progress = 0;
            const float TIME_BY_DATAINFO = 1f;
            float loadingDataStep = 1f / nbStaticColumns;

            if (nbStaticColumns > 0)
            {
                for (int i = 0; i < nbStaticColumns; ++i)
                {
                    token.ThrowIfCancellationRequested();
                    StaticColumn staticColumn = staticColumns[i];
                    StaticDataInfo[] dataInfos = staticColumn.Dataset.GetStaticDataInfos().Where(data => Patients.Contains(data.Patient) && staticColumn.DataName == data.Name).ToArray();
                    progress += loadingDataStep;
                    onChangeProgress(progress, TIME_BY_DATAINFO * dataInfos.Length, new LoadingText("Loading Static column ", staticColumn.Name, " [" + (i + 1) + "/" + nbStaticColumns + "]"));
                    staticColumn.Data.Load(dataInfos);
                }
            }
        }
        #endregion

        #region Interfaces
        bool ILoadable<Visualization>.LoadFromFile(string path, out Visualization[] result)
        {
            bool success = LoadFromFile(path, out Visualization visualization);
            result = new Visualization[] { visualization };
            return success;
        }
        string[] ILoadable<Visualization>.GetExtensions()
        {
            return GetExtensions();
        }
        #endregion

        #region Serialization
        protected override void OnSerializing()
        {
            base.OnSerializing();
            m_PatientsID = Patients.Select(p => p.ID).ToList();
        }
        protected override void OnDeserialized()
        {
            base.OnDeserialized();
            Patients = m_PatientsID.Select(id => ApplicationState.LoadedProject.Patients.FirstOrDefault(p => p.ID == id)).ToList();
            Patients.RemoveAll(p => p == null);
        }
        #endregion
    }
}