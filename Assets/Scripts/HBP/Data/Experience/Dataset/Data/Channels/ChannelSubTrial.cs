using HBP.Data.Experience.Protocol;
using System.Collections.Generic;

namespace HBP.Data.Experience.Dataset
{
    public struct ChannelSubTrial
    {
        #region Properties
        public Dictionary<Event, EventInformation> InformationsByEvent { get; set; }
        public float[] Values { get; set; }
        public bool Found { get; set; }
        #endregion

        #region Constructors
        public ChannelSubTrial(float[] values, bool found, Dictionary<Event, EventInformation> informationsByEvent)
        {
            Values = values;
            Found = found;
            InformationsByEvent = informationsByEvent;
        }
        public ChannelSubTrial(SubTrial subTrial, string channel)
        {
            Values = subTrial.ValuesByChannel[channel];
            Found = subTrial.Found;
            InformationsByEvent = subTrial.InformationsByEvent;
        }
        #endregion
    }
}