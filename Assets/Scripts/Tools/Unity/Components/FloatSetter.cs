using UnityEngine;
using UnityEngine.Events;

namespace HBP.UI.Tools
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
