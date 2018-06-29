using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using HBP.Data.Experience.Dataset;
using HBP.Data.Experience.Protocol;
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
    public class Column : ICloneable
    {
        #region Properties
        [DataMember] public string Name { get; set; }

        public enum ColumnType { Anatomy, iEEG }
        [DataMember] public ColumnType Type { get; set; }

        [DataMember(Name = "Dataset")]
        private string datasetID;
        /// <summary>
        /// Dataset of the column.
        /// </summary>
        public Dataset Dataset
        {
            get { return ApplicationState.ProjectLoaded.Datasets.FirstOrDefault(p => p.ID == datasetID); }
            set { datasetID = value.ID; }
        }

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
        public Protocol Protocol
        {
            get { return ApplicationState.ProjectLoaded.Protocols.FirstOrDefault(p => p.ID == protocolID); }
            set { protocolID = value.ID; }
        }

        [DataMember(Name = "Bloc")]
        private string blocID;
        /// <summary>
        /// Protocol bloc of the column.
        /// </summary>
        public Bloc Bloc
        {
            get { return Protocol.Blocs.FirstOrDefault(p => p.ID == blocID); }
            set { blocID = value.ID; }
        }

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

        public Dictionary<int, Timeline> TimeLineByFrequency { get; set; }
        public Dictionary<int, IconicScenario> IconicScenarioByFrequency { get; set; }

        public List<int> Frequencies = new List<int>();
        private List<Tuple<Localizer.Bloc, int>> m_Blocs = new List<Tuple<Localizer.Bloc, int>>();
        private Dictionary<SiteConfiguration, int> m_FrequencyBySiteConfiguration = new Dictionary<SiteConfiguration, int>();
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new Column instance.
        /// </summary>
        /// <param name="dataset">Dataset to use in the visualization Column.</param>
        /// <param name="dataLabel">Label of the data to use in the visualization Column.</param>
        /// <param name="protocol">Protocol to use in the visualization Column.</param>
        /// <param name="bloc">Bloc of the Protocol to use in the visualization Column.</param>
        public Column(string name, Dataset dataset, string dataLabel, Protocol protocol, Bloc bloc, ColumnType type, ColumnConfiguration configuration)
        {
            Name = name;
            Dataset = dataset;
            Protocol = protocol;
            Bloc = bloc;
            Data = dataLabel;
            Type = type;
            Configuration = configuration;
        }
        public Column(int column, IEnumerable<Patient> patients, IEnumerable<Dataset> datasets) : this()
        {
            Dataset datasetUsable = null;
            string nameUsable = string.Empty;
            foreach (Dataset dataset in datasets)
            {
                foreach (var name in dataset.Data.Select(d => d.Name).Distinct())
                {
                    if(patients.All((p) => dataset.Data.Any((d) => (d.Patient == p && d.Name == name))))
                    {
                        datasetUsable = dataset;
                        nameUsable = name;
                        goto Found;
                    }
                }
            }
            Found:
            if(datasetUsable != null)
            {
                Protocol = datasetUsable.Protocol;
                Bloc = datasetUsable.Protocol.Blocs.First();
                Dataset = datasetUsable;
                Data = nameUsable;
                Configuration = new ColumnConfiguration();
            }
            Name = "Column n°" + column;
            Type = ColumnType.iEEG;
        }
        /// <summary>
        /// Create a new Column instance with default values.
        /// </summary>
        public Column():this("New column", new Dataset(), string.Empty,new Protocol(),new Bloc(), ColumnType.iEEG, new ColumnConfiguration())
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
            Frequencies = new List<int>();
            m_Blocs = new List<Tuple<Localizer.Bloc, int>>();
            m_FrequencyBySiteConfiguration = new Dictionary<SiteConfiguration, int>();
            foreach (DataInfo dataInfo in columnData)
            {
                Experience.EpochedData epochedData = DataManager.GetData(dataInfo, Bloc);
                int frequency = UnityEngine.Mathf.RoundToInt(epochedData.Frequency);
                Frequencies.Add(frequency);
                Localizer.Bloc averagedBloc = Localizer.Bloc.Average(epochedData.Blocs,ApplicationState.UserPreferences.Data.EEG.Averaging, ApplicationState.UserPreferences.Data.Event.PositionAveraging);
                m_Blocs.AddRange(from bloc in epochedData.Blocs select new Tuple<Localizer.Bloc, int>(bloc, frequency));
                foreach (var site in averagedBloc.ValuesBySite.Keys)
                {
                    SiteConfiguration siteConfiguration;
                    if (Configuration.ConfigurationBySite.TryGetValue(site, out siteConfiguration))
                    {
                        siteConfiguration.Values = averagedBloc.ValuesBySite[site];
                        siteConfiguration.NormalizedValues = averagedBloc.NormalizedValuesBySite[site];
                        siteConfiguration.Unit = averagedBloc.UnitBySite[site];
                    }
                    else
                    {
                        siteConfiguration = new SiteConfiguration(averagedBloc.ValuesBySite[site], averagedBloc.NormalizedValuesBySite[site], averagedBloc.UnitBySite[site], false, false, false, false, false);
                        Configuration.ConfigurationBySite.Add(site, siteConfiguration);
                    }
                    m_FrequencyBySiteConfiguration.Add(siteConfiguration, frequency);
                }
            }
            Frequencies = Frequencies.Distinct().ToList();
        }
        public void SetTimeline(int maxFrequency)
        {
            // TODO
            //Frequencies.Add(maxFrequency);
            //Frequencies = Frequencies.Distinct().ToList();
            //Event mainEvent = new Event();
            //List<Event> secondaryEvents = new List<Event>(Bloc.SecondaryEvents.Count);
            //switch (ApplicationState.UserPreferences.Data.Event.PositionAveraging)
            //{
            //    case Enums.AveragingType.Mean:
            //        mainEvent = new Event(Bloc.MainEvent.Name, UnityEngine.Mathf.RoundToInt((from bloc in m_Blocs select bloc.Object1.PositionByEvent[Bloc.MainEvent] * ((float)maxFrequency / bloc.Object2)).ToArray().Mean()));
            //        for (int i = 0; i < Bloc.SecondaryEvents.Count; i++)
            //        {
            //            List<Tuple<Localizer.Bloc, int>> blocWhereEventFound = (from bloc in m_Blocs where bloc.Object1.PositionByEvent[Bloc.SecondaryEvents[i]] * (maxFrequency / bloc.Object2) >= 0 select bloc).ToList();
            //            if (blocWhereEventFound.Count > 0)
            //            {
            //                float rate = (float)blocWhereEventFound.Count / m_Blocs.Count;
            //                secondaryEvents.Add(new Event(Bloc.SecondaryEvents[i].Name, UnityEngine.Mathf.RoundToInt((from bloc in blocWhereEventFound select bloc.Object1.PositionByEvent[Bloc.SecondaryEvents[i]] * (maxFrequency / bloc.Object2)).ToArray().Mean()), rate));
            //            }
            //        }
            //        break;
            //    case Enums.AveragingType.Median:
            //        mainEvent = new Event(Bloc.MainEvent.Name, UnityEngine.Mathf.RoundToInt((from bloc in m_Blocs select bloc.Object1.PositionByEvent[Bloc.MainEvent] * ((float)maxFrequency / bloc.Object2)).ToArray().Median()));
            //        for (int i = 0; i < Bloc.SecondaryEvents.Count; i++)
            //        {
            //            List<Tuple<Localizer.Bloc, int>> blocWhereEventFound = (from bloc in m_Blocs where bloc.Object1.PositionByEvent[Bloc.SecondaryEvents[i]] * (maxFrequency / bloc.Object2) >= 0 select bloc).ToList();
            //            if (blocWhereEventFound.Count > 0)
            //            {
            //                float rate = (float)blocWhereEventFound.Count / m_Blocs.Count;
            //                secondaryEvents.Add(new Event(Bloc.SecondaryEvents[i].Name, UnityEngine.Mathf.RoundToInt((from bloc in blocWhereEventFound select bloc.Object1.PositionByEvent[Bloc.SecondaryEvents[i]] * (maxFrequency / bloc.Object2)).ToArray().Median()), rate));
            //            }
            //        }
            //        break;
            //}

            //// Timeline
            //TimeLineByFrequency = new Dictionary<int, Timeline>();
            //IconicScenarioByFrequency = new Dictionary<int, IconicScenario>();
            //foreach (int frequency in Frequencies)
            //{
            //    TimeLineByFrequency.Add(frequency, new Timeline(Bloc.Window, mainEvent, secondaryEvents.ToArray(), frequency));
            //    IconicScenarioByFrequency.Add(frequency, new IconicScenario(Bloc, frequency, TimeLineByFrequency[frequency]));
            //}
            //TimeLine = new Timeline(Bloc.Window, mainEvent, secondaryEvents.ToArray(), maxFrequency);
            //IconicScenario = new IconicScenario(Bloc, maxFrequency, TimeLine);

            //// Resampling taking frequencies into account
            //if (Frequencies.Count > 1)
            //{
            //    //int maxSize = (from siteConfiguration in Configuration.ConfigurationBySite.Values select siteConfiguration.Values).Max(v => v.Length);
            //    foreach (var siteConfiguration in Configuration.ConfigurationBySite.Values)
            //    {
            //        int frequency;
            //        if (m_FrequencyBySiteConfiguration.TryGetValue(siteConfiguration, out frequency))
            //        {
            //            Timeline timeline = TimeLineByFrequency[frequency];
            //            int samplesBefore = (int)((timeline.Start.RawValue - TimeLine.Start.RawValue) / TimeLine.Step);
            //            int samplesAfter = (int)((TimeLine.End.RawValue - timeline.End.RawValue) / TimeLine.Step);
            //            siteConfiguration.ResizeValues(TimeLine.Lenght, samplesBefore, samplesAfter);
            //        }
            //    }
            //}
        }
        /// <summary>
        /// Test if the visualization Column is compatible with a Patient.
        /// </summary>
        /// <param name="patient">Patient to test.</param>
        /// <returns>\a True if is compatible and \a false otherwise.</returns>
        public bool IsCompatible(Patient patient)
        {
            switch (Type)
            {
                case ColumnType.Anatomy:
                    return true;
                case ColumnType.iEEG:
                    return Dataset.Data.Any((data) => data.Name == Data && data.Patient == patient && data.isOk);
                default:
                    return false;
            }
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
            int diffBefore = before - TimeLine.MainEvent.Position;
            int diffAfter = after - (TimeLine.Lenght - TimeLine.MainEvent.Position);
            TimeLine.Resize(diffBefore, diffAfter);
            foreach (var siteConfiguration in Configuration.ConfigurationBySite.Values)
            {
                siteConfiguration.Resize(diffBefore, diffAfter);
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
            return new Column(Name, Dataset, Data, Protocol, Bloc, Type, Configuration.Clone() as ColumnConfiguration);
        }
        #endregion
    }
}