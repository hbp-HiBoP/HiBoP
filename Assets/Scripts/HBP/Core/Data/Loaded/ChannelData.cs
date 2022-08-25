using System.Linq;
using System.Collections.Generic;

namespace HBP.Core.Data
{
    /// <summary>
    /// A class containing all the data about a channel.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader>
    /// <term>Data</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term><b>Unit</b></term>
    /// <description>Unit of data contained in this channel.</description>
    /// </item>
    /// <item>
    /// <term><b>DataByBloc</b></term>
    /// <description>Data contained in this channel by bloc.</description>
    /// </item>
    /// </list>
    /// </remarks>
    public class ChannelData
    {
        #region Properties
        /// <summary>
        /// Unit of data contained in this channel.
        /// </summary>
        public string Unit { get; set; }
        /// <summary>
        /// Data contained in this channel by bloc.
        /// </summary>
        public Dictionary<Bloc, BlocChannelData> DataByBloc { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new ChannelData instance.
        /// </summary>
        /// <param name="data">Data contained in this channel</param>
        /// <param name="channel">Name of the Channel</param>
        public ChannelData(EpochedData data, string channel) : this(data.DataByBloc.ToDictionary(kv => kv.Key,kv => new BlocChannelData(kv.Value,channel)),data.UnitByChannel[channel])
        {

        }
        /// <summary>
        /// Create new ChannelData instance.
        /// </summary>
        /// <param name="dataByBloc">Data contained in this channel by bloc</param>
        /// <param name="unit">Unit of data contained in this channel</param>
        public ChannelData(Dictionary<Bloc, BlocChannelData> dataByBloc, string unit)
        {
            DataByBloc = dataByBloc;
            Unit = unit;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Clear all the data in this ChannelData instance. Called by the Data manager.
        /// </summary>
        public void Clear()
        {
            Unit = "";
            foreach (var blocChannelData in DataByBloc.Values)
            {
                blocChannelData.Clear();
            }
            DataByBloc.Clear();
        }
        #endregion
    }
}