using System;
using Tools.Unity;
using UnityEngine;

namespace Theme.Components
{
    [ExecuteInEditMode]
    public class ThemeElement : MonoBehaviour
    {
        #region Properties
        public State ActualState;
        public Element Element;
        #endregion

        #region Public Methods
        public void Set()
        {
            try
            {
                ActualState = Element.Set(gameObject);
            }
            catch(Exception e)
            {
                ActualState = null;
                Debug.LogError("Missing theme element at " + gameObject.transform.GetFullName());
                throw e;
            }
        }
        public void Set(State state)
        {
            try
            {
                ActualState = Element.Set(gameObject, state);
            }
            catch(Exception e)
            {
                ActualState = null;
                Debug.LogError("Missing theme element at " + gameObject.transform.GetFullName());
                throw e;
            }
        }
        #endregion

        #region Private Methods
        void Awake()
        {
            Set();
        }
        #endregion
    }
}
