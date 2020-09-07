using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.QuickStart
{
    public abstract class QuickStartPanel : MonoBehaviour
    {
        #region Properties
        public QuickStartPanel PreviousPanel;
        public QuickStartPanel NextPanel;
        #endregion

        #region Private Methods
        private void Awake()
        {
            Initialize();
        }
        protected virtual void Initialize()
        {

        }
        #endregion

        #region Public Methods
        public virtual void OpenPanel()
        {
            gameObject.SetActive(true);
        }
        public virtual void ClosePanel()
        {
            gameObject.SetActive(false);
        }
        #endregion
    }
}