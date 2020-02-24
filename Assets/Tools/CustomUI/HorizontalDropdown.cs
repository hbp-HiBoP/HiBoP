using UnityEngine.EventSystems;

namespace UnityEngine.UI
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
            Canvas canvas = GetTopmostCanvas(this);
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

        /// <summary>
        /// Get the topmost canvas of a component
        /// </summary>
        /// <param name="component">Component in a canvas</param>
        /// <returns>Canvas associated to the component</returns>
        private Canvas GetTopmostCanvas(Component component)
        {
            Canvas[] parentCanvases = component.GetComponentsInParent<Canvas>();
            if (parentCanvases != null && parentCanvases.Length > 0)
            {
                return parentCanvases[parentCanvases.Length - 1];
            }
            return null;
        }
    }
}