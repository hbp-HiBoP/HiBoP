using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using HBP.Data.Anatomy;

namespace HBP.UI.Anatomy
{
    public class TransformationGestion : MonoBehaviour
    {
        #region Properties
        [SerializeField] TransformationList m_TransformationList;
        [SerializeField] Button m_AddTransformation;
        [SerializeField] Button m_RemoveTransformation;
        Data.Patient m_Patient;
        bool m_Interactable;
        public bool interactable
        {
            get { return m_Interactable; }
            set
            {
                m_Interactable = value;
                m_AddTransformation.interactable = interactable;
                m_RemoveTransformation.interactable = interactable;
                m_TransformationList.interactable = interactable;
            }
        }
        #endregion

        #region Public Methods
        public void Save()
        {
            m_TransformationList.SaveAll();
        }
        public void Set(Data.Patient patient)
        {
            m_Patient = patient;
        }
        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
            if (active) m_TransformationList.Objects = m_Patient.Brain.Transformations.ToArray();
        }
        public void AddTransformation()
        {
            Transformation transformation = new Transformation();
            m_TransformationList.Add(transformation);
            m_Patient.Brain.Transformations.Add(transformation);
        }
        public void RemoveTransformation()
        {
            m_TransformationList.Remove(m_TransformationList.ObjectsSelected);
            m_Patient.Brain.Transformations = m_TransformationList.Objects.ToList();
        }
        #endregion
    }
}