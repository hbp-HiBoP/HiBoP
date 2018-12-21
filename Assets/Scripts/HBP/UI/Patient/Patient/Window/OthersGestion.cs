using UnityEngine;
using UnityEngine.UI;
using HBP.Data.Anatomy;
using Tools.Unity;

namespace HBP.UI.Anatomy
{
    public class OthersGestion : MonoBehaviour
    {
        #region Properties
        [SerializeField] Dropdown m_EpilepsyDropdown;

        protected Data.Patient m_Patient;

        protected bool m_Interactable;
        public bool Interactable
        {
            get
            {
                return m_Interactable;
            }
            set
            {
                m_Interactable = value;
                m_EpilepsyDropdown.interactable = Interactable;
            }
        }
        #endregion

        #region Public Methods
        public void Initialize()
        {
     
        }
        public void Save()
        {
            m_Patient.Brain.Epilepsy.Type = (Epilepsy.EpilepsyType) m_EpilepsyDropdown.value;
        }
        public void Set(Data.Patient patient)
        {
            m_Patient = patient;
            m_EpilepsyDropdown.Set(typeof(Epilepsy.EpilepsyType), (int)patient.Brain.Epilepsy.Type);
        }
        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
        }
        #endregion
    }
}