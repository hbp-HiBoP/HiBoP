using data = HBP.Data.Informations;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.TrialMatrix.Grid
{
    public class ChannelHeader : MonoBehaviour
    {
        #region Properties
        data.ChannelStruct m_Channel;
        public data.ChannelStruct Channel
        {
            get
            {
                return m_Channel;
            }
            set
            {
                m_Channel = value;
                m_Text.text = value.Channel + " (" + value.Patient.Name + ")";
            }
        }
        [SerializeField] Text m_Text;
        #endregion
    }
}

