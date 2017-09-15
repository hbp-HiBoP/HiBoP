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
        #endregion

        #region Public Methods
        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
        }
        public void Set(SingleMesh mesh)
        {
            m_MeshFileSelector.File = mesh.Path;
            m_MeshFileSelector.onValueChanged.RemoveAllListeners();
            m_MeshFileSelector.onValueChanged.AddListener((path) => mesh.Path = path);

            m_MarsAtlasFileSelector.File = mesh.MarsAtlasPath;
            m_MarsAtlasFileSelector.onValueChanged.RemoveAllListeners();
            m_MarsAtlasFileSelector.onValueChanged.AddListener((path) => mesh.MarsAtlasPath = path);
        }
        #endregion

        #region Private Methods

        #endregion
    }
}