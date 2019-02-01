using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;

namespace Tools.Unity.Graph
{
    public class InformationsGestion : MonoBehaviour
    {
        #region Properties
        LegendsGestion legendsGestion;
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
            Transform legend = transform.Find("Legend");
            if(legend != null)
            {
                legendsGestion = legend.GetComponentInChildren<LegendsGestion>();
            }
        }
        #endregion
    }
}
