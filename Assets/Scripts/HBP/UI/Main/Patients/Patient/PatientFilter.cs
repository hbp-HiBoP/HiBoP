using HBP.Core.Data;
using HBP.UI.Tools;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Main
{
    public class PatientFilter : ListFilter<Patient>
    {
        #region Properties
        [SerializeField] Toggle m_FilterName;
        [SerializeField] InputField m_NameInputField;

        [SerializeField] Toggle m_FilterPlace;
        [SerializeField] Transform m_PlaceTogglesParent;
        List<FilterToggle> m_PlaceToggles = new();

        [SerializeField] Toggle m_FilterDate;
        [SerializeField] Transform m_DateTogglesParent;
        List<FilterToggle> m_DateToggles = new();

        [SerializeField] GameObject m_FilterTogglePrefab;
        #endregion

        #region Private Methods
        protected override void SetObjects()
        {
            m_FilterName.onValueChanged.RemoveAllListeners();
            m_FilterName.onValueChanged.AddListener((value) => m_NameInputField.interactable = value);
            m_FilterName.isOn = false;

            m_FilterPlace.onValueChanged.RemoveAllListeners();
            m_PlaceToggles.Clear();
            foreach (var place in m_Objects.Select(p => p.Place).Distinct().OrderBy(p => p))
            {
                var toggle = Instantiate(m_FilterTogglePrefab, m_PlaceTogglesParent).GetComponent<FilterToggle>();
                toggle.Label = place;
                m_PlaceToggles.Add(toggle);
            }
            m_FilterPlace.onValueChanged.AddListener((value) =>
            {
                foreach (var toggle in m_PlaceToggles)
                {
                    toggle.Interactable = value;
                }
            });
            m_FilterPlace.isOn = false;

            m_FilterDate.onValueChanged.RemoveAllListeners();
            m_DateToggles.Clear();
            foreach (var date in m_Objects.Select(p => p.Date).Distinct().OrderBy(d => d))
            {
                var toggle = Instantiate(m_FilterTogglePrefab, m_DateTogglesParent).GetComponent<FilterToggle>();
                toggle.Label = date.ToString();
                m_DateToggles.Add(toggle);
            }
            m_FilterDate.onValueChanged.AddListener((value) =>
            {
                foreach (var toggle in m_DateToggles)
                {
                    toggle.Interactable = value;
                }
            });
            m_FilterDate.isOn = false;
        }
        protected override bool CheckConditions(Patient obj)
        {
            bool result = true;
            if (m_FilterName.isOn) result &= obj.Name.Contains(m_NameInputField.text);
            if (m_FilterPlace.isOn) result &= m_PlaceToggles.Any(t => t.IsOn && t.Label == obj.Place);
            if (m_FilterDate.isOn) result &= m_DateToggles.Any(t => t.IsOn && t.Label == obj.Date.ToString());
            return result;
        }
        #endregion
    }
}