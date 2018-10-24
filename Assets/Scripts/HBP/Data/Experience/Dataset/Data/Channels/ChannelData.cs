using System.Linq;
using System.Collections.Generic;

namespace HBP.Data.Experience.Dataset
{
    public class ChannelData
    {
        #region Properties
        public string Unit { get; set; }
        public Dictionary<Protocol.Bloc, BlocChannelData> DataByBloc { get; set; }
        #endregion

        #region Constructors
        public ChannelData(Data data, string channel) : this(data.DataByBloc.ToDictionary(kv => kv.Key,kv => new BlocChannelData(kv.Value,channel)),data.UnitByChannel[channel])
        {

        }
        public ChannelData(Dictionary<Protocol.Bloc, BlocChannelData> dataByBloc, string unit)
        {
            DataByBloc = dataByBloc;
            Unit = unit;
        }
        #endregion
    }
}