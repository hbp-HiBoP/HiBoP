using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HBP.UI.Module3D
{
    /// <summary>
    /// Base class of an overlay element of a column
    /// </summary>
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
        /// <summary>
        /// Displays the overlay element only if there is enough space to display it
        /// </summary>
        public void HandleEnoughSpace()
        {
            SetActive(m_ColumnUI.HasEnoughSpaceForOverlay && m_IsActive);
        }
        /// <summary>
        /// Setup the overlay element
        /// </summary>
        /// <param name="scene">Associated 3D scene</param>
        /// <param name="column">Associated 3D column</param>
        /// <param name="columnUI">Parent UI column</param>
        public virtual void Setup(Base3DScene scene, Column3D column, Column3DUI columnUI)
        {
            Initialize();
            m_ColumnUI = columnUI;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Set the visibility of the overlay element
        /// </summary>
        /// <param name="active">Is the overlay element active ?</param>
        void SetActive(bool active)
        {
            if (active != gameObject.activeSelf) gameObject.SetActive(active);
        }
        #endregion
    }
}