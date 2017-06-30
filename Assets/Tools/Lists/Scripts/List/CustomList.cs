using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using CielaSpike;

namespace Tools.Unity.Lists
{
    [RequireComponent(typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter))]
    public class CustomList<T> : MonoBehaviour
    {
        #region Properties
        [SerializeField]
        protected GameObject[] m_Items;

        protected List<T> m_Objects = new List<T>();
        protected Dictionary<T, Item<T>> m_ObjectsToItems = new Dictionary<T, Item<T>>();
        public virtual T[] Objects { get { return m_Objects.ToArray(); } }

        protected bool m_IsDisplaying = false;
        protected bool m_IsWaiting = false;
        #endregion

        #region Public Methods
        public virtual void Display(T[] objectToDisplay)
        {
            Display(objectToDisplay, true);
        }
        public virtual void Display(T[] objectsToDisplay, bool update)
        {
            this.StartCoroutineAsync(c_Display(objectsToDisplay, update));
        }
        public virtual void Clear()
        {
            StopAllCoroutines();
            m_IsDisplaying = false;
            m_IsWaiting = false;
            m_Objects = new List<T>();
            m_ObjectsToItems = new Dictionary<T, Item<T>>();
            for (int i = 0; i < transform.childCount; i++)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
        }
        public virtual void Remove(T objectToRemove)
        {
            Destroy(Get(objectToRemove).gameObject);
            m_Objects.Remove(objectToRemove);
            m_ObjectsToItems.Remove(objectToRemove);
        }
        public virtual void Remove(T[] objectsToRemove)
        {
            foreach(T obj in objectsToRemove)
            {
                Remove(obj);
            }
        }
        public virtual void Add(T objectToAdd)
        {
            GameObject l_itemToInstantiate = m_Items.First((prefab) => prefab.GetComponent<Item<T>>().GetObjectType() == objectToAdd.GetType());
            Item<T> l_listItem = Instantiate(l_itemToInstantiate, transform).GetComponent<Item<T>>();
            m_Objects.Add(objectToAdd);
            m_ObjectsToItems.Add(objectToAdd, l_listItem);
            Set(objectToAdd, l_listItem);
        }
        public virtual void Add(T[] objectsToAdd)
        {
            foreach (T obj in objectsToAdd)
            {
                Add(obj);
            }
        }
        public virtual void UpdateObj(T objectToUpdate)
        {
            Item<T> item = m_ObjectsToItems[objectToUpdate];
            item.Object = objectToUpdate;
        }
        #endregion

        #region Protected Methods
        protected virtual IEnumerator c_Display(T[] objectsToDisplay, bool update)
        {
            m_IsWaiting = true;
            while (m_IsDisplaying)
            {
                yield return null;
            }
            m_IsWaiting = false;
            m_IsDisplaying = true;
            if (objectsToDisplay.Length == 0)
            {
                yield return Ninja.JumpToUnity;
                Clear();
                yield return Ninja.JumpBack;
            }
            else
            {
                List<T> m_objToAdd = new List<T>();
                List<T> m_objToRemove = new List<T>();
                List<T> m_objToUpdate = new List<T>();

                // Find obj to remove.
                foreach (T obj in m_Objects)
                {
                    if (!objectsToDisplay.Contains(obj))
                    {
                        m_objToRemove.Add(obj);
                    }
                }

                // Find obj to add and object to update.
                foreach (T obj in objectsToDisplay)
                {
                    if (!Objects.Contains(obj))
                    {
                        m_objToAdd.Add(obj);
                    }
                    else
                    {
                        m_objToUpdate.Add(obj);
                    }
                }

                // Remove obj.
                foreach (T obj in m_objToRemove)
                {
                    yield return Ninja.JumpToUnity;
                    Remove(obj);
                    yield return Ninja.JumpBack;
                }

                // Add obj.
                foreach (T obj in m_objToAdd)
                {
                    yield return Ninja.JumpToUnity;
                    Add(obj);
                    yield return Ninja.JumpBack;
                }


                if (update)
                {
                    // Update obj
                    foreach (T obj in m_objToUpdate)
                    {
                        yield return Ninja.JumpToUnity;
                        UpdateObj(obj);
                        yield return Ninja.JumpBack;
                    }
                }
            }
            m_IsDisplaying = false;
        }
        protected virtual void Set(T objectToSet,Item<T> listItem )
        {
            listItem.Object = objectToSet;
        }
        protected virtual Item<T> Get(T objectToGet)
        {
            Item<T> l_result = null;
            m_ObjectsToItems.TryGetValue(objectToGet, out l_result);
            return l_result;
        }
        protected virtual void ApplySort()
        {
            for (int i = 0; i < m_Objects.Count; i++)
            {
                m_ObjectsToItems[m_Objects[i]].transform.SetSiblingIndex(i);
            }

        }
        #endregion
    }
}