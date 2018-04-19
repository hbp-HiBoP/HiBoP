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
            Element.Set(gameObject);
        }
        public void Set(State state)
        {
            Element.Set(gameObject, state);
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
