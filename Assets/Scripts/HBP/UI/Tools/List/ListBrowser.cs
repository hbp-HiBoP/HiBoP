using UnityEngine;
using UnityEngine.Events;

namespace HBP.UI.Tools.Lists
{
    public class ListBrowser : MonoBehaviour
    {
        #region Properties
        public enum Direction { UpDown, LeftRight }

        [SerializeField] private Direction m_Direction = Direction.UpDown;
        [SerializeField] private float m_KeyHoldDelay = 0.5f;
        [SerializeField] private float m_KeyHoldRepeatRate = 0.1f;

        private float m_DownKeyHoldTimer = 0f;
        private float m_UpKeyHoldTimer = 0f;

        private Selector m_ParentSelector;
        #endregion

        #region Events
        public UnityEvent OnSelectNext = new();
        public UnityEvent OnSelectPrevious = new();
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_ParentSelector = GetComponentInParent<Selector>();
        }
        private void Update()
        {
            if (m_ParentSelector != null && !m_ParentSelector.Selected)
                return;

            if ((Input.GetKeyDown(KeyCode.DownArrow) && m_Direction == Direction.UpDown) || (Input.GetKeyDown(KeyCode.RightArrow) && m_Direction == Direction.LeftRight))
            {
                OnSelectNext.Invoke();
                m_DownKeyHoldTimer = 0f;
            }
            else if ((Input.GetKey(KeyCode.DownArrow) && m_Direction == Direction.UpDown) || (Input.GetKey(KeyCode.RightArrow) && m_Direction == Direction.LeftRight))
            {
                m_DownKeyHoldTimer += Time.deltaTime;
                if (m_DownKeyHoldTimer >= m_KeyHoldDelay)
                {
                    if (m_DownKeyHoldTimer - m_KeyHoldDelay >= m_KeyHoldRepeatRate)
                    {
                        OnSelectNext.Invoke();
                        m_DownKeyHoldTimer -= m_KeyHoldRepeatRate;
                    }
                }
            }
            else
            {
                m_DownKeyHoldTimer = 0f;
            }

            if ((Input.GetKeyDown(KeyCode.UpArrow) && m_Direction == Direction.UpDown) || (Input.GetKeyDown(KeyCode.LeftArrow) && m_Direction == Direction.LeftRight))
            {
                OnSelectPrevious.Invoke();
                m_UpKeyHoldTimer = 0f;
            }
            else if ((Input.GetKey(KeyCode.UpArrow) && m_Direction == Direction.UpDown) || (Input.GetKey(KeyCode.LeftArrow) && m_Direction == Direction.LeftRight))
            {
                m_UpKeyHoldTimer += Time.deltaTime;
                if (m_UpKeyHoldTimer >= m_KeyHoldDelay)
                {
                    if (m_UpKeyHoldTimer - m_KeyHoldDelay >= m_KeyHoldRepeatRate)
                    {
                        OnSelectPrevious.Invoke();
                        m_UpKeyHoldTimer -= m_KeyHoldRepeatRate;
                    }
                }
            }
            else
            {
                m_UpKeyHoldTimer = 0f;
            }
        }
        #endregion
    }
}