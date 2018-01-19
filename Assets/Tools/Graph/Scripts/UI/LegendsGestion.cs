using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Tools.Unity.Graph
{
    public class LegendsGestion : MonoBehaviour
    {
        #region Properties
        [SerializeField] GameObject groupLegendPrefab;
        public GenericEvent<GroupCurveData, bool> OnDisplayGroup = new GenericEvent<GroupCurveData, bool>();
        public GenericEvent<CurveData, bool> OnDisplayCurve = new GenericEvent<CurveData, bool>();
        #endregion

        #region Public Methods
        public void SetLegends(Dictionary<GroupCurveData, bool> stateByGroupCurve, Dictionary<CurveData, bool> stateByCurve)
        {
            Clear();
            foreach (var group in stateByGroupCurve)
            {
                AddLegend(group.Key, group.Value, stateByCurve);
            }
        }
        #endregion

        #region Private Methods
        void AddLegend(GroupCurveData group, bool active, Dictionary<CurveData,bool> stateByCurve)
        {
            GameObject legendGameObject = Instantiate(groupLegendPrefab,transform);
            GroupLegend legend = legendGameObject.GetComponent<GroupLegend>();
            legend.Set(group,active, stateByCurve);
            legend.OnDisplayCurve.RemoveAllListeners();
            legend.OnDisplayCurve.AddListener(OnDisplayCurve.Invoke);
            legend.OnDisplayGroup.RemoveAllListeners();
            legend.OnDisplayGroup.AddListener(OnDisplayGroup.Invoke);
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
