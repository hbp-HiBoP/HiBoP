using UnityEngine;

namespace Tools.Unity
{
    public class ToggleMethodCaller : MonoBehaviour
    {
        public FunctionToCall FunctionsToCallOn;
        public FunctionToCall FunctionsToCallOff;

        public void ChangeValue()
        {
            if(transform.GetComponent<UnityEngine.UI.Toggle>().isOn)
            {
                FunctionsToCallOn.Send();
            }
            else
            {
                FunctionsToCallOff.Send();
            }
        }
    }
}

