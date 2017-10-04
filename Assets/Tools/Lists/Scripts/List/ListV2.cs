using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;

namespace Tools.Unity.Lists
{
    [RequireComponent(typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter))]
    public class ListV2 : MonoBehaviour
    {
        #region Properties
        [SerializeField] protected GameObject[] m_Prefabs;
        protected Dictionary<IListable, Item<IListable>> m_ObjectsToItems = new Dictionary<IListable, Item<IListable>>();
        public virtual IListable[] Objects
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
        public virtual void Add(IListable objectToAdd)
        {
            if (!m_ObjectsToItems.Keys.Contains(objectToAdd))
            {
                GameObject prefab = m_Prefabs.First((p) => p.GetComponent<Item<IListable>>().Type == objectToAdd.GetType());
                Item<IListable> item = Instantiate(prefab, transform).GetComponent<Item<IListable>>();
                m_ObjectsToItems.Add(objectToAdd, item);
                item.Object = objectToAdd;
                item.interactable = interactable;
            }
        }
        public virtual void Add(IEnumerable<IListable> objectsToAdd)
        {
            foreach (IListable obj in objectsToAdd) Add(obj);
        }
        public virtual void Remove(IListable objectToRemove)
        {
            if (m_ObjectsToItems.Keys.Contains(objectToRemove))
            {
                Destroy(m_ObjectsToItems[objectToRemove].gameObject);
                m_ObjectsToItems.Remove(objectToRemove);
            }
        }
        public virtual void Remove(IEnumerable<IListable> objectsToRemove)
        {
            foreach (IListable obj in objectsToRemove) Remove(obj);
        }
        public virtual void UpdateObject(IListable objectToUpdate)
        {
            m_ObjectsToItems[objectToUpdate].Object = objectToUpdate;
        }
        #endregion
    }
}