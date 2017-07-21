using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using HBP.Data.Anatomy;

namespace HBP.UI.Anatomy
{
    public class OthersGestion : MonoBehaviour
    {
        #region Properties
        [SerializeField] Dropdown m_EpilepsyDropdown;
        Data.Patient m_Patient;
        #endregion

        #region Public Methods
        public void Save()
        {
            m_Patient.Brain.Epilepsy.Type = (Epilepsy.EpilepsyType) m_EpilepsyDropdown.value;
        }
        public void Set(Data.Patient patient)
        {
            m_Patient = patient;
            m_EpilepsyDropdown.options = (from type in System.Enum.GetNames(typeof(Epilepsy.EpilepsyType)) select new Dropdown.OptionData(type)).ToList();
            m_EpilepsyDropdown.value = (int) patient.Brain.Epilepsy.Type;
        }
        public void SetInteractable(bool interactable)
        {
            m_EpilepsyDropdown.interactable = interactable;
        }
        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
        }
        #endregion
    }
}