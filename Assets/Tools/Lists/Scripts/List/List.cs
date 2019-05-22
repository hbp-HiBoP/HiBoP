using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Tools.Unity.Lists
{
    [RequireComponent(typeof(ScrollRect))]
    public class List<T> : MonoBehaviour
    {
        #region Properties
        public GameObject ItemPrefab;
        public float ItemHeight;

        public enum Sorting { Ascending, Descending};

        protected System.Collections.Generic.List<Item<T>> m_Items = new System.Collections.Generic.List<Item<T>>();
        protected int m_NumberOfItems;

        protected System.Collections.Generic.List<T> m_Objects = new System.Collections.Generic.List<T>();
        public virtual T[] Objects
        {
            get
            {
                return m_Objects.ToArray();
            }
            set
            {
                Remove(m_Objects.ToArray());
                Add(value);
            }
        }
        protected int m_NumberOfObjects;

        protected bool m_Interactable;
        public bool Interactable
        {
            get
            {
                return m_Interactable;
            }
            set
            {
                m_Interactable = value;
                foreach (var item in m_Items) item.interactable = value;
            }
        }

        protected ScrollRect m_ScrollRect;
        protected int m_MaximumNumberOfItems;
        protected const int NUMBER_OF_ADDITIONAL_ITEMS = 1;

        protected int m_FirstIndexDisplayed;
        protected int m_LastIndexDisplayed;
        protected bool m_Initialized;
        #endregion

        #region Public Methods
        public virtual bool Add(T obj)
        {
            if (!m_Objects.Contains(obj))
            {
                m_Objects.Add(obj);
                m_NumberOfObjects++;
                m_ScrollRect.content.sizeDelta += new Vector2(0, ItemHeight);
                m_ScrollRect.content.hasChanged = true;
                return true;
            }
            return false;
        }
        public virtual bool Add(IEnumerable<T> objectsToAdd)
        {
            bool result = true;
            foreach (T obj in objectsToAdd) result &= Add(obj);
            return result;
        }
        public virtual bool Remove(T obj)
        {
            if (m_Objects.Contains(obj))
            {
                if (m_NumberOfObjects <= m_MaximumNumberOfItems)
                {
                    DestroyItem(1,false);
                }
                m_Objects.Remove(obj);
                m_NumberOfObjects--;
                UpdateContent();
                GetLimits(out m_FirstIndexDisplayed, out m_LastIndexDisplayed);
                Refresh();
                return true;
            }
            return false;
        }
        public virtual bool Remove(IEnumerable<T> objectsToRemove)
        {
            bool result = true;
            foreach (T obj in objectsToRemove) result &= Remove(obj);
            return result;
        }
        public virtual bool UpdateObject(T objectToUpdate)
        {
            Item<T> item;
            if (GetItemFromObject(objectToUpdate, out item))
            {
                item.Object = objectToUpdate;
                return true;
            }
            return false;
        }
        public virtual void Refresh()
        {
            Item<T>[] items =  m_Items.OrderByDescending((item) => item.transform.localPosition.y).ToArray();
            for (int i = m_FirstIndexDisplayed, j=0; i < m_LastIndexDisplayed && j < m_NumberOfItems; i++, j++)
            {
                items[j].Object = m_Objects[i];
            }
        }
        public void RefreshPosition()
        {
            int itemsLength = m_Items.Count;
            for (int i = m_FirstIndexDisplayed, j = 0; i <= m_LastIndexDisplayed && j < itemsLength; i++, j++)
            {
                Item<T> item = m_Items[j];
                item.transform.localPosition = new Vector3(item.transform.localPosition.x, -i * ItemHeight, item.transform.localPosition.z);
            }
        }
        public virtual bool Initialize()
        {
            if(!m_Initialized)
            {
                m_Objects = new System.Collections.Generic.List<T>();
                m_Items = new System.Collections.Generic.List<Item<T>>();
                m_ScrollRect = GetComponent<ScrollRect>();
                m_Initialized = true;
                return true;
            }
            return false;
        }
        public void ScrollToObject(T objectToScroll)
        {
            GetLimits(out int min, out int max);
            int index = m_Objects.IndexOf(objectToScroll);
            if (index == -1) return;

            if (index > max - 2)
            {
                float bottomOfTargetItem = ItemHeight * (index + 1);
                float position = 1.0f - ((bottomOfTargetItem - m_ScrollRect.viewport.sizeDelta.y) / (m_ScrollRect.content.sizeDelta.y - m_ScrollRect.viewport.sizeDelta.y));
                m_ScrollRect.verticalNormalizedPosition = Mathf.Clamp(position, 0f, 1f);
                m_ScrollRect.content.hasChanged = true;
            }
            else if (index < min + 1)
            {
                float topOfTargetItem = ItemHeight * index;
                float position = 1.0f - (topOfTargetItem / (m_ScrollRect.content.sizeDelta.y - m_ScrollRect.viewport.sizeDelta.y));
                m_ScrollRect.verticalNormalizedPosition = Mathf.Clamp(position, 0f, 1f);
                m_ScrollRect.content.hasChanged = true;
            }
        }
        #endregion

        #region Private Methods
        private void Awake()
        {
            Initialize();
        }
        void Update()
        {
            if (m_ScrollRect.viewport.hasChanged)
            {
                m_MaximumNumberOfItems = Mathf.CeilToInt(m_ScrollRect.viewport.rect.height / ItemHeight) + NUMBER_OF_ADDITIONAL_ITEMS;
                m_ScrollRect.verticalNormalizedPosition = Mathf.Clamp(m_ScrollRect.verticalNormalizedPosition, 0f, 1f);
                m_ScrollRect.viewport.hasChanged = false;
            }
            if (m_ScrollRect.content.hasChanged)
            {
                Display();
                m_ScrollRect.content.hasChanged = false;
            }
        }
        void Display()
        {
            int newFirstIndexDisplayed, newLastIndexDisplayed; GetLimits(out newFirstIndexDisplayed, out newLastIndexDisplayed);
            int firstIndexDifference = newFirstIndexDisplayed - m_FirstIndexDisplayed;
            int lastIndexDifference = newLastIndexDisplayed - m_LastIndexDisplayed;

            // Resize viewport and list.
            int sizeDifference = lastIndexDifference - firstIndexDifference;
            if (sizeDifference > 0)
            {
                int numberOfItemToSpawnOnBot = Mathf.Min(sizeDifference, m_NumberOfObjects - m_LastIndexDisplayed);
                int numberOfItemToSpawnOnTop = sizeDifference - numberOfItemToSpawnOnBot;
                SpawnItem(numberOfItemToSpawnOnBot, false);
                SpawnItem(numberOfItemToSpawnOnTop, true);
                m_FirstIndexDisplayed -= numberOfItemToSpawnOnTop;
                m_LastIndexDisplayed += numberOfItemToSpawnOnBot;
            }
            else if(sizeDifference < 0)
            {
                int numberOfItemToDestroyOnBot = Mathf.Abs(sizeDifference);
                DestroyItem(numberOfItemToDestroyOnBot, false);
                m_LastIndexDisplayed -= numberOfItemToDestroyOnBot;
            }
            
            // Move items.
            int deplacement = newFirstIndexDisplayed - m_FirstIndexDisplayed;
            if (deplacement > 0)
            {
                MoveItemsDownwards(Mathf.Abs(deplacement));
            }
            else if (deplacement < 0)
            {
                MoveItemsUpwards(Mathf.Abs(deplacement));
            }

            m_FirstIndexDisplayed = newFirstIndexDisplayed;
            m_LastIndexDisplayed = newLastIndexDisplayed;

            Refresh();
        }
        void UpdateContent()
        {
            m_ScrollRect.content.sizeDelta = new Vector2(m_ScrollRect.content.sizeDelta.x, m_ScrollRect.content.sizeDelta.y - ItemHeight);
            if (m_ScrollRect.content.localPosition.y > ItemHeight)
            {
                if (m_NumberOfObjects >= m_MaximumNumberOfItems - 1)
                {
                    m_ScrollRect.content.localPosition = new Vector3(m_ScrollRect.content.localPosition.x, m_ScrollRect.content.localPosition.y - ItemHeight, m_ScrollRect.content.localPosition.z);

                    if (m_NumberOfObjects >= m_MaximumNumberOfItems)
                    {
                        foreach (var item in m_Items)
                        {
                            item.transform.localPosition = new Vector3(item.transform.localPosition.x, item.transform.localPosition.y + ItemHeight, item.transform.localPosition.z);
                        }
                    }
                }
            }
            m_ScrollRect.verticalScrollbar = m_ScrollRect.verticalScrollbar;
            m_ScrollRect.content.hasChanged = true;
        }
        protected void SpawnItem(int numberOfItemToSpawn, bool spawnOnTop)
        {
            for (int i = 0; i < numberOfItemToSpawn; i++)
            {
                int index = spawnOnTop ? (m_FirstIndexDisplayed - 1) - i : m_LastIndexDisplayed + i;
                SpawnItemAt(index);
            }
        }
        protected void SpawnItemAt(int index)
        {
            T obj = m_Objects[index];
            Item<T> item = Instantiate(ItemPrefab, m_ScrollRect.content).GetComponent<Item<T>>();
            RectTransform itemRectTransform = item.transform as RectTransform;
            itemRectTransform.sizeDelta = new Vector2(0, itemRectTransform.sizeDelta.y);
            itemRectTransform.localPosition = new Vector3(itemRectTransform.localPosition.x, -index * ItemHeight, itemRectTransform.localPosition.z);
            m_Items.Add(item);
            m_NumberOfItems++;
            SetItem(item, obj);
        }
        protected virtual void SetItem(Item<T> item, T obj)
        {
            item.Object = obj;
        }
        protected void DestroyItem(int numberOfItemToDestroy, bool destroyOnTop)
        {
            Item<T>[] items = m_Items.OrderByDescending((item) => item.transform.localPosition.y).ToArray();
            int numberOfItems = m_NumberOfItems;
            for (int i = 0; i < numberOfItemToDestroy; i++)
            {
                int index = destroyOnTop ? i : (numberOfItems - 1) - i;
                DestroyItem(items[index]);
            }
        }
        protected void DestroyItem(Item<T> item)
        {
            Destroy(item.gameObject);
            m_Items.Remove(item);
            m_NumberOfItems--;
        }
        protected void MoveItemsUpwards(int deplacement)
        {
            Item<T>[] items = m_Items.OrderByDescending((item) => item.transform.localPosition.y).ToArray();
            int startIndex = deplacement > m_NumberOfItems ?deplacement - m_NumberOfItems : 0;
            for (int i = startIndex; i < deplacement; i++)
            {
                int itemID = ((m_NumberOfItems - 1 - i) % m_NumberOfItems + m_NumberOfItems) % m_NumberOfItems;
                int objID = m_FirstIndexDisplayed - 1 - i;
                Item<T> item = items[itemID];
                T newObj = m_Objects[objID];
                item.transform.localPosition = new Vector3(item.transform.localPosition.x, -objID * ItemHeight, item.transform.localPosition.z);
                SetItem(item, newObj);
            }
        }
        protected void MoveItemsDownwards(int deplacement)
        {
            Item<T>[] items = m_Items.OrderByDescending((item) => item.transform.localPosition.y).ToArray();
            int startIndex = deplacement > m_NumberOfItems ? deplacement - m_NumberOfItems : 0;
            for (int i = startIndex; i < deplacement; i++)
            {
                int itemID = (i % m_NumberOfItems + m_NumberOfItems) % m_NumberOfItems;
                int objID = m_LastIndexDisplayed + i;
                Item<T> item = items[itemID];
                T newObj = m_Objects[objID];
                item.transform.localPosition = new Vector3(item.transform.localPosition.x, -objID * ItemHeight, item.transform.localPosition.z);
                SetItem(item, newObj);
            }
        }
        protected void GetLimits(out int firstIndexDisplayed, out int lastIndexDisplayed)
        {
            firstIndexDisplayed = Mathf.FloorToInt((m_ScrollRect.content.localPosition.y / m_ScrollRect.content.sizeDelta.y) * m_NumberOfObjects);
            lastIndexDisplayed = Mathf.CeilToInt(((m_ScrollRect.content.localPosition.y + m_ScrollRect.viewport.sizeDelta.y) / m_ScrollRect.content.sizeDelta.y) * m_NumberOfObjects);

            int firstIndexMaximumValue = Mathf.Max(0, m_NumberOfObjects - 1);
            int lastIndexMaximumValue = Mathf.Max(0, m_NumberOfObjects);
            firstIndexDisplayed = Mathf.Clamp(firstIndexDisplayed, 0, firstIndexMaximumValue);
            lastIndexDisplayed = Mathf.Clamp(lastIndexDisplayed, 0, lastIndexMaximumValue);
            if (lastIndexDisplayed >= m_NumberOfObjects && firstIndexDisplayed != 0)
            {
                firstIndexDisplayed = lastIndexDisplayed - m_MaximumNumberOfItems;
                firstIndexDisplayed = Mathf.Clamp(firstIndexDisplayed, 0, firstIndexMaximumValue);
            }
            else
            {
                lastIndexDisplayed = firstIndexDisplayed + m_MaximumNumberOfItems;
                lastIndexDisplayed = Mathf.Clamp(lastIndexDisplayed, 0, lastIndexMaximumValue);
            }
        }
        protected bool GetItemFromObject(T obj, out Item<T> itemToReturn)
        {
            foreach (var item in m_Items)
            {
                if (item.Object.Equals(obj))
                {
                    itemToReturn = item;
                    return true;
                }
            }
            itemToReturn = null;
            return false;
        }
        #endregion
    }
}
