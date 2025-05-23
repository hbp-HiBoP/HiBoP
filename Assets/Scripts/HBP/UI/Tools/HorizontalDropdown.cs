using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;
using HBP.Core.Tools;

namespace HBP.UI.Tools
{
    public class HorizontalDropdown : Dropdown
    {
        private RectTransform m_DropdownRectTransform;
        private RectTransform m_RectTransform;

        protected override void Awake()
        {
            base.Awake();
            m_RectTransform = GetComponent<RectTransform>();
        }

        protected override GameObject CreateBlocker(Canvas rootCanvas)
        {
            Canvas canvas = m_RectTransform.GetTopmostCanvas();
            GameObject blocker = base.CreateBlocker(canvas);
            return blocker;
        }

        protected override GameObject CreateDropdownList(GameObject template)
        {
            GameObject dropdown = base.CreateDropdownList(template);
            m_DropdownRectTransform = dropdown.GetComponent<RectTransform>();
            return dropdown;
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            base.OnPointerClick(eventData);

            if (!m_DropdownRectTransform) return;

            m_DropdownRectTransform.pivot = new Vector2(0, 1);
            m_DropdownRectTransform.position = new Vector3(m_RectTransform.position.x + m_RectTransform.rect.width / 2, m_RectTransform.position.y + m_RectTransform.rect.height / 2, m_DropdownRectTransform.position.z);
        }
    }
}