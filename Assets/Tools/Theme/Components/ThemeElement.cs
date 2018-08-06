using System;
using UnityEngine;

namespace NewTheme.Components
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
            if(name == "Label" && transform.parent.parent.name == "Number of intermediate values")
            {
                Debug.Log("Set()");
            }
            try
            {
                ActualState = Element.Set(gameObject);
            }
            catch(Exception e)
            {
                ActualState = null;
                Debug.LogError("Missing theme element at " + gameObject.transform.FullName());
                throw e;
            }
        }
        public void Set(State state)
        {
            if (name == "Label" && transform.parent.parent.name == "Number of intermediate values")
            {
                Debug.Log("Set() "+ state.name);
            }
            try
            {
                ActualState = Element.Set(gameObject, state);
            }
            catch(Exception e)
            {
                ActualState = null;
                Debug.LogError("Missing theme element at " + gameObject.transform.FullName());
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
