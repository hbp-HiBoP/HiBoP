using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace Tools.Unity
{
    [RequireComponent(typeof(Selectable))]
    public class OnInteractableSelectableDecorator : MonoBehaviour
    {
        bool m_Interactable;
        Selectable m_Selectable;

        [SerializeField] BoolEvent m_OnChangeValue;
        public BoolEvent OnChangeValue => m_OnChangeValue;

        void OnEnable()
        {
            m_Selectable = GetComponent<Selectable>();
            m_Interactable = m_Selectable.interactable;
        }

        private void Update()
        {
            if(m_Interactable != m_Selectable.interactable)
            {
                m_Interactable = m_Selectable.interactable;
                m_OnChangeValue.Invoke(m_Interactable);
            }
        }

    }
}