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
                m_Objects = value;
                m_NumberOfObjects = value.Count;
                m_ScrollRect.content.sizeDelta = new Vector2(m_ScrollRect.content.sizeDelta.x, m_NumberOfObjects * ItemHeight);
                m_ScrollRect.content.hasChanged = true;
                Refresh();
            }
        }
        #endregion

        #region Public Methods

        #endregion
    }
}