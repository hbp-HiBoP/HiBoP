using System.Linq;
using System.Collections.Generic;

namespace HBP.Core.Data
{
    public class ChannelTrial
    {
        #region Properties
        public Dictionary<SubBloc, ChannelSubTrial> ChannelSubTrialBySubBloc { get; set; }
        public bool IsValid { get; set; }
        public float[] Values
        {
            get
            {
                return ChannelSubTrialBySubBloc.OrderBy(kv => kv.Key.Order).SelectMany(kv => kv.Value.Values).ToArray();
            }
        }
        #endregion

        #region Constructor
        public ChannelTrial(Dictionary<SubBloc, ChannelSubTrial> siteSubTrialBySubBloc, bool isValid)
        {
            ChannelSubTrialBySubBloc = siteSubTrialBySubBloc;
            IsValid = isValid;
        }
        public ChannelTrial(Dictionary<SubBloc, SubTrial> subTrialBySubBloc, string channel, bool isValid)
        {
            ChannelSubTrialBySubBloc = subTrialBySubBloc.ToDictionary((pair) => pair.Key, (pair) => new ChannelSubTrial(pair.Value, channel));
            IsValid = isValid;
        }
        public ChannelTrial(Trial trial, string channel) : this(trial.SubTrialBySubBloc, channel, trial.IsValid)
        {
        }
        #endregion

        #region Public Methods
        public void Clear()
        {
            foreach (var channelSubTrial in ChannelSubTrialBySubBloc.Values)
            {
                channelSubTrial.Clear();
            }
            ChannelSubTrialBySubBloc.Clear();
            ChannelSubTrialBySubBloc = new Dictionary<SubBloc, ChannelSubTrial>();

            IsValid = false;
        }
        #endregion
    }
}