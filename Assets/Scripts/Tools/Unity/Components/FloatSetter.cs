using UnityEngine;
using UnityEngine.Events;

namespace Tools.Unity.Components
{
    public class FloatSetter : MonoBehaviour
    {
        #region Properties
        public float Value
        {
            set
            {
                OnChangeValue.Invoke(value);
            }
        }
        public FloatEvent OnChangeValue;
        #endregion
    }
}
