using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Main
{
    /// <summary>
    /// Component to display protocol selection for BIDS export.
    /// </summary>
    public class BIDSProtocolItem : MonoBehaviour
    {
        #region Properties
        [SerializeField] private Text m_ProtocolNameText;
        [SerializeField] private Toggle m_Toggle;
        
        public string Name => m_ProtocolNameText.text;
        public bool IsSelected => m_Toggle.isOn;
        public Toggle.ToggleEvent OnToggleChanged => m_Toggle.onValueChanged;
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the protocol item with protocol name.
        /// </summary>
        /// <param name="protocolName">Name of the protocol</param>
        public void Initialize(string protocolName)
        {
            m_ProtocolNameText.text = protocolName;
            m_Toggle.isOn = false;
        }

        /// <summary>
        /// Set the selected state of the protocol.
        /// </summary>
        /// <param name="selected">Whether the protocol should be selected</param>
        public void SetSelected(bool selected)
        {
            m_Toggle.isOn = selected;
        }
        #endregion
    }
}