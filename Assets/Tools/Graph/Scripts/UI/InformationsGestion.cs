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
        LimitsInput windowInputGestion;
        Limits limits;
        [HideInInspector] public UnityEvent OnAutoLimits = new UnityEvent();
        [HideInInspector] public GenericEvent<CurveData, bool> OnDisplayCurve = new GenericEvent<CurveData, bool>();
        [HideInInspector] public GenericEvent<CurveGroupData, bool> OnDisplayGroup = new GenericEvent<CurveGroupData, bool>();
        #endregion

        #region Public Methods
        public void SetLegends(Dictionary<CurveGroupData,bool> stateByGroupCurve, Dictionary<CurveData,bool> stateByCurve)
        {
            if(legendsGestion != null) legendsGestion.SetLegends(stateByGroupCurve, stateByCurve);
            legendsGestion.OnDisplayCurve.RemoveAllListeners();
            legendsGestion.OnDisplayCurve.AddListener(OnDisplayCurve.Invoke);
            legendsGestion.OnDisplayGroup.RemoveAllListeners();
            legendsGestion.OnDisplayGroup.AddListener(OnDisplayGroup.Invoke);
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
            //Transform window = transform.Find("Window");
            //if(window != null)
            //{
            //    windowInputGestion = transform.Find("Window").GetComponent<LimitsInput>();
            //    Toggle parentToggle = GetComponentInParent<Toggle>();
            //    parentToggle.onValueChanged.RemoveAllListeners();
            //    parentToggle.onValueChanged.AddListener((b) => windowInputGestion.gameObject.SetActive(b));
            //    parentToggle.onValueChanged.AddListener(delegate
            //    {
            //        windowInputGestion.Abscissa = abscissa.Title;
            //        windowInputGestion.Ordinate = ordinate.Title;
            //        windowInputGestion.AbscissaDisplayRange = limits;
            //        windowInputGestion.OrdinateDisplayRange = ordinate
            //    });
            //}
        }
        #endregion
    }
}
