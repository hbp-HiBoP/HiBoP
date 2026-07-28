using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Main
{
    /// <summary>
    /// Component to display data selection for BIDS export.
    /// </summary>
    public class BIDSDataItem : MonoBehaviour
    {
        #region Properties

        [SerializeField] private Text m_DataNameText;
        [SerializeField] private Toggle m_Toggle;

        public string DataName => m_DataNameText.text;
        public bool IsSelected => m_Toggle.isOn;
        public Toggle.ToggleEvent OnToggleChanged => m_Toggle.onValueChanged;

        #endregion

        #region Public Methods

        /// <summary>
        /// Initialize the data item with data name.
        /// </summary>
        /// <param name="dataName">Name of the data</param>
        public void Initialize(string dataName)
        {
            m_DataNameText.text = dataName;
            m_Toggle.isOn = false;
        }

        /// <summary>
        /// Set the selected state of the data.
        /// </summary>
        /// <param name="selected">Whether the data should be selected</param>
        public void SetSelected(bool selected)
        {
            m_Toggle.isOn = selected;
        }

        #endregion
    }
}
