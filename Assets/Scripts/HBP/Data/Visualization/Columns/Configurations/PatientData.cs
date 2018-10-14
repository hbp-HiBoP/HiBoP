using HBP.Data.Experience.Dataset;
using HBP.Data.Experience.Protocol;
using HBP.Data.Localizer;
using System.Collections.Generic;
using System.Linq;

namespace HBP.Data.Visualization
{
    public class PatientData
    {
        #region Properties
        public Frequency Frequency { get; set; }
        public Dictionary<SubBloc, EventData> EventDataBySubBloc { get; set; }
        #endregion

        #region constructors
        public EventData(EpochedData epochedData, string site)
        {
            epochedData.
            Trials = epochedData.Trials.Select(t => new SiteTrial(t, site)).ToArray();
        }
        public EventData(SiteTrial[] trials, string unit)
        {
            Trials = trials;
            Unit = unit;
        }
        public EventData() : this(new SiteTrial[0], "")
        {

        }
        #endregion
    }
}