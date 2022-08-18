using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Tools.Lists
{
    /// <summary>
    /// Base generic component for every UI list.
    /// </summary>
    public class List<T> : BaseList
    {
        #region Properties
        /// <summary>
        /// UI items displayed by the list.
        /// </summary>
        protected System.Collections.Generic.List<Item<T>> m_Items = new System.Collections.Generic.List<Item<T>>();

        protected System.Collections.Generic.List<T> m_Objects = new System.Collections.Generic.List<T>();
        /// <summary>
        /// Objects of the list.
        /// </summary>
        public virtual ReadOnlyCollection<T> Objects
        {
            get
            {
                return new ReadOnlyCollection<T>(m_Objects);
            }
        }
        /// <summary>
        /// List of the displayed objects.
        /// </summary>
        protected System.Collections.Generic.List<T> m_DisplayedObjects = new System.Collections.Generic.List<T>();

        /// <summary>
        /// Callback executed when a object is added.
        /// </summary> 
        public UnityEvent<T> OnAddObject { get; } = new GenericEvent<T>();
        /// <summary>
        /// Callback executed when a object is removed.
        /// </summary> 
        public UnityEvent<T> OnRemoveObject { get; } = new GenericEvent<T>();
        /// <summary>
        /// Callback executed when a object is updated.
        /// </summary> 
        public UnityEvent<T> OnUpdateObject { get; } = new GenericEvent<T>();

        /// <summary>
        /// Use to enable or disable the ability to select a selectable UI element (for example, a Button).
        /// </summary>
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
        /// <summary>
        /// Set the objects of the list.
        /// </summary>
        /// <param name="objects"></param>
        public virtual void Set(IEnumerable<T> objects)
        {
            var obj = objects.ToArray();
            Remove(m_Objects.ToArray());
            Add(obj);
        }
        /// <summary>
        /// Add a object to the list.
        /// </summary>
        /// <param name="obj">Object to add</param>
        /// <returns>True if added, False otherwise</returns>
        public virtual bool Add(T obj)
        {
            if (!m_Objects.Contains(obj))
            {
                m_Objects.Add(obj);
                if (!m_DisplayedObjects.Contains(obj))
                {
                    m_DisplayedObjects.Add(obj);
                    ScrollRect.content.sizeDelta += new Vector2(0, m_ItemHeight);
                    ScrollRect.content.hasChanged = true;
                }
                OnAddObject.Invoke(obj);
                return true;
            }
            return false;
        }
        /// <summary>
        /// Add multiple objects to the list.
        /// </summary>
        /// <param name="objectsToAdd">Objects to add</param>
        /// <returns>True if added, False otherwise</returns>
        public virtual bool Add(IEnumerable<T> objectsToAdd)
        {
            bool result = true;
            foreach (T obj in objectsToAdd.ToArray()) result &= Add(obj);
            return result;
        }
        /// <summary>
        /// Remove a object from the list.
        /// </summary>
        /// <param name="obj">Object to add</param>
        /// <returns>True if removed, False otherwise</returns>
        public virtual bool Remove(T obj)
        {
            if (m_Objects.Contains(obj))
            {
                m_Objects.Remove(obj);
                if (m_DisplayedObjects.Contains(obj))
                {
                    if (m_DisplayedObjects.Count <= m_MaximumNumberOfItems)
                    {
                        DestroyItem(1, false);
                    }
                    m_DisplayedObjects.Remove(obj);
                    UpdateContent();
                    GetLimits(out m_FirstIndexDisplayed, out m_LastIndexDisplayed);
                    Refresh();
                }
                OnRemoveObject.Invoke(obj);
                return true;
            }
            return false;
        }
        /// <summary>
        /// Remove multiple objects from the list.
        /// </summary>
        /// <param name="objectsToRemove">Objects to remove</param>
        /// <returns>True if removed, False otherwise</returns>
        public virtual bool Remove(IEnumerable<T> objectsToRemove)
        {
            bool result = true;
            foreach (T obj in objectsToRemove.ToArray()) result &= Remove(obj);
            return result;
        }
        /// <summary>
        /// Update a object from the list.
        /// </summary>
        /// <param name="objectToUpdate">Object to update.</param>
        /// <returns>True if updated, False otherwise</returns>
        public virtual bool UpdateObject(T objectToUpdate)
        {
            int index = m_Objects.FindIndex(o => o.Equals(objectToUpdate));
            m_Objects[index] = objectToUpdate;

            if (GetItemFromObject(objectToUpdate, out Item<T> item))
            {
                item.Object = objectToUpdate;
                OnUpdateObject.Invoke(objectToUpdate);
                return true;
            }
            return false;
        }
        /// <summary>
        /// Refresh all the list.
        /// </summary>
        public virtual void Refresh()
        {
            Item<T>[] items =  m_Items.OrderByDescending((item) => item.transform.localPosition.y).ToArray();
            for (int i = m_FirstIndexDisplayed, j=0; i < m_LastIndexDisplayed && j < m_Items.Count; i++, j++)
            {
                items[j].Object = m_DisplayedObjects[i];
            }
        }
        /// <summary>
        /// Refresh the position of the items.
        /// </summary>
        public virtual void RefreshPosition()
        {
            int itemsLength = m_Items.Count;
            for (int i = m_FirstIndexDisplayed, j = 0; i <= m_LastIndexDisplayed && j < itemsLength; i++, j++)
            {
                Item<T> item = m_Items[j];
                item.transform.localPosition = new Vector3(item.transform.localPosition.x, -i * m_ItemHeight, item.transform.localPosition.z);
            }
        }
        /// <summary>
        /// Scroll the list to the target object with the shortest scroll amount.
        /// </summary>
        /// <param name="objectToScroll">Target object</param>
        public virtual void ScrollToObject(T objectToScroll)
        {
            GetLimits(out int min, out int max);
            int index = m_DisplayedObjects.IndexOf(objectToScroll);
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
        /// <summary>
        /// Mask the list of objects to only display some of them
        /// </summary>
        /// <param name="mask">Mask for the list</param>
        public virtual bool MaskList(bool[] mask)
        {
            if (mask.Length != m_Objects.Count) return false;

            m_DisplayedObjects.Clear();
            for (int i = 0; i < mask.Length; i++)
            {
                if (mask[i]) m_DisplayedObjects.Add(m_Objects[i]);
            }
            ScrollRect.content.sizeDelta = new Vector2(0, m_ItemHeight * m_DisplayedObjects.Count);
            ScrollRect.content.hasChanged = true;

            return true;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Display all the items.
        /// </summary>
        protected virtual void Display()
        {
            int newFirstIndexDisplayed, newLastIndexDisplayed; GetLimits(out newFirstIndexDisplayed, out newLastIndexDisplayed);
            int firstIndexDifference = newFirstIndexDisplayed - m_FirstIndexDisplayed;
            int lastIndexDifference = newLastIndexDisplayed - m_LastIndexDisplayed;

            // Resize viewport and list.
            int sizeDifference = lastIndexDifference - firstIndexDifference;
            if (sizeDifference > 0)
            {
                int numberOfItemToSpawnOnBot = Mathf.Min(sizeDifference, m_DisplayedObjects.Count - m_LastIndexDisplayed);
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
        /// <summary>
        /// Update the content size and position to fit with the number of items displayed.
        /// </summary>
        protected virtual void UpdateContent()
        {
            ScrollRect.content.sizeDelta = new Vector2(ScrollRect.content.sizeDelta.x, ScrollRect.content.sizeDelta.y - m_ItemHeight);
            if (ScrollRect.content.localPosition.y > m_ItemHeight)
            {
                if (m_DisplayedObjects.Count >= m_MaximumNumberOfItems - 1)
                {
                    ScrollRect.content.localPosition = new Vector3(ScrollRect.content.localPosition.x, ScrollRect.content.localPosition.y - m_ItemHeight, ScrollRect.content.localPosition.z);

                    if (m_DisplayedObjects.Count >= m_MaximumNumberOfItems)
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
        /// <summary>
        /// Spawn new items.
        /// </summary>
        /// <param name="numberOfItemToSpawn">Number of items to spawn</param>
        /// <param name="spawnOnTop">True to spawn on top of the list view, False otherwise</param>
        protected virtual void SpawnItem(int numberOfItemToSpawn, bool spawnOnTop)
        {
            for (int i = 0; i < numberOfItemToSpawn; i++)
            {
                int index = spawnOnTop ? (m_FirstIndexDisplayed - 1) - i : m_LastIndexDisplayed + i;
                SpawnItemAt(index);
            }
        }
        /// <summary>
        /// Spawn a item at a specified index.
        /// </summary>
        /// <param name="index">Index </param>
        protected virtual void SpawnItemAt(int index)
        {
            T obj = m_DisplayedObjects[index];
            Item<T> item = Instantiate(ItemPrefab, ScrollRect.content).GetComponent<Item<T>>();
            RectTransform itemRectTransform = item.transform as RectTransform;
            itemRectTransform.sizeDelta = new Vector2(0, itemRectTransform.sizeDelta.y);
            itemRectTransform.localPosition = new Vector3(itemRectTransform.localPosition.x, -index * m_ItemHeight, itemRectTransform.localPosition.z);
            item.Interactable = Interactable;
            m_Items.Add(item);
            SetItem(item, obj);
        }
        /// <summary>
        /// Set a item with a object to display.
        /// </summary>
        /// <param name="item">Item to set.</param>
        /// <param name="obj">Object to display</param>
        protected virtual void SetItem(Item<T> item, T obj)
        {
            item.Object = obj;
        }
        /// <summary>
        /// Destroy items.
        /// </summary>
        /// <param name="numberOfItemToDestroy">Number of items to destroy</param>
        /// <param name="destroyOnTop">True to destroy on top of the list view, False otherwise</param>
        protected virtual void DestroyItem(int numberOfItemToDestroy, bool destroyOnTop)
        {
            Item<T>[] items = m_Items.OrderByDescending((item) => item.transform.localPosition.y).ToArray();
            int numberOfItems = m_Items.Count;
            for (int i = 0; i < numberOfItemToDestroy; i++)
            {
                int index = destroyOnTop ? i : (numberOfItems - 1) - i;
                DestroyItem(items[index]);
            }
        }
        /// <summary>
        /// Destroy a specific item.
        /// </summary>
        /// <param name="item">Item to destroy</param>
        protected virtual void DestroyItem(Item<T> item)
        {
            Destroy(item.gameObject);
            m_Items.Remove(item);
        }
        /// <summary>
        /// Move items upwards.
        /// </summary>
        /// <param name="deplacement">Steps</param>
        protected virtual void MoveItemsUpwards(int deplacement)
        {
            Item<T>[] items = m_Items.OrderByDescending((item) => item.transform.localPosition.y).ToArray();
            int numberOfItems = items.Length;
            int startIndex = deplacement > numberOfItems ? deplacement - numberOfItems : 0;
            for (int i = startIndex; i < deplacement; i++)
            {
                int itemID = ((numberOfItems - 1 - i) % numberOfItems + numberOfItems) % numberOfItems;
                int objID = m_FirstIndexDisplayed - 1 - i;
                Item<T> item = items[itemID];
                T newObj = m_DisplayedObjects[objID];
                item.transform.localPosition = new Vector3(item.transform.localPosition.x, -objID * m_ItemHeight, item.transform.localPosition.z);
                SetItem(item, newObj);
            }
        }
        /// <summary>
        /// Move items downwards.
        /// </summary>
        /// <param name="deplacement">Steps</param>
        protected virtual void MoveItemsDownwards(int deplacement)
        {
            Item<T>[] items = m_Items.OrderByDescending((item) => item.transform.localPosition.y).ToArray();
            int numberOfItems = items.Length;
            int startIndex = deplacement > numberOfItems ? deplacement - numberOfItems : 0;
            for (int i = startIndex; i < deplacement; i++)
            {
                int itemID = (i % numberOfItems + numberOfItems) % numberOfItems;
                int objID = m_LastIndexDisplayed + i;
                Item<T> item = items[itemID];
                T newObj = m_DisplayedObjects[objID];
                item.transform.localPosition = new Vector3(item.transform.localPosition.x, -objID * m_ItemHeight, item.transform.localPosition.z);
                SetItem(item, newObj);
            }
        }
        /// <summary>
        /// Get the limits of the elements displayed.
        /// </summary>
        /// <param name="firstIndexDisplayed">Index of the first element displayed</param>
        /// <param name="lastIndexDisplayed">Index of the last element displayed</param>
        protected virtual void GetLimits(out int firstIndexDisplayed, out int lastIndexDisplayed)
        {
            firstIndexDisplayed = Mathf.FloorToInt((ScrollRect.content.localPosition.y / ScrollRect.content.sizeDelta.y) * m_DisplayedObjects.Count);
            lastIndexDisplayed = Mathf.CeilToInt(((ScrollRect.content.localPosition.y + ScrollRect.viewport.sizeDelta.y) / ScrollRect.content.sizeDelta.y) * m_DisplayedObjects.Count);

            int firstIndexMaximumValue = Mathf.Max(0, m_DisplayedObjects.Count - 1);
            int lastIndexMaximumValue = Mathf.Max(0, m_DisplayedObjects.Count);
            firstIndexDisplayed = Mathf.Clamp(firstIndexDisplayed, 0, firstIndexMaximumValue);
            lastIndexDisplayed = Mathf.Clamp(lastIndexDisplayed, 0, lastIndexMaximumValue);
            if (lastIndexDisplayed >= m_DisplayedObjects.Count && firstIndexDisplayed != 0)
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
        /// <summary>
        /// Get the item which display a specified object.
        /// </summary>
        /// <param name="obj">Object displayed</param>
        /// <param name="itemToReturn">Item which display </param>
        /// <returns></returns>
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