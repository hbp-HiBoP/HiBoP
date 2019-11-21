using UnityEngine;
using UnityEngine.UI;

namespace Tools.Unity.Lists
{
    public class BaseList : MonoBehaviour
    {
        #region Properties
        public enum Sorting { Ascending, Descending };

        [HideInInspector, SerializeField] protected float m_ItemHeight;

        [SerializeField] protected bool m_Interactable = true;
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

        public GameObject ItemPrefab;
        public ScrollRect ScrollRect;


        protected int m_MaximumNumberOfItems;
        protected const int NUMBER_OF_ADDITIONAL_ITEMS = 1;

        protected int m_FirstIndexDisplayed;
        protected int m_LastIndexDisplayed;
        #endregion

        #region Private Methods
        protected void OnValidate()
        {
            Interactable = Interactable;
            CalculateItemHeight();
        }
        protected void CalculateItemHeight()
        {
            if(ItemPrefab != null)
            {
                var layoutElement = ItemPrefab.GetComponent<LayoutElement>();
                if (layoutElement != null) m_ItemHeight = layoutElement.preferredHeight;
            }
        }
        #endregion
    }
}