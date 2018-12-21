using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HBP.UI.Module3D
{
    public abstract class ColumnOverlayElement : OverlayElement
    {
        #region Properties
        /// <summary>
        /// Associated Column3DUI
        /// </summary>
        protected Column3DUI m_ColumnUI;
        
        protected bool m_IsActive = false;
        /// <summary>
        /// Is this overlay element active ?
        /// </summary>
        public bool IsActive
        {
            get
            {
                return m_IsActive;
            }
            set
            {
                m_IsActive = value;
                HandleEnoughSpace();
            }
        }
        #endregion

        #region Public Methods
        public void HandleEnoughSpace()
        {
            SetActive(m_ColumnUI.HasEnoughSpaceForOverlay && m_IsActive);
        }
        public virtual void Setup(Base3DScene scene, Column3D column, Column3DUI columnUI)
        {
            Initialize();
            m_ColumnUI = columnUI;
        }
        #endregion

        #region Private Methods
        void SetActive(bool active)
        {
            if (active != gameObject.activeSelf) gameObject.SetActive(active);
        }
        #endregion
    }
}