using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using HBP.Data.Anatomy;

namespace HBP.UI.Anatomy
{
    public class MeshGestion : MonoBehaviour
    {
        #region Properties
        [SerializeField] MeshList m_MeshList;
        [SerializeField] Button m_AddSingleMeshButton;
        [SerializeField] Button m_AddLeftRightMeshButton;
        [SerializeField] Button m_RemoveMeshButton;
        Data.Patient m_Patient;
        #endregion

        #region Public Methods
        public void Save()
        {
            m_MeshList.SaveAll();
        }
        public void Set(Data.Patient patient)
        {
            m_Patient = patient;
        }
        public void SetInteractable(bool interactable)
        {
            m_AddSingleMeshButton.interactable = interactable;
            m_AddLeftRightMeshButton.interactable = interactable;
            m_RemoveMeshButton.interactable = interactable;
            m_MeshList.interactable = interactable;
        }
        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
            if(active) m_MeshList.Display(m_Patient.Brain.Meshes.ToArray());
        }
        public void AddSingleMesh()
        {
            SingleMesh newMesh = new SingleMesh();
            m_MeshList.Add(newMesh);
            m_Patient.Brain.Meshes.Add(newMesh);
        }
        public void AddLeftRightMesh()
        {
            LeftRightMesh leftRightMesh = new LeftRightMesh();
            m_Patient.Brain.Meshes.Add(leftRightMesh);
            m_MeshList.Add(leftRightMesh);
        }
        public void RemoveSelectedMeshes()
        {
            m_MeshList.Remove(m_MeshList.GetObjectsSelected());
            m_Patient.Brain.Meshes = m_MeshList.Objects.ToList();
        }
        #endregion
    }
}