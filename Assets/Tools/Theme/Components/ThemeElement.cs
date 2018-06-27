using System;
using UnityEngine;

namespace NewTheme.Components
{
    [ExecuteInEditMode]
    public class ThemeElement : MonoBehaviour
    {
        #region Properties
        public Element Element;
        #endregion

        #region Public Methods
        public void Set()
        {
            try
            {
                Element.Set(gameObject);
            }
            catch(Exception e)
            {
                Debug.LogError(gameObject.transform.FullName());
                throw e;
            }
        }
        public void Set(State state)
        {
            try
            {
                Element.Set(gameObject, state);
            }
            catch(Exception e)
            {
                Debug.LogError(gameObject.transform.FullName());
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
