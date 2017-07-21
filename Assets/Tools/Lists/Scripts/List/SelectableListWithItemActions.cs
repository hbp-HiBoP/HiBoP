using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Linq;

namespace Tools.Unity.Lists
{
    public abstract class SelectableListWithItemAction<T> : SelectableList<T>
    {
        protected GenericEvent<T, int> m_OnAction = new GenericEvent<T,int>();
        public GenericEvent<T,int> OnAction { get { return m_OnAction; } }

        public override void Add(T objectToAdd)
        {
            if (!m_ObjectsToItems.Keys.Contains(objectToAdd))
            {
                GameObject prefab = m_Prefabs.First((p) => p.GetComponent<ActionnableItem<T>>().Type == objectToAdd.GetType());
                ActionnableItem<T> item = Instantiate(prefab, transform).GetComponent<ActionnableItem<T>>();
                m_ObjectsToItems.Add(objectToAdd, item);
                item.Object = objectToAdd;
                if (!m_MultiSelection) item.GetComponent<Toggle>().group = m_ToggleGroup;
                item.OnChangeSelected.AddListener((selected) => OnSelectionChanged.Invoke(objectToAdd, selected));
                item.OnAction.RemoveAllListeners();
                item.OnAction.AddListener((obj, i) => m_OnAction.Invoke(obj, i));
            }
        }
    }
}