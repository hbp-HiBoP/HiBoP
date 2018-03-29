using UnityEngine;
using UnityEngine.UI;

namespace Tools.Unity
{
    [RequireComponent(typeof(Toggle))]
    public class ToggleMethodCaller : MonoBehaviour
    {
        Toggle toggle;
        public FunctionToCall OnMethod;
        public FunctionToCall OffMethod;

        void Start()
        {
            toggle = GetComponent<Toggle>();
            toggle.onValueChanged.AddListener((isOn) => CallMethods(isOn));
        }

        void CallMethods(bool isOn)
        {
            if(isOn) OnMethod.Send();
            else OffMethod.Send();
        }
    }
}

