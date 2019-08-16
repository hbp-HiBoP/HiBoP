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
    * \brief 3D brain vizualisation.
    * 
    * \details Define a 3D brain visualization and contains:
    * 
    *   - \a ID.
    *   - \a Name.
    *   - \a Configuration.
    *   - \a Columns.   
    */
    [DataContract]
    public class Visualization :  ICloneable , ICopiable, ILoadable, IIdentifiable
    {
        #region Properties
        public const string EXTENSION = ".visualization";

        /// <summary>
        /// Unique ID.
        /// </summary>
        [DataMember(Order = 1)] public string ID { get; set; }

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

        const float FIND_FILES_TO_READ_PROGRESS = 0.025f;
        const float LOAD_DATA_PROGRESS = 0.8f;
        const float LOAD_COLUMNS_PROGRESS = 0.175f;
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new visualization instance.
        /// </summary>
        /// <param name="name">Name of the visualization.</param>
        /// <param name="columns">Columns of the visualization.</param>
        /// <param name="id">Unique ID.</param>
        public Visualization(string name, IEnumerable<Patient> patients, IEnumerable<Column> columns, VisualizationConfiguration configuration, string id)
        {
            Name = name;
            Columns = columns.ToList();
            SetPatients(patients);
            Configuration = configuration;
            ID = id;
        }
        /// <summary>
        /// Create a new visualization instance.
        /// </summary>
        /// <param name="name">Name of the visualization.</param>
        /// <param name="columns">Columns of the visualization.</param>
        /// <param name="id">Unique ID.</param>
        public Visualization(string name, IEnumerable<Patient> patients, IEnumerable<Column> columns, VisualizationConfiguration configuration) : this(name, patients, columns, configuration, Guid.NewGuid().ToString())
        {
        }
        /// <summary>
        /// Create a new visualization instance.
        /// </summary>
        /// <param name="name">Name of the visualization.</param>
        /// <param name="columns">Columns of the visualization.</param>
        /// <param name="id">Unique ID.</param>
        public Visualization(string name, IEnumerable<Patient> patients, IEnumerable<Column> columns, string id) : this(name, patients, columns, new VisualizationConfiguration(), id)
        {
        }
        /// <summary>
        /// Create a new visualization instance.
        /// </summary>
        /// <param name="name">Name of the visualization.</param>
        /// <param name="columns">Columns of the visualization.</param>
        public Visualization(string name, IEnumerable<Patient> patients, IEnumerable<Column> columns) : this(name, patients, columns, Guid.NewGuid().ToString())
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

        #region Public Methods
        public void Load(string path)
        {
            Visualization result;
            try
            {
                result = ClassLoaderSaver.LoadFromJson<Visualization>(path);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
                throw new CanNotReadVisualizationFileException(Path.GetFileNameWithoutExtension(path));
            }
            Copy(result);
        }
        public string GetExtension()
        {
            return EXTENSION[0] == '.' ? EXTENSION.Substring(1) : EXTENSION;
        }

        /// <summary>
        /// Load the visualization.
        /// </summary>
        /// <returns></returns>
        public IEnumerator c_Load(GenericEvent<float,float, LoadingText> onChangeProgress = null)
        {
            if (onChangeProgress == null) onChangeProgress = new GenericEvent<float, float, LoadingText>();

            Exception exception = null;
            float progress = 0.0f;
            yield return Ninja.JumpToUnity;
            if (IEEGColumns.Count > 0 || CCEPColumns.Count > 0) // FIXME : this security should not exist
            {
                yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_LoadColumnsData(progress, onChangeProgress, e => { exception = e; }));
            }
            yield return Ninja.JumpBack;
            if (exception != null)
            {
                throw exception;
            }
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
            return Patients.First().Implantations.Where((i) => Patients.All(p => p.Implantations.Any((ii) => ii.Name == i.Name && ii.WasUsable))).Select((i) => i.Name).ToList();
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

        public void Unload()
        {
            foreach (var column in Columns)
            {
                column.Unload();
            }
        }
        #endregion

        #region Operators
        public void GenerateNewIDs()
        {
            ID = Guid.NewGuid().ToString();
        }
        /// <summary>
        /// Clone this instance.
        /// </summary>
        /// <returns>Clone of this instance.</returns>
        public object Clone()
        {
            return new Visualization(Name, Patients, Columns.DeepClone(), Configuration.Clone() as VisualizationConfiguration, ID);
        }
        /// <summary>
        /// Copy an instance in this instance.
        /// </summary>
        /// <param name="copy">Instance to copy.</param>
        public void Copy(object copy)
        {
            Visualization visualization = copy as Visualization;
            Name = visualization.Name;
            Columns = visualization.Columns;
            ID = visualization.ID;
            Configuration = visualization.Configuration;
            SetPatients(visualization.Patients);
        }
        #endregion

        #region Private Methods
        IEnumerator c_LoadColumnsData(float progress, GenericEvent<float, float, LoadingText> onChangeProgress, Action<Exception> outPut)
        {
            Exception exception = null;

            // Find dataInfo.
            Dictionary<Column, IEnumerable<DataInfo>> dataInfoByColumn = new Dictionary<Column, IEnumerable<DataInfo>>();
            yield return Ninja.JumpToUnity;
            yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_FindDataInfoToRead(progress, onChangeProgress, (value, progressValue, e) => { dataInfoByColumn = value; progress = progressValue; exception = e; }));
            yield return Ninja.JumpBack;

            // Load Data.
            if (exception == null)
            {
                yield return Ninja.JumpToUnity;
                yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_LoadData(dataInfoByColumn, progress, onChangeProgress, (value, e) => { progress = value; exception = e; }));
                yield return Ninja.JumpBack;
            }
            // Load Columns.
            if (exception == null)
            {
                yield return Ninja.JumpToUnity;
                yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_LoadColumns(dataInfoByColumn, progress, onChangeProgress, (value, e) => { progress = value; exception = e; }));
                yield return Ninja.JumpBack;
            }

            if (exception != null)
            {
                outPut(exception);
            }
        }
        IEnumerator c_FindDataInfoToRead(float progress, GenericEvent<float, float, LoadingText> onChangeProgress, Action<Dictionary<Column, IEnumerable<DataInfo>>, float, Exception> outPut)
        {
            Exception exception = null;
            // Find files to read.
            Dictionary<Column, IEnumerable<DataInfo>> dataInfoByColumn = new Dictionary<Column, IEnumerable<DataInfo>>();
            float progressStep = FIND_FILES_TO_READ_PROGRESS / Columns.Count;
            foreach (var column in Columns)
            {
                // Update progress;
                yield return Ninja.JumpToUnity;
                progress += progressStep;
                onChangeProgress.Invoke(progress, 0.0f, new LoadingText("Finding files to read."));
                yield return Ninja.JumpBack;

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
            }
            outPut(dataInfoByColumn, progress, exception);
        }
        IEnumerator c_LoadData(Dictionary<Column, IEnumerable<DataInfo>> dataInfoByColumn, float progress, GenericEvent<float, float, LoadingText> onChangeProgress, Action<float, Exception> outPut)
        {
            Exception exception = null;
            string additionalInformation = "";
            yield return Ninja.JumpBack;
            IEnumerable<DataInfo> dataInfoCollection = dataInfoByColumn.SelectMany(d => d.Value).Distinct();
            int i = 0;
            int dataInfoCollectionLength = dataInfoCollection.Count();
            float progressStep = LOAD_DATA_PROGRESS / (dataInfoCollectionLength + 1);
            foreach (var dataInfo in dataInfoCollection)
            {
                yield return Ninja.JumpToUnity;
                progress += progressStep;
                onChangeProgress.Invoke(progress, 1.0f, new LoadingText("Loading ", string.Format("{0} ({1})", dataInfo.Name, dataInfo.Dataset.Name) + (dataInfo is PatientDataInfo patientDataInfo ? " for " + patientDataInfo.Patient.Name : ""), " [" + (i + 1).ToString() + "/" + dataInfoCollectionLength + "]"));
                yield return Ninja.JumpBack;
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
                                if (epochedData.DataByBloc.ContainsKey(iEEGColumn.Bloc) && !epochedData.DataByBloc[iEEGColumn.Bloc].IsValid)
                                {
                                    additionalInformation = "No bloc " + iEEGColumn.Bloc.Name + " could be epoched.";
                                    throw new Exception();
                                }
                            }
                            else if (column is CCEPColumn ccepColumn)
                            {
                                if (epochedData.DataByBloc.ContainsKey(ccepColumn.Bloc) && !epochedData.DataByBloc[ccepColumn.Bloc].IsValid)
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
                i++;
            }
            yield return Ninja.JumpToUnity;
            progress += progressStep;
            onChangeProgress.Invoke(progress, 1.0f, new LoadingText("Normalizing data"));
            yield return Ninja.JumpBack;
            if (exception == null)
            {
                DataManager.NormalizeiEEGData();
            }
            outPut(progress, exception);
        }
        IEnumerator c_LoadColumns(Dictionary<Column, IEnumerable<DataInfo>> dataInfoByColumn, float progress, GenericEvent<float, float, LoadingText> onChangeProgress, Action<float, Exception> outPut)
        {
            Exception exception = null;

            ReadOnlyCollection<IEEGColumn> iEEGColumns = IEEGColumns;
            int iEEGColumnsLength = iEEGColumns.Count;
            ReadOnlyCollection<CCEPColumn> ccepColumns = CCEPColumns;
            int ccepColumnsLength = ccepColumns.Count;
            float progressStep = LOAD_COLUMNS_PROGRESS / (iEEGColumnsLength * 2 + ccepColumnsLength * 2);

            // iEEG Columns
            if (iEEGColumnsLength > 0)
            {
                for (int i = 0; i < iEEGColumnsLength; ++i)
                {
                    IEEGColumn iEEGColumn = iEEGColumns[i];
                    yield return Ninja.JumpToUnity;
                    progress += progressStep;
                    onChangeProgress.Invoke(progress, 1.0f, new LoadingText("Loading iEEG column ", iEEGColumn.Name, " [" + (i + 1).ToString() + "/" + iEEGColumnsLength + "]"));
                    yield return Ninja.JumpBack;
                    try
                    {
                        iEEGColumn.Data.Load(dataInfoByColumn[iEEGColumn].OfType<iEEGDataInfo>(), iEEGColumn.Bloc);
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogException(e);
                        exception = e;
                        outPut(progress, exception);
                        yield break;
                    }
                }
                Tools.CSharp.EEG.Frequency maxiEEGFrequency = new Tools.CSharp.EEG.Frequency(iEEGColumns.Max(column => column.Data.Frequencies.Max(f => f.RawValue)));
                for (int i = 0; i < iEEGColumnsLength; ++i)
                {
                    IEEGColumn column = iEEGColumns[i];
                    yield return Ninja.JumpToUnity;
                    progress += progressStep;
                    onChangeProgress.Invoke(progress, 1.0f, new LoadingText("Loading timeline of iEEG column ", column.Name, " [" + (i + 1).ToString() + "/" + Columns.Count + "]"));
                    yield return Ninja.JumpBack;
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
                        outPut(progress, exception);
                        yield break;
                    }
                }
            }

            // CCEP Columns
            if (ccepColumnsLength > 0)
            {
                for (int i = 0; i < ccepColumnsLength; ++i)
                {
                    CCEPColumn ccepColumn = ccepColumns[i];
                    yield return Ninja.JumpToUnity;
                    progress += progressStep;
                    onChangeProgress.Invoke(progress, 1.0f, new LoadingText("Loading CCEP column ", ccepColumn.Name, " [" + (i + 1).ToString() + "/" + ccepColumnsLength + "]"));
                    yield return Ninja.JumpBack;
                    try
                    {
                        ccepColumn.Data.Load(dataInfoByColumn[ccepColumn].OfType<CCEPDataInfo>(), ccepColumn.Bloc);
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogException(e);
                        exception = e;
                        outPut(progress, exception);
                        yield break;
                    }
                }
                Tools.CSharp.EEG.Frequency maxCCEPFrequency = new Tools.CSharp.EEG.Frequency(ccepColumns.Max(column => column.Data.Frequencies.Max(f => f.RawValue)));
                for (int i = 0; i < ccepColumnsLength; ++i)
                {
                    CCEPColumn column = ccepColumns[i];
                    yield return Ninja.JumpToUnity;
                    progress += progressStep;
                    onChangeProgress.Invoke(progress, 1.0f, new LoadingText("Loading timeline of CCEP column ", column.Name, " [" + (i + 1).ToString() + "/" + Columns.Count + "]"));
                    yield return Ninja.JumpBack;
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
                        outPut(progress, exception);
                        yield break;
                    }
                }
            }

            outPut(progress, exception);
        }
        #endregion
    }
}