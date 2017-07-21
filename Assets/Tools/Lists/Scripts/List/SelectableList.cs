using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Linq;
using System.Collections.Generic;

namespace Tools.Unity.Lists
{
    [RequireComponent(typeof(ToggleGroup))]
    public class SelectableList<T> : List<T>
    {
        #region Properties 
        protected ToggleGroup m_ToggleGroup;
        protected GenericEvent<T, bool> m_OnSelectionChanged = new GenericEvent<T, bool>();
        public virtual GenericEvent<T, bool> OnSelectionChanged
        {
            get { return m_OnSelectionChanged; }
        }
        public virtual T[] ObjectsSelected
        {
            get
            {
                return (from couple in m_ObjectsToItems where (couple.Value as SelectableItem<T>).selected select couple.Key).ToArray();
            }
            set
            {
                Deselect(from couple in m_ObjectsToItems.Where((elt) => !value.Contains(elt.Key)) select couple.Key);
                Select(from couple in m_ObjectsToItems.Where((elt) => value.Contains(elt.Key)) select couple.Key);
            }
        }
        [SerializeField, Candlelight.PropertyBackingField]
        protected bool m_MultiSelection;
        public virtual bool MultiSelection
        {
            get
            {
                return m_MultiSelection;
            }
            set
            {
                m_MultiSelection = value;
                if(!value)
                {
                    foreach (var item in m_ObjectsToItems.Values) item.GetComponent<Toggle>().group = m_ToggleGroup;
                    T[] objectSelected = ObjectsSelected;
                    for (int i = 1; i < objectSelected.Length; i++)
                    {
                        Deselect(objectSelected[i]);
                    }
                }
                else
                {
                    foreach (var item in m_ObjectsToItems.Values) item.GetComponent<Toggle>().group = null;
                }
            }
        }
        #endregion

        #region Public Methods
        public override void Add(T objectToAdd)
        {
            if (!m_ObjectsToItems.Keys.Contains(objectToAdd))
            {
                GameObject prefab = m_Prefabs.First((p) => p.GetComponent<SelectableItem<T>>().Type == objectToAdd.GetType());
                SelectableItem<T> item = Instantiate(prefab, transform).GetComponent<SelectableItem<T>>();
                m_ObjectsToItems.Add(objectToAdd, item);
                item.Object = objectToAdd;
                if(!m_MultiSelection) item.GetComponent<Toggle>().group = m_ToggleGroup;
                item.OnChangeSelected.AddListener((selected) => OnSelectionChanged.Invoke(objectToAdd, selected));
            }
        }
        public virtual void SelectAll()
        {
            Select(m_ObjectsToItems.Keys);
        }
        public virtual void DeselectAll()
        {
            Deselect(m_ObjectsToItems.Keys);
        }
        public virtual void Select(T objectToSelect)
        {
            (m_ObjectsToItems[objectToSelect] as SelectableItem<T>).selected = true;
        }
        public virtual void Select(IEnumerable<T> objectsToSelect)
        {
            foreach (var obj in objectsToSelect) Select(obj);
        }
        public virtual void Deselect(T objectToDeselect)
        {
            (m_ObjectsToItems[objectToDeselect] as SelectableItem<T>).selected = false;
        }
        public virtual void Deselect(IEnumerable<T> objectsToDeselect)
        {
            foreach (var obj in objectsToDeselect) Deselect(obj);
        }
        #endregion

        #region Private Methods
        void Awake()
        {
            m_ToggleGroup = GetComponent<ToggleGroup>();
        }
        #endregion
    }
}