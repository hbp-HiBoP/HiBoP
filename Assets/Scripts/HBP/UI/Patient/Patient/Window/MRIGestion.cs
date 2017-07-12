using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using HBP.Data.Anatomy;

namespace HBP.UI.Anatomy
{
    public class MRIGestion : MonoBehaviour
    {
        #region Properties
        [SerializeField] MRIList m_MRIList;
        [SerializeField] Button m_AddMeshButton;
        [SerializeField] Button m_RemoveMeshButton;
        Data.Patient m_Patient;
        #endregion

        #region Public Methods
        public void Save()
        {
            m_MRIList.SaveAll();
        }
        public void Set(Data.Patient patient)
        {
            m_Patient = patient;
        }
        public void SetInteractable(bool interactable)
        {
            m_AddMeshButton.interactable = interactable;
            m_RemoveMeshButton.interactable = interactable;
        }
        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
            if(active) m_MRIList.Display(m_Patient.Brain.MRIs.ToArray());
        }
        public void AddMRI()
        {
            MRI MRI = new MRI();
            m_MRIList.Add(MRI);
            m_Patient.Brain.MRIs.Add(MRI);
        }
        public void RemoveMRI()
        {
            m_MRIList.Remove(m_MRIList.GetObjectsSelected());
            m_Patient.Brain.MRIs = m_MRIList.Objects.ToList();
        }
        #endregion
    }
}