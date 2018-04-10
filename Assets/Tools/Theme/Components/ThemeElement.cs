using System.Linq;
using UnityEngine;

namespace NewTheme
{
    public class ThemeElement : MonoBehaviour
    {
        #region Properties
        public State Default;
        public ElementsByState[] ElementsByState;
        #endregion

        #region Public Methods
        public void Set()
        {
            Set(Default);
        }

        public void Set(State state)
        {
            Element[] Elements = ElementsByState.FirstOrDefault((t) => t.State == state).Elements;
            foreach (var element in Elements)
            {
                if (element)
                {
                    element.Set(gameObject);
                }
            }
        }
        #endregion

        #region Private Methods
        private void Start()
        {
            Set();
        }
        #endregion
    }
}
