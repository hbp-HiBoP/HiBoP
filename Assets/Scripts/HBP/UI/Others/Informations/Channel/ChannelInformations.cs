using HBP.Data.Experience.Protocol;
using System.Linq;
using UnityEngine;
using data = HBP.Data.Informations;

namespace HBP.UI.Informations
{
    public class ChannelInformations : MonoBehaviour
    {
        #region Properties
        [SerializeField] data.ChannelStruct[] m_Channels;
        [SerializeField] data.Column[] m_Columns;

        [SerializeField] GraphZone m_GraphZone;
        [SerializeField] TrialMatrixZone m_TrialMatrixZone;
        #endregion

        #region Public Methods
        public void SetMaxNumberOfTrialMatrixColumn(int max)
        {
            m_GraphZone.CreateGraphPool(max);
        }
        public void Display(data.ChannelStruct[] channels, data.Column[] columns)
        {
            m_Channels = channels;
            m_Columns = columns;

            if(isActiveAndEnabled)
            {
                m_TrialMatrixZone.Display(channels, columns.Select(column => column.Data).ToArray());
                m_GraphZone.Display(channels, m_Columns);
            }
        }
        public void DisplayTrialMatrices(data.ChannelStruct[] channels, data.Column[] columns)
        {
            m_Channels = channels;
            m_Columns = columns;

            if(isActiveAndEnabled)
            {
                m_TrialMatrixZone.Display(channels, columns.Select(column => column.Data).ToArray());
            }
        }
        public void DisplayGraphs(data.ChannelStruct[] channels, data.Column[] columns)
        {
            m_Channels = channels;
            m_Columns = columns;

            if(isActiveAndEnabled)
            {
                m_GraphZone.Display(channels, columns);
            }
        }
        public void UpdateTime(data.Column column, SubBloc subBloc, float currentTime)
        {
            m_GraphZone.UpdateTime(column, subBloc, currentTime);
        }
        #endregion

        #region Private Methods
        void OnEnable()
        {
            if(m_Channels != null && m_Columns != null)
            {
                m_TrialMatrixZone.Display(m_Channels, m_Columns.Select(column => column.Data).ToArray());
                m_GraphZone.Display(m_Channels, m_Columns);
            }
        }
        #endregion
    }
}