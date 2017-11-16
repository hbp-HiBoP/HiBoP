using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;

namespace Tools.Unity.Lists
{
    [RequireComponent(typeof(RectTransform), typeof(VerticalLayoutGroup) , typeof(ContentSizeFitter))]
    public class List<T> : MonoBehaviour
    {
        #region Properties
        [SerializeField] protected GameObject[] m_Prefabs;
        protected Dictionary<T, Item<T>> m_ObjectsToItems = new Dictionary<T, Item<T>>();
        public virtual T[] Objects
        {
            get
            {
                return m_ObjectsToItems.Keys.ToArray();
            }
            set
            {
                Remove(m_ObjectsToItems.Keys.ToArray());
                Add(value);
            }
        }
        protected bool m_Interactable;
        public bool interactable
        {
            get
            {
                return m_Interactable;
            }
            set
            {
                m_Interactable = value;
                foreach (var item in m_ObjectsToItems.Values) item.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public virtual void Add(T objectToAdd)
        {
            if(!m_ObjectsToItems.Keys.Contains(objectToAdd))
            {
                GameObject prefab = m_Prefabs.First((p) => p.GetComponent<Item<T>>().Type == objectToAdd.GetType());
                Item<T> item = Instantiate(prefab, transform).GetComponent<Item<T>>();
                m_ObjectsToItems.Add(objectToAdd, item);
                item.Object = objectToAdd;
                item.interactable = interactable;
            }
        }
        public virtual void Add(IEnumerable<T> objectsToAdd)
        {
            foreach (T obj in objectsToAdd) Add(obj);
        }
        public virtual void Remove(T objectToRemove)
        {
            if(m_ObjectsToItems.Keys.Contains(objectToRemove))
            {
                Destroy(m_ObjectsToItems[objectToRemove].gameObject);
                m_ObjectsToItems.Remove(objectToRemove);
            }
        }
        public virtual void Remove(IEnumerable<T> objectsToRemove)
        {
            foreach(T obj in objectsToRemove) Remove(obj);
        }
        public virtual void UpdateObject(T objectToUpdate)
        {
            m_ObjectsToItems[objectToUpdate].Object = objectToUpdate;
        }
        #endregion
    }
}