using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI
{
    [RequireComponent(typeof(Button))]
    public class ButtonGestion : MonoBehaviour
    {
        #region Properties
        [SerializeField]
        Color m_interactableColor;
        public Color InteractableColor
        {
            get { return m_interactableColor; }
            set { SetInteractableColor(value); }
        }

        [SerializeField]
        Color m_notInteractableColor;
        public Color NotInteractableColor
        {
            get { return m_notInteractableColor; }
            set { SetNotInteractableColor(value); }
        }

        [SerializeField]
        bool m_interactable = true;
        public bool interactable
        {
            get { return m_interactable; }
            set { SetInteractable(value); }
        }



        [SerializeField]
        bool m_needProject;
        public bool NeedProject { get { return m_needProject; } }
        [SerializeField]
        bool m_needPatients;
        public bool NeedPatients { get { return m_needPatients; } }
        [SerializeField]
        bool m_needGroups;
        public bool NeedGroups { get { return m_needGroups; } }
        [SerializeField]
        bool m_needProtocols;
        public bool NeedProtocols { get { return m_needProtocols; } }
        [SerializeField]
        bool m_needDataset;
        public bool NeedDataset { get { return m_needDataset; } }

        [SerializeField]
        Text[] m_texts;

        #endregion

        #region Private Method
        void SetInteractable(bool interactable)
        {
            Button l_button = GetComponent<Button>();
            m_interactable = interactable;
            l_button.interactable = interactable;
            foreach (Text text in m_texts)
            {
                if (interactable)
                {
                    text.color = InteractableColor;
                }
                else
                {
                    text.color = NotInteractableColor;
                }
            }
        }

        void SetInteractableColor(Color color)
        {
            m_interactableColor = color;
            foreach (Text text in m_texts)
            {
                if (interactable)
                {
                    text.color = InteractableColor;
                }
            }
        }

        void SetNotInteractableColor(Color color)
        {
            m_notInteractableColor = color;
            foreach (Text text in m_texts)
            {
                if (!interactable)
                {
                    text.color = NotInteractableColor;
                }
            }
        }
        #endregion
    }

}