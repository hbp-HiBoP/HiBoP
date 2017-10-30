using System.Linq;
using Tools.CSharp;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tools.Unity.Lists
{
    [RequireComponent(typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter))]
    public abstract class CustomGrid<T> : MonoBehaviour
    {
        #region Properties
        [SerializeField]
        protected GameObject m_Item;
        [SerializeField]
        protected GameObject m_Container;
        [SerializeField]
        int m_RowMax = 30;
        [SerializeField]
        int m_ColMax = 3;

        protected System.Collections.Generic.List<T> m_Objects = new System.Collections.Generic.List<T>();
        protected System.Collections.Generic.List<Transform> m_Items = new System.Collections.Generic.List<Transform>();
        public virtual T[] Objects { get { return m_Objects.ToArray(); } }
        protected bool m_GridDisplayed = false;

        protected GenericEvent<T,int> m_OnAction = new GenericEvent<T, int> { };
        public GenericEvent<T, int> OnAction { get { return m_OnAction; } }
        #endregion

        #region Public Methods
        public virtual void Display(T[] objectsToDisplay)
        {
            if (!m_GridDisplayed) DisplayGrid();

            System.Collections.Generic.List<T> m_objToAdd = new System.Collections.Generic.List<T>();
            System.Collections.Generic.List<T> m_objToRemove = new System.Collections.Generic.List<T>();
            System.Collections.Generic.List<T> m_objToUpdate = new System.Collections.Generic.List<T>();

            // Find obj to remove.
            foreach (T obj in m_Objects)
            {
                if (!objectsToDisplay.Contains(obj))
                {
                    m_objToRemove.Add(obj);
                }
            }

            // Find obj to add.
            foreach (T obj in objectsToDisplay)
            {
                if (!m_Objects.Contains(obj))
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
            m_GridDisplayed = false;
            m_Objects = new System.Collections.Generic.List<T>();
            for (int i = 0; i < transform.childCount; i++)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
        }
        #endregion

        #region Protected Methods
        protected virtual void DisplayGrid()
        {
            for (int r = 0; r < m_RowMax * m_ColMax; r++)
            {
                GameObject l_gameObject = Instantiate(m_Container);
                ContainerItem l_containerItem = l_gameObject.GetComponent<ContainerItem>();
                l_containerItem.OnAction.AddListener((index, i) => ContainerEvent(index));
                l_gameObject.transform.SetParent(transform);
                l_gameObject.transform.localScale = new Vector3(1, 1, 1);
                l_gameObject.transform.localPosition = new Vector3(0, 0, 0);
            }
            m_GridDisplayed = true;
        }
        protected virtual void Add(T obj)
        {
            GameObject l_gameObject = Instantiate(m_Item);
            Transform l_transform = l_gameObject.transform;
            Set(l_transform,obj);
            ActionnableItem<T> l_item = l_transform.GetComponent<ActionnableItem<T>>();
            l_item.OnAction.AddListener((b, type) => ItemEvent(b, type));
            l_transform.SetParent(GetContainer(l_item.Object));
            l_transform.localScale = new Vector3(1, 1, 1);
            l_transform.transform.localPosition = new Vector3(0, 0, 0);
            m_Objects.Add(obj);
            m_Items.Add(l_transform);
        }
        protected virtual void Remove(T obj)
        {
            int index = m_Objects.FindIndex(o => o.Equals(obj));
            m_Objects.Remove(obj);
            Destroy(m_Items[index].gameObject);
            m_Items.RemoveAt(index);
        }
        protected virtual void UpdateObj(T objectToUpdate)
        {
            T oldObj = m_Objects.Find(obj => obj.Equals(objectToUpdate));
            oldObj = objectToUpdate;
            Set(Get(oldObj), oldObj);
        }
        protected void Set(Transform item,T obj)
        {
            ActionnableItem<T> l_item = item.GetComponent<ActionnableItem<T>>();
            l_item.Object = obj;
        }
        protected T Get(Transform item)
        {
            return m_Objects[m_Items.FindIndex(t => t.Equals(item))];
        }
        protected Transform Get(T obj)
        {
            return m_Items[m_Objects.FindIndex(t => obj.Equals(t))];
        }
        protected virtual void OnRectTransformDimensionsChange()
        {
            GridLayoutGroup l_grid = GetComponent<GridLayoutGroup>();
            RectTransform l_rect = transform as RectTransform;
            float width = l_rect.rect.width / m_ColMax;
            float height = width * 0.5f;
            l_grid.cellSize = new Vector2(width, height);
        }
        protected virtual Position PositionFromIndex(int index)
        {
            return new Position(index / m_ColMax + 1, index % m_ColMax + 1);
        }
        protected virtual int IndexFromPosition(Position position)
        {
            return (position.Row - 1) * m_ColMax + (position.Column - 1);
        }
        protected virtual Transform GetContainer(T obj)
        {
            return transform.GetChild(IndexFromPosition(GetPosition(obj)));
        }
        protected virtual void ItemEvent(T obj, int type)
        {
            OnAction.Invoke(obj, type);
            if (type == 1) Remove(obj);
            else if (type == 2) MoveObjToMousePosition(obj);
        }
        protected virtual void MoveObjToMousePosition(T obj)
        {
            System.Collections.Generic.List<RaycastResult> l_raycastResult = new System.Collections.Generic.List<RaycastResult>();
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = Input.mousePosition;
            EventSystem.current.RaycastAll(eventData, l_raycastResult);
            foreach(RaycastResult result in l_raycastResult)
            {
                if (result.gameObject.tag == "Container")
                {
                    Transform container = result.gameObject.transform;
                    Position newPosition = PositionFromIndex(container.GetSiblingIndex());
                    ActionnableItem<T> item = container.GetComponentInChildren<ActionnableItem<T>>();
                    
                    if(item != null)
                    {
                        T oldOBj = item.Object;
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
            OnAction.Invoke(obj,-1);
        }

        protected abstract T CreateObjAtPosition(Position position);
        protected abstract void SetPosition(T obj, Position position);
        protected abstract Position GetPosition(T obj);
        #endregion

    }
}