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
            foreach (T obj in m_objects)
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
            foreach (T obj in m_objects)
            {
                SelectObject(obj);
            }
            isSelecting = false;
            SelectEventInvoke();
        }
        public virtual void DeselectAllObjects()
        {
            isSelecting = true;
            foreach (T obj in m_objects)
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
        protected override void Set(T objectToSet,ListItem<T> listItem)
        {
            listItem.Set(objectToSet, transform.parent.GetComponent<RectTransform>().rect);
            listItem.SelectEvent.AddListener(() => SelectEventInvoke());
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
            return m_objectsToItems[objectToTest].isOn;
        }

        protected virtual void SetSelect(T objectToSet, bool selected)
        {
            m_objectsToItems[objectToSet].isOn = selected;
        }
        #endregion
    }
}