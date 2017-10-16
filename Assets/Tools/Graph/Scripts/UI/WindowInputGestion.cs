using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace Tools.Unity.Graph
{
    public class WindowInputGestion : MonoBehaviour
    {
        #region Properties
        [SerializeField]
        Text m_AbscissaLabel;
        [SerializeField]
        Text m_OrdinateLabel;
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
        [SerializeField]
        Button m_AutoButton;
        [HideInInspector] public UnityEvent OnAutoLimits = new UnityEvent();
        #endregion

        #region Public Methods
        public void OnChange()
        {
            Limits limits = new Limits(float.Parse(m_xmin.text), float.Parse(m_xmax.text), float.Parse(m_ymin.text), float.Parse(m_ymax.text));
            m_display.OnChangeLimits.Invoke(limits,false);
        }

        public void SetFields(string abscissa,string ordinate, Limits limits)
        {
            m_AbscissaLabel.text = abscissa;
            m_OrdinateLabel.text = ordinate;
            m_xmin.text = limits.AbscissaMin.ToString();
            m_xmax.text = limits.AbscissaMax.ToString();
            m_ymin.text = limits.OrdinateMin.ToString();
            m_ymax.text = limits.OrdinateMax.ToString();
            m_AutoButton.onClick.RemoveAllListeners();
            m_AutoButton.onClick.AddListener(() => OnAutoLimits.Invoke());
        }
        #endregion
    }
}