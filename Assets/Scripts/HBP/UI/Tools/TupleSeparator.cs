using UnityEngine;
using UnityEngine.Events;

namespace HBP.UI.Tools
{
    public class TupleSeparator : MonoBehaviour
    {
        [SerializeField]FloatEvent m_OnChangeFirstValue;
        [SerializeField]FloatEvent m_OnChangeSecondValue;

        public void OnChangeTuple(float v1, float v2)
        {
            m_OnChangeFirstValue.Invoke(v1);
            m_OnChangeSecondValue.Invoke(v2);
        }

        [System.Serializable]
        public class FloatEvent : UnityEvent<float> { }
    }
}