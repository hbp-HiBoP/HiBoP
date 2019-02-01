using System.Globalization;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI.Extensions;

namespace Tools.Unity.Components
{
    public class Parser : MonoBehaviour
    {
        #region Properties
        [SerializeField] string m_Format = "N2";
        public string Format
        {
            get
            {
                return m_Format;
            }
            set
            {
                SetPropertyUtility.SetClass(ref m_Format, value);
            }
        }

        [SerializeField] string m_CultureInfo = "en-US";
        public string CultureInfo
        {
            get
            {
                return m_CultureInfo;
            }
            set
            {
                SetPropertyUtility.SetClass(ref m_CultureInfo, value);
            }
        }

        [SerializeField] StringEvent m_OnChangeStringResult;
        public StringEvent OnChangeStringResult
        {
            get
            {
                return m_OnChangeStringResult;
            }
        }

        [SerializeField] FloatEvent m_OnChangeFloatResult;
        public FloatEvent OnChangeFloatResult
        {
            get
            {
                return m_OnChangeFloatResult;
            }
        }

        [SerializeField] IntEvent m_OnChangeIntResult;
        public IntEvent OnChangeIntResult
        {
            get
            {
                return m_OnChangeIntResult;
            }
        }
        #endregion

        #region Public Methods
        public void ParseFromString(string value)
        {
            if (NumberExtension.TryParseFloat(value, out float floatResult))
            {
                m_OnChangeFloatResult.Invoke(floatResult);
            }
            if (int.TryParse(value, out int intResult))
            {
                m_OnChangeIntResult.Invoke(intResult);
            }
        }
        public void ParseFromInt(int value)
        {
            CultureInfo cultureInfo = System.Globalization.CultureInfo.GetCultureInfo(CultureInfo);
            string result = value.ToString(Format, cultureInfo);
            m_OnChangeStringResult.Invoke(result);
        }
        public void ParseFromFloat(float value)
        {
            CultureInfo cultureInfo = System.Globalization.CultureInfo.GetCultureInfo(CultureInfo);
            string result = value.ToString(Format, cultureInfo);
            m_OnChangeStringResult.Invoke(result);
        }
        #endregion
    }
}