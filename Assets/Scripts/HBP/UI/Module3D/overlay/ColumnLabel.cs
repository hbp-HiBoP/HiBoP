using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public class ColumnLabel : MonoBehaviour
    {
        #region Properties
        [SerializeField]
        private Text m_Text;
        #endregion

        #region Public Methods
        public void Initialize(Base3DScene scene, Column3D column)
        {
            m_Text.text = column.Label;
        }
        #endregion
    }
}