using UnityEngine;
using data = HBP.Display.Informations.TrialMatrix.Grid;
using HBP.Display.Informations;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace HBP.UI.Module3D.Informations.TrialMatrix.Grid
{
    public class TrialMatrixGrid : MonoBehaviour
    {
        #region Properties
        Color[] m_Colors;
        private Color[] Colors
        {
            get
            {
                return m_Colors;
            }
            set
            {
                m_Colors = value;
                foreach (var data in m_Data)
                {
                    data.Colors = value;
                }
            }
        }
        [SerializeField] Texture2D m_Colormap;
        public Texture2D Colormap
        {
            get
            {
                return m_Colormap;
            }
            set
            {
                m_Colormap = value;
                foreach (var data in m_Data)
                {
                    data.Colormap = value;
                }
                Colors = ExtractColormap(value);
            }
        } 

        [SerializeField] RectTransform m_DataContainer;
        [SerializeField] GameObject m_DataPrefab;

        [SerializeField] RectTransform m_ChannelHeaderContainer;
        [SerializeField] GameObject m_ChannelHeaderPrefab;

        List<Data> m_Data = new List<Data>();
        public ReadOnlyCollection<Data> Data
        {
            get
            {
                return new ReadOnlyCollection<Data>(m_Data);
            }
        }

        data.TrialMatrixGrid m_TrialMatrixGridData;
        #endregion

        #region Public Methods
        public void Display(data.TrialMatrixGrid trialMatrixGridData, Texture2D colormap = null)
        {
            Clear();
            m_TrialMatrixGridData = trialMatrixGridData;
            DisplayChannels(trialMatrixGridData.Channels);
            if(colormap != null) Colormap = colormap;
            foreach (var data in trialMatrixGridData.Data) AddData(data);
        }
        #endregion

        #region Private Methods
        void DisplayChannels(ChannelStruct[] channels)
        {
            foreach (var channel in channels)
            {
                ChannelHeader header = Instantiate(m_ChannelHeaderPrefab, m_ChannelHeaderContainer).GetComponent<ChannelHeader>();
                header.Channel = channel;
            }
        }
        void AddData(data.Data d)
        {
            Data data = Instantiate(m_DataPrefab, m_DataContainer).GetComponent<Data>();
            data.Set(d, m_Colormap, m_Colors);
            m_Data.Add(data);
        }
        Color[] ExtractColormap(Texture2D colormap)
        {
            Color[] colors = new Color[colormap.width];
            for (int x = 0; x < colormap.width; x++)
            {
                colors[x] = colormap.GetPixel(x, 0);
            }
            return colors;
        }
        void Clear()
        {
            foreach (Transform child in m_DataContainer)
            {
                Destroy(child.gameObject);
            }
            foreach (Transform child in m_ChannelHeaderContainer)
            {
                Destroy(child.gameObject);
            }
            m_Data = new List<Data>();
        }
        #endregion
    }
}