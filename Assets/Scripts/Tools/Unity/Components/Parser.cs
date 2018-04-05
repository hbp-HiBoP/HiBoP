using UnityEngine;

namespace Tools.Unity.Components
{
    public class Parser : MonoBehaviour
    {
        #region Properties
        public string Format;

        public StringEvent OnStringResult;
        public FloatEvent OnFloatResult;
        #endregion

        #region Public Methods
        public void ParseToFloat(string value)
        {
            float result;
            if(float.TryParse(value,out result))
            {
                OnFloatResult.Invoke(result);
            }
        }
        public void Parse(float value)
        {
            string result = value.ToString(Format);
            OnStringResult.Invoke(result);
        }
        #endregion

        #region Private Methods

        #endregion
    }
}