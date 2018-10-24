using HBP.Data.Experience.Protocol;
using System.Collections.Generic;
using System.Linq;

namespace HBP.Data.Experience.Dataset
{
    public struct ChannelTrialStat
    {
        #region Properties
        public Dictionary<SubBloc, ChannelSubTrialStat> SiteSubTrialBySubBloc { get; set; }
        public int TotalNumberOfTrials { get; set; }
        public int NumberOfValidTrials { get; set; }
        public float[] AllValues
        {
            get
            {
                return SiteSubTrialBySubBloc.OrderBy(kv => kv.Key.Order).ThenBy(kv => kv.Key.Name).SelectMany(kv => kv.Value.Values).ToArray();
            }
        }
        #endregion

        #region Constructor
        public ChannelTrialStat(Dictionary<SubBloc, ChannelSubTrialStat> siteSubTrialBySubBloc, int totalNumberOfTrials, int numberOfValidTrials)
        {
            SiteSubTrialBySubBloc = siteSubTrialBySubBloc;
            TotalNumberOfTrials = totalNumberOfTrials;
            NumberOfValidTrials = numberOfValidTrials;
        }
        public ChannelTrialStat(ChannelTrial[] siteTrials, Enums.AveragingType averaging)
        {
            SiteSubTrialBySubBloc = new Dictionary<SubBloc, ChannelSubTrialStat>();
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
                SiteSubTrialBySubBloc.Add(subBloc, new ChannelSubTrialStat(siteSubTrials, isValid, averaging));
            }
        }
        #endregion
    }
}
