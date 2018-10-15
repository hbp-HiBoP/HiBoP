using System.Linq;
using System.Collections.Generic;
using HBP.Data.Experience.Protocol;

namespace HBP.Data.Experience.Dataset
{
    public struct SiteTrial
    {
        #region Properties
        public Dictionary<SubBloc, SiteSubTrial> SiteSubTrialBySubBloc { get; set; }
        public bool IsValid { get; set; }
        #endregion

        #region Constructor
        public SiteTrial(Dictionary<SubBloc, SiteSubTrial> siteSubTrialBySubBloc, bool isValid)
        {
            SiteSubTrialBySubBloc = siteSubTrialBySubBloc;
            IsValid = isValid;
        }
        public SiteTrial(Dictionary<SubBloc, SubTrial> subTrialBySubBloc, string site, bool isValid)
        {
            SiteSubTrialBySubBloc = subTrialBySubBloc.ToDictionary((kv) => kv.Key, (kv) => new SiteSubTrial(kv.Value, site));
            IsValid = isValid;
        }
        public SiteTrial(Trial trial, string site) : this(trial.SubTrialBySubBloc, site, trial.IsValid)
        {
        }
        #endregion
    }
}