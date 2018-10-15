using HBP.Data.Experience.Dataset;
using HBP.Data.Experience.Protocol;
using System.Collections.Generic;
using System.Linq;

namespace HBP.Data.Visualization
{
    public struct SiteTrialStat
    {
        #region Properties
        public Dictionary<SubBloc, SiteSubTrialStat> SiteSubTrialBySubBloc { get; set; }
        public int TotalNumberOfTrials { get; set; }
        public int NumberOfValidTrials { get; set; }
        #endregion

        #region Constructor
        public SiteTrialStat(Dictionary<SubBloc, SiteSubTrialStat> siteSubTrialBySubBloc, int totalNumberOfTrials, int numberOfValidTrials)
        {
            SiteSubTrialBySubBloc = siteSubTrialBySubBloc;
            TotalNumberOfTrials = totalNumberOfTrials;
            NumberOfValidTrials = numberOfValidTrials;
        }
        public SiteTrialStat(SiteTrial[] siteTrials, Enums.AveragingType averaging)
        {
            SiteSubTrialBySubBloc = new Dictionary<SubBloc, SiteSubTrialStat>();
            TotalNumberOfTrials = siteTrials.Length;
            NumberOfValidTrials = siteTrials.Count(s => s.IsValid);

            foreach (var subBloc in siteTrials[0].SiteSubTrialBySubBloc.Keys)
            {
                SiteSubTrial[] siteSubTrials = new SiteSubTrial[TotalNumberOfTrials];
                bool[] isValid = new bool[TotalNumberOfTrials];
                for (int i = 0; i < TotalNumberOfTrials; i++)
                {
                    siteSubTrials[i] = siteTrials[i].SiteSubTrialBySubBloc[subBloc];
                    isValid[i] = siteTrials[i].IsValid;
                }
                SiteSubTrialBySubBloc.Add(subBloc, new SiteSubTrialStat(siteSubTrials, isValid, averaging));
            }
        }
        #endregion
    }
}
