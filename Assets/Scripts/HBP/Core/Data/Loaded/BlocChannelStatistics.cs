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

        internal long ManagedBytes
        {
            get
            {
                long bytes = 0;
                if (Trial.ChannelSubTrialBySubBloc == null)
                    return bytes;
                foreach (ChannelSubTrialStat statistics in Trial.ChannelSubTrialBySubBloc.Values)
                {
                    bytes += statistics.Values?.LongLength * sizeof(float) ?? 0;
                    bytes += statistics.SEM?.LongLength * sizeof(float) ?? 0;
                }
                return bytes;
            }
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
