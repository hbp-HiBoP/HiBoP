using UnityEngine;
using System;
using System.Collections.ObjectModel;
using Tools.Unity.Lists;
using System.Collections.Generic;

namespace HBP.UI
{
    public abstract class ItemGestion<T> : Window where T : ICloneable, ICopiable, new()
    {
        #region Properties
        [SerializeField] protected GameObject m_ModifierPrefab;
        protected ItemModifier<T> m_Modifier;
        protected SelectableList<T> m_List;
        private System.Collections.Generic.List<T> m_Items = new System.Collections.Generic.List<T>();
        protected ReadOnlyCollection<T> Items
        {
            get { return new ReadOnlyCollection<T>(m_Items); }
        }
        #endregion

        #region Public Methods
        public virtual void Save()
        {
            FindObjectOfType<MenuButtonState>().SetInteractables();
            Close();
        }
        public virtual void Create()
        {
            OpenModifier(new T(),true);
        }
        public virtual void Remove()
        {
            T[] itemsToRemove = m_List.ObjectsSelected;
            foreach(T item in itemsToRemove)
            {
                RemoveItem(item);
            }
        }
        #endregion

        #region Protected Methods
        protected virtual void OpenModifier(T item,bool interactable)
        {
            m_List.DeselectAll();
            RectTransform obj = Instantiate(m_ModifierPrefab).GetComponent<RectTransform>();
            obj.SetParent(GameObject.Find("Windows").transform);
            obj.localPosition = new Vector3(0, 0, 0);
            m_Modifier = obj.GetComponent<ItemModifier<T>>();
            m_Modifier.Open(item, interactable);
            m_Modifier.CloseEvent.AddListener(() => OnCloseModifier());
            m_Modifier.SaveEvent.AddListener(() => OnSaveModifier());
        }
        protected virtual void OnCloseModifier()
        {
            SetInteractable(true);
            m_Modifier = null;
        }
        protected virtual void OnSaveModifier()
        {
            if(!Items.Contains(m_Modifier.Item))
            {
                AddItem(m_Modifier.Item);
            }
            else
            {
                m_List.UpdateObject(m_Modifier.Item);
            }
        }
        protected virtual void AddItem(T item)
        {
            m_Items.Add(item);
            m_List.Add(item);
        }
        protected virtual void AddItem(IEnumerable<T> items)
        {
            foreach(T item in items)
            {
                AddItem(item);
            }
        }
        protected virtual void RemoveItem(T item)
        {
            m_Items.Remove(item);
            m_List.Remove(item);
        }
        protected virtual void RemoveItem(IEnumerable<T> items)
        {
            foreach(T item in items)
            {
                RemoveItem(item);
            }
        }
        #endregion
    }
}