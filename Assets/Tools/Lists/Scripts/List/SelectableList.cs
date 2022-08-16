using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using HBP.Core.Interfaces;

namespace HBP.UI.Lists
{
    /// <summary>
    /// List to display elements which can be selected. 
    /// </summary>
    public class SelectableList<T> : List<T>, ISelectionCountable
    {
        #region Properties
        /// <summary>
        /// All types of selection.
        /// </summary>
        public enum SelectionType { None, SingleItem, MultipleItems }
        [SerializeField] protected SelectionType m_ItemSelection = SelectionType.MultipleItems;
        /// <summary>
        /// Selection Type.
        /// </summary>
        public SelectionType ItemSelection
        {
            get
            {
                return m_ItemSelection;
            }
            set
            {
                m_ItemSelection = value;
                T[] objectSelected = ObjectsSelected;
                switch (value)
                {
                    case SelectionType.None:
                        if (m_SelectAllToggle) m_SelectAllToggle.interactable = false;
                        DeselectAll();
                        foreach (var item in m_Items) item.Interactable = false;
                        break;
                    case SelectionType.SingleItem:
                        if (m_SelectAllToggle) m_SelectAllToggle.interactable = false;
                        for (int i = 1; i < objectSelected.Length; i++) Deselect(objectSelected[i]);
                        foreach (var item in m_Items) item.Interactable = true;
                        break;
                    case SelectionType.MultipleItems:
                        if (m_SelectAllToggle) m_SelectAllToggle.interactable = true;
                        foreach (var item in m_Items) item.Interactable = true;
                        break;
                }
            }
        }

        /// <summary>
        /// Toggle to select/deselect all items of the list.
        /// </summary>
        [SerializeField] protected Toggle m_SelectAllToggle;

        protected Dictionary<T, bool> m_SelectedStateByObject = new Dictionary<T, bool>();
        /// <summary>
        /// Objects selected in the list.
        /// </summary>
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

        /// <summary>
        /// Number of item selected.
        /// </summary>
        int ISelectionCountable.NumberOfItemSelected
        {
            get { return ObjectsSelected.Length; }
        }

        /// <summary>
        /// Callback executed when selection is changed.
        /// </summary> 
        UnityEvent ISelectionCountable.OnSelectionChanged { get; } = new UnityEvent();

        /// <summary>
        /// Callback executed when a object is selected.
        /// </summary> 
        public virtual UnityEvent<T> OnSelect { get; } = new GenericEvent<T>();
        /// <summary>
        /// Callback executed when a object is deselected.
        /// </summary> 
        public virtual UnityEvent<T> OnDeselect { get; } = new GenericEvent<T>();

        public override bool Interactable
        {
            get => base.Interactable;
            set
            {
                base.Interactable = value;
                if (m_SelectAllToggle != null) m_SelectAllToggle.interactable = value && m_ItemSelection == SelectionType.MultipleItems;
            }
        }
        /// <summary>
        /// Locker to avoid loop selection.
        /// </summary>
        bool m_SelectionLock;
        /// <summary>
        /// Last objet selected.
        /// </summary>
        T m_LastSelectedObject;
        #endregion

        #region Public Methods
        public override bool Add(T obj)
        {
            if (base.Add(obj))
            {
                m_SelectedStateByObject.Add(obj, false);
                OnSelectionChanged();
                return true;
            }
            return false;
        }
        public override bool Remove(T obj)
        {
            if (base.Remove(obj))
            {
                m_SelectedStateByObject.Remove(obj);
                OnSelectionChanged();
                return true;
            }
            return false;
        }
        /// <summary>
        /// Select all objects.
        /// </summary>
        public virtual void SelectAll()
        {
            SelectAll(Toggle.ToggleTransition.None);
        }
        /// <summary>
        /// Select all objects with a specified transition.
        /// </summary>
        /// <param name="transition">Transition</param>
        public virtual void SelectAll(Toggle.ToggleTransition transition)
        {
            Select(m_DisplayedObjects, transition);
        }
        /// <summary>
        /// Deselect all objects.
        /// </summary>
        public virtual void DeselectAll()
        {
            DeselectAll(Toggle.ToggleTransition.None);
        }
        /// <summary>
        /// Select all objects with a specified transition.
        /// </summary>
        /// <param name="transition">Transition</param>
        public virtual void DeselectAll(Toggle.ToggleTransition transition)
        {
            Deselect(m_DisplayedObjects, transition);
        }
        /// <summary>
        /// Select a specified object with a specified transition.
        /// </summary>
        /// <param name="objectToSelect">Object to select</param>
        /// <param name="transition">Transition</param>
        public virtual void Select(T objectToSelect, Toggle.ToggleTransition transition = Toggle.ToggleTransition.None)
        {
            switch (m_ItemSelection)
            {
                case SelectionType.None:
                    DeselectAll();
                    return;
                case SelectionType.SingleItem:
                    Deselect(m_DisplayedObjects.Where((o) => !o.Equals(objectToSelect)));
                    break;
            }
            if (m_SelectedStateByObject.ContainsKey(objectToSelect))
            {
                m_SelectedStateByObject[objectToSelect] = true;
            }
            if (GetItemFromObject(objectToSelect, out Item<T> item))
            {
                (item as SelectableItem<T>).ChangeSelectionValue(true, transition);
            }
            OnSelect.Invoke(objectToSelect);
            OnSelectionChanged();
        }
        /// <summary>
        /// Select specified objects with a specified transition. 
        /// </summary>
        /// <param name="objectsToSelect">Objects to select</param>
        /// <param name="transition">Transition</param>
        public virtual void Select(IEnumerable<T> objectsToSelect, Toggle.ToggleTransition transition = Toggle.ToggleTransition.None)
        {
            foreach (var obj in objectsToSelect) Select(obj, transition);
        }
        /// <summary>
        /// Deselect a specified object with a specified transition.
        /// </summary>
        /// <param name="objectToDeselect">Object to deselect</param>
        /// <param name="transition">Transition</param>
        public virtual void Deselect(T objectToDeselect, Toggle.ToggleTransition transition = Toggle.ToggleTransition.None)
        {
            if (m_SelectedStateByObject.ContainsKey(objectToDeselect))
            {
                m_SelectedStateByObject[objectToDeselect] = false;
            }
            if (GetItemFromObject(objectToDeselect, out Item<T> item))
            {
                (item as SelectableItem<T>).ChangeSelectionValue(false, transition);
            }
            OnDeselect.Invoke(objectToDeselect);
            OnSelectionChanged();
        }
        /// <summary>
        /// Deselect specified objects with a specified transition.
        /// </summary>
        /// <param name="objectsToDeselect">Specified objects</param>
        /// <param name="transition">Transition</param>
        public virtual void Deselect(IEnumerable<T> objectsToDeselect, Toggle.ToggleTransition transition = Toggle.ToggleTransition.None)
        {
            foreach (var obj in objectsToDeselect) Deselect(obj, transition);
        }
        /// <summary>
        /// Update specified object.
        /// </summary>
        /// <param name="objectToUpdate">Object to update</param>
        /// <returns>True if updated, False otherwise</returns>
        public override bool UpdateObject(T objectToUpdate)
        {
            int index = m_Objects.FindIndex(o => o.Equals(objectToUpdate));
            m_Objects[index] = objectToUpdate;

            if (GetItemFromObject(objectToUpdate, out Item<T> item))
            {
                SelectableItem<T> selectableItem = item as SelectableItem<T>;
                selectableItem.Object = objectToUpdate;
                selectableItem.OnChangeSelected.RemoveAllListeners();
                selectableItem.ChangeSelectionValue(m_SelectedStateByObject[objectToUpdate]);
                selectableItem.OnChangeSelected.AddListener((selected) => OnChangeSelectionState(objectToUpdate, selected));
                OnUpdateObject.Invoke(objectToUpdate);
                return true;
            }
            return false;
        }
        /// <summary>
        /// Refresh all the list.
        /// </summary>
        public override void Refresh()
        {
            Item<T>[] items = m_Items.OrderByDescending((item) => item.transform.localPosition.y).ToArray();
            int itemsLength = items.Length;
            for (int i = m_FirstIndexDisplayed, j = 0; i <= m_LastIndexDisplayed && j < itemsLength; i++, j++)
            {
                SelectableItem<T> item = items[j] as SelectableItem<T>;
                T obj = m_DisplayedObjects[i];
                item.Object = obj;
                item.OnChangeSelected.RemoveAllListeners();
                item.ChangeSelectionValue(m_SelectedStateByObject[obj]);
                item.OnChangeSelected.AddListener((selected) => OnChangeSelectionState(obj, selected));
            }
        }
        /// <summary>
        /// Mask the list of objects to only display some of them
        /// </summary>
        /// <param name="mask">Mask for the list</param>
        public override bool MaskList(bool[] mask)
        {
            if (base.MaskList(mask))
            {
                m_SelectedStateByObject = m_SelectedStateByObject.ToDictionary(s => s.Key, s => false);
                return true;
            }
            return false;
        }
        #endregion

        #region Private Methods
        void OnValidate()
        {
            Validate();
        }
        void Awake()
        {
            if (m_SelectAllToggle != null) m_SelectAllToggle.onValueChanged.AddListener(OnSelectAllToggleValueChanged);
            Validate();
        }
        protected override void Validate()
        {
            base.Validate();
            ItemSelection = ItemSelection;
        }
        protected override void SetItem(Item<T> item, T obj)
        {
            base.SetItem(item, obj);
            SelectableItem<T> selectableItem = item as SelectableItem<T>;
            selectableItem.OnChangeSelected.RemoveAllListeners();
            selectableItem.ChangeSelectionValue(m_SelectedStateByObject[obj]);
            selectableItem.OnChangeSelected.AddListener((selected) => OnChangeSelectionState(obj, selected));
        }
        /// <summary>
        /// Callback executed when a object selection is changed.
        /// </summary> 
        protected virtual void OnChangeSelectionState(T obj, bool selected)
        {
            if (!m_SelectionLock)
            {
                m_SelectionLock = true;
                switch (m_ItemSelection)
                {
                    case SelectionType.None:
                        return;
                    case SelectionType.SingleItem:
                        Deselect(m_DisplayedObjects.Where((o) => !o.Equals(obj)), Toggle.ToggleTransition.Fade);
                        break;
                    case SelectionType.MultipleItems:
                        if (Input.GetKey(KeyCode.LeftShift))
                        {
                            int lastIndex = m_DisplayedObjects.IndexOf(m_LastSelectedObject);
                            int actualIndex = m_DisplayedObjects.IndexOf(obj);
                            int min = Mathf.Min(lastIndex, actualIndex);
                            int max = Mathf.Max(lastIndex, actualIndex);
                            for (int i = min; i < max; i++)
                            {
                                Select(m_DisplayedObjects[i]);
                            }
                        }
                        else
                        {
                            if (selected) m_LastSelectedObject = obj;
                        }
                        break;
                }
                if (m_SelectedStateByObject.ContainsKey(obj))
                {
                    m_SelectedStateByObject[obj] = selected;
                }
                if (selected) OnSelect.Invoke(obj);
                else OnDeselect.Invoke(obj);
                OnSelectionChanged();
                m_SelectionLock = false;
            }
        }
        /// <summary>
        /// Callback executed when objects selection is changed.
        /// </summary> 
        protected virtual void OnSelectionChanged()
        {
            if (m_SelectAllToggle != null) m_SelectAllToggle.isOn = m_DisplayedObjects.Count == ObjectsSelected.Length && m_DisplayedObjects.Count > 0 && m_ItemSelection == SelectionType.MultipleItems;
            (this as ISelectionCountable).OnSelectionChanged.Invoke();
        }
        /// <summary>
        /// Callback executed when select all toggle value is changed.
        /// </summary> 
        protected virtual void OnSelectAllToggleValueChanged(bool toggle)
        {
            if (!m_SelectionLock && ItemSelection == SelectionType.MultipleItems)
            {
                m_SelectionLock = true;
                if (toggle) SelectAll();
                else DeselectAll();
                m_SelectionLock = false;
            }
        }
        #endregion
    }
}
