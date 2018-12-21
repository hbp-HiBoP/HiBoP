using System.Collections.Generic;
using System.Linq;
using HBP.Data.Localizer;

namespace HBP.Data.Experience.Dataset
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
        public Trial[] Trials { get; set; }
        #endregion

        #region Constructors
        public BlocData(RawData data, Protocol.Bloc bloc) : this(data.ValuesByChannel, data.UnitByChannel, data.POS, data.Frequency, bloc) { }
        public BlocData(Dictionary<string,float[]> valuesByChannel, Dictionary<string, string> unitByChannel, POS pos, Frequency frequency, Protocol.Bloc bloc)
        {
            // Find all occurences for each event.
            Dictionary<Protocol.Event,EventOccurences> occurencesByEvent = bloc.SubBlocs.SelectMany((s) => s.Events).ToDictionary((e) => e, (e) => new EventOccurences(e.Codes.ToDictionary((c) => c, (c) => pos.GetOccurences(c).ToArray())));

            // Get all occurences for the mainEvent of the mainSubBloc.
            POS.Occurence[] MainSubBlocMainEventOccurences = occurencesByEvent[bloc.MainSubBloc.MainEvent].GetOccurences();

            // Initialize loop.
            List<Trial> trials = new List<Trial>(MainSubBlocMainEventOccurences.Length);
            int startIndex, endIndex;

            // All main event position but the last one.
            for (int i = 0; i < MainSubBlocMainEventOccurences.Length; i++)
            {
                startIndex = (i - 1 < 0) ? 0 : MainSubBlocMainEventOccurences[i - 1].Index;
                endIndex = (i + 1 >= MainSubBlocMainEventOccurences.Length) ? int.MaxValue : MainSubBlocMainEventOccurences[i + 1].Index;
                trials.Add(new Trial(valuesByChannel, unitByChannel, startIndex, MainSubBlocMainEventOccurences[i] , endIndex, occurencesByEvent, bloc, frequency));
            }
            Trials = trials.ToArray();
        }
        #endregion

        #region Structs
        public struct EventOccurences
        {
            #region Properties
            Dictionary<int, POS.Occurence[]> m_OccurencesByCode;
            #endregion

            #region Constructors
            public EventOccurences(Dictionary<int, POS.Occurence[]> occurencesByCode)
            {
                m_OccurencesByCode = occurencesByCode;
            }
            #endregion

            #region Public Methods
            public POS.Occurence[] GetOccurences()
            {
                return m_OccurencesByCode.SelectMany((kv) => kv.Value).ToArray();
            }
            public POS.Occurence[] GetOccurences(int code)
            {
                return m_OccurencesByCode[code];
            }
            public POS.Occurence[] GetOccurences(int start, int end)
            {
                return m_OccurencesByCode.SelectMany((kv) => kv.Value.Where(o => o.Index >= start && o.Index <= end)).ToArray();
            }
            #endregion
        }
        #endregion
    }
} 