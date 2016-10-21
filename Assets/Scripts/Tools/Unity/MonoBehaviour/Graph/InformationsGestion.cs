using UnityEngine;
using UnityEngine.UI;
using d = Tools.Unity.Graph.Data;

namespace Tools.Unity.Graph
{
    public class InformationsGestion : MonoBehaviour
    {
        #region Properties
        // Title
        Text m_title;
        public string Title { get { return m_title.text; } set { m_title.text = value; } }

        // Absciss
        Text m_abscissa;
        public string Abscissa { get { return m_abscissa.text; } set { m_abscissa.text = value; } }

        // Ordinate
        Text m_ordinate;
        public string Ordinate { get { return m_ordinate.text; } set { m_ordinate.text = value; } }

        // Color
        public Color Color { get { return m_title.color; } set { m_title.color = value; m_abscissa.color = value; m_ordinate.color = value; } }

        // Curves
        [SerializeField]
        LegendsGestion m_legends;
        public d.Curve[] Curves { set { m_legends.Curves = value; } get { return m_legends.Curves; } }
        #endregion

        #region Private Methods
        void Awake()
        {
            m_title = transform.GetChild(0).GetComponent<Text>();
            m_abscissa = transform.GetChild(1).GetComponent<Text>();
            m_ordinate = transform.GetChild(2).GetChild(0).GetComponent<Text>();
        }
        #endregion
    }
}
