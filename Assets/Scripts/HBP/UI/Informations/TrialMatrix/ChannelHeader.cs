using data = HBP.Data.Informations;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Informations.TrialMatrix
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
                string label = value.Channel + " (" + value.Patient.Name + ")";
                m_Text.text = label;
                gameObject.name = label;
            }
        }
        [SerializeField] Text m_Text;
        #endregion
    }
}

