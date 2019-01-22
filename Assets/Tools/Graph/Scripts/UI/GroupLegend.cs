using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Tools.Unity.Graph
{
    public class GroupLegend : MonoBehaviour
    {
        #region Properties
        [SerializeField] Text m_Text;
        [SerializeField] Toggle m_Toggle;
        [SerializeField] RectTransform m_CurvesRectTransform;
        [SerializeField] GameObject m_CurvePrefab;
        CurveGroupData m_Group;
        static Color m_InactiveColor = Color.grey;

        public GenericEvent<CurveData, bool> OnDisplayCurve = new GenericEvent<CurveData,bool>();
        public GenericEvent<CurveGroupData, bool> OnDisplayGroup = new GenericEvent<CurveGroupData, bool>();
        #endregion

        #region Public Methods
        public void Set(CurveGroupData group, bool active, Dictionary<CurveData, bool> stateByCurve)
        {
            name = group.Name;
            m_Group = group;
            m_Text.text = group.Name;

            m_Toggle.isOn = active;
            ChangeColor(active);

            foreach (var curveData in group.Curves)
            {
                AddLegend(curveData, stateByCurve[curveData]);
            }
        }
        public void ChangeToggleState(bool isOn)
        {
            OnDisplayGroup.Invoke(m_Group, isOn);
            m_CurvesRectTransform.gameObject.SetActive(isOn);
            ChangeColor(isOn);
        }
        #endregion

        #region Private Methods
        void AddLegend(CurveData curve, bool active)
        {
            Legend legend = Instantiate(m_CurvePrefab, m_CurvesRectTransform).GetComponent<Legend>();
            legend.name = curve.Name;
            legend.Set(curve, active);
            legend.OnDisplayCurve.RemoveAllListeners();
            legend.OnDisplayCurve.AddListener(OnDisplayCurve.Invoke);
        }
        void ChangeColor(bool isOn)
        {
            m_Text.color = isOn ? Color.white : Color.grey;
        }
        #endregion
    }
}