using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Tools.Lists
{
    /// <summary>
    /// Base component for every UI list.
    /// </summary>
    public class BaseList : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// Sorting Type.
        /// </summary>
        public enum Sorting { Ascending, Descending };

        /// <summary>
        /// Height of a item of the list.
        /// </summary>
        [HideInInspector, SerializeField] protected float m_ItemHeight;

        [SerializeField] protected bool m_Interactable = true;
        /// <summary>
        /// Use to enable or disable the ability to select a selectable UI element (for example, a Button).
        /// </summary>
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

        /// <summary>
        /// Item list prefab instantiate to display a element of the list.
        /// </summary>
        public GameObject ItemPrefab;
        /// <summary>
        /// ScrollRect to scroll the list.
        /// </summary>
        public ScrollRect ScrollRect;

        /// <summary>
        /// Maximumn number of items displayed at the same time.
        /// </summary>
        protected int m_MaximumNumberOfItems;
        /// <summary>
        /// Number of additional items to make seamless effect.
        /// </summary>
        protected const int NUMBER_OF_ADDITIONAL_ITEMS = 1;

        /// <summary>
        /// First element index displayed at the top of the scroll view.
        /// </summary>
        protected int m_FirstIndexDisplayed;
        /// <summary>
        /// Last element index displayed at the bottom of the scroll view.
        /// </summary>
        protected int m_LastIndexDisplayed;
        #endregion

        #region Private Methods
        void OnValidate()
        {
            Validate();
        }
        /// <summary>
        /// Calculate the item height of the item prefab. Use the preferredHeight of the layoutElement.
        /// </summary>
        protected void CalculateItemHeight()
        {
            if (ItemPrefab != null)
            {
                var layoutElement = ItemPrefab.GetComponent<LayoutElement>();
                if (layoutElement != null) m_ItemHeight = layoutElement.preferredHeight;
            }
        }
        /// <summary>
        /// Called on OnValidate(). You can override this method and use it to make all work needed OnValidate().
        /// </summary>
        protected virtual void Validate()
        {
            Interactable = Interactable;
            CalculateItemHeight();
        }
        #endregion
    }
}