namespace HBP.Data.TrialMatrix
{
    public class SubTrial
    {
        #region Attributs
        public Core.Data.ChannelSubTrial Data { get; set; }
        #endregion

        #region Constructor
        public SubTrial(Core.Data.ChannelSubTrial data)
        {
            Data = data;
        }
        #endregion
    }
}