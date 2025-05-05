using System;
using System.Collections.Generic;
using System.Linq;

namespace HBP.Core.Data
{
    public class BlocData
    {
        #region Properties
        public bool IsValid
        {
            get
            {
                return Trials.Length > 0 && Trials.Any(t => t.IsValid);
            }
        }
        public Tools.Frequency Frequency { get; set; }
        public Trial[] Trials { get; set; }
        #endregion

        #region Constructors
        public BlocData(DynamicData data, Bloc bloc)
        {
            // Find all occurences for each event.
            Dictionary<Event, EventOccurences> occurencesByEvent = bloc.SubBlocs.SelectMany((s) => s.Events).ToDictionary((e) => e, (e) => new EventOccurences(e.Codes.ToDictionary((c) => c, (c) => data.GetOccurences(c).ToArray())));

            // Get all occurences for the mainEvent of the mainSubBloc.
            EventOccurence[] MainSubBlocMainEventOccurences = occurencesByEvent[bloc.MainSubBloc.MainEvent].GetOccurences();

            // Initialize loop.
            List<Trial> trials = new List<Trial>(MainSubBlocMainEventOccurences.Length);
            int startIndex, endIndex;

            // All main event position but the last one.
            for (int i = 0; i < MainSubBlocMainEventOccurences.Length; i++)
            {
                startIndex = (i - 1 < 0) ? 0 : MainSubBlocMainEventOccurences[i - 1].Index;
                endIndex = (i + 1 >= MainSubBlocMainEventOccurences.Length) ? int.MaxValue : MainSubBlocMainEventOccurences[i + 1].Index;
                trials.Add(new Trial(data.ValuesByChannel, data.UnitByChannel, startIndex, MainSubBlocMainEventOccurences[i], endIndex, occurencesByEvent, bloc, data.Frequency));
            }
            Trials = SortTrials(bloc, trials);

            Frequency = data.Frequency;
        }
        #endregion

        #region Public Methods
        public void Clear()
        {
            foreach (var trial in Trials)
            {
                trial.Clear();
            }
            Trials = new Trial[0];
        }
        #endregion

        #region Private Methods
        static Trial[] SortTrials(Bloc bloc, IEnumerable<Trial> trials)
        {
            var orderedTrials = trials.Where(t => t.IsValid).ToList();
            string[] orders = bloc.Sort.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = orders.Length - 1; i >= 0; i--)
            {
                string[] parts = orders[i].Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 3) continue;

                string subBlocName = parts[0];
                string eventName = string.Join('_', parts[1..^1]);
                string command = parts[^1];

                SubBloc subBloc = bloc.SubBlocs.FirstOrDefault(s => s.Name == subBlocName);
                if (subBloc == null) continue;

                Event @event = subBloc.Events.FirstOrDefault(e => e.Name == eventName);
                if (@event == null) continue;

                var trialsFound = new List<Trial>();
                var trialsNotFound = new List<Trial>();

                foreach (var trial in orderedTrials)
                {
                    if (trial.SubTrialBySubBloc[subBloc].InformationsByEvent[@event].IsFound)
                        trialsFound.Add(trial);
                    else
                        trialsNotFound.Add(trial);
                }

                orderedTrials = command switch
                {
                    "LATENCY" => trialsFound.OrderBy(t => t.SubTrialBySubBloc[subBloc].InformationsByEvent[@event].Occurences.First().TimeFromMainEvent).ToList(),
                    "CODE" => trialsFound.OrderBy(t => t.SubTrialBySubBloc[subBloc].InformationsByEvent[@event].Occurences.First().Code).ToList(),
                    _ => trialsFound
                };

                orderedTrials.AddRange(trialsNotFound);
            }
            return orderedTrials.ToArray();
        }

        #endregion

        #region Structs
        public struct EventOccurences
        {
            #region Properties
            Dictionary<int, EventOccurence[]> m_OccurencesByCode;
            #endregion

            #region Constructors
            public EventOccurences(Dictionary<int, EventOccurence[]> occurencesByCode)
            {
                m_OccurencesByCode = occurencesByCode;
            }
            #endregion

            #region Public Methods
            public EventOccurence[] GetOccurences()
            {
                return m_OccurencesByCode.SelectMany((kv) => kv.Value).ToArray();
            }
            public EventOccurence[] GetOccurences(int code)
            {
                return m_OccurencesByCode[code];
            }
            public EventOccurence[] GetOccurences(int start, int end)
            {
                return m_OccurencesByCode.SelectMany((kv) => kv.Value.Where(o => o.Index >= start && o.Index <= end)).ToArray();
            }
            #endregion
        }
        #endregion
    }
} 