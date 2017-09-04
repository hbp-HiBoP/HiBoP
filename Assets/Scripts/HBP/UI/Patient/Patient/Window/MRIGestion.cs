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
        [SerializeField] Text m_MRICounter;
        [SerializeField] Button m_AddMeshButton;
        [SerializeField] Button m_RemoveMeshButton;
        Data.Patient m_Patient;
        bool m_Interactable;
        public virtual bool interactable
        {
            get { return m_Interactable; }
            set
            {
                m_Interactable = value;
                m_AddMeshButton.interactable = interactable;
                m_RemoveMeshButton.interactable = interactable;
                m_MRIList.interactable = interactable;
            }
        }
        #endregion

        #region Public Methods
        public void Save()
        {
            m_MRIList.SaveAll();
        }
        public void Set(Data.Patient patient)
        {
            m_Patient = patient;
            m_MRIList.OnSelectionChanged.RemoveAllListeners();
            m_MRIList.OnSelectionChanged.AddListener((mesh, i) => m_MRICounter.text = m_MRIList.ObjectsSelected.Length.ToString());
        }
        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
            if(active) m_MRIList.Objects = m_Patient.Brain.MRIs.ToArray();
        }
        public void AddMRI()
        {
            MRI MRI = new MRI();
            m_MRIList.Add(MRI);
            m_Patient.Brain.MRIs.Add(MRI);
        }
        public void RemoveMRI()
        {
            m_MRIList.Remove(m_MRIList.ObjectsSelected);
            m_Patient.Brain.MRIs = m_MRIList.Objects.ToList();
        }
        #endregion
    }
}