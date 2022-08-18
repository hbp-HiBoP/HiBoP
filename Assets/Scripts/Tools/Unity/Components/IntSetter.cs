﻿using UnityEngine;
using UnityEngine.Events;

namespace HBP.UI.Tools
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
