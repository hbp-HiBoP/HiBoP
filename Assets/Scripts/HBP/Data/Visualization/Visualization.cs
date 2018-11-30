using UnityEngine.Events;
using System;
using System.Linq;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Runtime.Serialization;
using CielaSpike;
using HBP.Data.Experience.Dataset;
using HBP.Data.Experience;
using HBP.Data.Localizer;
using Tools.Unity;
using System.IO;

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
        [DataMember(Order = 5)] public List<BaseColumn> Columns { get; set; }
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
        const float LOAD_COLUMNS_PROGRESS = 0.1f;
        const float STANDARDIZE_COLUMNS_PROGRESS = 0.075f;
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new visualization instance.
        /// </summary>
        /// <param name="name">Name of the visualization.</param>
        /// <param name="columns">Columns of the visualization.</param>
        /// <param name="id">Unique ID.</param>
        public Visualization(string name, IEnumerable<Patient> patients, IEnumerable<BaseColumn> columns, string id, VisualizationConfiguration configuration)
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
        public Visualization(string name, IEnumerable<Patient> patients, IEnumerable<BaseColumn> columns, string id) : this(name, patients, columns, id, new VisualizationConfiguration())
        {
        }
        /// <summary>
        /// Create a new visualization instance.
        /// </summary>
        /// <param name="name">Name of the visualization.</param>
        /// <param name="columns">Columns of the visualization.</param>
        public Visualization(string name, IEnumerable<Patient> patients, IEnumerable<BaseColumn> columns) : this(name, patients, columns, Guid.NewGuid().ToString())
        {
        }
        /// <summary>
        /// Create a new visualization instance with default value.
        /// </summary>
        public Visualization() : this("Unknown", new Patient[0], new BaseColumn[0])
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
            if (!Patients.Contains(patient)) m_PatientsID.Add(patient.ID);
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
            catch
            {
                throw new CanNotReadVisualizationFileException(Path.GetFileNameWithoutExtension(path));
            }
            Copy(result);
        }
        public string GetExtension()
        {
            return EXTENSION;
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
            if (IEEGColumns.Count > 0)
            {
                yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_LoadEEG(progress, onChangeProgress, e => { exception = e; }));
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
            BaseColumn tmp = Columns[index1];
            Columns[index1] = Columns[index2];
            Columns[index2] = tmp;
        }
        /// <summary>
        /// Get the DataInfo of the column.
        /// </summary>
        /// <param name="column">Column</param>
        /// <returns>DataInfo of the column.</returns>
        public IEnumerable<DataInfo> GetDataInfo(IEEGColumn column)
        {
            return column.Dataset.Data.Where((data) => (column.DataName == data.Name && Patients.Contains(data.Patient)));
        }
        /// <summary>
        /// Get the DataInfo used by the column for a specific Patient.
        /// </summary>
        /// <param name="patient">Patient concerned.</param>
        /// <param name="column">Column concerned.</param>
        /// <returns>DataInfo used by the column for the specific Patient.</returns>
        public DataInfo GetDataInfo(Patient patient, IEEGColumn column)
        {
            return GetDataInfo(column).First((dataInfo) => dataInfo.Patient == patient);
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
            return Patients.First().Brain.Implantations.Where((i) => Patients.All(p => p.Brain.Implantations.Any((ii) => ii.Name == i.Name && ii.WasUsable))).Select((i) => i.Name).ToList();
            // On a gardé le code pour tester si manque de performance.
            //List<string> commonImplantations = new List<string>();
            //foreach (Anatomy.Implantation implantation in Patients[0].Brain.Implantations)
            //{
            //    string implantationName = implantation.Name;
            //    bool isImplantationCommonToAllPatients = true;
            //    foreach (Patient patient in Patients)
            //    {
            //        if (patient.Brain.Implantations.FindIndex((i) => i.Name == implantationName && i.WasUsable) == -1)
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
        /// <summary>
        /// Clone this instance.
        /// </summary>
        /// <returns>Clone of this instance.</returns>
        public object Clone()
        {
            BaseColumn[] columns = (from column in Columns select column.Clone() as BaseColumn).ToArray();
            return new Visualization(Name, Patients, columns, ID, Configuration.Clone() as VisualizationConfiguration);
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
        IEnumerator c_LoadEEG(float progress, GenericEvent<float, float, LoadingText> onChangeProgress, Action<Exception> outPut)
        {
            Exception exception = null;

            // Find dataInfo.
            Dictionary<IEEGColumn, IEnumerable<DataInfo>> dataInfoByColumn = new Dictionary<IEEGColumn, IEnumerable<DataInfo>>();
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

            // Standardize Columns.
            if (exception == null)
            {
                yield return Ninja.JumpToUnity;
                yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_StandardizeColumns(progress, onChangeProgress, (value, e) => { progress = value; exception = e; }));
            }

            if (exception != null)
            {
                outPut(exception);
            }
        }
        IEnumerator c_FindDataInfoToRead(float progress, GenericEvent<float, float, LoadingText> onChangeProgress, Action<Dictionary<IEEGColumn, IEnumerable<DataInfo>>, float, Exception> outPut)
        {
            Exception exception = null;
            // Find files to read.
            Dictionary<IEEGColumn, IEnumerable<DataInfo>> dataInfoByColumn = new Dictionary<IEEGColumn, IEnumerable<DataInfo>>();
            float progressStep = FIND_FILES_TO_READ_PROGRESS / (IEEGColumns.Count);
            foreach (var column in IEEGColumns)
            {
                // Update progress;
                yield return Ninja.JumpToUnity;
                progress += progressStep;
                onChangeProgress.Invoke(progress, 0.0f, new LoadingText("Finding files to read."));
                yield return Ninja.JumpBack;

                try
                {
                    IEnumerable<DataInfo> dataInfoForThisColumn = GetDataInfo(column);
                    foreach (Patient patient in Patients)
                    {
                        if (!dataInfoForThisColumn.Any((dataInfo) => dataInfo.Patient == patient))
                        {
                            throw new CannotFindDataInfoException(patient.ID, column.DataName);
                        }
                    }
                    dataInfoByColumn.Add(column, GetDataInfo(column));
                }
                catch(Exception e)
                {
                    exception = e;
                }
            }
            outPut(dataInfoByColumn, progress, exception);
        }
        IEnumerator c_LoadData(Dictionary<IEEGColumn, IEnumerable<DataInfo>> dataInfoByColumn, float progress, GenericEvent<float, float, LoadingText> onChangeProgress, Action<float, Exception> outPut)
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
                onChangeProgress.Invoke(progress, 1.0f, new LoadingText("Loading ", dataInfo.Name + " for " + dataInfo.Patient.Name, " [" + (i + 1).ToString() + "/" + dataInfoCollectionLength + "]"));
                yield return Ninja.JumpBack;
                try
                {
                    Experience.Dataset.Data data = DataManager.GetData(dataInfo);
                    foreach (var column in dataInfoByColumn.Keys)
                    {
                        if (!data.DataByBloc[column.Bloc].IsValid)
                        {
                            additionalInformation = "No bloc " + column.Bloc.Name + " could be epoched.";
                            throw new Exception();
                        }
                    }
                }
                catch (Exception e)
                {
                    exception = new CannotLoadDataInfoException(dataInfo.Name, dataInfo.Patient.ID, additionalInformation);
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
                DataManager.NormalizeData();
            }
            outPut(progress, exception);
        }
        IEnumerator c_LoadColumns(Dictionary<IEEGColumn, IEnumerable<DataInfo>> dataInfoByColumn, float progress, GenericEvent<float, float, LoadingText> onChangeProgress, Action<float, Exception> outPut)
        {
            Exception exception = null;
            ReadOnlyCollection<IEEGColumn> columns = IEEGColumns;
            int columnsLength = columns.Count;

            float progressStep = LOAD_COLUMNS_PROGRESS / (columnsLength * 2);
            for (int i = 0; i < columnsLength; ++i)
            {
                IEEGColumn column = columns[i];
                yield return Ninja.JumpToUnity;
                progress += progressStep;
                onChangeProgress.Invoke(progress, 1.0f, new LoadingText("Loading column ", column.Name, " [" + (i + 1).ToString() + "/" + columnsLength + "]"));
                yield return Ninja.JumpBack;
                try
                {
                    column.Data.Load(dataInfoByColumn[column], column.Bloc);
                }
                catch (Exception e)
                {
                    exception = e;
                    outPut(progress, exception);
                    yield break;
                }
            }
            Frequency maxFrequency = new Frequency(columns.Max(column => column.Data.Frequencies.Max(f => f.RawValue)));
            for (int i = 0; i < columnsLength; ++i)
            {
                IEEGColumn column = columns[i];
                yield return Ninja.JumpToUnity;
                progress += progressStep;
                onChangeProgress.Invoke(progress, 1.0f, new LoadingText("Loading timeline of column ", column.Name, " [" + (i + 1).ToString() + "/" + Columns.Count + "]"));
                yield return Ninja.JumpBack;
                column.Data.SetTimeline(maxFrequency, column.Bloc, columns.Select(c => c.Bloc).Distinct());
                yield return Ninja.JumpToUnity;
                try
                {
                    column.Data.IconicScenario.LoadIcons();
                }
                catch (Exception e)
                {
                    exception = e;
                    outPut(progress, exception);
                    yield break;
                }
            }
            outPut(progress, exception);
        }
        IEnumerator c_StandardizeColumns(float progress, GenericEvent<float, float, LoadingText> onChangeProgress, Action<float, Exception> outPut)
        {
            ReadOnlyCollection<IEEGColumn> columns = IEEGColumns;
            int columnsLength = columns.Count;
            Exception exception = null;

            float progressStep = STANDARDIZE_COLUMNS_PROGRESS / columnsLength;
            //int maxBefore = columns.Max(column => column.Data.TimeLine.MainEvent.Position);
            //int maxAfter = columns.Max(column => column.Data.TimeLine.Lenght - column.Data.TimeLine.MainEvent.Position);
            for (int i = 0; i < columnsLength; ++i)
            {
                IEEGColumn column = columns[i];
                yield return Ninja.JumpToUnity;
                progress += progressStep;
                onChangeProgress.Invoke(progress, 0, new LoadingText("Standardize column ", column.Name, " [" + (i + 1).ToString() + "/" + Columns.Count + "]"));
                yield return Ninja.JumpBack;
                //try
                //{
                //    column.Data.Standardize(maxBefore, maxAfter);
                //}
                //catch (Exception e)
                //{
                //    exception = e;
                //    break;
                //}
            }
            outPut(progress, exception);
        }
        #endregion
    }
}