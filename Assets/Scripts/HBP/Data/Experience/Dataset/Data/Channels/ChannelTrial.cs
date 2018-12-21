using System.Linq;
using System.Collections.Generic;
using HBP.Data.Experience.Protocol;

namespace HBP.Data.Experience.Dataset
{
    public class ChannelTrial
    {
        #region Properties
        public Dictionary<SubBloc, ChannelSubTrial> ChannelSubTrialBySubBloc { get; set; }
        public bool IsValid { get; set; }
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
    }
}