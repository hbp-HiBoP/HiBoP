﻿using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Graphs
{
    public class GraphOverlay : MonoBehaviour
    {
        #region Properties
        [SerializeField] Text m_OrdinateValueText;
        [SerializeField] Text m_AbscissaValueText;

        public Graph Graph;
        #endregion

        #region Private Methods
        private void Display()
        {
            if(Graph != null)
            {

            }
        }
        #endregion
    }
}


