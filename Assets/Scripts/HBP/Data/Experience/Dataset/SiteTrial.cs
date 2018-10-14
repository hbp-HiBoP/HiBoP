using System.Linq;
using System.Collections.Generic;
using HBP.Data.Experience.Protocol;

namespace HBP.Data.Experience.Dataset
{
    public struct SiteTrial
    {
        #region Properties
        public Dictionary<SubBloc, SiteSubTrial> SiteSubTrialBySubBloc { get; set; }
        #endregion

        #region Constructor
        public SiteTrial(Dictionary<SubBloc, SiteSubTrial> siteSubTrialBySubBloc)
        {
            SiteSubTrialBySubBloc = siteSubTrialBySubBloc;
        }
        public SiteTrial(Dictionary<SubBloc, SubTrial> subTrialBySubBloc, string site)
        {
            SiteSubTrialBySubBloc = subTrialBySubBloc.ToDictionary((kv) => kv.Key, (kv) => new SiteSubTrial(kv.Value,site));
        }
        public SiteTrial(Trial trial, string site): this(trial.SubTrialBySubBloc,site)
        {
        }
        #endregion
    }
}