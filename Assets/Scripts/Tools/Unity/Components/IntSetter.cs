using UnityEngine;
using UnityEngine.Events;

namespace Tools.Unity.Components
{
    public class IntSetter : MonoBehaviour
    {
        #region Properties
        public int Value
        {
            set
            {
                OnChangeValue.Invoke(value);
            }
        }
        public IntEvent OnChangeValue;
        #endregion
    }
}
