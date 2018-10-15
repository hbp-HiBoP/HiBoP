using HBP.Data.Experience.Dataset;
using HBP.Data.Localizer;
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

        public Dictionary<Frequency, Timeline> TimeLineByFrequency { get; set; }
        public Dictionary<Frequency, IconicScenario> IconicScenarioByFrequency { get; set; }

        public Dictionary<Patient, PatientData> DataByPatient = new Dictionary<Patient, PatientData>();
        public Dictionary<string, SiteData> DataBySite = new Dictionary<string, SiteData>();
        #endregion

        #region Constructors

        #endregion

        #region Public Methods
        public void Load(IEnumerable<DataInfo> columnData, Experience.Protocol.Bloc bloc)
        {
            foreach (DataInfo dataInfo in columnData)
            {
                EpochedData epochedData = DataManager.GetData(dataInfo, bloc);
                // Values
                foreach (var site in epochedData.UnitByChannel.Keys)
                {
                    DataBySite.Add(site, new SiteData(epochedData, site));
                }
                // Events
                DataByPatient.Add(dataInfo.Patient, new PatientData(epochedData));
            }
        }
        public void SetTimeline(int maxFrequency)
        {
            //// TODO
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

