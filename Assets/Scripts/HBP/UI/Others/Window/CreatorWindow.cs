using UnityEngine;
using UnityEngine.UI;
using HBP.Data.Enums;
using System.Collections.Generic;
using System;
using System.Linq;
using Tools.CSharp;

namespace HBP.UI
{
    public class CreatorWindow : DialogWindow
    {
        #region Properties
        [SerializeField] bool m_IsLoadableFromFile = false;
        public bool IsCreatableFromFile
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

        [SerializeField] bool m_IsLoadableFromDatabase = false;
        public bool IsCreatableFromDatabase
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

        [SerializeField] bool m_IsCreatableFromScratch = false;
        public bool IsCreatableFromScratch
        {
            get
            {
                return m_IsCreatableFromScratch;
            }
            set
            {
                m_IsCreatableFromScratch = value;
                Set();
            }
        }

        [SerializeField] bool m_IsCreatableFromExistingObjects = false;
        public bool IsCreatableFromExistingObjects
        {
            get
            {
                return m_IsCreatableFromExistingObjects;
            }
            set
            {
                m_IsCreatableFromExistingObjects = value;
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

        #region Protected Methods
        protected override void Initialize()
        {
            Set();
            base.Initialize();
        }
        protected virtual void Set()
        {
            List<Dropdown.OptionData> options = Enum.GetNames(typeof(CreationType)).Select((name) => new Dropdown.OptionData(StringExtension.CamelCaseToWords(name))).ToList();
            if (!IsCreatableFromFile) options.RemoveAll(o => o.text == "From file");
            if (!IsCreatableFromDatabase) options.RemoveAll(o => o.text == "From database");
            if (!IsCreatableFromExistingObjects) options.RemoveAll(o => o.text == "From existing object");
            if (!IsCreatableFromScratch) options.RemoveAll(o => o.text == "From scratch");
            if (options.Count == 0)
            {
                options.Add(new Dropdown.OptionData("None"));
                m_TypeDropdown.interactable = false;
                m_OKButton.interactable = false;
            }
            else
            {
                Interactable = Interactable;
            }
            m_TypeDropdown.options = options;
            m_TypeDropdown.value = (int)Type;
            m_TypeDropdown.RefreshShownValue();
            m_TypeDropdown.onValueChanged.AddListener((value) => Type = (CreationType)value);
        }
        #endregion
    }
}