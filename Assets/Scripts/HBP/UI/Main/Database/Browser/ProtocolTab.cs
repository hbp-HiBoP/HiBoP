using HBP.Core.Data;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Database
{
    public class ProtocolTab : MonoBehaviour
    {
        #region Properties
        [SerializeField] private Toggle m_Toggle;
        public Toggle Toggle => m_Toggle;
        [SerializeField] private Text m_Text;

        public GenericEvent<Protocol> OnSelect = new();
        #endregion

        #region Public Methods
        public void Initialize(Protocol protocol)
        {
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