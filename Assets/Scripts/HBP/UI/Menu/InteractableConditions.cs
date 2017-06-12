using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI
{
    [RequireComponent(typeof(Selectable))]
    public class InteractableConditions : MonoBehaviour
    {
        #region Properties
        Selectable m_Selectable;
        public bool interactable
        {
            get { return m_Selectable.interactable; }
            set { m_Selectable.interactable = value; }
        }

        [SerializeField, Candlelight.PropertyBackingField]
        private bool m_NeedProject;
        public bool NeedProject
        {
            get { return m_NeedProject; }
            set { m_NeedProject = value; }
        }

        [SerializeField, Candlelight.PropertyBackingField]
        private bool m_NeedPatient;
        public bool NeedPatient
        {
            get { return m_NeedPatient; }
            set { m_NeedPatient = value; }
        }

        [SerializeField, Candlelight.PropertyBackingField]
        private bool m_NeedGroup;
        public bool NeedGroup
        {
            get { return m_NeedGroup; }
            set { m_NeedGroup = value; }
        }

        [SerializeField, Candlelight.PropertyBackingField]
        private bool m_NeedProtocol;
        public bool NeedProtocol
        {
            get { return m_NeedProtocol; }
            set { m_NeedProtocol = value; }
        }

        [SerializeField, Candlelight.PropertyBackingField]
        private bool m_NeedDataset;
        public bool NeedDataset
        {
            get { return m_NeedDataset; }
            set { m_NeedDataset = value; }
        }
        #endregion

        #region Private Method
        void OnEnable()
        {
            m_Selectable = GetComponent<Selectable>();
        }
        #endregion
    }

}