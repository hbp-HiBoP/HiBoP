using HBP.Data.TrialMatrix.Grid;
using System.Collections.Generic;
using UnityEngine;

namespace HBP.UI.TrialMatrix.Grid
{
    public class ChannelHeaderDisplayer : MonoBehaviour
    {
        #region Properties
        List<ChannelHeader> m_Headers= new List<ChannelHeader>();
        public ChannelHeader[] Headers
        {
            get
            {
                return m_Headers.ToArray();
            }
        }

        [SerializeField] GameObject m_HeaderPrefab;
        #endregion

        #region Public Methods
        public void Set(IEnumerable<ChannelStruct> channels)
        {
            Clear();
            foreach (var channel in channels)
            {
                ChannelHeader header = Instantiate(m_HeaderPrefab, transform).GetComponent<ChannelHeader>();
                header.Channel = channel;
                m_Headers.Add(header);
            }
        }
        #endregion

        #region Private Methods
        void Clear()
        {
            foreach (var header in m_Headers)
            {
                Destroy(header.gameObject);
            }
            m_Headers = new List<ChannelHeader>();
        }
        #endregion
    }
}