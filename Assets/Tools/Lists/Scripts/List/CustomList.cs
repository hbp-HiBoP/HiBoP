using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using CielaSpike;

namespace Tools.Unity.Lists
{
    [RequireComponent(typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter))]
    public class List<T> : MonoBehaviour
    {
        #region Properties
        [SerializeField] protected GameObject[] m_Prefabs;

        protected SortedDictionary<T, Item<T>> m_ObjectsToItems = new SortedDictionary<T, Item<T>>();
        public virtual T[] Objects { get { return m_ObjectsToItems.Keys.ToArray(); } }

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
            foreach (var item in m_ObjectsToItems.Values) Destroy(item.gameObject);
            m_ObjectsToItems = new SortedDictionary<T, Item<T>>();
        }
        public virtual void Remove(T objectToRemove)
        {
            Destroy(m_ObjectsToItems[objectToRemove].gameObject);
            m_ObjectsToItems.Remove(objectToRemove);
        }
        public virtual void Remove(T[] objectsToRemove)
        {
            foreach(T obj in objectsToRemove) Remove(obj);
        }
        public virtual void Add(T objectToAdd)
        {
            Item<T> item = Instantiate(m_Prefabs.First((prefab) => prefab.GetComponent<Item<T>>().Object.GetType() == objectToAdd.GetType()), transform).GetComponent<Item<T>>();
            item.Object = objectToAdd;
            m_ObjectsToItems.Add(objectToAdd, item);
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
           m_ObjectsToItems[objectToUpdate].Object = objectToUpdate;
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
                System.Collections.Generic.List<T> m_objToAdd = new System.Collections.Generic.List<T>();
                System.Collections.Generic.List<T> m_objToRemove = new System.Collections.Generic.List<T>();
                System.Collections.Generic.List<T> m_objToUpdate = new System.Collections.Generic.List<T>();

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
        protected virtual void Sort()
        {
            foreach (var elt in m_ObjectsToItems) elt.Value.transform.SetAsLastSibling();
        }
        #endregion
    }
}