using System.Collections.Generic;

namespace UnityEngine.UI
{
    public class AutoResizeDropdown : Dropdown
    {
        private float m_Width = 0;
        private RectTransform m_ListRectTransform = null;
        private List<DropdownItem> m_Items = new List<DropdownItem>();

        protected override GameObject CreateDropdownList(GameObject template)
        {
            GameObject list = base.CreateDropdownList(template);
            m_ListRectTransform = list.GetComponent<RectTransform>();
            m_Width = GetComponent<RectTransform>().rect.width;
            m_Items.Clear();
            return list;
        }
        protected override DropdownItem CreateItem(DropdownItem itemTemplate)
        {
            DropdownItem item = base.CreateItem(itemTemplate);
            m_Items.Add(item);
            return item;
        }
        private void Update()
        {
            if (m_ListRectTransform)
            {
                foreach (var item in m_Items)
                {
                    m_Width = Mathf.Max(item.text.preferredWidth, m_Width);
                }
                m_ListRectTransform.sizeDelta = new Vector2(m_Width, m_ListRectTransform.sizeDelta.y);
                m_ListRectTransform = null;
            }
        }
    }
}