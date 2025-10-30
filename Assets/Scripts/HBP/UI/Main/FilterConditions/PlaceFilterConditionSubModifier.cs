using HBP.Core.Data;
using HBP.UI.Tools;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Main
{
    public class PlaceFilterConditionSubModifier : SubModifier<PlaceFilterCondition>
    {
        #region Properties
        protected List<object> m_FilteringObjects;
        public List<object> FilteringObjects
        {
            get => m_FilteringObjects;
            set
            {
                m_FilteringObjects = value;
                SetToggles();
            }
        }

        private List<FilterToggle> m_Toggles = new List<FilterToggle>();

        [SerializeField] GameObject m_PlaceFilterTogglePrefab;
        [SerializeField] Transform m_PlaceFilterParent;
        [SerializeField] Button m_SelectAllButton;
        [SerializeField] Button m_DeselectAllButton;
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
            SetToggles();
        }
        #endregion

        #region Private Methods
        protected override void SetFields(PlaceFilterCondition objectToDisplay)
        {
            base.SetFields(objectToDisplay);

            foreach (var toggle in m_Toggles)
                toggle.IsOn = objectToDisplay.Places.Contains(toggle.Label);
        }
        protected void SetToggles()
        {
            foreach (var toggle in m_Toggles)
                Destroy(toggle.gameObject);
            m_Toggles.Clear();

            if (m_FilteringObjects != null && m_FilteringObjects.Count > 0)
            {
                var places = m_FilteringObjects.OfType<Patient>().Select(p => p.Place).Distinct().OrderBy(p => p).ToList();
                foreach (var place in places)
                {
                    var toggle = Instantiate(m_PlaceFilterTogglePrefab, m_PlaceFilterParent).GetComponent<FilterToggle>();
                    toggle.Label = place;
                    toggle.OnValueChanged.AddListener((isOn) =>
                    {
                        if (isOn) Object.Places.Add(place);
                        else Object.Places.Remove(place);
                    });
                    m_Toggles.Add(toggle);
                }
            }

            if (m_Object != null)
                SetFields(m_Object);
        }
        #endregion
    }
}