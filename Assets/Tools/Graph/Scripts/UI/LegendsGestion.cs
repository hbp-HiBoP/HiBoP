using UnityEngine;

namespace Tools.Unity.Graph
{
    public class LegendsGestion : MonoBehaviour
    {
        #region Properties
        [SerializeField]
        GameObject legendPrefab;
        #endregion

        #region Public Methods
        public void SetLegends(CurveData[] curves)
        {
            Clear();
            if(curves != null) foreach(CurveData curve in curves) AddLegend(curve);
        }
        #endregion

        #region Private Methods
        void AddLegend(CurveData curve)
        {
            GameObject legendGameObject = Instantiate(legendPrefab);
            legendGameObject.name = curve.Label;
            legendGameObject.transform.SetParent(transform);
            legendGameObject.GetComponent<Legend>().Set(curve);
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
