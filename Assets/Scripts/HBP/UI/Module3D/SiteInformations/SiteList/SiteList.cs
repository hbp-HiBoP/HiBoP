using UnityEngine;
using HBP.UI.Lists;

namespace HBP.UI.Module3D
{
    /// <summary>
    /// List to display Module3D sites.
    /// </summary>
    public class SiteList : List<Core.Object3D.Site>
    {
        #region Properties
        /// <summary>
        /// List of module3D sites.
        /// </summary>
        public System.Collections.Generic.List<Core.Object3D.Site> ObjectsList
        {
            get
            {
                return m_Objects;
            }
            set
            {
                m_Objects = value;
                ScrollRect.content.sizeDelta = new Vector2(0, m_ItemHeight * m_Objects.Count);
                ScrollRect.content.hasChanged = true;
            }
        }
        #endregion
    }
}