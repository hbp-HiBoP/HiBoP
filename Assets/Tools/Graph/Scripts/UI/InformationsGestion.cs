using UnityEngine;
using UnityEngine.UI;

namespace Tools.Unity.Graph
{
    public class InformationsGestion : MonoBehaviour
    {
        #region Properties
        Text titleText;
        Axe abscissa;
        Axe ordinate;
        LegendsGestion legendsGestion;
        WindowInputGestion windowInputGestion;
        Limits limits;
        #endregion

        #region Public Methods
        public void SetLegends(CurveData[] curves)
        {
            if(legendsGestion != null) legendsGestion.SetLegends(curves);
        }
        public void SetTitle(string title)
        {
            titleText.text = title;
        }
        public void SetAbscissaLabel(string abcissa)
        {
            this.abscissa.SetLabel(abcissa);
        }
        public void SetOrdinateLabel(string ordinate)
        {
            this.ordinate.SetLabel(ordinate);
        }
        public void SetLimits(Limits limits)
        {
            this.limits = limits;
            abscissa.SetLimits(limits.Abscissa);
            ordinate.SetLimits(limits.Ordinate);
        }
        public void SetAbscissaLimits(Vector2 limits)
        {
            abscissa.SetLimits(limits);
        }
        public void SetOrdinateLimits(Vector2 limits)
        {
            ordinate.SetLimits(limits);
        }
        public void SetColor(Color color)
        {
            titleText.color = color;
            abscissa.SetColor(color);
            ordinate.SetColor(color);
        }
        #endregion

        #region Private Methods
        void Awake()
        {
            titleText = transform.FindChild("Title").GetComponent<Text>();
            abscissa = transform.FindChild("Abscissa").GetComponent<Axe>();
            ordinate = transform.FindChild("Ordinate").GetComponent<Axe>();
            Transform legend = transform.FindChild("Legend");
            if(legend != null)
            {
                legendsGestion = legend.GetComponentInChildren<LegendsGestion>();
            }
            Transform window = transform.FindChild("Window");
            if(window != null)
            {
                windowInputGestion = transform.FindChild("Window").GetComponent<WindowInputGestion>();
                Toggle parentToggle = GetComponentInParent<Toggle>();
                parentToggle.onValueChanged.RemoveAllListeners();
                parentToggle.onValueChanged.AddListener((b) => windowInputGestion.gameObject.SetActive(b));
                parentToggle.onValueChanged.AddListener((l) => windowInputGestion.SetFields(limits));
            }
        }
        #endregion
    }
}
