using UnityEngine;
using data = HBP.Data.Informations;

namespace HBP.UI.Informations
{
    public class ChannelInformations : MonoBehaviour
    {
        #region Properties
        [SerializeField] data.ChannelStruct[] m_ChannelStructs;
        [SerializeField] data.DataStruct[] m_DataStructs;

        [SerializeField] GraphZone m_GraphZone;
        [SerializeField] TrialMatrixZone m_TrialMatrixZone;
        #endregion

        #region Public Methods
        public void Display(data.ChannelStruct[] channelStructs, data.DataStruct[] dataStructs)
        {
            m_ChannelStructs = channelStructs;
            m_DataStructs = dataStructs;

            UnityEngine.Profiling.Profiler.BeginSample("TrialMatrixZone");
            m_TrialMatrixZone.Display(channelStructs, dataStructs);
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("GraphZone");
            m_GraphZone.Display(channelStructs, dataStructs);
            UnityEngine.Profiling.Profiler.EndSample();
        }
        #endregion
    }
}