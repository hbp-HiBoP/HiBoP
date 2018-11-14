using UnityEngine;
using UnityEngine.UI;
using HBP.Data.Enums;
using System.Collections.Generic;
using System;
using System.Linq;
using Tools.CSharp;

namespace HBP.UI
{
    public class CreatorWindow : SavableWindow
    {
        #region Properties
        bool m_IsLoadable = false;
        public bool IsLoadable
        {
            get
            {
                return m_IsLoadable;
            }
            set
            {
                m_IsLoadable = value;
                Set();
            }
        }
        public CreationType Type { private set; get; }
        [SerializeField] Dropdown m_TypeDropdown;

        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }

            set
            {
                base.Interactable = value;
                m_TypeDropdown.interactable = value;
            }
        }
        #endregion

        #region Private Methods
        protected override void Initialize()
        {
            Set();
            base.Initialize();
        }
        void Set()
        {
            List<Dropdown.OptionData> options = Enum.GetNames(typeof(CreationType)).Select((name) => new Dropdown.OptionData(StringExtension.CamelCaseToWords(name))).ToList();
            if (!IsLoadable) options.RemoveAll(o => o.text == "From file");
            m_TypeDropdown.options = options;
            m_TypeDropdown.value = (int)Type;
            m_TypeDropdown.RefreshShownValue();
            m_TypeDropdown.onValueChanged.AddListener((value) => Type = (CreationType)value);
        }
        #endregion
    }
}