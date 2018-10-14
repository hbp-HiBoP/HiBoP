using HBP.Data.Experience.Dataset;
using System.Collections.Generic;
using System.Linq;

namespace HBP.Data.Visualization
{
    public class EventData
    {
        #region Properties
        public Dictionary<Event, EventInformation> InformationsByEvent { get; set; }
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