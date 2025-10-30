using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Main
{
    [RequireComponent(typeof(Button)), RequireComponent(typeof(InteractableConditions))]
    public class MenuButton : MonoBehaviour
    {
        #region Properties
        private Button m_Button;
        private InteractableConditions m_InteractableConditions;
        private UnityAction m_Action;
        #endregion

        #region Public Methods
        public void Initialize(Menu parentMenu, UnityAction action)
        {
            m_Button = GetComponent<Button>();
            m_InteractableConditions = GetComponent<InteractableConditions>();
            m_Action = () =>
            {
                if (m_InteractableConditions.interactable)
                {
                    action.Invoke();
                    parentMenu.Close();
                }
            };

            m_Button.onClick.AddListener(m_Action);
        }
        public void Action()
        {
            m_Action.Invoke();
        }
        #endregion
    }
}