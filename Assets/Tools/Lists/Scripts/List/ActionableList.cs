using System.Linq;
using UnityEngine.Events;

namespace Tools.Unity.Lists
{
    public class ActionableList<T> : SelectableList<T>
    {
        #region Properties
        protected GenericEvent<T, int> m_OnAction = new GenericEvent<T, int>();
        public GenericEvent<T, int> OnAction { get { return m_OnAction; } }
        #endregion

        #region Public Methods
        public override bool UpdateObject(T objectToUpdate)
        {
            Item<T> item;
            if (GetItemFromObject(objectToUpdate, out item))
            {
                int index = m_Objects.FindIndex((obj) => obj.Equals(objectToUpdate));
                m_Objects[index] = objectToUpdate;
                ActionnableItem<T> actionnableItem = item as ActionnableItem<T>;
                actionnableItem.Object = objectToUpdate;
                actionnableItem.OnChangeSelected.RemoveAllListeners();
                actionnableItem.Select(m_SelectedStateByObject[objectToUpdate]);
                actionnableItem.OnChangeSelected.AddListener((selected) => OnChangeSelectionState(objectToUpdate, selected));
                actionnableItem.OnAction.RemoveAllListeners();
                actionnableItem.OnAction.AddListener((actionID) => m_OnAction.Invoke(objectToUpdate, actionID));
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
                ActionnableItem<T> item = items[j] as ActionnableItem<T>;
                T obj = m_Objects[i];
                item.Object = obj;
                item.OnChangeSelected.RemoveAllListeners();
                item.Select(m_SelectedStateByObject[obj]);
                item.OnChangeSelected.AddListener((selected) => OnChangeSelectionState(obj, selected));
                item.OnAction.RemoveAllListeners();
                item.OnAction.AddListener((actionID) => m_OnAction.Invoke(obj, actionID));
            }
        }
        #endregion

        #region Private Methods
        protected override void SetItem(Item<T> item, T obj)
        {
            base.SetItem(item, obj);
            ActionnableItem<T> actionnableItem = item as ActionnableItem<T>;
            actionnableItem.OnAction.RemoveAllListeners();
            actionnableItem.OnAction.AddListener((actionID) => m_OnAction.Invoke(obj, actionID));
        }
        #endregion
    }
}
