using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Collections.Generic;
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
        /// Load a data in the column.
        /// </summary>
        /// <param name="data"></param>
        public void Load(Experience.Dataset.Data data)
        {
            Experience.EpochedData epochedData = new Experience.EpochedData(Bloc,data);
            Localizer.Bloc bloc = Localizer.Bloc.Average(epochedData.Blocs);
            foreach (Electrode electrode in data.Patient.Brain.Implantation.Electrodes)
            {
                foreach (Site site in electrode.Sites)
                {
                    SiteConfiguration siteConfiguration = Configuration.ConfigurationByPatient[data.Patient].ConfigurationByElectrode[electrode].ConfigurationBySite[site];
                    siteConfiguration.IsMasked = data.MaskBySite[site];
                    siteConfiguration.Values = bloc.ValuesBySite[site];
                }
            }
            TimeLine = new Timeline(Bloc.DisplayInformations, new Event(Bloc.MainEvent.Name, bloc.PositionByEvent[Bloc.MainEvent]), (from evt in Bloc.SecondaryEvents select new Event(evt.Name, bloc.PositionByEvent[evt])).ToArray(), data.Frequency);
            IconicScenario = new IconicScenario(Bloc, data.Frequency, TimeLine);
        }
        /// <summary>
        /// Load multiple data in the column.
        /// </summary>
        /// <param name="data"></param>
        public void Load(IEnumerable<Experience.Dataset.Data> data)
        {
            Dictionary<Experience.Dataset.Data, Localizer.Bloc> BlocByData = new Dictionary<Experience.Dataset.Data, Localizer.Bloc>();
            foreach (var d in data)
            {
                Experience.EpochedData epochedData = new Experience.EpochedData(Bloc, d);
                BlocByData.Add(d,Localizer.Bloc.Average(epochedData.Blocs));
                foreach (Electrode electrode in d.Patient.Brain.Implantation.Electrodes)
                {
                    foreach (Site site in electrode.Sites)
                    {
                        SiteConfiguration siteConfiguration = Configuration.ConfigurationByPatient[d.Patient].ConfigurationByElectrode[electrode].ConfigurationBySite[site];
                        siteConfiguration.IsMasked = d.MaskBySite[site];
                        siteConfiguration.Values = BlocByData[d].ValuesBySite[site];
                    }
                }
            }
            TimeLine = new Timeline(Bloc.DisplayInformations, new Event(Bloc.MainEvent.Name, (int) Math.Round(BlocByData.Values.Average((b) => b.PositionByEvent[Bloc.MainEvent]))), (from evt in Bloc.SecondaryEvents select new Event(evt.Name, (int) BlocByData.Values.Average((b) => b.PositionByEvent[evt]))).ToArray(), data.Average((d) => d.Frequency));
            IconicScenario = new IconicScenario(Bloc, data.Average((d) => d.Frequency), TimeLine);
        }
        /// <summary>
        /// Test if the visualization Column is compatible with a Patient.
        /// </summary>
        /// <param name="patient">Patient to test.</param>
        /// <returns>\a True if is compatible and \a false otherwise.</returns>
        public bool IsCompatible(Patient patient)
        {
            return Dataset.Data.Exists((dataInfo) => dataInfo.Name == DataLabel && dataInfo.Protocol == Protocol && dataInfo.Patient == patient && dataInfo.UsableInMultiPatients);
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