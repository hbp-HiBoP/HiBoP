using HBP.Core.Tools;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Tools
{
    public class CircularDropdown : MonoBehaviour
    {
        #region Properties
        [SerializeField] private Button m_LeftButton;
        [SerializeField] private Button m_RightButton;
        [SerializeField] private Dropdown m_Dropdown;

        public bool Interactable
        {
            get => m_Dropdown.interactable && m_LeftButton.interactable && m_RightButton.interactable;
            set
            {
                m_Dropdown.interactable = value;
                m_LeftButton.interactable = value;
                m_RightButton.interactable = value;
            }
        }

        public List<Dropdown.OptionData> Options
        {
            get => m_Dropdown.options;
            set
            {
                m_Dropdown.options = value;
                if (m_Dropdown.options.Count > 0)
                    m_Dropdown.SetValue(0);
                else
                    Interactable = false;
            }
        }
        #endregion

        #region Events
        public Dropdown.DropdownEvent OnValueChanged => m_Dropdown.onValueChanged;
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_LeftButton.onClick.AddListener(SelectPrevious);
            m_RightButton.onClick.AddListener(SelectNext);
        }
        #endregion

        #region Public Methods
        public void SelectNext()
        {
            int newValue = (m_Dropdown.value + 1) % m_Dropdown.options.Count;
            m_Dropdown.SetValue(newValue);
        }
        public void SelectPrevious()
        {
            int newValue = (m_Dropdown.value - 1 + m_Dropdown.options.Count) % m_Dropdown.options.Count;
            m_Dropdown.SetValue(newValue);
        }
        public void SetValue(int value)
        {
            m_Dropdown.SetValue(value);
        }
        #endregion
    }
}