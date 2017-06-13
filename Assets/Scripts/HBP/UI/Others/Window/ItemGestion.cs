using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Tools.Unity.Lists;

namespace HBP.UI
{
    public abstract class ItemGestion<T> : Window where T : ICloneable, ICopiable, new()
    {
        #region Properties
        [SerializeField]
        protected GameObject modifierPrefab;
        protected ItemModifier<T> modifier;
        protected SelectableList<T> list;
        private List<T> items = new List<T>();
        protected ReadOnlyCollection<T> Items
        {
            get { return new ReadOnlyCollection<T>(items); }
        }
        #endregion

        #region Public Methods
        public virtual void Save()
        {
            GameObject.FindGameObjectWithTag("Gestion").GetComponent<MenuButtonState>().SetInteractables();
            Close();
        }
        public virtual void Create()
        {
            OpenModifier(new T(),true);
        }
        public virtual void Remove()
        {
            T[] itemsToRemove = list.GetObjectsSelected();
            foreach(T item in itemsToRemove)
            {
                RemoveItem(item);
            }
        }
        #endregion

        #region Protected Methods
        protected virtual void OpenModifier(T item,bool interactable)
        {
            SetInteractable(false);
            list.DeselectAllObjects();
            RectTransform obj = Instantiate(modifierPrefab).GetComponent<RectTransform>();
            obj.SetParent(GameObject.Find("Windows").transform);
            obj.localPosition = new Vector3(0, 0, 0);
            modifier = obj.GetComponent<ItemModifier<T>>();
            modifier.Open(item, interactable);
            modifier.CloseEvent.AddListener(() => OnCloseModifier());
            modifier.SaveEvent.AddListener(() => OnSaveModifier());
        }
        protected virtual void OnCloseModifier()
        {
            SetInteractable(true);
            modifier = null;
        }
        protected virtual void OnSaveModifier()
        {
            if(!Items.Contains(modifier.Item))
            {
                AddItem(modifier.Item);
            }
            else
            {
                list.UpdateObj(modifier.Item);
            }
        }
        protected virtual void AddItem(T item)
        {
            items.Add(item);
            list.Add(item);
        }
        protected virtual void AddItem(T[] items)
        {
            foreach(T item in items)
            {
                AddItem(item);
            }
        }
        protected virtual void RemoveItem(T item)
        {
            items.Remove(item);
            list.Remove(item);
        }
        protected virtual void RemoveItem(T[] items)
        {
            foreach(T item in items)
            {
                RemoveItem(item);
            }
        }
        #endregion
    }
}