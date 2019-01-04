using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;

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
        [HideInInspector] public UnityEvent OnAutoLimits = new UnityEvent();
        [HideInInspector] public GenericEvent<CurveData, bool> OnDisplayCurve = new GenericEvent<CurveData, bool>();
        [HideInInspector] public GenericEvent<GroupCurveData, bool> OnDisplayGroup = new GenericEvent<GroupCurveData, bool>();
        #endregion

        #region Public Methods
        public void SetLegends(Dictionary<GroupCurveData,bool> stateByGroupCurve, Dictionary<CurveData,bool> stateByCurve)
        {
            if(legendsGestion != null) legendsGestion.SetLegends(stateByGroupCurve, stateByCurve);
            legendsGestion.OnDisplayCurve.RemoveAllListeners();
            legendsGestion.OnDisplayCurve.AddListener(OnDisplayCurve.Invoke);
            legendsGestion.OnDisplayGroup.RemoveAllListeners();
            legendsGestion.OnDisplayGroup.AddListener(OnDisplayGroup.Invoke);
        }
        public void SetTitle(string title)
        {
            titleText.text = title;
        }
        public void SetAbscissaLabel(string abcissa)
        {
            this.abscissa.Title = abcissa;
        }
        public void SetOrdinateLabel(string ordinate)
        {
            this.ordinate.Title = ordinate;
        }
        public void SetLimits(Limits limits)
        {
            this.limits = limits;
            abscissa.Limits = limits.Abscissa;
            ordinate.Limits = limits.Ordinate;
        }
        public void SetAbscissaLimits(Vector2 limits)
        {
            abscissa.Limits =limits;
        }
        public void SetOrdinateLimits(Vector2 limits)
        {
            ordinate.Limits = limits;
        }
        public void SetColor(Color color)
        {
            titleText.color = color;
            abscissa.Color = color;
            ordinate.Color = color;
        }
        public void UpdateWindowValues()
        {
            windowInputGestion.SetFields(abscissa.Title, ordinate.Title, limits);
        }
        #endregion

        #region Private Methods
        void Awake()
        {
            titleText = transform.Find("Title").GetComponent<Text>();
            abscissa = transform.Find("Abscissa").GetComponent<Axe>();
            ordinate = transform.Find("Ordinate").GetComponent<Axe>();
            Transform legend = transform.Find("Legend");
            if(legend != null)
            {
                legendsGestion = legend.GetComponentInChildren<LegendsGestion>();
            }
            Transform window = transform.Find("Window");
            if(window != null)
            {
                windowInputGestion = transform.Find("Window").GetComponent<WindowInputGestion>();
                Toggle parentToggle = GetComponentInParent<Toggle>();
                parentToggle.onValueChanged.RemoveAllListeners();
                parentToggle.onValueChanged.AddListener((b) => windowInputGestion.gameObject.SetActive(b));
                parentToggle.onValueChanged.AddListener((l) => windowInputGestion.SetFields(abscissa.Title,ordinate.Title,limits));
                windowInputGestion.OnAutoLimits.RemoveAllListeners();
                windowInputGestion.OnAutoLimits.AddListener(OnAutoLimits.Invoke);
            }
        }
        #endregion
    }
}
