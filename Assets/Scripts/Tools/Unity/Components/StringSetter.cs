using UnityEngine;
using UnityEngine.Events;

namespace Tools.Unity.Components
{
    public class StringSetter : MonoBehaviour
    {
        #region Properties
        public string Value
        {
            set
            {
                OnChangeValue.Invoke(value);
            }
        }
        public StringEvent OnChangeValue;
        #endregion
    }
}