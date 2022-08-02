using System.Collections.Generic;
using System.Linq;

namespace HBP.Core.Data
{
    public class BlocEventsStatistics
    {
        #region Properties
        public Dictionary<SubBloc, SubBlocEventsStatistics> EventsStatisticsBySubBloc { get; set; }
        #endregion

        #region Constructors
        public BlocEventsStatistics(DataInfo dataInfo, Bloc bloc)
        {
            BlocData blocData = DataManager.GetData(dataInfo, bloc);
            EventsStatisticsBySubBloc = bloc.SubBlocs.ToDictionary(s => s, s => new SubBlocEventsStatistics(blocData, s));
        }
        #endregion

        #region Public Methods
        public void Clear()
        {
            foreach (var subBlocEventsStatistics in EventsStatisticsBySubBloc.Values)
            {
                subBlocEventsStatistics.Clear();
            }
            EventsStatisticsBySubBloc.Clear();
            EventsStatisticsBySubBloc = new Dictionary<SubBloc, SubBlocEventsStatistics>();
        }
        #endregion
    }
}
