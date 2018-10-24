using HBP.Data.Experience.Protocol;
using System.Collections.Generic;
using System.Linq;

namespace HBP.Data.Experience.Dataset
{
    public class BlocEventsStatistics
    {
        #region Properties
        Dictionary<SubBloc, SubBlocEventsStatistics> EventsStatisticsBySubBloc { get; set; }
        #endregion

        #region Constructors
        public BlocEventsStatistics(DataInfo dataInfo, Bloc bloc)
        {
            BlocData blocData = DataManager.GetData(dataInfo, bloc);
            EventsStatisticsBySubBloc = bloc.SubBlocs.ToDictionary(s => s, s => new SubBlocEventsStatistics(blocData, s));
        }
        #endregion
    }
}
