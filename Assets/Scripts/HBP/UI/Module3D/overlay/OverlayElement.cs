using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HBP.UI.Module3D
{
    public abstract class OverlayElement : MonoBehaviour
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
                //if (m_IsActive != gameObject.activeSelf)
                //{
                //    if (m_ColumnUI.HasEnoughSpaceForOverlay && m_IsActive)
                //    {
                //        gameObject.SetActive(true);
                //    }
                //    else
                //    {
                //        gameObject.SetActive(false);
                //    }
                //}
            }
        }
        #endregion

        #region Public Methods
        public void HandleEnoughSpace()
        {
            SetActive(m_ColumnUI.HasEnoughSpaceForOverlay && m_IsActive);
        }
        void SetActive(bool active)
        {
            if (active != gameObject.activeSelf) gameObject.SetActive(active);
        }
        #endregion
    }
}