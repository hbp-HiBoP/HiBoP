using UnityEngine.Events;
using System;
using System.Linq;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Runtime.Serialization;
using CielaSpike;
using HBP.Data.Experience.Dataset;
using Tools.Unity;
using System.IO;
using Tools.CSharp;

namespace HBP.Data.Visualization
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
    [DataContract]
    public class Visualization :  BaseData, ILoadable<Visualization>, INameable
    {
        #region Properties
        public const string EXTENSION = ".visualization";
        /// <summary>
        /// Name of the visualization.
        /// </summary>
        [DataMember(Order = 2)] public string Name { get; set; }

        [DataMember(Name = "Patients", Order = 3)] List<string> m_PatientsID;
        /// <summary>
        /// Patients of the Visualization.
        /// </summary>
        public ReadOnlyCollection<Patient> Patients
        {
            get
            {
                IEnumerable<Patient> patients = from patient in ApplicationState.ProjectLoaded.Patients where m_PatientsID.Contains(patient.ID) select patient;
                return new ReadOnlyCollection<Patient>(patients.ToArray());
            }
        }

        /// <summary>
        /// Configuration of the visualization.
        /// </summary>
        [DataMember(Order = 4)] public VisualizationConfiguration Configuration { get; set; }

        /// <summary>
        /// Columns of the visualization.
        /// </summary>
        [DataMember(Order = 5)] public List<Column> Columns { get; set; }
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

        /// <summary>
        /// Test if the visualization is visualizable.
        /// </summary>
        public virtual bool IsVisualizable
        {
            get { return Columns.Count > 0 && Patients.Count > 0 && Columns.All((column) => column.IsCompatible(Patients)) && FindUsableImplantations().Count > 0; }
        }
        /// <summary>
        /// Is the visualization opened in a scene ?
        /// </summary>
        public bool IsOpen { get { return ApplicationState.Module3D.Visualizations.Contains(this); } }
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
            Columns = columns.ToList();
            SetPatients(patients);
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
            Columns = columns.ToList();
            SetPatients(patients);
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

        #region Getter/Setter
        /// <summary>
        /// Add a patient to the visualization.
        /// </summary>
        /// <param name="patient">Patient to add.</param>
        public void AddPatient(Patient patient)
        {
            if (!m_PatientsID.Contains(patient.ID)) m_PatientsID.Add(patient.ID);
        }
        /// <summary>
        /// Add patients to the visualization.
        /// </summary>
        /// <param name="patients">Patients to add.</param>
        public void AddPatient(IEnumerable<Patient> patients)
        {
            foreach (Patient patient in patients)
            {
                AddPatient(patient);
            }
        }
        /// <summary>
        /// Remove a patient to the visualization.
        /// </summary>
        /// <param name="patient">Patient to remove.</param>
        public void RemovePatient(Patient patient)
        {
            m_PatientsID.Remove(patient.ID);
        }
        /// <summary>
        /// Remove patients to the visualization.
        /// </summary>
        /// <param name="patients">Patients to remove.</param>
        public void RemovePatient(IEnumerable<Patient> patients)
        {
            foreach (Patient patient in patients) RemovePatient(patient);
        }
        /// <summary>
        /// Set patients in the visualization.
        /// </summary>
        /// <param name="patients">Patients to set in the visualization.</param>
        public void SetPatients(IEnumerable<Patient> patients)
        {
            m_PatientsID = new List<string>();
            AddPatient(from patient in patients where !m_PatientsID.Contains(patient.ID) select patient);
        }
        /// <summary>
        /// Remove all patients in the visualization.
        /// </summary>
        public void RemoveAllPatients()
        {
            RemovePatient(Patients);
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
        public static string GetExtension()
        {
            return EXTENSION[0] == '.' ? EXTENSION.Substring(1) : EXTENSION;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Load the visualization.
        /// </summary>
        /// <returns></returns>
        public IEnumerator c_Load(Action<float,float, LoadingText> onChangeProgress)
        {
            yield return Ninja.JumpBack;

            int nbDynamicColumns = CCEPColumns.Count + IEEGColumns.Count;

            onChangeProgress(0, 0, new LoadingText("Loading Visualization"));

            if (nbDynamicColumns > 0) // FIXME : this security should not exist
            {
                Exception exception = null;
                int nbPatients = m_PatientsID.Count;

                float steps = 1 + 2 * nbPatients * nbDynamicColumns;
                float progress = 0.0f;

                float findDataInfoToReadProgress = 1 / steps;
                float LoadDataProgress = nbPatients * nbDynamicColumns / steps;
                float LoadColumnsProgress = nbPatients * nbDynamicColumns / steps;

                // Find dataInfo.
                Dictionary<Column, IEnumerable<DataInfo>> dataInfoByColumn = new Dictionary<Column, IEnumerable<DataInfo>>();
                yield return Ninja.JumpToUnity;
                yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_FindDataInfoToRead((localProgress, duration, text) => onChangeProgress(progress + localProgress * findDataInfoToReadProgress, duration, text), (value, e) => { dataInfoByColumn = value; exception = e; }));
                progress += findDataInfoToReadProgress;

                // Load Data.
                if (exception == null)
                {
                    yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_LoadData(dataInfoByColumn, (localProgress, duration, text) => onChangeProgress(progress + localProgress * LoadDataProgress, duration, text), (e) => { exception = e; }));
                    progress += LoadDataProgress;
                }
                // Load Columns.
                if (exception == null)
                {
                    yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_LoadColumns(dataInfoByColumn, (localProgress, duration, text) => onChangeProgress(progress + localProgress * LoadColumnsProgress, duration, text), (e) => exception = e));
                    progress += LoadColumnsProgress;
                }

                yield return Ninja.JumpBack;
                if (exception != null)
                {
                    throw exception;
                }
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
                return GetDataInfo(column).OfType<iEEGDataInfo>().First((dataInfo) => dataInfo.Patient == patient);
            }
            else if (column is CCEPColumn ccepColumn)
            {
                return GetDataInfo(column).OfType<CCEPDataInfo>().First((dataInfo) => dataInfo.Patient == patient);
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
        /// <summary>
        /// Find implantations that are usable in all patients
        /// </summary>
        /// <param name="patients"></param>
        /// <returns></returns>
        public List<string> FindUsableImplantations()
        {
            return new List<string>();
            //return Patients.First().Sites.Where((i) => Patients.All(p => p.Sites.Any((ii) => ii.Name == i.Name && ii.WasUsable))).Select((i) => i.Name).ToList();
            // On a gardé le code pour tester si manque de performance.
            //List<string> commonImplantations = new List<string>();
            //foreach (Anatomy.Implantation implantation in Patients[0].Implantations)
            //{
            //    string implantationName = implantation.Name;
            //    bool isImplantationCommonToAllPatients = true;
            //    foreach (Patient patient in Patients)
            //    {
            //        if (patient.Implantations.FindIndex((i) => i.Name == implantationName && i.WasUsable) == -1)
            //        {
            //            isImplantationCommonToAllPatients = false;
            //            break;
            //        }
            //    }
            //    if (isImplantationCommonToAllPatients)
            //    {
            //        commonImplantations.Add(implantation.Name);
            //    }
            //}
            //return commonImplantations;
        }
        public override void GenerateID()
        {
            base.GenerateID();
            foreach (var column in Columns) column.GenerateID();
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
            return new Visualization(Name, Patients, Columns.DeepClone(), Configuration.Clone() as VisualizationConfiguration, ID);
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
                Columns = visualization.Columns;
                ID = visualization.ID;
                Configuration = visualization.Configuration;
                SetPatients(visualization.Patients);
            }
        }
        #endregion

        #region Private Methods
        IEnumerator c_FindDataInfoToRead(Action<float, float, LoadingText> onChangeProgress, Action<Dictionary<Column, IEnumerable<DataInfo>>, Exception> outPut)
        {
            yield return Ninja.JumpBack;
            Exception exception = null;

            // Find files to read.
            Dictionary<Column, IEnumerable<DataInfo>> dataInfoByColumn = new Dictionary<Column, IEnumerable<DataInfo>>();
            int count = 0;
            int length = Columns.Count;
            foreach (var column in Columns)
            {
                onChangeProgress((float) count / length, 0.0f, new LoadingText("Finding dataInfo for ", column.Name, " [" + (count + 1) + "/" + length + "]"));
                try
                {
                    if (column is IEEGColumn iEEGColumn)
                    {
                        IEnumerable<iEEGDataInfo> dataInfoForThisColumn = GetDataInfo(iEEGColumn).OfType<iEEGDataInfo>();
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
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                    exception = e;
                }
                count++;
            }
            outPut(dataInfoByColumn, exception);
        }
        IEnumerator c_LoadData(Dictionary<Column, IEnumerable<DataInfo>> dataInfoByColumn, Action<float, float, LoadingText> onChangeProgress, Action<Exception> outPut)
        {
            yield return Ninja.JumpBack;

            Exception exception = null;
            string additionalInformation = "";

            IEnumerable<DataInfo> dataInfoCollection = dataInfoByColumn.SelectMany(d => d.Value).Distinct();
            int count = 0;
            int length = dataInfoCollection.Count();
            const float LOADING_DATA_PROGRESS = 0.95f;
            const float NORMALIZING_DATA_PROGRESS = 0.05f;
            foreach (var dataInfo in dataInfoCollection)
            {
                onChangeProgress(((float)count / length) * LOADING_DATA_PROGRESS, 1.0f, new LoadingText("Loading ", string.Format("{0} ({1})", dataInfo.Name, dataInfo.Dataset.Name) + (dataInfo is PatientDataInfo patientDataInfo ? " for " + patientDataInfo.Patient.Name : ""), " [" + (count + 1) + "/" + length + "]"));

                try
                {
                    // PROBABLY FIXME
                    Experience.Dataset.Data data = DataManager.GetData(dataInfo);
                    if (data is EpochedData epochedData)
                    {
                        foreach (var column in dataInfoByColumn.Keys)
                        {
                            if (column is IEEGColumn iEEGColumn)
                            {
                                if(epochedData.DataByBloc.TryGetValue(iEEGColumn.Bloc, out BlocData blocData) && !blocData.IsValid)
                                {
                                    additionalInformation = "No bloc " + iEEGColumn.Bloc.Name + " could be epoched.";
                                    throw new Exception();
                                }
                            }
                            else if (column is CCEPColumn ccepColumn)
                            {
                                if (epochedData.DataByBloc.TryGetValue(ccepColumn.Bloc, out BlocData blocData) && !blocData.IsValid)
                                {
                                    additionalInformation = "No bloc " + ccepColumn.Bloc.Name + " could be epoched.";
                                    throw new Exception();
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                    exception = new CannotLoadDataInfoException(string.Format("{0} ({1})", dataInfo.Name, dataInfo.Dataset.Name), (dataInfo is PatientDataInfo pDataInfo ? " for " + pDataInfo.Patient.ID : "Unkwown patient") , additionalInformation);
                    break;
                }
                count++;
            }
            onChangeProgress.Invoke(LOADING_DATA_PROGRESS + NORMALIZING_DATA_PROGRESS, 1.0f, new LoadingText("Normalizing data"));
            if (exception == null)
            {
                DataManager.NormalizeiEEGData();
            }
            outPut(exception);
        }
        IEnumerator c_LoadColumns(Dictionary<Column, IEnumerable<DataInfo>> dataInfoByColumn, Action<float, float, LoadingText> onChangeProgress, Action<Exception> outPut)
        {
            yield return Ninja.JumpBack;

            Exception exception = null;

            ReadOnlyCollection<IEEGColumn> iEEGColumns = IEEGColumns;
            int iEEGColumnsLength = iEEGColumns.Count;
            ReadOnlyCollection<CCEPColumn> ccepColumns = CCEPColumns;
            int ccepColumnsLength = ccepColumns.Count;
            int length = 2 * (iEEGColumnsLength + ccepColumnsLength);

            float progress = 0;
            const float LOADING_DATA_PROGRESS = 0.95f;
            const float LOADING_TIMELINE_PROGRESS = 0.05f;
            float ieegloadingDataProgressStep = LOADING_DATA_PROGRESS * iEEGColumnsLength;
            float ieegloadingTimeLineProgressStep = LOADING_TIMELINE_PROGRESS * iEEGColumnsLength;
            float cceploadingDataProgressStep = LOADING_DATA_PROGRESS * ccepColumnsLength;
            float cceploadingTimeLineProgressStep = LOADING_TIMELINE_PROGRESS * ccepColumnsLength;
            const float TIME_BY_DATAINFO = 0.15f;
            // iEEG Columns
            if (iEEGColumnsLength > 0)
            {
                for (int i = 0; i < iEEGColumnsLength; ++i)
                {
                    IEEGColumn iEEGColumn = iEEGColumns[i];
                    onChangeProgress(progress + ieegloadingDataProgressStep, TIME_BY_DATAINFO * dataInfoByColumn[iEEGColumn].Count() , new LoadingText("Loading iEEG column ", iEEGColumn.Name, " [" + (i + 1) + "/" + iEEGColumnsLength + "]"));
                    try
                    {
                        iEEGColumn.Data.Load(dataInfoByColumn[iEEGColumn].OfType<iEEGDataInfo>(), iEEGColumn.Bloc);
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogException(e);
                        exception = e;
                        outPut(exception);
                        yield break;
                    }
                    progress += ieegloadingDataProgressStep;
                }
                Tools.CSharp.EEG.Frequency maxiEEGFrequency = new Tools.CSharp.EEG.Frequency(iEEGColumns.Max(column => column.Data.Frequencies.Max(f => f.RawValue)));
                for (int i = 0; i < iEEGColumnsLength; ++i)
                {
                    IEEGColumn column = iEEGColumns[i];
                    onChangeProgress(progress, 0, new LoadingText("Loading timeline of iEEG column ", column.Name, " [" + (i + 1) + "/" + iEEGColumnsLength + "]"));
                    column.Data.SetTimeline(maxiEEGFrequency, column.Bloc, iEEGColumns.Select(c => c.Bloc).Distinct());
                    yield return Ninja.JumpToUnity;
                    try
                    {
                        column.Data.IconicScenario.LoadIcons();
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogException(e);
                        exception = e;
                        outPut(exception);
                        yield break;
                    }
                    yield return Ninja.JumpBack;
                    progress += ieegloadingTimeLineProgressStep;
                }
            }

            // CCEP Columns
            if (ccepColumnsLength > 0)
            {
                for (int i = 0; i < ccepColumnsLength; ++i)
                {
                    CCEPColumn ccepColumn = ccepColumns[i];
                    onChangeProgress(progress + cceploadingDataProgressStep, TIME_BY_DATAINFO * dataInfoByColumn[ccepColumn].Count(), new LoadingText("Loading CCEP column ", ccepColumn.Name, " [" + (i + 1) + "/" + ccepColumnsLength + "]"));
                    try
                    {
                        ccepColumn.Data.Load(dataInfoByColumn[ccepColumn].OfType<CCEPDataInfo>(), ccepColumn.Bloc);
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogException(e);
                        exception = e;
                        outPut(exception);
                        yield break;
                    }
                    progress += cceploadingDataProgressStep;
                }
                Tools.CSharp.EEG.Frequency maxCCEPFrequency = new Tools.CSharp.EEG.Frequency(ccepColumns.Max(column => column.Data.Frequencies.Max(f => f.RawValue)));
                for (int i = 0; i < ccepColumnsLength; ++i)
                {
                    CCEPColumn column = ccepColumns[i];
                    onChangeProgress.Invoke(progress, 0, new LoadingText("Loading timeline of CCEP column ", column.Name, " [" + (i + 1) + "/" + ccepColumnsLength + "]"));
                    column.Data.SetTimeline(maxCCEPFrequency, column.Bloc, ccepColumns.Select(c => c.Bloc).Distinct());
                    yield return Ninja.JumpToUnity;
                    try
                    {
                        column.Data.IconicScenario.LoadIcons();
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogException(e);
                        exception = e;
                        outPut(exception);
                        yield break;
                    }
                    yield return Ninja.JumpBack;
                    progress += cceploadingTimeLineProgressStep;
                }
            }
            outPut(exception);
        }
        #endregion

        #region Interfaces
        bool ILoadable<Visualization>.LoadFromFile(string path, out Visualization result)
        {
            return LoadFromFile(path, out result);
        }
        string ILoadable<Visualization>.GetExtension()
        {
            return GetExtension();
        }
        #endregion
    }
}