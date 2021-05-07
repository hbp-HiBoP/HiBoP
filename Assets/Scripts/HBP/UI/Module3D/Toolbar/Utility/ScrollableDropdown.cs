using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    [RequireComponent(typeof(Dropdown))]
    public class ScrollableDropdown : MonoBehaviour, IScrollHandler
    {
        private Dropdown m_Dropdown;
        private float m_ScrollDelta = 0;

        private void Awake()
        {
            m_Dropdown = GetComponent<Dropdown>();
        }
        public void OnScroll(PointerEventData eventData)
        {
            m_ScrollDelta += eventData.scrollDelta.y;
            if (Mathf.Abs(m_ScrollDelta) < 1) return;

            int newValue = m_Dropdown.value + (m_ScrollDelta < 0 ? 1 : -1);
            int total = m_Dropdown.options.Count;
            m_Dropdown.value = ((newValue % total) + total) % total;
            m_Dropdown.RefreshShownValue();
            m_ScrollDelta = 0;
        }
    }
}