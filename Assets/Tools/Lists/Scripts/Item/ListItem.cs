using UnityEngine;
using UnityEngine.UI;

namespace Tools.Unity.Lists
{
    [RequireComponent(typeof(Toggle))]
    public abstract class ListItem<T> : MonoBehaviour
    {
        protected SelectEvent selectEvent = new SelectEvent();
        public SelectEvent SelectEvent { get { return selectEvent; } }
         
        #region Properties
        protected T m_object;
        public T Object
        {
            get { return m_object; }
            protected set { m_object = value; SetObject(value); }
        }

        public bool isOn
        {
            get { return GetComponent<Toggle>().isOn; }
            set { if (isInteractable) { GetComponent<Toggle>().isOn = value; } }
        }

        public bool isInteractable
        {
            get { return GetComponent<Toggle>().interactable; }
            set { if (!value) isOn = value; GetComponent<Toggle>().interactable = value;  }
        }
        #endregion

        #region Public Methods
        public void ChangeSelectState()
        {
            SelectEvent.Invoke();
        }

        public virtual void Set(T objectToSet,Rect rect)
        {
            Object = objectToSet;
            GetComponent<CanvasRenderer>().EnableRectClipping(rect);
        }

        public T Get()
        {
            return Object;
        }
        #endregion

        #region Abstract Methods
        protected abstract void SetObject(T objectToSet);
        #endregion
    }
}
