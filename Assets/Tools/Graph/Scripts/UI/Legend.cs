using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Tools.Unity.Graph
{
    public class Legend : MonoBehaviour
    {
        #region Properties
        [SerializeField] Text m_Text;
        [SerializeField] Image m_Image;
        [SerializeField] Toggle m_Toggle;

        CurveData m_Curve;
        static Color m_InactiveColor = Color.grey;

        public GenericEvent<CurveData, bool> OnDisplayCurve = new GenericEvent<CurveData,bool>();
        #endregion

        #region Public Methods
        public void Set(CurveData curve, bool active)
        {
            name = curve.name;
            m_Curve = curve;
            m_Text.text = curve.name;

            m_Toggle.isOn = active;
            ChangeColor(active);
        }
        public void OnChangeValue(bool isOn)
        {
            OnDisplayCurve.Invoke(m_Curve, isOn);
            ChangeColor(isOn);
        }
        #endregion

        #region Private Methods

        void ChangeColor(bool isOn)
        {
            m_Text.color = isOn ? Color.white : Color.grey;
            m_Image.color = isOn ? m_Curve.Color : Color.grey;
        }
        #endregion
    }
}