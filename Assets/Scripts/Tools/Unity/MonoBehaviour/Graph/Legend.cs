using UnityEngine;
using UnityEngine.UI;
using d = Tools.Unity.Graph.Data;

namespace Tools.Unity.Graph
{
    public class Legend : MonoBehaviour
    {
        #region Properties
        [SerializeField]
        Image m_image;
        [SerializeField]
        Text m_text;
        #endregion

        #region Public Methods
        public void Set(d.Curve curve)
        {
            m_image.color = curve.Color;
            m_text.color = curve.Color;
            m_text.text = curve.Label;
        }
        #endregion
    }
}

