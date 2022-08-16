using UnityEngine;
using UnityEngine.Events;

namespace HBP.UI.Components
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