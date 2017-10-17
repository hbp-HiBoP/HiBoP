using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using HBP.Data.Anatomy;
using HBP.Data.Experience.Dataset;
using HBP.Data.Experience.Protocol;
using UnityEngine;
using Tools.CSharp;


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
        public string Data { get; set; }

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

        /// <summary>
        /// Display label of the column
        /// </summary>
        public string DisplayLabel
        {
            get
            {
                return Data + " | " + Dataset.Name + " | " + Protocol.Name + " | " + Bloc.DisplayInformations.Name;
                //return "Data: " + DataLabel + ", Dataset: " + Dataset.Name + ", Protocol: " + Protocol.Name + ", Bloc: " + Bloc.DisplayInformations.Name;
            }
        }
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
            Data = dataLabel;
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
        /// <param name="columnData"></param>
        public void Load(IEnumerable<DataInfo> columnData)
        {
            // FIXME
            float frequency = 0;

            List<Localizer.Bloc> blocs = new List<Localizer.Bloc>();
            Dictionary<string, SiteConfiguration> siteConfigurationsByID = new Dictionary<string, SiteConfiguration>();
            foreach (DataInfo dataInfo in columnData)
            {
                DataManager.GetData(dataInfo, Bloc);
            }
            DataManager.NormalizeData();
            foreach (DataInfo dataInfo in columnData)
            {
                Experience.EpochedData epochedData = DataManager.GetData(dataInfo, Bloc);
                //FIXME
                frequency = epochedData.Frequency;
                Localizer.Bloc averagedBloc = Localizer.Bloc.Average(epochedData.Blocs);
                blocs.AddRange(epochedData.Blocs);
                foreach (var site in averagedBloc.ValuesBySite.Keys)
                {
                    siteConfigurationsByID.Add(site, new SiteConfiguration(averagedBloc.ValuesBySite[site],averagedBloc.NormalizedValuesBySite[site], false, false, false, false));
                    if (Configuration.ConfigurationBySite.ContainsKey(site))
                    {
                        siteConfigurationsByID[site].LoadSerializedConfiguration(Configuration.ConfigurationBySite[site]);
                    }
                }
            }

            Configuration.ConfigurationBySite = siteConfigurationsByID;
            Event mainEvent = new Event();
            Event[] secondaryEvents = new Event[Bloc.SecondaryEvents.Count];
            switch (ApplicationState.GeneralSettings.EventPositionAveraging)
            {
                case Settings.GeneralSettings.AveragingMode.Mean:
                    mainEvent = new Event(Bloc.MainEvent.Name,(int) (from bloc in blocs select bloc.PositionByEvent[Bloc.MainEvent]).ToList().Average());
                    for (int i = 0; i < secondaryEvents.Length; i++)
                    {
                        List<Localizer.Bloc> blocWhereEventFound = (from bloc in blocs where bloc.PositionByEvent[Bloc.SecondaryEvents[i]] >= 0 select bloc).ToList();
                        float rate = (float) blocWhereEventFound.Count / blocs.Count;
                        secondaryEvents[i] = new Event(Bloc.SecondaryEvents[i].Name,(int) (from bloc in blocWhereEventFound select bloc.PositionByEvent[Bloc.SecondaryEvents[i]]).ToList().Average(), rate);
                    }
                    break;
                case Settings.GeneralSettings.AveragingMode.Median:
                    mainEvent = new Event(Bloc.MainEvent.Name, (from bloc in blocs select bloc.PositionByEvent[Bloc.MainEvent]).ToList().Median());
                    for (int i = 0; i < secondaryEvents.Length; i++)
                    {
                        List<Localizer.Bloc> blocWhereEventFound = (from bloc in blocs where bloc.PositionByEvent[Bloc.SecondaryEvents[i]] >= 0 select bloc).ToList();
                        float rate = (float)blocWhereEventFound.Count / blocs.Count;
                        secondaryEvents[i] = new Event(Bloc.SecondaryEvents[i].Name, (int)(from bloc in blocWhereEventFound select bloc.PositionByEvent[Bloc.SecondaryEvents[i]]).ToList().Median(), rate);
                    }
                    break;
            }
            TimeLine = new Timeline(Bloc.DisplayInformations, mainEvent, secondaryEvents, frequency);
            IconicScenario = new IconicScenario(Bloc, frequency, TimeLine);
        }
        /// <summary>
        /// Test if the visualization Column is compatible with a Patient.
        /// </summary>
        /// <param name="patient">Patient to test.</param>
        /// <returns>\a True if is compatible and \a false otherwise.</returns>
        public bool IsCompatible(Patient patient)
        {
            return Dataset.Data.Any((data) => data.Name == Data && data.Patient == patient && data.isOk);
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
            int diffBefore = TimeLine.MainEvent.Position - before;
            int diffAfter = (TimeLine.Lenght - TimeLine.MainEvent.Position) - after;
            TimeLine.Resize(diffBefore, diffAfter);
        }
        #endregion

        #region Operators
        /// <summary>
        /// Clone this instance.
        /// </summary>
        /// <returns>Clone of this instance.</returns>
        public object Clone()
        {
            return new Column(Dataset, Data, Protocol, Bloc, Configuration.Clone() as ColumnConfiguration);
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