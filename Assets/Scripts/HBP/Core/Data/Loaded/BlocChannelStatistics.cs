using HBP.Data.Preferences;

namespace HBP.Core.Data
{
    public class BlocChannelStatistics
    {
        #region Properties
        public ChannelTrialStat Trial { get; set; }
        #endregion

        #region Constructors
        public BlocChannelStatistics(BlocChannelData data)
        {
            Trial = new ChannelTrialStat(data.Trials, PreferencesManager.UserPreferences.Data.EEG.Averaging);
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