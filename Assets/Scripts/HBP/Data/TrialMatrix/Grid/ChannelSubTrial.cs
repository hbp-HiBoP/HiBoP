using d = HBP.Data.Experience.Dataset;

namespace HBP.Data.TrialMatrix.Grid
{
    public class ChannelSubTrial
    {
        #region Attributs
        public d.ChannelSubTrial Data { get; set; }
        #endregion

        #region Constructor
        public ChannelSubTrial(d.ChannelSubTrial data)
        {
            Data = data;
        }
        #endregion
    }
}