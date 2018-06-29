using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI
{
    public abstract class Window : MonoBehaviour, IClosable, IInteractable
    {
        #region Properties
        public UnityEvent OnClose { get; set; }
        protected bool m_Interactable;
        public bool Interactable
        {
            get
            {
                return m_Interactable;
            }
            set
            {
                m_Interactable = value;
                SetInteractable(value);
            }
        }
        [SerializeField] protected Button m_CloseButton;
        #endregion

        #region Public Methods
        public virtual void Close()
        {
            OnClose.Invoke();
            Destroy(gameObject);        
        }
        public static Window Open(bool interactable)
        {
            // GetType
            Type type = MethodBase.GetCurrentMethod().DeclaringType;

            // GetPrefab
            GameObject prefab = FindObjectOfType<WindowsReferencer>().GetPrefab(type);

            // Instantiate
            GameObject go = Instantiate(prefab, GameObject.Find("Windows").transform);
            go.transform.localPosition = Vector3.zero;

            // SetWindow
            Window window = go.GetComponent<Window>();
            window.Interactable = interactable;

            return window;
        }
        #endregion

        #region Private Methods
        protected abstract void SetInteractable(bool interactable);
        protected virtual void Initialize()
        {
            m_CloseButton.onClick.AddListener(Close);
        }
        #endregion
    }
}
