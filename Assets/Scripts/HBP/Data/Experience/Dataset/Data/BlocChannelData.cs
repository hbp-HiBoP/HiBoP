using System.Linq;

namespace HBP.Data.Experience.Dataset
{
    public class BlocChannelData
    {
        #region Properties
        public ChannelTrial[] Trials { get; set; }
        #endregion

        #region constructors
        public BlocChannelData(BlocData data, string channel) : this(data.Trials.Select(trial => new ChannelTrial(trial, channel)).ToArray())
        {
        }
        public BlocChannelData(ChannelTrial[] trials)
        {
            Trials = trials;
        }
        #endregion
    }
}