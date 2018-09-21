using HBP.Data.Experience.Dataset;
using System;
using System.Collections.Generic;

namespace HBP.Data.Visualization
{
    public class IEEGData
    {
        #region Properties
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
        List<Tuple<Localizer.Bloc, int>> m_Blocs = new List<Tuple<Localizer.Bloc, int>>();

        public Dictionary<string, SiteData> DataBySite = new Dictionary<string, SiteData>();
        #endregion

        #region Constructors
        #endregion

        #region Public Methods
        public void Load(IEnumerable<DataInfo> columnData)
        {
            //Frequencies = new List<int>();
            //m_Blocs = new List<Tuple<Localizer.Bloc, int>>();
            //m_FrequencyBySiteConfiguration = new Dictionary<SiteConfiguration, int>();
            //foreach (DataInfo dataInfo in columnData)
            //{
            //    Experience.EpochedData epochedData = DataManager.GetData(dataInfo, Bloc);
            //    int frequency = UnityEngine.Mathf.RoundToInt(epochedData.Frequency);
            //    Frequencies.Add(frequency);
            //    Localizer.Bloc averagedBloc = Localizer.Bloc.Average(epochedData.Blocs, ApplicationState.UserPreferences.Data.EEG.Averaging, ApplicationState.UserPreferences.Data.Event.PositionAveraging);
            //    m_Blocs.AddRange(from bloc in epochedData.Blocs select new Tuple<Localizer.Bloc, int>(bloc, frequency));
            //    foreach (var site in averagedBloc.ValuesBySite.Keys)
            //    {
            //        SiteConfiguration siteConfiguration;
            //        if (IEEGConfiguration.ConfigurationBySite.TryGetValue(site, out siteConfiguration))
            //        {
            //            siteConfiguration.Values = averagedBloc.ValuesBySite[site];
            //            siteConfiguration.NormalizedValues = averagedBloc.NormalizedValuesBySite[site];
            //            siteConfiguration.Unit = averagedBloc.UnitBySite[site];
            //        }
            //        else
            //        {
            //            siteConfiguration = new SiteConfiguration(averagedBloc.ValuesBySite[site], averagedBloc.NormalizedValuesBySite[site], averagedBloc.UnitBySite[site], false, false, false, false, false);
            //            IEEGConfiguration.ConfigurationBySite.Add(site, siteConfiguration);
            //        }
            //        m_FrequencyBySiteConfiguration.Add(siteConfiguration, frequency);
            //    }
            //}
            //Frequencies = Frequencies.Distinct().ToList();
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
            //        mainEvent = new Event(Bloc.MainEvent.Name, UnityEngine.Mathf.RoundToInt((from bloc in m_Blocs select bloc.Item1.PositionByEvent[Bloc.MainEvent] * ((float)maxFrequency / bloc.Item2)).ToArray().Mean()));
            //        for (int i = 0; i < Bloc.SecondaryEvents.Count; i++)
            //        {
            //            List<Tuple<Localizer.Bloc, int>> blocWhereEventFound = (from bloc in m_Blocs where bloc.Item1.PositionByEvent[Bloc.SecondaryEvents[i]] * (maxFrequency / bloc.Item2) >= 0 select bloc).ToList();
            //            if (blocWhereEventFound.Count > 0)
            //            {
            //                float rate = (float)blocWhereEventFound.Count / m_Blocs.Count;
            //                secondaryEvents.Add(new Event(Bloc.SecondaryEvents[i].Name, UnityEngine.Mathf.RoundToInt((from bloc in blocWhereEventFound select bloc.Item1.PositionByEvent[Bloc.SecondaryEvents[i]] * (maxFrequency / bloc.Item2)).ToArray().Mean()), rate));
            //            }
            //        }
            //        break;
            //    case Enums.AveragingType.Median:
            //        mainEvent = new Event(Bloc.MainEvent.Name, UnityEngine.Mathf.RoundToInt((from bloc in m_Blocs select bloc.Item1.PositionByEvent[Bloc.MainEvent] * ((float)maxFrequency / bloc.Item2)).ToArray().Median()));
            //        for (int i = 0; i < Bloc.SecondaryEvents.Count; i++)
            //        {
            //            List<Tuple<Localizer.Bloc, int>> blocWhereEventFound = (from bloc in m_Blocs where bloc.Item1.PositionByEvent[Bloc.SecondaryEvents[i]] * (maxFrequency / bloc.Item2) >= 0 select bloc).ToList();
            //            if (blocWhereEventFound.Count > 0)
            //            {
            //                float rate = (float)blocWhereEventFound.Count / m_Blocs.Count;
            //                secondaryEvents.Add(new Event(Bloc.SecondaryEvents[i].Name, UnityEngine.Mathf.RoundToInt((from bloc in blocWhereEventFound select bloc.Item1.PositionByEvent[Bloc.SecondaryEvents[i]] * (maxFrequency / bloc.Item2)).ToArray().Median()), rate));
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
        public void Standardize(int before, int after)
        {
            //int diffBefore = before - TimeLine.MainEvent.Position;
            //int diffAfter = after - (TimeLine.Lenght - TimeLine.MainEvent.Position);
            //TimeLine.Resize(diffBefore, diffAfter);
            //foreach (var siteConfiguration in IEEGConfiguration.ConfigurationBySite.Values)
            //{
            //    siteConfiguration.Resize(diffBefore, diffAfter);
            //}
        }
        #endregion
    }
}

