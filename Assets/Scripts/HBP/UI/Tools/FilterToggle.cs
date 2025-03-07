using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Tools
{
    [RequireComponent(typeof(Toggle))]
    public class FilterToggle : MonoBehaviour
    {
        #region Properties
        [SerializeField] Text m_Label;
        [SerializeField] Toggle m_Toggle;
        public bool IsOn
        {
            get => m_Toggle.isOn;
            set => m_Toggle.isOn = value;
        }
        public string Label
        {
            get => m_Label.text;
            set => m_Label.text = value;
        }
        public bool Interactable
        {
            get => m_Toggle.interactable;
            set => m_Toggle.interactable = value;
        }

        public Toggle.ToggleEvent OnValueChanged => m_Toggle.onValueChanged;
        #endregion
    }
}