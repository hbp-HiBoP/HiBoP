﻿using System.Collections.Generic;

namespace HBP.Core.Data
{
    public class EventsStatistics
    {
        #region Properties
        public Dictionary<Bloc, BlocEventsStatistics> EventsStatisticsByBloc { get; set; }
        #endregion

        #region Constructors
        public EventsStatistics(DataInfo dataInfo)
        {
            EventsStatisticsByBloc = new Dictionary<Bloc, BlocEventsStatistics>();
            DataManager.GetData(dataInfo);
            foreach (var bloc in dataInfo.Dataset.Protocol.Blocs)
            {
                EventsStatisticsByBloc.Add(bloc,DataManager.GetEventsStatistics(dataInfo, bloc));
            }
        }
        #endregion

        #region Public Methods
        public void Clear()
        {
            foreach (var blocEventsStatistics in EventsStatisticsByBloc.Values)
            {
                blocEventsStatistics.Clear();
            }
            EventsStatisticsByBloc.Clear();
            EventsStatisticsByBloc = new Dictionary<Bloc, BlocEventsStatistics>();
        }
        #endregion
    }
}