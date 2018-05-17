using HBP.Module3D;
using System.Linq;
using Tools.Unity.Lists;
using UnityEngine;
using UnityEngine.Profiling;

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
                int newNumberOfObjects = value.Count;
                int resize = Mathf.Min(newNumberOfObjects, m_MaximumNumberOfItems) - m_Items.Count;
                if (resize < 0)
                {
                    DestroyItem(resize,false);
                }
                else if (resize > 0)
                {
                    SpawnItem(resize,false);
                }
                m_Objects = value;
                m_NumberOfObjects = newNumberOfObjects;
                m_ScrollRect.content.sizeDelta = new Vector2(m_ScrollRect.content.sizeDelta.x, newNumberOfObjects * ItemHeight);
                m_ScrollRect.verticalScrollbar = m_ScrollRect.verticalScrollbar;
                m_ScrollRect.content.hasChanged = true;
                GetLimits(out m_FirstIndexDisplayed, out m_LastIndexDisplayed);
                RefreshPosition();
                Refresh();
            }
        }
        #endregion
    }
}