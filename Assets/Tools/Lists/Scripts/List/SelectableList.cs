using System.Collections.Generic;
using UnityEngine.Events;
using System.Linq;

namespace Tools.Unity.Lists
{
    public class SelectableList<T> : List<T>
    {
        #region Properties
        protected new Dictionary<T, SelectableItem<T>> m_ObjectsToItems
        {
            get { return base.m_ObjectsToItems.ToDictionary((k) => k.Key, (v) => v.Value as SelectableItem<T>); }
        } 
        public GenericEvent<T,bool> OnChangeSelected { get; set; }
        public T[]  SelectedObject
        {
            get { return m_Objects.Where((obj) => m_ObjectsToItems[obj].selected).ToArray(); }
            set { foreach (var i in value) m_ObjectsToItems[i].selected = true; }
        }
        #endregion

        #region Public Methods
        public virtual void SelectAll()
        {
            foreach (var item in m_ObjectsToItems.Values) item.selected = true;
        }
        public virtual void DeselectAll()
        {
            foreach (var item in m_ObjectsToItems.Values) item.selected = false;
        }
        #endregion

        #region Protected Methods
        protected override void Set(T objectToSet, Item<T> listItem)
        {
            base.Set(objectToSet, listItem);

            (listItem as SelectableItem<T>).OnChangeSelected.AddListener(() => m_OnSelectItem());
        }
        #endregion
    }
}