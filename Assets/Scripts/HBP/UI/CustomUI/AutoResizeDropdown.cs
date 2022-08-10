using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.Display.UI.Tools
{
    public class AutoResizeDropdown : Dropdown
    {
        private float m_Width = 0;
        private RectTransform m_ListRectTransform = null;
        private List<DropdownItem> m_ItemsList = new List<DropdownItem>();

        protected override GameObject CreateDropdownList(GameObject template)
        {
            GameObject list = base.CreateDropdownList(template);
            m_ListRectTransform = list.GetComponent<RectTransform>();
            m_Width = GetComponent<RectTransform>().rect.width;
            m_ItemsList.Clear();
            return list;
        }
        protected override DropdownItem CreateItem(DropdownItem itemTemplate)
        {
            DropdownItem item = base.CreateItem(itemTemplate);
            m_ItemsList.Add(item);
            return item;
        }
        private void Update()
        {
            if (m_ListRectTransform)
            {
                foreach (var item in m_ItemsList)
                {
                    m_Width = Mathf.Max(item.text.preferredWidth, m_Width);
                }
                m_ListRectTransform.sizeDelta = new Vector2(m_Width, m_ListRectTransform.sizeDelta.y);
                m_ListRectTransform = null;
            }
        }
    }
}