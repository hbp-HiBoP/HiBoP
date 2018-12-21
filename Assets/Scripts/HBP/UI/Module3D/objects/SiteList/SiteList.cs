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
                m_Objects = value;
                m_NumberOfObjects = value.Count;
                m_ScrollRect.content.sizeDelta = new Vector2(0, ItemHeight * m_NumberOfObjects);
                m_ScrollRect.content.hasChanged = true;
            }
        }
        #endregion
    }
}