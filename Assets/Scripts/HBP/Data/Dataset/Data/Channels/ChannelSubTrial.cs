using HBP.Data.Experience.Protocol;
using System.Collections.Generic;

namespace HBP.Data.Experience.Dataset
{
    /// <summary>
    /// A Structure containing all the data about subTrial in a specific channel.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader>
    /// <term>Data</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term><b>InformationsByEvent</b></term>
    /// <description>Informations by event.</description>
    /// </item>
    /// <item>
    /// <term><b>Unit</b></term>
    /// <description>Unit of data contained in this channel.</description>
    /// </item>
    /// <item>
    /// <term><b>Values</b></term>
    /// <description>Values for this sub-trial contained in this channel.</description>
    /// </item>
    /// <item>
    /// <term><b>Found</b></term>
    /// <description>True if the sub-trial is found, False otherwise.</description>
    /// </item>
    /// </list>
    /// </remarks>
    public struct ChannelSubTrial
    {
        #region Properties
        /// <summary>
        /// Informations by event.
        /// </summary>
        public Dictionary<Event, EventInformation> InformationsByEvent { get; set; }
        /// <summary>
        /// Unit of data contained in this channel.
        /// </summary>
        public string Unit { get; set; }
        /// <summary>
        /// Values for this sub-trial contained in this channel.
        /// </summary>
        public float[] Values { get; set; }
        /// <summary>
        /// True if the sub-trial is found, False otherwise.
        /// </summary>
        public bool Found { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new ChannelSubTrial instance.
        /// </summary>
        /// <param name="values">Values for this sub-trial contained in this channel</param>
        /// <param name="unit">Unit of data contained in this channel</param>
        /// <param name="found">True if the sub-trial is found, False otherwise</param>
        /// <param name="informationsByEvent">Informations by event</param>
        public ChannelSubTrial(float[] values, string unit, bool found, Dictionary<Event, EventInformation> informationsByEvent)
        {
            Values = values;
            Found = found;
            Unit = unit;
            InformationsByEvent = informationsByEvent;
        }
        /// <summary>
        /// Create a new ChannelSubTrial instance.
        /// </summary>
        /// <param name="subTrial">Data related subTrial</param>
        /// <param name="channel">Data related subTrial</param>
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

        #region Public Methods
        /// <summary>
        /// Clear all the data in this ChannelSubTrial instance. Called by the Data manager.
        /// </summary>
        public void Clear()
        {
            foreach (var eventInformation in InformationsByEvent.Values)
            {
                eventInformation.Clear();
            }
            InformationsByEvent.Clear();
            InformationsByEvent = new Dictionary<Event, EventInformation>();

            Unit = "";
            Values = new float[0];
            Found = false;
        }
        #endregion
    }
}