using System.Collections.Generic;
using System.Linq;

namespace HBP.Data.Experience.Dataset
{
    public class ChannelStatistics
    {
        #region Properties
        Dictionary<Protocol.Bloc, BlocChannelStatistics> StatisticsByBloc { get; set; }
        #endregion

        #region Constructors
        public ChannelStatistics(ChannelData data)
        {
            data.DataByBloc.ToDictionary(kv => kv.Key, kv => new BlocChannelStatistics(kv.Value));
        }
        #endregion
    }
}