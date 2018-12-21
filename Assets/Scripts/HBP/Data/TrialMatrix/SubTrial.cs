using HBP.Data.Experience.Dataset;

namespace HBP.Data.TrialMatrix
{
    public class SubTrial
    {
        #region Attributs
        public ChannelSubTrial Data { get; set; }
        #endregion

        #region Constructor
        public SubTrial(ChannelSubTrial data)
        {
            Data = data;
        }
        #endregion
    }
}