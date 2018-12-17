using HBP.Data.Experience.Protocol;
using System.Collections.Generic;

namespace HBP.Data.Experience.Dataset
{
    public struct ChannelSubTrial
    {
        #region Properties
        public Dictionary<Event, EventInformation> InformationsByEvent { get; set; }
        public string Unit { get; set; }
        public float[] Values { get; set; }
        public bool Found { get; set; }
        #endregion

        #region Constructors
        public ChannelSubTrial(float[] values, string unit, bool found, Dictionary<Event, EventInformation> informationsByEvent)
        {
            Values = values;
            Found = found;
            Unit = unit;
            InformationsByEvent = informationsByEvent;
        }
        public ChannelSubTrial(SubTrial subTrial, string channel)
        {
            Found = subTrial.Found;
            if (Found)
            {
                Values = subTrial.ValuesByChannel[channel];
                Unit= subTrial.UnitByChannel[channel];
            }
            else
            {
                Values = new float[0];
                Unit = "";
            }
            InformationsByEvent = subTrial.InformationsByEvent;

        }
        #endregion
    }
}