using HBP.Core.Data;
using HBP.Theme;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using HBP.Core.Tools;

namespace HBP.UI.Database
{
    public class ProtocolTab : MonoBehaviour
    {
        #region Properties
        [SerializeField] private Toggle m_Toggle;
        public Toggle Toggle => m_Toggle;
        [SerializeField] private UnityEngine.UI.Text m_Text;
        public Protocol Protocol { get; private set; }

        public GenericEvent<Protocol> OnSelect = new();

        private bool m_HasData = false;
        public bool HasData
        {
            get => m_HasData;
            set
            {
                m_HasData = value;
                m_Toggle.graphic.GetComponent<ThemeElement>().Set(value ? m_HasDataState : m_NoDataState);
                m_Text.GetComponent<ThemeElement>().Set(value ? m_HasDataState : m_NoDataState);
            }
        }
        [SerializeField] private State m_HasDataState;
        [SerializeField] private State m_NoDataState;
        #endregion

        #region Public Methods
        public void Initialize(Protocol protocol)
        {
            Protocol = protocol;
            m_Text.text = protocol.Name;
            m_Toggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    OnSelect.Invoke(protocol);
                }
            });
        }
        #endregion
    }
}