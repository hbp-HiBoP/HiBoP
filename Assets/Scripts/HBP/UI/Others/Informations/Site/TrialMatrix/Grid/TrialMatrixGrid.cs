using HBP.Data.TrialMatrix.Grid;
using d = HBP.Data.TrialMatrix;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace HBP.UI.TrialMatrix.Grid
{
    public class TrialMatrixGrid : MonoBehaviour
    {
        #region Properties
        ChannelStruct[] m_Channels;
        DataStruct[] m_Data;

        [SerializeField] Texture2D m_Colormap;
        [SerializeField] ChannelHeaderDisplayer m_ChannelHeaderDisplayer;
        [SerializeField] DataDisplayer m_DataDisplayer;
        Dictionary<DataStruct, Dictionary<ChannelStruct, d.TrialMatrix>> TrialMatrixByChannelAndData = new Dictionary<DataStruct, Dictionary<ChannelStruct, d.TrialMatrix>>();
        #endregion

        #region Public Methods
        public void Display(ChannelStruct[] channels, DataStruct[] data)
        {
            GenerateTrialMatrices(channels, data);
            DisplayChannels(channels);
            DisplayData(TrialMatrixByChannelAndData);
        }
        #endregion

        #region Private Methods
        void GenerateTrialMatrices(ChannelStruct[] channels, DataStruct[] dataStructs)
        {
            foreach (var data in dataStructs)
            {
                TrialMatrixByChannelAndData.Add(data, new Dictionary<ChannelStruct, d.TrialMatrix>());
                foreach (ChannelStruct channel in channels)
                {
                    HBP.Data.Experience.Dataset.DataInfo dataInfo = data.Dataset.Data.FirstOrDefault(d => d.Patient == channel.Patient && d.Name == data.Data);
                    if (dataInfo != null)
                    {
                        Elan.ElanFile elanFile = new Elan.ElanFile(dataInfo.EEG, false);
                        if(elanFile.Channels.Any(c => c.Label == channel.Channel))
                        {
                            Debug.Log("Request -> " + dataInfo.Name + " " + channel.Channel);
                            d.TrialMatrix trialMatrixData = new d.TrialMatrix(dataInfo, channel.Channel);
                            TrialMatrixByChannelAndData[data].Add(channel, trialMatrixData);
                        }
                        else
                        {
                            TrialMatrixByChannelAndData[data].Add(channel, null);
                        }
                    }
                    else
                    {
                        TrialMatrixByChannelAndData[data].Add(channel, null);
                    }
                }
            }
        }
        void DisplayChannels(ChannelStruct[] channels)
        {
            m_ChannelHeaderDisplayer.Set(channels);
        }
        void DisplayData(Dictionary<DataStruct, Dictionary<ChannelStruct, d.TrialMatrix>> trialMatrixByChannelAndData)
        {
            m_DataDisplayer.Set(trialMatrixByChannelAndData, m_Colormap);
        }
        #endregion
    }
}