using UnityEngine;
using HBP.Data.Anatomy;
using Tools.Unity;

namespace HBP.UI.Anatomy
{
    public class SingleMeshGestion : MonoBehaviour
    {
        #region Properties
        [SerializeField] FileSelector m_MeshFileSelector;
        [SerializeField] FileSelector m_MarsAtlasFileSelector;

        bool m_interactable;
        public bool interactable
        {
            get { return m_interactable; }
            set
            {
                m_interactable = value;
                m_MeshFileSelector.interactable = value;
                m_MarsAtlasFileSelector.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
        }
        public void Set(SingleMesh mesh)
        {
            m_MeshFileSelector.File = mesh.SavedPath;
            m_MeshFileSelector.onValueChanged.RemoveAllListeners();
            m_MeshFileSelector.onValueChanged.AddListener((path) => mesh.Path = path);

            m_MarsAtlasFileSelector.File = mesh.SavedMarsAtlasPath;
            m_MarsAtlasFileSelector.onValueChanged.RemoveAllListeners();
            m_MarsAtlasFileSelector.onValueChanged.AddListener((marsAtlasPath) => mesh.MarsAtlasPath = marsAtlasPath);
        }
        #endregion

        #region Private Methods

        #endregion
    }
}