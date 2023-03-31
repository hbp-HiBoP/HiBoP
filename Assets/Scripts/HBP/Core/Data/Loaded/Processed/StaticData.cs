using System;
using System.Collections.Generic;

namespace HBP.Core.Data.Processed
{
    public class StaticData
    {
        #region Properties
        public Dictionary<string, Dictionary<string, float>> ValueByChannelIDByLabel { get; set; } = new Dictionary<string, Dictionary<string, float>>();
        #endregion

        #region Public Methods
        public void Load(IEnumerable<StaticDataInfo> columnData)
        {
            foreach (StaticDataInfo dataInfo in columnData)
            {
                Core.Data.StaticData data = DataManager.GetData(dataInfo) as Core.Data.StaticData;
                for (int i = 0; i < data.Labels.Length; ++i)
                {
                    if (!ValueByChannelIDByLabel.ContainsKey(data.Labels[i])) ValueByChannelIDByLabel.Add(data.Labels[i], new Dictionary<string, float>());
                    var values = ValueByChannelIDByLabel[data.Labels[i]];
                    foreach (var valuesByChannel in data.ValuesByChannel)
                    {
                        string channelID = dataInfo.Patient.ID + "_" + valuesByChannel.Key;
                        if (!values.ContainsKey(channelID)) values.Add(channelID, valuesByChannel.Value[i]);
                    }
                }
            }
        }
        public void Unload()
        {
            ValueByChannelIDByLabel.Clear();
        }
        #endregion
    }
}

