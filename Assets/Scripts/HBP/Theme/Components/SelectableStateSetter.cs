using UnityEngine;

namespace HBP.Theme
{
    [RequireComponent(typeof(UnityEngine.UI.Selectable))]
    public class SelectableStateSetter : MonoBehaviour
    {
        #region Properties
        public State Interactable;
        public State NotInteractble;
        public StateEvent OnChangeState;

        UnityEngine.UI.Selectable m_Selectable;
        bool m_lastState;
        #endregion

        #region Private Methods
        void OnEnable()
        {
            m_Selectable = GetComponent<UnityEngine.UI.Selectable>();
            m_lastState = m_Selectable.interactable;
            Send();
        }

        // Update is called once per frame
        void Update()
        {
            if (m_lastState != m_Selectable.interactable)
            {
                m_lastState = m_Selectable.interactable;
                Send();
            }
        }

        void Send()
        {
            if (m_Selectable.interactable)
            {
                OnChangeState.Invoke(Interactable);
            }
            else
            {
                OnChangeState.Invoke(NotInteractble);
            }
        }
        #endregion
    }
}