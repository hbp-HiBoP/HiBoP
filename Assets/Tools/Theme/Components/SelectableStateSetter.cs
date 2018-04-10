using UnityEngine;
using UnityEngine.Events;

namespace NewTheme
{
    [RequireComponent(typeof(UnityEngine.UI.Selectable))]
    public class SelectableStateSetter : MonoBehaviour
    {
        #region Properties
        UnityEngine.UI.Selectable m_Selectable;
        bool m_lastState;
        public State Interactable;
        public State NotInteractble;
        public StateEvent OnChangeState;
        #endregion

        #region Private Methods
        void Start()
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
                Debug.Log("ChangeState");
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