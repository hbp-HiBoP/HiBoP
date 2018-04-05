using UnityEngine;
using UnityEngine.Events;

namespace Tools.Unity.Components
{
    public class ToggleMethodCaller : MonoBehaviour
    {
        #region Properties
        public UnityEvent OnIsOn;
        public UnityEvent OnIsOff;
        #endregion

        #region Public Methods
        public void OnValueChanged(bool isOn)
        {
            if (isOn) OnIsOn.Invoke();
            else OnIsOff.Invoke();
        }
        #endregion
    }
}

