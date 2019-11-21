using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace HBP.UI
{
    [RequireComponent(typeof(Toggle))]
    public class Tab : MonoBehaviour //, IBeginDragHandler, IDropHandler
    {
        #region Properties
        [SerializeField] Text m_Text;
        public string Title
        {
            get { return m_Text.text;  }
            set { m_Text.text = value; }
        }

        Toggle m_Toggle;
        public ToggleGroup Group
        {
            get { return m_Toggle.group; }
            set { m_Toggle.group = value; }
        }

        bool m_IsActive;
        public bool IsActive
        {
            get { return m_IsActive; }
            set { m_IsActive = value;  m_Toggle.isOn = value; OnValueChanged.Invoke(value); }
        }

        UnityEvent<bool> m_OnValueChanged = new Toggle.ToggleEvent();
        public UnityEvent<bool> OnValueChanged
        {
            get { return m_OnValueChanged; }
        }
        #endregion

        #region Events
        //public void OnBeginDrag(PointerEventData data)
        //{
        //    Debug.Log("OnBeginDrag");
        //    GetComponentInParent<TabGestion>().OnBeginDrag(data.pointerCurrentRaycast.gameObject.transform.parent.parent);
        //}

        //public void OnDrop(PointerEventData data)
        //{
        //    Debug.Log("OnDrop");
        //    GetComponentInParent<TabGestion>().OnEndDrag(data.pointerCurrentRaycast.gameObject.transform.parent.parent);
        //}
        #endregion

        #region Private Methods
        void Awake()
        {
            m_Toggle = GetComponent<Toggle>();
            m_Toggle.onValueChanged.AddListener((value) => IsActive = value);
        }
        #endregion
    }
}
