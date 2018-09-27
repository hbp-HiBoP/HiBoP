using UnityEngine;

namespace Tools.Unity.Components
{
    public class Parser : MonoBehaviour
    {
        #region Properties
        public string Format;

        public StringEvent OnStringResult;
        public FloatEvent OnFloatResult;
        public IntEvent OnIntResult;
        #endregion

        #region Public Methods
        public void ParseFromString(string value)
        {
            float floatResult;
            if(float.TryParse(value,out floatResult))
            {
                OnFloatResult.Invoke(floatResult);
            }
            int intResult;
            if (int.TryParse(value, out intResult))
            {
                OnIntResult.Invoke(intResult);
            }
        }
        public void ParseFromInt(int value)
        {
            string result = value.ToString(Format);
            OnStringResult.Invoke(result);
        }
        public void ParseFromFloat(float value)
        {
            string result = value.ToString(Format);
            OnStringResult.Invoke(result);
        }
        #endregion

        #region Private Methods

        #endregion
    }
}