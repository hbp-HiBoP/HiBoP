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

        public virtual bool Interactable { get; set; }

        protected int m_MaximumNumberOfItems;
        protected const int NUMBER_OF_ADDITIONAL_ITEMS = 1;

        protected int m_FirstIndexDisplayed;
        protected int m_LastIndexDisplayed;
        #endregion
    }
}