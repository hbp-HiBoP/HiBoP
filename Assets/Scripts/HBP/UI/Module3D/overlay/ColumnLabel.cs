using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public class ColumnLabel : OverlayElement
    {
        #region Properties
        [SerializeField]
        private Text m_Text;
        #endregion

        #region Public Methods
        public void Initialize(Base3DScene scene, Column3D column, Column3DUI columnUI)
        {
            m_ColumnUI = columnUI;
            IsActive = true;

            m_Text.text = column.Label;
        }
        #endregion
    }
}