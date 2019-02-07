using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Tools.Unity.Graph
{
    public class LegendsGestion : MonoBehaviour
    {
        #region Properties
        [SerializeField] GameObject m_GroupLegendPrefab;
        [SerializeField] RectTransform m_GroupLegendContainer;

        [SerializeField] CurveGroupDataEvent m_OnDisplayGroup = new CurveGroupDataEvent();
        public CurveGroupDataEvent OnDisplayGroup
        {
            get
            {
                return m_OnDisplayGroup;
            }
        }

        [SerializeField] CurveDataEvent m_OnDisplayCurve = new CurveDataEvent();
        public CurveDataEvent OnDisplayCurve
        {
            get
            {
                return m_OnDisplayCurve;
            }
        }
        #endregion

        #region Public Methods
        public void SetLegends(Dictionary<CurveGroupData, bool> stateByGroupCurve, Dictionary<CurveData, bool> stateByCurve)
        {
            Clear();
            foreach (var group in stateByGroupCurve)
            {
                AddLegend(group.Key, group.Value, stateByCurve);
            }
        }
        #endregion

        #region Private Methods
        void AddLegend(CurveGroupData group, bool active, Dictionary<CurveData,bool> stateByCurve)
        {
            GameObject legendGameObject = Instantiate(m_GroupLegendPrefab,transform);
            GroupLegend legend = legendGameObject.GetComponent<GroupLegend>();
            legend.Set(group,active, stateByCurve);
            legend.OnDisplayCurve.RemoveAllListeners();
            legend.OnDisplayCurve.AddListener(m_OnDisplayCurve.Invoke);
            legend.OnDisplayGroup.RemoveAllListeners();
            legend.OnDisplayGroup.AddListener(m_OnDisplayGroup.Invoke);
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

        #region Events
        [Serializable] public class CurveGroupDataEvent : UnityEvent<CurveGroupData, bool> { }
        [Serializable] public class CurveDataEvent : UnityEvent<CurveData, bool> { }
        #endregion
    }
}
