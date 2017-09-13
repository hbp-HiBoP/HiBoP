using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using HBP.Data.Anatomy;

namespace HBP.UI.Anatomy
{
    public class ImplantationGestion : MonoBehaviour
    {
        #region Properties
        [SerializeField] ImplantationList m_ImplantationList;
        [SerializeField] Text m_ImplantationCounter;
        [SerializeField] Button m_AddButton;
        [SerializeField] Button m_RemoveButton;
        Data.Patient m_Patient;
        bool m_Interactable;
        public virtual bool interactable
        {
            get { return m_Interactable; }
            set
            {
                m_Interactable = value;
                m_AddButton.interactable = interactable;
                m_RemoveButton.interactable = interactable;
                m_ImplantationList.interactable = interactable;
            }
        }
        #endregion

        #region Public Methods
        public void Save()
        {
            m_ImplantationList.SaveAll();
        }
        public void Set(Data.Patient patient)
        {
            m_Patient = patient;
            m_ImplantationList.OnSelectionChanged.RemoveAllListeners();
            m_ImplantationList.OnSelectionChanged.AddListener((mesh, i) => m_ImplantationCounter.text = m_ImplantationList.ObjectsSelected.Length.ToString());
        }
        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
            if (active) m_ImplantationList.Objects = m_Patient.Brain.Implantations.ToArray();
            else m_ImplantationList.SaveAll();
        }
        public void AddImplantation()
        {
            Implantation Implantation = new Implantation();
            m_ImplantationList.Add(Implantation);
            m_Patient.Brain.Implantations.Add(Implantation);
        }
        public void RemoveImplantation()
        {
            m_ImplantationList.Remove(m_ImplantationList.ObjectsSelected);
            m_Patient.Brain.Implantations = m_ImplantationList.Objects.ToList();
        }
        #endregion
    }
}