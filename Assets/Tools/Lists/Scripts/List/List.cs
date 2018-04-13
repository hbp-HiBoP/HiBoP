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

        protected System.Collections.Generic.List<Item<T>> m_Items;
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
        protected int m_NumberOfObjectsVisibleAtTheSameTime;
        protected int m_NumberOfObjects;
        protected int m_Start;
        protected int m_End;
        protected bool m_Initialized;
        #endregion

        #region Public Methods
        public virtual bool Add(T obj)
        {
            if (!m_Objects.Contains(obj))
            {
                m_Objects.Add(obj);
                m_NumberOfObjects++;
                m_ScrollRect.content.sizeDelta = new Vector2(m_ScrollRect.content.sizeDelta.x, m_ScrollRect.content.sizeDelta.y + ItemHeight);
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
                if (m_NumberOfObjects <= m_NumberOfObjectsVisibleAtTheSameTime)
                {
                    DestroyItem(-1);
                }
                m_NumberOfObjects--;
                m_Objects.Remove(obj);
                UpdateContent();
                GetLimits(out m_Start, out m_End);
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
            int itemsLength = items.Length;
            for (int i = m_Start, j=0; i <= m_End && j < itemsLength; i++, j++)
            {
                items[j].Object = m_Objects[i];
            }
        }
        public void RefreshPosition()
        {
            int itemsLength = m_Items.Count;
            for (int i = m_Start, j = 0; i <= m_End && j < itemsLength; i++, j++)
            {
                Item<T> item = m_Items[j];
                item.transform.localPosition = new Vector3(item.transform.localPosition.x, -i * ItemHeight, item.transform.localPosition.z);
            }
        }
        public virtual bool Initialize()
        {
            if (!m_Initialized)
            {
                m_Objects = new System.Collections.Generic.List<T>();
                m_Items = new System.Collections.Generic.List<Item<T>>();
                m_ScrollRect = GetComponent<ScrollRect>();
                m_Initialized = true;
                return true;
            }
            return false;
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
                m_NumberOfObjectsVisibleAtTheSameTime = Mathf.CeilToInt(m_ScrollRect.viewport.rect.height / ItemHeight) + 1;
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
            int start, end; GetLimits(out start, out end);

            // Resize viewport and list.
            int resize = (end - start) - (m_End - m_Start);
            if (resize >= 0) SpawnItem(resize);
            else if (resize < 0) DestroyItem(resize);

            // Move content.
            int deplacement = start - m_Start;
            if (deplacement > 0) MoveItemsDownwards(deplacement);
            else if (deplacement < 0) MoveItemsUpwards(deplacement);

            m_Start = start;
            m_End = end;
        }
        void UpdateContent()
        {
            m_ScrollRect.content.sizeDelta = new Vector2(m_ScrollRect.content.sizeDelta.x, m_ScrollRect.content.sizeDelta.y - ItemHeight);
            if (m_ScrollRect.content.localPosition.y > ItemHeight)
            {
                if (m_NumberOfObjects >= m_NumberOfObjectsVisibleAtTheSameTime - 1)
                {
                    m_ScrollRect.content.localPosition = new Vector3(m_ScrollRect.content.localPosition.x, m_ScrollRect.content.localPosition.y - ItemHeight, m_ScrollRect.content.localPosition.z);

                    if (m_NumberOfObjects >= m_NumberOfObjectsVisibleAtTheSameTime)
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
        protected virtual void SpawnItem(int number)
        {
            int end = Mathf.Min(m_End + number, m_NumberOfObjects - 1);
            int itemNumber = m_Items.Count;
            for (int i = m_Start + itemNumber; i <= end; i++)
            {
                T obj = m_Objects[i];
                Item<T> item = Instantiate(ItemPrefab, m_ScrollRect.content).GetComponent<Item<T>>();
                RectTransform itemRectTransform = item.transform as RectTransform;
                itemRectTransform.sizeDelta = new Vector2(0, itemRectTransform.sizeDelta.y);
                itemRectTransform.localPosition = new Vector3(itemRectTransform.localPosition.x, -i * ItemHeight, itemRectTransform.localPosition.z);
                m_Items.Add(item);
                item.Object = obj;
            }
        }
        protected void DestroyItem(int number)
        {
            Item<T>[] items = m_Items.OrderByDescending((item) => item.transform.localPosition.y).ToArray();
            int itemNumber = items.Length;
            for (int i = number; i < 0; i++)
            {
                Item<T> item = items[itemNumber + i];
                Destroy(item.gameObject);
                m_Items.Remove(item);
            }
        }
        protected virtual void MoveItemsUpwards(int deplacement)
        {
            Item<T>[] items = m_Items.OrderByDescending((item) => item.transform.localPosition.y).ToArray();
            int itemNumber = items.Length;
            for (int i = 0; i > deplacement; i--)
            {
                int itemID = ((itemNumber - 1 + i) % itemNumber + itemNumber) % itemNumber;
                Item<T> item = items[itemID];
                T newObj = m_Objects[m_Start - 1 + i];
                item.transform.localPosition = new Vector3(item.transform.localPosition.x, -(m_Start - 1 + i) * ItemHeight, item.transform.localPosition.z);
                item.Object = newObj;
            }
        }
        protected virtual void MoveItemsDownwards(int deplacement)
        {
            Item<T>[] items = m_Items.OrderByDescending((item) => item.transform.localPosition.y).ToArray();
            int itemNumber = items.Length;
            for (int i = 0; i < deplacement; i++)
            {
                int itemID = (i % itemNumber + itemNumber) % itemNumber;
                Item<T> item = items[itemID];
                T newObj = m_Objects[m_End + 1 + i];
                item.transform.localPosition = new Vector3(item.transform.localPosition.x, -(m_End + 1 + i) * ItemHeight, item.transform.localPosition.z);
                item.Object = newObj;
            }
        }
        protected void GetLimits(out int start, out int end)
        {
            int maxNumberOfItem = m_NumberOfObjects - m_NumberOfObjectsVisibleAtTheSameTime;
            start = Mathf.Clamp(Mathf.FloorToInt((m_ScrollRect.content.localPosition.y / m_ScrollRect.content.sizeDelta.y) * m_NumberOfObjects), 0, Mathf.Max(maxNumberOfItem, 0));
            end = Mathf.Clamp(start + m_NumberOfObjectsVisibleAtTheSameTime - 1, 0, m_NumberOfObjects > 0 ? m_NumberOfObjects - 1 : 0);
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
