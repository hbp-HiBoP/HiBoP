using UnityEngine;
using UnityEngine.EventSystems;

namespace HBP.UI.Tools
{
    public class DisplayOnHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private GameObject m_DisplayObject;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (m_DisplayObject != null)
            {
                m_DisplayObject.SetActive(true);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (m_DisplayObject != null)
            {
                m_DisplayObject.SetActive(false);
            }
        }
    }
}