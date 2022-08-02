using System.Collections.Generic;
using System.Linq;

namespace HBP.Core.Data
{
    public struct SubBlocEventsStatistics
    {
        #region Properties
        public Dictionary<Event, EventStatistics> StatisticsByEvent { get; set; }
        #endregion

        #region Constructors
        public SubBlocEventsStatistics(BlocData data, SubBloc subBloc) : this(subBloc.Events.ToDictionary(e => e, e => data.Trials.Where(t => t.SubTrialBySubBloc[subBloc].Found).Select(t => t.SubTrialBySubBloc[subBloc].InformationsByEvent[e]).ToArray()))
        {
        }
        public SubBlocEventsStatistics(Dictionary<Event, EventInformation[]> informationsByEvent)
        {
            StatisticsByEvent = informationsByEvent.ToDictionary(kv => kv.Key, kv => new EventStatistics(kv.Value, ApplicationState.UserPreferences.Data.Protocol.PositionAveraging));
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