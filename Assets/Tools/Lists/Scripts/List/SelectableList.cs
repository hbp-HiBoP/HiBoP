using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Tools.Unity.Lists
{
    public class SelectableList<T> : List<T>, ISelectionCountable
    {
        #region Properties
        protected UnityEvent m_OnSelectionChanged = new UnityEvent();
        public virtual UnityEvent OnSelectionChanged
        {
            get { return m_OnSelectionChanged; }
        }
        public BoolEvent OnAllSelected = new BoolEvent();
        public virtual T[] ObjectsSelected
        {
            get
            {
                return (from couple in m_SelectedStateByObject where couple.Value select couple.Key).ToArray();
            }
            set
            {
                Deselect(from obj in m_Objects.Where((elt) => !value.Contains(elt)) select obj);
                Select(from obj in m_Objects.Where((elt) => value.Contains(elt)) select obj);
            }
        }
        protected Dictionary<T, bool> m_SelectedStateByObject = new Dictionary<T, bool>();
        [SerializeField] protected bool m_MultiSelection;
        public virtual bool MultiSelection
        {
            get
            {
                return m_MultiSelection;
            }
            set
            {
                m_MultiSelection = value;
                if (!value)
                {
                    T[] objectSelected = ObjectsSelected;
                    for (int i = 1; i < objectSelected.Length; i++)
                    {
                        Deselect(objectSelected[i]);
                    }
                }
            }
        }
        public int NumberOfItemSelected
        {
            get { return ObjectsSelected.Length; }
        }
        protected bool m_AllSelected;
        #endregion

        #region Public Methods
        public override bool Add(T obj)
        {
            if (base.Add(obj))
            {
                m_SelectedStateByObject.Add(obj, false);
                OnSelectionChangeCallBack();
                return true;
            }
            return false;
        }
        public override bool Remove(T obj)
        {
            if (base.Remove(obj))
            {
                m_SelectedStateByObject.Remove(obj);
                OnSelectionChangeCallBack();
                return true;
            }
            return false;
        }
        public virtual void SelectAll()
        {
            SelectAll(Toggle.ToggleTransition.None);
        }
        public virtual void SelectAll(Toggle.ToggleTransition transition)
        {
            Select(m_Objects, transition);
        }
        public virtual void DeselectAll()
        {
            DeselectAll(Toggle.ToggleTransition.None);
        }
        public virtual void DeselectAll(Toggle.ToggleTransition transition) 
        {
            Deselect(m_Objects, transition);
        }
        public virtual void Select(T objectToSelect, Toggle.ToggleTransition transition = Toggle.ToggleTransition.None)
        {
            if (!m_MultiSelection)
            {
                Deselect(m_Objects.Where((o) => !o.Equals(objectToSelect)));
            }
            if (m_SelectedStateByObject.ContainsKey(objectToSelect))
            {
                m_SelectedStateByObject[objectToSelect] = true;
            }
            Item<T> item;
            if (GetItemFromObject(objectToSelect, out item))
            {
                (item as SelectableItem<T>).Select(true, transition);
            }
            OnSelectionChangeCallBack();
        }
        public virtual void Select(IEnumerable<T> objectsToSelect, Toggle.ToggleTransition transition = Toggle.ToggleTransition.None)
        {
            foreach (var obj in objectsToSelect) Select(obj, transition);
        }
        public virtual void Deselect(T objectToDeselect, Toggle.ToggleTransition transition = Toggle.ToggleTransition.None)
        {
            if (m_SelectedStateByObject.ContainsKey(objectToDeselect))
            {
                m_SelectedStateByObject[objectToDeselect] = false;
            }
            Item<T> item;
            if (GetItemFromObject(objectToDeselect, out item))
            {
                (item as SelectableItem<T>).Select(false, transition);
            }
            OnSelectionChangeCallBack();
        }
        public virtual void Deselect(IEnumerable<T> objectsToDeselect, Toggle.ToggleTransition transition = Toggle.ToggleTransition.None)
        {
            foreach (var obj in objectsToDeselect) Deselect(obj, transition);
        }
        public override bool UpdateObject(T objectToUpdate)
        {
            Item<T> item;
            if (GetItemFromObject(objectToUpdate, out item))
            {
                SelectableItem<T> selectableItem = item as SelectableItem<T>;
                selectableItem.Object = objectToUpdate;
                selectableItem.OnChangeSelected.RemoveAllListeners();
                selectableItem.Select(m_SelectedStateByObject[objectToUpdate]);
                selectableItem.OnChangeSelected.AddListener((selected) => OnSelection(objectToUpdate, selected));
                return true;
            }
            return false;
        }
        public override void Refresh()
        {
            Item<T>[] items = m_Items.OrderByDescending((item) => item.transform.localPosition.y).ToArray();
            int itemsLength = items.Length;
            for (int i = m_FirstIndexDisplayed, j = 0; i <= m_LastIndexDisplayed && j < itemsLength; i++, j++)
            {
                SelectableItem<T> item = items[j] as SelectableItem<T>;
                T obj = m_Objects[i];
                item.Object = obj;
                item.OnChangeSelected.RemoveAllListeners();
                item.Select(m_SelectedStateByObject[obj]);
                item.OnChangeSelected.AddListener((selected) => OnSelection(obj, selected));
            }
        }
        #endregion

        #region Private Methods
        protected override void SetItem(Item<T> item, T obj)
        {
            base.SetItem(item, obj);
            SelectableItem<T> selectableItem = item as SelectableItem<T>;
            selectableItem.OnChangeSelected.RemoveAllListeners();
            selectableItem.Select(m_SelectedStateByObject[obj]);
            selectableItem.OnChangeSelected.AddListener((selected) => OnSelection(obj, selected));
        }
        protected virtual void OnSelection(T obj, bool selected)
        {
            if (!m_MultiSelection)
            {
                Deselect(m_Objects.Where((o) => !o.Equals(obj)), Toggle.ToggleTransition.Fade);
            }
            if (m_SelectedStateByObject.ContainsKey(obj))
            {
                m_SelectedStateByObject[obj] = selected;
            }
            OnSelectionChangeCallBack();
        }
        protected virtual void OnSelectionChangeCallBack()
        {
            OnSelectionChanged.Invoke();
            bool allSelected = m_Objects.Count == ObjectsSelected.Length && m_Objects.Count > 0;
            if (m_AllSelected != allSelected)
            {
                m_AllSelected = allSelected;
                OnAllSelected.Invoke(allSelected);
            }
        }
        #endregion
    }
}
