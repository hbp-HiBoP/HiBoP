using System;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Tools
{
    // uGUI clones the authored template for every popup. Only the popup needs to
    // disable its generated rows, once Dropdown has finished populating them.
    public sealed class DropdownPopupOptions : MonoBehaviour
    {
        [SerializeField] private Dropdown m_Owner;
        private Func<int, string> m_UnavailableReason;
        private bool m_ShowTooltip;
        private DropdownPopupOptions m_Popup;

        public static DropdownPopupOptions Attach(Dropdown owner, Func<int, string> unavailableReason = null, bool showTooltip = false)
        {
            if (!owner.template) return null;
            var options = owner.template.gameObject.AddComponent<DropdownPopupOptions>();
            options.m_Owner = owner;
            options.m_UnavailableReason = unavailableReason;
            options.m_ShowTooltip = showTooltip;
            return options;
        }

        private void OnEnable()
        {
            if (m_Owner && transform != m_Owner.template)
                m_Owner.template.GetComponent<DropdownPopupOptions>().m_Popup = this;
        }

        private void Start()
        {
            if (!m_Owner || transform == m_Owner.template) return;
            var options = m_Owner.template.GetComponent<DropdownPopupOptions>();
            if (options.m_UnavailableReason == null) return;

            // The inactive first row is uGUI's item template, followed by the choices.
            var template = GetComponentInChildren<Toggle>(true);
            var parent = template.transform.parent;
            for (int i = 1; i < parent.childCount; i++)
            {
                string reason = options.m_UnavailableReason(i - 1);
                if (reason == null) continue;
                var item = parent.GetChild(i).GetComponent<Toggle>();
                item.interactable = false;
                var label = item.GetComponentInChildren<Text>();
                if (label)
                {
                    var color = label.color;
                    color.a *= 0.5f;
                    label.color = color;
                }

                if (options.m_ShowTooltip)
                {
                    var tooltip = item.GetComponent<Tooltip>() ?? item.gameObject.AddComponent<Tooltip>();
                    tooltip.Text = reason;
                }
            }
        }

        public void Close()
        {
            if (!m_Popup || !m_Owner || !m_Owner.isActiveAndEnabled) return;
            m_Popup.gameObject.SetActive(false);
            // A pending uGUI fade must not destroy the next popup after a context change.
            m_Owner.StopAllCoroutines();
            m_Owner.enabled = false;
            m_Owner.enabled = true;
            m_Popup = null;
        }
    }
}
