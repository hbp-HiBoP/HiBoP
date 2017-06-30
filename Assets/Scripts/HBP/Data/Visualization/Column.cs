using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using HBP.Data.Anatomy;
using HBP.Data.Experience.Dataset;
using HBP.Data.Experience.Protocol;

namespace HBP.Data.Visualization
{
    /**
    * \class Column
    * \author Adrien Gannerie
    * \version 1.0
    * \date 10 janvier 2017
    * \brief Visualization column.
    * 
    * \detail Visualization column is a class which contains all the information for the display wanted for a column and contains:
    *   - \a Dataset.
    *   - \a DataLabel.
    *   - \a Protocol.
    *   - \a Bloc.
    */
    [DataContract]
    public class Column: ICloneable
    {
        #region Properties
        [DataMember(Name = "Dataset")]
        private string datasetID;
        /// <summary>
        /// Dataset of the column.
        /// </summary>
        public Dataset Dataset { get; set; }

        /// <summary>
        /// Data label of the column.
        /// </summary>
        [DataMember(Name = "Label")]
        public string DataLabel { get; set; }

        [DataMember(Name = "Protocol")]
        private string protocolID;
        /// <summary>
        /// Protocol of the column.
        /// </summary>
        public Protocol Protocol { get; set; }

        [DataMember(Name = "Bloc")]
        private string blocID;
        /// <summary>
        /// Protocol bloc of the column.
        /// </summary>
        public Bloc Bloc { get; set; }

        /// <summary>
        /// Configuration of the column.
        /// </summary>
        [DataMember(Name = "Configuration")]
        public ColumnConfiguration Configuration { get; set; }

        /// <summary>
        /// Timeline of the column which define the size,limits,events.
        /// </summary>
        public Timeline TimeLine { get; set; }

        /// <summary>
        /// Iconic scenario which define the labels,images to display during the timeLine. 
        /// </summary>
        public IconicScenario IconicScenario { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new Column instance.
        /// </summary>
        /// <param name="dataset">Dataset to use in the visualization Column.</param>
        /// <param name="dataLabel">Label of the data to use in the visualization Column.</param>
        /// <param name="protocol">Protocol to use in the visualization Column.</param>
        /// <param name="bloc">Bloc of the Protocol to use in the visualization Column.</param>
        public Column(Dataset dataset, string dataLabel, Protocol protocol, Bloc bloc,ColumnConfiguration configuration)
        {
            Dataset = dataset;
            Protocol = protocol;
            Bloc = bloc;
            DataLabel = dataLabel;
            Configuration = configuration;
        }
        /// <summary>
        /// Create a new Column instance with default values.
        /// </summary>
        public Column():this(new Dataset(), string.Empty,new Protocol(),new Bloc(),new ColumnConfiguration())
        {
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Load multiple data in the column.
        /// </summary>
        /// <param name="data"></param>
        public void Load(IEnumerable<Experience.Dataset.Data> data)
        {
            Dictionary<Experience.Dataset.Data, Localizer.Bloc> blocByData = new Dictionary<Experience.Dataset.Data, Localizer.Bloc>();
            Dictionary<string, SiteConfiguration> siteConfigurationsByName = new Dictionary<string, SiteConfiguration>();
            foreach (var d in data)
            {
                Experience.EpochedData epochedData = new Experience.EpochedData(Bloc, d);
                blocByData.Add(d,Localizer.Bloc.Average(epochedData.Blocs));
                foreach(var item in blocByData[d].ValuesBySite)
                {
                    siteConfigurationsByName.Add(item.Key, new SiteConfiguration(item.Value,false,false,false,false,false,UnityEngine.Color.white));
                }
            }
            TimeLine = new Timeline(Bloc.DisplayInformations, new Event(Bloc.MainEvent.Name, (int) Math.Round(blocByData.Values.Average((b) => b.PositionByEvent[Bloc.MainEvent]))), (from evt in Bloc.SecondaryEvents select new Event(evt.Name, (int) blocByData.Values.Average((b) => b.PositionByEvent[evt]))).ToArray(), data.Average((d) => d.Frequency));
            IconicScenario = new IconicScenario(Bloc, data.Average((d) => d.Frequency), TimeLine);
        }
        /// <summary>
        /// Test if the visualization Column is compatible with a Patient.
        /// </summary>
        /// <param name="patient">Patient to test.</param>
        /// <returns>\a True if is compatible and \a false otherwise.</returns>
        public bool IsCompatible(Patient patient)
        {
            return Dataset.Data.Exists((dataInfo) => dataInfo.Name == DataLabel && dataInfo.Protocol == Protocol && dataInfo.Patient == patient && dataInfo.isOk);
        }
        /// <summary>
        /// Test if the visualisaation Column is compatible with some Patients.
        /// </summary>
        /// <param name="patients">Patients to test.</param>
        /// <returns>\a True if is compatible and \a false otherwise.</returns>
        public bool IsCompatible(IEnumerable<Patient> patients)
        {
            return patients.All((patient) => IsCompatible(patient));
        }
        /// <summary>
        /// Dispose the data of the column
        /// </summary>
        public void Unload()
        {
            TimeLine = null;
            IconicScenario = null;
        }
        /// <summary>
        /// Standardize column.
        /// </summary>
        /// <param name="before">sample before</param>
        /// <param name="after">sample after</param>
        public void Standardize(int before,int after)
        {
            UnityEngine.Debug.Log("Before : " + before);
            UnityEngine.Debug.Log("After : " + after);
            int diffBefore = TimeLine.MainEvent.Position - before;
            int diffAfter = (TimeLine.Lenght - TimeLine.MainEvent.Position) - after;
            UnityEngine.Debug.Log("DiffBefore : " + diffBefore);
            UnityEngine.Debug.Log("DiffAfter : " + diffAfter);
            TimeLine.Resize(diffBefore, diffAfter);
            //foreach(var patientConfiguration in Configuration.ConfigurationByPatient.Values)
            //{
            //    foreach(var electrodeConfiguration in patientConfiguration.ConfigurationByElectrode.Values)
            //    {
            //        foreach(var siteConfiguration in electrodeConfiguration.ConfigurationBySite.Values)
            //        {
            //            siteConfiguration.Values;
            //        }
            //    }
            //}
        }
        #endregion

        #region Operators
        /// <summary>
        /// Clone this instance.
        /// </summary>
        /// <returns>Clone of this instance.</returns>
        public object Clone()
        {
            return new Column(Dataset.Clone() as Dataset, DataLabel.Clone() as string, Protocol.Clone() as Protocol, Bloc.Clone() as Bloc, Configuration.Clone() as ColumnConfiguration);
        }
        #endregion

        #region Serialization
        [OnSerializing]
        void OnSerializing(StreamingContext streamingContext)
        {
            datasetID = Dataset.ID;
            protocolID = Protocol.ID;
            blocID = Bloc.ID;
        }
        [OnDeserialized]
        void OnDeserialized(StreamingContext streamingContext)
        {
            Dataset = ApplicationState.ProjectLoaded.Datasets.FirstOrDefault(p => p.ID == datasetID);
            Protocol = ApplicationState.ProjectLoaded.Protocols.FirstOrDefault(p => p.ID == protocolID);
            Bloc = Protocol.Blocs.ToList().Find(p => p.ID == blocID);
        }
        #endregion
    }
}