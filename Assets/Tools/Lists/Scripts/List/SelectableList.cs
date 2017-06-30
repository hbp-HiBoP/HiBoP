using UnityEngine;
using System.Collections.Generic;

namespace Tools.Unity.Lists
{
    public class SelectableList<T> : CustomList<T>
    {
        #region Properties
        protected SelectEvent selectEvent = new SelectEvent();
        public SelectEvent SelectEvent { get { return selectEvent; } }
        protected bool isSelecting;
        #endregion

        #region Public Methods
        public virtual T[] GetObjectsSelected()
        {
            List<T> l_objectsSelected = new List<T>();
            foreach (T obj in m_Objects)
            {
                if (isSelected(obj))
                {
                    l_objectsSelected.Add(obj);
                }
            }
            return l_objectsSelected.ToArray();
        }

        public virtual void SelectAllObjects()
        {
            isSelecting = true;
            foreach (T obj in m_Objects)
            {
                SelectObject(obj);
            }
            isSelecting = false;
            SelectEventInvoke();
        }
        public virtual void DeselectAllObjects()
        {
            isSelecting = true;
            foreach (T obj in m_Objects)
            {
                DeselectObject(obj);
            }
            isSelecting = false;
            SelectEventInvoke();
        }

        public virtual void SelectObject(T objectToSelect)
        {
            SetSelect(objectToSelect, true);
        }
        public virtual void DeselectObject(T objectToDeselect)
        {
            SetSelect(objectToDeselect, false);
        }
        #endregion

        #region Protected Methods
        protected override void Set(T objectToSet, Item<T> listItem)
        {
            base.Set(objectToSet, listItem);
            (listItem as SelectableItem<T>).OnChangeSelected.AddListener(() => SelectEventInvoke());
        }
        void SelectEventInvoke()
        {
            if(!isSelecting)
            {
                SelectEvent.Invoke();
            }
        }
        protected virtual bool isSelected(T objectToTest)
        {
            return (m_ObjectsToItems[objectToTest] as SelectableItem<T>).Selected;
        }
        protected virtual void SetSelect(T objectToSet, bool selected)
        {
            (m_ObjectsToItems[objectToSet] as SelectableItem<T>).Selected = selected;
        }
        #endregion
    }
}