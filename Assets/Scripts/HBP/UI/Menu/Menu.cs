using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace HBP.UI
{
    public class Menu : MonoBehaviour
    {
        #region Properties
        [SerializeField]
        RectTransform[] m_subMenu;

        [SerializeField]
        InteractableConditions[] m_buttonGestion;

        bool m_isOnMenu;
        #endregion

        #region Public Methods
        public void OnPointerEnter(int index)
        {
            if(m_isOnMenu)
            {
                Set(index);
            }
        }
        public void OnClick(int index)
        {
            m_isOnMenu = !m_isOnMenu;
            Set(index);
        }
        public void CloseAll()
        {
            m_isOnMenu = false;
            foreach (RectTransform rect in m_subMenu)
            {
                rect.gameObject.SetActive(false);
            }
        }
        #endregion

        #region Private Methods
        void Set(int index)
        {
            for (int i = 0; i < m_subMenu.Length; i++)
            {
                if (i == index && m_buttonGestion[i].interactable)
                {
                    m_subMenu[i].gameObject.SetActive(!m_subMenu[i].gameObject.activeSelf);
                }
                else
                {
                    m_subMenu[i].gameObject.SetActive(false);
                }
            }
        }
        #endregion

        #region Unity Region
        void Update()
        {
            if(Input.GetMouseButtonUp(0))
            {
                PointerEventData pointer = new PointerEventData(EventSystem.current);
                // convert to a 2D position
                pointer.position = Input.mousePosition;
                List<RaycastResult> raycastResults = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointer, raycastResults);
                if (raycastResults.Count > 0)
                {
                    if(raycastResults[0].gameObject.layer != 23)
                    {
                        //Close All
                        CloseAll();
                    }
                }
            }
        }
        #endregion
    }
}