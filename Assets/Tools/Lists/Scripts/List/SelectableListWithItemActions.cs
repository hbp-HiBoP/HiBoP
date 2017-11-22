using UnityEngine;
using UnityEngine.Events;

namespace Tools.Unity.Lists
{
    public class SelectableListWithItemAction<T> : SelectableList<T>
    {
        #region Properties
        protected GenericEvent<T, int> m_OnAction = new GenericEvent<T, int>();
        public GenericEvent<T, int> OnAction { get { return m_OnAction; } }
        #endregion

        #region Private Methods
        protected override void SpawnItem(int number)
        {
            int end = Mathf.Min(m_End + number, m_NumberOfObjects - 1);
            for (int i = m_Start; i <= end; i++)
            {
                T obj = m_Objects[i];
                if (!m_ItemByObject.ContainsKey(obj))
                {
                    ActionnableItem<T> item = Instantiate(ItemPrefab, m_ScrollRect.content).GetComponent<ActionnableItem<T>>();
                    RectTransform itemRectTransform = item.transform as RectTransform;
                    itemRectTransform.sizeDelta = new Vector2(0, itemRectTransform.sizeDelta.y);
                    itemRectTransform.localPosition = new Vector3(itemRectTransform.localPosition.x, -i * ItemHeight, itemRectTransform.localPosition.z);
                    m_ItemByObject.Add(obj, item);
                    item.OnChangeSelected.RemoveAllListeners();
                    item.Select(m_SelectedStateByObject[obj]);
                    item.OnChangeSelected.AddListener((selected) => OnSelection(obj, selected));
                    item.OnAction.RemoveAllListeners();
                    item.OnAction.AddListener((actionID) => m_OnAction.Invoke(obj, actionID));
                    item.Object = obj;
                }
            };
        }
        protected override void MoveItemsDownwards(int deplacement)
        {
            for (int i = 0; i < deplacement; i++)
            {
                T obj = m_Objects[m_Start + i];
                ActionnableItem<T> item = m_ItemByObject[obj] as ActionnableItem<T>;
                m_ItemByObject.Remove(obj);
                T newObj = m_Objects[m_End + 1 + i];
                m_ItemByObject.Add(newObj, item);
                item.transform.localPosition = new Vector3(item.transform.localPosition.x, -(m_End + 1 + i) * ItemHeight, item.transform.localPosition.z);
                item.OnChangeSelected.RemoveAllListeners();
                item.Select(m_SelectedStateByObject[newObj]);
                item.OnChangeSelected.AddListener((selected) => OnSelection(newObj, selected));
                item.OnAction.RemoveAllListeners();
                item.OnAction.AddListener((actionID) => m_OnAction.Invoke(newObj, actionID));
                item.Object = newObj;
            }
        }
        protected override void MoveItemsUpwards(int deplacement)
        {
            for (int i = 0; i > deplacement; i--)
            {
                T obj = m_Objects[m_End + i];
                ActionnableItem<T> item = m_ItemByObject[obj] as ActionnableItem<T>;
                m_ItemByObject.Remove(obj);
                T newObj = m_Objects[m_Start - 1 + i];
                m_ItemByObject.Add(newObj, item);
                item.transform.localPosition = new Vector3(item.transform.localPosition.x, -(m_Start - 1 + i) * ItemHeight, item.transform.localPosition.z);
                item.OnChangeSelected.RemoveAllListeners();
                item.Select(m_SelectedStateByObject[newObj]);
                item.OnChangeSelected.AddListener((selected) => OnSelection(newObj, selected));
                item.OnAction.RemoveAllListeners();
                item.OnAction.AddListener((actionID) => m_OnAction.Invoke(newObj, actionID));
                item.Object = newObj;
            }
        }
        #endregion
    }
}
