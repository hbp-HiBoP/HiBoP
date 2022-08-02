using System.Collections.Generic;
using System.Linq;

namespace HBP.Core.Data
{
    public class ChannelStatistics
    {
        #region Properties
        Dictionary<Bloc, BlocChannelStatistics> StatisticsByBloc { get; set; }
        #endregion

        #region Constructors
        public ChannelStatistics(ChannelData data)
        {
            data.DataByBloc.ToDictionary(kv => kv.Key, kv => new BlocChannelStatistics(kv.Value));
        }
        #endregion

        #region Public Methods
        public void Clear()
        {
            foreach (var blocChannelStatistics in StatisticsByBloc.Values)
            {
                blocChannelStatistics.Clear();
            }
            StatisticsByBloc.Clear();
            StatisticsByBloc = new Dictionary<Bloc, BlocChannelStatistics>();
        }
        #endregion
    }
}