using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HBP.UI.Main
{
    [RequireComponent(typeof(InteractableConditions)), RequireComponent(typeof(Button))]
    public class Menu : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        #region Properties
        [SerializeField] RectTransform m_SubMenu;
        private InteractableConditions m_InteractableConditions;
        private Button m_Button;

        bool m_IsOpen;
        public bool IsOpen
        {
            get
            {
                return m_IsOpen;
            }
            set
            {
                if (m_IsOpen != value)
                {
                    m_IsOpen = value;
                    if (m_SubMenu && m_InteractableConditions && m_InteractableConditions.interactable)
                        m_SubMenu.gameObject.SetActive(value);
                    OnChangeOpenState.Invoke(value);
                }
            }
        }

        bool m_IsHovered;
        public bool IsHovered
        {
            get
            {
                return m_IsHovered;
            }
            set
            {
                if (m_IsHovered != value)
                {
                    m_IsHovered = value;
                    OnHover.Invoke(value);
                }
            }
        }

        public GenericEvent<bool> OnChangeOpenState { get; } = new GenericEvent<bool>();
        public GenericEvent<bool> OnHover { get; } = new GenericEvent<bool>();
        #endregion

        #region Private Methods
        protected virtual void Awake()
        {
            m_InteractableConditions = GetComponent<InteractableConditions>();
            m_Button = GetComponent<Button>();
            m_Button.onClick.AddListener(SwapOpenState);
        }
        #endregion

        #region Public Methods
        public void Open()
        {
            IsOpen = true;
        }
        public void Close()
        {
            IsOpen = false;
        }
        public void SwapOpenState()
        {
            IsOpen = !IsOpen;
        }
        public void OnPointerEnter(PointerEventData data)
        {
            IsHovered = true;
        }
        public void OnPointerExit(PointerEventData data)
        {
            IsHovered = false;
        }
        #endregion
    }
}