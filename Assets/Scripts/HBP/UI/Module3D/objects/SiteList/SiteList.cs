using HBP.Module3D;
using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI.Module3D
{
    public class SiteList : List<Site>
    {
        #region Properties
        public System.Collections.Generic.List<Site> ObjectsList
        {
            get
            {
                return m_Objects;
            }
            set
            {
                foreach (var item in m_ItemByObject.Values)
                {
                    Destroy(item.gameObject);
                }
                m_ItemByObject.Clear();
                m_Start = 0;
                m_End = 0;
                m_Objects = value;
                m_NumberOfObjects = value.Count;
                m_ScrollRect.content.sizeDelta = new Vector2(m_ScrollRect.content.sizeDelta.x, m_NumberOfObjects * ItemHeight);
                m_ScrollRect.content.hasChanged = true;
            }
        }
        #endregion

        #region Public Methods

        #endregion
    }
}