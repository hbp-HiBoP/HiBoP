using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace HBP.UI
{
    public class Tab : MonoBehaviour, IBeginDragHandler, IDropHandler, IEndDragHandler, IDragHandler
    {
        #region Attributs
        private Toggle m_toggle;
        public bool IsOn { get { return m_toggle.isOn; } set { m_toggle.isOn = value; } }

        #endregion

        #region Events
        public void OnBeginDrag(PointerEventData data)
        {
            GetComponentInParent<TabGestion>().OnBeginDrag(data.pointerCurrentRaycast.gameObject.transform.parent.parent);
        }

        public void OnDrop(PointerEventData data)
        {
            GetComponentInParent<TabGestion>().OnEndDrag(data.pointerCurrentRaycast.gameObject.transform.parent.parent);
        }

        public void OnDrag(PointerEventData data)
        {
        }

        public void OnEndDrag(PointerEventData data)
        {
        }

        public void OnChangeState()
        {
            if(IsOn)
            {
                m_toggle.group.GetComponent<TabGestion>().OnChangeSelectedTab();
            }
        }
        #endregion

        #region Private Methods
        void Awake()
        {
            m_toggle = GetComponent<Toggle>();
        }
        #endregion

    }
}
