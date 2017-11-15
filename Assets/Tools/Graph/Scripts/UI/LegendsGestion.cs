using UnityEngine;
using UnityEngine.Events;

namespace Tools.Unity.Graph
{
    public class LegendsGestion : MonoBehaviour
    {
        #region Properties
        [SerializeField]
        GameObject legendPrefab;

        public GenericEvent<CurveData, bool> OnDisplayCurve = new GenericEvent<CurveData, bool>();
        #endregion

        #region Public Methods
        public void SetLegends(Tuple<CurveData,bool>[] curves)
        {
            Clear();
            foreach(Tuple<CurveData, bool> curve in curves) AddLegend(curve.Object1,curve.Object2);
        }
        #endregion

        #region Private Methods
        void AddLegend(CurveData curve, bool active)
        {
            GameObject legendGameObject = Instantiate(legendPrefab);
            legendGameObject.transform.SetParent(transform);
            Legend legend = legendGameObject.GetComponent<Legend>();
            legend.Set(curve,active);
            legend.OnDisplayCurve.AddListener(OnDisplayCurve.Invoke);
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
