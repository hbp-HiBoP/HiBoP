using System.Collections.Generic;
using System.Linq;
using HBP.Core.Enums;

namespace HBP.Core.Data
{
    public struct SubBlocEventsStatistics
    {
        #region Properties
        public Dictionary<Event, EventStatistics> StatisticsByEvent { get; set; }
        #endregion

        #region Constructors
        public SubBlocEventsStatistics(BlocData data, SubBloc subBloc, AveragingType averaging) : this(subBloc.Events.ToDictionary(e => e, e => data.Trials.Where(t => t.SubTrialBySubBloc[subBloc].Found).Select(t => t.SubTrialBySubBloc[subBloc].InformationsByEvent[e]).ToArray()), averaging)
        {
        }
        public SubBlocEventsStatistics(Dictionary<Event, EventInformation[]> informationsByEvent, AveragingType averaging)
        {
            StatisticsByEvent = informationsByEvent.ToDictionary(kv => kv.Key, kv => new EventStatistics(kv.Value, averaging));
        }
        #endregion

        #region Public Methods
        public void Clear()
        {
            StatisticsByEvent.Clear();
            StatisticsByEvent = new Dictionary<Event, EventStatistics>();
        }
        #endregion
    }
}