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
        public enum SelectionType { None, SingleItem, MultipleItems }
        [SerializeField] protected SelectionType m_ItemSelection = SelectionType.MultipleItems;
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
                        if(m_SelectAllToggle) m_SelectAllToggle.interactable = false;
                        DeselectAll();
                        foreach (var item in m_Items) item.Interactable = false;
                        break;
                    case SelectionType.SingleItem:
                        if (m_SelectAllToggle)  m_SelectAllToggle.interactable = false;
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

        [SerializeField] protected Toggle m_SelectAllToggle;

        protected Dictionary<T, bool> m_SelectedStateByObject = new Dictionary<T, bool>();
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

        int ISelectionCountable.NumberOfItemSelected
        {
            get { return ObjectsSelected.Length; }
        }
        UnityEvent ISelectionCountable.OnSelectionChanged { get; } = new UnityEvent();

        public virtual UnityEvent<T> OnSelect { get; } = new GenericEvent<T>();
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
        bool m_SelectionLock;
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
            switch (m_ItemSelection)
            {
                case SelectionType.None:
                    DeselectAll();
                    return;
                case SelectionType.SingleItem:
                    Deselect(m_Objects.Where((o) => !o.Equals(objectToSelect)));
                    break;
            }
            if (m_SelectedStateByObject.ContainsKey(objectToSelect))
            {
                m_SelectedStateByObject[objectToSelect] = true;
            }
            if (GetItemFromObject(objectToSelect, out Item<T> item))
            {
                (item as SelectableItem<T>).Select(true, transition);
            }
            OnSelect.Invoke(objectToSelect);
            OnSelectionChanged();
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
            if (GetItemFromObject(objectToDeselect, out Item<T> item))
            {
                (item as SelectableItem<T>).Select(false, transition);
            }
            OnDeselect.Invoke(objectToDeselect);
            OnSelectionChanged();
        }
        public virtual void Deselect(IEnumerable<T> objectsToDeselect, Toggle.ToggleTransition transition = Toggle.ToggleTransition.None)
        {
            foreach (var obj in objectsToDeselect) Deselect(obj, transition);
        }
        public override bool UpdateObject(T objectToUpdate)
        {
            if (GetItemFromObject(objectToUpdate, out Item<T> item))
            {
                SelectableItem<T> selectableItem = item as SelectableItem<T>;
                selectableItem.Object = objectToUpdate;
                selectableItem.OnChangeSelected.RemoveAllListeners();
                selectableItem.Select(m_SelectedStateByObject[objectToUpdate]);
                selectableItem.OnChangeSelected.AddListener((selected) => OnChangeSelectionState(objectToUpdate, selected));
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
                item.OnChangeSelected.AddListener((selected) => OnChangeSelectionState(obj, selected));
            }
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
            selectableItem.Select(m_SelectedStateByObject[obj]);
            selectableItem.OnChangeSelected.AddListener((selected) => OnChangeSelectionState(obj, selected));
        }
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
                        Deselect(m_Objects.Where((o) => !o.Equals(obj)), Toggle.ToggleTransition.Fade);
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
        protected virtual void OnSelectionChanged()
        {
            if (m_SelectAllToggle != null) m_SelectAllToggle.isOn = m_Objects.Count == ObjectsSelected.Length && m_Objects.Count > 0 && m_ItemSelection == SelectionType.MultipleItems;
            (this as ISelectionCountable).OnSelectionChanged.Invoke();
        }
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
