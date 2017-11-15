using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Tools.Unity.Graph
{
    public class Legend : MonoBehaviour
    {
        #region Properties
        [SerializeField] Text m_Text;
        [SerializeField] Toggle m_Toggle;

        CurveData m_Curve;
        static Color m_InactiveColor = Color.grey;

        public GenericEvent<CurveData,bool> OnDisplayCurve = new GenericEvent<CurveData,bool>();
        #endregion

        #region Public Methods
        public void Set(CurveData curve, bool active)
        {
            name = curve.Label;
            m_Curve = curve;
            m_Text.color = curve.Color;
            m_Text.text = curve.Label;

            m_Toggle.isOn = active;
            ChangeColor(active);
            m_Toggle.onValueChanged.AddListener(ChangeToggleState);
        }
        #endregion

        #region Private Methods
        void ChangeToggleState(bool isOn)
        {
            OnDisplayCurve.Invoke(m_Curve, isOn);
        }
        void ChangeColor(bool isOn)
        {
            m_Text.color = isOn ? m_Curve.Color : m_InactiveColor;
        }
        #endregion
    }
}