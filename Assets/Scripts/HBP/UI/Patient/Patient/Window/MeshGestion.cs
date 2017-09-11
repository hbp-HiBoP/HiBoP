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
        [SerializeField] Text m_MeshCounter;
        [SerializeField] Button m_AddSingleMeshButton;
        [SerializeField] Button m_AddLeftRightMeshButton;
        [SerializeField] Button m_RemoveMeshButton;
        Data.Patient m_Patient;
        bool m_Interactable;
        public virtual bool interactable
        {
            get
            {
                return m_Interactable;
            }
            set
            {
                m_Interactable = value;
                m_AddSingleMeshButton.interactable = value;
                m_AddLeftRightMeshButton.interactable = value;
                m_RemoveMeshButton.interactable = value;
                m_MeshList.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public void Save()
        {
            m_MeshList.SaveAll();
        }
        public void Set(Data.Patient patient)
        {
            m_Patient = patient;
            m_MeshList.OnSelectionChanged.RemoveAllListeners();
            m_MeshList.OnSelectionChanged.AddListener((mesh, i) => m_MeshCounter.text = m_MeshList.ObjectsSelected.Length.ToString());
        }
        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
            if(active) m_MeshList.Objects = m_Patient.Brain.Meshes.ToArray();

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
            m_MeshList.Remove(m_MeshList.ObjectsSelected);
            m_Patient.Brain.Meshes = m_MeshList.Objects.ToList();
            m_MeshCounter.text = m_MeshList.ObjectsSelected.Count().ToString();
        }
        #endregion
    }
}