using System.Collections.Generic;
using System.Linq;

namespace HBP.Core.Data 
{
    public struct ChannelTrialStat
    {
        #region Properties
        public Dictionary<SubBloc, ChannelSubTrialStat> ChannelSubTrialBySubBloc { get; set; }
        public int TotalNumberOfTrials { get; set; }
        public int NumberOfValidTrials { get; set; }
        public float[] AllValues
        {
            get
            {
                return ChannelSubTrialBySubBloc.OrderBy(kv => kv.Key.Order).ThenBy(kv => kv.Key.Name).SelectMany(kv => kv.Value.Values).ToArray();
            }
        }
        #endregion

        #region Constructor
        public ChannelTrialStat(Dictionary<SubBloc, ChannelSubTrialStat> siteSubTrialBySubBloc, int totalNumberOfTrials, int numberOfValidTrials)
        {
            ChannelSubTrialBySubBloc = siteSubTrialBySubBloc;
            TotalNumberOfTrials = totalNumberOfTrials;
            NumberOfValidTrials = numberOfValidTrials;
        }
        public ChannelTrialStat(ChannelTrial[] siteTrials, Enums.AveragingType averaging)
        {
            ChannelSubTrialBySubBloc = new Dictionary<SubBloc, ChannelSubTrialStat>();
            TotalNumberOfTrials = siteTrials.Length;
            NumberOfValidTrials = siteTrials.Count(s => s.IsValid);

            foreach (var subBloc in siteTrials[0].ChannelSubTrialBySubBloc.Keys)
            {
                ChannelSubTrial[] siteSubTrials = new ChannelSubTrial[TotalNumberOfTrials];
                bool[] isValid = new bool[TotalNumberOfTrials];
                for (int i = 0; i < TotalNumberOfTrials; i++)
                {
                    siteSubTrials[i] = siteTrials[i].ChannelSubTrialBySubBloc[subBloc];
                    isValid[i] = siteTrials[i].IsValid;
                }
                ChannelSubTrialBySubBloc.Add(subBloc, new ChannelSubTrialStat(siteSubTrials, isValid, averaging));
            }
        }
        #endregion

        #region Public Methods
        public void Clear()
        {
            foreach (var channelSubTrialStat in ChannelSubTrialBySubBloc.Values)
            {
                channelSubTrialStat.Clear();
            }
            ChannelSubTrialBySubBloc.Clear();
            ChannelSubTrialBySubBloc = new Dictionary<SubBloc, ChannelSubTrialStat>();

            TotalNumberOfTrials = 0;
            NumberOfValidTrials = 0;
        }
        #endregion
    }
}
