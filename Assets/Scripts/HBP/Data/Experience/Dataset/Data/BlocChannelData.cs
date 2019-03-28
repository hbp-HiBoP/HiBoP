using System.Linq;

namespace HBP.Data.Experience.Dataset
{
    public class BlocChannelData
    {
        #region Properties
        public ChannelTrial[] Trials { get; set; }
        #endregion

        #region Constructors
        public BlocChannelData(BlocData data, string channel) : this(data.Trials.Select(trial => new ChannelTrial(trial, channel)).ToArray())
        {
        }
        public BlocChannelData(ChannelTrial[] trials)
        {
            Trials = trials;
        }
        #endregion

        #region Public Methods
        public void Clear()
        {
            foreach (var channelTrial in Trials)
            {
                channelTrial.Clear();
            }
            Trials = new ChannelTrial[0];
        }
        #endregion
    }
}