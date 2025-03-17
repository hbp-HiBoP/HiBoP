using HBP.Core.Data;
using HBP.Core.Tools;
using HBP.Data.Database;
using HBP.UI.Tools;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Main
{
    public class ProtocolFilterConditionSubModifier : SubModifier<ProtocolFilterCondition>
    {
        #region Properties
        protected List<BaseData> m_FilteringObjects;
        public List<BaseData> FilteringObjects
        {
            get => m_FilteringObjects;
            set
            {
                m_FilteringObjects = value;
            }
        }

        private List<FilterToggle> m_Toggles = new List<FilterToggle>();

        [SerializeField] Dropdown m_LogicDropdown;
        [SerializeField] GameObject m_ProtocolFilterTogglePrefab;
        [SerializeField] Transform m_ProtocolFilterParent;
        [SerializeField] Button m_SelectAllButton;
        [SerializeField] Button m_DeselectAllButton;
        [SerializeField] Dropdown m_ScopeDropdown;
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();

            m_SelectAllButton.onClick.AddListener(() =>
            {
                foreach (var toggle in m_Toggles)
                    toggle.IsOn = true;
            });
            m_DeselectAllButton.onClick.AddListener(() =>
            {
                foreach (var toggle in m_Toggles)
                    toggle.IsOn = false;
            });

            m_LogicDropdown.onValueChanged.AddListener(OnChangeLogic);
            m_ScopeDropdown.onValueChanged.AddListener(OnChangeScope);
            m_ScopeDropdown.interactable = ApplicationState.LoadedProject != null;
        }
        #endregion

        #region Private Methods
        protected override void SetFields(ProtocolFilterCondition objectToDisplay)
        {
            base.SetFields(objectToDisplay);

            foreach (var protocol in DatabaseManager.Database.Protocols)
            {
                var toggle = Instantiate(m_ProtocolFilterTogglePrefab, m_ProtocolFilterParent).GetComponent<FilterToggle>();
                toggle.Label = protocol.Name;
                toggle.OnValueChanged.AddListener((isOn) =>
                {
                    if (isOn) Object.Protocols.Add(protocol);
                    else Object.Protocols.Remove(protocol);
                });
                m_Toggles.Add(toggle);
            }

            foreach (var toggle in m_Toggles)
                toggle.IsOn = objectToDisplay.Protocols.Select(p => p.Name).Contains(toggle.Label);

            m_LogicDropdown.value = (int)objectToDisplay.Logic;
            m_ScopeDropdown.value = (int)objectToDisplay.Scope;
        }
        private void OnChangeLogic(int value)
        {
            Object.Logic = (ProtocolFilterCondition.CheckLogic)value;
        }
        private void OnChangeScope(int value)
        {
            Object.Scope = (ProtocolFilterCondition.CheckScope)value;
        }
        #endregion
    }
}