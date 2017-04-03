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
        protected GameObject m_item;

        protected List<T> m_objects = new List<T>();
        protected Dictionary<T, ListItem<T>> m_objectsToItems = new Dictionary<T, ListItem<T>>();
        public virtual T[] Objects { get { return m_objects.ToArray(); } }

        protected bool IsDisplaying = false;
        protected bool IsWaiting = false;


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
            IsDisplaying = false;
            IsWaiting = false;
            m_objects = new List<T>();
            m_objectsToItems = new Dictionary<T, ListItem<T>>();
            for (int i = 0; i < transform.childCount; i++)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
        }
        public virtual void Remove(T objectToRemove)
        {
            Destroy(Get(objectToRemove).gameObject);
            m_objects.Remove(objectToRemove);
            m_objectsToItems.Remove(objectToRemove);
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
            Transform l_item = Instantiate(m_item).transform;
            ListItem<T> l_listItem = l_item.GetComponent<ListItem<T>>();
            l_item.SetParent(transform);
            m_objects.Add(objectToAdd);
            m_objectsToItems.Add(objectToAdd, l_listItem);
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
            m_objectsToItems[objectToUpdate].Set(objectToUpdate,transform.parent.GetComponent<RectTransform>().rect);
        }
        #endregion

        #region Protected Methods
        protected virtual IEnumerator c_Display(T[] objectsToDisplay, bool update)
        {
            IsWaiting = true;
            while (IsDisplaying)
            {
                yield return null;
            }
            IsWaiting = false;
            IsDisplaying = true;
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
                foreach (T obj in m_objects)
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
            IsDisplaying = false;
        }
        protected virtual void Set(T objectToSet,ListItem<T> listItem )
        {
            listItem.Set(objectToSet, transform.parent.GetComponent<RectTransform>().rect);
        }
        protected virtual ListItem<T> Get(T objectToGet)
        {
            ListItem<T> l_result = null;
            m_objectsToItems.TryGetValue(objectToGet, out l_result);
            return l_result;
        }
        protected virtual void ApplySort()
        {
            for (int i = 0; i < m_objects.Count; i++)
            {
                m_objectsToItems[m_objects[i]].transform.SetSiblingIndex(i);
            }

        }
        #endregion
    }
}