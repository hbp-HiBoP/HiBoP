using HBP.Data.Experience.Dataset;
using HBP.Data.Experience.Protocol;
using System.Collections.Generic;
using System.Linq;

namespace HBP.Data.Visualization
{
    public struct EventData
    {
        #region Properties
        public Dictionary<Experience.Protocol.Event, EventInformation[]> InformationByEvent { get; set; }
        public Dictionary<Experience.Protocol.Event, EventInformationStat> InformationStatByEvent { get; set; }
        #endregion

        #region Constructors
        public EventData(SubTrial[] subTrials, SubBloc subBloc)
        {
            InformationByEvent = new Dictionary<Experience.Protocol.Event, EventInformation[]>();
            foreach (var e in subBloc.Events)
            {
                InformationByEvent.Add(e, subTrials.Select(st => st.InformationsByEvent[e]).ToArray());
            }

            InformationStatByEvent = new Dictionary<Experience.Protocol.Event, EventInformationStat>();
            foreach (var e in subBloc.Events)
            {
                InformationStatByEvent.Add(e, new EventInformationStat(InformationByEvent[e], ApplicationState.UserPreferences.Data.Protocol.PositionAveraging));
            }
        }
        public EventData(Dictionary<Experience.Protocol.Event, EventInformation[]> informationByEvent)
        {
            InformationByEvent = informationByEvent;
            InformationStatByEvent = new Dictionary<Experience.Protocol.Event, EventInformationStat>();
            foreach (var e in InformationByEvent.Keys)
            {
                InformationStatByEvent.Add(e, new EventInformationStat(InformationByEvent[e], ApplicationState.UserPreferences.Data.Protocol.PositionAveraging));
            }
        }
        #endregion
    }
}