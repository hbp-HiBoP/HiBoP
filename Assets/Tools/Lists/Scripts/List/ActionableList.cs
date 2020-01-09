using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Tools.Unity.Lists
{
    public class ActionableList<T> : SelectableList<T>
    {
        #region Properties
        [SerializeField] protected bool m_Actionable = true;
        public bool Actionable
        {
            get
            {
                return m_Actionable;
            }
            set
            {
                m_Actionable = value;
                foreach (var item in m_Items.OfType<ActionnableItem<T>>())
                {
                    item.Actionable = value;
                }
            }
        }

        public GenericEvent<T, int> OnAction { get; } = new GenericEvent<T, int>();
        #endregion

        #region Public Methods
        public override bool UpdateObject(T objectToUpdate)
        {
            if (GetItemFromObject(objectToUpdate, out Item<T> item))
            {
                int index = m_Objects.FindIndex((obj) => obj.Equals(objectToUpdate));
                m_Objects[index] = objectToUpdate;
                ActionnableItem<T> actionnableItem = item as ActionnableItem<T>;
                actionnableItem.Object = objectToUpdate;
                actionnableItem.Actionable = Actionable;
                actionnableItem.OnChangeSelected.RemoveAllListeners();
                actionnableItem.Select(m_SelectedStateByObject[objectToUpdate]);
                actionnableItem.OnChangeSelected.AddListener((selected) => OnChangeSelectionState(objectToUpdate, selected));
                actionnableItem.OnAction.RemoveAllListeners();
                actionnableItem.OnAction.AddListener((action) => OnActionHandler(action, objectToUpdate));
                OnUpdateObject.Invoke(objectToUpdate);
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
                item.Actionable = Actionable;
                item.OnChangeSelected.RemoveAllListeners();
                item.Select(m_SelectedStateByObject[obj]);
                item.OnChangeSelected.AddListener((selected) => OnChangeSelectionState(obj, selected));
                item.OnAction.RemoveAllListeners();
                item.OnAction.AddListener((actionID) => OnAction.Invoke(obj, actionID));
            }
        }
        #endregion

        #region Private Methods
        void OnValidate()
        {
            Validate();
        }
        protected override void Validate()
        {
            base.Validate();
            Actionable = Actionable;
        }
        protected void OnActionHandler(int action, T objectToUpdate)
        {
            if (m_Actionable)
            {
                OnAction.Invoke(objectToUpdate, action);
            }
        }
        protected override void SetItem(Item<T> item, T obj)
        {
            base.SetItem(item, obj);
            ActionnableItem<T> actionnableItem = item as ActionnableItem<T>;
            actionnableItem.OnAction.RemoveAllListeners();
            actionnableItem.OnAction.AddListener((actionID) => OnAction.Invoke(obj, actionID));
        }
        #endregion
    }
}