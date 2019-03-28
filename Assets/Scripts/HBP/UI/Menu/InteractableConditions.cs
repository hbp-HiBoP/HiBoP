using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace HBP.UI
{
    [RequireComponent(typeof(Selectable))]
    public class InteractableConditions : MonoBehaviour
    {
        #region Properties
        Selectable m_Selectable;
        public bool interactable
        {
            get
            {
                if(m_Selectable == null)
                {
                    m_Selectable = GetComponent<Selectable>();
                }
                return m_Selectable.interactable;
            }
            set
            {
                if (m_Selectable == null)
                {
                    m_Selectable = GetComponent<Selectable>();
                }
                m_Selectable.interactable = value;
            }
        }

        [SerializeField]
        private bool m_NeedProject;
        public bool NeedProject
        {
            get { return m_NeedProject; }
            set { m_NeedProject = value; }
        }

        [SerializeField]
        private bool m_NeedPatient;
        public bool NeedPatient
        {
            get { return m_NeedPatient; }
            set { m_NeedPatient = value; }
        }

        [SerializeField]
        private bool m_NeedGroup;
        public bool NeedGroup
        {
            get { return m_NeedGroup; }
            set { m_NeedGroup = value; }
        }

        [SerializeField]
        private bool m_NeedProtocol;
        public bool NeedProtocol
        {
            get { return m_NeedProtocol; }
            set { m_NeedProtocol = value; }
        }

        [SerializeField]
        private bool m_NeedDataset;
        public bool NeedDataset
        {
            get { return m_NeedDataset; }
            set { m_NeedDataset = value; }
        }
        #endregion
    }

}