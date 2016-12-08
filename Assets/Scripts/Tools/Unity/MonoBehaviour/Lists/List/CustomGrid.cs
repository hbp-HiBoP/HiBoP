using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;

namespace Tools.Unity.Lists
{
    [RequireComponent(typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter))]
    public abstract class CustomGrid<T> : MonoBehaviour
    {
        #region Properties
        [SerializeField]
        protected GameObject m_item;
        [SerializeField]
        protected GameObject m_container;
        [SerializeField]
        int m_rowMax = 30;
        [SerializeField]
        int m_colMax = 3;

        protected List<T> m_objects = new List<T>();
        protected List<Transform> m_items = new List<Transform>();
        public virtual T[] Objects { get { return m_objects.ToArray(); } }
        protected bool m_gridDisplayed = false;

        protected ActionEvent<T> m_actionEvent = new ActionEvent<T>{ };
        public ActionEvent<T> ActionEvent { get { return m_actionEvent; } }

        protected struct Position
        {
            public int Row;
            public int Col;
        }
        #endregion

        #region Public Methods
        public virtual void Display(T[] objectsToDisplay)
        {
            if (!m_gridDisplayed) DisplayGrid();

            List<T> m_objToAdd = new List<T>();
            List<T> m_objToRemove = new List<T>();
            List<T> m_objToUpdate = new List<T>();

            // Find obj to remove.
            foreach (T obj in m_objects)
            {
                if (!objectsToDisplay.Contains(obj))
                {
                    m_objToRemove.Add(obj);
                }
            }

            // Find obj to add.
            foreach (T obj in objectsToDisplay)
            {
                if (!m_objects.Contains(obj))
                {
                    m_objToAdd.Add(obj);
                }
                else
                {
                    m_objToUpdate.Add(obj);
                }
            }

            // Remove obj.
            foreach (T obj in m_objToRemove)
            {
                Remove(obj);
            }

            // Add obj.
            foreach (T obj in m_objToAdd)
            {
                Add(obj);
            }

            // Update obj
            foreach (T obj in m_objToUpdate)
            {
                UpdateObj(obj);
            }
        }
        public virtual void Clear()
        {
            m_gridDisplayed = false;
            m_objects = new List<T>();
            for (int i = 0; i < transform.childCount; i++)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
        }
        #endregion

        #region Protected Methods
        protected virtual void DisplayGrid()
        {
            for (int r = 0; r < m_rowMax * m_colMax; r++)
            {
                GameObject l_gameObject = Instantiate(m_container);
                ContainerItem l_containerItem = l_gameObject.GetComponent<ContainerItem>();
                l_containerItem.ActionEvent.AddListener((index, i) => ContainerEvent(index));
                l_gameObject.transform.SetParent(transform);
                l_gameObject.transform.localScale = new Vector3(1, 1, 1);
                l_gameObject.transform.localPosition = new Vector3(0, 0, 0);
            }
            m_gridDisplayed = true;
        }
        protected virtual void Add(T obj)
        {
            GameObject l_gameObject = Instantiate(m_item);
            Transform l_transform = l_gameObject.transform;
            Set(l_transform,obj);
            ListItemWithActions<T> l_item = l_transform.GetComponent<ListItemWithActions<T>>();
            l_item.ActionEvent.AddListener((b, type) => ItemEvent(b, type));
            l_transform.SetParent(GetContainer(l_item.Object));
            l_transform.localScale = new Vector3(1, 1, 1);
            l_transform.transform.localPosition = new Vector3(0, 0, 0);
            m_objects.Add(obj);
            m_items.Add(l_transform);
        }
        protected virtual void Remove(T obj)
        {
            int index = m_objects.FindIndex(o => o.Equals(obj));
            m_objects.Remove(obj);
            Destroy(m_items[index].gameObject);
            m_items.RemoveAt(index);
        }
        protected virtual void UpdateObj(T objectToUpdate)
        {
            T oldObj = m_objects.Find(obj => obj.Equals(objectToUpdate));
            oldObj = objectToUpdate;
            Set(Get(oldObj), oldObj);
        }
        protected void Set(Transform item,T obj)
        {
            ListItemWithActions<T> l_item = item.GetComponent<ListItemWithActions<T>>();
            l_item.Set(obj,transform.GetComponentInParent<RectTransform>().rect);
        }
        protected T Get(Transform item)
        {
            return m_objects[m_items.FindIndex(t => t.Equals(item))];
        }
        protected Transform Get(T obj)
        {
            return m_items[m_objects.FindIndex(t => obj.Equals(t))];
        }
        protected virtual void OnRectTransformDimensionsChange()
        {
            GridLayoutGroup l_grid = GetComponent<GridLayoutGroup>();
            RectTransform l_rect = transform as RectTransform;
            float width = l_rect.rect.width / m_colMax;
            float height = width * 0.5f;
            l_grid.cellSize = new Vector2(width, height);
        }
        protected virtual Position PositionFromIndex(int index)
        {
            Position l_position;
            l_position.Row = index / m_colMax + 1;
            l_position.Col = index % m_colMax + 1;
            return l_position;
        }
        protected virtual int IndexFromPosition(Position position)
        {
            return (position.Row - 1) * m_colMax + (position.Col - 1);
        }
        protected virtual Transform GetContainer(T obj)
        {
            return transform.GetChild(IndexFromPosition(GetPosition(obj)));
        }
        protected virtual void ItemEvent(T obj, int type)
        {
            ActionEvent.Invoke(obj, type);
            if (type == 1) Remove(obj);
            else if (type == 2) MoveObjToMousePosition(obj);
        }
        protected virtual void MoveObjToMousePosition(T obj)
        {
            List<RaycastResult> l_raycastResult = new List<RaycastResult>();
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = Input.mousePosition;
            EventSystem.current.RaycastAll(eventData, l_raycastResult);
            foreach(RaycastResult result in l_raycastResult)
            {
                if (result.gameObject.tag == "Container")
                {
                    Transform container = result.gameObject.transform;
                    Position newPosition = PositionFromIndex(container.GetSiblingIndex());
                    ListItemWithActions<T> item = container.GetComponentInChildren<ListItemWithActions<T>>();
                    
                    if(item != null)
                    {
                        T oldOBj = item.Get();
                        SetPosition(oldOBj, GetPosition(obj));
                        Get(oldOBj).SetParent(GetContainer(oldOBj));
                        UpdateObj(oldOBj);
                    }
                    SetPosition(obj, newPosition);
                    Get(obj).SetParent(GetContainer(obj));
                    UpdateObj(obj);
                }
            }
        }
        protected virtual void ContainerEvent(int i)
        {
            T obj = CreateObjAtPosition(PositionFromIndex(i));
            ActionEvent.Invoke(obj,-1);
        }

        protected abstract T CreateObjAtPosition(Position position);
        protected abstract void SetPosition(T obj, Position position);
        protected abstract Position GetPosition(T obj);
        #endregion

    }
}