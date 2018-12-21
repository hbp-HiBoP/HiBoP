using HBP.Data.Informations;
using System.Linq;

namespace HBP.Data.TrialMatrix.Grid
{
    public class TrialMatrixGrid
    {
        #region Properties
        public ChannelStruct[] ChannelStructs { get; private set; }
        public DataStruct[] DataStructs { get; private set; }
        public Data[] Data { get; private set; }
        #endregion

        #region Constructors
        public TrialMatrixGrid(ChannelStruct[] channelStructs, DataStruct[] dataStructs)
        {
            ChannelStructs = channelStructs;
            DataStructs = dataStructs;
            Data = dataStructs.Select(data => new Data(data, channelStructs)).ToArray();
        }
        #endregion
    }
}