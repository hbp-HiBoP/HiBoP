using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Tools.Unity.Lists
{
    public class List<T> : BaseList
    {
        #region Properties
        protected System.Collections.Generic.List<Item<T>> m_Items = new System.Collections.Generic.List<Item<T>>();
        protected int m_NumberOfItems;

        protected System.Collections.Generic.List<T> m_Objects = new System.Collections.Generic.List<T>();
        public virtual ReadOnlyCollection<T> Objects
        {
            get
            {
                return new ReadOnlyCollection<T>(m_Objects);
            }
        }
        protected int m_NumberOfObjects;

        public UnityEvent<T> OnAddObject { get; } = new GenericEvent<T>();
        public UnityEvent<T> OnRemoveObject { get; } = new GenericEvent<T>();
        public UnityEvent<T> OnUpdateObject { get; } = new GenericEvent<T>();

        public override bool Interactable
        {
            get => base.Interactable;
            set
            {
                base.Interactable = value;
                foreach (var item in m_Items) item.Interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public virtual void Set(IEnumerable<T> objects)
        {
            Remove(m_Objects.ToArray());
            Add(objects);
        }
        public virtual bool Add(T obj)
        {
            if (!m_Objects.Contains(obj))
            {
                m_Objects.Add(obj);
                m_NumberOfObjects++;
                ScrollRect.content.sizeDelta += new Vector2(0, m_ItemHeight);
                ScrollRect.content.hasChanged = true;
                OnAddObject.Invoke(obj);
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
                OnRemoveObject.Invoke(obj);
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
        public virtual bool UpdateObject(T obj)
        {
            if (GetItemFromObject(obj, out Item<T> item))
            {
                item.Object = obj;
                int index = m_Objects.FindIndex(o => o.Equals(obj));
                m_Objects[index] = obj;
                OnUpdateObject.Invoke(obj);
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
        public virtual void RefreshPosition()
        {
            int itemsLength = m_Items.Count;
            for (int i = m_FirstIndexDisplayed, j = 0; i <= m_LastIndexDisplayed && j < itemsLength; i++, j++)
            {
                Item<T> item = m_Items[j];
                item.transform.localPosition = new Vector3(item.transform.localPosition.x, -i * m_ItemHeight, item.transform.localPosition.z);
            }
        }
        public virtual void ScrollToObject(T objectToScroll)
        {
            GetLimits(out int min, out int max);
            int index = m_Objects.IndexOf(objectToScroll);
            if (index == -1) return;

            if (index > max - 2)
            {
                float bottomOfTargetItem = m_ItemHeight * (index + 1);
                float position = 1.0f - ((bottomOfTargetItem - ScrollRect.viewport.sizeDelta.y) / (ScrollRect.content.sizeDelta.y - ScrollRect.viewport.sizeDelta.y));
                ScrollRect.verticalNormalizedPosition = Mathf.Clamp(position, 0f, 1f);
                ScrollRect.content.hasChanged = true;
            }
            else if (index < min + 1)
            {
                float topOfTargetItem = m_ItemHeight * index;
                float position = 1.0f - (topOfTargetItem / (ScrollRect.content.sizeDelta.y - ScrollRect.viewport.sizeDelta.y));
                ScrollRect.verticalNormalizedPosition = Mathf.Clamp(position, 0f, 1f);
                ScrollRect.content.hasChanged = true;
            }
        }
        #endregion

        #region Private Methods
        protected virtual void Display()
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
            else if (sizeDifference < 0)
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
        protected virtual void UpdateContent()
        {
            ScrollRect.content.sizeDelta = new Vector2(ScrollRect.content.sizeDelta.x, ScrollRect.content.sizeDelta.y - m_ItemHeight);
            if (ScrollRect.content.localPosition.y > m_ItemHeight)
            {
                if (m_NumberOfObjects >= m_MaximumNumberOfItems - 1)
                {
                    ScrollRect.content.localPosition = new Vector3(ScrollRect.content.localPosition.x, ScrollRect.content.localPosition.y - m_ItemHeight, ScrollRect.content.localPosition.z);

                    if (m_NumberOfObjects >= m_MaximumNumberOfItems)
                    {
                        foreach (var item in m_Items)
                        {
                            item.transform.localPosition = new Vector3(item.transform.localPosition.x, item.transform.localPosition.y + m_ItemHeight, item.transform.localPosition.z);
                        }
                    }
                }
            }
            ScrollRect.verticalScrollbar = ScrollRect.verticalScrollbar;
            ScrollRect.content.hasChanged = true;
        }
        protected virtual void SpawnItem(int numberOfItemToSpawn, bool spawnOnTop)
        {
            for (int i = 0; i < numberOfItemToSpawn; i++)
            {
                int index = spawnOnTop ? (m_FirstIndexDisplayed - 1) - i : m_LastIndexDisplayed + i;
                SpawnItemAt(index);
            }
        }
        protected virtual void SpawnItemAt(int index)
        {
            T obj = m_Objects[index];
            Item<T> item = Instantiate(ItemPrefab, ScrollRect.content).GetComponent<Item<T>>();
            RectTransform itemRectTransform = item.transform as RectTransform;
            itemRectTransform.sizeDelta = new Vector2(0, itemRectTransform.sizeDelta.y);
            itemRectTransform.localPosition = new Vector3(itemRectTransform.localPosition.x, -index * m_ItemHeight, itemRectTransform.localPosition.z);
            item.Interactable = Interactable;
            m_Items.Add(item);
            m_NumberOfItems++;
            SetItem(item, obj);
        }
        protected virtual void SetItem(Item<T> item, T obj)
        {
            item.Object = obj;
        }
        protected virtual void DestroyItem(int numberOfItemToDestroy, bool destroyOnTop)
        {
            Item<T>[] items = m_Items.OrderByDescending((item) => item.transform.localPosition.y).ToArray();
            int numberOfItems = m_NumberOfItems;
            for (int i = 0; i < numberOfItemToDestroy; i++)
            {
                int index = destroyOnTop ? i : (numberOfItems - 1) - i;
                DestroyItem(items[index]);
            }
        }
        protected virtual void DestroyItem(Item<T> item)
        {
            Destroy(item.gameObject);
            m_Items.Remove(item);
            m_NumberOfItems--;
        }
        protected virtual void MoveItemsUpwards(int deplacement)
        {
            Item<T>[] items = m_Items.OrderByDescending((item) => item.transform.localPosition.y).ToArray();
            int startIndex = deplacement > m_NumberOfItems ?deplacement - m_NumberOfItems : 0;
            for (int i = startIndex; i < deplacement; i++)
            {
                int itemID = ((m_NumberOfItems - 1 - i) % m_NumberOfItems + m_NumberOfItems) % m_NumberOfItems;
                int objID = m_FirstIndexDisplayed - 1 - i;
                Item<T> item = items[itemID];
                T newObj = m_Objects[objID];
                item.transform.localPosition = new Vector3(item.transform.localPosition.x, -objID * m_ItemHeight, item.transform.localPosition.z);
                SetItem(item, newObj);
            }
        }
        protected virtual void MoveItemsDownwards(int deplacement)
        {
            Item<T>[] items = m_Items.OrderByDescending((item) => item.transform.localPosition.y).ToArray();
            int startIndex = deplacement > m_NumberOfItems ? deplacement - m_NumberOfItems : 0;
            for (int i = startIndex; i < deplacement; i++)
            {
                int itemID = (i % m_NumberOfItems + m_NumberOfItems) % m_NumberOfItems;
                int objID = m_LastIndexDisplayed + i;
                Item<T> item = items[itemID];
                T newObj = m_Objects[objID];
                item.transform.localPosition = new Vector3(item.transform.localPosition.x, -objID * m_ItemHeight, item.transform.localPosition.z);
                SetItem(item, newObj);
            }
        }
        protected virtual void GetLimits(out int firstIndexDisplayed, out int lastIndexDisplayed)
        {
            firstIndexDisplayed = Mathf.FloorToInt((ScrollRect.content.localPosition.y / ScrollRect.content.sizeDelta.y) * m_NumberOfObjects);
            lastIndexDisplayed = Mathf.CeilToInt(((ScrollRect.content.localPosition.y + ScrollRect.viewport.sizeDelta.y) / ScrollRect.content.sizeDelta.y) * m_NumberOfObjects);

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
        protected virtual bool GetItemFromObject(T obj, out Item<T> itemToReturn)
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
        void Update()
        {
            if (ScrollRect.viewport.hasChanged)
            {
                m_MaximumNumberOfItems = Mathf.CeilToInt(ScrollRect.viewport.rect.height / m_ItemHeight) + NUMBER_OF_ADDITIONAL_ITEMS;
                ScrollRect.verticalNormalizedPosition = Mathf.Clamp(ScrollRect.verticalNormalizedPosition, 0f, 1f);
                ScrollRect.viewport.hasChanged = false;
            }
            if (ScrollRect.content.hasChanged)
            {
                Display();
                ScrollRect.content.hasChanged = false;
            }
        }
        #endregion
    }
}