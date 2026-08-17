using UnityEngine;
using UnityEngine.Events;
using HBP.Core.Tools;

namespace HBP.UI.Tools
{
    public class StringSetter : MonoBehaviour
    {
        #region Properties

        public string Value
        {
            set { OnChangeValue.Invoke(value); }
        }

        public StringEvent OnChangeValue;

        #endregion
    }
}
