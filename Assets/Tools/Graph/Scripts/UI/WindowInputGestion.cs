using UnityEngine;
using UnityEngine.UI;

namespace Tools.Unity.Graph
{
    public class WindowInputGestion : MonoBehaviour
    {
        #region Properties
        [SerializeField]
        InputField m_xmin;
        [SerializeField]
        InputField m_xmax;
        [SerializeField]
        InputField m_ymin;
        [SerializeField]
        InputField m_ymax;
        [SerializeField]
        PlotGestion m_display;
        #endregion

        #region Public Methods
        public void OnChange()
        {
            Limits limits = new Limits(float.Parse(m_xmin.text), float.Parse(m_xmax.text), float.Parse(m_ymin.text), float.Parse(m_ymax.text));
            m_display.OnChangeLimits.Invoke(limits);
        }

        public void SetFields(Limits limits)
        {
            m_xmin.text = limits.AbscissaMin.ToString();
            m_xmax.text = limits.AbscissaMax.ToString();
            m_ymin.text = limits.OrdinateMin.ToString();
            m_ymax.text = limits.OrdinateMax.ToString();
        }
        #endregion
    }
}