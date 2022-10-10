using HBP.Core.Enums;

namespace HBP.Core.Data
{
    public class BlocChannelStatistics
    {
        #region Properties
        public ChannelTrialStat Trial { get; set; }
        #endregion

        #region Constructors
        public BlocChannelStatistics(BlocChannelData data, AveragingType averaging)
        {
            Trial = new ChannelTrialStat(data.Trials, averaging);
        }
        #endregion

        #region Public Methods
        public void Clear()
        {
            Trial.Clear();
            Trial = new ChannelTrialStat();
        }
        #endregion
    }
}