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
        bool m_IsLoadableFromFile = false;
        public bool IsLoadableFromFile
        {
            get
            {
                return m_IsLoadableFromFile;
            }
            set
            {
                m_IsLoadableFromFile = value;
                Set();
            }
        }

        bool m_IsLoadableFromDatabase = false;
        public bool IsLoadableFromDatabase
        {
            get
            {
                return m_IsLoadableFromDatabase;
            }
            set
            {
                m_IsLoadableFromDatabase = value;
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
            if (!IsLoadableFromFile) options.RemoveAll(o => o.text == "From file");
            if (!IsLoadableFromDatabase) options.RemoveAll(o => o.text == "From database");
            m_TypeDropdown.options = options;
            m_TypeDropdown.value = (int)Type;
            m_TypeDropdown.RefreshShownValue();
            m_TypeDropdown.onValueChanged.AddListener((value) => Type = (CreationType)value);
        }
        #endregion
    }
}