using UnityEngine;
using data = HBP.Data.TrialMatrix.Grid;
using HBP.Data.Informations;

namespace HBP.UI.TrialMatrix.Grid
{
    public class TrialMatrixGrid : MonoBehaviour
    {
        #region Properties
        [SerializeField] Texture2D m_Colormap;

        [SerializeField] RectTransform m_DataContainer;
        [SerializeField] GameObject m_DataPrefab;

        [SerializeField] RectTransform m_ChannelHeaderContainer;
        [SerializeField] GameObject m_ChannelHeaderPrefab;

        Data[] m_Data;
        data.TrialMatrixGrid m_TrialMatrixGridData;
        #endregion

        #region Public Methods
        public void Display(data.TrialMatrixGrid trialMatrixGridData)
        {
            m_TrialMatrixGridData = trialMatrixGridData;
            DisplayChannels(trialMatrixGridData.ChannelStructs);

            foreach (Transform child in m_DataContainer)
            {
                Destroy(child.gameObject);
            }
            m_Data = new Data[0];

            foreach (var data in trialMatrixGridData.Data) AddData(data);
        }
        #endregion

        #region Private Methods
        void DisplayChannels(ChannelStruct[] channels)
        {
            foreach(Transform child in m_ChannelHeaderContainer)
            {
                Destroy(child.gameObject);
            }
            foreach (var channel in channels)
            {
                ChannelHeader header = Instantiate(m_ChannelHeaderPrefab, m_ChannelHeaderContainer).GetComponent<ChannelHeader>();
                header.Channel = channel;
            }
        }
        void AddData(data.Data d)
        {
            Data data = Instantiate(m_DataPrefab, m_DataContainer).GetComponent<Data>();
            data.Set(d, m_Colormap);
        }
        #endregion
    }
}