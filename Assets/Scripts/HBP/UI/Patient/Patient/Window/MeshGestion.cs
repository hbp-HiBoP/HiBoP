using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HBP.Data.Anatomy;

namespace HBP.UI.Anatomy
{
    public class MeshGestion : MonoBehaviour
    {
        #region Properties
        [SerializeField] MeshList m_MeshList;
        [SerializeField] ItemModifier<Data.Anatomy.Mesh> m_Modifier;
        [SerializeField] GameObject m_ModifierPrefab;
        [SerializeField] Text m_MeshCounter;
        [SerializeField] Button m_AddMeshButton;
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
                m_AddMeshButton.interactable = value;
                m_RemoveMeshButton.interactable = value;
                m_MeshList.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public void Set(Data.Patient patient)
        {
            m_Patient = patient;
            m_MeshList.OnSelectionChanged.RemoveAllListeners();
            m_MeshList.OnSelectionChanged.AddListener((mesh, i) => m_MeshCounter.text = m_MeshList.ObjectsSelected.Length.ToString());

            m_MeshList.OnAction.RemoveAllListeners();
            m_MeshList.OnAction.AddListener((mesh, i) => OpenModifier(mesh, interactable));

            m_MeshList.Objects = m_Patient.Brain.Meshes.ToArray();
            m_MeshList.SortByName(MeshList.Sorting.Descending);
        }
        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
            m_MeshList.SortByName(MeshList.Sorting.Descending);
        }
        public void AddMesh()
        {
            OpenModifier(new LeftRightMesh(), interactable);
        }
        public void RemoveSelectedMeshes()
        {
            m_MeshList.Remove(m_MeshList.ObjectsSelected);
            m_Patient.Brain.Meshes = m_MeshList.Objects.ToList();
            m_MeshCounter.text = m_MeshList.ObjectsSelected.Count().ToString();
        }
        public void Save()
        {
            m_Patient.Brain.Meshes = m_MeshList.Objects.ToList();
        }
        #endregion

        #region Private Methods
        void OpenModifier(Data.Anatomy.Mesh mesh,bool interactable)
        {
            RectTransform obj = Instantiate(m_ModifierPrefab).GetComponent<RectTransform>();
            obj.SetParent(GameObject.Find("Windows").transform);
            obj.localPosition = new Vector3(0, 0, 0);
            m_Modifier = obj.GetComponent<ItemModifier<Data.Anatomy.Mesh>>();
            m_Modifier.Open(mesh, interactable);
            m_Modifier.CloseEvent.AddListener(() => OnCloseModifier());
            m_Modifier.SaveEvent.AddListener(() => OnSaveModifier());
        }
        void OnCloseModifier()
        {
            m_Modifier = null;
        }
        void OnSaveModifier()
        {
            m_Patient.Brain.Meshes = m_MeshList.Objects.ToList();
            int index = m_MeshList.Objects.ToList().FindIndex((m) => m.Equals(m_Modifier.Item));
            if (index < 0)
            {
                m_MeshList.Add(m_Modifier.Item);
            }
            else
            {
                m_MeshList.Objects[index] = m_Modifier.Item;
                m_MeshList.UpdateObject(m_Modifier.Item);
            }
            m_MeshList.SortByNone();
        }
        #endregion
    }
}