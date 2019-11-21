using UnityEngine;
using UnityEngine.UI;

namespace Tools.Unity.Lists
{
    public class BaseList : MonoBehaviour
    {
        #region Properties
        public enum Sorting { Ascending, Descending };

        public ScrollRect ScrollRect;
        public GameObject ItemPrefab;
        public float ItemHeight;

        [SerializeField] bool m_Interactable = true;
        public virtual bool Interactable
        {
            get
            {
                return m_Interactable;
            }
            set
            {
                m_Interactable = value;
            }
        }

        protected int m_MaximumNumberOfItems;
        protected const int NUMBER_OF_ADDITIONAL_ITEMS = 1;

        protected int m_FirstIndexDisplayed;
        protected int m_LastIndexDisplayed;
        #endregion

        #region Private Methods
        private void OnValidate()
        {
            Interactable = Interactable;
        }
        #endregion
    }
}