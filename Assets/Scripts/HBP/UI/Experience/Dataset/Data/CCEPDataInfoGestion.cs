using UnityEngine;
using UnityEngine.UI;
using d = HBP.Data.Experience.Dataset;

namespace HBP.UI.Experience.Dataset
{
    public class CCEPDataInfoGestion : MonoBehaviour
    {
        #region Properties     
        [SerializeField] InputField m_ChannelInputField;

        protected bool m_Interactable;
        public virtual bool Interactable
        {
            get
            {
                return m_Interactable;
            }
            set
            {
                m_Interactable = value;
                m_ChannelInputField.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
        }
        public void Set(d.CCEPDataInfo CCEPDataInfo)
        {
            m_ChannelInputField.text = CCEPDataInfo.Channel;

            m_ChannelInputField.onValueChanged.RemoveAllListeners();
            m_ChannelInputField.onValueChanged.AddListener((channel) => CCEPDataInfo.Channel = channel);
        }
        #endregion
    }
}