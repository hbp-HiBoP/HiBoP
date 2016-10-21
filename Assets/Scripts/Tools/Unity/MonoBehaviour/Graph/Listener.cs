using UnityEngine;
using System.Collections;

 namespace Tools.Unity.Graph
{
    [RequireComponent(typeof(DisplayGestion))]
    public class Listener : MonoBehaviour
    {
        #region Properties
        DisplayGestion m_displayGestion;
        RectTransform m_rect;
        Rect m_lastRect;
        float m_lastHeight;
        float m_lastWidth;
        Vector2 m_alreadyMoved;
        Vector2 m_initialMousePosition;
        bool m_firstUse = true;

        #endregion

        #region Public Methods
        public void OnBeginDrag()
        {
            m_alreadyMoved = new Vector2();
            m_initialMousePosition = Input.mousePosition;
        }

        public void OnDrag()
        {
            Vector2 l_command = (Vector2)Input.mousePosition - m_initialMousePosition;
            l_command = new Vector2(l_command.x / m_rect.rect.width, l_command.y / m_rect.rect.height);
            Vector2 l_alreadyMovedTemp = l_command;
            l_command -= m_alreadyMoved;
            m_displayGestion.Move(l_command);
            m_alreadyMoved = l_alreadyMovedTemp;
        }

        public void OnScroll()
        {
            float l_scroll = Input.mouseScrollDelta.y;
            if(l_scroll > 0)
            {
                m_displayGestion.Zoom();
            }
            else if(l_scroll < 0)
            {
                m_displayGestion.Dezoom();
            }
        }
        #endregion

        #region Private Methods
        void Awake()
        {
            m_displayGestion = GetComponent<DisplayGestion>();
            m_rect = GetComponent<RectTransform>();
            m_lastHeight = m_rect.rect.height;
            m_lastWidth = m_rect.rect.width;
        }

        void OnRectTransformDimensionsChange()
        {
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(c_OnRect());
            }
        }

        IEnumerator c_OnRect()
        {
            yield return new WaitForEndOfFrame();
            OnRect();
        }

        void OnRect()
        {
            Rect l_actualRect = m_rect.rect;
            if (m_lastHeight != l_actualRect.height || m_lastWidth != l_actualRect.width)
            {
                if (m_firstUse)
                {
                    m_lastHeight = l_actualRect.height;
                    m_lastWidth = l_actualRect.width;
                    m_firstUse = false;
                }
                else
                {
                    Vector2 l_command = Vector2.zero;
                    l_command.x = (l_actualRect.width - m_lastWidth) / m_lastWidth;
                    l_command.y = (l_actualRect.height - m_lastHeight) / m_lastHeight;
                    m_lastHeight = l_actualRect.height;
                    m_lastWidth = l_actualRect.width;
                    m_displayGestion.ChangeRectSize(l_command);
                }
            }
        }
        #endregion
    }
}