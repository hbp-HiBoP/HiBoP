using UnityEngine;
using d = Tools.Unity.Graph.Data;

namespace Tools.Unity.Graph
{
    public class LegendsGestion : MonoBehaviour
    {
        #region Properties
        [SerializeField]
        GameObject m_legend;

        d.Curve[] m_curves;
        public d.Curve[] Curves
        {
            get
            {
                return m_curves;
            }
            set
            {
                Clear();
                m_curves = value;
                foreach (d.Curve c in m_curves)
                {
                    AddLegend(c);
                }
            }
        }
        #endregion

        #region Private Methods
        void AddLegend(d.Curve curve)
        {
            GameObject l_GO = Instantiate(m_legend);
            l_GO.name = curve.Label;
            l_GO.transform.SetParent(transform);
            l_GO.GetComponent<Legend>().Set(curve);
        }
        void Clear()
        {
            int iMax = transform.childCount;
            for (int i = 0; i < iMax; i++)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
        }
        #endregion
    }
}
