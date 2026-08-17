using UnityEngine;
using UnityEngine.Events;
using HBP.Core.Tools;

namespace HBP.UI.Tools
{
    public class IntSetter : MonoBehaviour
    {
        #region Properties

        public int Value
        {
            set { OnChangeValue.Invoke(value); }
        }

        public IntEvent OnChangeValue;

        #endregion
    }
}
