using UnityEngine;
using data = HBP.Data.Informations;

namespace HBP.UI.Informations
{
    public class ChannelInformations : MonoBehaviour
    {
        #region Properties
        data.ChannelStruct[] m_ChannelStructs;
        data.DataStruct[] m_DataStructs;

        Texture2D m_Colormap;
        public Texture2D Colormap
        {
            get
            {
                return m_Colormap;
            }
            set
            {
                m_Colormap = value;
                m_TrialMatrixZone.Colormap = value;
            }
        }
        [SerializeField] GraphZone m_GraphZone;
        [SerializeField] TrialMatrixZone m_TrialMatrixZone;
        #endregion

        #region Public Methods
        public void Display(data.ChannelStruct[] channelStructs, data.DataStruct[] dataStructs)
        {
            m_ChannelStructs = channelStructs;
            m_DataStructs = dataStructs;

            m_TrialMatrixZone.Display(channelStructs, dataStructs);
        }
        #endregion
    }
}