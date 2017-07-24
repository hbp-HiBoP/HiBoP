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

        private bool m_IsActive = false;
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
                if (m_ColumnUI.HasEnoughSpaceForOverlay && m_IsActive)
                {
                    Debug.Log("hey");
                    gameObject.SetActive(true);
                }
                else
                {
                    gameObject.SetActive(false);
                }
            }
        }
        #endregion
    }
}