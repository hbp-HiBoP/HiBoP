using System.Collections.Generic;
using System.Linq;

namespace HBP.Data.Experience.Dataset
{
    public class BlocChannelData
    {
        #region Properties
        public ChannelTrial[] Trials { get; set; }
        #endregion

        #region Constructors
        public BlocChannelData(BlocData data, string channel) : this(data.Trials.Select(trial => new ChannelTrial(trial, channel)).ToArray())
        {
        }
        public BlocChannelData(ChannelTrial[] trials)
        {
            Trials = trials;
        }
        #endregion

        #region Public Methods
        public void Clear()
        {
            foreach (var channelTrial in Trials)
            {
                channelTrial.Clear();
            }
            Trials = new ChannelTrial[0];
        }
        #endregion

        #region Structs
        public struct EventOccurences
        {
            #region Properties
            Dictionary<int, EventOccurence[]> m_OccurencesByCode;
            #endregion

            #region Constructors
            public EventOccurences(Dictionary<int, EventOccurence[]> occurencesByCode)
            {
                m_OccurencesByCode = occurencesByCode;
            }
            #endregion

            #region Public Methods
            public EventOccurence[] GetOccurences()
            {
                return m_OccurencesByCode.SelectMany((kv) => kv.Value).ToArray();
            }
            public EventOccurence[] GetOccurences(int code)
            {
                return m_OccurencesByCode[code];
            }
            public EventOccurence[] GetOccurences(int start, int end)
            {
                return m_OccurencesByCode.SelectMany((kv) => kv.Value.Where(o => o.Index >= start && o.Index <= end)).ToArray();
            }
            #endregion
        }
        #endregion
    }
}